# 现实感与游戏实际 / Realism and Gameplay

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 已有离线端口可达及部分用地指标，但本文交通/结构/游戏运行要求仍未验证。 / Offline reachability and partial footprint metrics do not validate operation or structure. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

状态 / Status: **Design addendum; all runtime cases NOT_RUN.** 现实工程参考、引擎规则、视觉目标与游戏实测分别记录；本项目不用于道路施工或安全认证。

## 新增需求 / Additional requirements

|ID|注意事项 / Consideration|设计与验收要求 / Requirement|
|---|---|---|
|R13|服务型与系统型 / Service vs system interchange|高速↔地面路与高速↔高速分开；独立于full/partial及free-flow选择。 / Separate network role from movement coverage and control.|
|R14|主线连续与驾驶预期 / Mainline continuity|直行不应无提示变成出口；箭头、标线、路牌与逐车道连接一致，右行优先右出口/左行镜像，例外明确。 / Keep route and lane intent readable; explicit exceptions.|
|R15|驾驶视角而非仅俯视 / Driver-view review|急弯+坡顶+出口、护栏/桥墩遮挡纳视觉检查；不声称游戏模拟现实视距/超高安全。 / Inspect combined geometry without claiming real-world safety simulation.|
|R16|出口回堵 / Ramp spillback|下游红绿灯、环岛和路口纳影响区，记录储车段、队尾触及主线与消散时间。 / Include downstream terminals and spillback, not only ramps.|
|R17|编织与集散 / Weaving and collector-distributor roads|比较出入口次序、辅助车道、C-D等方案；C-D不是普通辅路，也不是默认更好。 / Compare options and detect bottlenecks at re-entry.|
|R18|完整横断面与结构 / Cross-section and structures|检查车道、路肩、中隔、护栏、分流鼻、桥面与梁底、桥墩，不只看中心线。 / Validate the entire physical envelope.|
|R19|保护地方与服务可达 / Preserve local and service access|不支持生成的步行/特殊网络也要检测并保护；断开则阻塞或明确警告，不静默删除。 / Protect existing paths even when generation is unsupported.|
|R20|真实交通评价 / Honest traffic evaluation|记录需求、完成出行、未完成/消失（能观测时）、排队、旅行时间及吞吐；无法控需求则标观察性结果。 / Track outcomes, not only an aggregate traffic percentage.|

## 验收层级 / Acceptance layers

1. 节点/车道/OD的网络正确性。 / Network correctness.
2. 引擎可建造性、几何/净空、支持域及未知项。 / Buildability, geometry and unknowns.
3. 固定条件下游戏寻路、回堵和服务行为。 / Controlled or explicitly observational simulation.
4. 外观和驾驶可读性。 / Appearance and driver readability.

四者不得互相代替。路面空了可能是需求变了、出行失败或其他机制，不自动等于优化成功；同样，游戏能放置不说明现实合理。 / Empty roads and successful placement are not sufficient success criteria.

## 版本与性能 / Versioning and performance

上游游戏可能改变出口换道、掉头成本及失败出行处理。必须把行为结论绑定实测build，不从旧帖子提取永恒节点距离。发布前在相同存档副本、需求/观察窗口、交通侧、Mod配置下比较，并保留无法观测的指标。 / Bind conclusions to the tested build and report unobservable metrics.

对候选方案同时呈现占地、道路/桥隧长度、被修改对象/节点数量；游戏能读取时才报告实际建造与维护费用。无可靠成本数据则标未知，不伪精确估价。复杂多层结构增加对象与模拟负担，性能应在相同场景测量，而非凭面数猜帧率。 / Report space, edit scope and measured costs/performance only where available.

参考：[现实原则研究](research/realism_addendum.md)、[游戏行为研究](research/game_pitfalls.md)。其中建议是本项目需求，社区意见未被复现。

## 生成后回读与运行协议 / Read-back and simulation protocol

生成后重新读取实际车道图与交通控制（信号/停牌/让行/优先级/转向限制），不能用规划图代替游戏结果；不能设置的控制标UNSUPPORTED，装饰标志不替代实际控制。左右行重验全部语义。 / Read back the generated lane graph and control state; decorative signs do not implement control.

比较采用同源存档分支、相同预热与模拟时间，记录天气/路况、下游控制、车辆类型、人口就业等条件。若不能锁定需求/随机性，重复观察并报告波动与限制。服务/货运按类别验证，普通车成功不替代应急到达；缺失观测能力保持UNKNOWN。 / Use comparable save branches and conditions; qualify uncontrolled observations and per-vehicle-class access.

性能应分别测预览、生成、加载、存档大小、模拟推进和渲染帧时间；FPS不等于仿真速度。 / Separate UI, build, load, save, simulation and rendering performance.

## 占地与堆叠权衡
详见 [两曲线与场地占用](CURVED_SITE_OPTIMIZATION.md)。集散/展开/紧凑只是同需求候选，不能从城区/郊区标签推断最优结构。规划预留不是法律征地，桥下空间不是默认可开发土地，少占地也不等于游戏性能更好。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 真实资料与合成基准 / Real materials and synthetic benchmarks

[深度案例](DEEP_CASE_PLAN.md) 优先可追溯主管部门平纵横图与地形，尺寸分实测/来源给定/推定/未知；影像不作高程真值，无资料明确合成。 / Distinguish provenance classes and explicitly synthetic benchmarks. Optional SUMO comparison is not CS2 equivalence. No new traffic or game validation.
