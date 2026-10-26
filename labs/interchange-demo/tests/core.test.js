import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import { defaultCase, FULL, clone, laneMapping } from "../src/core/model.js";
import { solve, validate, reachable } from "../src/core/solver.js";
import {
  sample,
  station,
  derivative,
  unit,
  norm,
  sub,
  regular,
} from "../src/core/geometry.js";
import { area, union, metrics, ribbon } from "../src/core/area.js";
import { UnavailableGameAdapter } from "../src/adapters/game.js";
import { messages } from "../src/i18n.js";
const fixture = (n) => JSON.parse(fs.readFileSync(`examples/${n}.json`));
const rect = (x, y, w, h) => [
  [
    [x, y],
    [x + w, y],
    [x + w, y + h],
    [x, y + h],
    [x, y],
  ],
];
test("01: input limits and unsupported domain are explicit", () => {
  for (const key of ["anchor", "handle", "areaCap", "heightCap", "layerCap"])
    for (const v of [null, NaN, Infinity, -1, "5"]) {
      const c = defaultCase();
      c[key] = v;
      assert.ok(validate(c).length, key);
    }
  for (const v of [null, [], {}, 42]) assert.ok(validate(v).length);
  const c = defaultCase();
  c.roadType = "rail";
  assert.equal(solve(c).status, "UNSUPPORTED");
});
test("02: stationary and zero-length curves rejected", () => {
  assert.equal(
    regular([
      [0, 0],
      [1, 0],
      [-1, 0],
      [0, 0],
    ]),
    false,
  );
  const c = defaultCase();
  c.curves[0] = Array(4).fill([0, 0]);
  assert.equal(solve(c).status, "UNSUPPORTED");
});
test("03: arc station is metres, local tangent is analytical", () => {
  const c = sample(defaultCase().curves[0]);
  const a = station(c, 100);
  assert.ok(Math.abs(a.s - 100) < 1e-9);
  assert.ok(norm(sub(a.tangent, unit(derivative(c.c, a.t)))) < 1e-12);
  assert.throws(() => station(c, -1));
});
test("04: skew intersection keeps nonorthogonal original curves", () => {
  const c = defaultCase(),
    r = solve(c);
  assert.equal(r.crossings.length, 1);
  assert.ok(r.crossings[0].angle < 0.9);
  assert.deepEqual(r.curves[0].c, c.curves[0]);
});
test("05: multi-crossings require explicit selection; choose second", () => {
  const c = fixture("multiple");
  let r = solve(c);
  assert.equal(r.crossings.length, 3);
  assert.equal(r.diagnostics[0].code, "SELECT_CROSSING");
  c.crossing = 1;
  r = solve(c);
  assert.equal(r.candidates.length, 3);
});
test("06: near tangent is unsupported and disjoint search not a proof", () => {
  assert.equal(solve(fixture("near-tangent")).status, "UNSUPPORTED");
  const c = defaultCase();
  c.curves[1] = c.curves[0].map(([x, y]) => [x, y + 900]);
  assert.equal(solve(c).status, "NO_CANDIDATE_FOUND");
});
test("07: exact six unique lane edges and no reversal/duplication", () => {
  assert.deepEqual(
    laneMapping().map((l) => l.to),
    ["M1", "M2", "M3", "M4", "R1", "R2"],
  );
  for (const side of ["RHT", "LHT"]) {
    const c = defaultCase();
    c.trafficSide = side;
    const r = solve(c).candidates[1];
    assert.equal(r.lanePaths.length, 6);
    assert.equal(new Set(r.lanePaths.map((l) => l.from)).size, 6);
    for (const l of r.lanePaths) {
      assert.ok(norm(sub(l.c[0], l.a.p)) < 1e-9);
      assert.ok(norm(sub(unit(derivative(l.c, 0)), l.a.tangent)) < 1e-9);
      assert.ok(norm(sub(unit(derivative(l.c, 1)), l.b.tangent)) < 1e-9);
    }
  }
});
test("08: full is fixed 12, partial never redefines baseline or adds forbidden reachability", () => {
  const c = fixture("partial"),
    r = solve(c);
  assert.equal(r.mode, "partial");
  assert.equal(r.fullBaseline.length, 12);
  assert.equal(r.excluded.length, 7);
  for (const v of r.candidates) {
    assert.equal(v.od.reachable.length, 5);
    assert.deepEqual(v.od.forbiddenReachable, []);
  }
  assert.equal(
    reachable(
      [
        { from: "a", to: "b" },
        { from: "b", to: "c" },
      ],
      "a",
      "c",
    ),
    true,
  );
  assert.equal(reachable([{ from: "a", to: "b" }], "b", "a"), false);
});
test("09: bridge projection union counts identical footprint once", () => {
  const p = rect(0, 0, 10, 20);
  assert.equal(area(union([p, p])), 200);
});
test("10: set difference added/removed/retained and nonrectangular area", () => {
  const a = [rect(0, 0, 10, 10)],
    b = [rect(5, 0, 10, 10)],
    site = [rect(-5, -5, 30, 30)];
  const m = metrics(a, b, site);
  assert.deepEqual(
    [m.total, m.added, m.removed, m.retained, m.net],
    [100, 50, 50, 50, 0],
  );
  assert.equal(area(union([rect(0, 0, 10, 20), rect(10, 0, 10, 10)])), 300);
});
test("11: holes and overflow not clipped out of total", () => {
  for (const name of ["small-site", "exclusion"]) {
    const r = solve(fixture(name));
    assert.ok(
      r.candidates.every(
        (v) =>
          v.land.overflow > 0 &&
          v.diagnostics.some((d) => d.code === "OVERFLOW"),
      ),
    );
    for (const v of r.candidates)
      assert.ok(
        Math.abs(v.land.total - v.land.inside - v.land.overflow) < 1e-5,
      );
  }
});
test("12: area cap cannot silently delete OD", () => {
  const c = fixture("partial");
  c.areaCap = 1;
  const r = solve(c);
  assert.equal(r.status, "NO_CANDIDATE_FOUND");
  for (const v of r.candidates) {
    assert.ok(v.diagnostics.some((d) => d.code === "AREA_CAP"));
    assert.deepEqual(v.od.required, c.required);
  }
});
test("13: preferences retain hard constraints and candidate geometry", () => {
  const c = fixture("partial"),
    a = solve(c);
  c.preference = "suburban";
  const b = solve(c);
  assert.deepEqual(a.candidates, b.candidates);
  assert.equal(b.recommended, "d1.2");
});
test("14: deterministic reload and immutable input", () => {
  const c = fixture("partial"),
    before = clone(c);
  assert.deepEqual(solve(c), solve(clone(c)));
  assert.deepEqual(c, before);
});
test("15: analytical endpoint tangent continuity for every route", () => {
  const r = solve(defaultCase());
  for (const c of r.candidates)
    for (const p of c.routes) {
      assert.ok(norm(sub(unit(derivative(p.c, 0)), p.a.tangent)) < 1e-9);
      assert.ok(norm(sub(unit(derivative(p.c, 1)), p.b.tangent)) < 1e-9);
    }
});
test("16: refinement bound decreases and measured area converges", () => {
  const c = defaultCase().curves[0],
    a = sample(c, 128),
    b = sample(c, 256);
  assert.equal(a.positionBound / b.positionBound, 4);
  assert.ok(Math.abs(area(ribbon(a, 28)) - area(ribbon(b, 28))) < 5);
});
test("17: rotation preserves metrics within numerical tolerance", () => {
  const c = fixture("partial"),
    r = solve(c);
  const rot = ([x, y]) => [-y, x];
  c.curves = c.curves.map((a) => a.map(rot));
  c.site = c.site.map(rot);
  const s = solve(c);
  for (let i = 0; i < 3; i++)
    assert.ok(
      Math.abs(r.candidates[i].land.total - s.candidates[i].land.total) < 1e-5,
    );
});
test("18: invalid rings, exclusions, selected crossing, schema rejected", () => {
  const c = defaultCase();
  c.site = [
    [0, 0],
    [10, 10],
    [0, 10],
    [10, 0],
    [0, 0],
  ];
  assert.equal(solve(c).status, "INPUT_INVALID");
  c.site = defaultCase().site;
  c.exclusions = [rect(500, 500, 10, 10)[0]];
  assert.equal(solve(c).status, "INPUT_INVALID");
  c.exclusions = [];
  c.crossing = 40;
  assert.equal(solve(c).diagnostics[0].code, "SELECT_CROSSING");
  c.solver = "future";
  assert.equal(solve(c).status, "UNSUPPORTED");
});
test("19: no unconditional PASS with unassessed height/levels and no game apply", async () => {
  const c = fixture("partial");
  c.heightCap = 0;
  c.layerCap = 1;
  const r = solve(c);
  assert.equal(r.status, "UNKNOWN");
  assert.ok(r.unknown.includes("vertical"));
  const adapter = new UnavailableGameAdapter();
  assert.equal(adapter.capabilities().apply, false);
  await assert.rejects(adapter.apply());
});
test("20: all UI message entries have Chinese and English", () => {
  for (const [key, v] of Object.entries(messages)) {
    assert.equal(v.length, 2, key);
    assert.ok(
      v.every((s) => typeof s === "string" && s.length > 0),
      key,
    );
  }
});

test("21: JSON own fields matching prototype names are unsupported", () => {
  for (const key of [
    "constructor",
    "toString",
    "__proto__",
    "hasOwnProperty",
    "unexpected",
  ]) {
    const c = JSON.parse(
      JSON.stringify(defaultCase()).replace("{", `{${JSON.stringify(key)}:{},`),
    );
    assert.ok(Object.hasOwn(c, key));
    const r = solve(c);
    assert.equal(r.status, "UNSUPPORTED");
    assert.ok(
      r.diagnostics.some((d) => d.code === "UNSUPPORTED" && d.path === key),
    );
  }
  assert.deepEqual(validate(defaultCase()), []);
});

test("22: OD and separate split statuses do not hide each other's diagnostics", () => {
  const c = defaultCase();
  c.anchor = 10;
  c.handle = 100;
  c.required = FULL.filter((m) => m.split(">")[0] === m.split(">")[1]);
  const r = solve(c);
  assert.equal(r.status, "UNKNOWN");
  const v = r.candidates.find((c) => c.splitDiagnostics.length);
  assert.equal(v.status, "UNKNOWN");
  assert.equal(v.splitStatus, "CONSTRAINT_VIOLATION");
  assert.ok(v.splitDiagnostics.some((d) => d.code === "LANE_CROSSING"));
});

test("23: out-of-range anchors leave the lane experiment NOT_RUN", () => {
  const c = defaultCase();
  c.anchor = 300;
  const r = solve(c);
  const v = r.candidates.find((v) => v.id === "d1");
  assert.ok(v.diagnostics.some((d) => d.code === "ANCHOR_RANGE"));
  assert.equal(v.splitStatus, "NOT_RUN");
  assert.deepEqual(v.splitDiagnostics, []);
  assert.equal(v.lanePaths, undefined);
});
