# V2 executed results / 第二切片实测

Baseline commit7c140927344d705afd3ce21e8bf6124c419e4a91, local branch experiment/explicit-tail-v1. Implementation version tail-v2, schema1 input/output retained. Final commit is supplied in delivery; computation.json contains exact source hashes and preceding HEAD, not a claim that uncommitted source existed in that HEAD. No push/PR/merge/release.

| Command from labs/interchange-demo | Actual result |
|---|---|
| npm test |68 core tests passed:59 prior+9 phase2|
| npm run test:browser |81 baseline browser assertions passed|
| npm run test:3d |37 baseline WebGL assertions passed|
| node tests/diagnostic-focus.js |13 focus assertions passed|
| npm run test:tail-browser |29 V1/review browser assertions passed|
| npm run test:tail-phase2-browser |35 V2 actual browser assertions passed|
| EVIDENCE_DIR=evidence-tail-v2 npm run evidence:tail |7 independent reference/fault fixtures plus actual generated candidates recorded|
| npm run build |Both entries built; shared Three.js chunk advisory only|
| EVIDENCE_DIR=evidence-tail-v2 python3 scripts/check-tail-isolation.py |27 protected original Mod blobs unchanged; original solver/view/UI/adapter/historical evidence unchanged|

Logs and JSON in this directory preserve inputs/expected/actual; computation.json includes source SHA256 for all source/tests/scripts/config. All listed tests were rerun on final source, including all baseline suites. Old evidence and evidence-tail restored byte-for-byte after runs; fresh baseline JSON copied here. Real full-page screenshots: automatic-overpass-zh.png, curved-context-en.png, automatic-mobile.png. Actual generated profile is distinct from selecting handwritten reference. Geometry uses no vertical exaggeration. Plan view now fits all context bounds, and screenshots reset current camera to fit.

Independent read-only code review found width binding omitted:4m main-road with8m port could pass. Fixed PORT_CONTEXT_WIDTH_UNSUPPORTED and lateral extent UNKNOWN; reproduced blocked in both modes and added regression. Other reviewed bounds: named-pair exemptions, summed curve approximation errors, full-overlap clearance extrema and1e-7 uncertainty, multi-crossing plateau, fixed-route-only infeasibility statement, async import token and no old permission resurrection. Reviewer reran new9-test suite and the original width repro; no editing or remote action.

Failures retained as narrative: initial browser connection refused because old dev server had exited; started Vite in same cloud workspace. Initial second-crossing test at x260 correctly exhausted8% grade budget (160m descent gives9.375%); corrected independent feasible input x230 (190m descent gives~7.895%), not the constraint. Width-profile rejection was initially inserted in the input validator with wrong result shape, caught by test and moved to generation UNSUPPORTED. Final failure count0.

Supported additional slice: automatic +X straight-plan rise/platform/fall, flat orthogonal lower roads with known thickness, actual bounded Bezier context corridors and bound ports, repeated mode/import race handling. Missing-thickness legacy bridge fixture remains UNSUPPORTED generation. General oblique/curved 3D crossings, curved over/under routing, varying road width, crossfall, terrain/pier/vehicle envelopes, game lane topology and adapter remain unsupported/UNKNOWN. Nine side combinations are checked, not all accepted: same-width whole-road offset beyond host width is blocked.

Game, Anarchy, C# compilation/tests, save/reload, ownership recovery/P0 pier reproduction and remote CI remain NOT_RUN. Static isolation does not prove game compatibility. No publicly shared preview; localhost4173/curve-lab.html only. Rollback local V2 commit to reviewed V1.
