# Repository execution and documentation rules

This is the single maintenance repository for the existing Mod and its offline research lab. Read README, docs/DOCUMENTATION_INDEX.md, docs/INTEGRATION.md and the relevant scoped instructions before changes. Imported lab AGENTS remains applicable; historical source-repository PR targets are superseded by this integration's target-repository policy, not by a change to evidence.

- Review all tracked documents on each change; update every affected claim/link/version and record unchanged groups with reasons in the index.
- Use topic branches and Draft PRs against this repository's main unless an explicitly authorized dependency branch is required. No automatic merge, release or deployment.
- Keep C# runtime and Node lab toolchains separate. Do not change runtime/release files or merge PR #3/#4 as part of lab integration.
- Preserve input/expected/actual/version evidence. Run lab scripts from labs/interchange-demo; they overwrite local evidence, so archive the exact historical baseline first and store verification outputs separately.
- Do not equate offline geometry, WebGL, ownership heuristics or core tests with game lane connectivity, clearance, saves or compatibility. The imported adapter is unsupported.
- Record failures and UNKNOWN/NOT_RUN honestly. Protect existing world objects; do not infer ownership from proximity or treat remaining piers as successful roads.
- Respect labs/interchange-demo/RIGHTS.md and its third-party notices. The root MIT license does not relicense the imported snapshot.
- Do not introduce game assemblies, binary packages, private saves, credentials, paid services or access to the user's computer. Do not modify unrelated repositories.

The single semi-direct left-turn over/under case is approved for subsequent staged work; integration comes first, material/oracle/tolerance freeze precedes algorithm implementation, and game verification remains an independent gate. See docs/LEFT_TURN_PLAN.md.

Current authorized development: the Lab is an algorithm verification harness for Mod migration, not a separate product. On isolated development branches, new default-disabled pure Core algorithms and tests are permitted; preserve existing runtime entry paths and installed/released versions. Record explicit diff/default behavior/regressions rather than treating historical27-file equality as a permanent gate. Real engine hooks require verified base/game contracts; no invented adapter or automatic release.
