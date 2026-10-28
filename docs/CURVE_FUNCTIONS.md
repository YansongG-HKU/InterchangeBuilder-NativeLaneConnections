# Curve function expansion / 曲线函数扩充

This local slice extends the Mod's pure Core, not the Lab UI or game runtime. Base c8a27019ab6fa39703f38b482795c4e81b32058b. Default generation/search remains disabled; all outputs CanApplyToGame=false. No new dependencies.

## Implemented / 已实现

- `CurveFunctions.Line`: analytic position, unit travel tangent, zero curvature; length through Measure.
- `Arc` / `ArcLength`: analytic circle position, tangent, signed curvature and length, radius(1e-6,1000], signed sweep[-π,π] excluding abs<1e-8. Arc is a mathematical primitive ONLY, not an accepted engine/inspector segment or an arc-to-tail routing candidate. A circle-to-line curvature jump is not G2.
- `Bezier`: degree1–5 jet; degenerate derivative rejects. `Hermite`: quintic matching both endpoint positions/unit tangents/signed curvatures, zero tangential endpoint acceleration, positive prescribed speed. Both same/opposite signed curvature jets are representable.
- `Measure`: chord/control-polygon arc-length interval with recursive per-branch error budget; depth/leaf exhaustion gives unresolved. These are mathematical bounds in floating-point arithmetic, not directed-rounding formal interval arithmetic. Near numerical thresholds retain existing conservative unknown behavior; no universal machine-error proof.
- `LocateStation`: inverse horizontal station parameter bracket using prefix length bounds and a derivative-control speed bound. Returns unresolved if prefix intervals or budget cannot establish the requested spatial error. Never assumes Bezier t=s/L. Rejects invalid station/domain; positive targets inside the uncertain total-length endpoint region are unresolved.
- `CurveVerticalFunctions.Height/Grade/Inspect`: cubic Hermite in horizontal arc station, signed grades, analytic peak-grade bound over a given length interval. Endpoint length values suffice because grade at fixed normalized station is affine in1/L. Unresolved lengths never pass; limit equality margin1e-10 is conservative. No automatic vertical routing, clearance, terrain or structural certification. Callers must supply bounds from the actual planar geometry; this utility alone does not bind a profile to a request.
- `CurveCandidateSearch`: baseline3 quintics +1 explicit line +9 two-piece quintics (waypoint lateral offsets−.2/0/.2 chord, speed scales.8/1/1.2). Exactly13 attempts in supported domain; out-of-domain construction attempts counted separately. Actual start/end/side offsets and finite40m test tails remain fixed. Every resulting piece set passes through the independent verifier; search status remains CandidateUnverified. No silent winner, optimization, placement or infeasibility proof.

The split joins use the chord direction and zero common curvature, thus admit smooth sign-changing S shapes without calling every reversal valid. General endpoint G1 does not promise endpoint G2. Strict target nonzero curvature plus finite line/G2 remains ConstraintConflict. All joins remain checked for G2 by the existing inspector. No external obstacle search or named-road exemption is added; asymmetry/full-width claims remain limited to the existing inspector's explicit local scope, not arbitrary corridor feasibility.

## Independent checks and comparison / 独立验收与比较

[Actual results](curve-functions-evidence/RESULTS.md) include actual control points, endpoint errors, finite tail, length bounds, sampled radius, status/reasons, segment counts and measured time for every candidate. Radius is101-point per-segment diagnostic sampling, NOT a guaranteed minimum-radius constraint. Null means degenerate/straight diagnostic, not certified unlimited radius. Timing is one local run, not a benchmark claim.

Independent references: 3–4–5 line; mirrored quarter-circle20m radius length10π; analytic parabola x=t,y=t² with exact integral length; independently expressed JS polynomial jets/heights/grades; exact de Casteljau split consistency. 200m/12m flat-end profile has9% peak, not its6% average. Translation and90°/37° rotation, mirror, scale, required S curvature signs, extra reversing-loop injection, invalid/overflow/zero inputs and insufficient budgets are checked. JS/C# agreement is differential evidence alongside analytic references, not independent correctness by itself.

Rotation testing exposed old inspector `error==0` handling: tiny rotated collinearity residuals triggered an overconservative curvature bound. Replaced it with max absolute pairwise cross of first/second derivative controls divided by positive speed lower bound cubed. Convex-hull bounds justify this in every orientation; it does not weaken validation based on search results. Independent reviewer checked that bound, length/grade bounds, inverse-station bracket and no-game-permission boundaries.

## Explicitly deferred / 明确未实现

No demonstrated case in this fixed matrix changes baseline-all-fail into new-pass. Split curves offer alternative shapes; their extra degrees of freedom are not claimed as broader solved coverage yet. Tight turn and almost180° short cases remain failed/unknown; no global impossibility inference. No general minimum-radius constraint, arbitrary obstacles, swept-surface proof, biarc solver, clothoid solver, automatic engine conversion or actual game adapter.

Biarc is deferred because a tangent join generally still needs curvature-transition handling before satisfying this G2 piece contract and a finite straight tail. Clothoid investigation requires bounded integration/inversion and conversion error as well as a fair fixed-budget comparison; no implementation or timing result is asserted. Current two-piece quintics can be validated without adding a new numerical integration dependency. These are next algorithm evaluation candidates, not a claim that quintics dominate them.

Runtime migration still needs exact base Mod/game assembly versions and actual curve/piece creation contracts. Do not install or activate these functions merely because pure Core tests pass. Game/Anarchy/net48/remote CI remain NOT_RUN.

Current extension: [hard-region routing and independent smoothing validation](RESTRICTED_ROUTING.md); bounded planar support, runtime disabled.
