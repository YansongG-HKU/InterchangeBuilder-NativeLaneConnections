// Bounded local candidate family. No game node selection, obstacle rerouting or global optimizer.
export const VERSION = "tail-v1";
const add = (a, b) => a.map((v, i) => v + b[i]),
  scale = (a, k) => a.map((v) => v * k);
const normal = (t) => [-t[1], t[0]];
export function portPosition(p) {
  return add(p.position, scale(normal(p.direction), p.offset));
}
function evaluate(c, t) {
  const n = c.length - 1;
  let choose = 1;
  return c.reduce(
    (p, q, i) => {
      if (i) choose = (choose * (n - i + 1)) / i;
      return add(p, scale(q, choose * t ** i * (1 - t) ** (n - i)));
    },
    [0, 0],
  );
}
function length(c) {
  let total = 0,
    p = c[0];
  for (let i = 1; i <= 2048; i++) {
    const q = evaluate(c, i / 2048);
    total += Math.hypot(q[0] - p[0], q[1] - p[1]);
    p = q;
  }
  return total;
}
export function validateInput(input) {
  const finite = (x) => typeof x === "number" && Number.isFinite(x);
  const point = (p) =>
    Array.isArray(p) &&
    p.length === 2 &&
    p.every(finite) &&
    p.every((x) => Math.abs(x) <= 10000);
  if (
    !input ||
    input.schema !== 1 ||
    !["normal", "no-collision"].includes(input.mode) ||
    !["G1", "G2"].includes(input.continuity)
  )
    return "schema/mode/continuity";
  for (const p of [input.start, input.end])
    if (
      !p ||
      typeof p.id !== "string" ||
      !p.id ||
      !point(p.position) ||
      !point(p.direction) ||
      Math.abs(Math.hypot(...p.direction) - 1) > 1e-8 ||
      !finite(p.width) ||
      p.width <= 0 ||
      p.width > 50 ||
      !finite(p.offset) ||
      Math.abs(p.offset) > 100 ||
      !finite(p.curvature) ||
      Math.abs(p.curvature) > 1 ||
      !finite(p.z) ||
      Math.abs(p.z) > 1000 ||
      !finite(p.grade) ||
      Math.abs(p.grade) > 0.3
    )
      return "port";
  if (
    [input.start, input.end].some(
      (p) =>
        !["left", "middle", "right"].includes(p.side) ||
        (p.side === "left" && p.offset <= 0) ||
        (p.side === "right" && p.offset >= 0) ||
        (p.side === "middle" && p.offset !== 0),
    )
  )
    return "side/offset";
  if (
    !finite(input.width) ||
    input.width <= 0 ||
    input.width > 50 ||
    !finite(input.tail) ||
    input.tail <= 0 ||
    input.tail > 500 ||
    !finite(input.maxGrade) ||
    input.maxGrade <= 0 ||
    input.maxGrade > 0.3 ||
    !Array.isArray(input.obstacles) ||
    input.obstacles.length > 12 ||
    !Array.isArray(input.joins) ||
    input.joins.length > 4
  )
    return "bounds";
  for (const o of input.obstacles)
    if (
      !o ||
      typeof o.id !== "string" ||
      !["road", "building"].includes(o.kind) ||
      !point(o.a) ||
      !point(o.b) ||
      Math.hypot(o.b[0] - o.a[0], o.b[1] - o.a[1]) < 1e-6 ||
      !finite(o.width) ||
      o.width <= 0 ||
      o.width > 50
    )
      return "obstacle";
  if (new Set(input.obstacles.map((o) => o.id)).size !== input.obstacles.length)
    return "obstacle ids";
  for (const j of input.joins)
    if (
      j.route !== "ramp" ||
      typeof j.road !== "string" ||
      !Array.isArray(j.box) ||
      j.box.length !== 4 ||
      !j.box.every(finite) ||
      j.box[0] >= j.box[2] ||
      j.box[1] >= j.box[3]
    )
      return "join";
  return null;
}
export function generate(input, budget = 3) {
  const invalid = validateInput(input);
  if (invalid)
    return { status: "INPUT_INVALID", reason: invalid, candidates: [] };
  if (
    [input.start, input.end].some(
      (p) => p.width !== input.width || 1 - p.offset * p.curvature <= 0.05,
    )
  )
    return {
      status: "UNSUPPORTED",
      reason:
        "Variable-width transitions or singular offset ports are unsupported.",
      candidates: [],
    };
  if (input.verticalMode !== "endpoint")
    return {
      status: "UNSUPPORTED",
      reason:
        "Intermediate crossing profile generation is not implemented; inspect the explicit reference instead.",
      candidates: [],
    };
  if (input.continuity === "G2" && Math.abs(input.end.curvature) > 1e-9)
    return {
      status: "PROVEN_CONSTRAINT_CONFLICT",
      reason:
        "A finite straight tail has zero curvature; the fixed target requires nonzero curvature and G2.",
      candidates: [],
    };
  const a = portPosition(input.start),
    b = portPosition(input.end),
    q = add(b, scale(input.end.direction, -input.tail));
  const distance = Math.hypot(q[0] - a[0], q[1] - a[1]);
  if (distance < 1e-6)
    return {
      status: "CANDIDATE_FAILED",
      reason: "Degenerate transition in this candidate family.",
      candidates: [],
    };
  const candidates = [];
  for (const factor of [0.8, 1, 1.2].slice(
    0,
    Math.max(0, Math.min(3, budget)),
  )) {
    const speed = distance * factor,
      v = scale(input.start.direction, speed),
      w = scale(input.end.direction, speed),
      acc = scale(
        normal(input.start.direction),
        (input.start.curvature /
          (1 - input.start.offset * input.start.curvature)) *
          speed *
          speed,
      );
    // Quintic endpoint derivative constraints; acceleration at tail join is zero.
    const controls = [
      a,
      add(a, scale(v, 1 / 5)),
      add(add(a, scale(v, 2 / 5)), scale(acc, 1 / 20)),
      add(q, scale(w, -2 / 5)),
      add(q, scale(w, -1 / 5)),
      q,
    ];
    const curveLength = length(controls),
      L = curveLength + input.tail;
    const z0 = input.start.z,
      z1 = input.end.z,
      g0 = input.start.grade / (1 - input.start.offset * input.start.curvature),
      g1 = input.end.grade / (1 - input.end.offset * input.end.curvature);
    const A = 2 * z0 - 2 * z1 + L * (g0 + g1),
      B = -3 * z0 + 3 * z1 - L * (2 * g0 + g1),
      C = L * g0;
    const u = curveLength / L,
      z = (A * u + B) * u * u + C * u + z0,
      g = (3 * A * u * u + 2 * B * u + C) / L;
    candidates.push({
      schema: 1,
      id: `q${factor}`,
      route: "ramp",
      startId: input.start.id,
      endId: input.end.id,
      width: input.width,
      segments: [
        { kind: "bezier", controls, z: { z0, z1: z, g0, g1: g } },
        { kind: "line", controls: [q, b], z: { z0: z, z1, g0: g, g1 } },
      ],
    });
  }
  return {
    status: candidates.length ? "CANDIDATES" : "SEARCH_BUDGET_EXHAUSTED",
    candidates,
  };
}
// Lossless geometry transport, not a game conversion. Never refit a straight tail.
export function convert(output) {
  return JSON.parse(JSON.stringify(output));
}
