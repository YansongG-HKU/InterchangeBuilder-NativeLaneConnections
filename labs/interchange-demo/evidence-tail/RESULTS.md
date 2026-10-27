# 显式直尾 V1 实测 / Explicit-tail V1 results

本地实验分支 `experiment/explicit-tail-v1`，基于PR #9 `a13fa7919abcd4689bff035f65bd6d618978cc2b`，2026-10-27 UTC。只在云端修改/验证，不推送或变更PR。 / Local cloud implementation, no remote mutation.

输入/预期/故障/实测和本次源码SHA-256见 [computation.json](computation.json)，控制点参考与故障名称另保存在`tests/tail-fixtures`。报告中的sourceParent是基线提交；最终本地提交由交付消息给出，源码manifest标识运行内容。不是把基线SHA冒作新代码版本。 / Source hashes identify tested contents; sourceParent is only the baseline.

| Command (from lab directory) | Actual result |
|---|---|
| `npm test` | 58 tests passed: 37 preserved baseline + 21 new reference/property/boundary/fault tests. [Output](unit-tests.txt) |
| `npm run evidence:tail` | Five explicit reference/fault fixtures covering four classes; all expected checks passed. [Records](computation.json) |
| `npm run test:tail-browser` | 25 actual Chromium assertions passed. [Output](browser-run.txt), [record](browser.json) |
| `npm run test:browser` | 81 baseline assertions passed. [Output](baseline-browser-run.txt), [record](baseline-browser.json) |
| `npm run test:3d` | 37 baseline WebGL assertions passed. [Output](baseline-three-run.txt), [record](baseline-browser3d.json) |
| `node tests/diagnostic-focus.js` | 13 baseline focus assertions passed. [Output](baseline-focus-run.txt), [record](baseline-diagnostic-focus.json) |
| `npm run build` | Both index.html and curve-lab.html built; existing large shared Three.js chunk warning retained. [Output](build.txt) |
| `python3 scripts/check-tail-isolation.py` | 27 protected original Mod files match stable main9f083061 Git blobs; original solver/scene/adapter/UI module and historical evidence unchanged. [Record](mod-protection.json) |

Browser checks cover actual WebGL output, generated versus reference status, explicit tail edits, normal/no-collision/normal with identical facts and newer revision, immediate stale-input export disable, exact export/reload independent recheck, G2 conflict with cleared mesh, unsupported arch generation and separately selected full-width reference, travel-left offset/node identity, invalid input/import and 390px layout. The fixed camera screenshots are [middle tail zh](middle-three-zh.png), [bridge reference en](bridge-reference-en.png) and [mobile en](mobile-en.png). They are real browser captures, not generated images. Source code/tests and generated evidence were reviewed; current geometry is not a realistic game-road renderer.

Visual inspection caught and fixed endpoint-only rendering of straight-plan Hermite elevation: display now samples the actual profile at <=2m chord stations, with an analytic 50m/1.5625m regression. Nearby labels use a separate context-ID list rather than overlapping in the scene. No vertical exaggeration. Plan stationing uncertainty remains disclosed in the contract.

Reference bridge maximum grade7.5%, full overlap minimum deck-bottom difference8.5m with known1.5m thickness; changing thickness or low interval edges fails independently. This is only orthogonal/flat full-width reference validation; generating the equal-height arch remains UNSUPPORTED. General curved 3D clearance, terrain, supports, variable-width seams, complete lane graph and game conversion remain UNKNOWN/NOT_RUN.

Historical lab evidence was copied aside before baseline browser tests and restored byte-for-byte. New outputs are separate here. Existing runtime/project/build/CI blobs are unchanged; static inspection confirms explicit C# project/vendor/UI paths do not sweep in lab JS/HTML. This is not MSBuild or game execution. Local C# tests/build, game/Anarchy compatibility, save/reload, ownership recovery, traffic and P0 pier failure reproduction are NOT_RUN. CI for this unpushed branch is NOT_RUN. Revert the local experiment commit for rollback.
