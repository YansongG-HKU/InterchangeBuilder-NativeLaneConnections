# InterchangeBuilder 离线实验室 / Offline lab

原Mod项目内隔离的**业余爱好研发实验室**，不是主业、商业交付或真实道路工程设计。研究 Cities: Skylines II 的高速出入口、立交、道路节点及 Mod 协作，以可复现的小实验积累经验，不以数量或固定工期驱动。

本目录迁入自固定源快照，后续只在目标Mod仓维护；当前整合尚未合并main。当前唯一执行计划为 [获批左转方案](../../docs/LEFT_TURN_PLAN.md)，[整合记录](../../docs/INTEGRATION.md) 说明来源、历史与权利。以下旧项目环境和测试说明保留其历史边界。 / This lab extends the same Mod project; integration does not establish a working game adapter.

## 当前阶段

- 已建立研究范围、开发规则、兼容性矩阵、验证模板及有限支持域的游戏外二维/三维连接预览 Demo。
- **尚无已验收立交、游戏内测试结果或 Paradox Mods 发布版本。**
- Road Builder、Anarchy 等是兼容目标，不是已经测试通过的承诺。
- 不把静态桥梁网格当作可通车道路，不将无碰撞摆放当作车道连接或寻路成功。

## 研究方向

1. 单个高速出口/入口：分流、汇流、车道增减和连接连续性。
2. 菱形、喇叭形等立交；复杂多层互通须在基础验证完成后再探索。
3. 道路节点、边/路段、车道连接、交叉口、曲线、坡度、标高、桥下净空、桥墩及地形。
4. Road Builder 自定义道路；Anarchy 等放置限制调整工具；按需评估 Traffic、Move It 等。
5. 原版基线与 Mod 组合分别验证；保存、重载、道路升级和 Mod 更新后复测。

## 文档导航

- [项目定位与路线图](docs/ROADMAP.md)
- [道路与节点规则](docs/ROAD_RULES.md)
- [Mod 兼容矩阵](docs/COMPATIBILITY.md)
- [开发与文档同步规则](docs/DEVELOPMENT.md)
- [测试与验收](docs/VALIDATION.md)
- [来源与证据](docs/SOURCES.md)
- [文档索引及变更影响记录](docs/DOCUMENTATION_INDEX.md)
- [实验记录模板](docs/EXPERIMENT_TEMPLATE.md)
- [贡献约定](CONTRIBUTING.md) · [智能体约束](AGENTS.md) · [变更记录](CHANGELOG.md)

## 不可跳过的原则

每次开发和提交均须**全面检查全部文档的一致性**，并同步更新 README、规则、兼容矩阵、测试、计划、变更记录等所有受影响文件；不受影响的文件也应在变更影响记录中给出无需修改理由。禁止只改代码/模型而留下过期说明，也不为凑数机械重写无关文档。

真实游戏验证前，状态只能是未测试、研究中或待验证。没有游戏环境时可以做资料、设计及离线准备，不得编造版本号、性能、截图、成功兼容或发布回执。

## 环境与发布

准备工作在云端进行；未经用户明确授权不得操作用户电脑、付费开机、安装未知工具或扩大账户权限。发布游戏资产需另行确认目标、版本和依赖；公开 GitHub 仓库不等于已发布游戏 Mod。

本仓库不附带游戏文件、第三方 Mod 二进制、账号凭据或未经许可的素材。当前尚未选择开源许可证；公开可见不意味着授予任意再分发许可，详见 [权利说明](RIGHTS.md)。非官方项目，与 Colossal Order、Paradox Interactive 及各 Mod 作者无隶属关系。

## 第一阶段设计需求 / Phase 1 design

需求仍是完整目标；现有游戏外连接预览 Demo 只覆盖有限子集，详见支持域与未完成项。 / Requirements remain the full target; a bounded offline connection demo now implements a subset. Game/Mod functionality remains unimplemented.

- [中英需求 / Requirements](docs/DESIGN_REQUIREMENTS.md)
- [曲线与车道契约 / Geometry and lanes](docs/GEOMETRY_AND_LANES.md)
- [交互与双语 / UX and i18n](docs/UX_AND_I18N.md)
- [验收场景 / Acceptance scenarios](docs/REQUIREMENTS_ACCEPTANCE.md)

研究参考暂定 CS2 Placemaker；借鉴“意图—预览—确认”而非宣称其已有立交求解能力。详细证据与社区线索见 [sources](docs/SOURCES.md)。

新增 [现实感与游戏实际 / Realism and gameplay](docs/REALISM_AND_GAMEPLAY.md)：服务/系统型、主线连续、回堵、集散、桥梁全包络及交通评价；不把现实工程参考当游戏硬规则。 / Design-only requirements; no runtime validation.

核心扩展：[两曲线与场地占用 / Curved site optimization](docs/CURVED_SITE_OPTIMIZATION.md)。城区紧凑、郊区展开少堆叠是可编辑偏好，与硬面积/高度/层数/禁建边界分开；不牺牲车道或流向来偷偷凑方案。 / Editable preferences never silently relax movement or site constraints.

## 可运行 Demo / Runnable demo

[运行、架构、支持域及误差 / Runtime, architecture and limits](docs/OFFLINE_DEMO.md) · [测试证据 / Test evidence](docs/OFFLINE_VALIDATION.md) · [依赖许可 / Dependency licenses](docs/THIRD_PARTY.md)

```sh
npm ci --ignore-scripts --cache /tmp/cs2-npm
npm run dev -- --port 4173
npm test
# With the server running and Chromium at /usr/bin/chromium:
npm run test:browser
npm run test:3d
npm run build
```

打开本机 `http://127.0.0.1:4173`。可以改参数、选交会、检查真实计算、切换中英和导入导出 JSON。默认 full 案例有紧转向风险；partial 案例 OD 候选仍为 UNKNOWN；独立车道实验另显示自身状态，不能据此推断所有二维检查均无冲突。三维和独立纵断面已可交互；完整结构面积、净空、地形、游戏路径与兼容性仍未验证。 / Open the loopback URL on the same machine. Edit, select crossings, inspect computed candidates, switch locale, import/export JSON. Full fixture exposes tight-turn failures; partial OD remains UNKNOWN; the separate lane experiment reports its own state. Orbit/pan/zoom, complete OD highlighting and independently computed profiles are available. No public preview deployment or game release.

选择 `elevated-full` 查看高架全连接预览；选择 OD 会高亮入口—匝道—出口，支持旋转/平移/缩放、俯视复位及纵断面。高度和坡度参数会重算场景，红色无效连接保留。 / Choose `elevated-full`, select an OD and inspect its full connection in 3D or profile. [高差支持域与迁移 / Elevation and migration](docs/VERTICAL_PROFILES.md) · [场景契约 / Scene contract](docs/THREE_D_DESIGN.md).

## 下一阶段：一个完整深度案例 / Next: one complete deep case

[新版研究与执行门槛](docs/DEEP_CASE_PLAN.md)：以逐车道拓扑、三维几何、独立检查与游戏映射贯通一个案例；本次仅研究/文档，算法、独立真值和游戏验证均未执行。现有Demo历史测试不等于新目标完成。 / Research/design only; no new algorithm, independent oracle or game validation has been executed.

## 显式直尾实验 / Explicit-tail experiment

打开 `/curve-lab.html`，独立于原基线；支持有限真实直尾、同向横移/局部左转、参考检查与本地碰撞许可切换。见 [实验契约](docs/EXPLICIT_TAIL_EXPERIMENT.md) 与 [实测结果](evidence-tail/RESULTS.md)。`npm run test:tail`、`npm run test:tail-browser`、`npm run evidence:tail`；中部高跨生成及游戏适配未实现。 / New bounded experiment, not a replacement for baseline or the stable Mod.

## 2026-10-27 V2 local extension / 本地第二切片

Current additional capability: [automatic orthogonal crossings and actual curved context corridors](docs/EXPLICIT_TAIL_PHASE2.md). V1 statements about unsupported intermediate generation remain true for its missing-thickness/unsupported-shape inputs; V2 adds a constrained automatic family, not arbitrary3D routing. Full same-version regression and independent-review evidence are separate in evidence-tail-v2. Game/Anarchy/C#/remote CI remain NOT_RUN; no push/PR change. / 新增有界自动上跨、真实主路走廊与导入竞态检查，不改稳定Mod。

Current direction / 当前目标: [algorithm validation → Mod migration](../../docs/MOD_CURVE_MIGRATION.md). The Lab is only a curve calculation/comparison/test harness. Current algorithm verification extends rigid-direction invariance and bounded curved/oblique profiles; older +X/orthogonal-only statements document V2, not the current superset. Pure C# candidate construction remains disabled/unwired; independent local planar actual-output validation is now implemented (see the root migration contract). External-road/vertical C# checks and game build/apply remain unimplemented. No new Demo product features. Standard netstandard2.0 build and 13 xUnit tests now pass, including 192 detailed contracts; net48/Anarchy/game/remote CI remain NOT_RUN. Historical execution records are preserved.
