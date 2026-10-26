# 离线验证记录 / Offline validation record

See [runtime and support domain](OFFLINE_DEMO.md). Requirements base: `bfccebe7467b2a8935306910b7763957ec98c207`. Tests execute the source identified by SHA-256 in [source manifest](../evidence/source-manifest.json); the browser record's `sourceCommit` identifies the checkout parent before the current tested edits, **not an assertion that dirty implementation existed in that commit**. The final Git head is reported in the PR and delivery message. / 精确源文件哈希绑定被测实现，避免自引用提交哈希。

## Actual evidence / 实测证据

- [Unit/property/regression output](../evidence/unit-tests.txt): 37 assertions grouped as Node tests, including degeneracy, limits, fixed OD, G1, unions/differences, area/overflow, rotation, reload and unavailable adapter.
- [Real browser record](../evidence/browser.json): 81 Chromium interaction assertions, Chinese/English, geometry-invariant locale switch, fractional input, undo, preset, JSON download/import, second intersection selection, near tangent, area cap, overflow, error location, malformed/null JSON, empty numeric input, recovery and narrow layout.
- [Fixture computations](../evidence/computation.json): complete inputs, expected and observed statuses, candidate diagnostics, OD, area, timings and 128/256 refinement differences. These timings are this machine's observations, not a responsiveness guarantee.
- [3D browser record](../evidence/browser3d.json): real WebGL2 rendered pixels, mouse orbit/pan/wheel, camera reset/top, complete OD highlight, full/partial redraw, height/grade inputs, selected profile, schema 2 reload, explicit legacy migration, invalid-input scene clearing, 390px controls and renderer fallback.
- [Build output](../evidence/build.txt): production build; examples copied into distribution.
- Screenshots: [中文](../evidence/browser-zh.png), [English overflow](../evidence/browser-overflow-en.png), [narrow English](../evidence/browser-mobile-en.png), [窄屏中文](../evidence/browser-mobile-zh.png).
- [Export/reload fixture](../evidence/exported-case.json).

Node 24.19.0, npm 11.9.0. Exact package pins and integrity in package-lock.json; browser version in browser.json. No game build/Mod version exists for this experiment. / 未安装游戏或 Mod，不虚构版本。

Initial browser run exposed an asynchronous fixture-loading race in the **test wait condition**; corrected to wait for the selected model and completed solve. Final assertions exercise real DOM events. Initial full-geometry checks found tight turn offsets and retain those failures as expected fixture outcomes. Six-lane local fork is independently visualized and checked, not conflated with the OD experiment or its area. / 保留失败含义，不以成功断言掩盖候选不可行。

## Requirement coverage / 需求覆盖

| Requirement cases | Offline evidence | Remaining gap / 未完成 |
|---|---|---|
| A01/A02/A04/A05/A32 | six mappings, explicit single-direction schema, local tangents, skew/multiple/near tangent | full lane sweeps, actual game lanes |
| A07/A08 | fixed 12 OD; partial exclusions; RHT/LHT, rotation property | game read-back; full mirrored gameplay |
| A11/A13/A23/A40 | local JSON reload, locale invariant, numeric recovery, exact source hashes | game saves, unit switching, game updates |
| A18/A19 | explicit limited model and versions, sampled curve bound | engine discretization and certified geometry |
| A33/A34/A36/A37/A39 | preference independence; concave polygon/holes; union/difference and overflow | complete physical/ground reservation and complex terrain |
| A35/A38 | duplicated polygon counted once; UNKNOWN height/levels; bounded search failure | actual local deck count/height/clearance solver |
| All game-dependent A cases | NOT_RUN | CS2 APIs, saves, mod lifecycle, vehicles and traffic |

These are **partial offline subcases**, not acceptance of the full A01–A40 suite. All game compatibility rows remain NOT_TESTED. / 仅离线子项通过，不把 40 个需求全标 PASS。

CI: no workflow has been added or executed by this change. Local `npm test`, browser and build success is **not CI success**. / 没有把本地测试冒称 CI 通过。

## Historical schema 1 review regression / schema 1 历史审查回归

Added own-key schema regression (`constructor`, `toString`, `__proto__`, `hasOwnProperty`, ordinary extra key), plus real JSON file import rejection and unchanged previous input/result in both locales. This is a validation bug fix, not evidence of code execution/prototype pollution.

Desktop and 390px zh/en checks assert six readable, nonoverlapping labels (font >=14px), select each of the six actual connectors, and verify selection preserves the complete computation. Screenshots were refreshed and visually inspected.

Playwright intercepts actual fixture fetches and deliberately releases older responses last. It separately tests late fetch after reset, numeric edit and language change. A controlled `File.text()` promise exercised by the actual file input resolves after reset; it cannot replace the reset model. Tests additionally assert OD UNKNOWN and independent fork CONSTRAINT_VIOLATION coexist without hiding the lane warning.

验证：22 项核心测试、78 项真实浏览器断言、7 案例预期/实测检查、生产构建；完整游戏与 CI 仍 NOT_RUN。输入、支持域和计算几何不扩大。

Historical schema 1 boundary regression: anchor=300m makes candidate d1 fail ANCHOR_RANGE; its lane experiment is NOT_RUN, has no connector geometry, and displays 未运行 / NOT_RUN in both languages. At that schema 1 revision: 23 core tests, 81 browser assertions; seven fixtures and build pass. / 前置条件失败不是 UNKNOWN 检查完成。

## Schema 2 / Three-dimensional V1 evidence

Current test suite: 37 core tests (23 preserved planar and 14 new vertical/preview), 81 preserved browser assertions, plus 37 separately recorded 3D browser assertions. Eight fixture records include the original seven planar cases migrated explicitly to flat schema 2 data plus `elevated-full`. The Hermite 0→12m/200m benchmark records peak 9% at 100m, and equal-height overshoot records min/max rather than clamping.

Actual rendered screenshots: [orbit and full connection zh](../evidence/three-orbit-zh.png), [selected profile en](../evidence/three-profile-en.png), [height-edited oblique scene](../evidence/three-height-en.png), [invalid red candidates/top en](../evidence/three-invalid-top-en.png), [mobile 3D en](../evidence/three-mobile-en.png), [initial elevated scene](../evidence/three-initial.png). These are browser captures, not generated illustrations. Camera gestures and rendered-pixel changes are asserted; no real-device touch or game equivalence is claimed.

The existing browser regression now explicitly selects the 2D view and targets its JSON disclosure by ID, since the new profile panel adds a second disclosure. Early 3D test correction: road-kind metadata was added to the read-only scene probe so invalid-ramp assertions can distinguish ramps from mainlines; visual geometry already retained its invalid flag. Final pass counts are recorded by the test scripts, not inferred from screenshots.

Height/terrain caps, structural clearance, local stacked decks, full structure/ground area and all CS2/Mod checks remain UNKNOWN/NOT_RUN. Mathematical profile checks do not change that boundary. No CI workflow introduced.

## Diagnostic navigation regression (2026-10-26 UTC)

`node tests/diagnostic-focus.js` runs 13 real Chromium assertions on elevated-full with B.z0=80m, candidate d1: click a located ramp GRADE_LIMIT in Chinese and English, verify the camera actually moves and target XY equals the diagnostic; switch to plan, verify the existing SVG marker matches the same point and camera remains unchanged. Inputs, selected diagnostic, expected/actual assertions, browser and parent version are in [diagnostic-focus.json](../evidence/diagnostic-focus.json). Original 37 core, 81 planar browser, 37 3D browser checks, eight fixtures and build were rerun; CI/game remain NOT_RUN. Source manifest includes this targeted regression. No new screenshot is needed for the behavioral fix; actual camera/marker coordinates are recorded.

## 历史证据边界 / Historical evidence boundary

上述测试属于现有Demo。新 [深度案例D0–D4](DEEP_CASE_PLAN.md) 全部NOT_RUN；本轮仅文档链接/一致性检查，不重跑或改写历史数值，不把旧断言计作独立真值验证。 / Existing results remain historical demo evidence; documentation-only changes do not execute the new deep-case gates.
