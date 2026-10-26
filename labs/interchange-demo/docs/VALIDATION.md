# 测试与验收

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 新增游戏外计算/浏览器实测；本文游戏测试仍 NOT_RUN。 / Offline computation/browser tests now exist; game tests here remain NOT_RUN. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

当前没有任何游戏实测记录；以下是测试计划，不是通过清单。

## 必填环境
游戏版本/平台、Mod名称及唯一ID/版本、依赖、配置、交通侧、地图/存档版本、操作步骤、时间、源commit、实际结果、日志与截图、已知限制。

## 原版与兼容分层
1. 原版最小路段与节点基线。
2. Road Builder 单独启用。
3. Anarchy 单独启用，分别测试允许/不允许冲突放置。
4. Road Builder + Anarchy。
5. 如使用 Traffic/Move It 等，逐个加入，再测完整组合，避免无法归因。

## 必测场景
- 路段端点是否是真实连接；车道方向、入口/出口路径、禁止转向是否符合预期。
- 分合流、节点重建、道路替换/升级、弯道、坡度、桥隧衔接、地形变化。
- 桥下与桥墩净空、穿插和地表误差；无碰撞开启时仍需人工与模拟双检。
- 新放置、保存重载、重启游戏、依赖缺失/版本更新、删除与回滚。
- 左/右侧通行分别记录，未测试一侧不能宣称通用。
- 在明示需求/仿真时长下观察车辆、绕路、停滞及吞吐；不编造统一性能指标。
- 修改 Road Builder 道路后重新检查自定义车道连接与优先级。

## 状态枚举
NOT_TESTED / IN_PROGRESS / PASS / FAIL / BLOCKED / NEEDS_RETEST。
PASS 必须绑定确切版本组合及证据；版本改变后默认 NEEDS_RETEST。准备文档检查通过不能把任何兼容矩阵行改为 PASS。

## 设计验收补充
执行 [需求验收场景](REQUIREMENTS_ACCEPTANCE.md) 前先冻结支持域、容差和版本；全部Case目前NOT_RUN。双语、过期预览拒绝、节点重映射与Mod外部编辑是同等门禁。

新增A24–A31运行与现实感场景；采用相同存档副本/需求/观察窗口，若无法控制需求或统计失败出行，明确标观察性及缺测，不作容量证明。

新增A32–A40核验曲线/地块/堆叠/面积口径与候选公平比较；未冻结容差、基线和分析边界时不得PASS。仍全部NOT_RUN。

## 有界三维与纵断面扩展 / Bounded 3D/profile extension

[纵断面 V1](VERTICAL_PROFILES.md) 和 [三维连接预览](THREE_D_DESIGN.md) 记录 schema 2、共享标高基准、独立 Hermite 高程/坡度、偏移接入度量校正、解析极值、完整端口 OD 高亮及真实浏览器测试。平面面积集合口径和独立六车道实验范围不变；不以中心线高差代替净空，地形/结构/层数与游戏兼容仍未知。 / New bounded profile/preview implementation does not establish structural clearance, terrain or game compatibility. Exact inputs, source hashes, screenshots and test outputs remain in [offline evidence](OFFLINE_VALIDATION.md).

## 独立检查和证据 / Independent checking and evidence

执行前按 [D0–D4](DEEP_CASE_PLAN.md) 固定预期与误差。检查器不得复用生成器的连接结论；正反例、解析/独立复算、性质/收敛/盲测及干净环境复现分别记录。工程人工审查不等于第二个AI浏览。 / Freeze the independent oracle before generation and separately document adversarial, analytic, metamorphic, convergence, held-out and third-party reproduction evidence. Human engineering review is distinct. New gates NOT_RUN.

P0“道路消失/只余桥墩”的 [M11-A–E](research/road_mod_lessons.md) 注入矩阵和A45为独立验收：超时不等于失败、桥墩不等于道路创建成功；FAILED/RECOVERY_REQUIRED/VERIFIED按实测范围报告。 / Timeout and visible piers are not failure/success oracles; recovery tests remain NOT_RUN.
