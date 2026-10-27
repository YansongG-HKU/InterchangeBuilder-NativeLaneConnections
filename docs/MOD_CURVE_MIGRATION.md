# Curve algorithms → Mod Core / 曲线算法迁入

The Demo is only a calculation/comparison/verification harness for left/right-turn algorithms, not a separate product. Latest authorized work is local development on development/curve-kernel-port; no push, PR change, main edit, merge, release or installed-version replacement. Baseline d487f8b24cc805d6dc380543b2149bda04491dbd.

## Algorithm decision / 算法收敛

Selected bounded planar family: reserve an explicit finite tangent line, then quintic Bezier endpoint derivative constraints, with scales0.8/1/1.2 and independent checks. It covers mirrored left/right turns, same-direction shifts and explicit travel-frame side offsets. Strict G2 with nonzero target curvature conflicts with the straight tail; variable-width/singular-offset inputs are unsupported. Short/reversing cases may fail this family, not prove global infeasibility. No clothoid or global-optimum claim.

Lab-only additions support rigidly rotated straight arches and bounded curved/oblique candidates. Curves are genuinely subdivided and Hermite profiles use estimated horizontal arc lengths, not control parameter t. Independent inspector bounds actual arc length by chord/control polygon intervals and propagates those intervals into grade/height. Multiple crossings share one plateau. Flat straight lower-road capsules conservatively enclose full widths; overlap includes whole uncertain curved leaf intervals. Hermite is affine in segment length, so extrema over station intervals at both length endpoints bound all possible lengths. A lower clearance bound below the threshold is UNKNOWN, not proof of failure. No terrain/support/vehicle envelope claim.

Independent reference cases: rotated440m/10m arch preserves peak7.5% and underside8.5m; polynomial x480t/y90smoothstep split at t1/4 rises to6m by true arc, stays at6m, and explicit120m tail descends to0 (peak<=7.5%). Oblique road crosses plateau, underside4.5m. Generated candidates are computed without these reference outputs. JS/C# vectors are differential evidence only, separately from analytic endpoint/control-point oracles.

## Actual C# migration / 已实际迁入

New `src/InterchangeBuilder.LaneConnections.Core/ExperimentalCurveKernel.cs` belongs to existing netstandard2.0 Core. It contains immutable double-precision planar ports/points, explicit tail/quintic candidate construction, bounds/conflict rejection, full request/revision/mode identity checks and coordinate-only world XZ mapping. Enabled defaults false; existing runtime has no call site. Even enabled output is CandidateUnverified and CanApplyToGame=false. No global Anarchy setting changes. Existing Core/runtime/build/release files are not replaced; only new Core source plus tests/runner are added, and the test project links shared contract checks/vectors.

C# scope is deliberately planar candidate construction, not the entire JS corridor/vertical validator. C# corridor validation, elevation/clearance migration and actual engine conversion are still NOT_IMPLEMENTED. Do not switch a runtime route to this output until those checks and the real engine contract are integrated. This is real Core source and executable contract tests, not a fake game adapter.

## Verified engine contract gaps / 已核实引擎缺口

Runtime project targets net48 and references external InterchangeBuilderAssembly, Game.dll, Colossal.Logging/Core/Mathematics and Unity.Entities/Mathematics. Public tree does not contain the base CurveRoadApplyService implementation or game binaries. Scoped cloud checks found no vendor directory/base DLL/game installation in the target workspace. No user computer or game payload was accessed. SDK8.0.427 was fetched from official Microsoft distribution into a temporary directory only; no game assemblies were downloaded.

Actual source reads Game.Net.Curve.m_Bezier a/b/c/d, NetGeometryData widths and selected prefab, and calls NetUtils.CanConnect (EndpointAlignmentService). SelectedNode/RampEndpoint and snapshot patches preserve chosen positions/tangents and existing grade; ConnectionSelectionStore binds selections to snapshots. Reuse that selection, never recover identity from proximity. Current CoursePositionPatch edits XZ endpoint positions only. Bootstrap reflects CreateDefinitionEntities(snapshot) and7-argument CreateCoursePosition, but those signatures do not establish a full new curve input contract. A quintic cannot be passed off as that cubic a/b/c/d. Base-package versions/signatures, certified quintic→engine-piece conversion, explicit tail preservation and dependency ownership need verified adapters before actual construction.

Minimal next route: provide pure validated requests from the real selected snapshot (identity/version/position/direction/curvature/width/elevation/grade); port independent checks; establish actual base DLL curve/segment contract; add default-off preview route and recheck converted output; then authorized game generation, directed lane connectivity, save/reload, undo and road/pier ownership recovery. Existing Bootstrap catch/fallback logging is not transaction rollback, and RuntimeSmoke metadata is not game behavior. Do not expand/refactor those systems in this slice.

## Execution evidence and limits / 执行记录

Standard dotnet test could not start: NuGet-Migrations named Mutex throws IOException in this cloud environment. A separate no-NuGet runner compiles actual Core source using SDK Roslyn and net8 reference assemblies, then runs on .NET8; it does not pretend to execute xUnit or build netstandard2.0/net48. It passed68 explicit contract assertions, including independent straight controls, analytic endpoint curvature, faults, side/mirror cases, complete request invalidation, mode geometry invariance and8 JS/C# shared cases. The new xUnit entry calls the same contract checks when standard tooling is available; original xUnit12 and new wrapper remain NOT_RUN here.

Reproduce standard test: `dotnet test tests/InterchangeBuilder.LaneConnections.Core.Tests/InterchangeBuilder.LaneConnections.Core.Tests.csproj --configuration Release --nologo`. Fallback actual compiler/runtime execution: `python3 scripts/test-curve-contract-no-nuget.py /absolute/sdk-root`. Shared vectors are generated from the lab with `node scripts/export-core-contract.js`; never label differential output as independent truth. Source hashes/actual results in docs/curve-port-evidence and lab evidence-curve-port.

Fresh protection uses `scripts/check-curve-port-boundary.py`: approved new Core source and test integration are explicit, existing runtime/installation/build/release/CI remain unchanged. Prior27-file reports are historical evidence, not a permanent prohibition on authorized Core work. Game, Anarchy, actual net48/base Mod build, netstandard target build, standard xUnit and remote CI remain NOT_RUN. Revert this local commit for rollback; no installed files touched.

Official temporary SDK tool reference: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script .
