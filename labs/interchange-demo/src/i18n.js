export const messages = {
  viewThree: ["三维连接", "3D connections"],
  viewPlan: ["二维俯视", "2D plan"],
  viewProfile: [
    "所选匝道/主线参考纵断面",
    "Selected ramp / mainline reference profile",
  ],
  cameraReset: ["重置相机", "Reset camera"],
  cameraTop: ["三维俯视", "Top camera"],
  selectOD: ["选择 OD 高亮完整路径", "Select OD to highlight complete path"],
  pathPreview: [
    "高亮为影响区内入口—连接—出口规划路径；不是游戏寻路。",
    "Highlight: in-area approach–connector–exit planning path, not game routing.",
  ],
  cameraHelp: [
    "左键/单指旋转；右键平移；滚轮/双指缩放。坐标 x/y 为平面、z 为标高，均为米。",
    "Left drag/one finger: orbit; right drag: pan; wheel/two fingers: zoom. x/y plan, z elevation, metres.",
  ],
  sceneLegend: [
    "灰：两条主线路面；赭色：拟建匝道；红：已知无效连接；黄色：所选完整 OD。小球：接入点；箭头：方向。路面不是完整结构或逐车道设计，网格仅为项目零标高。",
    "Grey: source pavement; ochre: proposed ramps; red: invalid; yellow: selected complete OD. Dots: anchors; arrows: direction. Pavement is not full structure or lane allocation. Grid is project datum, not terrain.",
  ],
  webglUnavailable: [
    "WebGL2 不可用；已保留真实二维俯视，请检查浏览器支持。",
    "WebGL2 unavailable; real 2D plan retained. Check browser support.",
  ],
  verticalInputs: [
    "独立纵断面（实验参数）",
    "Independent profiles (experimental)",
  ],
  datum: ["共同项目标高基准", "Shared project datum"],
  z0: ["起点标高 (m)", "Start elevation (m)"],
  z1: ["终点标高 (m)", "End elevation (m)"],
  g0: ["起点纵坡 (%)", "Start grade (%)"],
  g1: ["终点纵坡 (%)", "End grade (%)"],
  maxGrade: ["绝对纵坡上限 (%)", "Absolute grade cap (%)"],
  terrainUnknown: [
    "地形、净空、局部层数未知；负标高不自动意味着隧道。",
    "Terrain, clearance and local levels unknown; negative elevation does not imply a tunnel.",
  ],
  GRADE_LIMIT: [
    "解析峰值纵坡超过实验上限；调整接入长度或纵断面。",
    "Analytical peak grade exceeds experimental cap; edit anchor length or profile.",
  ],
  peakGrade: ["解析峰值纵坡", "Analytical peak grade"],
  profileScale: [
    "图表横纵独立缩放，三维无高度夸张",
    "Chart axes independently scaled; no vertical exaggeration in 3D",
  ],
  heightDifference: [
    "中心线交会高差（不是最小实体净空）",
    "Centerline crossing height difference (NOT minimum structural clearance)",
  ],
  migrationWarning: [
    "旧 schema 1 没有高度数据。仅在接受后转换为两主线零标高、零坡度的平坦示意；地形和净空仍未知。",
    "Schema 1 has no elevations. Accept only to create illustrative zero elevations/grades on both mainlines; terrain and clearance remain unknown.",
  ],
  acceptMigration: ["接受平坦示意并转换", "Accept illustrative flat migration"],
  cancelMigration: ["取消导入", "Cancel import"],

  odState: ["OD 候选状态", "OD candidate status"],
  laneHelp: [
    "选择车道标签，在图中高亮对应计算连接（白色粗线）。",
    "Select a lane label to highlight its computed connector (thick white line).",
  ],
  splitState: ["独立车道实验状态", "Separate lane experiment status"],
  loadFailed: [
    "读取案例失败；保留原输入，请重试。",
    "Case load failed; previous input retained. Retry the load.",
  ],
  title: [
    "立交实验室 · 游戏外连接预览",
    "Interchange Lab · Offline connections",
  ],
  intro: [
    "调整参数 → 选交会 → 检查候选 → 保存案例。单位为米；所有默认值都是实验参数。",
    "Edit parameters → choose crossing → inspect candidates → save case. Metres; defaults are experimental.",
  ],
  warning: [
    "三维规划预览，不是可建造立交。纵坡做数学检查；结构、地面实占、净空、层数及游戏交通未知。",
    "3D planning preview, not a buildable interchange. Grades are mathematically checked; structures, ground, clearance, levels and game traffic are UNKNOWN.",
  ],
  inputs: ["输入与硬约束", "Inputs & hard constraints"],
  example: ["案例", "Example"],
  anchor: ["接入弧长距离 (m)", "Anchor arc distance (m)"],
  handle: ["切向控制臂 (m)", "Tangent handle (m)"],
  areaCap: ["部分预留面积上限 (m²)", "Partial reservation cap (m²)"],
  heightCap: [
    "相对地形高度上限 (m) · 未评估",
    "Height above terrain cap (m) · unassessed",
  ],
  layerCap: ["局部层数上限 · 未评估", "Local level cap · unassessed"],
  preference: [
    "软偏好（不改硬约束）",
    "Soft preference (preserves constraints)",
  ],
  urban: ["城区：按部分预留面积升序", "Urban: partial reservation ascending"],
  suburban: [
    "郊区：优先较长接入；层数未知",
    "Suburban: longer anchors first; levels unknown",
  ],
  custom: [
    "自定义：按部分预留面积升序",
    "Custom: partial reservation ascending",
  ],
  trafficSide: [
    "交通侧（独立于语言）",
    "Traffic side (independent of language)",
  ],
  RHT: ["靠右行驶", "Right-hand traffic"],
  LHT: ["靠左行驶", "Left-hand traffic"],
  crossing: [
    "交会候选（点击图中编号）",
    "Crossing candidate (click numbered marker)",
  ],
  choose: ["请选择", "Choose explicitly"],
  solve: ["重新计算", "Recompute"],
  reset: ["重置", "Reset"],
  undo: ["撤销输入", "Undo input"],
  export: ["导出案例 JSON", "Export case JSON"],
  import: ["导入案例 JSON", "Import case JSON"],
  advanced: [
    "高级：曲线、地块及全部输入 JSON",
    "Advanced: curves, site and complete input JSON",
  ],
  applyJSON: ["校验并应用 JSON", "Validate and apply JSON"],
  od: [
    "固定 OD 基准：四直行 + 八转向",
    "Fixed OD baseline: four through + eight turns",
  ],
  full: ["启用全部流向", "Enable full baseline"],
  partial: [
    "部分互通：排除项不生成",
    "Partial access: exclusions are not generated",
  ],
  candidates: ["有限候选集", "Finite candidate set"],
  preview: ["真实计算预览", "Computed preview"],
  legend: [
    "灰：原路；青：转向；虚线：预留；编号：交会；橙：问题位置。交叉不建立节点。",
    "Grey: source; teal: turns; dashed: reservation; numbers: crossings; orange: diagnostic. Crossings do not create nodes.",
  ],
  split: [
    "独立单向 6→4+2 逐车道实验",
    "Separate one-way 6→4+2 lane experiment",
  ],
  splitHelp: [
    "L1–L4 → M1–M4；L5–L6 → R1–R2。独立局部出口向外展开 45m；不接入 B，不计入 OD 面积。",
    "L1–L4 → M1–M4; L5–L6 → R1–R2. Separate local exit spreads 45m outward; not connected to B or counted in OD area.",
  ],
  metrics: [
    "面积：采样路面 + 侧向预留（部分估计）",
    "Area: sampled pavement + lateral reserve (partial estimate)",
  ],
  total: ["完整部分预留并集", "Unclipped partial reservation union"],
  projection: ["路面投影并集", "Pavement projection union"],
  overflow: ["场地外/禁建区内", "Outside site / inside exclusions"],
  ratio: ["场地内占比", "In-site ratio"],
  added: ["新增", "Added"],
  removed: ["移除", "Removed"],
  retained: ["保留", "Retained"],
  net: ["净变化", "Net change"],
  unknown: [
    "完整 P/G/R、净空、层数、交通：UNKNOWN",
    "Complete P/G/R, clearance, levels, traffic: UNKNOWN",
  ],
  approx: [
    "曲线采样有误差；集合运算针对采样多边形。完整面积误差界未知，不能视作准确结构包络。",
    "Curve sampling is approximate; Boolean operations apply to sampled polygons. Complete area error bound unknown; not an exact structure envelope.",
  ],
  diagnostics: ["诊断（点击定位）", "Diagnostics (click to locate)"],
  none: [
    "当前视图无列出的诊断；未验证项仍阻止通过。",
    "No listed diagnostic in this view; unverified checks still prevent PASS.",
  ],
  running: ["计算中，旧结果已失效…", "Computing; previous result is stale…"],
  INPUT_INVALID: [
    "输入无效：检查字段和有限数值",
    "Invalid input: check field and finite numeric values",
  ],
  UNSUPPORTED: [
    "不支持该模型、版本或退化曲线",
    "Unsupported model, version or degenerate curve",
  ],
  SELECT_CROSSING: ["请选择有效交会候选", "Select a valid crossing candidate"],
  NEAR_TANGENT: [
    "近切交会不支持；调整曲线或选区",
    "Near-tangent crossing unsupported; edit curves or selection",
  ],
  NO_CANDIDATE_FOUND: [
    "有限搜索未找到候选；不代表数学无解",
    "No candidate found in bounded search; not proof of infeasibility",
  ],
  OFFSET_RISK: [
    "紧弯/反折或偏移风险；增加展开或编辑曲线",
    "Tight curve/fold/offset risk; increase spread or edit curve",
  ],
  LANE_CROSSING: [
    "逐车道连接平面交叉；修改曲线/接入，不自动换层",
    "Lane connectors cross; edit curve/anchors; no automatic grade separation",
  ],
  OVERFLOW: [
    "预留超出场地或侵入禁区；修改边界或连接",
    "Reservation outside site or in exclusion; edit site or connectors",
  ],
  AREA_CAP: ["部分预留面积超过硬上限", "Partial reservation exceeds hard cap"],
  ANCHOR_RANGE: [
    "接入弧长超出输入道路；延长道路或缩短接入",
    "Anchor outside source road; extend road or reduce distance",
  ],
  NOT_RUN: ["未运行：前置条件未满足", "NOT_RUN: prerequisites not met"],
  UNKNOWN: [
    "未知：本实验仍有未验证项",
    "UNKNOWN: unverified checks in this experiment",
  ],
  CONSTRAINT_VIOLATION: ["当前候选违反约束", "Candidate violates constraints"],
  invalidJSON: [
    "JSON 解析失败；保留原输入，请修正格式",
    "JSON parse failed; prior input preserved; fix syntax",
  ],
  saveHelp: [
    "文件仅含离线输入、schema 与算法版本，不含游戏存档。",
    "File contains offline input, schema and solver version, not a game save.",
  ],
  game: ["应用到游戏：未实现", "Apply to game: not implemented"],
  through: ["直行锁定", "Through locked"],
  excluded: ["排除", "Excluded"],
  reachable: ["离线有向图可达", "Offline directed graph reachable"],
  bound: ["中心线位置误差界 (m)", "Centerline position error bound (m)"],
  selection: ["选中", "Selected"],
  from: ["输入车道", "Input lane"],
  to: ["输出车道", "Output lane"],
};
