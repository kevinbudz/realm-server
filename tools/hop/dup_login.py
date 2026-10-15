#!/usr/bin/env python3
"""Duplicate-login and relog check for the hop harness server (W02 gate).

usage: python3 -I tools/hop/dup_login.py [--http 7877] [--tcp 2060] --db <throwaway DB dir> --out <result.json>

Run it against a server started by server_ctl.sh with a fresh DB (one registration per IP per start).
Flow, one throwaway account:
  1. client A: Hello(-2) -> MapInfo. Verify /account/verify immediately (inside the Hello save window),
     then Create -> CreateSuccess. While A is live, /account/verify and /char/list must say "Account in use!".
  2. client B: Hello(-2) with the same credentials. A must be kicked (its socket closes) and B must get MapInfo.
     B then Loads and reaches CreateSuccess. While B is live, /account/verify must say "Account in use!".
  3. the account row in the throwaway realm.db shows Connected=True while B is live, and False after B closes.
  4. close B, settle, capture /char/list (A1). Relog (Hello + Load), close, settle, capture /char/list (A2).
     A1 must equal A2, and the relog must load the same character.
Credentials are generated per process and never printed. Exit 0 only if every check passes.
"""
import argparse
import json
import os
import random
import re
import sqlite3
import string
import struct
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hop_client as hc  # noqa: E402  (protocol helpers shared with the hop harness)

SETTLE = 1.0  # seconds for the writer to commit queued saves before reading the DB


def post(http, path, fields):
    # AppServer reads form parameters from the POST body (AppServer.cs:53), for every route.
    body = urllib.parse.urlencode(fields).encode("ascii")
    req = urllib.request.Request(f"http://127.0.0.1:{http}{path}", data=body,
                                 headers={"Content-Type": "application/x-www-form-urlencoded"})
    try:
        with urllib.request.urlopen(req, timeout=35) as resp:
            return resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as exc:
        return exc.read().decode("utf-8", "replace")


def db_row(db_dir, path):
    con = sqlite3.connect(f"file:{os.path.join(db_dir, 'realm.db')}?mode=ro", uri=True, timeout=5)
    try:
        row = con.execute("SELECT value FROM kv WHERE path = ?", (path,)).fetchone()
    finally:
        con.close()
    return row[0] if row else None


def db_connected(db_dir, acc_id):
    xml = db_row(db_dir, f"account.{acc_id}") or ""
    m = re.search(r"<Connected>(\w+)</Connected>", xml)  # XElement writes bools as true/false
    return m.group(1).capitalize() if m else None


def wait_eof(conn, timeout):
    end = time.monotonic() + timeout
    while not conn.eof and time.monotonic() < end:
        conn._read_some(0.05)
        conn.queue.clear()
    return conn.eof


def wait_char_list(http, user, pw, timeout):
    end = time.monotonic() + timeout
    last = ""
    while time.monotonic() < end:
        last = post(http, "/char/list", {"username": user, "password": pw})
        if "<Chars" in last:
            return last
        time.sleep(0.2)
    raise TimeoutError(f"char/list never succeeded: {last[:120]}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--http", type=int, default=7877)
    ap.add_argument("--tcp", type=int, default=2060)
    ap.add_argument("--db", required=True, help="throwaway DB dir that holds realm.db")
    ap.add_argument("--timeout", type=float, default=10.0)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    rnd = random.SystemRandom()
    user = "dup" + "".join(rnd.choice(string.ascii_lowercase + string.digits) for _ in range(8))
    pw = "".join(rnd.choice(string.ascii_letters + string.digits) for _ in range(16))
    checks = []

    def check(name, ok, detail=""):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})
        print(f"{'PASS' if ok else 'FAIL'} {name}" + (f": {detail}" if detail else ""), flush=True)

    reg = hc.register(args.http, user, pw)
    if "Success" not in reg:
        print("registration failed; not continuing", file=sys.stderr)
        return 1

    # 1. client A enters the realm; verify right after MapInfo, inside the Hello save window.
    a = hc.Conn(args.host, args.tcp)
    a.send(hc.PKT_HELLO, hc.hello_body(user, pw, hc.REALM_ID))
    a.wait_for({hc.PKT_MAP_INFO}, args.timeout, "MapInfo (A)")
    window = post(args.http, "/account/verify", {"username": user, "password": pw})
    check("A: verify right after MapInfo says in use", "Account in use!" in window, window[:100])
    a.send(hc.PKT_CREATE, struct.pack(">hh", hc.ROGUE, 0))
    _, _, body = a.wait_for({hc.PKT_CREATE_SUCCESS}, args.timeout, "CreateSuccess (A)")
    char_id = struct.unpack(">ii", body[:8])[1]
    a.drain_for(0.5)
    live_list = post(args.http, "/char/list", {"username": user, "password": pw})
    check("A live: /char/list says in use", "Account in use!" in live_list, live_list[:100])
    row = db_row(args.db, f"login.username.{user}")  # Database.IdFromUsername key
    acc_id = int(row) if row and row.isdigit() else None
    check("account id resolved from login.username", acc_id is not None, f"accountId={acc_id}")
    time.sleep(SETTLE)
    check("DB: Connected=True while A is live", db_connected(args.db, acc_id) == "True",
          f"Connected={db_connected(args.db, acc_id)}")

    # 2. client B, same credentials: A must be kicked, B must get in.
    b = hc.Conn(args.host, args.tcp)
    b.send(hc.PKT_HELLO, hc.hello_body(user, pw, hc.REALM_ID))
    check("second Hello kicks live session A (A socket closes)", wait_eof(a, args.timeout), "")
    try:
        b.wait_for({hc.PKT_MAP_INFO}, args.timeout, "MapInfo (B)")
        check("second Hello gets MapInfo (B)", True)
        b.send(hc.PKT_LOAD, struct.pack(">i", char_id))
        b.wait_for({hc.PKT_CREATE_SUCCESS}, args.timeout, "CreateSuccess (B)")
        check("B Load reaches CreateSuccess with the same character", True)
        b.drain_for(0.5)
    except Exception as exc:
        check("B enters the realm", False, str(exc)[:160])
    check("B live: verify says in use", "Account in use!" in post(args.http, "/account/verify",
                                                                 {"username": user, "password": pw}))
    time.sleep(SETTLE)
    check("DB: Connected=True while B is live", db_connected(args.db, acc_id) == "True",
          f"Connected={db_connected(args.db, acc_id)}")

    # 3-4. close B, settle, capture the character list; relog; capture again.
    b.close()
    time.sleep(SETTLE)
    check("DB: Connected=False after B closes", db_connected(args.db, acc_id) == "False",
          f"Connected={db_connected(args.db, acc_id)}")
    list1 = wait_char_list(args.http, user, pw, args.timeout)
    time.sleep(SETTLE)
    c = None
    try:
        c = hc.reenter_realm(args.host, args.tcp, user, pw, char_id, args.timeout)
        check("relog: Hello + Load reaches CreateSuccess with the same character", True)
    except Exception as exc:
        check("relog: Hello + Load reaches CreateSuccess", False, str(exc)[:160])
    if c is not None:
        time.sleep(SETTLE)
        check("relog: DB Connected=True again", db_connected(args.db, acc_id) == "True",
              f"Connected={db_connected(args.db, acc_id)}")
        c.close()
    time.sleep(SETTLE)
    list2 = wait_char_list(args.http, user, pw, args.timeout)
    check("char list after relog equals char list before relog", list1 == list2,
          f"len={len(list1)} vs {len(list2)}")

    # 5. no settle: a second Hello sent as soon as the first MapInfo arrives, while the first
    #    session's Connected=true save may still be queued on the writer.
    x = hc.Conn(args.host, args.tcp)
    x.send(hc.PKT_HELLO, hc.hello_body(user, pw, hc.REALM_ID))
    x.wait_for({hc.PKT_MAP_INFO}, args.timeout, "MapInfo (X)")
    y = hc.Conn(args.host, args.tcp)
    y.send(hc.PKT_HELLO, hc.hello_body(user, pw, hc.REALM_ID))
    check("immediate second Hello kicks first session X", wait_eof(x, args.timeout), "")
    try:
        y.wait_for({hc.PKT_MAP_INFO}, args.timeout, "MapInfo (Y)")
        y.send(hc.PKT_LOAD, struct.pack(">i", char_id))
        y.wait_for({hc.PKT_CREATE_SUCCESS}, args.timeout, "CreateSuccess (Y)")
        check("immediate second Hello gets in (MapInfo + CreateSuccess)", True)
    except Exception as exc:
        check("immediate second Hello gets in", False, str(exc)[:160])
    y.close()
    time.sleep(SETTLE)
    check("DB: Connected=False after immediate-test session closes", db_connected(args.db, acc_id) == "False",
          f"Connected={db_connected(args.db, acc_id)}")

    failed = sum(1 for ch in checks if not ch["ok"])
    with open(args.out, "w") as fh:
        json.dump({"checks": checks, "failed": failed, "accountId": acc_id, "charId": char_id}, fh, indent=2)
    print(f"dup_login: {len(checks) - failed}/{len(checks)} checks passed -> {args.out}", flush=True)
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
