# Mod 兼容矩阵

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 浏览器 Demo 的 npm 依赖与 Mod 兼容无关；下表仍全部 NOT_TESTED。 / Browser dependencies do not establish game compatibility; every row remains NOT_TESTED. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。


以下 `NOT_TESTED` 是本项目状态，资料阅读不能改成 PASS。安装前记录当前游戏 build、Mod 精确版本、PDX ID、依赖与 playset 快照。

| 组合 | 初始用途 | 本项目状态 | 必测重点 |
|---|---|---|---|
| 原版道路，无额外道路 Mod | 最小基线 | NOT_TESTED | 建造、方向、节点、所有匝道OD可达、重载 |
| 原版 + Road Builder | 自定义车道与路型 | NOT_TESTED | 车道类型/宽度/方向、道路更新、缺依赖表现 |
| 原版 + Anarchy | 放置限制开/关对照 | NOT_TESTED | 重叠、净空、工具开关后连接和保存重载 |
| Road Builder + Anarchy | 主要目标组合 | NOT_TESTED | 放置成功与路径连通分开核验，旧存档副本 |
| Road Builder + Traffic | 可选交通控制 | NOT_TESTED | 上游连接重置风险，编辑全部实例/单实例后回归 |
| Road Builder + Anarchy + Traffic | 综合压力组合 | NOT_TESTED | 三者单独基线先过，再核连接、优先级与重载 |
| 上述组合 + Move It | 可选后期形态调整 | NOT_TESTED | 移节点/曲线后拓扑、坡度、桥下净空与车道 |
| 上述组合 + Network Tools | 可选节点编辑 | NOT_TESTED | 节点拆分/合并、道路参数与存档兼容 |


## 已知上游风险

Traffic 作者说明 Road Builder 编辑道路可能重置自定义车道连接和优先级。这是上游风险，尚未在本项目复现，不等于永久不兼容。Anarchy 放置成功不保证拓扑、车道连通或原网络不被删除。详见 [来源](SOURCES.md)。

每次测试另记录精确版本、ID、加载/配置、交通侧、证据、复验日期；当前版本字段均待采集，不虚构版本锁。

## 设计阶段补充
Placemaker是交互参考，不自动加入运行依赖。RB/Traffic等外部编辑触发Apply前状态复核；缺少所需API或不支持车道类型时显式阻塞。兼容矩阵仍全部NOT_TESTED。

游戏版本更新涉及换道/路径成本/失败出行时，重新验证行为；RoadBuilder/Traffic依赖缺失后的更新与重载需单独回归。参考research/game_pitfalls.md，仍全部NOT_TESTED。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 深度案例的游戏门槛 / Deep-case runtime gate

[D4](DEEP_CASE_PLAN.md) 要求固定CS2 build、道路资产和Road Builder/Anarchy版本，实际车道读回、车辆、保存重载及编辑撤销。版本目前未选定，全部NOT_RUN；CS1源码与旧项目PR不能替代当前CS2实测。 / Exact runtime versions remain unset; historical source/PR research does not establish compatibility.

旧项目和CS1机制的 [来源边界及P0恢复契约](research/road_mod_lessons.md) 不证明CS2原子事务、Undo或稳定lane API；Anarchy只读能力探测不得替用户开关。 / Source lessons are not a compatibility guarantee; do not change the user’s Anarchy setting.
