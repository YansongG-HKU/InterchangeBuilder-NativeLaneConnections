# InterchangeBuilder Native Lane Connections

[![Core CI](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml/badge.svg)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/YansongG-HKU/InterchangeBuilder-NativeLaneConnections)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/releases)

InterchangeBuilder Native Lane Connections 是独立开发的《城市：天际线 II》道路端点选择与简体中文模组。

> [!IMPORTANT]
> 完整二进制 Release 已包含运行所需的全部项目文件，不包含任何《城市：天际线 II》游戏程序集。普通玩家只需下载并解压，无需准备其他模组或开发环境。

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

## 2.3.0 本地开发版

- 将起点来源道路与最终输出道路拆成两个独立状态。
- 道路列表选择后显示“待确认”，确认后显示“已锁定”。
- 锁定道路不会再被起点节点或自由绘制起点静默覆盖。
- “跟随起点”改为明确的用户操作，不再作为隐藏回退逻辑。
- 面板持续显示输出道路、起点来源和当前状态；更换道路会重新开始预览。
- 最终建造前会校验确认状态，并在日志中记录输出道路和起点来源。

## 2.2.0 做了什么

- 按新旧道路实际宽度生成端点横向候选，并由鼠标选择最近位置。
- 始终保留中心对齐。四车道接两车道等宽度不一致场景可选择左、中、右对齐。
- 同时提供《城市：天际线 II》原生 8 米分区格吸附与自由宽度对齐候选。
- 使用游戏原生 `NetUtils.CanConnect` 规则过滤道路、步道和轨道连接。
- 在三岔和多臂节点上，根据鼠标朝向选择目标道路分支。
- 将预览选择持久化到放置快照、`CoursePos` 以及左右对齐标志。
- 加入简体中文 UI 和可见的道路端点规则提示。

目前的自动验证包括 17 项纯算法/状态机测试，以及道路锁定和端点补丁目标的运行时元数据冒烟检查。运行时检查需要本机游戏程序集，因此公开 CI 只运行不依赖游戏文件的核心测试。

## 从源码构建

环境要求：Windows PowerShell、.NET 8 SDK、已安装的《城市：天际线 II》，以及项目的基础运行包。Node.js 是可选项；存在时会额外检查本地化 UI 包的 JavaScript 语法。

1. 将已发布 ZIP 中的顶层模组目录解压到 `vendor\InterchangeBuilder-Base`。
2. 运行：

```powershell
.\scripts\build-upgrade.ps1 `
  -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

基础运行包位于其他目录时，可加上 `-BasePackagePath 'D:\Mods\InterchangeBuilder-Base'`。构建结果位于 `artifacts\InterchangeBuilder-2.3.0-NativeLaneConnections`。

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

本项目源码采用 [MIT License](LICENSE)。游戏程序集从不包含在仓库或发布包中；游戏 API 和 NuGet 依赖说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

---

## English summary

InterchangeBuilder Native Lane Connections is an independently developed Cities: Skylines II mod. It keeps the chosen output network locked separately from the start-road source, preserves centre alignment, adds width-aware left/centre/right candidates, follows native connection compatibility, resolves multi-arm junctions from pointer direction, and carries the selected alignment through placement. The ready-to-use release contains every required project file; game files are never included.
