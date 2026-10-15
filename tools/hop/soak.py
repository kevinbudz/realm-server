#!/usr/bin/env python3
"""Soak and double-spend check for the W03 writer changes. Only for a server started by server_ctl.sh.

usage: python3 -I tools/hop/soak.py --http 7877 --tcp 2060 --db <throwaway DB dir> [--seconds 150]
           [--hop-clients 4] [--server-log <server.log>] [--out <result.json>]

The server allows one registration per IP per start, so setup registers ONE base account over HTTP and clones the
rest straight into the throwaway realm.db (same password and hash). Hop accounts create their character in game.
Purchase accounts get fame and credits and never log in, so the /account/* routes accept them.

Load for --seconds: hop clients loop hops; one thread calls purchaseCharSlot, one purchaseSkin and one setName on the
purchase accounts. Afterwards: the stored rows and /char/list must match the successes the clients counted; hop
accounts relog; two concurrent double-spend checks (8 requests for one slot, 8 renames for one 1000-credit payment)
must give exactly one success each. With --server-log, the tick durations logged by REALM_TICKTRACE=1 must stay
under 50 ms, and no HTTP call or hop may take 1 s or more.

Exit 0 only if every check passes. Credentials are generated per process and never printed.
"""
import argparse
import json
import os
import random
import re
import sqlite3
import string
import sys
import threading
import time
import urllib.parse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hop_client as hc  # noqa: E402  (protocol helpers shared with the hop harness)
import dup_login  # noqa: E402  (post() helper)

CHAR_SLOT_PRICE = 2000  # Database.CharSlotPrice (fame)
SKIN_PRICE = 1000  # Database.SkinPrice (credits)
RENAME_PRICE = 1000  # AppServer.AccountSetName (credits)
TICK_LIMIT_MS = 50.0
STALL_LIMIT_MS = 1000.0
SKIN_TYPES = [0x0342 + i for i in range(15)]  # Class="Skin" entries in EmbeddedData_SkinsCXML.xml (15 skins)
TICK_RE = re.compile(r"\[tick\] start=([\d.]+) period=([\d.]+) work=(\d+)")


def rand_text(n, alphabet=string.ascii_lowercase + string.digits):
    rnd = random.SystemRandom()
    return "".join(rnd.choice(alphabet) for _ in range(n))


def connect_rw(db_dir):
    con = sqlite3.connect(os.path.join(db_dir, "realm.db"), timeout=10, isolation_level=None)
    con.execute("PRAGMA busy_timeout = 10000")
    return con


def kv(db_dir, path):
    con = sqlite3.connect(f"file:{os.path.join(db_dir, 'realm.db')}?mode=ro", uri=True, timeout=10)
    try:
        row = con.execute("SELECT value FROM kv WHERE path = ?", (path,)).fetchone()
    finally:
        con.close()
    return row[0] if row else None


def clone_accounts(db_dir, base_id, count, fame, credits):
    """Copies the base account row and its login keys (same password) into `count` new accounts."""
    base_xml = kv(db_dir, f"account.{base_id}")
    base_hash = kv(db_dir, f"login.hash.{base_id}")
    base_salt = kv(db_dir, f"login.salt.{base_id}")
    assert base_xml and base_hash and base_salt, "base account not found"
    con = connect_rw(db_dir)
    out = []
    try:
        con.execute("BEGIN IMMEDIATE")
        nxt = int(con.execute("SELECT value FROM kv WHERE path = '@nextAccId'").fetchone()[0])
        for _ in range(count):
            aid = nxt
            nxt += 1
            name = "sk" + rand_text(8)
            xml = re.sub(r"<AccountId>\d+</AccountId>", f"<AccountId>{aid}</AccountId>", base_xml)
            xml = re.sub(r"<AliveChars>[^<]*</AliveChars>", "<AliveChars></AliveChars>", xml)
            xml = re.sub(r"<NextCharId>\d+</NextCharId>", "<NextCharId>0</NextCharId>", xml)
            xml = re.sub(r"<OwnedSkins>[^<]*</OwnedSkins>", "<OwnedSkins></OwnedSkins>", xml)
            xml = re.sub(r"<Connected>\w+</Connected>", "<Connected>false</Connected>", xml)
            xml = re.sub(r"<Fame>\d+</Fame>", f"<Fame>{fame}</Fame>", xml)
            xml = re.sub(r"<Credits>\d+</Credits>", f"<Credits>{credits}</Credits>", xml)
            for path, value in [(f"account.{aid}", xml), (f"login.username.{name}", str(aid)),
                                (f"login.id.{aid}", name), (f"login.hash.{aid}", base_hash),
                                (f"login.salt.{aid}", base_salt)]:
                con.execute("INSERT INTO kv(path, value) VALUES(?, ?) ON CONFLICT(path) DO UPDATE SET value = excluded.value",
                            (path, value))
            out.append({"id": aid, "name": name})
        con.execute("INSERT INTO kv(path, value) VALUES('@nextAccId', ?) ON CONFLICT(path) DO UPDATE SET value = excluded.value",
                    (str(nxt),))
        con.execute("COMMIT")
    except Exception:
        con.execute("ROLLBACK")
        raise
    finally:
        con.close()
    return out


def stored_account(db_dir, aid):
    xml = kv(db_dir, f"account.{aid}") or ""

    def num(tag):
        m = re.search(rf"<{tag}>(-?\d+)</{tag}>", xml)
        return int(m.group(1)) if m else None
    owned = re.search(r"<OwnedSkins>([^<]*)</OwnedSkins>", xml)
    return {"fame": num("Fame"), "credits": num("Credits"), "maxNumChars": num("MaxNumChars"),
            "ownedSkins": [int(x) for x in owned.group(1).split(",") if x] if owned else []}


def username_keys_for(db_dir, aid):
    con = sqlite3.connect(f"file:{os.path.join(db_dir, 'realm.db')}?mode=ro", uri=True, timeout=10)
    try:
        rows = con.execute("SELECT path FROM kv WHERE path LIKE 'login.username.%' AND value = ?", (str(aid),)).fetchall()
    finally:
        con.close()
    return [r[0] for r in rows]


def hop_worker(args, account, deadline, result):
    user, pw = account["user"], account["pw"]
    hops, lat, errors = 0, [], []
    conn = None
    try:
        conn, char_id = hc.enter_first_time(args.host, args.tcp, user, pw, args.timeout)
        while time.monotonic() < deadline:
            conn, rec = hc.one_hop(args.host, args.tcp, user, pw, char_id, conn, args.hop_settle, args.timeout)
            hops += 1
            lat.append(rec["escape_to_first_update_ms"])
            conn.close()
            conn = hc.reenter_realm(args.host, args.tcp, user, pw, char_id, args.timeout)
        conn.close()
        result.update(hops=hops, lat=lat, errors=errors, char_id=char_id)
    except Exception as exc:  # a broken hop is a failure, recorded not hidden
        errors.append(str(exc)[:200])
        try:
            if conn is not None:
                conn.close()
        except Exception:
            pass
        result.update(hops=hops, lat=lat, errors=errors, char_id=None)


def http_loop(args, call, accounts, deadline, out):
    """Calls one route in a loop over the purchase accounts until the deadline. out gets every reply."""
    i = 0
    while time.monotonic() < deadline:
        acc = accounts[i % len(accounts)]
        t0 = time.monotonic()
        reply, extra = call(acc, i)
        out.append({"acc": acc["id"], "reply": reply, "ms": (time.monotonic() - t0) * 1000.0, **extra})
        i += 1


def tick_stats(log_path, warmup=10):
    works, periods = [], []
    with open(log_path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            m = TICK_RE.search(line)
            if m:
                periods.append(float(m.group(2)))
                works.append(int(m.group(3)))
    works, periods = works[warmup:], periods[warmup:]
    if not works:
        return None
    return {"ticks": len(works), "work_max_ms": max(works), "work_mean_ms": sum(works) / len(works),
            "period_max_ms": max(periods), "period_min_ms": min(periods)}


def race(n, fn):
    """Runs fn(i) on n threads that start together; returns the replies in thread order."""
    barrier = threading.Barrier(n)
    replies = [None] * n

    def run(i):
        barrier.wait()
        replies[i] = fn(i)
    threads = [threading.Thread(target=run, args=(i,)) for i in range(n)]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    return replies


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--http", type=int, default=7877)
    ap.add_argument("--tcp", type=int, default=2060)
    ap.add_argument("--db", required=True, help="throwaway DB dir that holds realm.db")
    ap.add_argument("--seconds", type=float, default=150.0)
    ap.add_argument("--hop-clients", type=int, default=4)
    ap.add_argument("--hop-settle", type=float, default=0.2)
    ap.add_argument("--timeout", type=float, default=10.0)
    ap.add_argument("--server-log", default=None, help="server log written with REALM_TICKTRACE=1")
    ap.add_argument("--no-http", action="store_true", help="diagnostics: hop clients only")
    ap.add_argument("--no-hops", action="store_true", help="diagnostics: HTTP threads only")
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    checks = []

    def check(name, ok, detail=""):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})
        print(f"{'PASS' if ok else 'FAIL'} {name}" + (f": {detail}" if detail else ""), flush=True)

    base_user = "sk" + rand_text(8)
    base_pw = rand_text(16, string.ascii_letters + string.digits)
    reg = hc.register(args.http, base_user, base_pw)
    if "Success" not in reg:
        print("base registration failed; not continuing", file=sys.stderr)
        return 1
    base_id = int(kv(args.db, f"login.username.{base_user}"))

    hop_accts = clone_accounts(args.db, base_id, args.hop_clients, 0, 0)
    #Slot and skin purchases use buy_accts (stable login names). Renames change the login name, so they use name_accts.
    buy_accts = clone_accounts(args.db, base_id, 4, 100000, 100000)
    name_accts = clone_accounts(args.db, base_id, 4, 0, 100000)
    for a in hop_accts + buy_accts + name_accts:
        a["user"], a["pw"] = kv(args.db, f"login.id.{a['id']}"), base_pw
    all_accts = buy_accts + name_accts
    start = {a["id"]: stored_account(args.db, a["id"]) for a in all_accts}
    print(f"setup: {len(hop_accts)} hop accounts, {len(buy_accts)} buy accounts, {len(name_accts)} rename accounts", flush=True)

    # Load phase.
    deadline = time.monotonic() + args.seconds
    hop_results = [dict() for _ in hop_accts]
    hop_threads = [threading.Thread(target=hop_worker, args=(args, a, deadline, r))
                   for a, r in zip(hop_accts, hop_results)]
    slot_out, skin_out, name_out = [], [], []
    names_seen = {}

    def buy_slot(acc, i):
        reply = dup_login.post(args.http, "/account/purchaseCharSlot", {"username": acc["user"], "password": acc["pw"]})
        return reply, {}

    def buy_skin(acc, i):
        skin = SKIN_TYPES[(i // len(buy_accts)) % len(SKIN_TYPES)]
        reply = dup_login.post(args.http, "/account/purchaseSkin",
                               {"username": acc["user"], "password": acc["pw"], "skinType": str(skin)})
        return reply, {"skin": skin}

    def set_name(acc, i):
        new = "sn" + rand_text(10)
        reply = dup_login.post(args.http, "/account/setName", {"username": acc["user"], "password": acc["pw"], "name": new})
        if "<Success/>" in reply:
            acc["user"] = new  #the login name changed; later calls for this account use it
        return reply, {"name": new}

    http_threads = [] if args.no_http else [
        threading.Thread(target=http_loop, args=(args, buy_slot, buy_accts, deadline, slot_out)),
        threading.Thread(target=http_loop, args=(args, buy_skin, buy_accts, deadline, skin_out)),
        threading.Thread(target=http_loop, args=(args, set_name, name_accts, deadline, name_out)),
    ]
    run_threads = ([] if args.no_hops else hop_threads) + http_threads
    for t in run_threads:
        t.start()
    for t in run_threads:
        t.join()

    # Tallies from the replies the clients saw.
    def tally(out, ok_marker, expected_refusals):
        ok = [r for r in out if ok_marker in r["reply"]]
        bad = [r for r in out if ok_marker not in r["reply"] and not any(x in r["reply"] for x in expected_refusals)]
        return ok, bad
    slot_ok, slot_bad = tally(slot_out, "<Success/>", ["Not enough fame", "Account in use!"])
    skin_ok, skin_bad = tally(skin_out, "<Success/>", ["Could not buy skin", "Account in use!"])
    name_ok, name_bad = tally(name_out, "<Success/>", ["Not enough credits", "Duplicated name", "Account in use!"])
    all_http = slot_out + skin_out + name_out
    http_max = max((r["ms"] for r in all_http), default=0.0)
    print(f"load: buy-slot {len(slot_out)} calls ({len(slot_ok)} ok); buy-skin {len(skin_out)} ({len(skin_ok)} ok); "
          f"set-name {len(name_out)} ({len(name_ok)} ok); HTTP max {http_max:.0f} ms", flush=True)
    if not args.no_http:
        check("load: every HTTP reply was a success or an expected refusal",
              not (slot_bad or skin_bad or name_bad),
              f"unexpected: {[r['reply'][:80] for r in (slot_bad + skin_bad + name_bad)[:3]]}")
        check("load: no HTTP call took 1 s or more", http_max < STALL_LIMIT_MS, f"max {http_max:.1f} ms")

    hop_errors = [e for r in hop_results for e in r.get("errors", [])]
    hops_done = sum(r.get("hops", 0) for r in hop_results)
    hop_lat = [x for r in hop_results for x in r.get("lat", [])]
    hop_max = max(hop_lat, default=0.0)
    if not args.no_hops:
        check("load: hop clients completed without errors", not hop_errors and hops_done >= args.hop_clients,
              f"hops={hops_done} errors={hop_errors[:2]}")
        check("load: no hop took 1 s or more", hop_max < STALL_LIMIT_MS, f"max escape->first Update {hop_max:.1f} ms")

    # Stored rows must match what the clients saw.
    for a in all_accts:
        before = start[a["id"]]
        after = stored_account(args.db, a["id"])
        mine = [r for r in slot_out if r["acc"] == a["id"]]
        n_slot = sum(1 for r in mine if "<Success/>" in r["reply"])
        n_skin = sum(1 for r in skin_out if r["acc"] == a["id"] and "<Success/>" in r["reply"])
        n_name = sum(1 for r in name_out if r["acc"] == a["id"] and "<Success/>" in r["reply"])
        last_name = None
        for r in name_out:
            if r["acc"] == a["id"] and "<Success/>" in r["reply"]:
                last_name = r["name"]
        ok_fame = after["fame"] == before["fame"] - CHAR_SLOT_PRICE * n_slot
        ok_slots = after["maxNumChars"] == before["maxNumChars"] + n_slot
        ok_credits = after["credits"] == before["credits"] - SKIN_PRICE * n_skin - RENAME_PRICE * n_name
        ok_skins = sorted(after["ownedSkins"]) == sorted(
            set(r["skin"] for r in skin_out if r["acc"] == a["id"] and "<Success/>" in r["reply"]))
        ok_names = True
        if last_name is not None:
            stored = kv(args.db, f"login.id.{a['id']}")
            ok_names = stored == last_name and username_keys_for(args.db, a["id"]) == [f"login.username.{last_name}"]
        check(f"stored rows match the replies (account {a['id']}: slots {n_slot}, skins {n_skin}, renames {n_name})",
              ok_fame and ok_slots and ok_credits and ok_skins and ok_names,
              f"fame {before['fame']}->{after['fame']} maxChars {before['maxNumChars']}->{after['maxNumChars']} "
              f"credits {before['credits']}->{after['credits']} skins {after['ownedSkins']}")
        listing = dup_login.post(args.http, "/char/list", {"username": a["user"], "password": a["pw"]})
        m = re.search(r'maxNumChars="(\d+)"', listing)
        check(f"/char/list agrees with the stored slot count (account {a['id']})",
              m is not None and int(m.group(1)) == after["maxNumChars"],
              f"listing maxNumChars={m.group(1) if m else None} stored={after['maxNumChars']}")

    # Relog every hop account; its character must load.
    for a, r in zip(hop_accts, hop_results):
        if r.get("char_id") is None:
            check(f"relog hop account {a['id']}", False, "no character from the load phase")
            continue
        try:
            c = hc.reenter_realm(args.host, args.tcp, a["user"], a["pw"], r["char_id"], args.timeout)
            c.close()
            check(f"relog hop account {a['id']} loads its character", True)
        except Exception as exc:
            check(f"relog hop account {a['id']} loads its character", False, str(exc)[:160])

    # Double-spend: 8 concurrent buy-slot requests, fame for exactly one slot.
    ds_slot = clone_accounts(args.db, base_id, 1, CHAR_SLOT_PRICE, 0)[0]
    ds_slot["user"], ds_slot["pw"] = kv(args.db, f"login.id.{ds_slot['id']}"), base_pw
    replies = race(8, lambda i: dup_login.post(args.http, "/account/purchaseCharSlot",
                                              {"username": ds_slot["user"], "password": ds_slot["pw"]}))
    wins = sum(1 for r in replies if "<Success/>" in r)
    st = stored_account(args.db, ds_slot["id"])
    check("double-spend: 8 concurrent buy-slot requests give exactly one success",
          wins == 1 and st["fame"] == 0 and st["maxNumChars"] == 2,
          f"successes={wins} fame={st['fame']} maxNumChars={st['maxNumChars']} replies={[r[:30] for r in replies]}")

    # Double-spend: 8 concurrent renames, credits for exactly one.
    ds_name = clone_accounts(args.db, base_id, 1, 0, RENAME_PRICE)[0]
    ds_name["user"], ds_name["pw"] = kv(args.db, f"login.id.{ds_name['id']}"), base_pw
    tag = rand_text(6)
    replies = race(8, lambda i: dup_login.post(args.http, "/account/setName",
                                              {"username": ds_name["user"], "password": ds_name["pw"],
                                               "name": f"ds{tag}{i}"}))
    wins = sum(1 for r in replies if "<Success/>" in r)
    st = stored_account(args.db, ds_name["id"])
    check("double-spend: 8 concurrent renames give exactly one success",
          wins == 1 and st["credits"] == 0,
          f"successes={wins} credits={st['credits']} replies={[r[:30] for r in replies]}")

    tick = tick_stats(args.server_log) if args.server_log else None
    if args.server_log:
        if tick is None:
            check("tick log present", False, "no [tick] lines; was REALM_TICKTRACE=1 set?")
        else:
            check(f"tick work stays under {TICK_LIMIT_MS:g} ms (max {tick['work_max_ms']} ms over {tick['ticks']} ticks)",
                  tick["work_max_ms"] < TICK_LIMIT_MS, f"mean {tick['work_mean_ms']:.2f} ms")
            check("no tick period of 1 s or more", tick["period_max_ms"] < STALL_LIMIT_MS,
                  f"max period {tick['period_max_ms']:.1f} ms")

    failed = sum(1 for c in checks if not c["ok"])
    summary = {"checks": checks, "failed": failed, "hops": hops_done, "hop_max_ms": hop_max,
               "http_calls": len(all_http), "http_max_ms": http_max, "tick": tick}
    with open(args.out, "w") as fh:
        json.dump(summary, fh, indent=2)
    print(f"soak: {len(checks) - failed}/{len(checks)} checks passed -> {args.out}", flush=True)
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
