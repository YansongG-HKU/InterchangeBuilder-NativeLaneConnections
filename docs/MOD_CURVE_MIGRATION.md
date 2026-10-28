# Curve algorithms → Mod Core / 曲线算法迁入

The Demo is only a calculation/comparison/verification harness for left/right-turn algorithms, not a separate product. Latest authorized work is local development on development/curve-kernel-port; no push, PR change, main edit, merge, release or installed-version replacement. Baseline d487f8b24cc805d6dc380543b2149bda04491dbd.

## Algorithm decision / 算法收敛

Selected bounded planar family: reserve an explicit finite tangent line, then quintic Bezier endpoint derivative constraints, with scales0.8/1/1.2 and independent checks. It covers mirrored left/right turns, same-direction shifts and explicit travel-frame side offsets. Strict G2 with nonzero target curvature conflicts with the straight tail; variable-width/singular-offset inputs are unsupported. Short/reversing cases may fail this family, not prove global infeasibility. No clothoid or global-optimum claim.

Lab-only additions support rigidly rotated straight arches and bounded curved/oblique candidates. Curves are genuinely subdivided and Hermite profiles use estimated horizontal arc lengths, not control parameter t. Independent inspector bounds actual arc length by chord/control polygon intervals and propagates those intervals into grade/height. Multiple crossings share one plateau. Flat straight lower-road capsules conservatively enclose full widths; overlap includes whole uncertain curved leaf intervals. Hermite is affine in segment length, so extrema over station intervals at both length endpoints bound all possible lengths. A lower clearance bound below the threshold is UNKNOWN, not proof of failure. No terrain/support/vehicle envelope claim.

Independent reference cases: rotated440m/10m arch preserves peak7.5% and underside8.5m; polynomial x480t/y90smoothstep split at t1/4 rises to6m by true arc, stays at6m, and explicit120m tail descends to0 (peak<=7.5%). Oblique road crosses plateau, underside4.5m. Generated candidates are computed without these reference outputs. JS/C# vectors are differential evidence only, separately from analytic endpoint/control-point oracles.

## Actual C# migration / 已实际迁入

New `src/InterchangeBuilder.LaneConnections.Core/ExperimentalCurveKernel.cs` belongs to existing netstandard2.0 Core. It contains immutable double-precision planar ports/points, explicit tail/quintic candidate construction, bounds/conflict rejection, full request/revision/mode identity checks and coordinate-only world XZ mapping. Enabled defaults false; existing runtime has no call site. Even enabled output is CandidateUnverified and CanApplyToGame=false. No global Anarchy setting changes. Existing Core/runtime/build/release files are not replaced; only new Core source plus tests/runner are added, and the test project links shared contract checks/vectors.

C# now includes an independent bounded local planar output verifier: see [scope and evidence](CORE_GEOMETRY_VALIDATION.md). External-road corridor validation, elevation/clearance migration and actual engine conversion remain NOT_IMPLEMENTED. Do not switch a runtime route to this output until those checks and the real engine contract are integrated. This is real Core source and executable contract tests, not a fake game adapter.

## Verified engine contract gaps / 已核实引擎缺口

Runtime project targets net48 and references external InterchangeBuilderAssembly, Game.dll, Colossal.Logging/Core/Mathematics and Unity.Entities/Mathematics. Public tree does not contain the base CurveRoadApplyService implementation or game binaries. Scoped cloud checks found no vendor directory/base DLL/game installation in the target workspace. No user computer or game payload was accessed. SDK8.0.427 was fetched from official Microsoft distribution into a temporary directory only; no game assemblies were downloaded.

Actual source reads Game.Net.Curve.m_Bezier a/b/c/d, NetGeometryData widths and selected prefab, and calls NetUtils.CanConnect (EndpointAlignmentService). SelectedNode/RampEndpoint and snapshot patches preserve chosen positions/tangents and existing grade; ConnectionSelectionStore binds selections to snapshots. Reuse that selection, never recover identity from proximity. Current CoursePositionPatch edits XZ endpoint positions only. Bootstrap reflects CreateDefinitionEntities(snapshot) and7-argument CreateCoursePosition, but those signatures do not establish a full new curve input contract. A quintic cannot be passed off as that cubic a/b/c/d. Base-package versions/signatures, certified quintic→engine-piece conversion, explicit tail preservation and dependency ownership need verified adapters before actual construction.

Minimal next route: provide pure validated requests from the real selected snapshot (identity/version/position/direction/curvature/width/elevation/grade); extend checks to external corridors and elevation; establish actual base DLL curve/segment contract; add default-off preview route and recheck converted output; then authorized game generation, directed lane connectivity, save/reload, undo and road/pier ownership recovery. Existing Bootstrap catch/fallback logging is not transaction rollback, and RuntimeSmoke metadata is not game behavior. Do not expand/refactor those systems in this slice.

## Execution evidence and limits / 执行记录

The earlier NuGet-Migrations mutex failure is resolved using supported process-local XDG_RUNTIME_DIR (the default home filesystem was read-only). Standard SDK 8.0.427 now builds the actual netstandard2.0 Core and passes all 13 xUnit tests. The shared wrapper executes 192 detailed contracts; these are not 192 separate xUnit tests. Independent Roslyn/.NET8 runner also passes 192. Historical68-check records remain unchanged.

See [current evidence and exact commands](curve-verifier-evidence/RESULTS.md). JS/C# vectors are differential evidence, separate from analytic oracles. The generator remains default-disabled/unwired and CandidateUnverified; only actual inspected output may receive VerifiedLocalPlanar, never game placement permission.

Fresh protection uses `scripts/check-curve-port-boundary.py`: existing runtime/installation/build/release/CI remain unchanged. Prior27-file reports are historical evidence, not a permanent prohibition on authorized Core work. Game, Anarchy, actual net48/base Mod build and remote CI remain NOT_RUN. No installed files touched.

Official temporary SDK tool reference: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script .

2026-10-28 local extension: [curve primitives, bounded station/profile functions and inspected split candidates](CURVE_FUNCTIONS.md). The original inspector scope and game exclusions remain; experimental search is default-disabled. / 已扩充纯数学内核，未启用游戏路径。

Current extension: [hard-region routing and independent smoothing validation](RESTRICTED_ROUTING.md); bounded planar support, runtime disabled.

[Partial spatial proof checkpoint](SPATIAL_PROOF_CHECKPOINT.md): exact straight-prism references only; curved 3D/global self-proof and selection integration remain incomplete. Current runtime/game boundaries unchanged.

[Nonstraight exact quarter-arc proof slice](QUARTER_ARC_PROOF.md) adds bounded flat annular-strip+tail full-domain certification and actual 3D candidate selection. General curved/nonconstant-profile proof remains incomplete.

[General rising/descending Bezier external certificate](BEZIER_EXTERNAL_3D.md) now checks continuous station envelopes against all supplied obstacles. General self remains UNKNOWN; no complete placement permission.
