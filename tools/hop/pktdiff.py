#!/usr/bin/env python3
"""pktdiff.py - compare two wire captures written by the server under REALM_PKTCAP=<file>.

usage: python3 -I tools/hop/pktdiff.py BASE.bin NEW.bin [--mask ID:OFF:LEN ...] [--show N] [--json OUT]
       python3 -I tools/hop/pktdiff.py --jsonl BASE.jsonl NEW.jsonl   (chat_check replies, positions masked)

File format (Common/PacketCapture.cs): magic "PKTCAP1\\n", then per frame: u32 LE conn, u8 dir ('I' or 'O'),
u32 LE length, then the frame (packet id byte + body, no 4-byte length prefix).

Comparison, per (conn, dir) stream, in order:
  - Every frame except the tick-driven ids (Update 9, NewTick 11) and the excluded frames below must match exactly, in sequence.
  - Tick-driven frames are compared pairwise over the common prefix. Their count depends on wall-clock time
    (how long the client stayed in the world), so a count difference is reported as info, not as a diff. Their
    content differs run to run too (the random spawn position decides what is visible), so content differences are
    counted in tick_noise and not gated unless --include-tick. The W06 golden test (Networking/W06Golden.cs)
    covers their bytes for fixed inputs instead.
  - Excluded from the sequence (reported as info or not compared): Oryx's timer text, random ambient taunts
    (is_ambient_taunt), and the Ping/Pong keepalives (compared as a sorted multiset).
  - --no-normalize turns off the built-in field masks (raw bytes), to show the noise floor.
  Built-in masks in normalize(): Hello user and password, MapInfo display name and seed, CreateSuccess and
  QuestObjId object id, Goto and ShowEffect positions, Text "X:n, Y:n" / "Current Position" / "up for HHh:MMm:SSs"
  (uptime) and Pong's client time. Measured with --no-normalize between two identical baseline runs: Hello, MapInfo,
  CreateSuccess and QuestObjId vary (hop and dup_login); the uptime text and the ambient taunts vary (chat).
  The position masks (Goto, ShowEffect, Text positions) follow the random spawn; they were not isolated separately.
  - Masks (--mask ID:OFF:LEN, OFF counted from the id byte, repeatable) blank bytes that are known to be
    nondeterministic (wall-clock times, allocation-order ids). Use them only after the noise floor is shown.

Exit 0 when there are no diffs, 1 when there are diffs, 2 on a usage or format error.
"""
import argparse
import collections
import json
import struct
import sys

TICK_DRIVEN = {9, 11}  # Update, NewTick
KEEPALIVE = {61, 62}  # Ping (out), Pong (in): sent on a wall-clock timer, so their order against other frames is timing
ID_NAMES = {0: "Failure", 1: "CreateSuccess", 2: "Create", 6: "Text", 9: "Update", 11: "NewTick", 15: "Hello",
            19: "Reconnect", 20: "MapInfo", 21: "Load", 34: "AccountList", 35: "QuestObjId", 42: "Escape",
            61: "Ping", 62: "Pong"}


def parse(path):
    data = open(path, "rb").read()
    if data[:8] != b"PKTCAP1\n":
        raise ValueError(f"{path}: bad magic")
    streams = collections.defaultdict(list)
    p = 8
    while p < len(data):
        if p + 9 > len(data):
            raise ValueError(f"{path}: truncated record header at {p}")
        conn = struct.unpack_from("<I", data, p)[0]
        direction = chr(data[p + 4])
        length = struct.unpack_from("<I", data, p + 5)[0]
        p += 9
        if p + length > len(data):
            raise ValueError(f"{path}: truncated frame at {p}")
        frame = data[p:p + length]
        p += length
        streams[(conn, direction)].append(frame)
    return streams


def _str_span(f, pos):
    """(start, end) of a u16 BE length-prefixed string at pos, or None if it does not fit."""
    if pos + 2 > len(f):
        return None
    n = struct.unpack_from(">H", f, pos)[0]
    if pos + 2 + n > len(f):
        return None
    return pos + 2, pos + 2 + n


NORMALIZE = True


def normalize(frame):
    """Blank fields that are random per server or client run, keeping their lengths where the length is stable.

    Hello (15, inbound): the random throwaway user and password written by hop_client.py.
    MapInfo (20): the random displayName and realm seed (the displayName length differs between runs too).
    CreateSuccess (1) and QuestObjId (35): the world object id, which depends on how many entities spawned earlier.
    Text "Oryx the Mad God" (6): Oryx's timer broadcasts, excluded from the sequence (count is info only).
  Ping (61) and Pong (62): wall-clock keepalives. Their order against other frames is timing, so they are
    compared as a sorted multiset (the count is reported as info, the bytes are compared after masking).
  Goto (16) and ShowEffect (14): positions, which follow the random spawn pick (MathUtils.Next) in the world.
    Text (6): position text quoted by /pos and /server replies (the same random spawn).
    Pong (62, inbound): the client's clock.
    Every other frame is compared exactly.
    """
    if not frame or not NORMALIZE:
        return frame
    b = bytearray(frame)
    mid = b[0]
    if mid == 15:  # Hello: str buildVersion, i32 gameId, str user, str pass, i32
        version = _str_span(b, 1)
        user = _str_span(b, version[1] + 4) if version else None
        pwd = _str_span(b, user[1]) if user else None
        for span in (user, pwd):
            if span:
                for i in range(span[0], span[1]):
                    b[i] = 0
    elif mid == 20:  # MapInfo: i32 w, i32 h, str name, str displayName, u32 seed, i32 bg, bool, bool
        name = _str_span(b, 9)
        if name:
            dn = _str_span(b, name[1])
            if dn:
                lo, hi = dn
                # Replace the displayName (length prefix included) with a fixed placeholder of the same
                # framing, so the normalized frame keeps every field after it.
                tail = bytes(b[hi:])
                head = bytes(b[:lo - 2])
                placeholder = b"\x00\x02DN"
                tail = bytearray(tail)
                if len(tail) >= 4:
                    tail[0:4] = b"\x00\x00\x00\x00"
                return bytes(head + placeholder + bytes(tail))
    elif mid in (1, 35):  # CreateSuccess: i32 objectId, i32 charId; QuestObjId: i32 objectId
        for i in range(1, 5):
            b[i] = 0
    elif mid == 16:  # Goto: i32 objectId, f32 x, f32 y. Object ids follow spawn timing; positions follow random spawn picks.
        for i in range(1, 13):
            b[i] = 0
    elif mid == 14:  # ShowEffect: u8 effect, i32 target, u32 color, pos1 (8 bytes), optional pos2 (8 bytes)
        for i in range(10, min(len(b), 26)):
            b[i] = 0
    elif mid == 6:  # Text: the server's /pos and /server replies quote the random spawn position
        import re
        return re.sub(rb"up for \d+h:\d+m:\d+s", b"up for UPTIME",
                      re.sub(rb"X:-?\d+, Y:-?\d+|Current Position: -?\d+, -?\d+", b"POS", bytes(b)))
    elif mid == 62:  # Pong (inbound): i32 serial, i32 client time (the client's clock)
        for i in range(5, 9):
            b[i] = 0
    return bytes(b)


def text_fields(frame):
    """(name, numStars, bubbleTime, recipient, text) of a Text frame (id 6), or None when it does not parse.
    Layout (Networking/GameServer.Handlers.cs Text): u8 id, str name, i32 objectId, i32 numStars, u8 bubble, str recipient, str text."""
    if not frame or frame[0] != 6:
        return None
    name = _str_span(frame, 1)
    if not name:
        return None
    pos = name[1]
    if pos + 9 > len(frame):
        return None
    stars = struct.unpack_from(">i", frame, pos + 4)[0]
    bubble = frame[pos + 8]
    rec = _str_span(frame, pos + 9)
    if not rec:
        return None
    text = _str_span(frame, rec[1])
    if not text or text[1] != len(frame):
        return None
    return (frame[name[0]:name[1]].decode("utf-8", "replace"), stars, bubble,
            frame[rec[0]:rec[1]].decode("utf-8", "replace"), frame[text[0]:text[1]].decode("utf-8", "replace"))


def is_ambient_taunt(t):
    """A random NPC taunt (behaviors/Taunt.cs: "#<name>", numStars -1, bubble 3, no recipient). Its chance is
    per cooldown check, so the count varies run to run. The text is still checked by the content of the other frames."""
    return t is not None and t[0].startswith("#") and t[1] == -1 and t[2] == 3 and t[3] == ""


def excluded(frame):
    """Frames that never enter the sequence compare: tick-driven, keepalive, Oryx's timer chatter, random ambient taunts."""
    if frame[0] in TICK_DRIVEN or frame[0] in KEEPALIVE:
        return True
    # Text from "Oryx the Mad God": Oryx's periodic broadcasts run on a wall-clock timer (not a packet reply).
    if frame[0] == 6 and frame[1:3] == b"\x00\x10" and frame[3:19] == b"Oryx the Mad God":
        return True
    return is_ambient_taunt(text_fields(frame))


def apply_masks(frame, masks):
    if not frame:
        return frame
    b = bytearray(frame)
    for (mid, off, ln) in masks:
        if b[0] != mid:
            continue
        for i in range(off, min(off + ln, len(b))):
            b[i] = 0
    return bytes(b)


def first_diff(a, b):
    n = min(len(a), len(b))
    for i in range(n):
        if a[i] != b[i]:
            return i
    return None if len(a) == len(b) else n


POS_RE = None


def jsonl_mode(a_path, b_path):
    """chat_check replies: equal line by line once the X:n, Y:n position text (random spawn) is masked."""
    import re
    global POS_RE
    POS_RE = re.compile(r"X:-?\d+, Y:-?\d+|Current Position: -?\d+, -?\d+|up for \d+h:\d+m:\d+s")
    a = open(a_path, encoding="utf-8").read().splitlines()
    b = open(b_path, encoding="utf-8").read().splitlines()
    taunts = [0, 0]

    def strip(line, k):
        """Drop random ambient taunt events from one reply line (see is_ambient_taunt); count them in taunts[k]."""
        rec = json.loads(line)
        kept = []
        for ev in rec.get("events", []):
            t = (ev.get("name"), ev.get("stars"), ev.get("bubble"), ev.get("to"), ev.get("text"))
            if ev.get("t") == "Text" and is_ambient_taunt((t[0], t[1], t[2], t[3], t[4])):
                taunts[k] += 1
                continue
            kept.append(ev)
        rec["events"] = kept
        return json.dumps(rec, sort_keys=True)

    a = [strip(x, 0) for x in a]
    b = [strip(x, 1) for x in b]
    bad = [i for i, (x, y) in enumerate(zip(a, b)) if POS_RE.sub("POS", x) != POS_RE.sub("POS", y)]
    if len(a) != len(b):
        bad.append(-1)
    print(f"pktdiff jsonl: base={a_path} new={b_path} lines={len(a)}/{len(b)} differing={len(bad)}")
    print(f"  info: ambient taunt events (not compared): base={taunts[0]} new={taunts[1]}")
    for i in bad[:5]:
        print(f"  line {i}: {a[i] if i >= 0 else 'count'}\n     vs {b[i] if i >= 0 else 'count'}")
    print(f"  RESULT {'CLEAN' if not bad else 'DIFFER'}")
    return 0 if not bad else 1


def main():
    if len(sys.argv) == 4 and sys.argv[1] == "--jsonl":
        return jsonl_mode(sys.argv[2], sys.argv[3])
    ap = argparse.ArgumentParser()
    ap.add_argument("base")
    ap.add_argument("new")
    ap.add_argument("--mask", action="append", default=[], help="ID:OFF:LEN, OFF from the id byte")
    ap.add_argument("--show", type=int, default=8, help="max diffs to print")
    ap.add_argument("--json", default=None)
    ap.add_argument("--include-tick", action="store_true",
                    help="gate on tick-driven frames too (only meaningful once their content is deterministic)")
    ap.add_argument("--no-normalize", action="store_true",
                    help="skip the built-in field masks in normalize() (raw bytes; for finding the noise floor)")
    args = ap.parse_args()
    global NORMALIZE
    NORMALIZE = not args.no_normalize
    masks = []
    for m in args.mask:
        try:
            mid, off, ln = (int(x) for x in m.split(":"))
        except ValueError:
            print(f"pktdiff: bad --mask {m!r}", file=sys.stderr)
            return 2
        masks.append((mid, off, ln))
    try:
        A = parse(args.base)
        B = parse(args.new)
    except (OSError, ValueError) as e:
        print(f"pktdiff: {e}", file=sys.stderr)
        return 2

    diffs = []
    info = []
    tick_noise = []
    per_id = collections.Counter()
    keys = sorted(set(A) | set(B))
    for key in keys:
        a = A.get(key)
        b = B.get(key)
        conn, d = key
        if a is None or b is None:
            diffs.append({"conn": conn, "dir": d, "kind": "stream missing", "base": a is not None, "new": b is not None})
            continue
        # Exact sequence for everything that is not tick-driven: walk both streams, skipping tick frames.
        fa = [f for f in a if not excluded(f)]
        fb = [f for f in b if not excluded(f)]
        ka = sorted(f for f in a if f[0] in KEEPALIVE)
        kb = sorted(f for f in b if f[0] in KEEPALIVE)
        if ka != kb:
            info.append({"conn": conn, "dir": d, "kind": "keepalive multiset", "base": len(ka), "new": len(kb)})
        if len(fa) != len(fb):
            diffs.append({"conn": conn, "dir": d, "kind": "non-tick frame count", "base": len(fa), "new": len(fb)})
        for i, (x, y) in enumerate(zip(fa, fb)):
            per_id[x[0]] += 1
            mx, my = apply_masks(normalize(x), masks), apply_masks(normalize(y), masks)
            if mx != my:
                diffs.append({"conn": conn, "dir": d, "kind": "frame", "index": i, "id": x[0],
                              "name": ID_NAMES.get(x[0], str(x[0])), "len_base": len(x), "len_new": len(y),
                              "first_diff": first_diff(mx, my)})
        # Tick-driven frames: pairwise over the common prefix, plus a count note.
        ta = [f for f in a if f[0] in TICK_DRIVEN]
        tb = [f for f in b if f[0] in TICK_DRIVEN]
        if len(ta) != len(tb):
            info.append({"conn": conn, "dir": d, "kind": "tick-driven count", "base": len(ta), "new": len(tb)})
        for i in range(min(len(ta), len(tb))):
            x, y = ta[i], tb[i]
            mx, my = apply_masks(normalize(x), masks), apply_masks(normalize(y), masks)
            if mx != my:
                item = {"conn": conn, "dir": d, "kind": "tick frame", "index": i, "id": x[0],
                        "name": ID_NAMES.get(x[0], str(x[0])), "len_base": len(x), "len_new": len(y),
                        "first_diff": first_diff(mx, my)}
                (diffs if args.include_tick else tick_noise).append(item)
            else:
                per_id[x[0]] += 1

    print(f"pktdiff: base={args.base} new={args.new}")
    print(f"  streams: base={len(A)} new={len(B)}; frames compared by id: "
          + ", ".join(f"{ID_NAMES.get(k, k)}={v}" for k, v in sorted(per_id.items())))
    print(f"  masks: {masks if masks else 'none'}")
    for it in info:
        print(f"  info: conn {it['conn']} {it['dir']} {it['kind']} base={it['base']} new={it['new']} (timing, not a diff)")
    for it in diffs[:args.show]:
        print(f"  DIFF: {it}")
    print(f"  tick-driven frames (Update/NewTick) that differ in content: {len(tick_noise)} "
          f"(timing-dependent; not gated unless --include-tick)")
    print(f"  RESULT {'CLEAN' if not diffs else 'DIFFER'}: {len(diffs)} diff(s), {len(info)} info note(s)")
    if args.json:
        with open(args.json, "w") as f:
            json.dump({"diffs": diffs, "info": info, "tick_noise": tick_noise, "per_id": per_id, "masks": masks},
                      f, indent=1, default=str)
    return 0 if not diffs else 1


if __name__ == "__main__":
    sys.exit(main())
