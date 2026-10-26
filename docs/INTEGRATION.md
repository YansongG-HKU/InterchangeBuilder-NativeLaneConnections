# Integration provenance, scope and validation / 整合记录

This branch proposes consolidation into InterchangeBuilder-NativeLaneConnections, based on main `9f083061cdcf2bb08ed03c0527ebbd6f591c1179`. It is not merged and changes no C# runtime, released package, game assembly or existing CI workflow. 本轮仅归并研发资料与运行工具，不声称Lab已接通Mod。

## Exact import and history / 精确导入与历史

Source: https://github.com/YansongG-HKU/CS2-Interchange-Lab/tree/e790aa63926ebe7d499c5b811bad2f60248426cb ; branch `feat/phase1-offline-demo`, not source main. All 87 tracked files were first copied byte-for-byte into `labs/interchange-demo`. [Manifest](import-manifest.json) records original Git blob IDs, SHA-256 and one-to-one source/destination paths; [overlay record](import-overlays.json) records only current-maintenance wording changes and resulting hashes. All tracked code, examples and historical evidence remain identical.

This is an **auditable snapshot**, not a subtree/history import. Source history remains available at the pinned public repository and is not claimed as target ancestry. No source repository deletion/archive or PR closure. Subsequent feature work belongs here only. 源历史可查，不伪称保留git祖先。

The historical nested `.github` files are inert reference files, not active target workflows. Nested AGENTS and RIGHTS are retained byte-for-byte. Root AGENTS and the updated lab DEVELOPMENT explain target PR policy; source requirement-branch statements in historical records are not current instructions. The only current execution plan is [LEFT_TURN_PLAN](LEFT_TURN_PLAN.md); imported research and acceptance contracts support it.

## Rights and exclusions / 权利与排除

Original target MIT license remains unchanged for the original project. Imported snapshot content is **not relicensed MIT**: [source RIGHTS](../labs/interchange-demo/RIGHTS.md) states no selected project license, and [dependency notices](../labs/interchange-demo/docs/THIRD_PARTY.md) preserve separate licenses. No game DLLs, installer/base packages, personal saves, credentials or external asset copies were imported. PNGs are recorded real browser captures from the source snapshot.

## Existing proposals / 既有提案

- [PR #3](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/pull/3), head `50313d3667009146dda7e1a5938acab4ea05016d`: separate selection-state/endpoint work; unmerged and not game-verified here.
- [PR #4](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/pull/4), head `0e9b34492f82bc46f35a9c7ce10f334e2ce76eef`: selection identity, endpoint metadata, compatibility discovery and conditional placement-recovery proposals; unmerged, not accepted as a runtime dependency by this integration.
- PR #6 temporary standards-download work is unrelated and untouched.

Neither PR #3 nor #4 is merged or cherry-picked. Later convergence must compare their actual diffs, avoid duplicated selection/recovery changes, validate default/explicit/forbidden lane behavior and ownership, and keep their game gates independent. [Pinned lessons](../labs/interchange-demo/docs/research/road_mod_lessons.md) describe main versus PR evidence; they are not proof that proposals work. Integration can be reviewed without adopting them.

## Validation and rollback / 验证与回滚

Run commands from `labs/interchange-demo`, not repository root. Browser tests require `/usr/bin/chromium` and a lab dev server on `127.0.0.1:4173`; no game install is needed. Historical `evidence/` was frozen before tests, new outputs retained separately for verification, then the exact imported historical directory restored and all hashes checked again. [Integration validation](integration-evidence/RESULTS.md) records actual current results, not retroactively modified source evidence.

Existing root CI only tests C# Core; it is unchanged. Local offline passes are separate from CI, and game-dependent C# runtime/build/smoke is NOT_RUN without game/base-package dependencies. No unrelated workflow is manually run. No .NET SDK was found in this container, so C# core tests are also NOT_RUN locally.

Rollback is revert of the integration commit/PR; no target runtime file has been edited. The source repository and its Draft PR remain available. The latest scope leaves the source repository untouched; no migration-guide PR is created in this operation.

## Original Mod protection / 原Mod保护

[Per-file baseline hashes](integration-evidence/mod-protection.json) verify all 27 original non-updated files against main9f083061; only README/CHANGELOG/THIRD_PARTY_NOTICES are documentation updates. Hashes compare Git blob bytes, since existing .gitattributes applies CRLF checkout to PowerShell. Static inspection found no root/external compile glob or recursive repository packaging that includes labs: project references, UI paths and vendor payloads are explicit. The build copies root CHANGELOG/THIRD_PARTY_NOTICES as package documentation, so their explanatory text changes in future builds; build logic and runtime inputs do not. No existing release is rebuilt or uploaded. This is static isolation evidence, not MSBuild/game validation.
