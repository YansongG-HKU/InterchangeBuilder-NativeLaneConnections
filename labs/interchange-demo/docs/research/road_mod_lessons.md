# 修路 Mod 的机制与反例 / Road-mod mechanisms and counterexamples

研究日期 / Research date: 2026-10-26 UTC. 本文仅来源分析与后续验收设计，无Mod安装、游戏执行或代码移植。 / Source analysis and proposed acceptance only; no mod installation, game execution or code port.

CS1链接来自作者master读取快照，未取得可靠固定SHA；动态URL不代表固定发布版本或当前CS2能力。TM:PE文档有单独的历史版本标注。 / CS1 sources are dynamic author-master snapshots without a verified commit SHA, not pinned releases or evidence of CS2 capability. TM:PE documentation carries its own historical version label.

## 四条能力链 / Four capability chains

| ID / 来源级别与来源 / Evidence level and source | 已读机制与启示 / Observed mechanism and lesson | 迁移边界及可证伪验收 / Boundary and falsifiable acceptance |
|---|---|---|
| M01 — 作者说明与源码 / Author description and source: [Workshop description](https://github.com/MacSergey/NetworkMultitool/blob/master/Workshop/Workshop%20description.txt), [CreateCurve](https://raw.githubusercontent.com/MacSergey/NetworkMultitool/master/NetworkMultitool/ToolModes/ConnectionModes/CreateCurve.cs), [BaseCreate](https://raw.githubusercontent.com/MacSergey/NetworkMultitool/master/NetworkMultitool/ToolModes/ConnectionModes/BaseCreate.cs) | Network Multitool区分曲线—直线—曲线、平滑曲线及等坡；端部水平切向/方向与中间弧长采样参与生成，预览/建造共用SetSlope/SetTerrain。 / Separate horizontal tools and elevation handling; share the solved preview/build geometry, carry port direction and stationing. | Calculated、资金检查和模拟队列不证明原子事务。NetInfo/NetSegment/ushort/SimulationManager不是CS2接口。M01反例：合成12m高差、100m水平路程、8%坡度上限且无绕行空间须拒绝；取消预览实体数不变。 / Queueing is not rollback; the synthetic grade case must fail and cancel must leave entities unchanged. NOT_RUN. |
| M02 — 作者源码 / Author source: [SegmentEndData](https://raw.githubusercontent.com/MacSergey/NodeController30/master/NodeControllerRenewal/Manager/SegmentEndData.cs) | Node Controller Renewal记录左右边缘、Flat/Slope/FreeForm和几何bounds约束的偏移/旋转；样式能力不同。启示端口必须包含截面及左右边界有效区间。 / Ports need cross-section and edge validity, not only center points. | CS1 NetAI样式和序列化不能当CS2端部编辑API。窄匝道接宽主线并增加旋转，必须拒绝或明确显示限幅，不能路缘交叉仍有效。 / Reject or explicitly bound the narrow-ramp/wide-mainline rotation case; crossed edges cannot pass. NOT_RUN. |
| M03 — 作者历史文档 / Author historical documentation: [TM:PE lane connectors](https://doc.tmpe.me/lane-connectors.html), page verified against 11.5.2/11.6, 2021-01 | connector与lane arrows不同；无connector的incoming可回退箭头规则，不是关闭；旧路径与新路径可能不同。 / Explicit connections differ from arrows and unset differs from forbidden; distinguish old/new paths. | 这些回退不自动成为CS2事实。适配器需区分default/explicit/forbidden；删除唯一出口边，即使网格/标线完整，独立可达性必须失败；游戏观测分别记录新旧路径。 / Do not assume CS2 fallback behavior; a removed sole exit must fail independent reachability. NOT_RUN. |
| M04 — 作者源码 / Author source: [Move It TransformAction](https://github.com/Quboid/CS-MoveIt/blob/master/MoveIt/Actions/TransformAction.cs) | BaseTransformAction保存状态，Do/Undo先非路段后路段，处理桥墩替身、刷新及实例替换。启示记录原状态、创建对象、依赖及ID映射。 / Capture state/dependencies and replacement mappings; distinguish cancel, failure compensation and undo after success. | Virtual不证明隔离，也不保证CS2恢复机制。带桥墩生成后撤销须恢复实体与接入口拓扑；外部替换端口后不得以旧ID修改无关对象。 / Undo must restore pier/port topology; externally replaced ports require conflict detection. NOT_RUN. |

M01数值只用于合成实验，绝非现实工程标准或游戏硬限。上述链条均未证明CS2原子批量建路、完整Undo、稳定lane connector API或节点边缘编辑；后续须单独适配实验。 / Synthetic parameters are not engine limits. None of these sources establishes CS2 atomic road creation, full undo, stable lane-connector APIs or edge editing.

## 已确认旧项目 / Confirmed prior project

用户已确认指 [InterchangeBuilder-NativeLaneConnections](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections)。 / User confirmed the repository identity.

研究必须区分main `9f083061cdcf2bb08ed03c0527ebbd6f591c1179` 与未合并PR #4 head `0e9b34492f82bc46f35a9c7ce10f334e2ce76eef`。PR #3/#4描述与测试数量不等于main已实现或游戏通过；只读研究，不修改该仓库，不复制代码。 / Distinguish pinned main and unmerged PR heads; PR claims are not merged behavior or verified game results. Read-only research, no code copying.

来源级别：以下均为固定源码或未合并PR的只读分析；测试数量为作者自报，未独立重跑、未游戏复现。main自报12算法+7metadata测试，PR4自报29测试。main的MIT许可不改变本仓许可；本次无代码复制。 / Evidence level: pinned source/unmerged proposals; reported test counts are not independently reproduced. The prior repository's MIT license does not license this project.

| ID | 固定证据与边界 / Pinned evidence and limits | 未来反例 / Proposed adversarial acceptance (all NOT_RUN) |
|---|---|---|
| M05 | [main AlignmentMath L51–96](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/9f083061cdcf2bb08ed03c0527ebbd6f591c1179/src/InterchangeBuilder.LaneConnections.Core/AlignmentMath.cs#L51), [EndpointAlignmentService L174–197](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/9f083061cdcf2bb08ed03c0527ebbd6f591c1179/src/InterchangeBuilder.LaneConnections/EndpointAlignmentService.cs#L174)：宽度左右偏移、中心/格网/自由候选及NetUtils.CanConnect；L71–77特殊网络保留原逻辑。1.6m/8m不作为普适阈值，几何对齐不证明车道连通。 / Alignment candidates/native connection checks are not lane reachability or universal thresholds. | 镜像/反转方向、特殊网络必须保留语义；对齐而断连的负例必须失败。 / Preserve direction/special-network semantics; aligned but disconnected lanes fail. |
| M06 | [main ConnectionSelectionStore](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/9f083061cdcf2bb08ed03c0527ebbd6f591c1179/src/InterchangeBuilder.LaneConnections/ConnectionSelectionStore.cs), [Patches L335–386](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/9f083061cdcf2bb08ed03c0527ebbd6f591c1179/src/InterchangeBuilder.LaneConnections/Patches.cs#L335)：snapshot到CoursePos，NodeSplitOnly不改；XZ1m匹配不是稳定版本身份。 / Snapshot matching by planar proximity is not stable identity. | 旧snapshot、近邻端口和上下重叠端口不可串用。 / Stale snapshots and nearby/stacked ports must not alias. |
| M07 | [PR4 RoadSelectionStateMachine](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections.Core/RoadSelectionStateMachine.cs)：Selected/StartSource分离及entity index/version/revision；[EndpointAlignmentService](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/EndpointAlignmentService.cs)使用composition/default lanes/端点flow，但宽度仍prefab优先。 / Selection identity and endpoint metadata require independent validation. | 选B后点A起点仍应输出B；切换/动态重建必须失效旧快照；注入prefab宽度与动态metadata不一致。 / Preserve selected road B, invalidate rebuilt identities and detect inconsistent width metadata. |
| M08 | [PR4 AnarchyCompatibility](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/AnarchyCompatibility.cs)：反射可选接口、只读toggle，无接口回退原版；不是类型/方向绕过许可。 / Optional reflection/toggle discovery is not permission to bypass type/direction checks. | 无接口、关闭toggle与不支持道路类型均应保守处理，不替用户开启Anarchy。 / Fall back conservatively without changing the user's toggle. |
| M09 | [PR4 RouteGraphResolver L82–93](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections.Core/RouteGraphResolver.cs#L82), [PlacementRecoveryPatches L68–130](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/PlacementRecoveryPatches.cs#L68)：XZ双向图、6m端点容差与固定8段采样只是链归属/恢复启发式，不是有向车道通行或3D净空证明。 / Planar bidirectional proximity recovery is not directed lane or clearance verification. | 平行近邻、上下叠置、反向单行、6m内错误端点必须不能冒充正确道路；保守保留待复查不等于几何/车道通过。 / Nearby, stacked, reversed and wrong endpoints cannot certify ownership or validity. |
| M10 | [PR4 LanePortMath L294–327](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections.Core/LanePortMath.cs#L294)：距离贪心并分别排序两端ordinal，不是权威配对列表或全局最优。 / Greedy matching plus independent ordinal sorting is not an authoritative pair oracle. | 显式fromLane/toLane；用小规模穷举独立基准及贪心反例核验，不把独立排序后同索引强配。 / Require explicit pairs, exhaustive small independent oracles and greedy counterexamples. |

[PR #3](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/pull/3) 与 [PR #4](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/pull/4) 的道路选择、侧偏路由与timeout清理改动属于提案层，不混入main事实。 / Proposal history is separate from main and runtime observations.

## P0：用户报告修路失败、道路消失、只剩桥墩 / User-reported failed construction, disappearing roads and remaining piers

用户直接观察如此；具体游戏/Mod版本、存档、日志和最小操作尚缺。不能从补丁直接确定根因。 / This is a direct user report; exact versions, save, logs and minimal steps are missing, so causality is unconfirmed.

旧仓公开树缺基础CurveRoadApplyService实现，只见BeginFailure补丁，未见桥墩owner/生命周期实现。PR4仅在特定英文timeout前缀、非空候选且有offset snapshot时把清理实体参数置null；这是条件性止损，不能证明覆盖所有失败路径、事务恢复或已修复该现象。 / The public tree lacks the underlying apply service and pier ownership/lifecycle implementation. The guarded cleanup suppression handles only specific timeout text/candidates/snapshots, not general transaction recovery or a confirmed fix.

M11 / P0须分别检验路段创建失败、路面mesh未呈现、迟到创建、归属误判误删、道路与桥墩分离清理等假设。注入部分成功、延迟跨超时、错误归属、删除失败、重载恢复、非该英文错误消息、无snapshot、桥墩独立生命周期及人工改动。 / Separately test creation failure, missing mesh, late creation, ownership misclassification/deletion and separated road/pier cleanup; inject partial success, late completion, wrong ownership, deletion failure, reload, other messages, absent snapshots, independent pier lifecycle and manual edits.

创建成功必须读回实际道路与依赖，桥墩存在或预览可见均不足。创建/验证/清理依据明确所有权与依赖，禁止邻近扫描乱删、盲删桥墩、仅加timeout或开启Anarchy掩盖。保留原道路；新增孤立附属物提供可解释处置。分别报告FAILED、RECOVERY_REQUIRED、VERIFIED及验证范围；VERIFIED必须完成当前阶段所要求的读回，保守保留待检查不能标它。 / Read back roads/dependencies; only owned objects may be recovered. Preserve originals, explain orphan handling and distinguish failed, recovery-required and verified states. Placement/rendering or conservative retention is not verification.

M11关联H5/D4，也是未来实现前的P0故障回归设计。真实游戏复现NOT_RUN，根因UNKNOWN，未宣称bug已解决。 / This is a priority recovery regression contract, not an executed fix; runtime reproduction is NOT_RUN and root cause UNKNOWN.

## 与深度计划关系 / Relation to the deep plan

M01→H1/H4/D1；M02→H1/H4；M03→H2/H3/H5；M04→H5/D4，见 [深度计划](../DEEP_CASE_PLAN.md)。这些是新验收设计，全部NOT_RUN。通过源码阅读形成反例不等于完成独立检查器，也不替代真实车辆、存档或人工工程审查。 / These mappings are planned gates, all NOT_RUN; reading source is not an implemented independent checker or runtime evidence.

M05–M10进一步约束H1/H2/H3/H5；M11约束H5/D4及执行前P0恢复设计。 / Prior-project lessons constrain the same frozen hypotheses; all new acceptance cases remain unexecuted.

## P0未来适配契约 / Future adapter recovery contracts

进一步固定证据：[PR4清理保护L132–175](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/PlacementRecoveryPatches.cs#L132)、[Bootstrap](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/Bootstrap.cs)、[ConnectionSelectionStore](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/blob/0e9b34492f82bc46f35a9c7ce10f334e2ce76eef/src/InterchangeBuilder.LaneConnections/ConnectionSelectionStore.cs)。Bootstrap挂BeginFailure/TryResolveSubdividedChain；HasOffsetPort要求偏移>0.05m并依赖prefab/role/XZ匹配。这不是所有失败路径保护。 / These specific hooks and offset conditions are not universal failure protection.

| 契约 / Contract | 要求、反例及状态 / Requirement, counterexample and state |
|---|---|
| M11-A 所有权 / Ownership | 独立operation/transaction ID与对象账本，记新建/修改/借用、entity index+version及真实依赖，不凭空间距离认领。近邻相似对象、ID复用、并发操作不串清理，归属不明为RECOVERY_REQUIRED。 / Use operation IDs and ownership/dependency ledgers; proximity, reused IDs and concurrent work cannot establish ownership. NOT_RUN. |
| M11-B 异步状态 / Async state | requested/pending/created/verified/recovery-required/rolled-back明确，超时本身非失败证据；晚到可归属并收敛。注入先墩后路、先路后墩、超时后成功、重复回调及取消后晚到。 / Timeout alone is not failure; reconcile ordered, delayed, duplicate and post-cancel results. NOT_RUN. |
| M11-C 创建验证 / Creation verification | VERIFIED要求实际道路、prefab、拓扑和支撑关联一致；区分实体不存在与渲染不可见。只有桥墩不能算成功。 / Read back road/prefab/topology/support consistency; distinguish absent entities from invisible rendering. NOT_RUN. |
| M11-D 补偿 / Compensation | 幂等且仅限确认所有权对象；按游戏实测依赖顺序，不假定删除传播。覆盖逐步骤异常、重复恢复及恢复中保存重载；原世界不误删，无法清理保留账本与警告。事务仅应用级管理，不承诺游戏原子性。 / Idempotent owned-object compensation with measured dependency order, reload recovery and explicit unresolved ledger; no engine atomicity claim. NOT_RUN. |
| M11-E 分离判据 / Separate predicates | 归属启发式、车道通行、3D安全分开；结构化错误码代替英文字符串。覆盖XZ重叠高架/平行路/逆向/缺边/无snapshot/零偏移失败/timeout文本变化。 / Ownership, directed reachability and spatial safety are independent; test structured errors and adversarial geometry/context. NOT_RUN. |

最小待采材料：失败前后存档、精确游戏及实际安装Mod版本/hash、道路配置/Anarchy状态、最短操作、日志和短视频；未来授权环境再取逐帧实体差分、owner与创建/清理事件。中心/侧偏、地面/高架、原版/动态、Anarchy开关逐项单变量对照。 / Missing evidence includes before/after saves, exact installed versions/hashes, configuration, minimal steps, logs/video and later authorized entity/ownership event traces. Use controlled one-variable comparisons.

上述是复现缺口清单，不要求用户立即操作电脑，也不授权访问其电脑。所有M11契约尚未游戏验证，不能写作已解决“只剩桥墩”。 / This is an evidence-gap inventory, not a request to access the user's computer or a claim of a fixed bug.
