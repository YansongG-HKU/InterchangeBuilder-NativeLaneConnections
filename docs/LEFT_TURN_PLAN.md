# 已批准的半直接左转深度样例 / Approved semi-direct left-turn deep case

状态：用户已批准后续分阶段实施；先完成仓库整合，本轮未实现新算法。材料、独立真值与容差尚待冻结。无游戏环境不阻止离线研究/实现，但游戏门槛保持NOT_RUN。 / Approved for subsequent staged execution; integration first, no new algorithm in this revision. Materials/oracles/tolerances remain unfrozen; missing game runtime does not block offline work.

这是本仓唯一当前执行计划；[深度方法与独立检查](../labs/interchange-demo/docs/DEEP_CASE_PLAN.md)和[参考Mod/P0恢复研究](../labs/interchange-demo/docs/research/road_mod_lessons.md)是其方法依据，不是另一套并行路线图。 / This is the authoritative execution plan; imported depth/recovery contracts remain supporting methodology.

1. 冻结一个半直接左转高跨/低跨样例：两条弯曲主线、逐车道方向/需求、可接入站区、地形/结构假设和独立预期图。端口含位置、方向、曲率、纵坡、宽度、车道侧与身份。资料不足明确合成基准，不从输出反推真值。 / Freeze one over/under case, lane oracle, attachment intervals and terrain/structure assumptions; ports carry geometry, lane side and identity.
2. 先定拓扑走廊及每个交叉的上下关系，再分段平面曲线与纵断面联合反馈，最后全包络与逐车道独立验收。禁止直接连全部端口再抬高。 / Topology and crossing order precede piecewise plan/profile co-design, envelope and independent lane validation.
3. 单段Hermite两端同高且坡度为0会恒平，不能凭此拱起跨越；必须设计中间高程/分段约束与连续性。低跨不自动等于隧道，地下方案需地形、cover/portal及游戏能力单独门槛。 / Equal-height zero-grade single Hermite is flat; crossing requires additional vertical structure. Underpassing is not inherently tunnelling.
4. 专业美观先满足硬约束，再比较曲率平顺、反曲/起伏、桥高/长度与占地等多目标；不强制对称，不以美观掩盖不可达/碰撞。保留搜索未找到、失败与UNKNOWN，不改OD/车道来凑解。 / Hard validity first; optimize smoothness, reversals/undulations, height/length and land without forced symmetry or silent demand changes.
5. 沿用H1–H5、D0–D4及M01–M11，按角度/高差/端坡/场地、镜像/反向、full/partial及城区/郊区变体独立验证。输出显式fromLane/toLane，检查器重建而非信任连接标记。 / Retain falsifiable hypotheses and independent reconstruction with controlled variants and explicit lane pairs.
6. 三维展示同一数据：单车道全路径、截面和冲突定位；展示/检查/导出一致，不虚构游戏接通。当前adapter能力全false，snapshot/apply抛UNSUPPORTED。 / Shared data powers inspection and export; the current adapter remains unsupported.
7. 游戏实际生成、车辆、保存重载、编辑撤销，以及道路/桥墩P0所有权/迟到/补偿必须另门槛。未知版本、基础服务或生命周期实现先列证据缺口，不把条件性timeout保护认作事务恢复。 / Game creation, vehicles, reload, undo and ownership/recovery remain separate gates.

退出条件依次为材料/预期冻结、一个完整离线案例及负例、独立复现/人工审查、固定游戏版本组合实测。当前均未完成；不以新增功能或测试数量计进步。整合PR不会自动开始算法或游戏发布。 / Exit via evidence freeze, one complete offline case plus negatives, independent/human review, then versioned game validation. None is completed by this integration.

## 2026-10-27 首个本地切片 / First local slice

用户已明确要求实施Issue #10，6→4+2不再是前置。已落实 [显式直尾实验](../labs/interchange-demo/docs/EXPLICIT_TAIL_EXPERIMENT.md)：手写基准、有限分段生成、独立验收及本地模式切换。完整左转走廊/拓扑、复杂宽度/净空与游戏阶段未完成；等高中部上跨仅支持明确的参考验收，不冒充生成。 / Local bounded slice implemented; full gates remain incomplete. This does not merge integration PR #9 or authorize publishing this experimental branch.

Local V2 now implements bounded straight-plan overpass generation and finite curved main-road context validation; this does not complete general curved over/under left-turn routing. See [contract](../labs/interchange-demo/docs/EXPLICIT_TAIL_PHASE2.md). User authorized further local implementation; public upload remains unapproved for this experimental branch.

Latest user direction: stop treating Demo as a separate product; it verifies left/right-turn curve algorithms for migration. [Current migration contract](MOD_CURVE_MIGRATION.md) supersedes any Lab-only permanent restriction: pure Core additions are authorized on isolated branches, default old runtime remains. Planar candidate family is migrated; engine conversion and game validation await actual external contract.

Current local milestone: [independent C# planar verifier](CORE_GEOMETRY_VALIDATION.md) inspects actual geometry and request binding; standard netstandard2.0 build and xUnit now pass. No runtime activation or game permission. / 已迁入独立平面验收，未接入游戏。

2026-10-28 local extension: [curve primitives, bounded station/profile functions and inspected split candidates](CURVE_FUNCTIONS.md). The original inspector scope and game exclusions remain; experimental search is default-disabled. / 已扩充纯数学内核，未启用游戏路径。

Current extension: [hard-region routing and independent smoothing validation](RESTRICTED_ROUTING.md); bounded planar support, runtime disabled.

[Partial spatial proof checkpoint](SPATIAL_PROOF_CHECKPOINT.md): exact straight-prism references only; curved 3D/global self-proof and selection integration remain incomplete. Current runtime/game boundaries unchanged.

[Nonstraight exact quarter-arc proof slice](QUARTER_ARC_PROOF.md) adds bounded flat annular-strip+tail full-domain certification and actual 3D candidate selection. General curved/nonconstant-profile proof remains incomplete.
