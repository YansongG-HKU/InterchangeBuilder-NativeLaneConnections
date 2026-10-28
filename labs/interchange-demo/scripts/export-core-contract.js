import fs from "node:fs";
import { generate } from "../src/experiments/tail/generator.js";
const names = [
  "middle-tail",
  "same-direction-shift",
  "straight-main-left",
  "curved-main-left",
];
const rows = [];
for (const name of names) {
  const { input } = JSON.parse(
    fs.readFileSync(`tests/tail-fixtures/${name}.json`),
  );
  for (const mirror of [false, true]) {
    const x = structuredClone(input);
    if (mirror) {
      for (const p of [x.start, x.end]) {
        p.position[1] *= -1;
        p.direction[1] *= -1;
        p.curvature *= -1;
        p.offset *= -1;
        p.side =
          p.side === "left" ? "right" : p.side === "right" ? "left" : p.side;
      }
    }
    rows.push({
      name: name + (mirror ? "-mirror" : ""),
      input: x,
      candidates: generate(x).candidates.map((c) => ({
        controls: c.segments[0].controls,
        tail: c.segments[1].controls,
      })),
    });
  }
}
fs.writeFileSync(
  "../../tests/curve-contract-vectors.json",
  JSON.stringify(
    {
      purpose: "JS/C# differential vectors, NOT independent mathematical truth",
      rows,
    },
    null,
    2,
  ) + "\n",
);
