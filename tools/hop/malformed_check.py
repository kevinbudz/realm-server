#!/usr/bin/env python3
"""Malformed-frame check for the game socket (W06 gate; W06 ledger row: framing and length checks).

usage: python3 -I tools/hop/malformed_check.py [--http 7877] [--tcp 2060] [--timeout 5] --out result.json

Run it against a server started by server_ctl.sh (fresh DB; one registration per start, so this registers one
account). For each malformed frame, one "bad" socket sends the frame. A second, well-formed client B stays in the
Nexus. The check passes for a case when:
  - the bad socket is closed by the server (EOF) within the timeout, and
  - B still receives NewTick frames after the attack (B was not disconnected).

Cases:
  truncated       header says 100 body bytes, 20 arrive, then the sender half-closes (FIN). Mid-frame close.
  string-past     Hello whose buildVersion length (u16) says 0x7FFF but the payload ends right after it.
  negative-string Hello whose buildVersion length (u16) is 0xFFFF (-1 as a signed short).
  length-lt-5     frame length 3 (must be at least 5: the prefix plus the id byte).
  length-gt-64k   frame length 70000 (above the 64 KiB frame cap).

Credentials are generated per process and never printed. Exit 0 only if every case passes.
"""
import argparse
import json
import os
import random
import socket
import string
import struct
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hop_client as hc  # noqa: E402  (protocol helpers shared with the hop harness)


def rand_name(n=10):
    return "".join(random.choice(string.ascii_lowercase) for _ in range(n))


def wait_eof(sock, timeout):
    """True when the server closed the connection (EOF or reset) within timeout. Drains any bytes sent first."""
    end = time.monotonic() + timeout
    sock.settimeout(0.2)
    while time.monotonic() < end:
        try:
            data = sock.recv(65536)
            if data == b"":
                return True
        except socket.timeout:
            continue
        except (ConnectionResetError, BrokenPipeError):
            return True
        except OSError:
            return True
    return False


def send_raw(host, tcp, payload, half_close=False):
    sock = socket.create_connection((host, tcp), timeout=5)
    sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
    sock.sendall(payload)
    if half_close:
        sock.shutdown(socket.SHUT_WR)
    return sock


def hello_with_buildversion_len(length_u16):
    """A Hello body whose first string prefix is length_u16; the payload ends right after the prefix plus 3 bytes."""
    return struct.pack(">H", length_u16) + b"abc"


def case_payload(name):
    if name == "truncated":
        return struct.pack(">I", 5 + 100) + bytes([hc.PKT_HELLO]) + b"x" * 20, True
    if name == "string-past":
        return struct.pack(">I", 5 + len(hello_with_buildversion_len(0x7FFF))) + bytes([hc.PKT_HELLO]) + hello_with_buildversion_len(0x7FFF), False
    if name == "negative-string":
        return struct.pack(">I", 5 + len(hello_with_buildversion_len(0xFFFF))) + bytes([hc.PKT_HELLO]) + hello_with_buildversion_len(0xFFFF), False
    if name == "length-lt-5":
        return struct.pack(">I", 3) + bytes([hc.PKT_HELLO]), False
    if name == "length-gt-64k":
        return struct.pack(">I", 70000) + bytes([hc.PKT_HELLO]) + b"x" * 16, False
    raise ValueError(name)


CASES = ["truncated", "string-past", "negative-string", "length-lt-5", "length-gt-64k"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--http", type=int, default=7877)
    ap.add_argument("--tcp", type=int, default=2060)
    ap.add_argument("--timeout", type=float, default=5.0)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    user, pw = rand_name(), rand_name()
    hc.register(args.http, user, pw)
    client_b, _char = hc.enter_first_time(args.host, args.tcp, user, pw, args.timeout)
    results = {}
    ok_all = True
    for name in CASES:
        payload, half_close = case_payload(name)
        bad = send_raw(args.host, args.tcp, payload, half_close=half_close)
        closed = wait_eof(bad, args.timeout)
        bad.close()
        # B must still be in the world: it keeps receiving NewTick frames (sent every tick while it is in the Nexus).
        # Clear B's queue first so a frame received before the attack cannot satisfy the check.
        client_b.queue.clear()
        try:
            client_b.wait_for({hc.PKT_NEW_TICK}, args.timeout, f"NewTick after {name}")
            b_alive = True
        except Exception as e:  # noqa: BLE001 - report, do not crash the other cases
            b_alive = False
            results.setdefault("b_error", str(e))
        passed = closed and b_alive
        ok_all = ok_all and passed
        results[name] = {"bad_closed": closed, "second_client_alive": b_alive, "pass": passed}
        print(f"{'PASS' if passed else 'FAIL'} {name}: bad client closed={closed}, second client alive={b_alive}")
    client_b.close()
    with open(args.out, "w", encoding="utf-8") as fh:
        json.dump(results, fh, indent=1)
    print(f"malformed_check: {sum(1 for c in CASES if results[c]['pass'])}/{len(CASES)} cases passed -> {args.out}")
    return 0 if ok_all else 1


if __name__ == "__main__":
    sys.exit(main())
