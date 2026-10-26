import { hermite, profileSummary } from "../src/core/vertical.js";
import fs from "node:fs";
import crypto from "node:crypto";
import { execFileSync } from "node:child_process";
import { solve } from "../src/core/solver.js";
import { VERSION } from "../src/core/model.js";
const expected = {
  "curved-skew": "NO_CANDIDATE_FOUND",
  partial: "UNKNOWN",
  multiple: "INPUT_INVALID",
  "near-tangent": "UNSUPPORTED",
  "small-site": "NO_CANDIDATE_FOUND",
  exclusion: "NO_CANDIDATE_FOUND",
  invalid: "UNSUPPORTED",
  "elevated-full": "UNKNOWN",
};
const records = [];
for (const [name, status] of Object.entries(expected)) {
  const input = JSON.parse(fs.readFileSync(`examples/${name}.json`));
  const started = performance.now(),
    r = solve(input),
    elapsed = performance.now() - started;
  records.push({
    name,
    input,
    expected: status,
    actual: r.status,
    pass: status === r.status,
    elapsedMs: elapsed,
    crossings: r.crossings,
    diagnostics: r.diagnostics,
    candidates: r.candidates.map((c) => ({
      id: c.id,
      status: c.status,
      diagnostics: c.diagnostics,
      splitDiagnostics: c.splitDiagnostics,
      splitStatus: c.splitStatus,
      land: c.land,
      od: c.od,
      positionBound: c.positionBound,
      vertical: c.vertical
        ? {
            status: c.vertical.status,
            clearance: c.vertical.clearance,
            mainlines: c.vertical.mainlines.map((m) => ({
              id: m.id,
              profile: m.profile,
            })),
            ramps: c.vertical.ramps.map((r) => ({
              id: r.id,
              a: r.a,
              b: r.b,
              profile: r.profile,
              status: r.status,
            })),
          }
        : null,
      preview: c.preview
        ? {
            roads: c.preview.roads.map((r) => ({
              id: r.id,
              kind: r.kind,
              invalid: r.invalid,
            })),
            odPaths: Object.keys(c.preview.paths),
            crossingDiagnostics: c.preview.crossingDiagnostics,
          }
        : null,
    })),
  });
}
const input = JSON.parse(fs.readFileSync("examples/partial.json"));
const coarse = solve(input);
input.sampling = 256;
const fine = solve(input);
const refinement = coarse.candidates.map((c, i) => ({
  id: c.id,
  samples: [128, 256],
  area128: c.land.total,
  area256: fine.candidates[i].land.total,
  absoluteDifference: Math.abs(c.land.total - fine.candidates[i].land.total),
  meaning: "observed difference, NOT a certified area error bound",
}));
const files = execFileSync(
  "git",
  ["ls-files", "--cached", "--others", "--exclude-standard"],
  { encoding: "utf8" },
)
  .trim()
  .split("\n")
  .filter(
    (p) =>
      /^(src|tests|scripts|examples)\//.test(p) ||
      ["package.json", "package-lock.json", "index.html"].includes(p),
  );
const sha256 = Object.fromEntries(
  files
    .sort()
    .map((p) => [
      p,
      crypto.createHash("sha256").update(fs.readFileSync(p)).digest("hex"),
    ]),
);
fs.writeFileSync(
  "evidence/computation.json",
  JSON.stringify(
    {
      version: VERSION,
      node: process.version,
      date: new Date().toISOString(),
      requirementsHead: "bfccebe7467b2a8935306910b7763957ec98c207",
      records,
      refinement,
      verticalBenchmarks: [
        {
          input: { length: 200, z0: 0, z1: 12, g0: 0, g1: 0 },
          expectedPeak: 0.09,
          actual: profileSummary(hermite(200, { z0: 0, z1: 12, g0: 0, g1: 0 })),
        },
        {
          input: { length: 200, z0: 5, z1: 5, g0: 0.1, g1: -0.1 },
          expectedMaxZ: 10,
          actual: profileSummary(
            hermite(200, { z0: 5, z1: 5, g0: 0.1, g1: -0.1 }),
          ),
        },
      ],
    },
    null,
    2,
  ) + "\n",
);
fs.writeFileSync(
  "evidence/source-manifest.json",
  JSON.stringify({ algorithm: VERSION, sha256 }, null, 2) + "\n",
);
if (records.some((r) => !r.pass)) process.exitCode = 1;
console.log(
  records
    .map(
      (r) =>
        `${r.name}: expected=${r.expected}; actual=${r.actual}; ${r.pass ? "PASS" : "FAIL"}`,
    )
    .join("\n"),
);
console.log("Area refinement", refinement);
