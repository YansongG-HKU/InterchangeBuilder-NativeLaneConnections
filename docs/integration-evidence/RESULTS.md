# Nested lab verification / 子目录验证

Executed 2026-10-26 UTC in `/workspace/interchange-integration/labs/interchange-demo` on target main parent `9f083061cdcf2bb08ed03c0527ebbd6f591c1179` plus the proposed import. Runtime inputs/code match the source SHA-256 manifest exactly; five documentation-only overlays are separately listed. Node v24.19.0, npm 11.9.0; `/usr/bin/chromium`, actual Vite server serving this nested directory at loopback4173 (previous source server stopped).

| Action | Actual result |
|---|---|
| npm ci --ignore-scripts | 22 packages installed; no new dependency versions |
| npm test | 37 core tests passed; [output](unit-tests.txt) |
| npm run test:browser | 81 actual browser assertions passed; [record](browser.json) |
| npm run test:3d | 37 actual WebGL browser assertions passed; [record](browser3d.json) |
| node tests/diagnostic-focus.js | 13 actual zh/en focus assertions passed; [record](diagnostic-focus.json) |
| npm run evidence | Eight fixed input/expected/actual cases passed; [record](computation.json) |
| npm run build | Passed, existing >500kB bundle advisory retained; [output](build.txt) |
| Import hash/link verification | 87 tracked files match source or the five explicit Markdown overlays; every source/test/example/evidence hash unchanged |
| C# runtime/build/smoke | NOT_RUN: no game assemblies/base package |
| Local C# Core tests | NOT_RUN: no .NET SDK |
| New left-turn algorithm/game/P0 recovery | NOT_RUN; approved subsequent stage, not implemented here |

Historical `labs/interchange-demo/evidence` was copied aside before tests and restored byte-for-byte afterward. New structured outputs above are integration results; they do not replace historical source evidence. Actual new PNG hash values are in [rendered-screenshot-hashes](rendered-screenshot-hashes.json); historical PNG files remain in the lab. All browser outputs report zero unhandled errors. No public preview URL or game compatibility claim.

The existing root C#-only PR workflow is unchanged; automatic GitHub runs, if any, must be reported independently after push. It does not cover the lab. No workflow manually triggered by this verification.

Resumption checks: [original Mod blob hashes](mod-protection.json) verify 27 unchanged baseline files and three allowed root-document updates; all 87 imported hashes/recorded overlays rechecked. Project/build/CI static inspection found no lab inclusion in runtime compilation or binary payload. Existing PowerShell CRLF checkout is handled by comparing Git blob bytes. No tested source changed during resumption, so suites above were not unnecessarily repeated. Static protection is not game testing.
