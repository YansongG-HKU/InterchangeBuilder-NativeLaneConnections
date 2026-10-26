# CS2 Interchange Lab — Community-informed requirements / 社区需求调研

调研日期 / Accessed: 2026-10-25。范围为 Cities: Skylines II，第一阶段需求和验收设计，未运行游戏、未验证兼容性、未修改仓库。

## Evidence policy / 证据边界
- 作者仓库和官方说明用于确认其公开声明；README 会随版本改变，实施时必须记录游戏、mod、配置和版本。
- 社区帖子用于发现痛点，不构成引擎机制的证明。特别是“提前几个节点变道”“移动节点一定修好”等说法不得变成硬编码规则。
- 项目建议以下标注为 Proposed。2023 官方功能说明是历史功能背景，不是现行 SDK 契约。
- 排除了 CS1、TM:PE、Network Multitool 和 Steam app 255710 的机制说明；不能把它们直接套到 CS2。

## Findings that directly change requirements / 关键发现

1. **6→4+2 的问题不止是加法。** 一篇明确属于 CS2 的社区案例描述六车道拆为四加二，出口两车道仍集中使用其中一条；上游节点位置和下游转向需求均被讨论。应测试“节点布局 × 下游目的地”，不能把车道平均利用率当成唯一正确结果。[C1]
2. **截面一样不代表生成的节点一样。** CS2 Road Builder 克隆原版道路的案例报告中隔带缺口和意外 U-turn。该帖也出现误引用 CS1 工具后被纠正的情况。验收必须检查方向、转弯关系、节点类型和中央分隔，不只是道路宽度。[C2]
3. **可放置不等于正确连接。** Anarchy 作者说明它会放宽包含碰撞、形状、距离、坡度等检查，并不能阻止原版工具本来就会移除的网络。项目应保留自己的几何/连通性诊断，不因无碰撞开启就把方案标为合格。[A2]
4. **修改后的回归测试是必需项。** Traffic 作者列出网络组成变化、方向替换和 Road Builder 编辑可能重置或移除自定义连接。应把“路先建通，然后修改，仍然可达”列为一级验收，而不是只有首次预览。[A1]
5. **撤销必须分层说明。** 官方历史 Road Tools 介绍的是右键取消建设步骤，不等于全局建设后撤销。社区报告误删和丢失复杂互通的高昂成本。第一阶段至少提供可取消预览、变更清单和保存提醒，不承诺尚未实现的全局 Undo。[A3,C3,C4]

## Proposed requirements / 建议需求

| ID | Priority | 中文 / English | 验收边界 / Acceptance boundary |
|---|---|---|---|
| R01 | P0 | 明确车道口径 / Explicit lane-count convention | 6 是单向六车道还是双向合计必须显示；区分主线、出口、辅助、增加/减少车道。6→4+2 展示逐车道映射，不只显示总数相等。 |
| R02 | P0 | 有向转向矩阵 / Directed movement matrix | 按实际道路臂列出入口→出口；全互通、部分互通和自定义选择均可表达。明确 U-turn 是否计入；区分用户排除、求解不可行和意外断连，不默认一切都是十二种转向。 |
| R03 | P0 | 基于曲线的接入 / Curve-relative attachment | 挂接点用道路沿线位置和局部方向表达，不限定全局水平、竖直或 90°；主线曲率保留，匝道端点切向衔接有诊断。曲率、坡度和净空阈值需来自已验证规则或明确可配置假设。 |
| R04 | P0 | 立体与平面拓扑分离 / Separate spatial crossing from graph junction | 平面投影相交不自动视为连通；立交层级、连接点、高程和车辆通行路径分别可见。支持左右行驶并检查匝道进入正确方向。 |
| R05 | P0 | 节点与上下游上下文 / Node and downstream context | 显示接入区已有节点、短段、过近的连续合分流和下游目的地；不使用论坛传言作为固定节点间距规则。 |
| R06 | P0 | 解释式可视化 / Explainable preview | 路面、车道箭头、连接线、转向矩阵联动；错误定位到具体连接/路段。使用图标或文字加颜色，支持隐藏图层，显示改动前后差异。 |
| R07 | P0 | 安全提交与撤销边界 / Safe apply and explicit undo scope | 编辑历史先覆盖预览参数；取消预览不触碰既有路网。提交前列出删除、拆分、替换对象和依赖。游戏级回滚未验证前不得称为全局 Undo。 |
| R08 | P0 | 兼容性是版本化证据 / Versioned compatibility evidence | 分别记录 vanilla、Road Builder、Anarchy、Traffic、Move It 及组合测试。列明必需、可选、未测和已知冲突；“规划适配”不标成“已兼容”。 |
| R09 | P0 | 改路后的连接回归 / Post-edit connectivity regression | 修改截面、方向、道路资产或几何后重新检查 OD 与车道连接；如 Traffic 配置被重置应提示并让用户确认，不静默假定保留。 |
| R10 | P0 | 保存加载与依赖缺失 / Save-load and missing dependencies | 保存后重启读取；缺失道路资产/mod、版本变化和失效引用产生可读诊断。能导出计划配置，不把损坏的恢复标为成功。 |
| R11 | P0 | 中英完整体验 / Complete Chinese-English experience | 按钮、帮助、单位、警告、错误、空状态、导出报告均覆盖；语言不决定左右行驶。字体与布局允许英文变长，颜色不是唯一含义通道。 |
| R12 | P1 | 可解释的方案取舍 / Explainable alternatives | 对比占地、匝道数量、跨越层级、土方代理指标和违规项；区分几何评分与经过模拟的交通结果，不承诺零拥堵。 |
| R13 | P1 | 可重复交通验证 / Reproducible traffic evaluation | 固定需求与下游布置、记录预热时间和观察窗口；测试 OD 可达、排队/旅行时间、晚变道和出口溢出。某车道利用率低不自动判错。 |
| R14 | P1 | 新手与高级两层 / Basic and advanced controls | 普通用户先选道路、互通方向、出口车道；高级用户再设节点、几何约束和规则覆盖。每个覆盖显示风险，不强迫使用高级节点 mod。 |

P0 表示第一阶段需求文档和原型必须覆盖，不等于第一阶段承诺交付全部游戏执行功能。P1 为设计预留和后续实现候选。

## Acceptance scenarios / 验收场景

1. **6→4+2：** 指定单向六车道，四条直行、两条出口；展示每条车道的候选连接。分别设置出口后单一转向和分散目的地，检查出口前后的可达性及换道，不以自动平均分流作为验收条件。
2. **两条非正交曲线：** 选择两条弯曲主线，在斜交、多处平面相交或彼此接近但不相交情形选择接入区；不得擅自拉直原路。无法满足约束时指出冲突，而非生成交叉穿模方案。
3. **部分互通：** 明确禁用一组方向，预览中显示“未提供 / Not provided”，其余已选择方向必须可达；不能用不连通冒充“部分互通”。
4. **高架跨越：** 顶视图相交、不同层高，确保不产生意外平交关系；净空应基于道路结构和被跨越对象包络检查，不只比较中心线高度。
5. **无碰撞开启：** 允许用户看见放宽规则后的方案，同时保留过陡、净空不足、几何自交和断连的错误；默认不把“游戏允许放下”当“验收通过”。
6. **Road Builder + Traffic：** 建立连接后替换/编辑道路截面、改变方向和移动几何；重检连接并记录重置现象。缺少任一 mod 则输出未测试，而非假通过。
7. **节点过密：** 将分流附近的节点移动或增加，比较拓扑与交通现象；阈值以版本测试结果记录，不将社区经验固定写死。
8. **安全操作：** 取消预览后地图无变化；提交前确认删除范围；保存→退出→重载后比较道路、连接、参数与依赖。加载缺依赖时给出恢复指引。
9. **中英文与左右行驶：** 同一项目切换中英文不改变几何；切换左右行驶需重新校验方向。窄屏、长英文、红绿色弱均能看懂关键错误。

## Phase-one non-goals / 第一阶段非目标
- 不做真实公路工程认证，也不承诺参数符合任何特定国家的法定道路标准。
- 不承诺任意地形、任意车道数、任意 mod 版本的万能互通生成器。
- 不承诺所有形式的游戏全局撤销、自动修复存档、自动迁移第三方车道连接。
- 不把外观截图、无碰撞可放置、规划器中的路径可达等同于游戏交通验证通过。
- 不为了表面上的“全互通”默认增加不需要的 U-turn 或绕行。
- 不将 CS1 工具或方法直接作为 CS2 API/功能证据。

## Sources / 来源

### Authoritative feature statements (version-sensitive) / 作者及官方说明
- **A1 Traffic README:** https://github.com/krzychu124/Traffic/blob/main/README.md — lane connections, edit/reset and Road Builder limitations; save behavior. This source supports the need for regression tests, not a completed compatibility claim.
- **A2 Anarchy README:** https://github.com/yenyang/Anarchy/blob/master/README.md — suppressed construction errors and limits; recommends frequent saves. Collision bypass is not a topology validation contract.
- **A3 Official Road Tools feature:** https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools — historical 2023 curve/parallel-road tools, construction cancellation, ramp concepts. Not current API documentation.
- **A4 Official Traffic AI feature:** https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai — historical routing costs background; supports separating geometry from traffic outcome.
- **A5 Road Builder author repository:** https://github.com/JadHajjar/RoadBuilder-CSII — configurable road assets and city-saving context; exact compatibility must be tested against pinned builds.
- **A6 Network Tools author repository:** https://github.com/lucarager/CS2-NetworkTools — interactive 3D handles and live-preview example; optional UX reference, not a required dependency.

### Community signals / 社区需求线索（未复现实测）
- **C1 CS2 exit/lane split:** https://www.reddit.com/r/CitiesSkylines2/comments/1x27ho8/how_to_fix_traffic_at_intersection/ — upstream node changes and 6→2+4 discussion. User reports, not a proven universal lane-selection rule.
- **C2 Explicit CS2 Road Builder clone:** https://www.reddit.com/r/CitiesSkylines/comments/1wwlnbz/cs2_road_builder_mod_clone_of_a_vanilla_road/ — reported median breaks and U-turns despite similar road composition.
- **C3 CS2 Undo discussion (2023):** https://www.reddit.com/r/CitiesSkylines2/comments/17gspqa/no_undo_button/ — cost of accidental deletion; not evidence about current Undo support.
- **C4 CS2 lost interchange work (2024):** https://www.reddit.com/r/CitiesSkylines2/comments/1difbip/ — loss of complex interchange work; motivates backup and explicit apply boundary.
- **C5 CS2 unexpected routing:** https://www.reddit.com/r/CitiesSkylines2/comments/1vflpw4/why_do_my_citizens_take_this_route_instead_and/ — conflicting community explanations motivate controlled OD tests instead of speculative diagnoses.
- **C6 CS2 service/system and partial interchange options:** https://www.reddit.com/r/CitiesSkylines2/comments/1sr0vax/how_would_you_connect_these_roads/ — alternative connection choices and directionality corrections motivate explicit movement selection.
- **C7 CS2 split-carriageway intersection:** https://www.reddit.com/r/CitiesSkylines2/comments/1tdq1tb/highway_intersection_crossing_2_seperate_lanes/ — node/grouping pain signal; proposed community workarounds are not endorsed here.
- **C8 CS2 curves and slopes:** https://steamcommunity.com/app/949230/discussions/0/836125423060422126/ — requests for smoother alignment editing; app 949230 is CS2, unlike CS1 app 255710.

## Open questions to resolve in implementation / 后续必须实测
- Move It currently supported network edits and their event behavior; no specific implementation mechanism established in this research.
- Reliable boundaries for transaction-like apply and restore using the target game API.
- Whether the targeted game/mod combination exposes sufficient lane graph data for automated OD checks.
- Exact geometric feasibility tolerances, roadway envelopes and node behavior under the target versions.
- Which compatibility combinations are officially supported versus best-effort.
