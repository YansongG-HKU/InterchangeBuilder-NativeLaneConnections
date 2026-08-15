# InterchangeBuilder 2.2.0 原生端点选择升级说明

这是针对 TheJof 的 InterchangeBuilder 1.4.2（Paradox Mods ID 153013）制作的非官方升级层，不是独立模组。GitHub 仓库不包含原模组文件；本地构建会读取你合法取得的原版文件，并在 `artifacts\InterchangeBuilder-2.2.0` 生成一个单独的测试包。

## 道路端点规则

- 根据新建道路与既有道路的实际宽度生成横向连接候选点。
- 中心对齐始终保留；例如 24 米四车道接 16 米两车道时，可用鼠标选择左、中、右对齐。
- 分区道路同时提供游戏原生的 8 米单元吸附候选和自由宽度候选。
- 只对通过游戏 `NetUtils.CanConnect` 兼容性判断的道路、步道和轨道提供连接。
- 三岔及多臂节点由鼠标朝向决定目标道路分支；节点拆分操作保持原逻辑。
- 预览阶段选中的对齐会写入放置快照和 `CoursePos`，不会在落地时回到固定右对齐。
- UI 提供简体中文翻译，并在面板中显示上述规则提示。

中文文本在构建时由 `ui/InterchangeBuilder.zh-CN.json` 写入原 UI 副本；不会在 COUI 中运行 DOM 监听脚本。

## 构建

将原版 InterchangeBuilder 1.4.2 的完整文件放入 `vendor\InterchangeBuilder-1.4.2`，然后执行：

```powershell
.\scripts\build-upgrade.ps1 -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

也可以直接指定原版目录：

```powershell
.\scripts\build-upgrade.ps1 `
  -OriginalModPath 'D:\Mods\InterchangeBuilder-1.4.2' `
  -Cities2ManagedPath 'D:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed'
```

## 通过 Skyve II 本地测试

1. 将 `artifacts\InterchangeBuilder-2.2.0` 复制到 `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\InterchangeBuilder-2.2.0-NativeLaneConnections`。
2. 重启或刷新 Skyve II，让它重新扫描本地 `Mods` 目录。
3. 禁用订阅版 InterchangeBuilder（153013），启用本地升级版；不要同时加载两个副本。
4. 通过 Skyve II 或 Steam 启动游戏，以便平台服务和当前 Playset 正确初始化。

准备测试时可在本地包目录名前加一个点来暂时禁用：`.InterchangeBuilder-2.2.0-NativeLaneConnections`。Skyve II 会通过添加或移除这个点来切换本地模组状态。
