# 需求验收场景 / Requirements Acceptance Scenarios

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 本文 A01–A40 仍是完整需求验收目标，游戏项全部 NOT_RUN；已执行的有限离线子项单独记录，不升级为整项 PASS。 / Full A cases remain requirements; offline subcases are recorded separately, not full acceptance. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

全部完整验收项尚未通过；有限离线子项见证据，游戏项 NOT_RUN。 / Full cases not accepted; offline subcases have separate evidence; game cases NOT_RUN.

|Case|场景 / Scenario|期望 / Expected|
|---|---|---|
|A01|单向6→4+2 / Pure six-lane split|逐车道映射无丢失或无意复制 / Exact declared mapping|
|A02|双向总6与单向6混淆 / Ambiguous lane count|要求确认，不能猜 / Require confirmation|
|A03|4+2→4合流 / Tapering merge|明确汇入与终止，给冲突风险 / Explicit merge/drop plan|
|A04|两条弯曲道路斜交 / Curved skew crossing|沿局部切向衔接，不强制正交 / Tangent-aware attachments|
|A05|同两曲线多交点/近切/近平行 / Ambiguous geometry|选择目标区域或解释不可行 / Select region or explain failure|
|A06|地面投影交叉但高程不同 / Grade-separated crossing|不误建节点 / No accidental at-grade node|
|A07|全互通与部分互通 / Full versus partial|固定full基准，删一项自动partial；检出禁止方向；区分full/free-flow/parclo / Fixed full baseline; removing a movement changes mode; separate access from layout and control|
|A08|左右行切换及地图旋转 / Driving side and rotation|端口语义稳定、重新验证车道方向 / Stable intent, revalidated mapping|
|A09|Anarchy允许穿插 / Collision override|仍报实体净空/路径问题 / Continue clearance and reachability checks|
|A10|RoadBuilder修改Traffic连接道路 / External road edit|预览失效、提示连接重验，保留用户配置证据 / Invalidate and revalidate|
|A11|保存重载/依赖缺失/更新 / Reload and dependencies|可复现或清晰阻塞，副本存档安全 / Reproduce or fail explicitly|
|A12|取消/失败/撤销 / Cancel, failure and undo|不留下未披露半生成，不删除外部对象 / No undisclosed partial mutation|
|A13|中文英文、单位切换 / Language and units|设计不变、文本完整、数字解析正确 / Same design, complete text, correct parsing|
|A14|锁定建筑/原路/桥墩区域 / Locked areas|不静默修改，说明冲突 / Respect locks or explain infeasibility|
|A15|交通评估 / Traffic evaluation|固定需求与窗口，报告排队/吞吐/未到达，非零堵车承诺 / Report measured outcomes|

验收记录须有source commit、游戏/Mod精确版本、配置、交通侧、测试存档副本、实际步骤、证据和失败限制。新增规则须同步 README、ROAD_RULES、COMPATIBILITY、VALIDATION、SOURCES 与文档索引。 / Evidence must bind to exact versions and reproducible procedures; keep all affected docs synchronized.

|Case|附加场景 / Additional scenario|期望 / Expected|
|---|---|---|
|A16|预览后外部修改 / External edit before Apply|拒绝过期提交并重算 / Reject stale commit|
|A17|节点拆合与中隔带 / Node changes and median|保留原→新映射，无零长/重复/意外U-turn / Preserve mapping, no accidental connections|
|A18|特殊车道/第三走廊 / Unsupported lanes or corridors|明确不支持，不静默替换 / Explicitly reject unsupported inputs|
|A19|曲线离散后偏离 / Discretization error|按预先冻结容差复验，未知不通过 / Revalidate frozen tolerances|
|A20|两条匝道只用一条 / Uneven lane use|控制OD和相邻节点做对照，不把6=4+2当均分保证 / Controlled OD/node tests|
|A21|换城市/删对象/读档 / Save-scoped state|旧占位缓存失效，无跨城市污染 / No stale occupancy across saves|
|A22|手修后重生成 / Manual edit then regenerate|识别所有权和手改，冲突时阻止覆盖 / Preserve manual/external edits|
|A23|数值输入与步长 / Numeric controls|不跳极值、范围提示、键盘与撤销可用 / Stable accessible numeric editing|

## 需求—验收—来源追踪 / Traceability

|需求|用例|依据|
|---|---|---|
|R01–R03|A01–A03,A20|用户要求；ROAD_RULES；research/geometry；community C1|
|R04–R05|A04–A06,A14,A19|用户要求；官方Road Tools；几何方法参考（非游戏硬限）|
|R06|A07,A08|用户要求；有向图项目契约；research/geometry|
|R07|A06,A09,A14|Anarchy作者限制；实体净空项目契约|
|R08|A13,A23|Placemaker作者与issue1；UX研究|
|R09|A12,A16,A21,A22|Placemaker issue8/12；社区误删；项目安全契约|
|R10–R11|A10,A11,A16–A18|Traffic/RoadBuilder作者；community C2|
|R12|A13,A23|用户首阶段中英要求；UX与可访问性设计目标|

各研究文件列直接来源及证据限制，全部Case仍NOT_RUN。

## 现实感与运行新增场景 / Realism and operation cases

|Case|场景 / Scenario|期望 / Expected|
|---|---|---|
|A24|服务/系统型 / Interchange role|区分高速及地面端部，不混full/free-flow / Separate role and coverage|
|A25|主线与驾驶视角 / Continuity and view|检查坡顶+弯道+出口、标线箭头及镜像 / Check combined geometry and intent|
|A26|下游阻塞 / Blocked terminal|记录队尾、主线回堵、消散，不只出口车道数 / Observe spillback|
|A27|C-D与直接接入对照 / Collector-distributor alternative|控制需求，检查回流口瓶颈 / Compare without hiding bottlenecks|
|A28|全结构包络 / Full envelope|梁底/桥墩/护栏/路肩检查，未知不通过 / Inspect full structure|
|A29|地方/应急/步行服务 / Local access|保护已有通道，特殊类型不支持时阻塞 / Preserve existing routes|
|A30|空路与失败出行 / Empty roads and failed trips|同时记录需求和完成率，缺观察能力明确限制 / No false improvement claims|
|A31|版本更新与成本性能 / Version and cost|同条件重测并说明费用/性能数据来源 / Version-bound comparisons|

追溯：R13→A24，R14/R15→A25，R16→A26，R17→A27，R18→A28，R19→A29，R20→A30/A31。来源在REALISM_AND_GAMEPLAY及两份research补充。全部仍NOT_RUN。

## 曲线场地新增场景 / Curved-site cases

|Case|场景 / Scenario|期望 / Expected|
|---|---|---|
|A32|两曲线多交点/近切 / Multiple or tangent crossings|用户选目标区，歧义明确，无隐式正交化 / Explicit ambiguity handling|
|A33|同OD城区/郊区预设 / Same-OD urban/suburban|偏好透明、硬限制不变，无偷偷删方向 / No hidden intent changes|
|A34|L形/窄长/带洞地块 / Irregular parcels|按形状包络验，不仅总面积 / Shape-aware checks|
|A35|同投影多层桥 / Stacked decks|投影并集不重复，局部最大层数正确 / Union area and local levels|
|A36|新增/移除/保留 / Footprint changes|固定F与基线，区分差集/交集/净值 / Consistent set metrics|
|A37|越界与面积占比 / Ratio and overflow|B有效>0，内占比/外溢/总面积全部显示 / Valid denominator and visible overflow|
|A38|限层限高后搜索失败 / Failed bounded search|区分冲突/超时/未找到与证明无解 / No false proof of infeasibility|
|A39|全高度禁建与锁区 / Exclusions and locks|不自动高架跨越、开启无碰撞或删OD / Respect explicit exclusions|
|A40|候选与参数复现 / Reproducibility|同需求同尺度，Pareto限本次集合，记录版本/容差/设置 / Fair reproducible comparisons|

追溯：R21→A32；R22→A34/A39；R23→A33；R24→A35/A38；R25→A38/A40；R26→A35–A37/A40。来源见CURVED_SITE_OPTIMIZATION及research/land_optimization、research/land_controls。全部NOT_RUN。

## R27/R28 离线子项 / New bounded offline subcases

A41: Hermite endpoints, 0→12m/200m average6% vs peak9%, equal-height overshoot, reverse travel grades, common datum shift, offset metric correction and invalid input. A42: real orbit/pan/zoom, full approach–ramp–exit highlight, full/partial edit, height/grade redraw, profile/top/reset, explicit legacy migration and schema2 reload. Executed evidence is in [OFFLINE_VALIDATION](OFFLINE_VALIDATION.md). Structural clearance, terrain, local deck counts and all game equivalents remain NOT_RUN; A41/A42 offline passes are not complete game acceptance.

## 深度案例验收 / Deep-case acceptance

A43 / R29：生成前审查材料、H1–H5失败条件、独立车道/OD真值及误差来源；状态NOT_RUN。A44 / R30：完整案例的独立检查、注入断连/逆向/穿插、变体/盲测及游戏读回；状态NOT_RUN。 / A43 freezes provenance, hypotheses, independent oracle and errors; A44 validates the complete case independently with adversarial cases and game read-back. Use [D0–D4 gates](DEEP_CASE_PLAN.md), not historical demo assertion counts.

A45 / P0：用户“道路消失只余桥墩”报告的未来故障回归，按 [M11-A–E](research/road_mod_lessons.md) 验证所有权、异步收敛、实际道路/支撑读回、幂等补偿及结构化错误。覆盖部分成功、迟到、误归属、删除失败和恢复重载，不误删原道路。根因UNKNOWN、游戏复现NOT_RUN，不声称已修复。 / Future recovery gate; ownership and verified read-back must survive fault injection without damaging the pre-existing world. Unknown cause, unexecuted runtime reproduction.

A46：将 [M01–M10](research/road_mod_lessons.md) 映射到H1–H5，测试方向镜像、边缘交叉、删除唯一出口、近邻/上下重叠、旧snapshot、道路B选择不被起点A覆盖、动态metadata及贪心反例。 / Reference-derived port, lane, identity and matching counterexamples; all NOT_RUN.
