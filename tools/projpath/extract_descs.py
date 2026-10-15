#!/usr/bin/env python3
"""Lists every projectile desc in the server GameData with the client's parse rules.

Usage: extract_descs.py <Resources/GameData dir> <out.tsv>

Mirrors realm-cpp src/objects/Catalog.cpp (projectile block) and the server loader
(Common/Resources.cs LoadGameData, Common/Descriptors.cs ObjectDesc/ItemDesc):
- Speed and LifetimeMS are client integer() (truncating), Magnitude/Frequency
  default 3/1, Amplitude defaults 0, and flags are has() presence.
- Object classes: every <Projectile> child; Equipment/Dye: first <Projectile> only;
  Skin: none. Keys use the server's index order (XML document order).
Output: TSV with one row per desc, consumed by golden_harness.cpp.
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET


def parse_type(text):
    return int(text, 16) if text.startswith("0x") else int(text)


def client_number(text, fallback):
    return fallback if text is None or text == "" else float(text)


def main():
    gamedata, out_path = sys.argv[1], sys.argv[2]
    rows = []
    for path in sorted(glob.glob(os.path.join(gamedata, "*.xml")), key=lambda p: os.path.basename(p)):
        for obj in ET.parse(path).getroot().findall("Object"):
            cls = obj.findtext("Class", default="")
            if cls == "Skin":
                continue
            projectiles = obj.findall("Projectile")
            if cls in ("Equipment", "Dye"):
                projectiles = projectiles[:1]
            ctype = parse_type(obj.get("type"))
            for index, p in enumerate(projectiles):
                def text(name):
                    e = p.find(name)
                    return None if e is None else (e.text or "").strip()
                def has(name):
                    return p.find(name) is not None
                speed = int(client_number(text("Speed"), 0))
                lifetime = int(client_number(text("LifetimeMS"), 0))
                magnitude = client_number(text("Magnitude"), 3)
                amplitude = client_number(text("Amplitude"), 0)
                frequency = client_number(text("Frequency"), 1)
                rows.append((ctype, index, obj.get("id"), lifetime, speed, magnitude, amplitude, frequency,
                             int(has("Wavy")), int(has("Parametric")), int(has("Boomerang")),
                             int(has("Accelerate")), int(has("Decelerate"))))
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("type\tindex\tid\tlifetime\tspeed\tmagnitude\tamplitude\tfrequency\twavy\tparametric\tboomerang\taccelerate\tdecelerate\n")
        for r in rows:
            f.write("\t".join(repr(v) if isinstance(v, float) else str(v) for v in r) + "\n")
    print(f"descs {len(rows)} -> {out_path}")


if __name__ == "__main__":
    main()
