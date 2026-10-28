# Independent Core planar validation / 独立 Core 平面验收

The Lab serves algorithm validation → Mod migration. This slice completes a bounded planar generator-plus-independent-inspector kernel, not an engine adapter. Existing runtime has no calls to either experimental class. Generation is disabled by default; both generation and inspection always report CanApplyToGame=false.

## Actual implementation / 实现

`CurveGeometryVerifier.Verify(request, document)` reads immutable actual line/Bezier control points (degree1–5), not a generator validity flag. `VerifyGenerated(request, result, index)` extracts actual candidate controls and explicit tail and passes them to the same inspector. Captured request binding independently compares IDs, positions, unit travel directions, signed curvature, width, offsets/sides, tail, continuity, revision and mode. Changed intent invalidates old output even if revision is reused. Reconstruction must be inspected again; no engine conversion is implemented or implied.

Independent Bernstein evaluation, derivative controls and endpoint jets check actual offset positions, directed tangents, requested endpoint G2 and G2 piece joins. Explicit trailing forward collinear LINE segments must sum to the requested finite tail. A Bezier endpoint of zero curvature is not a finite line. Nonzero target curvature plus strict G2 and a line tail fails. G1 does not claim endpoint G2. Left/right are travel-frame offset signs, not world axes.

Position, angular and curvature absolute tolerances are 1e-7 (world length units, radians, inverse length respectively); these are experimental numerical tolerances, not game standards. Coordinates/control points are bounded to ±10000, width≤50, offset≤100, requested tail≤500; unit direction tolerance1e-8. At most16 segments, control count2–6, positive finite tolerance≤1, depth0–18. Default adaptive tolerance0.02, depth16 and2048 leaves. Out-of-domain/nonfinite/missing values reject before inspection. Variable width and singular offsets are unsupported, never local success.

Chord/control-polygon lengths bound length; convex control hull distance bounds each curve's distance to its chord. Positive derivative projection establishes leaf regularity; conservative second-derivative/speed bounds gate local offset regularity. Insufficient subdivision or budget yields Unknown. Exact polygonal self-crossings fail. Distant-limb corridor checks use width and leaf-error margins; limbs separated by at most twice the width in lower-bound station distance are outside this nonlocal corridor check. Thus VerifiedLocalPlanar is **not** a proof of a globally embedded swept road surface: nearby offset overlap, external roads/obstacles, named connection exemptions, vertical clearance, terrain, lanes and engine construction are excluded. No collision mode bypasses any implemented check.

Statuses: InvalidInput (including stale binding), Failed (observed bounded-contract violation), Unknown (unsupported or unresolved), VerifiedLocalPlanar (only above local scope). Never interpret failure of one candidate as global infeasibility. Metrics include endpoint errors, actual line-tail length and lower/upper length bounds. No geometric truth is inferred from a displayed preview.

## Evidence / 证据

[Current results](curve-verifier-evidence/RESULTS.md) retain input/expected/actual/source hashes. Analytic straight/control/endpoint references are independent. JS/C# shared vectors are explicitly differential comparisons: 4 base cases ×9 actual-output mutations cover position, seam, reversal, fake/short tail, degeneracy, numeric domain and resolution. Reconstructed output and mode changes are rechecked. Separate stale-request, NaN/Infinity/overflow, immutable-copy and self-crossing injections exercise failures. Existing left/right, side, strict-G2 and analytic endpoint tests remain included.

Independent reviewer inspected actual source and ran the then153-check suite without a false-pass finding. Final additions are39 test-only checks (nonfinite/extreme output and36 mode comparisons); final192-check direct and standard suites pass. This is code review and bounded regression, not a general proof or engineering certification.

## Remaining minimum engine inputs / 下一步最小输入

Need exact base InterchangeBuilder package/version/hash and accessible authorized assembly/source path; matching CS2/Game/Unity/Colossal versions/assemblies; real selected snapshot/port identity and create-curve/piece lifecycle signatures. Current public Harmony method names and7-argument hook do not establish these contracts. No private computer access is needed or attempted. Actual quintic→engine piece conversion, external-road/vertical C# validation, default-off runtime preview binding and game construction/save/reload/ownership recovery follow only with verified contracts. No fake adapter, guessed signature or game-compatible format claim.

2026-10-28 local extension: [curve primitives, bounded station/profile functions and inspected split candidates](CURVE_FUNCTIONS.md). The original inspector scope and game exclusions remain; experimental search is default-disabled. / 已扩充纯数学内核，未启用游戏路径。

2026-10-28 numerical audit: mathematical sufficiency and real-arithmetic enclosure logic are distinct from machine inclusion guarantees. Current double fixed guards are not a per-operation directed-rounding/roundoff proof; `Separated`/certificate means the scoped implementation result, not formally certified machine safety. See [design audit](MOD_DELIVERY_DESIGN.md). Historical results are not rewritten; automatic game permission remains false.
