# 两曲线与场地占用核心需求 / Curved Corridors and Site Optimization

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 二维子集使用采样路面并差集与侧向预留，完整 P/G/R 与结构净空仍 UNKNOWN；三维/纵断面预览已实现。 / Offline subset uses sampled pavement set operations; complete envelopes remain UNKNOWN. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

状态：**完整需求与数学设计草案；有限离线子集另见上方，游戏实验未运行。** / Full design target; bounded offline subset documented above; no game validation.

## 核心目标 / Core goal

两条已有曲线不是固定十字模板的变形背景，而是生成方案的几何输入。用户选择的用地边界和空间偏好也必须在求解前进入约束，而不是生成后才显示面积。 / Existing curves and site boundaries are inputs to the design, not post-hoc decorations or statistics.

城区/郊区只是可修改的偏好预设，不是强制结构形式。郊区通常可接受更大展开、较少垂直堆叠；城区通常更重视用地，但不能自动无限加层、缩小弯道、减少必需流向或开启Anarchy。 / Urban/suburban presets express editable preferences, not mandatory topology or permission to violate constraints.

## 新增需求 / Requirements

|ID|要求 / Requirement|验收 / Acceptance|
|---|---|---|
|R21|曲线×曲线为核心支持情境 / Curve–curve as a core case|预览体现原主线弯曲；用弧长站点、局部切向和实际边界衔接，不暗中拉直。 / Preserve source curvature and use tangent-aware attachments.|
|R22|明确场地与禁建区 / Explicit site and exclusions|可选/绘制可用范围、保留地块、建筑/水体等约束，分开硬禁建与软偏好。 / Distinguish hard exclusions and preferences.|
|R23|城区/郊区/自定义预设 / Urban, suburban and custom|列出预设改变的参数；同场地可手动覆盖，不按标签替用户决策。 / Expose changes and allow overrides.|
|R24|占地与堆叠约束 / Land and stacking limits|分别设置面积、最大层数、相对地形高度及坡度等边界，来源和单位明确。 / Independent bounds with units and provenance.|
|R25|可解释多方案比较 / Explainable alternatives|比较展开/均衡/紧凑等可行候选；未找到则说明搜索状态与冲突，不伪造3个可行解，也不冒称已证明无解。 / Explain search failure without claiming proven infeasibility.|
|R26|可信面积与可复现 / Honest metrics and reproducibility|面积口径一致、不多层重复算；保存道路、场地、参数、版本和求解设置。 / Consistent union-based metrics and reproducible inputs.|

## 两曲线定位 / Locating two curves

每方向走廊用水平弧长s参数化的平面曲线p(s)与标高z(s)。给每个接入点独立的s，不能用地图X/Y偏移当沿路距离。单位切向T=p′/||p′||；两方向局部交角可用atan2(cross(Ta,Tb), dot(Ta,Tb))表示，方向反转须同时维护端口与车道语义。 / Use horizontal stationing and directional tangents; preserve lane semantics on reversal.

在选定影响区内枚举交点/近接候选，容差去重；多交点由用户选目标区域。近平行/近切/重叠段不强求唯一交点，无相交但需连接可进入独立近接模式。零导数/零长段必须拒绝。地面投影相交不意味着同高程或允许创建连接节点。 / Handle ambiguity and degeneracy explicitly; projected intersections are not necessarily junctions.

曲线偏移q(s)=p(s)+d(s)N(s)只是一种局部几何表达，不保证在紧弯/变宽时无尖点或自交。必须验证全道路扫掠包络、离散节点和实际生成车道图。 / Offset curves require envelope and discrete-network validation.

## 面积口径 / Area definitions

先冻结有效场地多边形B（可有禁建洞，面积必须大于0）及同一投影坐标/米制单位。总面积不裁剪；场地内占比与场地外溢出分别显示，超出硬边界不得PASS。 / Use a valid positive-area site; report full footprint and overflow separately.

- A_projection：道路/桥梁结构水平扫掠投影的并集面积，多层重叠只计一次，不使用各层面积相加。 / Union of projected structural footprints.
- A_ground：可识别的地面路基、桥墩、挡墙、边坡等直接地面占用并集；高架桥下不自动视为可开发土地。 / Direct ground occupation; under-bridge land is not automatically usable.
- A_reserved：完整预留集合R的面积；R=P∪G∪E，P为全部结构投影，G为直接地面占用，E为额外侧向/维护/坡脚等预留。P与G不假定互相包含；R包含两者，包含高架投影是本项目保守规划口径，不是法律征地面积。 / R is the full conservative reservation union, not just a side buffer or legal right-of-way.
- A_change：先固定同一种footprint集合F及原始基线；新增=area(F_new\F_old)，移除=area(F_old\F_new)，保留=area(F_new∩F_old)，净变化=新增−移除另列。不得混用净变化与新增面积。 / Use the same footprint definition for added, removed, retained and net change.
- rho=area(R∩B)/area(B)：固定场地内占地比例；同时必显A_overflow=area(R\B)及完整area(R)。不能扩大分母或隐藏超界来制造低占比。 / Site occupancy ratio is paired with mandatory overflow and full-area reporting.

这些指标应显示组成图层与缺失数据；缺桥墩/边坡/地形数据时标部分估计或UNKNOWN，不显示完整占地PASS。 / Show components and uncertainty rather than false precision.

## 约束与目标 / Constraints and objectives

硬约束可包括：必需OD与禁行、合法路型、边界/禁建区、锁定主线、已验证引擎限制、最大高度/层数、净空/坡度目标及容差。必须注明哪些是用户设计要求、哪些是引擎限制，放宽需显式确认。 / Separate user bounds from verified engine limits; relaxation is explicit.

软目标可包括：减少新增占地、减少结构重叠/层数、高架长度、地形扰动、总行程与需改动节点、曲率/坡度变化。没有可靠模型时不把这些代理量当真实造价或交通容量。 / Objective proxies are not cost or capacity certificates.

先筛可行性，再做多目标比较。候选x支配y，指所有已选目标均不更差且至少一项更好；保留非支配候选供用户选，不假称唯一最优。米、平方米、层数和数量不可未经归一化直接加总；若用权重排序，显示归一化尺度与权重敏感性。 / Filter feasibility first, then compare non-dominated solutions; normalize and disclose any weighted ranking.

最大高度应相对沿线地形/道路基准明确定义；层数应在实际重叠区域按不同道路面及容差统计，不把全场地不同标高数量直接叫层数。城区紧凑可能增加层数，也可能根本无解；郊区低堆叠可能增加用地和行程，均不保证交通改善。 / Define local stacking and terrain-relative height; compactness may be infeasible.

## 交互 / Interaction

左侧输入：场地/保留区、交通侧、车道/OD、预设、面积/层数/高度上限；中间曲线预览和可拖拽接入站点；右侧候选比较；下方纵断面与约束清单。中英同时提供，单位切换不改几何。 / Bilingual site controls, curve preview, comparison and profile views.

调整一个参数需显示哪些硬约束接近边界、哪些目标变好/变差。取消不改存档；Apply前再核场地和源道路快照。参数变化不允许静默删OD、改full为partial或突破禁建区。 / Explain trade-offs and prohibit silent changes to intent.

## 研究与实现边界 / Limits

当前只定义数学与交互契约，尚未确定游戏API、具体求解算法、容差数值、性能预算或预设默认值。阶段一应从两普通机动车走廊的有限可解案例开始，复杂地形/多交点可以先识别并拒绝，不能宣称任意曲线全自动可解。 / No algorithmic completeness or runtime support is claimed.

## 失败状态与同条件比较 / Failure states and fair comparison

区分 INPUT_INVALID、UNSUPPORTED、CONSTRAINT_VIOLATION、SEARCH_LIMIT_REACHED、NO_CANDIDATE_FOUND、PROVEN_INFEASIBLE（仅有可验证证明时）。启发式搜索超时或有限候选未命中不代表数学无解；显示已探索范围、停止原因和可调整项。 / Search failure is not a mathematical infeasibility proof.

候选必须保持相同场地边界、交通侧、车道/OD需求、锁定对象和硬约束；若另提放宽边界或减少流向的方案，单独标为修改需求提案，不参与原条件最优排名。Pareto仅指当前已评估候选集，不冒称全局Pareto前沿或全局最优。 / Compare the same problem and label alternative briefs separately; dominance applies to the evaluated set.

层数验收：在局部投影重叠位置统计独立道路面，并报告全场最大层数及其位置；相接的同层路面去重，坡道沿程连续变高不作为许多独立层。分层高差/连接判定容差必须先冻结并版本化；缺实际路面数据时层数为UNKNOWN，不据中心线数量放行。 / Count independent local road decks, deduplicate connected same-level surfaces, and report the maximum with location and versioned tolerances.

禁建区初始默认全高度禁入；若未来支持“只保护地面而许可高架跨越”，必须显式标层级/高度区间且先验证，不可由求解器猜测。可用地块含形状与孔洞；相同面积的L形、窄长或离散地块不保证同样可放。候选用同尺度俯视图比较。 / Exclusions default to all-height; ground-only restrictions require explicit verified semantics. Shape and holes matter, and previews share a common scale.

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 同基准深度验证 / Same-benchmark validation

[深度案例](DEEP_CASE_PLAN.md) 在冻结真值/误差后改变交角、高差、端点纵坡和场地，比较城区/郊区及full/partial；不得删车道/OD或放宽硬约束。 / Vary one frozen benchmark without silent demand or constraint changes; search failure is not an infeasibility proof. Planned, NOT_RUN.
