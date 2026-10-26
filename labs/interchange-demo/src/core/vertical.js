import { derivative, second, cross, norm } from "./geometry.js";
export const flatVertical = () => ({
  datum: "project-local",
  terrain: "unknown",
  source: "user-defined",
  maxGrade: 0.08,
  profiles: [
    { z0: 0, z1: 0, g0: 0, g1: 0 },
    { z0: 12, z1: 12, g0: 0, g1: 0 },
  ],
});
const roots = (a, b, c) =>
  Math.abs(a) < 1e-12
    ? Math.abs(b) < 1e-12
      ? []
      : [-c / b]
    : b * b - 4 * a * c < 0
      ? []
      : [
          (-b + Math.sqrt(b * b - 4 * a * c)) / (2 * a),
          (-b - Math.sqrt(b * b - 4 * a * c)) / (2 * a),
        ];
export function hermite(length, { z0, z1, g0, g1 }) {
  if (
    !Number.isFinite(length) ||
    length <= 1e-8 ||
    ![z0, z1, g0, g1].every(Number.isFinite)
  )
    throw Error("INVALID_PROFILE");
  const a = 2 * z0 - 2 * z1 + length * (g0 + g1),
    b = -3 * z0 + 3 * z1 - length * (2 * g0 + g1),
    c = length * g0,
    d = z0;
  const at = (s) => {
    const t = s / length;
    return {
      z: ((a * t + b) * t + c) * t + d,
      grade: (3 * a * t * t + 2 * b * t + c) / length,
    };
  };
  const zs = [
    0,
    1,
    ...roots(3 * a, 2 * b, c).filter((t) => t > 0 && t < 1),
  ].map((t) => ({ s: t * length, ...at(t * length) }));
  const gs = [
    0,
    1,
    ...(Math.abs(a) > 1e-12 ? [-b / (3 * a)] : []).filter(
      (t) => t > 0 && t < 1,
    ),
  ].map((t) => ({ s: t * length, ...at(t * length) }));
  return {
    length,
    coefficients: [a, b, c, d],
    at,
    minZ: zs.reduce((a, b) => (a.z < b.z ? a : b)),
    maxZ: zs.reduce((a, b) => (a.z > b.z ? a : b)),
    peakGrade: gs.reduce((a, b) =>
      Math.abs(a.grade) > Math.abs(b.grade) ? a : b,
    ),
    zCritical: zs,
    gradeCritical: gs,
  };
}
export function profileSummary(p) {
  return {
    length: p.length,
    coefficients: p.coefficients,
    minZ: p.minZ,
    maxZ: p.maxZ,
    peakGrade: p.peakGrade,
    zCritical: p.zCritical,
    gradeCritical: p.gradeCritical,
  };
}
export function attachmentGrade(curve, anchor, profile, dir, offset) {
  const d = derivative(curve.c, anchor.t),
    k = cross(d, second(curve.c, anchor.t)) / norm(d) ** 3,
    metric = 1 - offset * dir * k;
  if (metric <= 1e-6) throw Error("OFFSET_METRIC");
  const v = profile.at(anchor.s);
  return {
    z: v.z,
    grade: (dir * v.grade) / metric,
    centerGrade: v.grade,
    metric,
    dir,
    offset,
  };
}
export function verticalCandidate(input, curves, routes) {
  const mains = curves.map((c, i) =>
    hermite(c.length, input.vertical.profiles[i]),
  );
  const diagnostics = [];
  const mainlines = curves.map((c, i) => {
    const profile = mains[i];
    if (Math.abs(profile.peakGrade.grade) > input.vertical.maxGrade + 1e-10)
      diagnostics.push({
        code: "GRADE_LIMIT",
        path: `vertical.profiles[${i}]`,
        measured: 100 * Math.abs(profile.peakGrade.grade),
        limit: 100 * input.vertical.maxGrade,
        unit: "%",
        station: profile.peakGrade.s,
      });
    return {
      id: i ? "B" : "A",
      profile: profileSummary(profile),
      points: c.nodes.map((n) => [...n.p, profile.at(n.s).z]),
    };
  });
  const ramps = routes.map((r) => {
    const i = r.from[0] === "A" ? 0 : 1,
      j = r.to[0] === "A" ? 0 : 1,
      di = r.from[1] === "+" ? 1 : -1,
      dj = r.to[1] === "+" ? 1 : -1,
      offset = ((input.trafficSide === "RHT" ? -1 : 1) * input.roadWidth) / 4;
    try {
      const a = attachmentGrade(curves[i], r.a, mains[i], di, offset),
        b = attachmentGrade(curves[j], r.b, mains[j], dj, offset),
        p = hermite(r.curve.length, {
          z0: a.z,
          z1: b.z,
          g0: a.grade,
          g1: b.grade,
        });
      if (Math.abs(p.peakGrade.grade) > input.vertical.maxGrade + 1e-10)
        diagnostics.push({
          code: "GRADE_LIMIT",
          path: r.id,
          measured: 100 * Math.abs(p.peakGrade.grade),
          limit: 100 * input.vertical.maxGrade,
          unit: "%",
          station: p.peakGrade.s,
          point: r.curve.nodes.reduce((a, b) =>
            Math.abs(a.s - p.peakGrade.s) < Math.abs(b.s - p.peakGrade.s)
              ? a
              : b,
          ).p,
        });
      return {
        id: r.id,
        a,
        b,
        profile: profileSummary(p),
        points: r.curve.nodes.map((n) => [...n.p, p.at(n.s).z]),
      };
    } catch {
      diagnostics.push({ code: "UNSUPPORTED", path: r.id, point: r.a.p });
      return { id: r.id, status: "UNSUPPORTED", points: [] };
    }
  });
  return {
    status: diagnostics.length
      ? "CONSTRAINT_VIOLATION"
      : ramps.some((r) => r.status)
        ? "UNSUPPORTED"
        : "MATHEMATICAL_CHECKS_ONLY",
    mainlines,
    ramps,
    diagnostics,
    clearance: "UNKNOWN",
    terrain: "UNKNOWN",
    levels: "UNKNOWN",
    ground: "UNKNOWN",
  };
}

export function reverseProfile(p) {
  const [a, b, c, d] = p.coefficients;
  return profileSummary(
    hermite(p.length, {
      z0: a + b + c + d,
      z1: d,
      g0: -(3 * a + 2 * b + c) / p.length,
      g1: -c / p.length,
    }),
  );
}
