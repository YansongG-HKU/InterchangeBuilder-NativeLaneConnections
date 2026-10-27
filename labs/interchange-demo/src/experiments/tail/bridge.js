// Bounded automatic plateau family, not arbitrary obstacle routing.
// All intervals are exact horizontal stations on a fixed +X straight alignment.
export function generateBridge(input, budget) {
  const origin = input.start.position,
    d = input.start.direction;
  const local = (p) => [
    (p[0] - origin[0]) * d[0] + (p[1] - origin[1]) * d[1],
    -(p[0] - origin[0]) * d[1] + (p[1] - origin[1]) * d[0],
  ];
  const direction = (v) => [
    v[0] * d[0] + v[1] * d[1],
    -v[0] * d[1] + v[1] * d[0],
  ];
  const i = structuredClone(input);
  for (const p of [i.start, i.end]) {
    p.position = local(p.position);
    p.direction = direction(p.direction);
  }
  for (const o of i.obstacles) {
    o.a = local(o.a);
    o.b = local(o.b);
    if (o.controls) o.controls = o.controls.map(local);
  }
  const near = (x) => Math.abs(x) < 1e-8;
  for (const p of [i.start, i.end]) {
    if (near(p.position[1])) p.position[1] = 0;
    if (near(p.direction[0] - 1) && near(p.direction[1])) p.direction = [1, 0];
  }
  for (const o of i.obstacles) if (near(o.a[0] - o.b[0])) o.b[0] = o.a[0];
  const r = generateLocal(i, budget);
  for (const c of r.candidates)
    for (const s of c.segments)
      s.controls = s.controls.map(([x, y]) => [
        origin[0] + x * d[0] - y * d[1],
        origin[1] + x * d[1] + y * d[0],
      ]);
  return r;
}
function generateLocal(input, budget) {
  const unsupported = (reason) => ({
    status: "UNSUPPORTED",
    reason,
    candidates: [],
  });
  const a = input.start,
    b = input.end,
    roads = input.obstacles.filter((o) => o.crossing);
  if (
    !Number.isFinite(input.deckThickness) ||
    !Number.isFinite(input.clearance)
  )
    return unsupported(
      "Known deckThickness and clearance are required for automatic crossing profiles.",
    );
  if (
    a.offset !== 0 ||
    b.offset !== 0 ||
    a.position[1] !== b.position[1] ||
    a.direction[0] !== 1 ||
    b.direction[0] !== 1 ||
    a.curvature !== 0 ||
    b.curvature !== 0 ||
    !roads.length ||
    roads.some(
      (o) =>
        o.kind !== "road" ||
        o.controls ||
        o.a[0] !== o.b[0] ||
        !Number.isFinite(o.z) ||
        Math.min(o.a[1], o.b[1]) > a.position[1] - input.width / 2 ||
        Math.max(o.a[1], o.b[1]) < a.position[1] + input.width / 2,
    )
  )
    return unsupported(
      "Automatic crossing requires +X straight fixed ports and flat orthogonal full-width lower roads; no curved or offset crossing model.",
    );
  const x0 = a.position[0],
    x1 = b.position[0],
    y = a.position[1];
  const lo = Math.min(...roads.map((o) => o.a[0] - o.width / 2)),
    hi = Math.max(...roads.map((o) => o.a[0] + o.width / 2));
  if (lo <= x0 || hi >= x1 || x1 - x0 < input.tail)
    return {
      status: "CANDIDATE_FAILED",
      reason:
        "This plateau family has no positive rise/fall station interval or sufficient straight tail.",
      candidates: [],
    };
  const height = Math.max(
    a.z,
    b.z,
    ...roads.map((o) => o.z + input.deckThickness + input.clearance + 0.05),
  );
  // Necessary integral bound only on this fixed straight horizontal route.
  if (Math.abs(b.z - a.z) > input.maxGrade * (x1 - x0) + 1e-9)
    return {
      status: "PROVEN_CONSTRAINT_CONFLICT",
      reason:
        "Endpoint height difference exceeds maximum grade times this fixed horizontal route length; no claim about a longer route.",
      candidates: [],
    };
  const candidates = [];
  for (const margin of [0, 2, 5].slice(0, Math.max(0, Math.min(3, budget)))) {
    const l = lo - margin,
      r = hi + margin;
    if (l <= x0 || r >= x1) continue;
    const segment = (x, z0, xx, z1, g0, g1) => ({
      kind: "line",
      controls: [
        [x, y],
        [xx, y],
      ],
      z: { z0, z1, g0, g1 },
    });
    candidates.push({
      schema: 1,
      id: `bridge-${margin}`,
      route: "ramp",
      startId: a.id,
      endId: b.id,
      width: input.width,
      thickness: input.deckThickness,
      segments: [
        segment(x0, a.z, l, height, a.grade, 0),
        segment(l, height, r, height, 0, 0),
        segment(r, height, x1, b.z, 0, b.grade),
      ],
    });
  }
  return {
    status: candidates.length ? "CANDIDATES" : "SEARCH_BUDGET_EXHAUSTED",
    candidates,
  };
}
