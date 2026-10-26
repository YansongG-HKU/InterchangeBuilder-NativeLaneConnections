import pc from "polygon-clipping";
import { add, mul, normal, unit, derivative } from "./geometry.js";
export function area(multi) {
  return multi.reduce(
    (total, poly) =>
      total +
      poly.reduce((v, ring, i) => {
        let a = 0;
        for (let j = 0; j < ring.length - 1; j++)
          a += ring[j][0] * ring[j + 1][1] - ring[j + 1][0] * ring[j][1];
        return v + (i ? -1 : 1) * Math.abs(a / 2);
      }, 0),
    0,
  );
}
export const union = (polys) => (polys.length ? pc.union(...polys) : []);
export const difference = (a, b) =>
  a.length ? (b.length ? pc.difference(a, b) : a) : [];
export const intersect = (a, b) =>
  a.length && b.length ? pc.intersection(a, b) : [];
// Flat end caps; union quadrilateral strips. This is a sampled pavement model,
// NOT a complete structure/earthwork envelope or a certified curve offset.
export function ribbon(curve, width) {
  const edges = curve.nodes.map((n) => {
    const v = mul(normal(unit(derivative(curve.c, n.t))), width / 2);
    return [add(n.p, v), add(n.p, mul(v, -1))];
  });
  return union(
    edges
      .slice(1)
      .map((e, i) => [[edges[i][0], e[0], e[1], edges[i][1], edges[i][0]]]),
  );
}
export function metrics(oldSet, newSet, site) {
  const total = area(newSet),
    inside = area(intersect(newSet, site)),
    added = area(difference(newSet, oldSet)),
    removed = area(difference(oldSet, newSet));
  return {
    total,
    site: area(site),
    inside,
    overflow: area(difference(newSet, site)),
    ratio: inside / area(site),
    added,
    removed,
    retained: area(intersect(oldSet, newSet)),
    net: added - removed,
  };
}
