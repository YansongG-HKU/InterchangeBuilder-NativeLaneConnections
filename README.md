# InterchangeBuilder Native Lane Connections

[![Core CI](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml/badge.svg)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/YansongG-HKU/InterchangeBuilder-NativeLaneConnections)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/releases)

InterchangeBuilder Native Lane Connections 是《城市：天际线 II》的道路端点选择与简体中文模组。

> [!IMPORTANT]
> 完整二进制 Release 已包含运行所需的全部项目文件，不包含任何《城市：天际线 II》游戏程序集。普通玩家只需下载并解压，无需准备其他模组或开发环境。

## 统一维护：Mod 与离线实验室 / Unified maintenance

本仓是唯一后续主项目。当前integration分支/Draft PR将精确来源 `CS2-Interchange-Lab@e790aa63926ebe7d499c5b811bad2f60248426cb` 的研究、Demo及证据整合到 `labs/interchange-demo`；**尚未合并main**，不修改原Mod运行时或发布包，也不自动合并PR #3/#4。研发归并不等于Demo已接通Mod：适配器仍UNSUPPORTED。 / This is the sole future maintenance repository; integration is proposed, not merged or game-connected.

- [统一文档索引](docs/DOCUMENTATION_INDEX.md) · [导入来源、许可与验证](docs/INTEGRATION.md)
- [已批准的左转深度计划](docs/LEFT_TURN_PLAN.md)：先整合，后按冻结材料/独立检查/游戏门槛推进；本轮不实现新算法。
- [离线Lab](labs/interchange-demo/README.md) · [历史测试证据](labs/interchange-demo/docs/OFFLINE_VALIDATION.md)

离线运行与验证（Node 24，真实浏览器脚本使用 `/usr/bin/chromium` 和4173端口）：

```sh
cd labs/interchange-demo
npm ci --ignore-scripts
npm run dev -- --port 4173
# Another terminal, same lab directory:
npm test
npm run test:browser
npm run test:3d
node tests/diagnostic-focus.js
npm run evidence
npm run build
```

脚本会改写lab/evidence；先保存历史基线，验证输出另存，见整合记录。Node工具链不移到根目录；原C#构建命令不变。现有CI只覆盖C# Core，本轮离线验证单列。 / Scripts overwrite evidence: preserve the imported baseline before running. Root C# and nested Node workflows remain separate.

许可例外：根MIT仅沿用原项目许可范围；导入Lab自身仍未选择开源许可，遵循其 [RIGHTS](labs/interchange-demo/RIGHTS.md) 与 [第三方声明](labs/interchange-demo/docs/THIRD_PARTY.md)，不因迁入而重新许可。 / Imported lab content is excluded from a new blanket MIT grant.

## 直接下载安装

1. 从 [v2.2.0 Release](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/releases/tag/v2.2.0) 下载 `InterchangeBuilder-2.2.0-NativeLaneConnections.zip`。
2. 退出游戏，将 ZIP 解压到：

   ```text
   %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods
   ```

3. 解压后应存在 `Mods\InterchangeBuilder-2.2.0-NativeLaneConnections\InterchangeBuilder.dll`。
4. 在 Skyve II 中刷新模组，禁用或移除其他 InterchangeBuilder 版本，只启用本地 2.2.0 版本。
5. 通过 Skyve II 或 Steam 启动游戏。

Release 同时提供 SHA-256 校验文件。普通玩家不需要安装 Visual Studio、.NET SDK，也不需要自己构建。

## 2.2.0 做了什么

- 按新旧道路实际宽度生成端点横向候选，并由鼠标选择最近位置。
- 始终保留中心对齐。四车道接两车道等宽度不一致场景可选择左、中、右对齐。
- 同时提供《城市：天际线 II》原生 8 米分区格吸附与自由宽度对齐候选。
- 使用游戏原生 `NetUtils.CanConnect` 规则过滤道路、步道和轨道连接。
- 在三岔和多臂节点上，根据鼠标朝向选择目标道路分支。
- 将预览选择持久化到放置快照、`CoursePos` 以及左右对齐标志。
- 加入简体中文 UI 和可见的道路端点规则提示。

目前的自动验证包括 12 项纯算法测试和 7 个运行时 Harmony 目标的元数据冒烟检查。运行时检查需要本机游戏程序集，因此公开 CI 只运行不依赖游戏文件的核心测试。

## 从源码构建

环境要求：Windows PowerShell、.NET 8 SDK、已安装的《城市：天际线 II》，以及项目的基础运行包。Node.js 是可选项；存在时会额外检查本地化 UI 包的 JavaScript 语法。

1. 将已发布 ZIP 中的顶层模组目录解压到 `vendor\InterchangeBuilder-Base`。
2. 运行：

```powershell
.\scripts\build-upgrade.ps1 `
  -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

基础运行包位于其他目录时，可加上 `-BasePackagePath 'D:\Mods\InterchangeBuilder-Base'`。构建结果位于 `artifacts\InterchangeBuilder-2.2.0-NativeLaneConnections`。

详细的端点规则与 Skyve II 本地安装步骤见 [README-upgrade.md](README-upgrade.md)。CS2 官方模组说明可参阅 [Cities: Skylines II Modding](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding)。

## 仓库结构

- `src/InterchangeBuilder.LaneConnections.Core`：与游戏无关的宽度、对齐和多臂节点算法。
- `src/InterchangeBuilder.LaneConnections`：游戏运行时集成与 Harmony 补丁。
- `tests/`：核心算法测试。
- `tools/InterchangeBuilder.Patcher`：在本地基础运行包副本中注入项目启动入口。
- `tools/InterchangeBuilder.RuntimeSmoke`：验证运行时目标仍与当前游戏程序集匹配。
- `ui/`：简体中文词条和规则提示样式。
- `scripts/build-upgrade.ps1`：本地完整构建、补丁、UI 本地化和冒烟验证流程。

## 许可证与贡献边界

原Mod源码沿用 [MIT License](LICENSE)；`labs/interchange-demo` 为明确例外，见上方权利说明。游戏程序集从不包含在仓库或发布包中；游戏 API 和 NuGet 依赖说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

---

## English summary

InterchangeBuilder Native Lane Connections is a Cities: Skylines II mod. It preserves centre alignment, adds width-aware left/centre/right candidates, follows native connection compatibility, resolves multi-arm junctions from pointer direction, and carries the selected alignment through placement. The ready-to-use release contains every required project file; game files are never included.

## 本地曲线实验 V1 / Local curve experiment

Issue #10 的首个隔离切片现可在Lab `/curve-lab.html`运行：有限直尾、五次接顺、同向横移、独立走廊检查与模式许可分离。参见 [契约与限制](labs/interchange-demo/docs/EXPLICIT_TAIL_EXPERIMENT.md)。中部跨越生成仍UNSUPPORTED；游戏NOT_RUN。本轮仅本地提交，不推送或更改稳定Mod。 / Bounded local implementation only; existing runtime is unchanged.

Local V2 experiment adds [automatic orthogonal overpass candidates and actual curved context corridors](labs/interchange-demo/docs/EXPLICIT_TAIL_PHASE2.md), with explicit unsupported boundaries and separate evidence. No stable runtime replacement, push or game validation.

Current focus: [curve algorithm verification → Mod Core migration](docs/MOD_CURVE_MIGRATION.md). The Lab is only a calculation/test harness. New default-disabled C# Core candidate construction is local and unconnected to runtime; game construction is not implemented or validated.

Current local milestone: [independent C# planar verifier](docs/CORE_GEOMETRY_VALIDATION.md) inspects actual geometry and request binding; standard netstandard2.0 build and xUnit now pass. No runtime activation or game permission. / 已迁入独立平面验收，未接入游戏。

2026-10-28 local extension: [curve primitives, bounded station/profile functions and inspected split candidates](docs/CURVE_FUNCTIONS.md). The original inspector scope and game exclusions remain; experimental search is default-disabled. / 已扩充纯数学内核，未启用游戏路径。

Current extension: [hard-region routing and independent smoothing validation](docs/RESTRICTED_ROUTING.md); bounded planar support, runtime disabled.

[Partial spatial proof checkpoint](docs/SPATIAL_PROOF_CHECKPOINT.md): exact straight-prism references only; curved 3D/global self-proof and selection integration remain incomplete. Current runtime/game boundaries unchanged.

[Nonstraight exact quarter-arc proof slice](docs/QUARTER_ARC_PROOF.md) adds bounded flat annular-strip+tail full-domain certification and actual 3D candidate selection. General curved/nonconstant-profile proof remains incomplete.

[General rising/descending Bezier external certificate](docs/BEZIER_EXTERNAL_3D.md) now checks continuous station envelopes against all supplied obstacles. General self remains UNKNOWN; no complete placement permission.

[Noncircular normal-strip global sufficient certificate](docs/NORMAL_STRIP_SELF_PROOF.md) now covers forward narrow-cone Bezier subsets; arbitrary self domains and game permission remain unsupported. Historical general-self UNKNOWN records are not rewritten.

2026-10-28 [面向可用Mod的代码事实调查与分阶段设计](docs/MOD_DELIVERY_DESIGN.md)：仅设计，本轮无新增算法/运行验证；先冻结真实接口并推进有限连接迁入。

2026-10-28 [structured numerical assurance](docs/NUMERICAL_ASSURANCE.md): all experimental result types explicitly remain EngineeringChecksOnly/IsMachineCertified=false/CanApplyToGame=false. User now authorizes stacked Draft PR publication; no merge/release.
