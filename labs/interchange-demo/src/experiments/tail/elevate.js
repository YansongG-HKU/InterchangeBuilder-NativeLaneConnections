// Candidate construction only. The inspector independently bounds actual output curves.
const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
function split(c, t) {
  let q = c.map((p) => p.slice()),
    l = [q[0]],
    r = [q.at(-1)];
  while (q.length > 1) {
    q = q.slice(1).map((p, i) => p.map((v, k) => q[i][k] * (1 - t) + v * t));
    l.push(q[0]);
    r.unshift(q.at(-1));
  }
  return [l, r];
}
function pointDistance(p, a, b) {
  const x = b[0] - a[0],
    y = b[1] - a[1],
    u = Math.max(
      0,
      Math.min(
        1,
        ((p[0] - a[0]) * x + (p[1] - a[1]) * y) / (x * x + y * y || 1),
      ),
    );
  return dist(p, [a[0] + u * x, a[1] + u * y]);
}
function segmentDistance(a, b, c, d) {
  const x = b[0] - a[0],
    y = b[1] - a[1],
    xx = d[0] - c[0],
    yy = d[1] - c[1],
    den = x * yy - y * xx;
  if (Math.abs(den) > 1e-12) {
    const u = ((c[0] - a[0]) * yy - (c[1] - a[1]) * xx) / den,
      v = ((c[0] - a[0]) * y - (c[1] - a[1]) * x) / den;
    if (u >= 0 && u <= 1 && v >= 0 && v <= 1) return 0;
  }
  return Math.min(
    pointDistance(a, c, d),
    pointDistance(b, c, d),
    pointDistance(c, a, b),
    pointDistance(d, a, b),
  );
}
function pieces(c) {
  const result = [];
  let exhausted = false;
  function walk(q, a, b, depth) {
    const chord = dist(q[0], q.at(-1)),
      upper = q.slice(1).reduce((s, p, i) => s + dist(p, q[i]), 0),
      error = Math.max(...q.map((p) => pointDistance(p, q[0], q.at(-1))));
    if (depth >= 18 || result.length > 4096) {
      exhausted = true;
      return;
    }
    if (chord <= 2 && error <= 0.005 && upper - chord <= 0.005) {
      result.push({ a, b, q, error, length: (chord + upper) / 2 });
      return;
    }
    const [l, r] = split(q, 0.5),
      m = (a + b) / 2;
    walk(l, a, m, depth + 1);
    walk(r, m, b, depth + 1);
  }
  walk(c, 0, 1, 0);
  return { result, exhausted };
}
function hermite(z0, z1, g0, g1, L, s) {
  const t = s / L,
    A = 2 * z0 - 2 * z1 + L * (g0 + g1),
    B = 3 * (z1 - z0) - L * (2 * g0 + g1),
    C = L * g0;
  return {
    z: ((A * t + B) * t + C) * t + z0,
    g: (3 * A * t * t + 2 * B * t + C) / L,
  };
}
export function elevateCandidates(input, plan) {
  const roads = input.obstacles.filter((o) => o.crossing),
    unsupported = (reason) => ({
      status: "UNSUPPORTED",
      reason,
      candidates: [],
    });
  if (
    !Number.isFinite(input.deckThickness) ||
    !Number.isFinite(input.clearance) ||
    !roads.length ||
    roads.some((o) => o.kind !== "road" || o.controls || !Number.isFinite(o.z))
  )
    return unsupported(
      "Curved/oblique elevation requires known thickness and flat straight lower-road swept corridors.",
    );
  if (!plan.candidates.length) return plan;
  const candidates = [];
  for (const base of plan.candidates) {
    const flat = base.segments.map((s) => pieces(s.controls));
    if (flat.some((x) => x.exhausted)) continue;
    const hits = [];
    flat.forEach((f, si) =>
      f.result.forEach((l) => {
        if (
          roads.some(
            (o) =>
              segmentDistance(l.q[0], l.q.at(-1), o.a, o.b) <=
              (input.width + o.width) / 2 + l.error + 0.5,
          )
        )
          hits.push({ si, a: l.a, b: l.b });
      }),
    );
    if (!hits.length) continue;
    const first = hits[0],
      last = hits.at(-1),
      parts = [];
    base.segments.forEach((s, si) => {
      const cuts = [0, 1];
      if (si === first.si) cuts.push(first.a);
      if (si === last.si) cuts.push(last.b);
      const sorted = [...new Set(cuts)].sort((a, b) => a - b);
      let q = s.controls,
        previous = 0;
      for (const t of sorted.slice(1)) {
        const [head, rest] = split(q, (t - previous) / (1 - previous));
        const center = si + (previous + t) / 2;
        parts.push({
          kind: s.kind,
          controls: head,
          zone:
            center < first.si + first.a
              ? "rise"
              : center > last.si + last.b
                ? "fall"
                : "plateau",
        });
        q = rest;
        previous = t;
      }
    });
    const lengths = parts.map((s) => pieces(s.controls));
    if (lengths.some((x) => x.exhausted)) continue;
    const L = lengths.map((x) => x.result.reduce((s, l) => s + l.length, 0)),
      rise = L.reduce((s, l, i) => s + (parts[i].zone === "rise" ? l : 0), 0),
      fall = L.reduce((s, l, i) => s + (parts[i].zone === "fall" ? l : 0), 0);
    if (rise < 1e-6 || fall < 1e-6) continue;
    const H = Math.max(
      input.start.z,
      input.end.z,
      ...roads.map((o) => o.z + input.deckThickness + input.clearance + 0.1),
    );
    let rs = 0,
      fs = 0;
    for (let i = 0; i < parts.length; i++) {
      const p = parts[i],
        g0 =
          input.start.grade / (1 - input.start.offset * input.start.curvature),
        g1 = input.end.grade / (1 - input.end.offset * input.end.curvature);
      let a, b;
      if (p.zone === "rise") {
        a = hermite(input.start.z, H, g0, 0, rise, rs);
        rs += L[i];
        b = hermite(input.start.z, H, g0, 0, rise, rs);
      } else if (p.zone === "fall") {
        a = hermite(H, input.end.z, 0, g1, fall, fs);
        fs += L[i];
        b = hermite(H, input.end.z, 0, g1, fall, fs);
      } else a = b = { z: H, g: 0 };
      p.z = { z0: a.z, z1: b.z, g0: a.g, g1: b.g };
      delete p.zone;
    }
    candidates.push({
      ...base,
      id: "elevated-" + base.id,
      thickness: input.deckThickness,
      segments: parts,
    });
  }
  return {
    status: candidates.length ? "CANDIDATES" : "SEARCH_BUDGET_EXHAUSTED",
    reason: candidates.length
      ? undefined
      : "No rise/fall space or subdivision budget left in this bounded family.",
    candidates,
  };
}
