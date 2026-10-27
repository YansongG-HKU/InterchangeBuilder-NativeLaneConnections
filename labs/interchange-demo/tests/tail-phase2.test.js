import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import { generate, validateInput } from "../src/experiments/tail/generator.js";
import {
  inspect,
  permission,
  crossingClearance,
} from "../src/experiments/tail/checker.js";
import { evaluateCase } from "../src/experiments/tail/run.js";
const fixture = (n) =>
    JSON.parse(fs.readFileSync(`tests/tail-fixtures/${n}.json`)),
  copy = structuredClone;
test("automatic crossing: independent 440m/10m/7.5% oracle; generation never consumes reference", () => {
  const f = fixture("automatic-overpass"),
    r = evaluateCase(f.input),
    c = r.candidates[0];
  assert.equal(r.status, "BOUNDED_CANDIDATE");
  assert.equal(c.output.id, "bridge-0");
  assert.equal(c.facts.metrics.maxGrade, 0.075);
  assert.equal(c.facts.collisions[0].clearance.minimum, 8.5);
  assert.deepEqual(c.output.segments, f.reference.segments); // separately frozen analytic oracle
  assert.equal(c.conversionIdentical, true);
  assert.equal(c.facts.metrics.straightTail, 440);
  const bad = copy(c.output);
  bad.segments[1].z.z0 = bad.segments[1].z.z1 = 5;
  assert.equal(
    permission(inspect(f.input, bad), "no-collision").allowedForOfflinePreview,
    false,
  );
});
test("same physical profile split at half-station preserves grade and full-overlap clearance", () => {
  const f = fixture("automatic-overpass"),
    a = generate(f.input).candidates[0],
    b = copy(a);
  b.segments.splice(
    0,
    1,
    {
      kind: "line",
      controls: [
        [0, 0],
        [100, 0],
      ],
      z: { z0: 0, z1: 5, g0: 0, g1: 0.075 },
    },
    {
      kind: "line",
      controls: [
        [100, 0],
        [200, 0],
      ],
      z: { z0: 5, z1: 10, g0: 0.075, g1: 0 },
    },
  );
  const x = inspect(f.input, a),
    y = inspect(f.input, b);
  assert.equal(y.status, "VERIFIED_BOUNDED");
  assert.equal(x.metrics.maxGrade, y.metrics.maxGrade);
  assert.equal(
    x.collisions[0].clearance.minimum,
    y.collisions[0].clearance.minimum,
  );
});
test("multiple and long crossing regions share plateau; grade budget is not a global infeasibility proof", () => {
  const f = fixture("automatic-overpass");
  f.input.obstacles.push({
    ...copy(f.input.obstacles[0]),
    id: "second",
    a: [230, -100],
    b: [230, 100],
  });
  assert.equal(evaluateCase(f.input).status, "BOUNDED_CANDIDATE");
  f.input.obstacles[0].width = 50;
  assert.equal(evaluateCase(f.input).status, "BOUNDED_CANDIDATE");
  f.input.maxGrade = 0.03;
  assert.equal(evaluateCase(f.input).status, "SEARCH_BUDGET_EXHAUSTED");
  f.input.end.position[0] = 100;
  f.input.end.z = 10;
  f.input.maxGrade = 0.05;
  f.input.obstacles = [
    { ...f.input.obstacles[0], a: [50, -100], b: [50, 100], width: 10 },
  ];
  assert.equal(evaluateCase(f.input).status, "PROVEN_CONSTRAINT_CONFLICT");
});
test("clearance boundaries, altered thickness and unresolved long oblique crossings never relax", () => {
  const f = fixture("automatic-overpass"),
    o = generate(f.input).candidates[0];
  f.input.clearance = 8.5;
  const boundary = inspect(f.input, o);
  assert.equal(boundary.status, "UNKNOWN");
  assert.equal(
    permission(boundary, "no-collision").allowedForOfflinePreview,
    false,
  );
  o.thickness += 0.1;
  assert.equal(
    permission(inspect(f.input, o), "no-collision").allowedForOfflinePreview,
    false,
  );
  f.input.obstacles[0].b[0] += 1000;
  assert.equal(generate(f.input).status, "SEARCH_BUDGET_EXHAUSTED");
  assert.notEqual(
    crossingClearance(o, f.input.obstacles[0], 5).status,
    "VERIFIED_MODEL",
  );
});
test("actual curved context agrees with fixed ports; all nine directed side pairs preserve identities", () => {
  const f = fixture("curved-context-left");
  assert.equal(inspect(f.input, f.reference).status, "VERIFIED_BOUNDED");
  for (const a of [-6, 0, 6])
    for (const b of [-6, 0, 6]) {
      const i = copy(f.input);
      for (const [p, v] of [
        [i.start, a],
        [i.end, b],
      ]) {
        p.offset = v;
        p.side = v < 0 ? "right" : v > 0 ? "left" : "middle";
      }
      const r = evaluateCase(i);
      assert.ok(r.candidates.length);
      for (const c of r.candidates) {
        if (a !== 0 || b !== 0)
          assert.equal(c.permission.allowedForOfflinePreview, false);
        assert.equal(c.output.startId, i.start.id);
        assert.equal(c.output.endId, i.end.id);
        assert.ok(c.facts.metrics.positionErrors.every((x) => x < 1e-7));
      }
    }
  const narrow = copy(f.input);
  narrow.obstacles[0].width = 4;
  assert.equal(
    permission(inspect(narrow, f.reference), "no-collision")
      .allowedForOfflinePreview,
    false,
  );
  assert.ok(
    inspect(narrow, f.reference).issues.some(
      (x) => x.code === "PORT_CONTEXT_WIDTH_UNSUPPORTED",
    ),
  );
  const bad = copy(f.input);
  bad.start.curvature += 0.001;
  assert.ok(
    inspect(bad, f.reference).issues.some(
      (i) => i.code === "PORT_CONTEXT_MISMATCH",
    ),
  );
});
test("curved-neighbor collision is not reduced to its endpoint chord or exempted by other road id", () => {
  const f = fixture("middle-tail");
  f.input.obstacles = [
    {
      id: "bow",
      kind: "road",
      a: [120, -20],
      b: [160, -20],
      controls: [
        [120, -20],
        [140, 40],
        [160, -20],
      ],
      width: 8,
      z: 0,
    },
  ];
  f.input.joins = [
    { route: "ramp", road: "other", box: [-100, -100, 500, 500] },
  ];
  assert.equal(
    permission(inspect(f.input, f.reference), "no-collision")
      .allowedForOfflinePreview,
    false,
  );
  f.input.obstacles[0].controls = [
    [120, -20],
    [120, -20],
    [120, -20],
  ];
  assert.ok(validateInput(f.input));
});
test("variable widths explicitly unsupported on input and output; narrow corridors fail closed", () => {
  const f = fixture("middle-tail");
  f.input.widthProfile = [8, 20, 8];
  assert.equal(generate(f.input).status, "UNSUPPORTED");
  delete f.input.widthProfile;
  f.reference.segments[0].width = 20;
  assert.equal(
    permission(inspect(f.input, f.reference), "no-collision")
      .allowedForOfflinePreview,
    false,
  );
  delete f.reference.segments[0].width;
  f.input.obstacles[0].a[1] = f.input.obstacles[0].b[1] = 7;
  assert.equal(
    permission(inspect(f.input, f.reference), "normal")
      .allowedForOfflinePreview,
    false,
  );
});
test("near-collinear, near-reverse, short acute and curvature-sign candidates retain finite fail-closed diagnostics", () => {
  const base = fixture("middle-tail").input;
  for (const [d, p, k] of [
    [[1, 1e-8], [160, 0], 0],
    [[-1, 1e-8], [30, 0], 0],
    [[0, 1], [4, 4], 0.5],
    [[0, 1], [120, 120], -0.002],
    [[0, 1], [120, 120], 0.002],
  ]) {
    const i = copy(base);
    i.obstacles = [];
    i.end.direction = d.map((v) => v / Math.hypot(...d));
    i.end.position = p;
    i.start.curvature = k;
    const r = evaluateCase(i);
    assert.notEqual(r.status, "PROVEN_CONSTRAINT_CONFLICT");
    for (const c of r.candidates) {
      assert.ok(c.facts.points.every((p) => p.every(Number.isFinite)));
      if (c.facts.status !== "VERIFIED_BOUNDED")
        assert.equal(c.permission.allowedForOfflinePreview, false);
    }
  }
});
test("context and route rotate/translate/mirror together without changing bounded decisions", () => {
  const f = fixture("curved-context-left");
  for (const mirror of [false, true]) {
    const i = copy(f.input),
      o = copy(f.reference),
      tx = ([x, y]) => [-y + 400, (mirror ? -x : x) + 200],
      vec = ([x, y]) => [-y, mirror ? -x : x];
    for (const p of [i.start, i.end]) {
      p.position = tx(p.position);
      p.direction = vec(p.direction);
      if (mirror) {
        p.offset = -p.offset;
        p.curvature = -p.curvature;
        p.side =
          p.side === "left" ? "right" : p.side === "right" ? "left" : p.side;
      }
    }
    for (const road of i.obstacles) {
      road.a = tx(road.a);
      road.b = tx(road.b);
      if (road.controls) road.controls = road.controls.map(tx);
    }
    for (const j of i.joins) {
      const a = tx(j.box.slice(0, 2)),
        b = tx(j.box.slice(2));
      j.box = [
        Math.min(a[0], b[0]),
        Math.min(a[1], b[1]),
        Math.max(a[0], b[0]),
        Math.max(a[1], b[1]),
      ];
    }
    for (const s of o.segments) s.controls = s.controls.map(tx);
    assert.equal(inspect(i, o).status, "VERIFIED_BOUNDED");
  }
});
