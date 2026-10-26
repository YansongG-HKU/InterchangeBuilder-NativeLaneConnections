import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import { hermite, attachmentGrade } from "../src/core/vertical.js";
import {
  sample,
  station,
  derivative,
  second,
  cross,
  norm,
} from "../src/core/geometry.js";
import { solve, validate } from "../src/core/solver.js";
import { defaultCase, clone } from "../src/core/model.js";
import { legacyProposal } from "../src/core/migration.js";
const near = (a, b, e = 1e-9) => assert.ok(Math.abs(a - b) < e, `${a} != ${b}`);
test("V01: 0→12m / 200m flat endpoints has 6% average but 9% peak", () => {
  const p = hermite(200, { z0: 0, z1: 12, g0: 0, g1: 0 });
  near(p.peakGrade.grade, 0.09);
  near(p.peakGrade.s, 100);
  near(p.at(0).z, 0);
  near(p.at(200).z, 12);
  near(p.at(0).grade, 0);
  near(p.at(200).grade, 0);
});
test("V02: zero/invalid length rejected before any mesh", () => {
  for (const L of [0, -1, NaN, Infinity])
    assert.throws(() => hermite(L, { z0: 0, z1: 1, g0: 0, g1: 0 }));
});
test("V03: equal heights and differing grades detect interior overshoot", () => {
  const p = hermite(200, { z0: 5, z1: 5, g0: 0.1, g1: -0.1 });
  assert.ok(p.maxZ.z > 5);
  near(p.maxZ.s, 100);
  near(p.maxZ.z, 10);
});
test("V04: endpoint constraints and analytic extrema bound dense samples", () => {
  for (let k = 0; k < 12; k++) {
    const L = 50 + k * 15,
      v = { z0: k - 5, z1: 20 - k, g0: (k - 6) / 50, g1: (5 - k) / 50 },
      p = hermite(L, v);
    near(p.at(0).z, v.z0);
    near(p.at(L).z, v.z1);
    near(p.at(0).grade, v.g0);
    near(p.at(L).grade, v.g1);
    for (let i = 0; i <= 1000; i++) {
      const a = p.at((L * i) / 1000);
      assert.ok(a.z >= p.minZ.z - 1e-9 && a.z <= p.maxZ.z + 1e-9);
      assert.ok(Math.abs(a.grade) <= Math.abs(p.peakGrade.grade) + 1e-9);
    }
  }
});
test("V05: grade reverses with travel direction and offset metric is corrected", () => {
  const curve = sample(defaultCase().curves[0]),
    p = hermite(curve.length, { z0: 0, z1: 20, g0: 0.04, g1: 0.04 }),
    a = station(curve, 100),
    forward = attachmentGrade(curve, a, p, 1, 0),
    backward = attachmentGrade(curve, a, p, -1, 0);
  near(forward.grade, -backward.grade);
  const off = attachmentGrade(curve, a, p, 1, 7),
    d = derivative(curve.c, a.t),
    k = cross(d, second(curve.c, a.t)) / norm(d) ** 3;
  near(off.grade, forward.grade / (1 - 7 * k));
  assert.notEqual(off.grade, forward.grade);
});
test("V06: common datum shift preserves grades, XY/OD and height differences", () => {
  const c = defaultCase(),
    a = solve(c);
  c.vertical.profiles.forEach((p) => {
    p.z0 += 100;
    p.z1 += 100;
  });
  const b = solve(c);
  for (let i = 0; i < 3; i++) {
    near(
      a.candidates[i].vertical.ramps[0].profile.peakGrade.grade,
      b.candidates[i].vertical.ramps[0].profile.peakGrade.grade,
    );
    assert.deepEqual(a.candidates[i].routes, b.candidates[i].routes);
    assert.deepEqual(a.candidates[i].od, b.candidates[i].od);
    near(
      a.candidates[i].preview.crossingDiagnostics[0].heightDifference,
      b.candidates[i].preview.crossingDiagnostics[0].heightDifference,
    );
  }
});
test("V07: every ramp matches source elevation and corrected endpoint grade", () => {
  const c = defaultCase();
  c.vertical.profiles[0] = { z0: 0, z1: 9, g0: 0.01, g1: 0.03 };
  const r = solve(c);
  for (const candidate of r.candidates)
    for (const ramp of candidate.vertical.ramps) {
      const p = hermite(ramp.profile.length, {
        z0: ramp.a.z,
        z1: ramp.b.z,
        g0: ramp.a.grade,
        g1: ramp.b.grade,
      });
      near(ramp.points[0][2], ramp.a.z);
      near(ramp.points.at(-1)[2], ramp.b.z);
      near(p.at(0).grade, ramp.a.grade);
      near(p.at(p.length).grade, ramp.b.grade);
    }
});
test("V08: profile changes keep planar geometry and footprint invariant", () => {
  const c = JSON.parse(fs.readFileSync("examples/partial.json")),
    a = solve(c);
  c.vertical.profiles[1].z0 = 35;
  c.vertical.profiles[1].z1 = 35;
  const b = solve(c);
  for (let i = 0; i < 3; i++) {
    assert.deepEqual(a.candidates[i].routes, b.candidates[i].routes);
    assert.deepEqual(a.candidates[i].land, b.candidates[i].land);
    assert.deepEqual(a.candidates[i].od, b.candidates[i].od);
    assert.ok(
      b.candidates[i].diagnostics.some((d) => d.code === "GRADE_LIMIT"),
    );
  }
});
test("V09: invalid vertical fields never reach scene generation", () => {
  for (const key of ["z0", "z1", "g0", "g1"])
    for (const val of [null, NaN, Infinity, "5"]) {
      const c = defaultCase();
      c.vertical.profiles[0][key] = val;
      assert.equal(solve(c).status, "INPUT_INVALID");
    }
  const c = defaultCase();
  c.vertical.terrain = "known";
  assert.ok(validate(c).length);
});
test("V10: legacy migration is an explicit proposal with flat provenance", () => {
  const old = defaultCase();
  delete old.vertical;
  old.schema = 1;
  old.solver = "offline-0.1.0";
  const before = clone(old),
    proposal = legacyProposal(old);
  assert.deepEqual(old, before);
  assert.equal(solve(old).status, "UNSUPPORTED");
  assert.equal(proposal.vertical.source, "legacy-flat-accepted");
  assert.ok(
    proposal.vertical.profiles.every(
      (p) => p.z0 === 0 && p.z1 === 0 && p.g0 === 0 && p.g1 === 0,
    ),
  );
  assert.equal(proposal.schema, 2);
  const bad = JSON.parse(JSON.stringify(old).replace("{", '{"__proto__":{},'));
  assert.equal(legacyProposal(bad), null);
});
test("V11: preview includes complete required paths, no excluded invented connections", () => {
  const c = JSON.parse(fs.readFileSync("examples/elevated-full.json")),
    r = solve(c),
    v = r.candidates.find((v) => v.id === "d1.2");
  assert.equal(v.preview.roads.length, 10);
  assert.equal(Object.keys(v.preview.paths).length, 12);
  const path = v.preview.paths["A+>B+"],
    ramp = v.vertical.ramps.find((r) => r.id === "A+>B+");
  assert.ok(path.length > ramp.points.length);
  assert.deepEqual(
    path.find(
      (p) =>
        Math.hypot(p[0] - ramp.points[0][0], p[1] - ramp.points[0][1]) < 1e-8,
    ),
    ramp.points[0],
  );
  c.required = c.required.filter((od) => od !== "A+>B+");
  const w = solve(c).candidates[2];
  assert.equal(w.preview.paths["A+>B+"], undefined);
  assert.equal(w.preview.roads.length, 9);
});
test("V12: no structural clearance or game PASS from elevation alone", () => {
  const c = defaultCase();
  c.vertical.profiles[0].z0 = -10;
  c.vertical.profiles[0].z1 = -10;
  const r = solve(c);
  for (const v of r.candidates) {
    assert.equal(v.vertical.clearance, "UNKNOWN");
    assert.equal(v.preview.clearance, "UNKNOWN");
    assert.equal(v.preview.layerCount, "UNKNOWN");
    assert.notEqual(v.status, "PASS");
    assert.ok(
      v.preview.roads.flatMap((r) => r.points.flat()).every(Number.isFinite),
    );
  }
});

test("V13: reversed through-profile display uses travel grade sign", () => {
  const c = defaultCase();
  c.vertical.profiles[0] = { z0: 0, z1: 10, g0: 0.01, g1: 0.03 };
  const v = solve(c).candidates[0].preview;
  const p = v.profiles["A->A-"];
  near(p.coefficients[3], 10);
  near(p.coefficients[2] / p.length, -0.03);
});

test("V14: emitted 3D entry/exit segment tangents converge in valid elevated fixture", () => {
  const input = JSON.parse(fs.readFileSync("examples/elevated-full.json"));
  input.vertical.profiles[0] = { z0: 0, z1: 8, g0: 0.02, g1: 0.01 };
  input.vertical.profiles[1] = { z0: 12, z1: 14, g0: -0.01, g1: 0.02 };
  const v = solve(input).candidates[2];
  for (const r of v.vertical.ramps) {
    const path = v.preview.paths[r.id];
    for (const p of [r.points[0], r.points.at(-1)]) {
      const i = path.findIndex(
        (a) => Math.hypot(...a.map((x, k) => x - p[k])) < 1e-8,
      );
      assert.ok(i > 0 && i < path.length - 1);
      const a = path[i].map((x, k) => x - path[i - 1][k]),
        b = path[i + 1].map((x, k) => x - path[i][k]),
        angle = Math.acos(
          Math.min(
            1,
            a.reduce((s, x, k) => s + x * b[k], 0) /
              Math.hypot(...a) /
              Math.hypot(...b),
          ),
        );
      assert.ok(
        angle < 0.01,
        `${r.id}: ${angle} rad exceeds fixture sampling tolerance`,
      );
    }
  }
});
