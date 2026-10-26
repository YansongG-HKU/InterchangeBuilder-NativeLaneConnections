# 游戏外连接预览 Demo / Offline connection preview

Version `offline-0.2.0`, schema `2`. Based on requirements head `bfccebe7467b2a8935306910b7763957ec98c207`, not initialization main. This is a bounded executable experiment, **not completion of all Phase 1 requirements**. / 基于需求分支的有限可运行实验，不代表全部一期需求完成。

## 运行 / Run

Node 24 + npm; Chromium for browser tests. No game, account, external service or backend required. / 无需游戏、账号、外部服务或后端。

```sh
npm ci --ignore-scripts --cache /tmp/cs2-npm
npm run dev -- --port 4173
# Open http://127.0.0.1:4173 on the same machine
npm test
npm run test:browser   # preserved 2D interaction regression
npm run test:3d        # true WebGL camera/OD/profile interaction; running dev server
npm run evidence
npm run build
# Serve dist with any local static HTTP server; examples are copied into dist/examples.
```

Loopback URL is **not a publicly accessible cloud preview**. Screenshots and reproduction commands are the deliverable; no public hosting was configured. / 上述地址仅本机可访问，未部署公开预览。

Choose a fixture, edit numeric values (metres/m²), choose a numbered intersection, inspect candidate diagnostics and click an error to locate it. Uncheck a turn to create partial coverage. Language is independent of geometry and traffic side. Advanced JSON edits curves/site/holes and all supported settings. Export downloads the input; import revalidates and recomputes it. Undo restores inputs only. / 选择案例、改参数、选编号交会、点击诊断定位；取消转向即为 partial；高级 JSON 可编辑曲线/地块/洞；导入后重算，撤销仅恢复离线输入。

## Architecture / 架构

| Layer | Files | Contract / 契约 |
|---|---|---|
| Data / 数据 | `src/core/model.js` | Versioned JSON, metres, stable directed port/lane IDs; no locale or game objects / 无语言或游戏对象 |
| Pure geometry / 纯计算 | `geometry.js`, `area.js`, `solver.js` | Input → deterministic result, no mutation, IO, random seed or game API / 确定性、无输入修改 |
| Browser / 浏览器 | `src/ui.js`, `i18n.js`, `style.css` | Same computed geometry for Three.js 3D, SVG plan/profile and reporting; zh/en, input undo, local JSON IO / 同一计算结果绘图和报告 |
| Adapter boundary / 适配边界 | `src/adapters/game.js` | Explicitly unavailable snapshot/apply; future revision check, atomic rollback and read-back required / 禁止未经验证的游戏应用 |
| Evidence / 证据 | `tests/`, `examples/`, `evidence/` | Unit/property/regression, real Chromium interaction, full fixture inputs and source hashes / 输入、实测及源文件哈希 |

Plain JavaScript ES modules + Vite; runtime packages are polygon-clipping and Three.js 0.186.1. No framework, persistence server, auth, paid service or game binary. UI uses one synchronous bounded solve after yielding a frame, invalidates prior results, and discards stale scheduled jobs. It is not a worker-based arbitrary-size interactive solver; inputs are bounded. / 轻量模块，无大框架；同步有限计算不是任意规模实时求解器。

## Frozen support domain / 冻结支持域

Exactly two regular planar cubic Bézier centreline curves, four control points each, coordinates ±2000m, ordinary motor roads, RHT/LHT. Stationing follows sampled horizontal chord length, not Bézier t or world-axis offsets. Closed non-self-crossing site polygon (4–40 vertices including closing point), up to ten closed all-height exclusions wholly inside site. Concave sites supported; disconnected site components, overlapping/near-tangent road generation, non-crossing proximity mode, terrain-aware vertical geometry, custom road types, extra schema keys and incompatible schema/solver versions are not supported. / 仅两条规则三次平面曲线、普通机动车、明确左右行；单个可凹场地和最多十个全高度禁建洞。其他对象/版本显式拒绝；无交点报告有限搜索未找到，不证明无交点或无解。

Frozen experimental ranges (not CS2 laws): anchor 10–300m; tangent handle 5–250m; lane width 2–5m; corridor width 24–60m; lateral reserve 0–20m; partial reservation cap 1–16,000,000m²; height 0–100m; levels integer 1–10; samples 128 or 256. Height and level caps are **stored but unassessed**, so candidates can never return overall PASS. These ranges bound workload and fixtures, not engineering acceptance. / 高度/层数仅保留独立硬约束，缺三维数据不能判定满足。

Intersection discovery uses segment pairs, distance deduplication 0.5m and a sine-angle rejection threshold 0.15. Multiple candidates require explicit index selection. Numerical cross-product epsilon 1e-10, geometry regularity epsilon 1e-7; tiny segment intersections can be missed. Near-collinear/coincident segments are not solved. The absence of sampled hits is `NO_CANDIDATE_FOUND`, never a proof. / 多交点需选中；采样交点有遗漏风险，重合/近切不生成。

Three candidate experiments use anchor and handle multipliers 0.8, 1, 1.2 under identical site/OD/hard settings. Endpoints come from local arc stations and directed normals; cubic controls follow local directed tangents (G0/G1, not G2). Offset-curvature risk is sampled at 513 parameters with `abs(kappa)*halfWidth < 0.8`; self-crossing checks use sampled line segments. These are prechecks, not certified complete envelope validation. / 有限三组局部接入候选，不保证全部曲线可解或全局最优。

Urban/custom rank candidates without detected 2D violations by partial reserved union area. Suburban prefers longer anchors; **fewer levels is still unassessed**, not a claim. Preferences never change caps, OD or geometry of the candidate set. No dimensionally mixed score or global Pareto claim. / 城区按面积排序；郊区偏好展开，不假称层数更少。

## Lane experiment and OD are separate / 逐车道实验与 OD 分开

6→4+2 is one direction, exact edges L1→M1…L4→M4, L5→R1, L6→R2. Each edge has its own computed cubic, direction, endpoints and tangents. Local A+ fork terminal spreads 45m outward and its tangent rotates using `unit(T + side*0.7*N)`; these are explicit experimental settings frozen in the solver version. LHT mirrors lateral semantics. Crossing connector pairs are diagnosed. Lane sweep collisions/gore shape/traffic allocation are not certified. This local fork does **not** connect to B and is **not** included in the four-arm OD land totals. / 真实六条有向连接及几何，不是只做数量加法；但不是完整四臂车道实现，未声称通行容量。

Four-arm OD baseline is fixed: A+/A-/B+/B-, four through movements plus eight cross-corridor turns, no U-turn. Through movements stay required. Removing any turn changes `full` to `partial`. Directed graph nodes are independent in/out ports; geometrical crossings do not add graph edges. Reachability and forbidden reachability are checked on this deliberately small **port-level planning graph**, not a full game lane graph. A required edge can be logically reachable while its geometry violates constraints; UI reports both independently. / 不以离线图论代替 CS2 真实节点、路径或逐车道 OD。

## Area and error model / 面积与误差

The displayed projection is the union of **sampled pavement strips**, not complete `A_projection` from the structural requirement. Each curve's left/right normal samples form quadrilateral strips with flat caps; polygon-clipping unions them (including overlaps only once). Partial reservation is the same model with lateral reserve added on both sides. Old baseline uses the unchanged source corridors at the same widths/reserve; new adds OD turning ribbons. It includes entire supplied corridor extents, not a hidden clipped influence zone. / 明确部分路面模型，保留完整输入道路，不把剪裁结果当总量。

Effective B = site minus union(exclusions). Report total area(R), inside area(R∩B), overflow area(R\B), ratio inside/area(B), and same-model added/new\old, removed/old\new, retained/intersection and net added−removed. Tiny numerical overflow >1e-6m² is a violation; the epsilon is computational, not an allowed encroachment. Polygon overlaps across hypothetical levels count once. **Ground footprint, supports, beams, guardrails, earthworks, local deck counts and complete P/G/R remain UNKNOWN.** / 不重复计多层投影；完整结构和地面占用不得冒充已知。

For n uniform Bézier parameter intervals, centreline linear interpolation deviation is bounded by `max(norm(p''(0)),norm(p''(1))) / (8*n*n)`. Chord-sum length underestimates true arc length. Its inversion is approximate; analytical point/tangent evaluation is at the recovered t. The bound does not certify offset area, tangent angle, arc station error, intersection completeness or topology. `evidence/computation.json` records 128→256 area convergence **observations, not guaranteed error bounds**. Complete envelope area error is unknown and therefore never passes a full land constraint. / 曲线位置界不等于面积误差界；不把代理/部分面积当准确包络。

## Status, recovery and limitations / 状态、恢复与限制

- `NOT_RUN`: the separate lane experiment was skipped because anchor prerequisites failed; no lane geometry/checks were run. / 接入范围前置条件失败，独立车道实验未运行。
- `INPUT_INVALID`: malformed inputs / 输入错误；JSON load preserves last valid input on validation failure.
- `UNSUPPORTED`: unsupported schema/type, stationary/self-crossing source or near-tangency / 不支持版本/道路/退化几何。
- `CONSTRAINT_VIOLATION`: specific candidate's known planar conflict / 候选已知二维冲突。
- `NO_CANDIDATE_FOUND`: bounded search failed / 有限候选未命中，**not PROVEN_INFEASIBLE**。
- `UNKNOWN`: the named experiment has incomplete checks; OD candidate status does not cover the separate lane experiment. `splitStatus` and `splitDiagnostics` report the fork independently. / 指定实验仍有未验证项；OD 候选状态不覆盖独立车道实验，后者另列状态及诊断。

Default full skew fixture deliberately exposes two tight-turn offset risks. The partial fixture provides a usable interactive numerical loop while retaining UNKNOWN for structure/game checks. Deferred: certified offset sweeps, integrated full lane-level interchange, merge/drop policies, arbitrary/close curves, terrain/structural clearance, direct road dragging and advanced layered views, unit display switching, gameplay lifecycle, free-flow/control assignment, traffic capacity and true local stacking. No Apply, game save editing, Mod integration or game release is implemented. / 延期项目不伪装完成。

Rollback: delete the independent checkout or revert this feature branch; there are no game/save/database migrations. Source branches and unrelated repositories stay untouched. / 可删除独立目录或回滚分支，无游戏/数据库副作用。

## Review fixes / 审查修正

The schema accepts only its own named fields via a frozen `Set(Object.keys(defaultCase()))`. JSON own keys named `constructor`, `toString`, `__proto__` and other non-schema names are rejected as UNSUPPORTED. This fixes a validation-contract mismatch; no execution or prototype-pollution exploit has been established. / 严格自有字段白名单，修复契约偏差，不夸大为已证实安全漏洞。

Lane labels are a responsive HTML list instead of overlapping text at tightly spaced curve origins. Each label selects the corresponding thick white computed connector without changing inputs or results; all six mappings remain readable at 390px in both languages. / 标签与图形联动，独立实验仍不计入 OD 面积。

File reads and example fetches share an input-request revision. A newer load, reset, recompute, undo, direct JSON submission, numeric/JSON typing, lane selection or language change invalidates older loads; rejected stale completions cannot change the model or diagnostics. Language changes cancel pending loads but preserve already computed geometry. / 统一加载代次，新的用户意图使旧请求失效；语言切换保留已有计算结果。

A through-only case with anchor=10m and handle=100m demonstrates separate statuses: OD candidate UNKNOWN while local lane connectors produce CONSTRAINT_VIOLATION. Neither is summarized as all planar checks passed. / 两实验分开报告，不互相掩盖诊断。

## Three-dimensional and vertical extension / 三维及高差扩展

See [vertical V1 and schema 2](VERTICAL_PROFILES.md) and [3D scene contract](THREE_D_DESIGN.md). Two independent mainline Hermite profiles and actual-anchor ramp profiles now drive a real orbit/pan/zoom scene, complete OD highlighting and selected connector/through profile. Choose `elevated-full` for a reproducible example. This extends the prior planar subset, without integrating the separate six-lane fork into a full lane interchange or validating game/structure behavior. Schema 1 import requires explicit acceptance of illustrative flat profiles; no silent zero terrain. / 已实现纵断面数学与真三维预览，未实现真实地形/结构/游戏路径。

## 下一阶段状态 / Next-stage status

现有能力保持不变。[深度案例计划](DEEP_CASE_PLAN.md) 是未执行的后续研究：集成逐车道拓扑、独立检查器、完整结构及游戏映射都不能从当前预览推断完成。 / Current runtime remains unchanged; new integrated lane, independent checker, structural and game gates are unexecuted.
