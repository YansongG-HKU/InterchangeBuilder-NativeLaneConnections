# InterchangeBuilder Native Lane Connections

[![Core CI](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml/badge.svg)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/YansongG-HKU/InterchangeBuilder-NativeLaneConnections)](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/releases)

《城市：天际线 II》InterchangeBuilder 的非官方原生道路端点选择与简体中文升级层。

> [!IMPORTANT]
> 本项目不隶属于 Colossal Order 或 Paradox Interactive。仓库源码树不包含游戏文件；可直接安装的二进制 Release 经仓库维护者确认已取得原作者 TheJof 的再分发许可，因此包含运行所需的 InterchangeBuilder 1.4.2 文件。原作者文件仍不受本仓库 MIT 许可证覆盖。

## 直接下载安装

1. 从 [v2.2.0 Release](https://github.com/YansongG-HKU/InterchangeBuilder-NativeLaneConnections/releases/tag/v2.2.0) 下载 `InterchangeBuilder-2.2.0-NativeLaneConnections.zip`。
2. 退出游戏，将 ZIP 解压到：

   ```text
   %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods
   ```

3. 解压后应存在 `Mods\InterchangeBuilder-2.2.0-NativeLaneConnections\InterchangeBuilder.dll`。
4. 在 Skyve II 中刷新模组，禁用订阅版 InterchangeBuilder（153013），启用本地 2.2.0 版本；不要同时加载两个副本。
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

环境要求：Windows PowerShell、.NET 8 SDK、已安装的《城市：天际线 II》，以及你合法取得的 InterchangeBuilder 1.4.2 完整目录。Node.js 是可选项；存在时会额外检查本地化 UI 包的 JavaScript 语法。

1. 将原模组完整文件放到 `vendor\InterchangeBuilder-1.4.2`。
2. 运行：

```powershell
.\scripts\build-upgrade.ps1 `
  -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

原模组位于其他目录时，可加上 `-OriginalModPath 'D:\Mods\InterchangeBuilder-1.4.2'`。构建结果位于 `artifacts\InterchangeBuilder-2.2.0`。

详细的端点规则与 Skyve II 本地安装步骤见 [README-upgrade.md](README-upgrade.md)。官方模组平台和 CS2 模组说明可参阅 [Paradox Mods](https://mods.paradoxplaza.com/) 与 [Cities: Skylines II Modding](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding)。

## 仓库结构

- `src/InterchangeBuilder.LaneConnections.Core`：与游戏无关的宽度、对齐和多臂节点算法。
- `src/InterchangeBuilder.LaneConnections`：游戏运行时集成与 Harmony 补丁。
- `tests/`：核心算法测试。
- `tools/InterchangeBuilder.Patcher`：在本地原模组副本中注入升级启动入口。
- `tools/InterchangeBuilder.RuntimeSmoke`：验证运行时目标仍与当前游戏程序集匹配。
- `ui/`：简体中文词条和规则提示样式。
- `scripts/build-upgrade.ps1`：本地完整构建、补丁、UI 本地化和冒烟验证流程。

## 许可证与贡献边界

本仓库原创升级层源码采用 [MIT License](LICENSE)。原版 InterchangeBuilder、游戏程序集、图标及其他第三方内容保持各自权利归属，详见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。二进制 Release 中的原版文件依据原作者许可提供；该许可不自动扩展到第三方重新发布。

---

## English summary

This is a native endpoint-alignment and Simplified Chinese upgrade layer for TheJof's InterchangeBuilder 1.4.2 (Paradox Mods ID 153013). It preserves centre alignment, adds width-aware left/centre/right candidates, follows native connection compatibility, resolves multi-arm junctions from pointer direction, and carries the selected alignment through placement. The ready-to-use binary release includes the required original-mod payload with redistribution permission confirmed by the repository maintainer; game files are never included.
