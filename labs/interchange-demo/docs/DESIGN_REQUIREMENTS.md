# 第一阶段设计需求 / Phase 1 Design Requirements

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 有限游戏外二维/三维及纵断面子集已经实现，完整一期与游戏门禁尚未通过。 / A bounded offline plan/3D/profile subset is implemented; full phase/game gates are not accepted. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

状态 / Status: **DRAFT — full requirements; bounded offline subset implemented separately; not game-tested.**

本阶段是业余研究项目的需求基线，不是承诺完成全自动立交生成器。中文与英文从首阶段并列支持。 / This is a hobby research requirements baseline, not a claim of a working automatic interchange generator. Chinese and English are first-phase requirements.

## 目标与边界 / Goals and boundaries

面向普通玩家先提供易理解的“选择道路—表达流向—预览—检查—确认”流程，进阶用户再展开参数。支持曲线与斜交而非仅正交模板。视觉美观、几何合法、网络可达、交通性能分别验收。 / Use a guided select–intent–preview–validate–confirm flow, with optional advanced controls. Curved and skewed roads are core cases. Appearance, geometry, reachability and traffic performance are separate acceptance layers.

第一阶段不承诺任意复杂路网一次生成、零拥堵、真实道路安全认证、自动修复任意存档或全部Mod版本兼容。 / No promises of arbitrary-network generation, zero congestion, engineering certification, universal save repair or all-version compatibility.

## 必须需求 / Must-have requirements

|ID|需求 / Requirement|验收 / Acceptance|
|---|---|---|
|R01|选择两条有方向的道路走廊，识别每方向道路与车道；“6车道”先说明单向6还是双向总6。 / Select directed corridors and declare whether six lanes means one direction or total.|歧义未解决不能生成；显示路向箭头与车道数。 / Block ambiguous inputs; show arrows and counts.|
|R02|6→4+2等逐车道分流；支持声明新增/终止/辅助车道。 / Explicit lane mapping for 6→4+2, with declared lane additions, drops and auxiliary lanes.|在无增减的示例，6条输入唯一分配到4条主线和2条匝道；共享出口车道另设模式，不偷偷一对多。 / Exact mapping in the pure split; optional shared lanes are explicit.|
|R03|4+2→6与4+2→4等不同合流策略。 / Distinguish lane addition from tapering merges.|显示谁让行、哪里变道及车道终止，不以算术相等保证容量。 / Show yielding, transitions and lane drops; counts do not certify capacity.|
|R04|两条任意平面曲线/斜交/错位与高程。 / Curved, skewed and offset corridors with elevations.|用局部切向和道路边界构造接入，不旋转正交模板硬套。 / Derive attachments from local tangents and road envelopes.|
|R05|沿道路弧长选择入口/出口接入位置。 / Locate attachments by distance along the road.|移动控制点后重算曲线、连接与约束；保持锁定的路段不被隐式修改。 / Recompute geometry and constraints while respecting locks.|
|R06|全互通、部分互通、自定义流向。 / Full, partial and custom movements.|固定所选走廊端口的全部合法非掉头跨走廊转向，并核主线直行；删除任一必需流向自动变为部分/自定义。 / Full access has a fixed movement universe; removing a required movement changes the mode to partial/custom.|
|R07|桥上/桥下、坡度、净空和地形约束。 / Grade separation, slope, clearance and terrain.|显示测量位置与依据；无碰撞开启仍检查实体包络和可达性。 / Report measured envelopes and provenance even with Anarchy.|
|R08|双语实时预览与原因解释。 / Bilingual live preview and explanations.|拓扑/车道/高程/冲突可切换；错误指出对象、原因、可选修正。 / Layers and actionable object-specific diagnostics.|
|R09|可逆生成、取消、Undo及事务边界。 / Reversible generation and explicit transaction boundaries.|预览不改存档；失败不得静默半生成。若引擎无法原子回滚，必须先证实备份/恢复方案，否则禁用Apply。 / No preview mutation; verified rollback or disable Apply.|
|R10|依赖、版本与外部编辑失效管理。 / Dependencies, versions and invalidation.|RB/Traffic/Move It改变道路后标预览过期并重新验证；不覆盖用户自定义连接。 / Revalidate stale previews; preserve custom connections.|
|R11|保存/重载/升级后仍可复现。 / Persistence and reproducibility.|记录输入、参数、精确版本及验证证据，副本存档复验。 / Record inputs, parameters, versions and evidence.|
|R12|第一阶段完整中英体验。 / Complete Chinese and English experience in phase 1.|按钮、教程、参数、警告、诊断、导出报告和关键文档都有对应文本，无缺失回退或截断。 / Cover UI, help, diagnostics, reports and key docs.|

## 基本/高级模式 / Basic and advanced modes

基本模式：选择道路、交通侧、入口出口车道数、互通流向、偏好（紧凑/平缓/少改动）、预览确认。高级模式：曲线控制、弧长定位、车道映射、坡度/净空目标、道路截面、节点和锁定区。偏好是目标，不是保证可行；若多目标冲突，提供候选与代价，不静默放宽约束。 / Basic mode captures intent and preferences. Advanced mode exposes geometry and lane controls. Preferences do not override hard constraints; show trade-offs or infeasibility.

## 第一阶段交付闸门 / Phase 1 gates

A. 需求、来源与交互原型审查（本轮仅文档）；B. 只读道路提取及预览；C. 独立存档中的单个分流/合流；D. 曲线互通和OD验证；E. 双语与Mod组合回归。每个实施阶段需另行推进，不把本需求提交记为实现。 / Review requirements first, then read-only preview, isolated split/merge experiments, curved movement validation and bilingual compatibility regression. This document implements none of those runtime features.

## 待定决策 / Open decisions

- 最终交付形式：游戏内交互Mod、可复用路型/布局、存档示例，或分层组合；先查当前官方与上游API支持，不预选不可用接口。 / Delivery format remains subject to API feasibility.
- Placemaker 暂指 CS2 的 ockidj/Placemaker，而非同名其他产品；等待用户纠正。 / Reference identity is provisional.
- 当前确切游戏与Mod版本、合法云游戏环境、可回滚编辑API均待验证。 / Runtime versions, cloud game environment and rollback APIs remain unverified.
- 不承诺所有数值阈值；每个阈值需注明引擎限制、Mod行为或项目设计目标。 / Distinguish engine limits, mod behavior and design targets.

## 支持域与提交前复核 / Supported domain and pre-apply checks

首个可实现里程碑限定两条普通机动车道路走廊、开放地面/桥梁场景及明确车道类型；分离双幅道路按同一走廊配对。铁路、有轨电车、公交/自行车专用车道、隧道复杂接入、三条以上走廊及自定义脚本路型先标 UNSUPPORTED，后续逐项扩展；不支持时显式拒绝，不能换成普通车道偷偷生成。 / The initial implementation domain is two ordinary motor-road corridors with ground/bridge geometry and known lanes. Unsupported modes must fail explicitly, not be silently substituted.

需求中覆盖复杂场景，是为了定义识别/拒绝/后续扩展，不代表第一版都能生成。 / Recognizing a difficult case does not promise generation support.

Apply之前重新读取源道路ID、版本、依赖和用户编辑状态，与预览快照比较；不同即阻止Apply并重算。不能仅标过期却仍应用旧结果。 / Re-read source state before Apply; stale previews cannot commit.

“适合大多数人”当前是目标假设，不是调研统计结论；后续以普通玩家任务成功率、错误恢复和教程反馈验证。 / Broad usability is a hypothesis to validate, not a measured claim.

## 现实感与运行补充 / Realism and operation
新增R13–R20见 [现实感与游戏实际](REALISM_AND_GAMEPLAY.md)：服务/系统型、主线识别、驾驶视角、下游回堵、集散、结构全包络、地方/服务可达性及诚实交通评价。均为未实现需求。 / Additional requirements remain unimplemented.

## 两曲线与用地核心扩展 / Curved-site core requirements
R21–R26在 [场地优化契约](CURVED_SITE_OPTIMIZATION.md) 中定义：曲线×曲线、可用地块/禁建区、城区/郊区预设、独立占地/高度/层数边界、非支配候选和可复现指标。仍未实现，搜索失败不等于证明无解。 / Core design requirements, not implemented features.

## 高差与完整连接预览 / Elevation and complete preview

|ID|Requirement / 要求|Acceptance / 验收边界|
|---|---|---|
|R27|独立 z(s)、方向坡度、接入三维切向和解析极值 / Independent station-based elevation, travel grades and critical points|有限 Hermite V1；零长/异常拒绝，overshoot 必报；未知地形/实体净空不通过 / Bounded V1, no structural/game claims|
|R28|同一可旋转/平移/缩放场景展示两主线及全部拟连接 / Complete interactive connection scene|OD 高亮入口—连接—出口；full/partial/参数修改同步；纵断面和俯视切换，中英与保存重载 / No independent fork substituted for full preview|

Current implementation scope and evidence: [profiles](VERTICAL_PROFILES.md), [scene](THREE_D_DESIGN.md), [validation](OFFLINE_VALIDATION.md). Not completion of every original phase/game gate.

## 深度案例补充 / Deep-case requirements

R29：按 [深度案例计划](DEEP_CASE_PLAN.md) 冻结H1–H5假设、独立真值、材料来源和误差；优先一个完整案例，不以功能/测试数量验收。R30：拓扑先行、平纵横联合、独立重建检查及独立游戏门槛。 / Freeze falsifiable hypotheses, provenance/errors and independent oracles; prioritize one integrated case with topology-first joint geometry, reconstructed validation and separate game gates. Both requirements are DESIGN ONLY / NOT_RUN.

R31 / P0：用户道路消失只余桥墩报告要求未来所有权账本、异步收敛、真实道路/依赖验证和幂等恢复；按 [M11-A–E](research/road_mod_lessons.md) 及A45验证，未知根因不得写已修复。 / Future ownership and recovery contract; runtime NOT_RUN, cause UNKNOWN.
