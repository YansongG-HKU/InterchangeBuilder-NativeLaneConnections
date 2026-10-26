# 可视化与双语体验 / Visualization and Bilingual UX

> **当前实现状态 / Current implementation (2026-10-25 UTC):** 已有中英浏览器二维 UI、诊断定位、参数及 JSON 输入和输入撤销；直接道路拖拽/单位切换/游戏 Apply 未实现；三维相机、OD 高亮和纵断面已实现。 / Bilingual 2D browser UI exists; direct road dragging, unit switching and game Apply remain deferred; 3D camera, OD highlighting and profiles are implemented. 详见 [Demo](OFFLINE_DEMO.md) 与 [离线证据 / evidence](OFFLINE_VALIDATION.md)。下文保留完整需求及设计/游戏阶段边界。

状态：完整交互目标；离线子集已实现，游戏 UI 未实现。 / Full interaction target; offline subset implemented, game UI unimplemented.

## 参考哲学 / Reference philosophy

暂以CS2 Placemaker为参考：先选区域/对象，再表达类型/样式，实时预览，最后确认；借鉴可理解的操作流程，不复制代码、美术或声称其具备立交求解能力。具体来源见 SOURCES。 / Provisionally reference CS2 Placemaker's intent–preview–confirm flow, not its code or artwork, and not an assumed interchange solver.

## 交互流程 / Flow

选择两走廊 → 确认单/双向、交通侧和车道 → 选择全/部分互通 → 编辑流向和车道分配 → 选择/比较可行候选 → 可视化约束 → 确认变更摘要 → 应用 → 验证/撤销。 / Select corridors → confirm directions and lanes → choose movements → map lanes → compare candidates → inspect constraints → review changes → apply → validate/undo.

初学者使用引导式默认值与术语提示；高级用户可直接调弧长站点、曲线手柄、锁定点和分层结构。默认值要有来源标签，不伪装成强制规范。 / Guided defaults for beginners, explicit geometry controls for advanced users; label default provenance.

## 可视化图层 / Layers

- 总览：道路轮廓、建设影响区、被替换/保留对象。 / Overview and change scope.
- 车道：方向箭头、6→4+2映射、渐变段、车道终止和冲突。 / Lane flow and transition details.
- OD：矩阵与地图路径联动，点击一个方向高亮完整可达路径。 / Linked movement matrix and path highlight.
- 几何：中心线/边界、节点、切向、曲率风险、弧长与锁定区。 / Geometry, tangents, stationing and locks.
- 高程：平面/3D/纵断面联动，桥层区分、净空与坡度标注。 / Linked plan, 3D and profile views.
- 诊断：硬错误、风险和建议区分；颜色外加图标/文字/线型，不仅红绿。 / Errors, warnings and advice with non-color cues.
- 版本与依赖：当前环境、过期预览、未测试状态常驻可查。 / Visible provenance and validation state.

预览须可取消、对比前后和查看具体修改量；缺环境/API时不能假装Apply可执行。移动视角不改变设计，拖拽误操作可撤销；参数刷新保留用户输入并明确失效区域。 / Preview is non-destructive, cancellable and comparable; navigation is separate from editing.

## 首阶段中英 / Chinese and English from phase 1

支持简体中文 zh-CN 与英语 en；界面、参数、帮助、错误、测试报告和安装/使用说明同阶段提供。文案按语义key管理，不字符串拼接；切换语言不改变设计参数、文件格式或交通侧。 / Cover UI, parameters, help, errors, reports and usage docs; locale does not change geometry or driving side.

长度内部米制，显示单位作为独立设置；单位切换只转换显示与输入解析，不重复缩放模型。数字小数、长英文、中文字体、键盘操作、对比度、缩放和截断均纳验收。第一阶段不承诺所有语言、英制道路规范或所有键盘布局。 / Use explicit units, locale-aware parsing and accessible layouts; no all-language promise.

|中文|English|
|---|---|
|分流|Diverge / Split|
|合流|Merge|
|主线|Mainline|
|匝道|Ramp|
|车道终止|Lane drop|
|全互通|Full-access interchange|
|部分互通|Partial-access interchange|
|交通流向|Movement|
|路段|Segment|
|节点|Node|
|净空|Clearance|
|预览过期|Preview out of date|
|未验证|Not verified|

性能验收在实际环境测量交互延迟、预览耗时和取消响应；目前不虚构毫秒或帧率目标。 / Measure responsiveness in the actual environment rather than inventing performance claims.

## 缓存与生成后编辑 / Cache and post-build editing

预览缓存按存档/城市和源道路状态隔离；读档、换城市、对象删除、外部编辑后失效，不能误报旧占位。提供直接数值输入、可解释范围、步长与恢复默认，避免仅拖动滑条。 / Scope caches to the save and source state; invalidate on load, deletion and edits. Provide reliable numeric inputs and explicit ranges.

生成对象记录所有权和依赖关系；允许用户解绑/手工细调但必须解释后续自动更新的边界。重生成前比较用户改动，不能删除非本工具对象或静默覆盖手改；无法区分时阻止重生成。 / Track ownership; support a deliberate detach/manual-edit mode and protect external objects.

这些需求受到Placemaker作者issue中数值输入、陈旧占用及手修诉求启发，不代表本项目或上游已经解决。 / These are issue-inspired requirements, not verified fixes.

## 运行与现实感图层 / Operation and realism layers
新增服务型/系统型选项、驾驶视角、队尾/回堵触及主线标记、未知数据标签，以及占地/道路长度/编辑对象量比较。费用与性能仅显示实际可读或测量值，不用空道路截图代替交通评价。 / Add role selection, driver views, spillback markers and honest measured comparisons.

## 场地预设与候选比较 / Site presets and alternatives
城区/郊区/自定义只设置透明偏好；独立显示硬边界、禁建洞、面积口径、层数与高度基准。用同尺度平面图、纵断面、全部指标及冲突位置比较有限候选，不凑数、不静默放宽。新术语同时中英：紧凑Compact、展开Spread-out、少堆叠Fewer stacked levels、预留包络Reservation envelope、越界面积Overflow area、未找到可行解No feasible candidate found。

离线审查修复：六车道使用响应式标签选择对应曲线，中文/英文及窄屏实测；旧异步加载不能覆盖新的选择/重置/编辑/语言意图。OD 与独立车道实验分别显示状态。 / Offline labels are responsive/selectable; stale loads cannot overwrite newer intent; OD and lane experiments report separate states. See [validation](OFFLINE_VALIDATION.md).

Schema 2 UI adds bilingual height/grade controls, orbit/pan/zoom/reset/top, complete OD highlighting, selected longitudinal profile and explicit schema 1 flat-assumption migration. Invalid ramps remain visible; mathematical/structural/game status stays separate. / 详见 [三维契约](THREE_D_DESIGN.md) 与 [纵断面](VERTICAL_PROFILES.md)。

诊断定位修复：三维模式下有位置的诊断移动相机到问题 XY；二维模式保留原标记和滚动行为。纵断面模式仅滚动当前纵断面面板，不声称自动选中了对应匝道。 / Located diagnostics focus the active 3D scene or existing plan marker; profile mode scrolls its panel.

## 后续验证型三维 / Next validation-focused 3D

[深度案例计划](DEEP_CASE_PLAN.md) 的车道完整追踪、断面和结构冲突定位尚未实现。现有OD高亮不等于完整逐车道交互。 / Future lane-level tracing, sections and structural conflict localization remain unimplemented; existing OD highlighting is not lane-level validation.

未来恢复界面应分开FAILED、RECOVERY_REQUIRED和已验证范围，显示账本与未清理附属物，不把仅桥墩存在显示成成功；见 [P0](research/road_mod_lessons.md)。 / Future recovery UI must expose unresolved ownership/cleanup and verified scope. Not implemented.
