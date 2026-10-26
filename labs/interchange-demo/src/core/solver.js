import { connectionPreview } from "./preview.js";
import { verticalCandidate } from "./vertical.js";
import { VERSION, FULL, PORTS, laneMapping, defaultCase } from "./model.js";
import {
  sample,
  station,
  crossings,
  connect,
  regular,
  selfCross,
  offsetSafe,
  norm,
  sub,
  add,
  mul,
  normal,
  unit,
} from "./geometry.js";
import { union, ribbon, metrics, area, difference, intersect } from "./area.js";
const INPUT_KEYS = new Set(Object.keys(defaultCase()));
const fail = (code, path, values = {}) => ({ code, path, ...values });
export function validate(x) {
  const errors = [];
  const bad = (path) => errors.push(fail("INPUT_INVALID", path));
  if (!x || typeof x !== "object") return [fail("INPUT_INVALID", "$")];
  for (const key of Object.keys(x))
    if (!INPUT_KEYS.has(key)) errors.push(fail("UNSUPPORTED", key));
  if (typeof x.name !== "string" || x.name.length > 100) bad("name");
  if (x.schema !== 2 || x.solver !== VERSION)
    errors.push(fail("UNSUPPORTED", "schema/solver"));
  if (
    x.roadType !== "motor" ||
    x.laneMode !== "one-way-6-to-4+2" ||
    !["RHT", "LHT"].includes(x.trafficSide)
  )
    errors.push(fail("UNSUPPORTED", "roadType/laneMode/trafficSide"));
  const finite = (v, min, max) =>
    typeof v === "number" && Number.isFinite(v) && v >= min && v <= max;
  const v = x.vertical;
  if (
    !v ||
    typeof v !== "object" ||
    Object.keys(v).some(
      (k) =>
        !["datum", "terrain", "source", "maxGrade", "profiles"].includes(k),
    ) ||
    v.datum !== "project-local" ||
    v.terrain !== "unknown" ||
    !["user-defined", "legacy-flat-accepted"].includes(v.source) ||
    !finite(v.maxGrade, 0.001, 0.3) ||
    !Array.isArray(v.profiles) ||
    v.profiles.length !== 2 ||
    !v.profiles.every(
      (p) =>
        p &&
        Object.keys(p).length === 4 &&
        Object.keys(p).every((k) => ["z0", "z1", "g0", "g1"].includes(k)) &&
        finite(p.z0, -200, 500) &&
        finite(p.z1, -200, 500) &&
        finite(p.g0, -0.3, 0.3) &&
        finite(p.g1, -0.3, 0.3),
    )
  )
    bad("vertical");
  const pt = (p) =>
    Array.isArray(p) &&
    p.length === 2 &&
    p.every((v) => finite(v, -2000, 2000));
  if (
    !Array.isArray(x.curves) ||
    x.curves.length !== 2 ||
    !x.curves.every((c) => Array.isArray(c) && c.length === 4 && c.every(pt))
  )
    bad("curves");
  else
    x.curves.forEach((c, i) => {
      if (!regular(c) || selfCross(sample(c).nodes.map((n) => n.p)))
        errors.push(fail("UNSUPPORTED", `curves[${i}]`));
    });
  const ring = (r) =>
    Array.isArray(r) &&
    r.length >= 4 &&
    r.length <= 40 &&
    r.every(pt) &&
    norm(sub(r[0], r.at(-1))) < 1e-9 &&
    !selfCross(r) &&
    area([[r]]) > 1 &&
    r.slice(1).every((p, i) => norm(sub(p, r[i])) > 1e-7);
  if (!ring(x.site)) bad("site");
  if (
    !Array.isArray(x.exclusions) ||
    x.exclusions.length > 10 ||
    !x.exclusions.every(ring)
  )
    bad("exclusions");
  for (const [key, min, max] of [
    ["anchor", 10, 300],
    ["handle", 5, 250],
    ["laneWidth", 2, 5],
    ["roadWidth", 24, 60],
    ["reserve", 0, 20],
    ["areaCap", 1, 16000000],
    ["heightCap", 0, 100],
    ["layerCap", 1, 10],
  ])
    if (!finite(x[key], min, max)) bad(key);
  if (!Number.isInteger(x.layerCap)) bad("layerCap");
  if (![128, 256].includes(x.sampling)) bad("sampling");
  if (x.crossing !== null && (!Number.isInteger(x.crossing) || x.crossing < 0))
    bad("crossing");
  if (!["urban", "suburban", "custom"].includes(x.preference))
    bad("preference");
  if (
    !Array.isArray(x.required) ||
    new Set(x.required).size !== x.required?.length ||
    !x.required.every((m) => FULL.includes(m)) ||
    !PORTS.every((p) => x.required.includes(`${p}>${p}`))
  )
    bad("required");
  if (!errors.length) {
    const outer = [[x.site]];
    if (x.exclusions.some((r) => area(difference([[r]], outer)) > 1e-7))
      bad("exclusions.outside");
    if (area(difference(outer, union(x.exclusions.map((r) => [r])))) <= 1)
      bad("site.usable");
  }
  return errors;
}
export function solve(x) {
  const errors = validate(x);
  const base = {
    version: VERSION,
    status: "INPUT_INVALID",
    diagnostics: errors,
    candidates: [],
    crossings: [],
    unknown: ["structures", "ground", "vertical", "game", "traffic"],
    fullBaseline: FULL,
  };
  if (errors.length) {
    base.status = errors[0].code;
    return base;
  }
  const curves = x.curves.map((c) => sample(c, x.sampling)),
    hits = crossings(...curves);
  base.crossings = hits;
  base.curves = curves;
  base.vertical = verticalCandidate(x, curves, []);
  base.preview = connectionPreview(x, base, null);
  base.mode = x.required.length === FULL.length ? "full" : "partial";
  base.lanes = laneMapping();
  base.excluded = FULL.filter((m) => !x.required.includes(m));
  if (!hits.length)
    return {
      ...base,
      status: "NO_CANDIDATE_FOUND",
      diagnostics: [fail("NO_CANDIDATE_FOUND", "curves")],
    };
  if (x.crossing === null || !hits[x.crossing])
    return {
      ...base,
      status: "INPUT_INVALID",
      diagnostics: [fail("SELECT_CROSSING", "crossing")],
    };
  const hit = hits[x.crossing];
  if (hit.angle < 0.15)
    return {
      ...base,
      status: "UNSUPPORTED",
      diagnostics: [fail("NEAR_TANGENT", "crossing", { point: hit.p })],
    };
  const site = difference([[x.site]], union(x.exclusions.map((r) => [r])));
  const oldP = union(curves.map((c) => ribbon(c, x.roadWidth))),
    oldR = union(curves.map((c) => ribbon(c, x.roadWidth + 2 * x.reserve)));
  for (const factor of [0.8, 1, 1.2]) {
    const distance = x.anchor * factor,
      handle = x.handle * factor,
      id = `d${factor}`;
    const diag = [];
    const routes = [];
    if (
      curves.some(
        (c, i) => hit.s[i] - distance < 0 || hit.s[i] + distance > c.length,
      )
    ) {
      base.candidates.push({
        id,
        status: "CONSTRAINT_VIOLATION",
        diagnostics: [fail("ANCHOR_RANGE", "anchor", { point: hit.p })],
        vertical: {
          status: "NOT_RUN",
          mainlines: [],
          ramps: [],
          diagnostics: [],
          clearance: "UNKNOWN",
        },
        splitStatus: "NOT_RUN",
        splitDiagnostics: [],
      });
      continue;
    }
    const anchor = (port, incoming, offset) => {
      const i = port[0] === "A" ? 0 : 1,
        dir = port[1] === "+" ? 1 : -1;
      return station(
        curves[i],
        hit.s[i] + (incoming ? -1 : 1) * dir * distance,
        dir,
        offset,
      );
    };
    for (const od of x.required) {
      const [from, to] = od.split(">");
      if (from === to) continue;
      const side = x.trafficSide === "RHT" ? -1 : 1;
      const a = anchor(from, true, (side * x.roadWidth) / 4),
        b = anchor(to, false, (side * x.roadWidth) / 4),
        c = connect(a, b, handle),
        curve = sample(c, x.sampling);
      routes.push({ id: od, from, to, c, curve, a, b, width: 2 * x.laneWidth });
      if (
        !regular(c) ||
        selfCross(curve.nodes.map((n) => n.p)) ||
        !offsetSafe(c, 2 * x.laneWidth + 2 * x.reserve)
      )
        diag.push(fail("OFFSET_RISK", od, { point: curve.nodes[64].p }));
    }
    // Separate local six-lane diverge experiment on A+. Ramp terminal is 45 m outward; not an OD route to B.
    const side = x.trafficSide === "RHT" ? -1 : 1;
    const lanePaths = laneMapping().map((m, i) => {
      const a = anchor("A+", true, side * (i + 0.5) * x.laneWidth);
      const b = anchor(
        "A+",
        false,
        side * ((i + 0.5) * x.laneWidth + (i < 4 ? 0 : 45)),
      );
      if (i >= 4)
        b.tangent = unit(add(b.tangent, mul(normal(b.tangent), side * 0.7)));
      const c = connect(a, b, handle);
      return { ...m, c, a, b, curve: sample(c, x.sampling) };
    });
    const splitConflicts = [];
    for (let i = 0; i < 6; i++)
      for (let j = i + 1; j < 6; j++)
        if (crossings(lanePaths[i].curve, lanePaths[j].curve).length)
          splitConflicts.push(`${lanePaths[i].from}/${lanePaths[j].from}`);
    const splitDiagnostics = splitConflicts.length
      ? [
          fail("LANE_CROSSING", "split", {
            pairs: splitConflicts,
            point: hit.p,
          }),
        ]
      : [];
    if (curves.some((c) => !offsetSafe(c.c, x.roadWidth + 2 * x.reserve)))
      diag.push(fail("OFFSET_RISK", "curves", { point: hit.p }));
    const projection = union([
      oldP,
      ...routes.map((r) => ribbon(r.curve, r.width)),
    ]);
    const reserved = union([
      oldR,
      ...routes.map((r) => ribbon(r.curve, r.width + 2 * x.reserve)),
    ]);
    const land = metrics(oldR, reserved, site);
    if (land.overflow > 1e-6)
      diag.push(
        fail("OVERFLOW", "site", {
          measured: land.overflow,
          limit: 0,
          point: difference(reserved, site)[0]?.[0]?.[0],
        }),
      );
    if (land.total > x.areaCap)
      diag.push(
        fail("AREA_CAP", "areaCap", {
          measured: land.total,
          limit: x.areaCap,
          point: hit.p,
        }),
      );
    const graph = x.required.map((od) => {
      const [from, to] = od.split(">");
      return { from: `${from}:in`, to: `${to}:out`, route: od };
    });
    const vertical = verticalCandidate(x, curves, routes);
    diag.push(...vertical.diagnostics);
    base.candidates.push({
      id,
      vertical,
      status: diag.length ? "CONSTRAINT_VIOLATION" : "UNKNOWN",
      diagnostics: diag,
      splitDiagnostics,
      splitStatus: splitDiagnostics.length ? "CONSTRAINT_VIOLATION" : "UNKNOWN",
      routes,
      lanePaths,
      graph,
      land,
      projection,
      reserved,
      projectionArea: area(projection),
      distance,
      handle,
      length: routes.reduce((s, r) => s + r.curve.length, 0),
      od: {
        required: x.required,
        reachable: x.required.filter((m) => {
          const [a, b] = m.split(">");
          return reachable(graph, `${a}:in`, `${b}:out`);
        }),
        forbiddenReachable: FULL.filter((m) => !x.required.includes(m)).filter(
          (m) => {
            const [a, b] = m.split(">");
            return reachable(graph, `${a}:in`, `${b}:out`);
          },
        ),
      },
      positionBound: Math.max(
        ...curves.map((c) => c.positionBound),
        ...routes.map((r) => r.curve.positionBound),
      ),
      heightCap: x.heightCap,
      layerCap: x.layerCap,
    });
  }
  for (const candidate of base.candidates)
    candidate.preview = connectionPreview(x, base, candidate);
  // Rank ONLY candidates without known planar violations; never claim feasible 3D.
  const valid = base.candidates.filter((c) => c.status === "UNKNOWN");
  valid.sort((a, b) =>
    x.preference === "suburban"
      ? b.distance - a.distance
      : a.land.total - b.land.total,
  );
  return {
    ...base,
    status: valid.length ? "UNKNOWN" : "NO_CANDIDATE_FOUND",
    diagnostics: [],
    recommended: valid[0]?.id,
    site,
  };
}
export function reachable(edges, start, end) {
  const seen = new Set([start]),
    q = [start];
  while (q.length) {
    const v = q.shift();
    if (v === end) return true;
    for (const e of edges)
      if (e.from === v && !seen.has(e.to)) {
        seen.add(e.to);
        q.push(e.to);
      }
  }
  return false;
}
