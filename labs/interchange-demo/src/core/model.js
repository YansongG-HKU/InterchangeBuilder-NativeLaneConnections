import { flatVertical } from "./vertical.js";
export const VERSION = "offline-0.2.0";
export const PORTS = ["A+", "A-", "B+", "B-"];
// End ports are directional: each incoming port has one through exit and two cross-corridor exits.
export const FULL = PORTS.flatMap((from) =>
  PORTS.filter((to) => to === from || to[0] !== from[0]).map(
    (to) => `${from}>${to}`,
  ),
);
export const clone = (x) => JSON.parse(JSON.stringify(x));
export const defaultCase = () => ({
  schema: 2,
  vertical: flatVertical(),
  solver: VERSION,
  name: "curved-skew",
  trafficSide: "RHT",
  roadType: "motor",
  laneMode: "one-way-6-to-4+2",
  curves: [
    [
      [-260, -45],
      [-100, -110],
      [100, 90],
      [260, 35],
    ],
    [
      [-160, -250],
      [-70, -85],
      [45, 80],
      [175, 250],
    ],
  ],
  site: [
    [-320, -320],
    [320, -320],
    [320, 320],
    [-320, 320],
    [-320, -320],
  ],
  exclusions: [],
  crossing: 0,
  anchor: 80,
  handle: 65,
  laneWidth: 3.5,
  roadWidth: 28,
  reserve: 3,
  areaCap: 100000,
  heightCap: 15,
  layerCap: 3,
  preference: "urban",
  required: [...FULL],
  sampling: 128,
});
export function laneMapping() {
  return Array.from({ length: 6 }, (_, i) => ({
    from: `L${i + 1}`,
    to: i < 4 ? `M${i + 1}` : `R${i - 3}`,
    direction: 1,
    type: "motor",
  }));
}
