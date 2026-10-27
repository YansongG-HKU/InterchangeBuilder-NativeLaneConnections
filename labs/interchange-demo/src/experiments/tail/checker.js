// Independent output inspector: no import from generator, ignores valid/connected flags.
const sub = (a, b) => [a[0] - b[0], a[1] - b[1]],
  dot = (a, b) => a[0] * b[0] + a[1] * b[1],
  norm = (a) => Math.hypot(...a),
  cross = (a, b) => a[0] * b[1] - a[1] * b[0];
const mid = (a, b) => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
export function casteljau(c, t) {
  let q = c.map((p) => p.slice());
  while (q.length > 1)
    q = q.slice(1).map((p, i) => p.map((v, k) => q[i][k] * (1 - t) + v * t));
  return q[0];
}
function derivatives(c) {
  return c.slice(1).map((p, i) => sub(p, c[i]).map((x) => x * (c.length - 1)));
}
function jet(c, t) {
  const d = casteljau(derivatives(c), t),
    dd = c.length > 2 ? casteljau(derivatives(derivatives(c)), t) : [0, 0];
  return { p: casteljau(c, t), d, k: cross(d, dd) / norm(d) ** 3 };
}
function pointDistance(p, a, b) {
  const d = sub(b, a),
    t = Math.max(0, Math.min(1, dot(sub(p, a), d) / (dot(d, d) || 1)));
  return norm(
    sub(
      p,
      a.map((x, i) => x + t * d[i]),
    ),
  );
}
function segmentDistance(a, b, c, d) {
  const r = sub(b, a),
    s = sub(d, c),
    den = cross(r, s);
  if (Math.abs(den) > 1e-12) {
    const u = cross(sub(c, a), s) / den,
      v = cross(sub(c, a), r) / den;
    if (u >= 0 && u <= 1 && v >= 0 && v <= 1) return 0;
  }
  return Math.min(
    pointDistance(a, c, d),
    pointDistance(b, c, d),
    pointDistance(c, a, b),
    pointDistance(d, a, b),
  );
}
function split(c) {
  let q = c.map((p) => p.slice()),
    left = [q[0]],
    right = [q.at(-1)];
  while (q.length > 1) {
    q = q.slice(1).map((p, i) => mid(p, q[i]));
    left.push(q[0]);
    right.unshift(q.at(-1));
  }
  return [left, right];
}
export function flatten(c, tolerance = 0.02, maxDepth = 16) {
  const leaves = [];
  let unresolved = false;
  function walk(q, depth, t0, t1) {
    const chord = norm(sub(q.at(-1), q[0])),
      upper = q.slice(1).reduce((s, p, i) => s + norm(sub(p, q[i])), 0),
      error = Math.max(...q.map((p) => pointDistance(p, q[0], q.at(-1))));
    if (
      (error <= tolerance && upper - chord <= tolerance) ||
      depth === maxDepth ||
      leaves.length >= 1024
    ) {
      const projection = derivatives(q).map(
        (d) => dot(d, sub(q.at(-1), q[0])) / (chord || 1),
      );
      const speed = Math.min(...projection);
      const dd =
        q.length > 2 ? Math.max(...derivatives(derivatives(q)).map(norm)) : 0;
      leaves.push({
        a: q[0],
        b: q.at(-1),
        error,
        lower: chord,
        upper,
        t0,
        t1,
        curvatureBound: speed > 1e-10 ? dd / speed ** 2 : Infinity,
      });
      if (error > tolerance || upper - chord > tolerance || speed <= 1e-10)
        unresolved = true;
      return;
    }
    const [l, r] = split(q),
      t = (t0 + t1) / 2;
    walk(l, depth + 1, t0, t);
    walk(r, depth + 1, t, t1);
  }
  walk(c, 0, 0, 1);
  return {
    leaves,
    unresolved,
    lower: leaves.reduce((s, x) => s + x.lower, 0),
    upper: leaves.reduce((s, x) => s + x.upper, 0),
  };
}
function profile(z, L) {
  const { z0, z1, g0, g1 } = z,
    A = 2 * z0 - 2 * z1 + L * (g0 + g1),
    B = 3 * (z1 - z0) - L * (2 * g0 + g1),
    C = L * g0;
  const value = (t) => ((A * t + B) * t + C) * t + z0,
    grade = (t) => (3 * A * t * t + 2 * B * t + C) / L;
  const ts = [0, 1];
  if (Math.abs(A) > 1e-12) {
    const t = -B / (3 * A);
    if (t > 0 && t < 1) ts.push(t);
  }
  return { value, grade, peak: Math.max(...ts.map((t) => Math.abs(grade(t)))) };
}
const target = (p) => [
  p.position[0] - p.direction[1] * p.offset,
  p.position[1] + p.direction[0] * p.offset,
];
function angle(a, b) {
  return Math.abs(Math.atan2(cross(a, b), dot(a, b)));
}
export function inspect(
  input,
  output,
  { tolerance = 0.02, maxDepth = 16 } = {},
) {
  const issues = [],
    metrics = {},
    fail = (code, detail = {}, severity = "error") =>
      issues.push({ code, severity, ...detail });
  if (
    !Number.isFinite(tolerance) ||
    tolerance <= 0 ||
    !Number.isInteger(maxDepth) ||
    maxDepth < 0 ||
    maxDepth > 18
  )
    return {
      status: "NUMERICAL_FAILURE",
      issues: [{ code: "RESOLUTION_INVALID", severity: "error" }],
      metrics,
    };
  const segments = output?.segments;
  if (
    !Array.isArray(segments) ||
    !segments.length ||
    segments.length > 16 ||
    !segments.every(
      (s) =>
        s &&
        ["bezier", "line"].includes(s.kind) &&
        Array.isArray(s.controls) &&
        s.controls.length >= 2 &&
        s.controls.length <= 6 &&
        (s.kind !== "line" || s.controls.length === 2) &&
        s.controls.every(
          (p) =>
            Array.isArray(p) &&
            p.length === 2 &&
            p.every((v) => Number.isFinite(v) && Math.abs(v) <= 10000),
        ) &&
        s.z &&
        ["z0", "z1", "g0", "g1"].every((k) =>
          Number.isFinite(s.z[k]) && Math.abs(s.z[k]) <= (k.startsWith("z") ? 10000 : 10)),
    )
  )
    return {
      status: "NUMERICAL_FAILURE",
      issues: [{ code: "INVALID_OUTPUT", severity: "error" }],
      metrics,
      points: [],
    };
  if (
    output.route !== "ramp" ||
    output.startId !== input.start.id ||
    output.endId !== input.end.id
  )
    fail("IDENTITY_MISMATCH");
  if (
    output.width !== input.width ||
    !Number.isFinite(output.width) ||
    output.width <= 0
  )
    return {
      status: "CANDIDATE_FAILED",
      issues: [{ code: "WIDTH_MISMATCH", severity: "error" }],
      metrics,
    };
  if (
    [input.start, input.end].some(
      (p) => p.width !== input.width || 1 - p.offset * p.curvature <= 0.05,
    )
  )
    fail("WIDTH_OR_OFFSET_UNSUPPORTED", {}, "unknown");
  const curves = segments.map((s) => flatten(s.controls, tolerance, maxDepth));
  if (curves.some((c) => c.lower < 1e-8))
    return {
      status: "CANDIDATE_FAILED",
      issues: [{ code: "DEGENERATE", severity: "error" }],
      metrics,
    };
  if (curves.some((c) => c.unresolved))
    fail("RESOLUTION_OR_REGULARITY", {}, "unknown");
  const ends = [jet(segments[0].controls, 0), jet(segments.at(-1).controls, 1)];
  metrics.positionErrors = ends.map((e, i) =>
    norm(sub(e.p, target(i ? input.end : input.start))),
  );
  metrics.directionErrors = ends.map((e, i) =>
    angle(e.d, (i ? input.end : input.start).direction),
  );
  metrics.curvatureErrors = ends.map((e, i) =>
    Math.abs(
      e.k -
        (i ? input.end : input.start).curvature /
          (1 -
            (i ? input.end : input.start).offset *
              (i ? input.end : input.start).curvature),
    ),
  );
  if (metrics.positionErrors.some((x) => x > 1e-7)) fail("ENDPOINT_POSITION");
  if (metrics.directionErrors.some((x) => !Number.isFinite(x) || x > 1e-7))
    fail("ENDPOINT_DIRECTION");
  if (metrics.curvatureErrors.some((x) => !Number.isFinite(x)))
    fail("DEGENERATE");
  else if (
    input.continuity === "G2" &&
    metrics.curvatureErrors.some((x) => x > 1e-7)
  )
    fail("ENDPOINT_CURVATURE");
  metrics.joinErrors = [];
  for (let i = 1; i < segments.length; i++) {
    const a = jet(segments[i - 1].controls, 1),
      b = jet(segments[i].controls, 0),
      j = {
        position: norm(sub(a.p, b.p)),
        direction: angle(a.d, b.d),
        curvature: Math.abs(a.k - b.k),
        height: Math.abs(segments[i - 1].z.z1 - segments[i].z.z0),
        grade: Math.abs(segments[i - 1].z.g1 - segments[i].z.g0),
      };
    metrics.joinErrors.push(j);
    if (Object.values(j).some((x) => !Number.isFinite(x) || x > 1e-7))
      fail("JOIN_DISCONTINUITY", { join: i, ...j });
  }
  metrics.heightErrors = [
    Math.abs(segments[0].z.z0 - input.start.z),
    Math.abs(segments.at(-1).z.z1 - input.end.z),
  ];
  metrics.gradeErrors = [
    Math.abs(
      segments[0].z.g0 -
        input.start.grade / (1 - input.start.offset * input.start.curvature),
    ),
    Math.abs(
      segments.at(-1).z.g1 -
        input.end.grade / (1 - input.end.offset * input.end.curvature),
    ),
  ];
  if ([...metrics.heightErrors, ...metrics.gradeErrors].some((x) => x > 1e-7))
    fail("VERTICAL_ENDPOINT");
  // Maximal terminal run must be explicitly straight, forward, and collinear.
  let tail = 0;
  const end = target(input.end);
  for (let i = segments.length - 1; i >= 0; i--) {
    const s = segments[i];
    if (
      s.kind !== "line" ||
      s.controls.some(
        (p) => Math.abs(cross(sub(p, end), input.end.direction)) > 1e-7,
      ) ||
      dot(sub(s.controls.at(-1), s.controls[0]), input.end.direction) <= 0
    )
      break;
    tail += norm(sub(s.controls.at(-1), s.controls[0]));
  }
  metrics.straightTail = tail;
  if (tail + 1e-7 < input.tail)
    fail("TAIL_TOO_SHORT", { measured: tail, required: input.tail });
  metrics.lengthBounds = [
    curves.reduce((v, c) => v + c.lower, 0),
    curves.reduce((v, c) => v + c.upper, 0),
  ];
  metrics.maxGrade = 0;
  metrics.gradeUncertainty = 0;
  segments.forEach((s, i) => {
    const c = curves[i],
      lo = profile(s.z, c.lower),
      hi = profile(s.z, c.upper);
    metrics.maxGrade = Math.max(metrics.maxGrade, lo.peak, hi.peak);
    metrics.gradeUncertainty = Math.max(
      metrics.gradeUncertainty,
      1.5 * Math.abs(s.z.z1 - s.z.z0) * Math.abs(1 / c.lower - 1 / c.upper),
    );
    if (
      Math.max(lo.peak, hi.peak) -
        1.5 * Math.abs(s.z.z1 - s.z.z0) * Math.abs(1 / c.lower - 1 / c.upper) >
      input.maxGrade + 1e-9
    )
      fail("GRADE_LIMIT", { segment: i, measured: Math.max(lo.peak, hi.peak) });
    else if (Math.max(lo.peak, hi.peak) > input.maxGrade - 1e-9)
      fail("GRADE_UNCERTAIN", { segment: i }, "unknown");
  });
  let station = 0;
  const leaves = curves.flatMap((c, si) =>
    c.leaves.map((l) => {
      const r = { ...l, segment: si, station };
      station += l.lower;
      return r;
    }),
  );
  if (leaves.length > 2048)
    return {
      status: "UNKNOWN",
      issues: [...issues, { code: "INSPECTION_BUDGET", severity: "unknown" }],
      metrics,
      collisions: [],
      points: [],
      clearance: "UNKNOWN",
    };
  if (leaves.some((l) => (l.curvatureBound * input.width) / 2 >= 1))
    fail("OFFSET_REGULARITY_UNCERTAIN", {}, "unknown");
  const maxK = Math.max(...leaves.map((l) => l.curvatureBound));
  metrics.maxAbsCurvatureUpperBound = Number.isFinite(maxK) ? maxK : null;
  metrics.minRadiusLowerBound =
    Number.isFinite(maxK) && maxK > 0 ? 1 / maxK : null;
  metrics.minRoadGap = null;
  const collisions = [];
  for (const o of input.obstacles) {
    let gap = Infinity,
      uncertain = false,
      hit = false,
      exempt = 0;
    for (const l of leaves) {
      const d =
        segmentDistance(l.a, l.b, o.a, o.b) - (input.width + o.width) / 2;
      const pad = input.width / 2 + l.error;
      // Conservative whole primitive bound; exemption is a named pair AND a region.
      const join = input.joins.some(
        (j) =>
          j.route === output.route &&
          j.road === o.id &&
          [l.a, l.b].every(
            (p) =>
              p[0] - pad >= j.box[0] &&
              p[0] + pad <= j.box[2] &&
              p[1] - pad >= j.box[1] &&
              p[1] + pad <= j.box[3],
          ),
      );
      if (join) {
        exempt++;
        continue;
      }
      gap = Math.min(gap, d - l.error);
      if (d + l.error < -1e-7) hit = true;
      else if (d - l.error <= 1e-7) uncertain = true;
    }
    if (Number.isFinite(gap))
      metrics.minRoadGap =
        metrics.minRoadGap === null ? gap : Math.min(gap, metrics.minRoadGap);
    if (hit) {
      const clearance = o.crossing
        ? crossingClearance(output, o, input.clearance)
        : null;
      collisions.push({
        road: o.id,
        kind: o.kind,
        gap,
        exemptPrimitives: exempt,
        clearance,
      });
      if (clearance?.status === "VERIFIED_REFERENCE") {
      } else if (clearance)
        fail(
          clearance.status === "FAILED"
            ? "CLEARANCE_LIMIT"
            : "CLEARANCE_UNSUPPORTED",
          { road: o.id, clearance },
          clearance.status === "FAILED" ? "error" : "unknown",
        );
      else fail("CORRIDOR_COLLISION", { road: o.id, kind: o.kind, gap });
    } else if (uncertain) fail("CORRIDOR_UNCERTAIN", { road: o.id }, "unknown");
  }
  // Exclude only local longitudinal overlap of the swept strip; test distant limbs.
  for (let i = 0; i < leaves.length; i++)
    for (let j = i + 2; j < leaves.length; j++) {
      const a = leaves[i],
        b = leaves[j],
        d = segmentDistance(a.a, a.b, b.a, b.b);
      if (d < 1e-9 && a.error + b.error < 1e-10)
        fail("SELF_INTERSECTION", { segments: [i, j] });
      else if (
        b.station - a.station - a.lower > input.width * 2 &&
        d < input.width - a.error - b.error
      )
        fail("NONLOCAL_CORRIDOR_OVERLAP", { segments: [i, j] });
      else if (
        b.station - a.station - a.lower > input.width * 2 &&
        d <= input.width + a.error + b.error
      )
        fail("SELF_CORRIDOR_UNCERTAIN", { segments: [i, j] }, "unknown");
    }
  const points = [];
  segments.forEach((s, i) => {
    const c = curves[i],
      z = profile(s.z, c.lower);
    let length = 0;
    c.leaves.forEach((l, j) => {
      if (i === 0 && j === 0) points.push([...l.a, z.value(0)]);
      const steps = Math.max(1, Math.ceil(l.lower / 2));
      for (let step = 1; step <= steps; step++) {
        const f = step / steps,
          t = l.t0 + (l.t1 - l.t0) * f;
        points.push([
          ...casteljau(s.controls, t),
          z.value((length + l.lower * f) / c.lower),
        ]);
      }
      length += l.lower;
    });
  });
  // Never allow failed floating-point arithmetic to fall through comparisons.
  const finiteTree = (v) => typeof v === "number" ? Number.isFinite(v)
    : Array.isArray(v) ? v.every(finiteTree)
    : v && typeof v === "object" ? Object.values(v).every(finiteTree) : true;
  if (!finiteTree({ metrics, points, collisions }))
    return { status: "NUMERICAL_FAILURE", issues: [{ code: "NONFINITE_DERIVED", severity: "error" }], metrics: {}, points: [], collisions: [], clearance: "UNKNOWN" };
  const errors = issues.filter((x) => x.severity === "error"),
    unknown = issues.some((x) => x.severity === "unknown");
  return {
    status: errors.length
      ? "CANDIDATE_FAILED"
      : unknown
        ? "UNKNOWN"
        : "VERIFIED_BOUNDED",
    issues,
    metrics,
    collisions,
    points,
    scope:
      "Constant-width planar swept-disc corridor; zero crossfall. No game, terrain, pier or general 3D clearance validation.",
    clearance: "UNKNOWN",
  };
}
export function permission(facts, mode) {
  const relaxed = mode === "no-collision" ? ["building"] : [];
  const blocking = facts.issues.filter(
    (i) =>
      i.severity !== "error" ||
      i.code !== "CORRIDOR_COLLISION" ||
      !relaxed.includes(i.kind),
  );
  return {
    mode,
    supportedClasses: relaxed,
    allowedForOfflinePreview:
      blocking.length === 0 && facts.status !== "NUMERICAL_FAILURE",
    blocking,
    relaxed: facts.issues.filter(
      (i) => i.code === "CORRIDOR_COLLISION" && relaxed.includes(i.kind),
    ),
    game: "NOT_RUN",
  };
}

// Exact reference-only envelope check: straight +X upper road, perpendicular flat lower road,
// constant width, zero crossfall, known deck thickness. Not a general 3D solver.
export function crossingClearance(output, road, required) {
  const ss = output.segments,
    thickness = output.thickness;
  if (
    !Number.isFinite(required) ||
    required < 0 ||
    !Number.isFinite(thickness) ||
    thickness < 0 ||
    !Number.isFinite(road.z) ||
    !ss.every(
      (s) =>
        s.kind === "line" &&
        Math.abs(s.controls[1][1] - s.controls[0][1]) < 1e-9 &&
        s.controls[1][0] > s.controls[0][0],
    ) ||
    Math.abs(road.a[0] - road.b[0]) > 1e-9
  )
    return { status: "UNSUPPORTED" };
  const y = ss[0].controls[0][1];
  if (
    ss.some((s) => Math.abs(s.controls[0][1] - y) > 1e-9) ||
    Math.min(road.a[1], road.b[1]) > y - output.width / 2 ||
    Math.max(road.a[1], road.b[1]) < y + output.width / 2
  )
    return { status: "UNSUPPORTED" };
  const range = [road.a[0] - road.width / 2, road.a[0] + road.width / 2];
  let min = Infinity,
    covered = 0;
  for (const s of ss) {
    const x = s.controls[0][0],
      L = s.controls[1][0] - x,
      lo = Math.max(range[0], x),
      hi = Math.min(range[1], x + L);
    if (hi <= lo) continue;
    covered += hi - lo;
    const a = (lo - x) / L,
      b = (hi - x) / L,
      z = s.z,
      A = 2 * z.z0 - 2 * z.z1 + L * (z.g0 + z.g1),
      B = 3 * (z.z1 - z.z0) - L * (2 * z.g0 + z.g1),
      C = L * z.g0,
      ts = [a, b];
    if (Math.abs(A) < 1e-12) {
      if (Math.abs(B) > 1e-12) ts.push(-C / (2 * B));
    } else {
      const disc = 4 * B * B - 12 * A * C;
      if (disc >= 0)
        ts.push(
          (-2 * B - Math.sqrt(disc)) / (6 * A),
          (-2 * B + Math.sqrt(disc)) / (6 * A),
        );
    }
    const p = profile(z, L);
    for (const t of ts.filter((t) => t >= a && t <= b))
      min = Math.min(min, p.value(t) - thickness - road.z);
  }
  if (covered < range[1] - range[0] - 1e-8) return { status: "UNSUPPORTED" };
  return {
    status: min >= required ? "VERIFIED_REFERENCE" : "FAILED",
    minimum: min,
    required,
    fullOverlapX: range,
    scope:
      "Full-width orthogonal flat-deck reference only; terrain/supports NOT_RUN",
  };
}
