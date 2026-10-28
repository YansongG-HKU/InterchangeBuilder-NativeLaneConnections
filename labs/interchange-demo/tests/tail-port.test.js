import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import { generate } from "../src/experiments/tail/generator.js";
import {
  inspect,
  crossingClearance,
  permission,
} from "../src/experiments/tail/checker.js";
import { evaluateCase } from "../src/experiments/tail/run.js";
const fixture = (n) =>
  JSON.parse(fs.readFileSync(`tests/tail-fixtures/${n}.json`));
function rotate(input, a) {
  const i = structuredClone(input),
    c = Math.cos(a),
    s = Math.sin(a),
    p = ([x, y]) => [1000 + x * c - y * s, -300 + x * s + y * c];
  for (const port of [i.start, i.end]) {
    port.position = p(port.position);
    const [x, y] = port.direction;
    port.direction = [x * c - y * s, x * s + y * c];
  }
  for (const o of i.obstacles) {
    o.a = p(o.a);
    o.b = p(o.b);
    if (o.controls) o.controls = o.controls.map(p);
  }
  return i;
}
test("actual 90degree and general-angle generated overpasses preserve endpoints grade and clearance", () => {
  const base = fixture("automatic-overpass").input;
  for (const angle of [Math.PI / 2, 0.67, -1.27]) {
    const i = rotate(base, angle),
      r = evaluateCase(i);
    assert.equal(r.status, "BOUNDED_CANDIDATE");
    for (const c of r.candidates) {
      assert.equal(c.facts.status, "VERIFIED_BOUNDED");
      assert.ok(c.facts.metrics.positionErrors.every((v) => v < 1e-7));
    }
    assert.ok(Math.abs(r.candidates[0].facts.metrics.maxGrade - 0.075) < 1e-9);
    assert.ok(
      Math.abs(r.candidates[0].facts.collisions[0].clearance.minimum - 8.5) <
        1e-9,
    );
  }
});
test("curved oblique candidate compared with independently frozen polynomial reference; exact finite tail retained", () => {
  const f = fixture("curved-oblique-overpass");
  assert.equal(inspect(f.input, f.reference).status, "VERIFIED_BOUNDED");
  for (const input of [f.input, rotate(f.input, 0.43)]) {
    const r = evaluateCase(input);
    assert.equal(r.status, "BOUNDED_CANDIDATE");
    for (const c of r.candidates) {
      assert.ok(c.output.segments.some((s) => s.kind === "bezier"));
      assert.equal(c.output.segments.at(-1).kind, "line");
      assert.ok(Math.abs(c.facts.metrics.straightTail - 120) < 1e-7);
      assert.ok(c.facts.metrics.maxGrade < input.maxGrade);
      assert.ok(
        c.facts.collisions[0].clearance.minimumLowerBound > input.clearance,
      );
      assert.ok(c.facts.points.some((p) => p[2] > 5));
    }
  }
});
test("oblique envelope includes low slope edges and near-parallel long overlap; neither mode invents clearance", () => {
  const f = fixture("automatic-overpass"),
    o = generate(f.input).candidates[0];
  const road = {
    id: "slant",
    kind: "road",
    a: [50, -20],
    b: [390, 20],
    width: 20,
    z: 0,
    crossing: true,
  };
  const c = crossingClearance(o, road, 8);
  assert.notEqual(c.status, "VERIFIED_MODEL");
  f.input.obstacles = [road];
  const facts = inspect(f.input, o);
  assert.equal(
    permission(facts, "no-collision").allowedForOfflinePreview,
    false,
  );
});
test("thicker deck and low vertical injection cannot pass conservative curved overlap", () => {
  const f = fixture("curved-oblique-overpass"),
    r = evaluateCase(f.input),
    o = structuredClone(r.candidates[0].output);
  o.thickness += 5;
  assert.equal(
    permission(inspect(f.input, o), "no-collision").allowedForOfflinePreview,
    false,
  );
  for (const s of o.segments) {
    s.z = { z0: 0, z1: 0, g0: 0, g1: 0 };
  }
  o.thickness = f.input.deckThickness;
  const facts = inspect(f.input, o);
  assert.equal(
    permission(facts, "no-collision").allowedForOfflinePreview,
    false,
  );
});
test("curved profile insufficient grade remains bounded search failure and right-turn mirror is equivalent", () => {
  const f = fixture("curved-oblique-overpass"),
    i = structuredClone(f.input);
  i.maxGrade = 0.01;
  assert.equal(evaluateCase(i).status, "SEARCH_BUDGET_EXHAUSTED");
  for (const p of [i.start, i.end]) {
    p.position[1] *= -1;
    p.direction[1] *= -1;
    p.curvature *= -1;
    p.offset *= -1;
  }
  for (const o of i.obstacles) {
    o.a[1] *= -1;
    o.b[1] *= -1;
  }
  i.maxGrade = 0.08;
  const r = evaluateCase(i),
    base = evaluateCase(f.input);
  assert.equal(r.status, "BOUNDED_CANDIDATE");
  assert.ok(
    Math.abs(
      r.candidates[0].facts.metrics.maxGrade -
        base.candidates[0].facts.metrics.maxGrade,
    ) < 1e-8,
  );
});
test("nonlinear collinear parameterization preserves physical profile, not Bezier t", () => {
  const f = fixture("automatic-overpass"),
    o = generate(f.input).candidates[0],
    p = structuredClone(o);
  p.segments[0].kind = "bezier";
  p.segments[0].controls = [
    [0, 0],
    [1, 0],
    [200, 0],
  ];
  const a = inspect(f.input, o),
    b = inspect(f.input, p);
  assert.equal(b.status, "VERIFIED_BOUNDED");
  assert.ok(Math.abs(a.metrics.maxGrade - b.metrics.maxGrade) < 1e-9);
  assert.ok(Math.abs(b.collisions[0].clearance.minimum - 8.5) < 1e-7);
});

test("left and mirrored right turn profiles retain fixed G1 ports and finite tail", () => {
  const { input } = fixture("curved-main-left");
  for (const p of [input.start, input.end]) {
    p.position = p.position.map((x) => x * 4);
    p.curvature /= 4;
  }
  input.tail *= 4;
  input.verticalMode = "intermediate";
  input.deckThickness = 1.5;
  input.clearance = 1.4;
  input.obstacles = [
    {
      id: "lower",
      kind: "road",
      a: [240, -100],
      b: [360, 500],
      width: 12,
      z: 0,
      crossing: true,
    },
  ];
  const left = evaluateCase(input);
  assert.equal(left.status, "BOUNDED_CANDIDATE");
  for (const p of [input.start, input.end]) {
    p.position[1] *= -1;
    p.direction[1] *= -1;
    p.curvature *= -1;
  }
  for (const o of input.obstacles) {
    o.a[1] *= -1;
    o.b[1] *= -1;
  }
  const right = evaluateCase(input);
  assert.equal(right.status, "BOUNDED_CANDIDATE");
  for (let n = 0; n < left.candidates.length; n++) {
    const a = left.candidates[n],
      b = right.candidates[n];
    assert.ok(
      Math.abs(a.facts.metrics.maxGrade - b.facts.metrics.maxGrade) < 1e-8,
    );
    assert.ok(Math.abs(b.facts.metrics.straightTail - 120) < 1e-7);
    assert.equal(b.output.startId, "S");
    assert.equal(b.output.endId, "T");
    assert.equal(b.facts.collisions[0].clearance.status, "VERIFIED_MODEL");
  }
});
