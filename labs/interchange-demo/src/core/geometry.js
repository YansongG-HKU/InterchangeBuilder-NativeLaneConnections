// Pure metre-based geometry; no browser/game state.
export const add = (a, b) => [a[0] + b[0], a[1] + b[1]];
export const sub = (a, b) => [a[0] - b[0], a[1] - b[1]];
export const mul = (a, k) => a.map((v) => v * k);
export const dot = (a, b) => a[0] * b[0] + a[1] * b[1];
export const cross = (a, b) => a[0] * b[1] - a[1] * b[0];
export const norm = (a) => Math.hypot(...a);
export const unit = (a) => mul(a, 1 / norm(a));
export const normal = (a) => [-a[1], a[0]];
export function point(c, t) {
  const u = 1 - t;
  return [0, 1].map(
    (k) =>
      u ** 3 * c[0][k] +
      3 * u * u * t * c[1][k] +
      3 * u * t * t * c[2][k] +
      t ** 3 * c[3][k],
  );
}
export function derivative(c, t) {
  return [0, 1].map(
    (k) =>
      3 * (1 - t) ** 2 * (c[1][k] - c[0][k]) +
      6 * (1 - t) * t * (c[2][k] - c[1][k]) +
      3 * t * t * (c[3][k] - c[2][k]),
  );
}
export function second(c, t) {
  return [0, 1].map(
    (k) =>
      6 *
      ((1 - t) * (c[2][k] - 2 * c[1][k] + c[0][k]) +
        t * (c[3][k] - 2 * c[2][k] + c[1][k])),
  );
}
export function sample(c, n = 128) {
  let s = 0;
  const nodes = Array.from({ length: n + 1 }, (_, i) => {
    const t = i / n,
      p = point(c, t);
    if (i) s += norm(sub(p, point(c, (i - 1) / n)));
    return { p, t, s };
  });
  return {
    c,
    n,
    nodes,
    length: s,
    positionBound:
      Math.max(norm(second(c, 0)), norm(second(c, 1))) / (8 * n * n),
  };
}
export function station(curve, s, dir = 1, offset = 0) {
  if (s < 0 || s > curve.length) throw Error("station");
  let i = curve.nodes.findIndex((p) => p.s >= s);
  i = Math.max(1, i);
  const a = curve.nodes[i - 1],
    b = curve.nodes[i],
    t = a.t + ((b.t - a.t) * (s - a.s)) / (b.s - a.s);
  const tangent = mul(unit(derivative(curve.c, t)), dir);
  return {
    s,
    t,
    tangent,
    p: add(point(curve.c, t), mul(normal(tangent), offset)),
  };
}
export function intersection(a, b, c, d) {
  const r = sub(b, a),
    q = sub(d, c),
    den = cross(r, q);
  if (Math.abs(den) < 1e-10) return null;
  const u = cross(sub(c, a), q) / den,
    v = cross(sub(c, a), r) / den;
  if (u < -1e-9 || u > 1 + 1e-9 || v < -1e-9 || v > 1 + 1e-9) return null;
  return { p: add(a, mul(r, u)), u, v };
}
export function crossings(a, b) {
  const result = [];
  for (let i = 1; i < a.nodes.length; i++)
    for (let j = 1; j < b.nodes.length; j++) {
      const x = intersection(
        a.nodes[i - 1].p,
        a.nodes[i].p,
        b.nodes[j - 1].p,
        b.nodes[j].p,
      );
      if (x && !result.some((r) => norm(sub(r.p, x.p)) < 0.5)) {
        const sa = a.nodes[i - 1].s + x.u * (a.nodes[i].s - a.nodes[i - 1].s),
          sb = b.nodes[j - 1].s + x.v * (b.nodes[j].s - b.nodes[j - 1].s);
        const angle = Math.abs(
          cross(station(a, sa).tangent, station(b, sb).tangent),
        );
        result.push({ p: x.p, s: [sa, sb], angle });
      }
    }
  return result.sort((x, y) => x.s[0] - y.s[0]);
}
export function connect(a, b, handle) {
  return [
    a.p,
    add(a.p, mul(a.tangent, handle)),
    sub(b.p, mul(b.tangent, handle)),
    b.p,
  ];
}
export function selfCross(points) {
  for (let i = 1; i < points.length; i++)
    for (let j = i + 2; j < points.length; j++) {
      if (
        i === 1 &&
        j === points.length - 1 &&
        norm(sub(points[0], points.at(-1))) < 1e-9
      )
        continue;
      if (intersection(points[i - 1], points[i], points[j - 1], points[j]))
        return true;
    }
  return false;
}
export function regular(c) {
  // Detect exact stationary parameters via both coordinate derivative roots.
  const ts = [0, 1];
  for (let k = 0; k < 2; k++) {
    const a = 3 * (-c[0][k] + 3 * c[1][k] - 3 * c[2][k] + c[3][k]),
      b = 6 * (c[0][k] - 2 * c[1][k] + c[2][k]),
      d = 3 * (c[1][k] - c[0][k]);
    if (Math.abs(a) < 1e-12) {
      if (Math.abs(b) > 1e-12) ts.push(-d / b);
    } else {
      const disc = b * b - 4 * a * d;
      if (disc >= 0)
        ts.push(
          (-b + Math.sqrt(disc)) / (2 * a),
          (-b - Math.sqrt(disc)) / (2 * a),
        );
    }
  }
  return !ts
    .filter((t) => t >= 0 && t <= 1)
    .some((t) => norm(derivative(c, t)) < 1e-7);
}
export function offsetSafe(c, width) {
  return Array.from({ length: 513 }, (_, i) => i / 512).every((t) => {
    const d = derivative(c, t);
    return (
      ((Math.abs(cross(d, second(c, t))) / norm(d) ** 3) * width) / 2 < 0.8
    );
  });
}
