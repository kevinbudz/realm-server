#!/usr/bin/env python3
"""Headless driver for the Escape -> Reconnect -> Nexus hop on the local test server.

Flow per run (single throwaway account, one Rogue):
  realm (Hello -2, Load) -> settle -> Escape -> Reconnect(-1) -> close old socket
  -> connect -> Hello(-1) -> MapInfo -> Load -> CreateSuccess -> first Update -> first NewTick
  -> close -> re-enter realm with a fresh Hello(-2)/Load (not timed) -> next run.

Wire format (BE): frame = u32 length (includes these 4 bytes) + u8 id + body.
Strings = u16 length + UTF-8. Pings are answered with Pong so the session stays up.
Credentials are generated per process and never printed.
"""
import argparse
import json
import os
import random
import select
import socket
import string
import struct
import sys
import time
import urllib.parse
import urllib.request

PKT_FAILURE = 0
PKT_CREATE_SUCCESS = 1
PKT_CREATE = 2
PKT_UPDATE = 9
PKT_NEW_TICK = 11
PKT_HELLO = 15
PKT_RECONNECT = 19
PKT_MAP_INFO = 20
PKT_LOAD = 21
PKT_ESCAPE = 42
PKT_PING = 61
PKT_PONG = 62
NAMES = {v: k for k, v in globals().items() if k.startswith("PKT_")}

BUILD_VERSION = "1.0.0"  # Settings.xml <BuildVersion>
REALM_ID = -2
NEXUS_ID = -1
ROGUE = 0x0300


def now():
    return time.perf_counter_ns()


def ms(a, b):
    return (b - a) / 1e6


def s16(text):
    raw = text.encode("utf-8")
    return struct.pack(">H", len(raw)) + raw


def frame(pid, body=b""):
    return struct.pack(">I", 5 + len(body)) + bytes([pid]) + body


def hello_body(user, pw, game_id):
    return s16(BUILD_VERSION) + struct.pack(">i", game_id) + s16(user) + s16(pw) + struct.pack(">i", 0)


class Conn:
    def __init__(self, host, port, timeout=10.0, source=None):
        self.sock = socket.create_connection((host, port), timeout=timeout, source_address=source)
        self.sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self.sock.settimeout(None)
        self.rbuf = bytearray()
        self.queue = []  # (t_ns, pid, body) not yet consumed
        self.eof = False

    def send(self, pid, body=b""):
        self.sock.sendall(frame(pid, body))

    def close(self):
        try:
            self.sock.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass
        self.sock.close()

    def _read_some(self, timeout):
        r, _, _ = select.select([self.sock], [], [], timeout)
        if not r:
            return False
        chunk = self.sock.recv(65536)
        if not chunk:
            self.eof = True
            return True
        self._parse(now(), chunk)
        return True

    def _parse(self, t, chunk):
        self.rbuf += chunk
        while len(self.rbuf) >= 5:
            length = struct.unpack_from(">I", self.rbuf, 0)[0]
            if length < 5 or length > (1 << 20):
                raise ValueError(f"bad frame length {length}")
            if len(self.rbuf) < length:
                break
            pid = self.rbuf[4]
            body = bytes(self.rbuf[5:length])
            del self.rbuf[:length]
            if pid == PKT_PING:
                serial = struct.unpack(">i", body[:4])[0]
                self.send(PKT_PONG, struct.pack(">ii", serial, int(time.time() * 1000) & 0x7FFFFFFF))
                continue
            self.queue.append((t, pid, body))

    def wait_for(self, pids, timeout, what=""):
        deadline = time.monotonic() + timeout
        while True:
            for i, (t, pid, body) in enumerate(self.queue):
                if pid == PKT_FAILURE:
                    code = struct.unpack(">i", body[:4])[0]
                    text = body[4:]
                    raise RuntimeError(f"server Failure {code}: {text!r} while waiting for {what or pids}")
                if pid in pids:
                    del self.queue[i]
                    return t, pid, body
            if self.eof:
                raise ConnectionError(f"peer closed while waiting for {what or pids}")
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                seen = [NAMES.get(p, p) for _, p, _ in self.queue][-10:]
                raise TimeoutError(f"timeout {timeout}s waiting for {what or pids}; recent queued={seen}")
            self._read_some(min(remaining, 0.05))

    def drain_for(self, seconds):
        end = time.monotonic() + seconds
        while time.monotonic() < end:
            self._read_some(max(0.0, min(0.05, end - time.monotonic())))
        self.queue.clear()


def register(http, user, pw):
    # AppServer reads form parameters from the POST body (AppServer.cs:50-52), not the query string.
    body = urllib.parse.urlencode({"newUsername": user, "newPassword": pw}).encode("ascii")
    req = urllib.request.Request(f"http://127.0.0.1:{http}/account/register", data=body,
                                 headers={"Content-Type": "application/x-www-form-urlencoded"})
    with urllib.request.urlopen(req, timeout=35) as resp:
        return resp.read().decode("utf-8", "replace")


def enter_first_time(host, tcp, user, pw, timeout):
    c = Conn(host, tcp)
    c.send(PKT_HELLO, hello_body(user, pw, REALM_ID))
    c.wait_for({PKT_MAP_INFO}, timeout, "MapInfo (initial)")
    c.send(PKT_CREATE, struct.pack(">hh", ROGUE, 0))
    _, _, body = c.wait_for({PKT_CREATE_SUCCESS}, timeout, "CreateSuccess (initial)")
    char_id = struct.unpack(">ii", body[:8])[1]
    c.drain_for(0.5)
    return c, char_id


def reenter_realm(host, tcp, user, pw, char_id, timeout):
    c = Conn(host, tcp)
    c.send(PKT_HELLO, hello_body(user, pw, REALM_ID))
    c.wait_for({PKT_MAP_INFO}, timeout, "MapInfo (realm re-entry)")
    c.send(PKT_LOAD, struct.pack(">i", char_id))
    c.wait_for({PKT_CREATE_SUCCESS}, timeout, "CreateSuccess (realm re-entry)")
    c.drain_for(0.5)
    return c


def one_hop(host, tcp, user, pw, char_id, conn, settle, timeout, connect_delay=0.0):
    conn.drain_for(settle)
    t_esc = now()
    conn.send(PKT_ESCAPE)
    t_rc, _, rc_body = conn.wait_for({PKT_RECONNECT}, timeout, "Reconnect")
    game_id = struct.unpack(">i", rc_body[:4])[0]
    conn.close()  # the C++ client closes the old transport before connecting the new one
    t_closed = now()
    if connect_delay > 0:
        time.sleep(connect_delay)  # experiment: let the writer's disconnect save commit before Hello
    new = Conn(host, tcp)
    t_connected = now()
    t_hello = now()
    new.send(PKT_HELLO, hello_body(user, pw, game_id))
    t_mi, _, _ = new.wait_for({PKT_MAP_INFO}, timeout, "MapInfo")
    t_load = now()
    new.send(PKT_LOAD, struct.pack(">i", char_id))
    t_cs, _, _ = new.wait_for({PKT_CREATE_SUCCESS}, timeout, "CreateSuccess")
    t_up, _, _ = new.wait_for({PKT_UPDATE}, timeout, "first Update")
    t_nt, _, _ = new.wait_for({PKT_NEW_TICK}, timeout, "first NewTick")
    rec = {
        "game_id_from_reconnect": game_id,
        "escape_to_reconnect_rx_ms": round(ms(t_esc, t_rc), 3),
        "old_socket_close_ms": round(ms(t_rc, t_closed), 3),
        "connect_ms": round(ms(t_closed, t_connected), 3),
        "hello_to_mapinfo_ms": round(ms(t_hello, t_mi), 3),
        "mapinfo_to_load_ms": round(ms(t_mi, t_load), 3),
        "load_to_createsuccess_ms": round(ms(t_load, t_cs), 3),
        "createsuccess_to_first_update_ms": round(ms(t_cs, t_up), 3),
        "createsuccess_to_first_newtick_ms": round(ms(t_cs, t_nt), 3),
        "escape_to_first_update_ms": round(ms(t_esc, t_up), 3),
        "escape_to_first_newtick_ms": round(ms(t_esc, t_nt), 3),
    }
    return new, rec


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--http", type=int, default=7877)
    ap.add_argument("--tcp", type=int, default=2060)
    ap.add_argument("--runs", type=int, default=12)
    ap.add_argument("--settle", type=float, default=1.0)
    ap.add_argument("--timeout", type=float, default=10.0)
    ap.add_argument("--connect-delay", type=float, default=0.0, help="seconds between closing the old socket and connecting the new one")
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    rnd = random.SystemRandom()
    user = "hop" + "".join(rnd.choice(string.ascii_lowercase + string.digits) for _ in range(8))
    pw = "".join(rnd.choice(string.ascii_letters + string.digits) for _ in range(16))
    reg = register(args.http, user, pw)
    print(f"register: {reg[:80]}", flush=True)
    if "Success" not in reg:
        print("registration failed; not continuing", file=sys.stderr)
        return 1

    conn, char_id = enter_first_time(args.host, args.tcp, user, pw, args.timeout)
    print(f"in realm, charId={char_id}", flush=True)

    results = []
    with open(args.out, "w") as fh:
        for i in range(args.runs):
            try:
                conn, rec = one_hop(args.host, args.tcp, user, pw, char_id, conn, args.settle, args.timeout, args.connect_delay)
            except Exception as exc:  # record and stop: a broken hop invalidates later runs
                print(f"run {i + 1} FAILED: {exc}", flush=True)
                fh.write(json.dumps({"run": i + 1, "error": str(exc)}) + "\n")
                break
            rec["run"] = i + 1
            rec["connect_delay_s"] = args.connect_delay
            results.append(rec)
            fh.write(json.dumps(rec) + "\n")
            fh.flush()
            print(f"run {i + 1}: escape->firstUpdate={rec['escape_to_first_update_ms']:.3f} ms "
                  f"escape->firstNewTick={rec['escape_to_first_newtick_ms']:.3f} ms "
                  f"hello->mapinfo={rec['hello_to_mapinfo_ms']:.3f} ms", flush=True)
            # Return to the realm for the next hop (not timed).
            conn.close()
            conn = reenter_realm(args.host, args.tcp, user, pw, char_id, args.timeout)
        conn.close()
    print(f"done: {len(results)} runs written to {args.out}", flush=True)
    return 0 if results else 1


if __name__ == "__main__":
    sys.exit(main())
