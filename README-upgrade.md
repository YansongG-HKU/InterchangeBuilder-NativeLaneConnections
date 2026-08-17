# InterchangeBuilder 2.4.0 原生道路面板与端点选择升级说明

InterchangeBuilder Native Lane Connections 是独立开发的《城市：天际线 II》道路端点选择模组。GitHub 二进制 Release 已包含运行所需的全部项目文件；游戏文件从不包含在发布包中。

## 直接安装

1. 下载 `InterchangeBuilder-2.4.0-NativeLaneConnections.zip`。
2. 退出游戏，将 ZIP 解压到 `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods`。
3. 确认文件位于 `Mods\InterchangeBuilder-2.4.0-NativeLaneConnections\InterchangeBuilder.dll`，没有多套一层同名目录。
4. 在 Skyve II 中刷新模组，禁用或移除其他 InterchangeBuilder 版本，只启用本地 2.4.0 版本。
5. 通过 Skyve II 或 Steam 启动游戏。

普通玩家只需要上述 ZIP，不需要源码、Visual Studio 或 .NET SDK。

## 输出道路选择规则

- 先在游戏原生道路面板选择要建造的道路，再打开立交道路生成器的建造模式。
- 模组自动记住原生面板最后一次选择，不再提供“确认并锁定”或“跟随起点”。
- 工具运行时仍可直接在原生道路面板点击另一条道路；模组会立即更新并清除旧预览。
- 起点道路只决定连接位置、切线方向和路口分支，绝不会覆盖原生面板选择。
- 模组面板只读显示当前道路，方便核对四车道、两车道以及自定义道路。
- 未在原生面板选择有效道路时，建造会停止并显示中文提示，避免静默使用错误道路。

## 道路端点规则

- 根据新建道路与既有道路的实际宽度生成横向连接候选点。
- 中心对齐始终保留；例如 24 米四车道接 16 米两车道时，可用鼠标选择左、中、右对齐。
- 分区道路同时提供游戏原生的 8 米单元吸附候选和自由宽度候选。
- 只对通过游戏 `NetUtils.CanConnect` 兼容性判断的道路、步道和轨道提供连接。
- 三岔及多臂节点由鼠标朝向决定目标道路分支；节点拆分操作保持原逻辑。
- 预览阶段选中的对齐会写入放置快照和 `CoursePos`，不会在落地时回到固定右对齐。
- UI 提供简体中文翻译，并在面板中显示上述规则提示。

中文文本在构建时由 `ui/InterchangeBuilder.zh-CN.json` 写入原 UI 副本；不会在 COUI 中运行 DOM 监听脚本。

## 从源码构建

将已发布 ZIP 中的顶层模组目录解压到 `vendor\InterchangeBuilder-Base`，然后执行：

```powershell
.\scripts\build-upgrade.ps1 -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

也可以直接指定基础运行包目录：

```powershell
.\scripts\build-upgrade.ps1 `
  -BasePackagePath 'D:\Mods\InterchangeBuilder-Base' `
  -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

## 开发包通过 Skyve II 本地测试

1. 将 `artifacts\InterchangeBuilder-2.4.0-NativeLaneConnections` 复制到 `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\InterchangeBuilder-2.4.0-NativeLaneConnections`。
2. 重启或刷新 Skyve II，让它重新扫描本地 `Mods` 目录。
3. 禁用或移除其他 InterchangeBuilder 版本，只启用本地 2.4.0 版本。
4. 通过 Skyve II 或 Steam 启动游戏，以便平台服务和当前 Playset 正确初始化。

准备测试时可在本地包目录名前加一个点来暂时禁用：`.InterchangeBuilder-2.4.0-NativeLaneConnections`。Skyve II 会通过添加或移除这个点来切换本地模组状态。
