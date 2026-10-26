# 实验记录模板

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 离线实验请填写 schema/solver 版本、输入 JSON、源文件哈希、预期/实际状态、截图与误差；游戏版本填 NOT_RUN，不虚构。 / Offline experiments record input, hashes, expected/actual, screenshots and errors; game version remains NOT_RUN. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

- 实验 ID / 标题：
- 问题与假设：
- 源 commit / 文件：
- 游戏版本、平台、交通侧：
- Mod 名称、ID、版本、配置、依赖：
- 存档与原始备份（不公开敏感数据）：
- 几何参数及单位、节点/路段/车道关系：
- 复现步骤与对照组：
- 预先确定的通过条件：
- 实际结果及证据文件：
- 状态（默认 NOT_TESTED）：
- 保存重载/升级/卸载验证：
- 局限、失败与下一步：
- 全仓文档影响记录：

- 需求ID/验收Case：
- 输入支持域、full/partial基准及禁止流向：
- 源道路快照与Apply前重新核验：
- 几何容差、单位、来源及离散后误差：
- 中英文本、单位切换与可访问性：

- 服务/系统型、下游端部与排队影响区：
- 需求/完成出行/未完成或消失/排队/观察窗口及不可观测项：
- 游戏建造维护费用与性能：实际来源或UNKNOWN：

- 场地B/禁建区/高程基准/锁定主线：
- footprint定义及基线、总面积/场地内占比/越界/新增移除保留：
- 预设实际参数、硬/软约束、归一化尺度：
- 候选集、搜索停止原因与复现设置（不编种子或全局最优）：

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 深度案例附加字段 / Deep-case fields

参见 [计划](DEEP_CASE_PLAN.md)：原始材料索引/许可/哈希；H1–H5假设与冻结失败标准；独立车道/OD真值；容差来源与误差预算；算法依据；源代码与检查器版本；正反例/盲测；真实截图/操作；复现及未完成项；第三方干净环境与人工工程审查分别记录。 / Record materials/rights/hashes, frozen hypotheses/oracle/errors, rationale, source/checker versions, positive/negative/held-out cases, captures/operations, reproduction and remaining work, with independent review provenance.

P0恢复追加：operation ID、创建/修改/借用对象与entity版本、依赖账本、逐步状态及结构化错误、原世界前后对照、孤立附属物、未完成清理；见 [M11](research/road_mod_lessons.md)。 / Add ownership/dependency ledger, state/error trace, before/after world and unresolved recovery evidence.
