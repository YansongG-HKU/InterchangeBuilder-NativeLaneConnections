# 显式直尾实验 V1 / Explicit-tail experiment V1

状态：隔离Lab的有限实现，基于PR #9 `a13fa7919abcd4689bff035f65bd6d618978cc2b`。本轮本地分支不推送、不更新PR，不替换稳定Mod。Issue #10及两条补充是需求依据；6→4+2不作为前置。 / Bounded offline implementation, not a runtime replacement or completed Issue #10. No push/PR mutation in this task.

## 运行与边界 / Run and scope

在 `labs/interchange-demo` 执行 `npm run dev -- --port 4173`，打开 `/curve-lab.html`；原 `/` 基线仍可用。`npm run build`构建两个入口。 / Run from the lab directory; both baseline and experiment are built.

支持固定身份、行驶方向、明确侧偏和局部曲率的合成端口；统一宽度、零横坡。左右沿行驶方向法向定义，中间明确为选定参考线而非自动识别游戏内部车道组。常偏移度量 `m=1-offset*k`，使用接入点曲率`k/m`、纵坡`g/m`，m<=0.05或端口/路径宽度不一致为UNSUPPORTED。不挪动节点、不擅自换路；没有读取游戏节点。 / Synthetic fixed ports only, travel-frame offsets and constant-width zero-crossfall assumptions. Offset metrics are corrected; variable-width transitions/singular offsets are unsupported. No game node extraction.

生成器先预留长度为tail的目标切线直段，再构造五次Bezier接顺曲线。首端给定切向/曲率，曲线到直段连接二阶导数为0；曲线内部曲率连续，允许同向横移和有限局部左转，不等于通用走廊/拓扑求解。仅尝试0.8/1/1.2三档端导数尺度，硬约束筛选后按长度代理排序，未找到仅为SEARCH_BUDGET_EXHAUSTED。 / Reserve the finite line first, then a quintic matching endpoint derivatives. Three bounded candidates, no global optimum. A failed candidate is not a proof of global infeasibility.

目标非零曲率且要求G2与严格直尾：PROVEN_CONSTRAINT_CONFLICT，仅证明这两个局部约束矛盾。允许G1时保留并显示曲率残差，不偷偷弯曲直尾。 / Strict straight tail and nonzero target curvature cannot both satisfy G2; G1 mode reports the residual.

端点纵断为独立cubic Hermite，按平面弧长分配到曲线和直段；平面直线可以有纵坡。生成器2048段弦长只是估计，检查器独立给出弧长上下界及坡度不确定性。 / Plan-straight is not elevation-flat; generation uses estimated stationing, independently bounded during inspection.

## 独立检查 / Independent inspection

`generator.js`使用显式五次控制点/二项式求值；`checker.js`不导入生成器，不采信valid/connected标记，用de Casteljau及控制多边形复算输出。手写参考在`tests/tail-fixtures`，含输入、预期和解析依据；反例实际输出保存在`evidence-tail/computation.json`，不是从生成器结果倒推答案。 / Handwritten reference geometry and explicit faults provide an independent oracle, not a generated valid flag.

检查G0、**有向**G1、端/段G2残差、标高/纵坡、显式线段组成的最大直尾长度。曲线细分使用控制点到弦的最大距离界、弦长下界和控制多边形长度上界；默认0.02m分辨率、深度16，超预算或导数正则性无法界定为UNKNOWN。导入输出高程限±10000m、坡度限±10（数值支持域，非工程许可）；上下文道路高程限±1000m。所有派生度量/显示点非有限时为NUMERICAL_FAILURE且无网格，禁止NaN比较落空。位置/角度/曲率/接头容差1e-7（各自m/rad/1/m），为实验参数而非CS2规范。 / Adaptive bounds and experimental tolerances are explicit; insufficient resolution never becomes a pass.

完整宽度按**平面中心线扫掠半径width/2的圆盘走廊**检查，包含转弯、接顺和尾段，属于明确保守几何模型，不是假称精确桥梁结构。对直线邻路做线段距离±曲线弦误差界；间距落入不确定带为UNKNOWN。非相邻中心线交叉/远离沿程的走廊重叠单独检查；局部偏移正则性用导数界保守核验，不能证明时UNKNOWN。 / Full-width swept-disc corridors, including transitions, are distinct from complete structural/earthwork envelopes.

合法接合豁免必须同时匹配`route=ramp`与命名road ID，且整个受检小段加半宽/误差的包围范围在显式box内；不豁免无关邻路或泛化整个端点附近。 / Pair identity AND bounded region are required; exemption is conservative and cannot suppress other roads.

纵坡极值解析求解，并用长度区间传播不确定性。检查器重验JSON无损传输后的**实际**几何；不拟合掉直尾。该转换只是离线传输，不是游戏路段转换。 / Export transport is revalidated without refitting; no game conversion claim.

## 四类基准与未支持项 / Four benchmark classes and gaps

1. 中间直尾：独立参考末段x120..160/y0，长度40m，两侧道路独立检查；缩短尾段/侵入有故障。
2. 同向横移：独立多项式`x=160t, y=30(10t³-15t⁴+6t⁵)`加40m线段；反向、错身份与断链故障。
3. 直/弯主路局部端口左转：+X→+Y，含非零源曲率及G1-only目标曲率。仅局部端口边界，不宣称沿完整曲线主路全幅接缝/复杂走廊已解决。
4. 同高端点中部上跨：**生成UNSUPPORTED**；可明确切换到手写参考检查。参考200m升至10m、40m平台、200m下降，解析峰值7.5%；正交平直下层道路完整20m宽重叠内，上层梁厚1.5m，最小梁底高差8.5m。检查降低边缘/增加厚度；不是只检查交点。该检查只支持+X直线路径与正交平直下层、已知厚度/零横坡、足够下层长度，其他组合UNSUPPORTED。地形/桥墩/真实车辆包络仍UNKNOWN/NOT_RUN。

The four groups cover tail, lateral shift, local straight/curved-port left turns and an equal-height crossing reference. General intermediate-elevation generation, curved full-width clearance, varying width/crossfall, terrain, piers, actual lanes and game conversion remain unsupported or untested. No low-crossing/tunnel inference is made.

## 模式与交互 / Modes and UI

正常和无碰撞共享同一输出和碰撞事实。本实验无碰撞只放宽明确的**合成building类别**；道路碰撞、断链、接反、直尾不足、净空失败和UNKNOWN仍阻塞。显示“已检测/按模式允许”，不是无冲突。仅离线预览许可，不对接/改变Anarchy全局状态。 / Only synthetic building collisions may be relaxed; geometry/road/clearance/uncertainty failures remain blocking. No third-party compatibility claim.

参数/模式变更增加revision并同步重验，输入中间态立即禁用导出/过期许可；语言不改几何，导出绑定input/output/revision，导入忽略旧验收并重算。三维只消费验收读取的实际采样点；保留原场景旋转/平移/缩放与俯视及二维走廊。 / Revision-bound display/check/export, stale-input invalidation, lossless reload and same-data 3D/plan views.

## 测试与复现 / Tests

`npm test`覆盖旧基线及新增纯计算测试；`node tests/tail-browser.js`为真实Chromium新页面交互；`node tests/tail-evidence.js`记录输入/手写预期/实测/故障/源哈希；旧browser/browser3d/diagnostic-focus回归单列。浏览器固定viewport，有真实截图；新证据在`evidence-tail`，旧`evidence`恢复原样。最终命令、计数与保护检查见 [实验结果](../evidence-tail/RESULTS.md)。 / Evidence remains separate from historic snapshots; source/game/CI claims are distinct.

回滚本地实验提交即可恢复原基线。目标main、稳定C#、构建/发布配置与既有PR不改。 / Revert the experiment commit; stable runtime and remote refs are untouched.

Independent pre-delivery review found and fixed finite-but-overflowing imported profiles, malformed null join/segment shapes and invalid context elevation. Numeric output limits are computational bounds, not CS2 rules; nonfinite derived values fail closed with no drawable geometry.

## 2026-10-27 V2 local extension / 本地第二切片

Current additional capability: [automatic orthogonal crossings and actual curved context corridors](EXPLICIT_TAIL_PHASE2.md). V1 statements about unsupported intermediate generation remain true for its missing-thickness/unsupported-shape inputs; V2 adds a constrained automatic family, not arbitrary3D routing. Full same-version regression and independent-review evidence are separate in evidence-tail-v2. Game/Anarchy/C#/remote CI remain NOT_RUN; no push/PR change. / 新增有界自动上跨、真实主路走廊与导入竞态检查，不改稳定Mod。
