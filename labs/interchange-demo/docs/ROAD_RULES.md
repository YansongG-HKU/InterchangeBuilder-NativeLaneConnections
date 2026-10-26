# 道路、节点与立交规则研究

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 离线新增规则、容差、面积口径及支持域见 Demo 文档；不是 CS2 或现实道路硬规则。 / Offline numerical rules are experimental, not engine or engineering limits. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

以下为学习和验收框架，不是已掌握全部游戏规则，也不是实际道路设计规范。


1. **分清对象**：道路类型/截面配置、放置后的 segment/edge、连接 node、行驶 lane/connection、桥梁装饰网格分别记录。静态桥模型不是能通车的路网，碰撞豁免不是连接证据。
2. **节点拓扑**：端点靠近或高度相同不代表连通；记录接入方向、目标流向、匝道各OD关系、匝道与主线是否真正连接。升级、移动、拆分、合并后重新检查。
3. **几何**：以米记录平面控制点、曲线半径/过渡、标高、坡度、净空、路肩、车道数、匝道渐变段和占地。具体阈值标明“官方/上游/实测/设计目标”，未知不得填伪精确数字。
4. **路口与匝道**：分流/合流、车道守恒、编织、出口顺序、方向和路权均用具体测试表记录。先做简单菱形/喇叭式，再比较复杂布局，不承诺某种形式天然更好。
5. **坡度/桥下空间**：以实际道路面与通行包络核净空，不只看中心线或视觉空隙；Anarchy 允许放置的斜坡也需独立驾驶路径测试。
6. **版本回归**：固定左/右行、地图坡度、交通需求、游戏/Mod版本与随机性条件；测试原版、单Mod、组合、禁用/缺失依赖、保存重载、升级后恢复。缺依赖测试只在可丢弃存档副本进行。
7. **交通评价**：同时记录旅行时间、排队长度/外溢、吞吐与未到达流向；没有固定负荷与观察窗，不写“零堵车”“全兼容”。
8. **发布边界**：先确定交付是路型配置、存档示例、地图、可复用布局还是代码Mod；不能把裸FBX或文档ZIP当已可订阅的立交。主库不打包未经授权的游戏/第三方Mod二进制。


## 待查证清单

- 当前游戏最短路段、节点间距、坡度/高程、曲线和净空限制，分别在原版与Mod配置下确认。
- 节点拆分/合并、道路截面替换后连接数据如何重建。
- 车道类型、方向、转向连接、路权与路径成本的实际关系。
- 可复用立交的真实交付格式，以及 Road Builder 自定义路型依赖。

每项须记录来源、适用版本、是否实测；未知保持未知。资料入口见 [SOURCES](SOURCES.md)。

## 第一阶段契约
详细规则见 [曲线与车道](GEOMETRY_AND_LANES.md)。车道守恒仅适用于声明的独占分流，不是所有合流/共享车道的普遍公式；Full互通采用固定OD基准，与自由流和模板形态分开。

补充 [现实感与游戏实际](REALISM_AND_GAMEPLAY.md)：外观/可建造/连通/交通运行分层验收，保留游戏版本差异。

两曲线与场地占用提升为核心需求，见CURVED_SITE_OPTIMIZATION.md；数字边界需来源与单位，搜索超时不声称无解。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 拓扑与结构证据优先 / Topology and structural evidence

后续按 [深度案例](DEEP_CASE_PLAN.md) 从拓扑联合设计平纵横，不允许全端口直连后抬高隐藏冲突；全宽结构净空未知就保持UNKNOWN。 / Joint geometry follows lane topology; raising direct port connections cannot establish structural validity. This remains a planned gate, not current capability.
