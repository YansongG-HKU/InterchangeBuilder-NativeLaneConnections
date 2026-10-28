# 自动上跨与实际走廊 V2 / Automatic crossings and actual corridors V2

Local-only extension of explicit-tail V1, based on reviewed head7c140927344d705afd3ce21e8bf6124c419e4a91. No push, PR change, stable runtime or dependency change. / 仅云端本地实验，未发布游戏功能。

## Automatic candidate family / 自动候选

With verticalMode=intermediate, explicit known deckThickness and clearance, fixed +X collinear zero-curvature non-offset ports, and flat orthogonal full-width lower roads, generator constructs rise/plateau/fall line pieces with cubic Hermite z(s). Horizontal station is exact for these line pieces. End elevation and grade are retained. Plateau covers the union's outermost crossing intervals, including gaps between multiple crossings; height is max(endpoint heights, lower surface+thickness+required clearance+0.05m). Three bounded plateau margins0/2/5m are searched. All candidates undergo the independent checker and lossless transport recheck; no reference geometry is fed into generation.

当前支持精确定义的直线高架纵断面，不是弯匝道自动寻路：+X固定平面，正交、水平、常宽下层，已知梁厚、零横坡。多跨越共用连续平台，避免逐交点反复起伏。端点高程和纵坡按真实水平站距匹配；平面直尾允许有纵坡。平台多目标目前仅可行优先再按长度代理，不称专业最优。

Independent fixed oracle:440m total,200m smoothstep rise0→10m,40m plateau,200m descent, grade peak7.5% under an8% experimental bound, full overlap x200..240 underside8.5m for thickness1.5m. With required clearance8.45m, generator produces its own equal geometry. Changing segmentation at100m preserves physical grade and clearance. Two nearby crossing regions share one plateau; long/remote regions may exhaust this family. Fixed-route endpoint |dz|>gMax*L is a necessary conflict only for that fixed route, not for possible detours. Other candidate failures remain SEARCH_BUDGET_EXHAUSTED.

净空按完整重叠范围的解析高程极值减梁厚/下层高程，±1e-7m临界带返回UNKNOWN，不受无碰撞模式放宽。斜交/近乎平行、曲线交叉、缺厚度/缺下层高程、未知结构均拒绝或UNKNOWN，不只测中心。Original equal-height-overpass fixture still lacks explicit input thickness and stays generation UNSUPPORTED; new automatic-overpass fixture provides it. Negative absolute elevation does not imply a tunnel.

## Actual planar context / 实际主路走廊

Obstacles may provide2–6 Bezier controls with exact a/b endpoints. The inspector independently subdivides both ramp and context, sums both chord error bounds, and checks constant-width swept-disc corridors. Context unresolved subdivision or offset regularity is UNKNOWN. Display reads the same actual context controls, no ornamental fit.

Ports may bind roadId/roadT: inspector checks exact point, directed tangent, signed curvature, flat elevation/grade, width and lateral extent against that actual road. Synthetic unbound V1 ports remain labeled as such. Curved-context-left contains full finite source/target quadratic pieces plus an asymmetric third road. Nine left/middle/right combinations preserve fixed selected identities; they are not all feasible. A same-width whole-road shifted beyond the host cross-section is blocked with PORT_CONTEXT_LATERAL_EXTENT. Different host/port widths are PORT_CONTEXT_WIDTH_UNSUPPORTED, not a fabricated width transition. Explicit input/output width profiles are unsupported and cannot be silently reduced to endpoint width. These finite road pieces do not establish general junction seam or lane connectivity.

合法汇入仍只豁免指定道路对且整段包围范围在明示区域内；其他ID邻路不得豁免。严格直尾/G2非零曲率冲突规则保留。左右依行驶方向；旋转、平移、镜像需同步变换方向/侧偏/符号曲率和障碍物，测试验证等价。自动上跨族自身限+X，不宣称旋转输入也由该生成器支持。

## Interaction and numeric safety / 交互和数值

New controls edit max grade, required clearance and known thickness. Import start clears old selected output and disables export; pending, failed or cancelled import cannot revive old permission. Generation tokens reject slowB after editC/mode toggles/fastD or language changes. Mode changes only building placement permission, never geometric facts. Export contains actual input/output/revision and import rechecks it. Numeric bounds and nonfinite-derived failure from V1 review remain active.

## Evidence / 验证

Final full core, old2D/3D/focus, original tail and new phase2 browser suites plus build are recorded in evidence-tail-v2. Historical evidence and evidence-tail retained byte-for-byte. Real full-page screenshots include generation/reference status. Core cases cover auto generation, split invariance, multi/long overlap, grade/clearance/thickness boundaries, curved corridor vs chord, mismatched context width, nine side pairs, near-collinear/reverse, short turn, curvature signs, narrow corridors, rotations/mirrors and faults. Boundary/error tests demonstrate failure, not arbitrary input completeness.

初轮测试记录：第二跨越设x260时下降段缩为160m导致9.375%峰坡，程序正确拒绝；可行双跨越基准冻结x230，下降段190m且峰坡约7.895%，没有放宽8%上限。独立代码审查发现主路宽4m/端口宽8m仍通过，已修复为UNKNOWN并补回归。旧浏览器服务未运行引起一次connection refused，随后在同环境启动本地Vite解决，不是产品算法失败。

Still unsupported: arbitrary oblique/curved full-width3D clearance, automated curved over/under routing, variable widths/crossfall, terrain/pier/vehicle envelopes, complete lane graph, game adapter, ownership/rollback and actual CS2 placement. Game/Anarchy/C#/remote CI NOT_RUN. Revert this local phase2 commit to restore reviewed V1; no merge/release.

Current direction / 当前目标: [algorithm validation → Mod migration](../../../docs/MOD_CURVE_MIGRATION.md). The Lab is only a curve calculation/comparison/test harness. Current algorithm verification extends rigid-direction invariance and bounded curved/oblique profiles; older +X/orthogonal-only statements document V2, not the current superset. Pure C# candidate construction remains disabled/unwired; independent local planar actual-output validation is now implemented (see the root migration contract). External-road/vertical C# checks and game build/apply remain unimplemented. No new Demo product features. Standard netstandard2.0 build and 13 xUnit tests passed with 192 detailed contracts in the prior verifier milestone; the root curve-functions contract records the newer extension and current counts; net48/Anarchy/game/remote CI remain NOT_RUN. Historical execution records are preserved.
