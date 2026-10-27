# Curve kernel migration evidence / 算法迁入实测

Local branch development/curve-kernel-port, preceding head d487f8b24cc805d6dc380543b2149bda04491dbd. Final local commit given in delivery. No push, PR, merge, release or installation change. Demo is only an algorithm verification harness.

| Execution | Result |
|---|---|
| python3 scripts/test-curve-contract-no-nuget.py /tmp/cs2-dotnet | Actual SDK8.0.427 Roslyn compilation of all Core sources and .NET8.0.33 execution;68 contract assertions passed, core-contracts.json includes expected/actual and hashes |
| Standard dotnet test existing net8 xUnit project | NOT_RUN: startup IOException creating NuGet-Migrations mutex; standard-test-blocker.txt. No xUnit results fabricated |
| npm test (lab) |75 passed,0 failed; includes all baseline and7 new migration-algorithm tests |
| npm run test:browser / test:3d / node tests/diagnostic-focus.js |81 /37 /13 actual baseline browser assertions passed |
| npm run test:tail-browser / test:tail-phase2-browser |29 /35 actual assertions passed |
| node tests/curve-port-browser.js |9 actual generation/profile browser assertions passed; two real full-page screenshots |
| EVIDENCE_DIR=evidence-curve-port npm run evidence:tail |9 reference/fault fixtures plus actual generated outputs, source hashes |
| npm run build |Both lab entries built; shared Three.js chunk advisory retained |
| python3 scripts/check-curve-port-boundary.py |26 originally protected files unchanged; approved existing test-project change + new Core class/test runner. No existing runtime call sites, no build/release/CI edits |

Source manifests identify exactly tested code; precedingHead is not falsely presented as containing the uncommitted changes. The source manifest here covers Core/test/tool scripts; lab computation.json covers lab sources and fixtures. Full browser/core logs and JSON are in ../../labs/interchange-demo/evidence-curve-port. Prior evidence/evidence-tail/evidence-tail-v2 were copied aside and restored byte-for-byte; current run copies are separate.

Independent read-only reviewer checked local-frame generation, conservative capsule/station interval clearance and actual output splitting; independently sampled50 seeded oblique straight-Hermite cases at0.01m against reported lower bounds, finding no bound above those samples (finite corroboration, not proof). Reviewed C# Matches complete immutable request identity, default Disabled/no runtime caller, CandidateUnverified/CanApplyToGame=false. Reviewed exact collinear positive-speed curvature bound repair and same physical non-linear parameterization test. No remaining definite false-pass found in this scope. Seven final algorithm tests cover90degree/general rotation, curved oblique and mirror left/right, finite tail, long overlap/low edge, thicker deck, insufficient grade and parameterization invariance. The C# shared vectors are differential comparisons, not independent mathematical truth; straight-point and endpoint-jet oracles are independent.

Recovered failures: a new nonlinear-collinear test initially returned UNKNOWN because tangential acceleration overestimated curvature; exact zero chord-error and positive-speed case now uses curvature0, and same physical straight geometry gets the same orthogonal clearance calculation without changing its output kind. The previous oblique-unsupported test now checks bounded-family exhaustion/unknown because that support domain has intentionally expanded. Standard SDK bootstrap first-use mutex failure remains a blocker to standard tooling; the documented direct compiler runner executes actual source without claiming to fix or emulate xUnit.

Runtime adaptation blocker is concrete: public/base snapshot lacks CurveRoadApplyService implementation and target workspace has no InterchangeBuilder/Game/Unity assemblies. Existing Game.Net cubic a/b/c/d and7-argument endpoint hook do not establish quintic/segmented course conversion. No fake engine calls were added. C# planar construction is migrated; C# corridor/profile validators and engine conversion/application still NOT_IMPLEMENTED. Actual netstandard target build, net48 runtime build, base-package smoke, game/Anarchy/save-load/ownership tests and remote CI NOT_RUN.

The official temporary SDK install documentation is recorded in ../MOD_CURVE_MIGRATION.md. No SDK/game binaries or package cache are committed. Rollback this local commit to restore previous sources; no installed version was touched.
