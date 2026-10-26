# 曲线、车道与互通契约 / Geometry, Lane and Movement Contract

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 已有纯计算几何、局部六车道实验及独立端口 OD 图；不是完整游戏车道图。 / Pure geometry, separate local six-lane experiment and port OD graph exist; no game lane graph. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

状态：设计规范草案，非游戏API或求解器实现。 / Draft contract, not an API or solver implementation.

## 输入模型 / Input model

每条走廊保留稳定标识、方向、中心线 r(s)、弧长 s、标高 z(s)、横断面、每车道类型/宽度/方向、源节点与路段关系。道路双向与单向分开表达；内外侧按行驶方向和交通侧解释，不用屏幕左右替代。 / Store corridor identity, directed centerline, arc length, elevation, cross-section and source topology. Screen-left is not driving-left.

不要把包围盒或两条无限直线的交点当唯一立交中心。先分析指定影响区内的曲线交点、最近点和高程，零交点、多交点、近切、近平行、重合段、自交或曲率不稳定时显示候选或明确拒绝。投影交叉不代表路网连接。 / Handle zero, multiple, tangential, parallel, overlapping and self-intersecting cases; projected crossing is not a network node.

## 曲线衔接 / Curve attachments

- 入口/出口由道路上的弧长站点及局部切向定义，按实际路面宽度偏移构造匝道边界。 / Use arc-length anchors, local tangents and road width envelopes.
- 目标至少位置连续G0和切向连续G1；曲率变化/过渡平顺性作为显式质量目标，不把单一三次Bezier自动当G2或道路合规。 / Require position and tangent continuity; assess curvature separately.
- 偏移曲线可能出现尖点、自交和半径塌缩，必须检查整条车道/路面包络，不只中心线。 / Validate offsets and full envelopes, not only centerlines.
- 高程、坡度和竖曲线独立于平面曲线求解；桥下净空用上层结构底面与下层通行包络，不以中心线高差代替。 / Solve vertical geometry separately; measure structural clearance.
- 每次更改锁定区、路型宽度、车道或节点位置都触发增量重算与失效标记。 / Geometry edits invalidate affected previews.
- 无解时说明冲突约束，允许用户调整位置、流向或设计偏好；禁止静默变成正交、改变交通侧、删除流向或忽略碰撞。 / Explain infeasibility; never silently alter intent.

## 车道映射 / Lane mapping

纯单向6→4+2示例：输入按行驶方向稳定编号L1…L6，L1…L4延续主线，L5…L6进入出口（仅为示例，左右侧与具体映射可选）。车道守恒是这个场景的契约而非普遍公式；共享车道、车道增减、汇入让行与辅路需要独立标签。 / A pure split maps each input lane explicitly; additions, drops, shared lanes and weaving are separate cases.

映射检查：无孤立车道、无意外反向、无未声明丢失/复制；若两条连接必须在平面相交，标出冲突或提出换层/重排，而不是用Anarchy隐藏。分流提前段、合流渐变段、编织距离为可测参数，未经来源与实测不硬编码任意“正确”数值。 / Detect orphan lanes, unintended reversal, loss, duplication and conflicting transitions.

## 全/部分互通 / Full and partial interchange

将走廊边界的进入端口作为起点、离开端口作为终点，建立必需/允许/禁止的有向OD矩阵。端口用稳定ID及道路方向命名，不能因地图旋转而换义。 / Build a directed required/allowed/forbidden movement matrix using stable ports.

四臂案例：每个进口可定义直行、左转、右转；掉头单独启用。全互通需逐项证明必需OD通过合法有向车道路径可达，不要求每项各有一座独立桥。部分互通由用户勾选子集，缺少的方向明确标识；禁止的关系也必须检测意外可达。 / Full access means all baseline required movements are reachable; partial access declares missing movements. Test unintended routes too.

只看图连通不够：检查路径连续性、车道方向/类型、转向许可和保存重载后的状态；交通量与排队仍需后续仿真实测。 / Graph connectivity is necessary but insufficient for runtime traffic performance.

## 完整性模式的防歧义规则 / Unambiguous completeness modes

Full 的基准由选定走廊边界、交通侧、合法端口类型生成：全部合法非掉头跨走廊转向，加上主线直行连续性。用户不能通过缩小勾选列表把缺方向的互通标成Full；禁用任何基准必需方向时自动切换Partial/Custom并显示缺失项。掉头另选，不混入默认full定义。 / Derive the full movement universe from corridor ports and legal directions, not a user-shrunk checklist. Removing a required movement changes the mode.

全互通(full access)与自由流(free flow)、对称造型、立交类型分别设置/显示；有信号控制但全部方向可达也可能是full access。部分苜蓿叶(parclo)是形态名，不自动表示部分方向缺失。 / Full access, free flow, symmetry and layout type are independent properties.

## 离散节点与容差 / Discretization and tolerances

从数学曲线转换引擎路段时，冻结位置、切向、采样弦误差、坡度及净空误差的容差与单位、来源和版本，再在实际离散结果上重复验证；当前不填未经证实的数字。容差未冻结时不得给几何PASS。 / Freeze versioned tolerances before testing and revalidate the actual discrete network, not only its ideal curve.

节点新建/拆分/合并保留原→新映射，保护未选路段与上游相邻节点，禁止零长或重复段；中隔带断开、道路宽度突变和意外掉头另行检查。相邻节点间距与下游目的地影响交通分配，应做控制实验，不写成固定距离万能规律。 / Track node remapping, protect adjacent networks and check degeneracy, median breaks and unintended U-turns.

OD验证限定选中立交影响区内的合法路径；经外部城市路口绕行不能充作本立交直达。全互通不承诺车道均匀利用，车辆OD需求与游戏路径选择需单独测试。 / External detours do not count as direct interchange movements; reachability does not guarantee balanced lane use.

## 全结构与端部 / Structures and terminals
几何影响区应包含服务型立交地面端部及回堵连接，不止匝道曲线；桥梁/护栏/分流鼻与道路横断面共同检查。视距/超高作为现实感目标与引擎实际支持分别标记，不将几何平顺等同现实安全。详见 [补充需求](REALISM_AND_GAMEPLAY.md)。

## 曲线与用地联合约束
按 [场地优化契约](CURVED_SITE_OPTIMIZATION.md) 同时处理接入弧长、局部交角、完整结构包络与地块形状。曲线数学可接不等于包络装得下；多层投影不重复算占地，坡道沿程标高不累计成多个层。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 完整案例后续契约 / Next complete-case contract

[H1–H4](DEEP_CASE_PLAN.md) 要求完整互通集成6→4+2，检查接收车道、分合流、渐变、方向及交织；独立检查器从实际输出重建有向车道图，不信任连接标记。 / Integrate the fork into the interchange and independently reconstruct directed lane reachability; current isolated fork and centerline preview do not satisfy this contract. NOT_RUN.
