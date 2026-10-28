import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import {
  generate,
  convert,
  portPosition,
  validateInput,
} from "../src/experiments/tail/generator.js";
import {
  inspect,
  permission,
  crossingClearance,
} from "../src/experiments/tail/checker.js";
import { evaluateCase } from "../src/experiments/tail/run.js";
const fixture = (name) =>
  JSON.parse(fs.readFileSync(`tests/tail-fixtures/${name}.json`));
const clone = structuredClone;
for (const name of [
  "middle-tail",
  "same-direction-shift",
  "straight-main-left",
  "curved-main-left",
  "equal-height-overpass",
]) {
  test(`independent handwritten oracle: ${name} and injected fault`, () => {
    const f = fixture(name);
    let input = clone(f.input),
      output = clone(f.reference);
    const normal = inspect(input, output);
    assert.equal(normal.status, f.expected);
    assert.ok(normal.metrics.straightTail >= input.tail);
    assert.ok(normal.metrics.maxGrade <= input.maxGrade);
    if (f.fault === "short-tail") output.segments.at(-1).controls[0][0] += 10;
    if (f.fault === "reverse-tail") output.segments.at(-1).controls.reverse();
    if (f.fault === "gap") output.segments.at(-1).controls[0][0] += 2;
    if (f.fault === "g2-conflict") input.continuity = "G2";
    if (f.fault === "thick-deck") output.thickness = 7;
    assert.ok(
      inspect(input, output).issues.some((x) => x.code === f.expectedFault),
    );
  });
}
test("bounded generated candidates survive independent and transport checks", () => {
  for (const name of [
    "middle-tail",
    "same-direction-shift",
    "straight-main-left",
    "curved-main-left",
  ]) {
    const { input } = fixture(name);
    const result = evaluateCase(input);
    assert.equal(result.status, "BOUNDED_CANDIDATE");
    for (const c of result.candidates) {
      assert.equal(c.facts.status, "VERIFIED_BOUNDED");
      assert.ok(c.conversionIdentical);
      assert.deepEqual(c.output, convert(c.output));
      assert.equal(c.facts.metrics.straightTail, input.tail);
    }
  }
});
test("strict tail and nonzero target curvature G2 is a proven local conflict; bridge generation unsupported", () => {
  const f = fixture("curved-main-left");
  f.input.continuity = "G2";
  assert.equal(generate(f.input).status, "PROVEN_CONSTRAINT_CONFLICT");
  assert.equal(
    generate(fixture("equal-height-overpass").input).status,
    "UNSUPPORTED",
  );
  assert.equal(
    generate(fixture("middle-tail").input, 0).status,
    "SEARCH_BUDGET_EXHAUSTED",
  );
});
test("normal/no-collision facts preserved; only synthetic building overlaps can relax", () => {
  const f = fixture("middle-tail");
  f.input.obstacles = [
    {
      id: "building",
      kind: "building",
      a: [140, -20],
      b: [140, 20],
      width: 10,
    },
  ];
  const facts = inspect(f.input, f.reference);
  assert.equal(permission(facts, "normal").allowedForOfflinePreview, false);
  assert.equal(
    permission(facts, "no-collision").allowedForOfflinePreview,
    true,
  );
  f.input.mode = "no-collision";
  assert.deepEqual(inspect(f.input, f.reference), facts);
  f.input.obstacles[0].kind = "road";
  assert.equal(
    permission(inspect(f.input, f.reference), "no-collision")
      .allowedForOfflinePreview,
    false,
  );
  const broken = clone(f.reference);
  broken.segments[1].controls.reverse();
  assert.equal(
    permission(inspect(f.input, broken), "no-collision")
      .allowedForOfflinePreview,
    false,
  );
});
test("named-pair AND bounded-region join exemptions never exempt adjacent road", () => {
  const f = fixture("same-direction-shift");
  f.input.obstacles = [
    { id: "target", kind: "road", a: [190, 30], b: [220, 30], width: 8 },
  ];
  f.input.joins = [{ route: "ramp", road: "target", box: [150, 20, 210, 40] }];
  assert.equal(inspect(f.input, f.reference).status, "VERIFIED_BOUNDED");
  f.input.obstacles.push({ ...f.input.obstacles[0], id: "unrelated" });
  assert.ok(
    inspect(f.input, f.reference).issues.some((i) => i.road === "unrelated"),
  );
  f.input.obstacles.pop();
  f.input.joins[0].box = [195, 20, 210, 40];
  assert.ok(
    inspect(f.input, f.reference).issues.some((i) => i.road === "target"),
  );
});
test("inspector distrusts flags and rejects nonfinite, degeneracy, width, identity and conversion loss", () => {
  const f = fixture("middle-tail");
  let o = clone(f.reference);
  o.valid = true;
  o.segments[0].controls[0][0] = 5;
  assert.equal(inspect(f.input, o).status, "CANDIDATE_FAILED");
  o = clone(f.reference);
  o.segments[0].controls[2][0] = NaN;
  assert.equal(inspect(f.input, o).status, "NUMERICAL_FAILURE");
  o = clone(f.reference);
  o.segments[1].controls[0] = o.segments[1].controls[1];
  assert.ok(inspect(f.input, o).issues.some((i) => i.code === "DEGENERATE"));
  o = clone(f.reference);
  o.width = 1;
  assert.ok(
    inspect(f.input, o).issues.some((i) => i.code === "WIDTH_MISMATCH"),
  );
  o = clone(f.reference);
  o.endId = "wrong";
  assert.ok(
    inspect(f.input, o).issues.some((i) => i.code === "IDENTITY_MISMATCH"),
  );
  o = clone(f.reference);
  o.segments[1].kind = "bezier";
  assert.ok(
    inspect(f.input, o).issues.some((i) => i.code === "TAIL_TOO_SHORT"),
  );
});
test("low subdivision budget reports UNKNOWN, refined length bounds converge", () => {
  const f = fixture("same-direction-shift"),
    coarse = inspect(f.input, f.reference, { tolerance: 0.1, maxDepth: 0 }),
    a = inspect(f.input, f.reference, { tolerance: 0.04 }),
    b = inspect(f.input, f.reference, { tolerance: 0.01 });
  assert.equal(coarse.status, "UNKNOWN");
  assert.ok(b.metrics.lengthBounds[0] >= a.metrics.lengthBounds[0] - 1e-9);
  assert.ok(b.metrics.lengthBounds[1] <= a.metrics.lengthBounds[1] + 1e-9);
});
test("nonadjacent crossing and close full-width limbs detected", () => {
  const f = fixture("same-direction-shift");
  f.reference.segments = [
    [
      [0, 0],
      [100, 100],
    ],
    [
      [100, 100],
      [0, 100],
    ],
    [
      [0, 100],
      [100, 0],
    ],
    [
      [100, 0],
      [200, 30],
    ],
  ].map((controls) => ({
    kind: "line",
    controls,
    z: { z0: 0, z1: 0, g0: 0, g1: 0 },
  }));
  assert.ok(
    inspect(f.input, f.reference).issues.some(
      (i) => i.code === "SELF_INTERSECTION",
    ),
  );
});
test("planar straight tail supports nonzero elevation grade; excessive grade fails", () => {
  const f = fixture("same-direction-shift");
  f.input.end.z = 6;
  f.input.start.grade = 0.02;
  f.input.end.grade = 0.02;
  const c = generate(f.input).candidates[1];
  assert.equal(inspect(f.input, c).status, "VERIFIED_BOUNDED");
  assert.notEqual(c.segments[1].z.z0, c.segments[1].z.z1);
  f.input.end.z = 100;
  assert.ok(
    inspect(f.input, generate(f.input).candidates[1]).issues.some(
      (i) => i.code === "GRADE_LIMIT",
    ),
  );
});
test("full overlap reference catches low edge even if crossing midpoint has clearance", () => {
  const f = fixture("equal-height-overpass");
  assert.equal(
    crossingClearance(f.reference, f.input.obstacles[0], 5.5).minimum,
    8.5,
  );
  f.input.obstacles[0].width = 50;
  const o = f.reference;
  o.segments[0].z.z1 = 6;
  o.segments[1].z = { z0: 6, z1: 10, g0: 0.3, g1: -0.3 };
  o.segments[2].z.z0 = 10;
  assert.equal(
    crossingClearance(o, f.input.obstacles[0], 5.5).status,
    "FAILED",
  );
  delete o.thickness;
  assert.equal(
    crossingClearance(o, f.input.obstacles[0], 5.5).status,
    "UNSUPPORTED",
  );
});
test("rigid rotation/translation and mirror preserve bounded checks and travel-side semantics", () => {
  const f = fixture("same-direction-shift"),
    orig = inspect(f.input, f.reference);
  for (const mirror of [1, -1]) {
    const i = clone(f.input),
      o = clone(f.reference),
      map = (p) => [-mirror * p[1] + 30, p[0] - 20],
      dir = (p) => [-mirror * p[1], p[0]];
    for (const p of [i.start, i.end]) {
      p.position = map(p.position);
      p.direction = dir(p.direction);
      p.curvature *= mirror;
    }
    for (const s of o.segments) s.controls = s.controls.map(map);
    const q = inspect(i, o);
    assert.equal(q.status, orig.status);
    assert.ok(
      Math.abs(q.metrics.lengthBounds[0] - orig.metrics.lengthBounds[0]) < 1e-9,
    );
  }
  const p = clone(f.input.end);
  p.side = "left";
  p.offset = 4;
  assert.deepEqual(portPosition(p), [200, 34]);
  p.direction = [-1, 0];
  assert.deepEqual(portPosition(p), [200, 26]);
});
test("schema invalid side, zero direction, missing values rejected without NaN geometry", () => {
  const { input } = fixture("middle-tail");
  input.end.direction = [0, 0];
  assert.equal(generate(input).status, "INPUT_INVALID");
  input.end.direction = [1, 0];
  input.end.side = "left";
  assert.ok(validateInput(input));
  input.end.offset = 4;
  assert.equal(validateInput(input), null);
  input.tail = null;
  assert.equal(evaluateCase(input).status, "INPUT_INVALID");
});

test("thin clearance margins retain numerical uncertainty instead of false PASS", () => {
  const f = fixture("middle-tail");
  f.input.obstacles = [
    { id: "tangent", kind: "road", a: [135, 8], b: [180, 8], width: 8 },
  ];
  assert.ok(
    inspect(f.input, f.reference).issues.some(
      (i) => i.code === "CORRIDOR_UNCERTAIN",
    ),
  );
});
test("nearby nonadjacent full-width limbs overlap even without centerline crossing", () => {
  const f = fixture("same-direction-shift");
  f.reference.segments = [
    [
      [0, 0],
      [100, 0],
    ],
    [
      [100, 0],
      [100, 6],
    ],
    [
      [100, 6],
      [0, 6],
    ],
    [
      [0, 6],
      [200, 30],
    ],
  ].map((controls) => ({
    kind: "line",
    controls,
    z: { z0: 0, z1: 0, g0: 0, g1: 0 },
  }));
  assert.ok(
    inspect(f.input, f.reference).issues.some(
      (i) => i.code === "NONLOCAL_CORRIDOR_OVERLAP",
    ),
  );
});

test("port curvature and grade use actual constant-offset travel metric; widths are explicit", () => {
  const f = fixture("curved-main-left");
  f.input.start.side = "left";
  f.input.start.offset = 6;
  f.input.start.grade = 0.01;
  const o = generate(f.input).candidates[1];
  assert.ok(Math.abs(o.segments[0].z.g0 - 0.01 / (1 - 6 * 0.002)) < 1e-12);
  assert.equal(inspect(f.input, o).status, "VERIFIED_BOUNDED");
  f.input.start.width = 12;
  assert.equal(generate(f.input).status, "UNSUPPORTED");
});

test("display points actually follow the reference Hermite arch, not endpoint-only chords", () => {
  const f = fixture("equal-height-overpass"),
    r = inspect(f.input, f.reference);
  const p = r.points.find((p) => Math.abs(p[0] - 50) < 1e-8);
  assert.ok(p);
  assert.ok(Math.abs(p[2] - 1.5625) < 1e-9);
  assert.ok(r.points.length > 200);
});

test("numerical/domain failures are not relabelled as a completed search", () => {
  const f = fixture("same-direction-shift");
  f.input.end.position = [10000, 10000];
  f.input.start.curvature = 1;
  assert.equal(evaluateCase(f.input).status, "NUMERICAL_FAILURE");
});

 test("review: overflowing imported profiles and malformed shapes fail closed", () => {
  const f = fixture("middle-tail");
  f.reference.segments[0].z.z1 = 1e308;
  f.reference.segments[1].z.z0 = 1e308;
  const facts = inspect(f.input, f.reference);
  assert.equal(facts.status, "NUMERICAL_FAILURE");
  assert.deepEqual(facts.points, []);
  assert.equal(permission(facts, "no-collision").allowedForOfflinePreview, false);
  f.reference.segments = [null];
  assert.equal(inspect(f.input, f.reference).status, "NUMERICAL_FAILURE");
  f.input.joins = [null];
  assert.equal(evaluateCase(f.input).status, "INPUT_INVALID");
  const valid = fixture("middle-tail");
  for (const z of ["bad", 1e308]) {
    const bad = clone(valid.input); bad.obstacles[0].z = z;
    assert.equal(evaluateCase(bad).status, "INPUT_INVALID");
  }
  assert.equal(inspect(valid.input, valid.reference, { maxDepth: NaN }).status, "NUMERICAL_FAILURE");
});
