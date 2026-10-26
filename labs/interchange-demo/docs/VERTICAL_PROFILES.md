# 高差与纵断面 V1 / Elevation and profile V1

Current: `offline-0.2.0`, schema 2. This is a bounded pure mathematical model and a complete **port-level connection preview**, not a complete lane interchange, game construction, structural design or terrain adaptation. / 有限纵断面数学与完整端口连接预览，不等于完整车道互通或游戏建路。

## Inputs and migration / 输入与迁移

Keep the two XY cubic curves unchanged. `vertical` adds `datum: project-local`, `terrain: unknown`, `source: user-defined|legacy-flat-accepted`, `maxGrade` and two `profiles` containing `z0,z1,g0,g1`. Both roads use the same local vertical datum. Elevations are metres; g=dz/ds is dimensionless, with UI percentages 100g. No degree conversion. `s` is horizontal stationing, using the existing sampled chord lengths. / 共同标高基准，坡度按水平弧长定义，不把角度当百分比。

Support: one Hermite profile per mainline; endpoint z in [-200,500]m, endpoint g in [-0.3,0.3], experimental absolute grade cap in [0.001,0.3]. These bounds constrain this experiment, **not CS2 or real engineering rules**. Extreme interior elevations may overshoot the endpoint range and are reported, not silently clamped. Terrain known-mode, crossfall/superelevation, piecewise profiles, foundations, tunnels and portal/cover rules are unsupported. A negative absolute z does not imply a tunnel. / 不暗中截断高程，不因负标高自动生成隧道。

Schema 1 has no heights. Direct solver validation rejects it; UI prepares a migration proposal and requires the explicit “Accept illustrative flat migration” button before replacing the current case. The proposal sets both roads to z=0 and g=0 and records `legacy-flat-accepted`; terrain and clearance remain unknown. Cancel preserves current input. Extra/invalid fields cannot bypass validation through migration. Schema 2 export/reload keeps all profile parameters. / 旧数据不静默当作真实零地形。

## Computation / 计算

For horizontal length L>0 and t=s/L:

`z(t)=(2t³−3t²+1)z0+(t³−2t²+t)Lg0+(−2t³+3t²)z1+(t³−t²)Lg1`.

Core expands this into coefficients a,b,c,d. Elevation extrema include both endpoints and real roots of `3at²+2bt+c=0` inside [0,1]. Grade extrema include endpoints and the root of `6at+2b=0`; peak absolute grade is measured in percent for display. Zero/nonfinite lengths or inputs are rejected before geometry. A 0→12m, L=200m ramp with flat endpoint grades has average 6% but **peak 9% at s=100m**. `L >= abs(delta z)/gMax` alone is not sufficient. Equal-height endpoints with differing grades can overshoot; all critical points are recorded. / 检查解析极值，而不是只检查均坡或离散样本。

Each generated ramp reads z and directional travel grade from its actual attachment stations and uses its own horizontal sampled length L. For an original centreline curvature κ and travel sign d=±1, offset o measured along the travel-left normal, the horizontal attachment metric is `m=1−o*d*κ`. Travel grade is `d*g_center/m`; nonpositive/near-zero m is unsupported. This corrects the fact that a constant lateral offset generally has a different station-distance metric. Reversing travel reverses grade semantics; through-profile display is reversed as well. / 不盲抄中心线坡度到偏移接入点。

The model assumes zero crossfall solely to define its illustrative pavement surface. Ramp endpoint height and grade match that model; this is **not verified full-width game/structural G1**. Planar Bézier points/tangents and offset curvature are analytical at the chosen parameters, but horizontal arc-length stationing and rendered surface meshes are sampled. Existing chord-length error therefore propagates into station locations, profile L and attached height/grade. We do not claim exact analytic-arc or certified mesh continuity; both core profiles and rendered geometry use the same frozen approximation. / 数值弧长与表面离散仍有误差，不夸称完整结构精确相切。

## Three-dimensional preview / 三维预览

`solver → verticalCandidate → connectionPreview` emits the only source for renderer geometry. Mainline strips carry their computed profiles; ramp strips carry independent Hermite profiles. The full selected OD path includes the directed offset incoming mainline segment, the actual ramp and the outgoing mainline segment; through movements traverse the corresponding whole source. No connecting route is fabricated for an excluded OD. Source roads remain visible when no candidate is generated. / 同一核心输出驱动三维与纵断面，没有装饰性另造曲线。

Choose `elevated-full` for a reproducible 12m-separated full preview; the longest candidate clears the current mathematical prechecks while remaining overall UNKNOWN. Choose a ramp OD and the profile view to see its local s, z, analytical peak grade and peak location. Through OD profiles follow travel direction but display the mainline reference-station profile, not an offset-lane distance profile. Ramp profiles use their own horizontal arc station. The SVG profile uses independently scaled axes; 3D has **no vertical exaggeration**. Invalid candidate ramps stay visible red, selectable and diagnosable. This is not a proof that the displayed arrangement is collision-free or game-buildable. / 红色无效方案保留，不把未知净空变绿灯。

The local 6→4+2 experiment remains a separate **2D** illustration; it does not provide the lane assignment for these full-width mainline/ramp strips and does not enter their OD area totals. / 独立六车道实验不替代完整互通预览。

## Honest status / 状态

`vertical.status=MATHEMATICAL_CHECKS_ONLY` means the implemented scalar-grade checks did not detect a violation, not overall feasibility. Known grade violations join candidate diagnostics with route ID, measured/limit %, station and approximate map location. Prior planar diagnostics remain independent and visible. Skipped profiles due to anchor range return NOT_RUN. Overall candidates never return game-ready PASS.

The displayed crossing-height difference compares the two mainline **centreline** heights at sampled plan intersections. It is not minimum full-width clearance; upper beam underside, lower traffic envelope, crossfall, supports and terrain are missing. Clearance, local independent deck count, height above terrain, full P/G/R footprints and game/traffic compatibility remain UNKNOWN. Vertical caps stored by the earlier model do not become validated simply because profiles can now be drawn. / 中心线高差不等于实体净空；高度上限缺地形仍未评估。

## Evidence and sources / 证据与来源

Tests cover the 6%-average/9%-peak counterexample, zero length, equal-height overshoot, endpoint conditions, reverse grade, common-datum translation, offset metric correction, unchanged XY/OD/area, invalid values, explicit migration, full and excluded preview paths, actual browser orbit/pan/wheel, profile selection, edits, save/reload and fallback. See [validation](OFFLINE_VALIDATION.md).

- [Canonical ASAM elevation page](https://simulation.pages.asam.net/opendrive-group/opendrive-antora-gen/ASAM_OpenDRIVE_Specification/v1.8.1/specification/10_roads/10_05_elevation.html): elevation is defined along horizontal road reference distance.
- [FHWA archived grade discussion](https://www.fhwa.dot.gov/publications/research/safety/13077/002.cfm): percent grade definition, not a game numeric limit.
- [CS2 Road Tools, 2023](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools): historical elevation/editing mechanisms, not current numerical engine constraints.
- [Three.js official APIs](https://threejs.org/docs/pages/OrbitControls.html), [renderer](https://threejs.org/docs/pages/WebGLRenderer.html), [geometry](https://threejs.org/docs/pages/BufferGeometry.html): library usage; pinned 0.186.1.

A nonzero-mainline-grade variation of the `elevated-full` fixture also checks incoming/emitted-ramp/outgoing 3D segment directions at every attachment within 0.01 radians of discretization tolerance (at 128 samples). This fixture-specific sampled check is not a certified all-curve or game G1 claim.

## 后续联合设计 / Future joint design

[深度案例](DEEP_CASE_PLAN.md) 将平纵横及结构包络联合验证；当前单段Hermite与中心线高差诊断不足以完成H4。 / Single-segment profiles and crossing height diagnostics do not satisfy future full-width structural validation. Existing bounded implementation is unchanged.
