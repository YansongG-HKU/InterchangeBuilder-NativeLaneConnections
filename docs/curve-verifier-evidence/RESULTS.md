# Core verifier execution — 2026-10-27

Base source commit: 2cf2fb410ad6d33563992a513358cc4b596b2a69; final-source hashes in contracts.json and source-hashes.json. Local isolated development/curve-kernel-port only. No push/PR/merge/release/install.

- Standard SDK8.0.427: actual netstandard2.0 Core build PASS; net8 xUnit13 passed,0 failed,0 skipped (tests.trx/dotnet-test.txt).
- Shared detailed contracts192 passed (contracts.json), also executed independently using Roslyn/net8 direct runner. 192 is NOT the xUnit test count.
- Node regression75 passed (node-test.txt). No JS production/UI change, so browser/build suites were not redundantly rerun; previous head's81/37/13/29/35/9 browser evidence remains historical, not a new run.
- Fresh boundary.json verifies original protected runtime/build/release/CI files and no runtime calls to generator/verifier; no binary payload tracked. Approved original test project changes link shared checks and JSON input only.
- Independent code reviewer inspected actual verifier/representation/conditional runner and independently executed153 checks; final39 added checks are tests only. Final reviewer readback confirmed all13 TRX tests passed and no blocker in final code/test/scope diff. No definite false-pass finding. Scope caveat: nonlocal corridor check excludes short station separations; local success is not global swept-surface certification.
- net48 runtime/base Mod/Game/Unity/Anarchy integration, actual game paths/saves and remote CI: NOT_RUN. Missing engine dependency contracts remain the real blocker; standard Core tooling is no longer blocked.

## Commands and tooling recovery

Initial standard command `dotnet test tests/InterchangeBuilder.LaneConnections.Core.Tests/InterchangeBuilder.LaneConnections.Core.Tests.csproj --configuration Release --nologo` failed in NuGet-Migrations named mutex initialization with System.IO.IOException. Traced filesystem cause: `mkdir("/home/agent/.dotnet", 0700) = -1 EROFS (Read-only file system)`. Changing DOTNET_CLI_HOME/TMPDIR alone did not move runtime mutex backing storage.

Official runtime8.0.33 source reads XDG_RUNTIME_DIR before falling back to user home: [sharedmemory.cpp](https://github.com/dotnet/runtime/blob/v8.0.33/src/coreclr/pal/src/sharedmemory/sharedmemory.cpp). Supported process-local recovery, no HOME override or global security change:

```sh
XDG_RUNTIME_DIR=/tmp/cs2-validator-runtime \
XDG_DATA_HOME=/tmp/cs2-validator-data \
NUGET_PACKAGES=/tmp/cs2-validator-nuget \
DOTNET_CLI_HOME=/tmp/cs2-validator-dotnet-home \
TMPDIR=/tmp/cs2-validator-temp \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
/tmp/cs2-dotnet/dotnet test tests/InterchangeBuilder.LaneConnections.Core.Tests/InterchangeBuilder.LaneConnections.Core.Tests.csproj --configuration Release --nologo --logger 'trx;LogFileName=curve-core-final.trx' --results-directory /tmp/cs2-validator-test-results
python3 scripts/test-curve-contract-no-nuget.py /tmp/cs2-dotnet
python3 scripts/check-curve-port-boundary.py
cd labs/interchange-demo
node scripts/export-verifier-contract.js
npm test
```

Restoring existing pinned dependencies then exposed CS0017 (shared runner Main conflicted with test entrypoint); fixed by CURVE_CONTRACT_RUNNER conditional, enabled only by standalone runner. Last test-only additions exposed CS0136 local-name shadowing; renamed test local and reran both final suites successfully. Failures are not counted as passing runs. No production runtime change or new dependency.

Historical evidence directories were not rewritten. Rollback: revert this local commit; no installed state needs restoration.
