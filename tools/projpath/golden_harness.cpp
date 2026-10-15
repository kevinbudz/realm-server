// Golden positions from realm-cpp's own projectile_position (compiled verbatim
// from src/objects/ProjectileProperties.cpp), for --projpath-verify <golden.bin>.
//
// Build (scratch, not in realm-cpp):
//   g++ -std=c++20 -O2 -ffp-contract=off -I<realm-cpp>/include golden_harness.cpp
//     <realm-cpp>/src/objects/ProjectileProperties.cpp -o golden_harness
// Run:
//   golden_harness descs.tsv golden.bin   (descs.tsv from extract_descs.py)
//
// Per desc: bullet ids {0,1,2,3,-1,-2,-3,-4} x angles {0, 1.234, -2.7} at start
// (50.5, 60.25), t = min(10*i, lifetime) for i in [0, ceil(lifetime/10)].
// Record layout is documented in ProjectilePathVerify.cs.
#include "realm/objects/ProjectileProperties.hpp"

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <iterator>
#include <cstdio>
#include <cstring>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>

namespace {

struct Desc {
  unsigned type;
  int index;
  realm::ProjectileProperties p;
};

template <typename T>
void put(std::ofstream& out, T value) {
  out.write(reinterpret_cast<const char*>(&value), sizeof value);
}

int check_anchors() {
  using realm::ProjectileProperties;
  using realm::projectile_position;
  ProjectileProperties p;
  p.speed = 100;
  p.lifetime_ms = 1000;
  int failures = 0;
  auto expect = [&](realm::Vec2 got, double x, double y, const char* what) {
    bool ok = std::fabs(got.x - x) <= 1e-9 && std::fabs(got.y - y) <= 1e-9;
    std::printf("anchor %-52s (%.17g, %.17g) %s\n", what, got.x, got.y, ok ? "ok" : "MISMATCH");
    failures += ok ? 0 : 1;
  };
  expect(projectile_position(p, {5, 7}, 0, 0, 250), 7.5, 7, "straight distance");
  p.accelerate = true;
  expect(projectile_position(p, {}, 0, 0, 500), 2.5, 0, "acceleration convention");
  p.accelerate = false;
  p.decelerate = true;
  expect(projectile_position(p, {}, 0, 0, 500), 7.5, 0, "deceleration convention");
  p.decelerate = false;
  p.boomerang = true;
  expect(projectile_position(p, {}, 0, 0, 500), 5, 0, "boomerang apex");
  expect(projectile_position(p, {}, 0, 0, 750), 2.5, 0, "boomerang return");
  expect(projectile_position(p, {}, 0, 0, 1000), 0, 0, "boomerang origin");
  p.accelerate = true;
  expect(projectile_position(p, {}, 0, 0, 750), 4.375, 0, "acceleration/boomerang base-speed halfway");
  p.accelerate = false;
  p.boomerang = false;
  p.amplitude = 2;
  p.frequency = 1;
  expect(projectile_position(p, {}, 0, 0, 250), 2.5, 2, "sine even phase");
  expect(projectile_position(p, {}, 0, 1, 250), 2.5, -2, "sine odd phase");
  p.amplitude = 0;
  p.parametric = true;
  p.magnitude = 3;
  expect(projectile_position(p, {}, 0, 0, 250), -3, 0, "parametric even phase");
  expect(projectile_position(p, {}, 0, 1, 250), 3, 0, "parametric odd phase");
  expect(projectile_position(p, {}, 0, 0, 125), -2.1213203435596424, 3, "parametric second harmonic (id 0)");
  expect(projectile_position(p, {}, 0, 2, 125), -2.1213203435596424, -3, "parametric quadrant phase (id 2)");
  expect(projectile_position(p, {}, 0, -1, 125), 2.1213203435596424, 3, "negative odd id, AS3 truthy sign (id -1)");
  expect(projectile_position(p, {}, 0, -2, 125), -2.1213203435596424, 3, "negative even id (id -2)");
  p.parametric = false;
  p.wavy = true;
  expect(projectile_position(p, {}, 0, 0, 250), 2.496988640512931, -0.12266918581854504, "wavy numeric reference");
  std::printf("anchors: %s\n", failures ? "FAIL" : "all match CoreTests.cpp");
  return failures;
}

}  // namespace

int main(int argc, char** argv) {
  if (argc != 3) {
    std::cerr << "usage: golden_harness descs.tsv golden.bin\n";
    return 2;
  }
  if (check_anchors() != 0) {
    return 1;
  }

  std::ifstream in(argv[1]);
  if (!in) {
    std::cerr << "cannot read " << argv[1] << "\n";
    return 2;
  }
  std::string line;
  std::getline(in, line);  // header
  std::vector<Desc> descs;
  while (std::getline(in, line)) {
    std::istringstream row(line);
    std::string id;
    Desc d{};
    double lifetime = 0;
    double speed = 0;
    int wavy = 0, parametric = 0, boomerang = 0, accelerate = 0, decelerate = 0;
    row >> d.type >> d.index;
    row >> std::ws;
    std::getline(row, id, '\t');  // object id (may contain spaces)
    row >> lifetime >> speed >> d.p.magnitude >> d.p.amplitude >> d.p.frequency >> wavy >> parametric >>
        boomerang >> accelerate >> decelerate;
    d.p.lifetime_ms = lifetime;
    d.p.speed = speed;
    d.p.wavy = wavy != 0;
    d.p.parametric = parametric != 0;
    d.p.boomerang = boomerang != 0;
    d.p.accelerate = accelerate != 0;
    d.p.decelerate = decelerate != 0;
    descs.push_back(d);
  }

  const std::int32_t ids[] = {0, 1, 2, 3, -1, -2, -3, -4};
  const double angles[] = {0.0, 1.234, -2.7};
  std::ofstream out(argv[2], std::ios::binary);
  out.write("PPG1", 4);
  put<std::int32_t>(out, static_cast<std::int32_t>(descs.size() * std::size(ids) * std::size(angles)));
  long long samples = 0;
  for (const Desc& d : descs) {
    const auto lifetime = static_cast<std::int32_t>(d.p.lifetime_ms);
    const std::int32_t count = (lifetime + 9) / 10 + 1;
    for (std::int32_t id : ids) {
      for (double angle : angles) {
        put<std::uint16_t>(out, static_cast<std::uint16_t>(d.type));
        put<std::uint8_t>(out, static_cast<std::uint8_t>(d.index));
        put<std::int32_t>(out, id);
        put<double>(out, angle);
        put<double>(out, 50.5);
        put<double>(out, 60.25);
        put<std::int32_t>(out, lifetime);
        put<std::int32_t>(out, count);
        for (std::int32_t i = 0; i < count; ++i) {
          const std::int32_t t = std::min<std::int32_t>(10 * i, lifetime);
          realm::Vec2 v = realm::projectile_position(d.p, {50.5, 60.25}, angle, id, t);
          put<double>(out, v.x);
          put<double>(out, v.y);
          ++samples;
        }
      }
    }
  }
  std::printf("wrote %zu descs, %lld samples -> %s\n", descs.size(), samples, argv[2]);
  return out ? 0 : 1;
}
