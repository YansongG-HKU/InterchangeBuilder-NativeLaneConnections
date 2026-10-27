import fs from "node:fs";
import crypto from "node:crypto";
import { execFileSync } from "node:child_process";
import { inspect } from "../src/experiments/tail/checker.js";
import { evaluateCase } from "../src/experiments/tail/run.js";
const rows = [];
for (const name of fs.readdirSync("tests/tail-fixtures").sort()) {
  const fixture = JSON.parse(fs.readFileSync("tests/tail-fixtures/" + name)),
    { input, reference } = fixture;
  const normal = inspect(input, reference),
    badInput = structuredClone(input),
    badOutput = structuredClone(reference);
  if (fixture.fault === "short-tail")
    badOutput.segments.at(-1).controls[0][0] += 10;
  if (fixture.fault === "reverse-tail")
    badOutput.segments.at(-1).controls.reverse();
  if (fixture.fault === "gap") badOutput.segments.at(-1).controls[0][0] += 2;
  if (fixture.fault === "g2-conflict") badInput.continuity = "G2";
  if (fixture.fault === "thick-deck") badOutput.thickness = 7;
  const fault = inspect(badInput, badOutput);
  if (
    normal.status !== fixture.expected ||
    !fault.issues.some((i) => i.code === fixture.expectedFault)
  )
    throw Error(name);
  rows.push({
    fixture,
    normal,
    fault: { input: badInput, output: badOutput, result: fault },
    generated: evaluateCase(input),
  });
}
const paths = execFileSync(
  "git",
  ["ls-files", "--cached", "--others", "--exclude-standard"],
  { encoding: "utf8" },
)
  .trim()
  .split("\n")
  .filter(
    (p) =>
      /^(src|tests|scripts)\//.test(p) ||
      [
        "package.json",
        "package-lock.json",
        "curve-lab.html",
        "index.html",
        "vite.config.js",
      ].includes(p),
  );
const hashes = Object.fromEntries(
  paths
    .sort()
    .map((p) => [
      p,
      crypto.createHash("sha256").update(fs.readFileSync(p)).digest("hex"),
    ]),
);
fs.writeFileSync(
  "evidence-tail/computation.json",
  JSON.stringify(
    {
      version: "tail-v1",
      sourceParent: execFileSync("git", ["rev-parse", "HEAD"], {
        encoding: "utf8",
      }).trim(),
      node: process.version,
      date: new Date().toISOString(),
      hashes,
      rows,
    },
    null,
    2,
  ) + "\n",
);
console.log(
  `${rows.length} independent reference/fault fixtures passed; source hashes recorded`,
);
