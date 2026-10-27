当前执行顺序以 [根计划](../../../docs/LEFT_TURN_PLAN.md) 为唯一权威；下面是导入时保留的方法/历史阶段，不是第二套维护计划。 / Root plan is authoritative; retained stages below are supporting history/methodology.

# 路线图

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 新增 P0.5：有限二维离线 Demo 已实现并留测试证据，下一步先冻结一个完整案例的材料、独立真值和误差，依深度计划另行授权执行；不跳过 P1–P5 游戏门禁。 / P0.5 offline demo implemented; freeze one complete-case evidence/oracle/error contract before the already approved subsequent stage. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

定位：非主业、业余爱好，不设商业里程碑、收入目标或固定工期。

|阶段|内容|当前状态|退出条件|
|---|---|---|---|
|P0|仓库、开发规则、来源与兼容框架|准备完成，研究条目仍待深化|文档同步、无虚假兼容结论|
|P1|原版道路节点/车道/标高的最小实验|未开始|合法云游戏环境及可复现存档证据|
|P2|单个高速出口和入口|未开始|连接、方向、坡度、升级、重载均验证|
|P3|Road Builder 与 Anarchy 组合|未测试|单 Mod 与组合矩阵均留证据|
|P4|菱形/喇叭形等基础立交|未开始|完整路径与交通行为测试|
|P5|复杂互通、包装与分享|未开始|前置验证通过，明确依赖及发布授权|

首个实验应只研究一个出口或入口，不同时引入复杂互通、多种道路和大量 Mod。所有假设先记录，失败也保留。

## 第一阶段设计基线（2026-10-26）
新增中英需求及23类验收情境，文档审查与运行实现分开。先完成只读输入域/预览可行性，再做有回滚的单分合流；复杂互通生成不因需求列出而自动提前。Placemaker仅为暂定交互参考，具体实现形式仍需API可行性评估。

需求补充R13–R20/A24–A31纳入前置设计；尚不提前实施C-D等复杂生成。优先验证单出口的下游回堵与实际完成出行，再扩展结构。

新增曲线×曲线与城区/郊区场地权衡需求，仍先设计和有限输入域验证，不承诺任意地块/复杂多曲线全自动求解。累计40个设计验收场景，不等于已测试。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 下一阶段优先级 / Next priority

[深度案例计划](DEEP_CASE_PLAN.md) 以D0材料/假设冻结→D1完整案例→D2反例/变体→D3独立复现→D4游戏映射替代按功能数量扩展的推进方式。全部NOT_RUN，本轮仅整合，获批算法阶段待材料冻结。原P1–P5游戏门槛继续有效；最小游戏节点实验可作为D4的前置，不跳过它。 / Depth-first gates remain unexecuted; existing game gates remain necessary.

P0恢复设计同时纳入D0：用户道路消失/残留桥墩观察及 [M11-A–E](research/road_mod_lessons.md) 先形成独立故障注入预期；实际游戏复现仍须D4合法环境，当前NOT_RUN。 / Freeze recovery fault expectations before implementation; runtime reproduction remains a separate unexecuted gate.

## 当前首个切片 / Current first slice

[直尾实验](EXPLICIT_TAIL_EXPERIMENT.md) 已有有限可运行实现；6→4+2无前置依赖。下一阶段仍需完整曲线走廊/分段高差生成/结构检查及独立游戏验证，未完成项不改标。 / Bounded progress does not complete deeper gates.

## 2026-10-27 V2 local extension / 本地第二切片

Current additional capability: [automatic orthogonal crossings and actual curved context corridors](EXPLICIT_TAIL_PHASE2.md). V1 statements about unsupported intermediate generation remain true for its missing-thickness/unsupported-shape inputs; V2 adds a constrained automatic family, not arbitrary3D routing. Full same-version regression and independent-review evidence are separate in evidence-tail-v2. Game/Anarchy/C#/remote CI remain NOT_RUN; no push/PR change. / 新增有界自动上跨、真实主路走廊与导入竞态检查，不改稳定Mod。
