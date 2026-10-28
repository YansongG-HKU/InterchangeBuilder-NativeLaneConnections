# 文档索引与提交影响记录

每次开发和提交全面检查全部文档；受影响内容同提交更新，无影响项说明理由。当前是人工门禁，无自动CI。

## 当前索引

- [.github/ISSUE_TEMPLATE/experiment.md](../.github/ISSUE_TEMPLATE/experiment.md)
- [.github/pull_request_template.md](../.github/pull_request_template.md)
- [AGENTS.md](../AGENTS.md)
- [CHANGELOG.md](../CHANGELOG.md)
- [CONTRIBUTING.md](../CONTRIBUTING.md)
- [README.md](../README.md)
- [RIGHTS.md](../RIGHTS.md)
- [docs/COMPATIBILITY.md](COMPATIBILITY.md)
- [docs/CURVED_SITE_OPTIMIZATION.md](CURVED_SITE_OPTIMIZATION.md)
- [docs/DESIGN_REQUIREMENTS.md](DESIGN_REQUIREMENTS.md)
- [docs/DEVELOPMENT.md](DEVELOPMENT.md)
- [docs/DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)
- [docs/EXPERIMENT_TEMPLATE.md](EXPERIMENT_TEMPLATE.md)
- [docs/GEOMETRY_AND_LANES.md](GEOMETRY_AND_LANES.md)
- [docs/REALISM_AND_GAMEPLAY.md](REALISM_AND_GAMEPLAY.md)
- [docs/REQUIREMENTS_ACCEPTANCE.md](REQUIREMENTS_ACCEPTANCE.md)
- [docs/ROADMAP.md](ROADMAP.md)
- [docs/ROAD_RULES.md](ROAD_RULES.md)
- [docs/SOURCES.md](SOURCES.md)
- [docs/UX_AND_I18N.md](UX_AND_I18N.md)
- [docs/VALIDATION.md](VALIDATION.md)
- [docs/research/community.md](research/community.md)
- [docs/research/game_pitfalls.md](research/game_pitfalls.md)
- [docs/research/geometry.md](research/geometry.md)
- [docs/research/land_controls.md](research/land_controls.md)
- [docs/research/land_optimization.md](research/land_optimization.md)
- [docs/research/realism_addendum.md](research/realism_addendum.md)
- [docs/research/ux.md](research/ux.md)

## 2026-10-26 第一阶段需求变更影响记录

所有索引文档已检查。新增：DESIGN_REQUIREMENTS、GEOMETRY_AND_LANES、UX_AND_I18N、REQUIREMENTS_ACCEPTANCE和research三份证据报告。更新：README、AGENTS、CONTRIBUTING、DEVELOPMENT、ROAD_RULES、COMPATIBILITY、VALIDATION、ROADMAP、SOURCES、EXPERIMENT_TEMPLATE、CHANGELOG、Issue/PR模板及本索引，以同步需求入口、状态和验收。RIGHTS无需修改：未引入第三方代码/资产，权利边界不变；.gitignore无需修改：仍仅Markdown文档，无新工具输出类型。

核验：文档内链接与需求/验收覆盖检查；独立设计审查修正full定义、外部编辑失效、支持域、节点映射和离散容差。游戏、求解器、Mod兼容、UI、交通性能全部NOT_RUN；本轮未安装Mod、未建立游戏实现或CI。

初始化历史：16文件准备提交已完成，全兼容NOT_TESTED。后续每个提交沿用逐文件/按同因分组列清单的影响记录，禁止为凑数机械改日期。

## 2026-10-26 现实感与游戏实际补充影响记录

新增REALISM_AND_GAMEPLAY、research/realism_addendum、research/game_pitfalls；更新README、CHANGELOG、DESIGN_REQUIREMENTS、GEOMETRY_AND_LANES、UX_AND_I18N、REQUIREMENTS_ACCEPTANCE（累计31场景）、DEVELOPMENT、ROAD_RULES、VALIDATION、COMPATIBILITY、ROADMAP、EXPERIMENT_TEMPLATE、SOURCES、PR模板及本索引。

已复核无需修改：AGENTS/CONTRIBUTING继续要求全仓同步及双语，规则无变；RIGHTS无第三方资产/代码引入；Issue模板已有需求/证据字段；research/geometry、community、ux保留上一轮历史证据；.gitignore无新增输出类型。不存在新实现或游戏实验。

检查内部链接和需求状态；全部运行验证仍NOT_RUN，不把现实参考数值或社区经验宣称为引擎规范。

## 2026-10-26 曲线与场地优化影响记录

新增CURVED_SITE_OPTIMIZATION、research/land_optimization和land_controls；更新README、DESIGN_REQUIREMENTS、GEOMETRY_AND_LANES、UX_AND_I18N、REALISM_AND_GAMEPLAY、ROAD_RULES、VALIDATION、REQUIREMENTS_ACCEPTANCE、ROADMAP、EXPERIMENT_TEMPLATE、DEVELOPMENT、PR模板、SOURCES、CHANGELOG及本索引。

全部其余文档已核对无需修改：AGENTS/CONTRIBUTING通用一致性规则仍适用，RIGHTS无新代码资产授权，COMPATIBILITY无新依赖/通过结论，Issue模板已有需求/证据字段，之前5份research保存历史来源不追改，.gitignore无新增输出类型。核心需求数学审查修正完整预留集合、差集/净值、分母和越界、局部层数及搜索状态；内部链接检查，全部运行场景仍NOT_RUN。

## 2026-10-25 UTC — 离线实现变更影响 / Offline implementation impact

新增索引 / New entries:
- [OFFLINE_DEMO](OFFLINE_DEMO.md): architecture, run, frozen domain, approximation and rollback.
- [OFFLINE_VALIDATION](OFFLINE_VALIDATION.md): evidence, partial acceptance mapping, unrun game/CI checks.
- [THIRD_PARTY](THIRD_PARTY.md): dependency versions/licenses and runtime notices.
- [Evidence source manifest](../evidence/source-manifest.json), [computation](../evidence/computation.json), [browser](../evidence/browser.json).

全面逐文件复核 / Full tracked-document review:

| Files | Disposition and reason / 处置及理由 |
|---|---|
| README.md, CHANGELOG.md, CONTRIBUTING.md | Updated: runnable subset, commands, actual evidence and limits / 可运行状态与验证 |
| RIGHTS.md | Updated: third-party npm licenses, project rights unchanged / 依赖许可分开 |
| docs/DESIGN_REQUIREMENTS.md, GEOMETRY_AND_LANES.md, UX_AND_I18N.md, CURVED_SITE_OPTIMIZATION.md | Updated: full design vs limited implemented subset, link current contracts / 完整目标不冒充完成 |
| docs/REQUIREMENTS_ACCEPTANCE.md, VALIDATION.md, EXPERIMENT_TEMPLATE.md | Updated: executed offline subcases vs NOT_RUN game cases / 测试分层 |
| docs/DEVELOPMENT.md, ROADMAP.md, ROAD_RULES.md | Updated: toolchain, P0.5 and experimental numerical rules / 工具链与阶段 |
| docs/COMPATIBILITY.md, REALISM_AND_GAMEPLAY.md, SOURCES.md | Updated: runtime libraries do not imply game compatibility; experimental definitions / 边界及来源 |
| docs/DOCUMENTATION_INDEX.md | Updated: new files and complete impact record / 全仓索引 |
| AGENTS.md | No change: execution/documentation/review restrictions still apply / 规则不变 |
| .github/pull_request_template.md, .github/ISSUE_TEMPLATE/experiment.md | No change: existing fields already cover evidence, versions, unrun items, rollback and scope / 模板可覆盖本次 |
| docs/research/geometry.md, community.md, ux.md, realism_addendum.md, game_pitfalls.md, land_optimization.md, land_controls.md | No change: dated research records and proposals, not current implementation status; preserve historical evidence / 保留历史研究，不重写来源 |
| .gitignore | Updated: exclude generated dist; no game/private data added / 构建产物忽略 |

New source/tests/examples and three new docs reviewed for matching version/schema, dependency rights, no secrets/game assets and accurate partial support. No .agents/skills exists in this checkout or readable workspace .agents directory. Existing unrelated workspace repositories were not modified. / 不修改其他仓库。

## 2026-10-25 UTC — 审查修复影响复核 / Review-fix impact review

已重新检查全部受版本控制文档。更新 README、CHANGELOG、OFFLINE_DEMO、OFFLINE_VALIDATION、UX_AND_I18N 及本索引：字段白名单、加载代次、标签交互、独立实验状态和实际测试数量。刷新 evidence 的源哈希/输入实测/构建记录及四张截图（新增 [窄屏中文](../evidence/browser-mobile-zh.png)）。

其余全部文档无需修改：AGENTS/CONTRIBUTING/DEVELOPMENT/RIGHTS/THIRD_PARTY 与 Issue/PR 模板的工作流、权利和依赖不变；DESIGN_REQUIREMENTS/GEOMETRY_AND_LANES/CURVED_SITE_OPTIMIZATION/REQUIREMENTS_ACCEPTANCE/VALIDATION/ROAD_RULES/ROADMAP/COMPATIBILITY/REALISM_AND_GAMEPLAY/EXPERIMENT_TEMPLATE/SOURCES 的支持域、数学/游戏边界和完整验收要求不变，当前离线证据仍由既有链接提供；七份 research 仍保留历史来源。不是新增游戏功能、交通或三维验证，也不扩展一期完成声明。

最后边界修复仅更新 OFFLINE_DEMO、OFFLINE_VALIDATION、CHANGELOG、本索引及源哈希/测试/构建证据：ANCHOR_RANGE 提前退出时车道实验为 NOT_RUN。其余文档已复核沿用上一轮无需修改理由，支持域、面积、依赖及游戏状态不变。 / Final boundary fix records NOT_RUN and refreshed evidence only.

## 三维/高差 V1 全仓影响 / Full 3D/profile impact review

New indexed documents: [VERTICAL_PROFILES](VERTICAL_PROFILES.md), [THREE_D_DESIGN](THREE_D_DESIGN.md). New evidence: [browser3d](../evidence/browser3d.json) and its six linked screenshots; `examples/elevated-full.json`; existing cases explicitly migrated to schema2 flat profiles.

Updated README, CHANGELOG, OFFLINE_DEMO, OFFLINE_VALIDATION, THIRD_PARTY, DESIGN_REQUIREMENTS, GEOMETRY_AND_LANES, CURVED_SITE_OPTIMIZATION, UX_AND_I18N, REQUIREMENTS_ACCEPTANCE, ROAD_RULES, DEVELOPMENT, COMPATIBILITY, ROADMAP, REALISM_AND_GAMEPLAY, VALIDATION, EXPERIMENT_TEMPLATE, SOURCES and this index: executable 3D/profile scope, migration, dependencies, test/evidence paths and truthful remaining limits.

Reviewed unchanged: AGENTS/CONTRIBUTING still impose applicable workflow and evidence discipline; RIGHTS already separates dependency licenses from project rights; Issue/PR templates already cover versions/scope/evidence/rollback; seven research files remain historical evidence rather than runtime claims; .gitignore already excludes dependencies and build output. No unrelated repository or game asset changes. / 已全面复核，无需修改项有明确理由。

## 2026-10-26 UTC — Diagnostic focus impact review

Reviewed all tracked Markdown against this narrowly scoped event-handler fix. Updated CHANGELOG, UX_AND_I18N, THREE_D_DESIGN, OFFLINE_VALIDATION and this index; added [diagnostic focus evidence](../evidence/diagnostic-focus.json), refreshed source hashes and applicable test/build records. Acceptance: located 3D error moves the real camera in zh/en; 2D marker and camera isolation remain correct. Rollback: revert this fix commit. No game/Mod version is targeted or tested.

Unchanged with reasons: README/OFFLINE_DEMO already promise diagnosis and link detailed validation, commands/support remain correct; AGENTS/CONTRIBUTING/DEVELOPMENT and both GitHub templates retain workflow requirements; RIGHTS/THIRD_PARTY/COMPATIBILITY have no dependency, license or game-state changes; DESIGN_REQUIREMENTS/GEOMETRY_AND_LANES/CURVED_SITE_OPTIMIZATION/VERTICAL_PROFILES/REQUIREMENTS_ACCEPTANCE/VALIDATION/ROAD_RULES/ROADMAP/REALISM_AND_GAMEPLAY/EXPERIMENT_TEMPLATE preserve geometry, acceptance scope and unknown boundaries; SOURCES and all seven research documents remain unchanged historical references. Examples/schema and package commands are unchanged; direct regression command is documented above. No new feature, publication or merge.

## 2026-10-26 UTC — 深度研究与计划全仓影响 / Deep research and plan impact

新增索引：[DEEP_CASE_PLAN](DEEP_CASE_PLAN.md)、[road_mod_lessons](research/road_mod_lessons.md)。前者冻结后续设计门槛，后者将CS1作者来源、旧项目固定main/未合并PR、用户故障观察和未证实根因分层；全部新增执行门槛NOT_RUN。 / New research and planning documents do not execute the proposed gates.

| 文档 / Files | 处置与理由 / Disposition and reason |
|---|---|
| README, CHANGELOG, ROADMAP | Updated: depth-first next task, status and priority recovery design; existing runtime claims preserved. |
| DESIGN_REQUIREMENTS, GEOMETRY_AND_LANES, CURVED_SITE_OPTIMIZATION, UX_AND_I18N, REQUIREMENTS_ACCEPTANCE | Updated: R29–R31/A43–A46, independent lane truth, joint geometry, reference counterexamples and P0 recovery boundaries. |
| DEVELOPMENT, VALIDATION, EXPERIMENT_TEMPLATE, both GitHub templates | Updated: pre-execution freeze, independent checks, error/provenance and ownership/recovery evidence. |
| COMPATIBILITY, ROAD_RULES, REALISM_AND_GAMEPLAY, SOURCES | Updated: separate source/game evidence, synthetic benchmarks, reference limitations and unverified recovery. |
| OFFLINE_DEMO, OFFLINE_VALIDATION, THREE_D_DESIGN, VERTICAL_PROFILES | Updated: current implementation vs unexecuted deep-case scope; existing tests not relabeled as new validation. |
| DOCUMENTATION_INDEX | Updated: new document links, full impact and document-only validation record. |
| AGENTS, CONTRIBUTING | Unchanged: current authorization, whole-repo review and evidence disciplines already govern this work. |
| RIGHTS, THIRD_PARTY | Unchanged: no copied third-party code/assets or dependency/license changes; prior repo MIT does not alter project rights. |
| Existing seven research documents | Unchanged: historical source records remain intact; new dated research is separately indexed. |
| Source/tests/scripts/examples/package files, evidence, .gitignore | Unchanged: documentation-only revision, no runtime/schema/case change and no new claims from historical results. |

核验：全部受控Markdown影响检查、相对链接、需求/验收/参考映射、NOT_RUN/UNKNOWN边界与git diff检查。本次未重跑核心/浏览器/构建，未安装或测试游戏/Mod；CI未执行不得算通过。用户只余桥墩故障尚未复现/定位。 / Checks cover documentation consistency and links only, not new runtime or game validation. Revert this documentation commit for rollback; keep Draft PR against the requirements branch.

## 目标仓整合维护 / Target-repository maintenance

当前执行计划、全仓索引与导入差异以 [根索引](../../../docs/DOCUMENTATION_INDEX.md) 和 [整合记录](../../../docs/INTEGRATION.md) 为准。本目录旧影响记录保留为历史，不暗示目标main已合并。 / Root index owns current maintenance; prior impact records are historical.

## 2026-10-27 显式直尾实验 / Explicit-tail experiment

新增 [EXPLICIT_TAIL_EXPERIMENT](EXPLICIT_TAIL_EXPERIMENT.md)、[evidence-tail结果](../evidence-tail/RESULTS.md)，更新当前受影响设计/运行/验收文档；完整逐组处置见 [根索引](../../../docs/DOCUMENTATION_INDEX.md)。旧导入manifest/历史证据保持原样，新实验源码哈希单独记录。 / New local implementation evidence is not retroactively written into historical import evidence. No push/PR mutation.

Pre-delivery independent review: updated experiment contract/results, input/output numeric validation and import regressions; refreshed only evidence-tail. Other documents retain the same limited support, rights, workflow, historical evidence and NOT_RUN boundaries; all prior unchanged-group reasons remain applicable. No new dependency, stable runtime change, push or PR mutation. / 交付前独立审查修复数值溢出与异常输入边界，不扩展功能。

## 2026-10-27 V2 complete impact review

New [V2 contract](EXPLICIT_TAIL_PHASE2.md) and evidence-tail-v2 record bounded automatic overpass generation, actual context curves, independent checks and import races. Updated root README/CHANGELOG/LEFT_TURN_PLAN and both indices; lab README, COMPATIBILITY, DEEP_CASE_PLAN, DESIGN_REQUIREMENTS, DEVELOPMENT, GEOMETRY_AND_LANES, OFFLINE_DEMO, OFFLINE_VALIDATION, REQUIREMENTS_ACCEPTANCE, ROADMAP, SOURCES, THREE_D_DESIGN, UX_AND_I18N, VALIDATION, VERTICAL_PROFILES, EXPLICIT_TAIL_EXPERIMENT distinguish current additional capabilities from V1 historical limits. Updated fixtures retain old references and add explicit known-thickness automatic and bound-context cases.

All other tracked documents reviewed unchanged: AGENTS/CONTRIBUTING/templates still govern local-only work/evidence; RIGHTS/licenses/notices unchanged without copied source/dependencies; original Mod releases and source research remain historical; INTEGRATION/manifests and prior evidence remain immutable baseline records; CURVED_SITE_OPTIMIZATION/REALISM_AND_GAMEPLAY/ROAD_RULES/EXPERIMENT_TEMPLATE retain full unmet goals and game gates. Stable C#/build/package/CI untouched; no game binaries/private data. New script optional EVIDENCE_DIR only selects lab report destination.

## Algorithm-to-Mod migration impact

New [migration contract](../../../docs/MOD_CURVE_MIGRATION.md) and current source/test evidence distinguish Lab calculation, pure C# Core and actual game adapter. Updated root AGENTS/README/CHANGELOG/LEFT_TURN_PLAN, both indices and lab current design/development/compatibility/validation/roadmap/geometry/vertical/UI/sources/V1/V2 contracts. New Core class is default-disabled and has no runtime call site; test project adds shared contracts. New no-NuGet runner is actual compilation, not a claimed xUnit run.

All other tracked documentation reviewed unchanged: CONTRIBUTING/templates still apply; RIGHTS/licenses/notices unchanged without copied external code/dependencies; old research/releases/integration/manifests/evidence remain historical; CURVED_SITE_OPTIMIZATION/REALISM_AND_GAMEPLAY/ROAD_RULES/EXPERIMENT_TEMPLATE retain unmet full requirements and independent game gates. Existing runtime/build/release/CI/installation unchanged. Historical27-file protection is not rewritten; current approved Core/test diff has a separate boundary report. No push or PR changes.

## Independent C# verifier impact / 独立验收迁入

New [local planar contract](../../../docs/CORE_GEOMETRY_VALIDATION.md) and root docs/curve-verifier-evidence preserve current actual-source tests, failure diagnosis and supported XDG recovery. Updated active migration summaries in root README/CHANGELOG/LEFT_TURN_PLAN/MOD_CURVE_MIGRATION and lab README plus current design/development/compatibility/geometry/vertical/UI/validation/roadmap/sources/V1/V2 docs: C# local verification is implemented; standard netstandard/xUnit now pass. Earlier execution records are immutable and retain their then-NOT_RUN status.

All tracked Markdown groups reviewed: AGENTS/CONTRIBUTING/templates still apply without rule changes; rights/licenses/notices unchanged (no new dependency or copied external code); integration manifests, original release/readme-upgrade and dated research/evidence remain historical; broader optimization/realism/road-rule plans still contain unmet goals, not current claims. No JS production/UI or game adapter changed, so prior browser evidence is preserved and no new screenshot is claimed. Runtime/build/package/CI unchanged; test project/standalone runner explicitly integrate verifier contracts. No push or PR action.

## 2026-10-28 curve functions impact

New [function/search contract](../../../docs/CURVE_FUNCTIONS.md) and root curve-functions-evidence preserve current outputs, bounds, comparisons and tests. Updated root README/CHANGELOG/LEFT_TURN_PLAN/MOD_CURVE_MIGRATION/CORE_GEOMETRY_VALIDATION; current lab summary paragraphs date prior192 results and refer to current extension. No Lab product/UI/production JS changes.

Reviewed all tracked Markdown: AGENTS/CONTRIBUTING/templates unchanged because local execution and evidence policy still apply; licenses/rights/notices unchanged without dependencies or copied code; historical research/release/import manifests and earlier evidence remain immutable. Broader lane/terrain/game plans retain unmet requirements. Existing external corridor/vertical JS contracts are unchanged, not newly implemented C# capabilities. Runtime/build/release/CI unchanged; only pure Core and test links evolve. No push, merge, release, new screenshot or game test.

## Restricted-region slice impact

Added [current contract](../../../docs/RESTRICTED_ROUTING.md), Core router/verifier and regression evidence; root current roadmap/migration/README/changelog link it. All tracked document groups reviewed: existing scope pages remain valid historical/local contracts; licenses, research, import/release records and old evidence unchanged; AGENTS/CONTRIBUTING apply without changes. No Lab production/UI change. Runtime, build, packaging and CI unchanged. New rectangle scope does not claim soft collision or merge-exemption support.
