# InterchangeBuilder 2.5.0 全车道端点与生成安全升级说明

InterchangeBuilder Native Lane Connections 是独立开发的《城市：天际线 II》道路端点选择模组。GitHub 二进制 Release 已包含运行所需的全部项目文件；游戏文件从不包含在发布包中。

## 直接安装

1. 下载 `InterchangeBuilder-2.5.0-NativeLaneConnections.zip`。
2. 退出游戏，将 ZIP 解压到 `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods`。
3. 确认文件位于 `Mods\InterchangeBuilder-2.5.0-NativeLaneConnections\InterchangeBuilder.dll`，没有多套一层同名目录。
4. 在 Skyve II 中刷新模组，禁用或移除其他 InterchangeBuilder 版本，只启用本地 2.5.0 版本。
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

- 从新建道路 prefab 和既有道路实际 composition 读取行车道位置、方向、宽度、车道组及 carriageway，不按二车道或四车道写死。
- 处理任意单向、双向、奇数/偶数、非对称、公交专用及 Road Builder 等运行时生成道路；左右行驶通过实际 `Invert`/`Twoway` 标志判断，不依赖国家设置猜测。
- 游戏原生宽度、自由宽度与 8 米分区格候选全部保留；中心对齐始终存在。实际车道布局还会补充完整的中间车道窗口。
- 鼠标指向哪个端口就选择哪个端口。面板实时显示“起点/终点、左/中/右、偏移米数、新路车道 → 既有路车道”，点击前即可核对。
- 例如一条双车道单行路接八车道单行路时，可选择所有七个连续双车道窗口；四车道接两车道时仍保留左、中、右。
- 只对通过游戏 `NetUtils.CanConnect` 兼容性判断的道路、步道和轨道提供连接。
- 三岔及多臂节点由鼠标朝向决定目标道路分支；节点拆分操作保持原逻辑。
- 缺少可识别车道数据的旧存档、自定义网络、步道和轨道会回退到原生宽度规则，不会因此失去中心候选。
- 预览阶段选中的对齐会写入放置快照和 `CoursePos`，不会在落地时回到固定一侧。
- UI 提供简体中文翻译，并在面板中显示上述规则提示。

## 道路消失与 Anarchy

- 偏移端点可能由游戏生成一个新的侧向节点，而不是沿用原中心节点。2.5.0 会按道路几何走廊和节点连通性重新识别完整生成链，避免把成功生成的道路误判为失败。
- 如果偏移连接最终仍匹配超时，安全保护会保留游戏已经生成的永久道路，不再自动标记为 `Deleted`；面板与日志会提示保留数量，便于在地图上检查后手动撤销。
- Anarchy 是可选兼容项，不是依赖。检测到已安装的 Anarchy 后，模组会通过公开运行接口登记 `InterchangeBuilder.Curve`，并实时显示 Anarchy 是否开启。
- 模组不会替用户开启或关闭 Anarchy。Anarchy 只放宽碰撞/净空错误；原版网络更新仍可能替换交叉位置的道路，因此生成链识别和安全回滚仍然需要保留。

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

1. 将 `artifacts\InterchangeBuilder-2.5.0-NativeLaneConnections` 复制到 `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\InterchangeBuilder-2.5.0-NativeLaneConnections`。
2. 重启或刷新 Skyve II，让它重新扫描本地 `Mods` 目录。
3. 禁用或移除其他 InterchangeBuilder 版本，只启用本地 2.5.0 版本。
4. 通过 Skyve II 或 Steam 启动游戏，以便平台服务和当前 Playset 正确初始化。

准备测试时可在本地包目录名前加一个点来暂时禁用：`.InterchangeBuilder-2.5.0-NativeLaneConnections`。Skyve II 会通过添加或移除这个点来切换本地模组状态。
