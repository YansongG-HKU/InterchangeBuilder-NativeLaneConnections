# 开发与提交纪律

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 工具链为 Node 24、纯 ES modules、Vite、polygon-clipping、Node test 与 Playwright；复现命令及支持域见 Demo 文档。 / Use the documented local tests/build; use topic/Draft PRs against the target Mod repository main; the source requirements branch is historical. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

## 全仓文档一致性
每次开发开始和每次提交前全面核对文档目录及新增文件。按 docs/DOCUMENTATION_INDEX.md 分类检查全部文档，并记录“已更新”或“无需修改及理由”。包括 README、AGENTS、贡献约定、道路规则、兼容矩阵、测试、路线图、来源、实验记录、变更记录、模板及未来新文档。

只要求修改受影响内容，不要求给每篇文档制造无意义变动。凡行为、接口、依赖、版本、状态、步骤或结论变化，必须在同一提交中更新其全部引用和说明。新增/删除/改名文档必须同步索引及内部链接。

## 工作流程
需求/假设 → 来源查证 → 小范围设计 → 独立实验 → 原版与 Mod 对照 → 保存重载/升级复验 → 全仓文档检查 → 提交/Draft PR → 审查。

- 初始准备提交可直接初始化主分支；后续采用主题分支，合并遵守授权。
- 提交消息建议 `docs:`、`research:`、`asset:`、`fix:`、`test:`，简要说明原因。
- 一个提交只包含可解释、可回滚的改动；不混入其他大学或主业项目。
- 每个 PR 必须包含对应文档影响记录、实测证据和未运行项目。
- 发布要记录游戏版本、Mod ID/版本、加载顺序（如有明确要求）、依赖、安装/卸载及回滚行为。

## 资源与安全
先云端资料和离线准备，游戏实测必须有合法游戏环境；不因环境缺失改用用户电脑。游戏存档先副本测试。未知软件执行、付费资源、新账号权限或发布协议按需获批。

## 完成定义
准备资料完成不等于资产完成；离线几何检查不等于网络测试；兼容一组版本不等于所有版本兼容；GitHub 提交不等于 Paradox Mods 已发布。

## 需求变更同步
设计变更必须同步 DESIGN_REQUIREMENTS、GEOMETRY_AND_LANES、UX_AND_I18N、REQUIREMENTS_ACCEPTANCE；中英文语义和状态一致。资料证据等级及未实现边界不得丢失。

交通结论必须注明控变量及数据能力；无法观测或不能控制需求时只作观察，不以低流量或空路宣称容量提升。现实参数不得无来源冒充游戏阈值。

场地优化记录面积集合定义与基线、单位、硬/软约束和候选集；改变这些语义须同步图例、双语、验收与数据记录。不能把有限搜索失败写成数学无解。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 深度方案执行边界 / Deep-plan execution boundary

本轮只将 [深度案例计划](DEEP_CASE_PLAN.md) 写入仓库。后续实施须获得明确执行授权并冻结材料/独立真值/容差；不追加算法或扩展类型。本轮回滚为撤销文档提交，Demo代码与历史证据保持原样。 / This revision is documentation only; subsequent implementation needs execution authorization and evidence/oracle/tolerance freeze. Revert the documentation commit to roll back.

当前维护及获批阶段以 [根计划](../../../docs/LEFT_TURN_PLAN.md) 和 [整合规则](../../../docs/INTEGRATION.md) 为准；不在源仓继续开发。 / Root plan governs current maintenance.

## 当前执行授权 / Current execution scope

最新用户已授权本地实现与验证 [Issue #10切片](EXPLICIT_TAIL_EXPERIMENT.md)，不推送或变更PR。此前“仅文档”描述是上一轮历史；当前游戏、发布与稳定运行时保护不变。 / Implement locally under the latest scope; do not push or mutate PRs.

## 2026-10-27 V2 local extension / 本地第二切片

Current additional capability: [automatic orthogonal crossings and actual curved context corridors](EXPLICIT_TAIL_PHASE2.md). V1 statements about unsupported intermediate generation remain true for its missing-thickness/unsupported-shape inputs; V2 adds a constrained automatic family, not arbitrary3D routing. Full same-version regression and independent-review evidence are separate in evidence-tail-v2. Game/Anarchy/C#/remote CI remain NOT_RUN; no push/PR change. / 新增有界自动上跨、真实主路走廊与导入竞态检查，不改稳定Mod。

Current direction / 当前目标: [algorithm validation → Mod migration](../../../docs/MOD_CURVE_MIGRATION.md). The Lab is only a curve calculation/comparison/test harness. Current algorithm verification extends rigid-direction invariance and bounded curved/oblique profiles; older +X/orthogonal-only statements document V2, not the current superset. Pure C# candidate construction remains disabled/unwired; independent local planar actual-output validation is now implemented (see the root migration contract). External-road/vertical C# checks and game build/apply remain unimplemented. No new Demo product features. Standard netstandard2.0 build and 13 xUnit tests now pass, including 192 detailed contracts; net48/Anarchy/game/remote CI remain NOT_RUN. Historical execution records are preserved.
