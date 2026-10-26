# 来源与证据

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 本轮新实现的算法、数值和支持域为项目实验定义，不来自游戏实测。polygon-clipping 使用 npm 锁定版本和许可证；不复制历史研究来源代码或素材。 / New numerical settings are project experiments, not game evidence. Dependencies and licenses are explicitly recorded. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。


### S1 官方 Road Tools（2023 年功能介绍，历史设计说明）

官方介绍包含平行道路、曲线、吸附/引导线、坡度显示与道路替换工具；交叉建路可以生成交叉口。这能作为学习入口，但不是所有现版本数值限制的规范。当前确切坡度、最短段长、节点间距和净空阈值必须通过固定版本实测或当前官方资料确定，不从 CS1 搬用。

来源：https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools

### S2 官方 Traffic AI（2023-06-26，历史设计说明）

路径选择包含时间、舒适、金钱、行为等成本，而不只是最短几何距离。项目应测试实际流向与可达性，而不能由立交外观推定交通效果；发布说明不得把游戏实验当真实工程交通认证。

来源：https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai

### S3 Road Builder 作者资料

已确认 CS2 仓库为 `JadHajjar/RoadBuilder-CSII`。它从现有路段创建自定义道路，支持修改所有实例或基于模板创建变体；高级模式可能暴露不受支持的组合。作者当前 README 也介绍自定义 net section/lane 的组件入口；使用这些组件的资产需要 Road Builder，并应标依赖。含自定义道路的共享存档也应声明依赖。上述为作者资料，不是本项目实测兼容。

- 源码与作者说明：https://github.com/JadHajjar/RoadBuilder-CSII
- Paradox Mods 页面：https://mods.paradoxplaza.com/mods/87190/Windows

注意：此次 PDX 正文为动态页面，浏览提取仅确认页面标题，未取得可靠最新版本号；不能把仓库主分支等同已发布版本。

### S4 Anarchy 作者资料

已确认 `yenyang/Anarchy`。作者说明它可抑制放置工具的碰撞、坡度、短距离等错误，并可允许违反其他网络净空；这不代表网络拓扑或车道连通正确。作者亦明确：原版工具会删除的网络仍可能被删除，UI 也仍可能阻止操作。需保留多份存档，不能以“没有报错”作为验收。

- 源码与说明：https://github.com/yenyang/Anarchy
- Paradox Mods 页面：https://mods.paradoxplaza.com/mods/74604/Windows

本项目用 Anarchy 作为优先研究的“无碰撞类”具体工具；不把 CS1 Fine Road Anarchy/Network Anarchy 名称直接当成 CS2 依赖。

### S5 Traffic 作者已声明的组合风险

`krzychu124/Traffic` 提供车道连接和优先级工具。作者 README 明确：Road Builder 对道路的操作可能重置 Traffic 的自定义连接与优先级；替换道路组成、方向或轨道升级也需重新核验连接。故 Road Builder + Traffic 必须包含“修改后重新检查、保存重载”的回归用例，不可仅测试首次摆放。

- 源码与说明：https://github.com/krzychu124/Traffic
- Paradox Mods 页面：https://mods.paradoxplaza.com/mods/80095/Windows

这是上游已报告风险，不是本项目已复现结果；不能简单标为永久不兼容，也不能忽略。

### S6 可选候选：Move It 与 Network Tools

Move It 的 PDX 作者页面显示它可移动节点和路段曲线；本次检索显示维护者 yenyang、原作者 Quboid，版本 0.5.16/建议游戏 1.6.* 为页面快照而非本仓实测版本锁。

https://mods.paradoxplaza.com/mods/74324/Windows

Network Tools 作者仓库明确提供插入/删除节点、坡度与曲线编辑等工具，并标早期访问。它是 CS2 项目，不等于把 CS1 Network Multitool 装入 CS2；先作为可选研究对象，不自动设为项目依赖。

https://github.com/lucarager/CS2-NetworkTools

https://mods.paradoxplaza.com/mods/133736/Windows


核查：2026-10-25 UTC。官方早期功能介绍不等于当前版本全部数值约束；作者文档不等于本仓游戏测试。

## 第一阶段扩展调研 / Phase 1 research

- [几何与工程方法（非游戏硬规范）](research/geometry.md)
- [社区需求线索（未在本项目复现）](research/community.md)
- [Placemaker与交互研究](research/ux.md)

核查日期2026-10-25 UTC。社区帖子只支持需求/故障情境，不支持通用引擎因果；当前无法读取的来源或未锁版本应保留限制。

## 现实感与运行补充来源

- [交通机构参考及游戏映射](research/realism_addendum.md)
- [官方版本与Mod行为、社区线索](research/game_pitfalls.md)

核查2026-10-25 UTC。真实工程资料仅借鉴原理，不构成全球/游戏数值标准；版本化行为不能由历史帖永久推断。

## 曲线与用地数学/交互研究

- [集合几何与多目标设计](research/land_optimization.md)
- [场地控制与可视化](research/land_controls.md)

数学文档支撑方法，不代表CS2 API可用或本项目已实现求解。所有面积/高度/层数默认值仍待来源与实际环境确定。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 2026-10-26 深度研究 / Deep-case research

[参考机制与反例](research/road_mod_lessons.md) 收录Network Multitool、Node Controller Renewal、TM:PE、Move It作者材料，以及用户确认的InterchangeBuilder-NativeLaneConnections固定main/未合并PR研究。CS1 master为动态读取快照，无可靠SHA，不伪造固定版本；旧仓main与PR4分别固定到9f083061/0e9b3449，测试数量仅自报。 / Distinguish dynamic author sources, pinned main, unmerged proposals and unverified test claims.

用户“修路失败/道路消失/只余桥墩”是直接观察，具体版本/日志/存档缺失；原因假设与源码证据分列，不将条件性补丁认作已解决。新 [深度计划](DEEP_CASE_PLAN.md) 用这些材料设计反例，未执行或移植代码。 / Direct user observations do not establish causality; references inform unexecuted adversarial contracts only.
