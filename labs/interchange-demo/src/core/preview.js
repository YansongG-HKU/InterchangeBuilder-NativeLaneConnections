import { reverseProfile } from "./vertical.js";
import { station } from "./geometry.js";
const elevation = (p, s) => {
  const t = s / p.length,
    [a, b, c, d] = p.coefficients;
  return ((a * t + b) * t + c) * t + d;
};
function section(curve, profile, start, end, dir, offset) {
  const ss = [
    start,
    ...curve.nodes
      .map((n) => n.s)
      .filter(
        (s) =>
          s > Math.min(start, end) + 1e-8 && s < Math.max(start, end) - 1e-8,
      ),
    end,
  ].sort((a, b) => dir * (a - b));
  return ss.map((s) => {
    const a = station(curve, s, dir, offset);
    return [...a.p, elevation(profile, s)];
  });
}
export function connectionPreview(input, result, candidate) {
  if (!result.curves || !result.vertical) return null;
  const v = candidate?.vertical?.mainlines?.length
      ? candidate.vertical
      : result.vertical,
    side = input.trafficSide === "RHT" ? -1 : 1,
    offset = (side * input.roadWidth) / 4;
  const roads = v.mainlines.map((m, i) => ({
    id: m.id,
    kind: "mainline",
    width: input.roadWidth,
    side,
    points: m.points,
    invalid: v.diagnostics.some((d) => d.path === `vertical.profiles[${i}]`),
  }));
  const paths = {},
    profiles = {};
  for (const od of input.required) {
    const [from, to] = od.split(">"),
      i = from[0] === "A" ? 0 : 1,
      di = from[1] === "+" ? 1 : -1,
      curve = result.curves[i],
      profile = v.mainlines[i].profile;
    if (from === to) {
      profiles[od] = di === 1 ? profile : reverseProfile(profile);
      paths[od] = section(
        curve,
        profile,
        di === 1 ? 0 : curve.length,
        di === 1 ? curve.length : 0,
        di,
        offset,
      );
      continue;
    }
    const route = candidate?.routes?.find((r) => r.id === od),
      ramp = v.ramps.find((r) => r.id === od);
    if (!route || !ramp?.points.length) continue;
    profiles[od] = ramp.profile;
    const j = to[0] === "A" ? 0 : 1,
      dj = to[1] === "+" ? 1 : -1,
      target = result.curves[j];
    const incoming = section(
        curve,
        profile,
        di === 1 ? 0 : curve.length,
        route.a.s,
        di,
        offset,
      ),
      outgoing = section(
        target,
        v.mainlines[j].profile,
        route.b.s,
        dj === 1 ? target.length : 0,
        dj,
        offset,
      );
    paths[od] = [...incoming, ...ramp.points.slice(1), ...outgoing.slice(1)];
    roads.push({
      id: od,
      kind: "ramp",
      width: route.width,
      points: ramp.points,
      invalid:
        candidate.diagnostics.some((d) => d.path === od) ||
        ramp.status === "UNSUPPORTED",
    });
  }
  const all = roads.flatMap((r) => r.points),
    xs = all.map((p) => p[0]),
    ys = all.map((p) => p[1]),
    zs = all.map((p) => p[2]);
  return {
    roads,
    paths,
    profiles,
    center: [
      (Math.min(...xs) + Math.max(...xs)) / 2,
      (Math.min(...ys) + Math.max(...ys)) / 2,
      (Math.min(...zs) + Math.max(...zs)) / 2,
    ],
    extent:
      Math.max(
        200,
        Math.max(...xs) - Math.min(...xs),
        Math.max(...ys) - Math.min(...ys),
      ) * 1.15,
    datum: 0,
    clearance: "UNKNOWN",
    layerCount: "UNKNOWN",
    crossingDiagnostics: result.crossings.map((h) => ({
      point: h.p,
      heightDifference: Math.abs(
        elevation(v.mainlines[0].profile, h.s[0]) -
          elevation(v.mainlines[1].profile, h.s[1]),
      ),
      meaning: "CENTERLINE_HEIGHT_DIFFERENCE_NOT_CLEARANCE",
    })),
  };
}
