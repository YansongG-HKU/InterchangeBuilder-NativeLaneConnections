# 面向《城市：天际线2》的曲线道路立交生成与独立验证
# Curved-road interchange generation and independent validation for Cities: Skylines II

**以一个完整案例打通逐车道拓扑、三维几何和游戏映射。 / Connect lane-level topology, 3D geometry and game mapping through one complete case.**

状态：用户已批准单左转样例的后续分阶段实施，先整合、待材料冻结；本轮未开始算法。当前唯一执行计划见 [根计划](../../../docs/LEFT_TURN_PLAN.md)，本文保留深度验证方法。 / Approved for staged subsequent execution, integration first and evidence freeze pending; no algorithm implementation in this revision. The root plan is authoritative.

现有基线为 `d6716995c8dd9954cf47525dc45a3e976dcadaa6`：有限三维连接预览、单段 Hermite 高程、独立六车道实验。37 核心、81 二维、37 三维及13定位断言是该 Demo 的历史证据，不证明下述工作完成。 / Existing bounded demo evidence does not establish the new lane, structure or game requirements.

## 1. 深度优先与停止条件 / Depth and stopping rules

不以功能、模板、面板、代码或测试数量衡量进步。首个完整案例需要解释每条车道为何可达、每个接入为何连续、每个实体重叠为何安全或未知，以及对应游戏对象能否读回。暂不扩展立交类型。 / Progress is a defensible complete case, not feature or test counts. Explain every lane path, attachment, structural overlap and game read-back before broadening types.

先冻结案例清单、独立真值、输入域和误差预算；任何更改均形成新实验版本，保留旧失败，不事后放宽。无资料、无环境或无可验证映射时停止对应结论，独立开展资料准备。 / Freeze the case, oracle, domain and error budget before generation; revisions create new experiments and preserve failures. Missing evidence blocks the relevant conclusion, not unrelated preparation.

## 2. 可证伪假设 / Falsifiable hypotheses

| ID | 假设与预先定义的失败 / Hypothesis and prior failure criterion | 证据 / Evidence |
|---|---|---|
| H1 | 选定曲线表示能在冻结误差内表达基准平纵横；超预算、退化或局部尖点则失败。 / The chosen representation fits the reference within frozen tolerances; excess residual, degeneracy or cusps fail. | 原始线形、拟合残差及收敛记录 / Original alignment, residuals and convergence |
| H2 | 输出实际车道图等于预先声明的可达/禁行集合；缺路、逆向或额外禁行连接均失败。 / Reconstructed lane reachability matches the independent required/forbidden oracle; missing, reversed or forbidden edges fail. | 独立车道图与端到端路径 / Independent lane graph and end-to-end paths |
| H3 | 单向6→4+2在完整互通内保持编号、方向、接收车道与分合流语义；算术或孤立实验通过不足。 / Integrated 6→4+2 preserves identity, direction, receiving lanes and merge/diverge semantics; arithmetic or an isolated fork is insufficient. | 逐车道映射、渐变、交织及接收能力检查 / Lane mapping, taper, weaving and receiving-lane checks |
| H4 | 全宽结构/通行包络及地形满足冻结约束；穿插或不足为失败，缺梁厚/横坡/地形等为UNKNOWN。 / Full-width structure, traffic envelope and terrain satisfy frozen bounds; intersections or shortfalls fail, missing structure/terrain stays UNKNOWN. | 完整结构、截面及最小间距复算 / Complete solids, sections and independent minimum-distance checks |
| H5 | 游戏导出再读回保留车道/节点/方向，并通过车辆、重载与编辑撤销；不一致即失败，无环境为NOT_RUN。 / Export/read-back preserves lanes, nodes and directions and survives vehicle, reload and edit/undo tests; mismatch fails, absent runtime is NOT_RUN. | 固定版本日志、对象快照、操作与存档 / Fixed-version logs, object snapshots, operations and saves |

数值容差尚未冻结，不在此编造数值。每个容差必须写明单位、参考来源、测量不确定性、算法离散误差和失败比较规则，并在生成前审批/冻结实验版本。 / Numeric tolerances remain unset: document units, provenance, measurement uncertainty, discretization error and failure comparisons before freezing the experiment.

## 3. 材料、真值与来源 / Materials, oracle and provenance

优先主管部门平面、纵断面、横断面图及可追溯地形；分别记录坐标系、标高基准、单位、日期、精度、许可和材料哈希。尺寸标为实测、来源给定、推定或未知。影像不能冒充高程真值。无可用真实资料时明确使用合成基准，保留精确构造过程，不暗示现实案例复刻。 / Prefer agency alignment/section drawings and traceable terrain; record coordinate/datum, units, date, accuracy, rights and hashes. Classify dimensions as measured, source-given, inferred or unknown. Imagery is not elevation truth; use an explicitly synthetic reproducible benchmark when necessary.

另建游戏材料表：CS2 build、道路资产ID/版本、Road Builder/Anarchy精确组合、实际节点/路段/车道、日志和合法可分享存档。研究Mod原始源码时固定commit、文件及许可；一代机制与二代接口分开。尚未取得材料不得填作已验证。 / Separately inventory exact game/build/assets/mod versions, actual nodes/segments/lanes, logs and shareable saves. Pin source commits, files and licenses; distinguish CS1 mechanisms from CS2 APIs. Missing materials remain missing.

生成器运行前，由需求与独立材料建立预期车道/OD表、禁行表、分流/合流表及约束表；不得从输出反推正确答案。现实参考、游戏实测限制和实验参数必须分列。 / Before generation, derive required/forbidden lane/OD and merge/diverge tables independently from requirements and reference materials. Do not derive the oracle from generated output; distinguish real-world references, measured game limits and experimental parameters.

## 4. 生成与独立检查 / Generation and independent checking

先明确车道拓扑，再联合平纵横设计：主线连续、分流、渐变、接收车道、合流与交织同时考虑。禁止把所有端口直接连曲线再抬高来掩盖冲突。当前中心线/路面预览不能替代全路幅车道拓扑。 / Establish lane topology before jointly designing plan, elevation and cross-sections. Preserve mainline continuity and account for tapers, receiving lanes, merges and weaving. Connecting all ports and lifting curves is not a conflict solution; current centerline/pavement previews are insufficient.

生成器输出可审计中间表示；检查器从输出的实际车道、端点、方向和几何重建图，不信任“connected”标记，不调用同一生成逻辑来制造预期值。展示、检查及导出使用同一版本数据，但独立检查实现不得只复述生成断言。 / Emit auditable data; the checker reconstructs graph/geometry from actual lanes, endpoints and directions, ignoring generator connectivity flags. Display, checking and export share versioned data, while checking logic and expected results remain independent.

验证包含解析基准、独立复算、性质、镜像/旋转、扰动、采样收敛和冻结后盲测。注入断连、逆向及穿插负例，检查器必须拒绝并定位。有限样本不等于普遍证明；报告覆盖域和未支持域。 / Use analytic benchmarks, independent calculations, properties, mirror/rotation, perturbation, convergence and held-out cases. Inject disconnections, reversals and interpenetration; require detection/localization. Finite samples do not prove universality.

同一基准改变交角、高差、端点坡度及场地，检验城区/郊区偏好和full/partial固定OD。不得暗删OD或车道、改变分母或放宽硬约束；搜索未找到与不可行证明分开。 / Vary angle, elevation, endpoint grade and site on the same benchmark, comparing urban/suburban preferences and full/partial fixed OD. No silent demand/lane removal or constraint relaxation; distinguish search failure from proof of infeasibility.

## 5. 三维与游戏门槛 / Visualization and game gates

三维用于验证：选一条车道追踪入口到出口，查看对应横/纵断面、冲突与误差位置；不得用漂亮动画替代证据。Three.js和浏览器通过仅说明该展示操作，SUMO如采用仅为可选独立对照，均不证明CS2兼容。 / Trace a selected lane end to end, inspect sections and locate conflicts/errors. Rendering and browser success are not game validation; optional SUMO comparisons are not CS2 equivalence.

游戏门槛独立：固定版本组合下实际创建对象，读回车道图，观察车辆通行，保存重载、编辑和撤销后复查。无碰撞放置只说明允许摆放；无环境明确NOT_RUN。本轮不安装游戏或Mod。 / Independently create/read back game objects under a frozen version combination; test vehicles, save/reload, edits and undo. Collision bypass proves only placement permission. No runtime means NOT_RUN; no game/mod installation in this revision.

## 6. 有界执行顺序与验收 / Bounded sequence and acceptance

| 门槛 / Gate | 可审查交付与失败条件 / Reviewable deliverable and failure | 当前状态 / State |
|---|---|---|
| D0 材料与冻结 / Evidence freeze | 材料索引、假设/误差表、独立真值、算法依据；来源或冻结标准缺失则不进入生成。 / Materials, hypotheses/errors, oracle and algorithm rationale; missing evidence blocks generation. | NOT_RUN |
| D1 完整案例 / Complete case | 集成逐车道6→4+2与三维几何；H1–H4逐项有独立证据；UNKNOWN不得算通过。 / Integrated lane case and 3D geometry, independent H1–H4 evidence; UNKNOWN is not pass. | NOT_RUN |
| D2 反例及变体 / Negatives and variants | 负例必须被拒绝；同基准扰动/城市郊区/full partial及收敛可复现。 / Detect injected failures; reproduce variants and convergence. | NOT_RUN |
| D3 独立复现 / Independent reproduction | 第三方干净环境复现及工程人工审查分别留证；第二个AI浏览不是替代。 / Separate third-party clean-environment reproduction and human engineering review; another AI glance is insufficient. | NOT_RUN |
| D4 游戏映射 / Game mapping | 固定组合实际车道、车辆、重载、编辑撤销证据；失败或无环境不准兼容声明。 / Actual lanes, vehicles, reload and edit/undo evidence; missing or failed runtime blocks compatibility claims. | NOT_RUN |

证据包必须包含原始材料索引及许可、假设/误差表、独立真值、算法依据、源代码精确版本、正反例、检查器、真实截图与操作记录、复现命令和未完成项。检查通过的含义按门槛分别报告，不能合成“game-ready PASS”。 / Evidence pack includes materials/rights, assumptions/errors, independent oracle, algorithm rationale, exact source, positive/negative cases, checker, real screenshots/operations, commands and remaining work. Report each gate separately, never an inferred game-ready PASS.

用户已批准后续分阶段工作；完成本轮整合及材料冻结后依根计划推进。回滚本轮文档提交即可恢复旧计划，不改变既有Demo源码或证据。 / Staged execution is approved; integration and evidence freeze precede implementation. Reverting this documentation revision restores the previous plan without changing demo code/evidence.

## 7. 参考机制与P0故障契约 / Reference mechanisms and priority failure contract

[CS1及已确认旧项目研究](research/road_mod_lessons.md) 将作者源码、固定main、未合并PR和用户观察分层，形成M01–M11可证伪反例。用户报告修路失败、道路消失而只余桥墩：根因UNKNOWN、真实复现NOT_RUN。缺基础放置服务/桥墩生命周期源码、实际安装版本、日志和存档，不把条件性timeout止损认作完整恢复。 / Source levels and the user-reported disappearing-road/orphan-pier symptom are kept separate; missing implementation/runtime evidence prevents a root-cause or fix claim.

P0必须在适配设计中落实M11-A–E：operation所有权账本、可收敛异步状态、道路及依赖实际读回、幂等且按实测依赖顺序的补偿、归属/车道/空间判据分离。覆盖部分成功、迟到/取消后返回、错误归属、删除失败、ID复用、并发、人工改动及恢复中重载。禁止邻近扫描乱删、盲删桥墩、仅延长timeout或开启Anarchy掩盖。 / Prioritize ownership, asynchronous reconciliation, read-back, idempotent compensation and independent predicates with fault injection; preserve existing world objects. These are future adapter contracts, not an engine transaction guarantee or implementation.

M01–M10的方向/端口边界/default-explicit-forbidden、状态快照、动态metadata、XZ归属和贪心配对反例纳入D0冻结清单。D4不得以预览、桥墩存在或保守保留替代VERIFIED。 / Freeze reference-derived adversarial cases before implementation; preview, piers or conservative retention cannot certify D4.

当前执行更新：用户已明确曲线功能落地，6→4+2不再作为本轮前置。[有限直尾实现](EXPLICIT_TAIL_EXPERIMENT.md) 是首个局部实验，不证明完整D0–D4案例已通过。 / Latest scope authorizes this local slice without the fork prerequisite; full gates remain incomplete.

## 2026-10-27 V2 local extension / 本地第二切片

Current additional capability: [automatic orthogonal crossings and actual curved context corridors](EXPLICIT_TAIL_PHASE2.md). V1 statements about unsupported intermediate generation remain true for its missing-thickness/unsupported-shape inputs; V2 adds a constrained automatic family, not arbitrary3D routing. Full same-version regression and independent-review evidence are separate in evidence-tail-v2. Game/Anarchy/C#/remote CI remain NOT_RUN; no push/PR change. / 新增有界自动上跨、真实主路走廊与导入竞态检查，不改稳定Mod。

Current direction / 当前目标: [algorithm validation → Mod migration](../../../docs/MOD_CURVE_MIGRATION.md). The Lab is only a curve calculation/comparison/test harness. Current algorithm verification extends rigid-direction invariance and bounded curved/oblique profiles; older +X/orthogonal-only statements document V2, not the current superset. Pure C# candidate construction remains disabled/unwired; independent local planar actual-output validation is now implemented (see the root migration contract). External-road/vertical C# checks and game build/apply remain unimplemented. No new Demo product features. Standard netstandard2.0 build and 13 xUnit tests now pass, including 192 detailed contracts; net48/Anarchy/game/remote CI remain NOT_RUN. Historical execution records are preserved.
