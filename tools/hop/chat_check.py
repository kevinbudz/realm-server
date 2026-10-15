#!/usr/bin/env python3
"""Scripted chat check for the chat command framework (W11), over the wire to a local test server.

Two modes, both headless and both on the same throwaway server (never the live one):

  seed  --accounts FILE --http PORT --tcp PORT
        Registers the test accounts. Each one registers from its own loopback address, because the
        server allows one registration per IP per start (Database.cs MaxRegisteredAccounts). Each account
        is then entered once so it has a character. Writes FILE with names, passwords, IPs and charIds.
  run   --accounts FILE --http PORT --tcp PORT --out FILE.jsonl [--who P,A,T1] [--skip-destructive]
        Logs the accounts into the Nexus and sends the command script (SCRIPT below). Every server reply is written as one
        JSON line: the Text packets (name, object id, stars, bubble, recipient, text), Notification and
        GlobalNotification bodies (hex), Failure packets, and Reconnect transfers. A Reconnect is followed
        automatically, the way the realm client does it. Replies are compared between builds by diffing the output.

Packet ids and framing come from hop_client.py (same wire format). The chat cooldown is 200 ms, so the
script waits between messages.

Accounts are referred to by key: T1 is a witness that stays in the realm (summon/tp/tell target), T2 is an
offline target for ban, rename and kick, P is a rank-0 account and A is the ranked account.
"""
import argparse
import http.client
import json
import os
import re
import struct
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hop_client as hc  # noqa: E402

PKT_PLAYER_TEXT = 5
PKT_TEXT = 6
PKT_NOTIFICATION = 10
PKT_GLOBAL_NOTIFICATION = 60
REPLY_WAIT = 0.35      # seconds to collect replies after each message
MESSAGE_GAP = 0.30     # > ChatCooldownMS (200 ms)
ROGUE = 0x0300
UPTIME_RE = re.compile(r"The server has been up for \d\dh:\d\dm:\d\ds\.")

# Commands whose side effects end the test session or disconnect the witnesses. P (rank 0) still sends them,
# because the rank check must deny them without side effects. A (ranked) skips them.
DESTRUCTIVE = {"/terminate", "/stop", "/reboot", "/disconnect", "/dcAll", "/dc", "/closerealm", "/quake"}

# (who, text). who = "ALL" sends to P and A (in that order); skip-destructive removes DESTRUCTIVE commands for A.
SCRIPT = [
    # Nexus part: hostile monsters do not shoot here, so the headless client never has to ack projectiles.
    "/allyshots", "/allyshots", "/allydamage", "/effects", "/sounds", "/notifications",
    "/online", "/who", "/server", "/pos", "/loc", "/where chkttwo", "/find chkaone",
    "/g hello", "/guild hello", "/trade chkttwo", "/trade", "/fame", "/famestats", "/stats",
    "/commands", "/uptime", "/position", "/time", "/world", "/pause", "/pause",
    "/ignore chkttwo", "/unignore chkttwo", "/lock chkttwo", "/unlock chkttwo",
    "/join", "/join nosuchguild", "/invite chkttwo", "/ginvite chkttwo", "/gwho", "/mates",
    "/servers", "/svrs", "/gkick", "/gkick chkttwo", "/lefttomax", "/ghall",
    "/tell chktone hi", "/t chktone hi", "/l hello local", "/tp chktone", "/gland", "/glands",
    "/gimme", "/give nosuchitem", "/create", "/spawn 2 zzznotathing",
    "/max", "/god", "/god", "/size 50", "/size 0", "/size 999", "/reskin 0", "/reskin 9999",
    "/eff slowed", "/eff nosuch", "/eff slowed",
    # /legendary 4 hangs the server on the seeded Rogue (slot 4 holds an item that never rolls T7: the while loop in
    # Player.UseItem-era /legendary never ends). Pre-existing; not run here. Slot 2 is empty, so this is a no-op.
    "/legendary 2", "/roll",
    "/clearinv", "/clearspawn", "/cs", "/cleargraves", "/cgraves", "/killall", "/ka",
    "/getquest", "/oryxsay hi", "/osay hi", "/announce hello all",
    "/summon chktone", "/summonall", "/killplayer chkttwo", "/tppos 500 500", "/goto 1 1",
    "/setpiece", "/setpiece nosuch", "/wall", "/wall nosuch", "/tq",
    "/mute chkttwo 5", "/unmute chkttwo", "/ban chkttwo test reason", "/unban chkttwo",
    "/banip chkttwo test", "/ipban chkttwo test", "/unban chkttwo", "/kick chkttwo",
    "/rename chkttwo chkttwob", "/unname chkttwob", "/grank chkttwo nosuch", "/grank chkttwo 20",
    "/compactloh", "/quake Nexus", "/link x", "/unlink x", "/level20", "/l20",
    "/bot", "/bot list", "/bot spawn 1", "/bot clear 1", "/bot clearall", "/bot follow", "/bot nosuch",
    "/badpacket", "/p5", "/p13",
    "/vault", "/nexus", "/realm",
    # Realm tail: only the commands whose effect needs a realm. Kept short so no monster volley is ever unacked.
    "/gland", "/glands", "/clearspawn", "/cs", "/cleargraves", "/cgraves", "/killall", "/ka",
    "/killplayer chkttwo", "/tq", "/tppos 500 500", "/goto 1 1", "/wall nosuch", "/setpiece nosuch",
    "/quake Nexus", "/visit chktone",
    # Denial-only probes for P; A skips these (DESTRUCTIVE). Each one is denied for rank 0 without side effects.
    "/terminate", "/stop", "/reboot", "/disconnect", "/dcAll", "/dc", "/closerealm", "/quake",
    "/spawn 1 Pirate",
    "/nosuchcommand",
    "/MAX",
]


def u16_string(buf, pos):
    n = struct.unpack_from(">H", buf, pos)[0]
    return buf[pos + 2:pos + 2 + n].decode("utf-8", "replace"), pos + 2 + n


def parse_text(body):
    name, pos = u16_string(body, 0)
    obj = struct.unpack_from(">i", body, pos)[0]; pos += 4
    stars = struct.unpack_from(">i", body, pos)[0]; pos += 4
    bubble = body[pos]; pos += 1
    to, pos = u16_string(body, pos)
    text, pos = u16_string(body, pos)
    return {"t": "Text", "name": name, "obj": obj, "stars": stars, "bubble": bubble, "to": to, "text": text}


class Account:
    def __init__(self, key, rec, host, tcp):
        self.key, self.name, self.pw, self.ip = key, rec["name"], rec["pw"], rec["ip"]
        self.char = rec.get("char")
        self.host, self.tcp = host, tcp
        self.conn = None

    def _connect(self, game_id):
        self.conn = hc.Conn(self.host, self.tcp, source=(self.ip, 0))
        self.conn.send(hc.PKT_HELLO, hc.hello_body(self.name, self.pw, game_id))
        self.conn.wait_for({hc.PKT_MAP_INFO}, 10, "MapInfo")

    def enter_nexus(self):
        self._connect(hc.NEXUS_ID)
        self.conn.send(hc.PKT_LOAD, struct.pack(">i", self.char))
        self.conn.wait_for({hc.PKT_CREATE_SUCCESS}, 10, "CreateSuccess")
        self.conn.drain_for(0.3)

    def create_char(self):
        self._connect(hc.REALM_ID)
        self.conn.send(hc.PKT_CREATE, struct.pack(">hh", ROGUE, 0))
        _, _, body = self.conn.wait_for({hc.PKT_CREATE_SUCCESS}, 10, "CreateSuccess (new)")
        self.char = struct.unpack(">ii", body[:8])[1]
        self.conn.drain_for(0.3)
        self.conn.close()
        self.conn = None

    def collect(self, seconds):
        events, reconnect = [], None
        end = time.monotonic() + seconds
        while time.monotonic() < end:
            self.conn._read_some(max(0.0, min(0.05, end - time.monotonic())))
        keep = []
        for _, pid, body in self.conn.queue:
            if pid == hc.PKT_RECONNECT:
                game_id = struct.unpack(">i", body[:4])[0]
                events.append({"t": "Reconnect", "gameId": game_id})
                reconnect = game_id
            elif pid == PKT_TEXT:
                events.append(parse_text(body))
            elif pid == PKT_NOTIFICATION:
                events.append({"t": "Notification", "hex": body.hex()})
            elif pid == PKT_GLOBAL_NOTIFICATION:
                events.append({"t": "GlobalNotification", "hex": body.hex()})
            elif pid == hc.PKT_FAILURE:
                events.append({"t": "Failure", "hex": body.hex()})
            else:
                keep.append((0, pid, body))
        self.conn.queue = []
        return events, reconnect

    def chat(self, text):
        self.conn.send(PKT_PLAYER_TEXT, hc.s16(text))
        events, reconnect = self.collect(REPLY_WAIT)
        if reconnect is not None:
            self.conn.close()
            self._connect(reconnect)
            self.conn.send(hc.PKT_LOAD, struct.pack(">i", self.char))
            self.conn.wait_for({hc.PKT_CREATE_SUCCESS}, 10, "CreateSuccess (transfer)")
            self.conn.drain_for(0.3)
        return events


def read_script(path):
    with open(path) as fh:
        return [line.strip() for line in fh if line.strip()]


def normalize(events):
    for ev in events:
        if ev.get("t") == "Text":
            ev["text"] = UPTIME_RE.sub("<UPTIME>", ev["text"])
    return events


def seed(args):
    port = args.http
    out = {"http": port, "tcp": args.tcp, "accounts": {}}
    for key, name, ip in [("P", "chkpone", "127.0.0.2"), ("A", "chkaone", "127.0.0.3"),
                          ("T1", "chktone", "127.0.0.4"), ("T2", "chkttwo", "127.0.0.5")]:
        pw = "pw" + name
        body = "newUsername=%s&newPassword=%s" % (name, pw)
        h = http.client.HTTPConnection("127.0.0.1", port, timeout=35, source_address=(ip, 0))
        h.request("POST", "/account/register", body=body, headers={"Content-Type": "application/x-www-form-urlencoded"})
        reply = h.getresponse().read().decode("utf-8", "replace")
        h.close()
        if "Success" not in reply:
            raise SystemExit(f"register {name} failed: {reply[:120]}")
        acc = Account(key, {"name": name, "pw": pw, "ip": ip}, "127.0.0.1", args.tcp)
        acc.create_char()
        out["accounts"][key] = {"name": name, "pw": pw, "ip": ip, "char": acc.char}
        print(f"seeded {key} name={name} charId={acc.char}", flush=True)
    with open(args.accounts, "w") as fh:
        json.dump(out, fh, indent=1)


def run(args):
    with open(args.accounts) as fh:
        spec = json.load(fh)
    who = args.who.split(",")
    accounts = {k: Account(k, v, "127.0.0.1", spec["tcp"]) for k, v in spec["accounts"].items()}
    # T1 witnesses from the start, so tell/tp/summon/visit have a target in the realm.
    order = ["T1"] + [k for k in who if k != "T1"]
    for k in order:
        accounts[k].enter_nexus()
    count = 0
    with open(args.out, "w") as fh:
        for text in (read_script(args.script) if args.script else SCRIPT):
            for k in who:
                if k == "A" and args.skip_destructive and text in DESTRUCTIVE:
                    continue
                events = normalize(accounts[k].chat(text))
                fh.write(json.dumps({"who": k, "cmd": text, "events": events}) + "\n")
                count += 1
    print(f"sent {count} messages for {who}", flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("mode", choices=["seed", "run"])
    ap.add_argument("--accounts", required=True)
    ap.add_argument("--http", type=int, default=7877)
    ap.add_argument("--tcp", type=int, default=2060)
    ap.add_argument("--out", default=None)
    ap.add_argument("--who", default="P,A")
    ap.add_argument("--skip-destructive", action="store_true")
    ap.add_argument("--script", default=None, help="file with one command per line; replaces SCRIPT")
    args = ap.parse_args()
    if args.mode == "seed":
        seed(args)
    else:
        if not args.out:
            raise SystemExit("run needs --out")
        run(args)


if __name__ == "__main__":
    main()
