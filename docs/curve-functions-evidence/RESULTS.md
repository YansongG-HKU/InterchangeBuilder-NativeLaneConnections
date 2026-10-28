# Curve functions actual execution / 本轮实际执行

Date2026-10-28; base c8a27019ab6fa39703f38b482795c4e81b32058b. Same isolated development/curve-kernel-port, local only; no push/PR/merge/release.

Actual netstandard2.0 Core build PASS. Standard xUnit13 passed/0failed/0skipped; shared detailed assertions297 pass, also executed by independent Roslyn/net8 runner. Existing Node75 pass. No JS production/UI change: browser/build not rerun; earlier evidence remains historical. No new screenshot needed or claimed for pure math changes. net48/runtime/base game/Anarchy/remote CI NOT_RUN.

## Fixed comparison

13 candidate attempts per non-conflicting request: baseline3 + line1 + split9. All inputs use width8/tail40 and fixed user ports. Actual outputs, errors, length bounds, sampled radius, execution time and failure reasons in contracts-and-comparison.json. Radius samples are NOT a certified minimum. Outcomes below are only VerifiedLocalPlanar, not external-corridor/vertical/vehicle/game success. Numbers count alternative candidates, not independent solved cases.

| Scenario | Baseline pass / 3 | New pass / 10 | Total failed | Total unknown |
|---|---:|---:|---:|---:|
| straight | 3 | 10 | 0 | 0 |
| near-collinear | 3 | 9 | 1 | 0 |
| epsilon-positive | 3 | 10 | 0 | 0 |
| epsilon-negative | 3 | 10 | 0 | 0 |
| pi-positive | 0 | 0 | 13 | 0 |
| pi-negative | 0 | 0 | 13 | 0 |
| shift | 3 | 9 | 1 | 0 |
| left | 3 | 9 | 1 | 0 |
| right | 3 | 9 | 1 | 0 |
| near-reverse | 2 | 3 | 4 | 4 |
| short-turn | 0 | 0 | 13 | 0 |
| same-curvature-G1 | 3 | 9 | 1 | 0 |
| opposite-curvature-G1 | 3 | 9 | 1 | 0 |
| strict-G2-conflict | 0 | 0 | 0 | 0 |

Strict-G2-conflict returns ConstraintConflict before any candidate, so its denominator is0 rather than3/10. No matrix case demonstrates baseline-all-fail becoming new-pass. Short-turn and π±1e-8 cases remain unsolved within this budget; no global no-solution claim.

## Reproduce

From repository root, reuse the process-local XDG configuration documented in ../curve-verifier-evidence/RESULTS.md:

```sh
XDG_RUNTIME_DIR=/tmp/cs2-validator-runtime XDG_DATA_HOME=/tmp/cs2-validator-data NUGET_PACKAGES=/tmp/cs2-validator-nuget DOTNET_CLI_HOME=/tmp/cs2-validator-dotnet-home TMPDIR=/tmp/cs2-validator-temp DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false /tmp/cs2-dotnet/dotnet test tests/InterchangeBuilder.LaneConnections.Core.Tests/InterchangeBuilder.LaneConnections.Core.Tests.csproj --configuration Release --nologo --logger 'trx;LogFileName=functions.trx' --results-directory /tmp/cs2-function-tests
python3 scripts/test-curve-contract-no-nuget.py /tmp/cs2-dotnet
python3 scripts/check-curve-port-boundary.py
cd labs/interchange-demo
node scripts/export-function-oracles.js
npm test
```

Independent agent reviewed actual source and independently ran the intermediate251 assertions, then separately reviewed the final inverse-station bracket. No mathematical false-pass blocker found; arc sweep comment corrected. Final297 assertion run additionally covers independent JS21 polynomial rows, split consistency, ±epsilon branches, S/mirror/scale and fault cases. Final profile Grade is the same analytic derivative used by the inspected envelope and checked against JS values. First rotation regression failed on old curvature bound; fixed pairwise derivative cross-hull bound, then all scenarios passed rotation invariance. Source hashes and current boundary evidence are saved separately from historical evidence.

No changes to stable runtime, installation, build/release/CI or third-party settings. New pure Core files are included only by the existing Core project; there is no runtime invocation. Math primitives are not engine-compatible formats. Remaining external DLL/signature/version gaps are described in ../CURVE_FUNCTIONS.md. Revert this local commit for rollback; no installed state changed.

Final independent read-only review confirmed the added Grade expression and final code/doc diff without blockers; no test rerun or mutation by reviewer in that final pass.
