param(
    [string]$Configuration = "Release",
    [string]$Cities2ManagedPath = "",
    [string]$BasePackagePath = ""
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repositoryRoot "artifacts\InterchangeBuilder-2.5.0-NativeLaneConnections"
$runtimeProject = Join-Path $repositoryRoot "src\InterchangeBuilder.LaneConnections\InterchangeBuilder.LaneConnections.csproj"
$runtimeOutput = Join-Path $repositoryRoot "src\InterchangeBuilder.LaneConnections\bin\$Configuration\net48"
$patcherProject = Join-Path $repositoryRoot "tools\InterchangeBuilder.Patcher\InterchangeBuilder.Patcher.csproj"
$runtimeSmokeProject = Join-Path $repositoryRoot "tools\InterchangeBuilder.RuntimeSmoke\InterchangeBuilder.RuntimeSmoke.csproj"
$testsProject = Join-Path $repositoryRoot "tests\InterchangeBuilder.LaneConnections.Core.Tests\InterchangeBuilder.LaneConnections.Core.Tests.csproj"
$uiTranslationsSource = Join-Path $repositoryRoot "ui\InterchangeBuilder.zh-CN.json"
$uiLocalizationStyles = Join-Path $repositoryRoot "ui\InterchangeBuilder.localization.css"

if ([string]::IsNullOrWhiteSpace($BasePackagePath))
{
    $BasePackagePath = Join-Path $repositoryRoot "vendor\InterchangeBuilder-Base"
}
elseif (![System.IO.Path]::IsPathRooted($BasePackagePath))
{
    $BasePackagePath = Join-Path $repositoryRoot $BasePackagePath
}
$BasePackagePath = [System.IO.Path]::GetFullPath($BasePackagePath)
$baseAssembly = Join-Path $BasePackagePath "InterchangeBuilder.dll"

if ([string]::IsNullOrWhiteSpace($Cities2ManagedPath))
{
    if (![string]::IsNullOrWhiteSpace($env:CITIES2_MANAGED_PATH))
    {
        $Cities2ManagedPath = $env:CITIES2_MANAGED_PATH
    }
    elseif (![string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)}))
    {
        $Cities2ManagedPath = Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed"
    }
}

if ([string]::IsNullOrWhiteSpace($Cities2ManagedPath))
{
    throw "Cities: Skylines II managed assemblies were not found. Pass -Cities2ManagedPath or set CITIES2_MANAGED_PATH."
}
$Cities2ManagedPath = [System.IO.Path]::GetFullPath($Cities2ManagedPath)
if (!(Test-Path -LiteralPath (Join-Path $Cities2ManagedPath "Game.dll") -PathType Leaf))
{
    throw "Game.dll was not found under '$Cities2ManagedPath'. Pass the game's Cities2_Data\Managed directory."
}
if (!(Test-Path -LiteralPath $baseAssembly -PathType Leaf))
{
    throw "InterchangeBuilder.dll was not found under '$BasePackagePath'. Extract a published project package there or pass -BasePackagePath."
}

dotnet test $testsProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0)
{
    throw "Core tests failed with exit code $LASTEXITCODE."
}
$runtimeBuildArguments = @(
    "build",
    $runtimeProject,
    "-c", $Configuration,
    "--nologo",
    "-p:Cities2ManagedPath=$Cities2ManagedPath",
    "-p:InterchangeBuilderAssembly=$baseAssembly"
)
dotnet @runtimeBuildArguments
if ($LASTEXITCODE -ne 0)
{
    throw "Runtime build failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null
$payload = @(
    "InterchangeBuilder_linux_x86_64.so",
    "InterchangeBuilder_mac_x86_64.bundle",
    "InterchangeBuilder_win_x86_64.dll",
    "InterchangeBuilder.css",
    "InterchangeBuilder.mjs",
    "InterchangeBuilder.mjs.LICENSE.txt"
)
foreach ($file in $payload)
{
    $source = Join-Path $BasePackagePath $file
    if (!(Test-Path -LiteralPath $source -PathType Leaf))
    {
        throw "Required base-package payload is missing: $source"
    }
    Copy-Item -LiteralPath $source -Destination $artifactsRoot -Force
}
$baseImages = Join-Path $BasePackagePath "images"
if (!(Test-Path -LiteralPath $baseImages -PathType Container))
{
    throw "Required base-package image directory is missing: $baseImages"
}
Copy-Item -LiteralPath $baseImages -Destination $artifactsRoot -Recurse -Force
foreach ($packageDocument in @("README-upgrade.md", "LICENSE", "THIRD_PARTY_NOTICES.md", "CHANGELOG.md"))
{
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $packageDocument) -Destination $artifactsRoot -Force
}

$artifactUiBundle = Join-Path $artifactsRoot "InterchangeBuilder.mjs"
$artifactUiStyles = Join-Path $artifactsRoot "InterchangeBuilder.css"

# Keep the published UI module's execution path intact.  COUI does not expose
# the complete browser DOM surface used by the retired runtime localizer, so
# translations are applied to exact JavaScript string literals while building
# the local package.  No extra script is evaluated when the game loads the UI.
$localizedBundle = [System.IO.File]::ReadAllText($artifactUiBundle)
$localizedBundle = [regex]::Replace(
    $localizedBundle,
    '(?m)^\s*\* Author:.*\r?\n',
    '')
$localizedBundle = [regex]::Replace(
    $localizedBundle,
    '(?m)^\s*\* Version:.*$',
    ' * Version: 2.5.0')

# Add two live values supplied by the runtime upgrade. They are deliberately
# read-only: the game map remains the place where an endpoint port is chosen,
# and Anarchy remains controlled by Anarchy's own UI/hotkey.
$runtimeBindingAnchor = 'S=(0,r.bindValue)(o,"roadName","Taken from the start road"),j='
$runtimeBindingReplacement = 'S=(0,r.bindValue)(o,"roadName","Taken from the start road"),ibEndpointStatus=(0,r.bindValue)(o,"endpointStatus","端点端口：将鼠标移到道路端点选择车道窗口。"),ibAnarchyStatus=(0,r.bindValue)(o,"anarchyStatus","碰撞规则：正在检测 Anarchy…"),j='
$runtimeBindingCount = ([regex]::Matches($localizedBundle, [regex]::Escape($runtimeBindingAnchor))).Count
if ($runtimeBindingCount -eq 1)
{
    $localizedBundle = $localizedBundle.Replace($runtimeBindingAnchor, $runtimeBindingReplacement)
}
elseif ($runtimeBindingCount -ne 0 -or !$localizedBundle.Contains('"endpointStatus"'))
{
    throw "The endpoint and Anarchy runtime bindings could not be inserted safely."
}
$translations = Get-Content -LiteralPath $uiTranslationsSource -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
$translatedLiteralCount = 0
$existingTranslatedLiteralCount = 0
foreach ($entry in $translations.GetEnumerator())
{
    $sourceLiteral = ConvertTo-Json -InputObject ([string]$entry.Key) -Compress
    $targetLiteral = ConvertTo-Json -InputObject ([string]$entry.Value) -Compress
    if ($localizedBundle.Contains($sourceLiteral))
    {
        $localizedBundle = $localizedBundle.Replace($sourceLiteral, $targetLiteral)
        $translatedLiteralCount++
    }
    elseif ($localizedBundle.Contains($targetLiteral))
    {
        $existingTranslatedLiteralCount++
    }
}

# "InterchangeBuilder" is also the UI module identifier, so only replace the
# two human-visible occurrences and leave JSON.parse('{"id":"InterchangeBuilder"}') untouched.
$panelTitleSource = 'children:"InterchangeBuilder"'
$panelTitleTarget = 'children:"立交道路生成器"'
$panelTitleSourceCount = ([regex]::Matches($localizedBundle, [regex]::Escape($panelTitleSource))).Count
if ($panelTitleSourceCount -eq 1)
{
    $localizedBundle = $localizedBundle.Replace($panelTitleSource, $panelTitleTarget)
}
elseif ($panelTitleSourceCount -ne 0 -or !$localizedBundle.Contains($panelTitleTarget))
{
    throw "The InterchangeBuilder panel title could not be localized safely."
}

$floatingLabelSource = 'tooltipLabel:"InterchangeBuilder"'
$floatingLabelTarget = 'tooltipLabel:"立交道路生成器"'
$floatingLabelSourceCount = ([regex]::Matches($localizedBundle, [regex]::Escape($floatingLabelSource))).Count
if ($floatingLabelSourceCount -eq 1)
{
    $localizedBundle = $localizedBundle.Replace($floatingLabelSource, $floatingLabelTarget)
}
elseif ($floatingLabelSourceCount -ne 0 -or !$localizedBundle.Contains($floatingLabelTarget))
{
    throw "The InterchangeBuilder floating-button label could not be localized safely."
}

# The game toolbar is the only road picker.  Replace the mod's duplicate
# interactive catalog with a read-only summary of the vanilla selection.
$roadSummaryHookAnchor = 'function Qa({toolState:e,hint:a,showRoadSelector:t,roadName:l,roadIndex:s,roadCount:d,metrics:u,validationStatus:c,validationMessage:m,extraRow:h}){const g='
$roadSummaryHookReplacement = 'function Qa({toolState:e,hint:a,showRoadSelector:t,roadName:l,roadIndex:s,roadCount:d,metrics:u,validationStatus:c,validationMessage:m,extraRow:h}){const ibEndpointText=(0,r.useValue)(ibEndpointStatus),ibAnarchyText=(0,r.useValue)(ibAnarchyStatus),g='
$roadSummaryHookCount = ([regex]::Matches($localizedBundle, [regex]::Escape($roadSummaryHookAnchor))).Count
if ($roadSummaryHookCount -eq 1)
{
    $localizedBundle = $localizedBundle.Replace($roadSummaryHookAnchor, $roadSummaryHookReplacement)
}
elseif ($roadSummaryHookCount -ne 0 -or !$localizedBundle.Contains('ibEndpointText=(0,r.useValue)(ibEndpointStatus)'))
{
    throw "The road-summary runtime value hook could not be inserted safely."
}

$roadSelectorPattern = ',t&&\(0,i\.jsxs\)\("div",\{className:Pe,children:\[.*?\]\}\),h,\(0,i\.jsx\)\("div",\{className:da'
$roadSelectorRegex = [regex]::new(
    $roadSelectorPattern,
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
$roadSelectorCount = $roadSelectorRegex.Matches($localizedBundle).Count
$roadSelectorReplacement = ',t&&(0,i.jsxs)("div",{className:"ib-native-road-selection",children:[(0,i.jsx)("span",{children:"当前道路（游戏原生面板）"}),(0,i.jsx)("strong",{children:l}),ibEndpointText&&(0,i.jsx)("p",{className:"ib-endpoint-status",children:ibEndpointText}),ibAnarchyText&&(0,i.jsx)("p",{className:"ib-anarchy-status",children:ibAnarchyText}),(0,i.jsx)("p",{children:"要更换道路，请直接在游戏原生道路面板点击另一条道路；起点不会改变道路类型。"})]}),h,(0,i.jsx)("div",{className:da'
if ($roadSelectorCount -eq 1)
{
    $localizedBundle = $roadSelectorRegex.Replace(
        $localizedBundle,
        $roadSelectorReplacement,
        1)
}
elseif ($roadSelectorCount -ne 0 -or !$localizedBundle.Contains('className:"ib-native-road-selection"'))
{
    throw "The duplicate InterchangeBuilder road selector could not be replaced safely."
}

# Insert the rule notice into the existing React tree.  This uses only the
# bundle's existing JSX runtime and ordinary elements; it does not query or
# mutate the COUI DOM during module initialization.
$ruleAnchor = 'children:[(0,i.jsx)("div",{className:$e'
$ruleAnchorCount = ([regex]::Matches($localizedBundle, [regex]::Escape($ruleAnchor))).Count
$ruleCard = 'children:[(0,i.jsxs)("div",{className:"ib-connection-rules",children:[(0,i.jsx)("strong",{className:"ib-connection-rules-title",children:"道路端点连接规则"}),(0,i.jsx)("p",{children:"道路类型以游戏原生道路面板最后一次选择为准；起点道路不会覆盖它。"}),(0,i.jsx)("p",{children:"端点候选同时使用游戏原生宽度/8 米分区格规则和实际行车道位置；中心始终保留，任意单向、双向、奇偶车道及非对称道路会按方向匹配可用车道窗口。"}),(0,i.jsx)("p",{children:"把鼠标移到端点的左、中、右或具体车道窗口上再点击；起点和终点分别记忆，面板会实时显示新路与既有路的车道对应关系。"}),(0,i.jsx)("p",{children:"若安装 Anarchy，本工具会自动登记兼容并显示当前开关。Anarchy 只放宽碰撞/净空错误，不保证原版不会替换相交道路；匹配超时时模组会优先保留已生成道路，避免误删。"}),(0,i.jsx)("p",{children:"三岔及多臂路口请把鼠标移向目标道路分支后再点击；缺少车道元数据或特殊区域吸附网络会安全回退到原生规则。"})]}),(0,i.jsx)("div",{className:$e'
if ($ruleAnchorCount -eq 1)
{
    $localizedBundle = $localizedBundle.Replace($ruleAnchor, $ruleCard)
}
elseif ($ruleAnchorCount -ne 0 -or !$localizedBundle.Contains('className:"ib-connection-rules"'))
{
    throw "The endpoint-rule notice could not be inserted or verified safely."
}

[System.IO.File]::WriteAllText(
    $artifactUiBundle,
    $localizedBundle,
    [System.Text.UTF8Encoding]::new($false))
$localizationStyles = [System.IO.File]::ReadAllText($uiLocalizationStyles)
$artifactStyles = [System.IO.File]::ReadAllText($artifactUiStyles)
if (!$artifactStyles.Contains(".ib-native-road-selection"))
{
    [System.IO.File]::AppendAllText(
        $artifactUiStyles,
        [Environment]::NewLine + $localizationStyles,
        [System.Text.UTF8Encoding]::new($false))
}

foreach ($requiredText in @(
    'JSON.parse(''{"id":"InterchangeBuilder"}'')',
    "立交道路生成器",
    "道路端点连接规则",
    "当前道路（游戏原生面板）",
    '"endpointStatus"',
    '"anarchyStatus"',
    "实际行车道位置",
    "起点道路不会覆盖它",
    "打开立交道路生成器",
    "直线"))
{
    if (!$localizedBundle.Contains($requiredText))
    {
        throw "Localized UI bundle is missing required marker: $requiredText"
    }
}
if ($localizedBundle.Contains("interchange-builder-localization-2.1") -or
    $localizedBundle.Contains("MutationObserver"))
{
    throw "Runtime DOM localization code was unexpectedly included in the UI bundle."
}
if (($translatedLiteralCount + $existingTranslatedLiteralCount) -lt 20)
{
    throw "Only $translatedLiteralCount UI literals were translated and $existingTranslatedLiteralCount were already localized; the source bundle may have changed."
}
Write-Host "Applied $translatedLiteralCount Simplified Chinese UI literal translations; $existingTranslatedLiteralCount were already localized."

$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
if ($nodeCommand)
{
    & $nodeCommand.Source --check $artifactUiBundle
    if ($LASTEXITCODE -ne 0)
    {
        throw "Localized UI bundle failed JavaScript syntax validation."
    }
}

$upgradeAssembly = Join-Path $runtimeOutput "InterchangeBuilder.LaneConnections.dll"
$patcherArguments = @(
    "run",
    "--project", $patcherProject,
    "-c", $Configuration,
    "--",
    $baseAssembly,
    $upgradeAssembly,
    $artifactsRoot
)
dotnet @patcherArguments
if ($LASTEXITCODE -ne 0)
{
    throw "Assembly patching failed with exit code $LASTEXITCODE."
}

$patchedAssembly = Join-Path $artifactsRoot "InterchangeBuilder.dll"
if (!(Test-Path -LiteralPath $patchedAssembly))
{
    throw "Assembly patching did not produce $patchedAssembly."
}

Copy-Item -LiteralPath $upgradeAssembly -Destination $artifactsRoot -Force
Copy-Item -LiteralPath (Join-Path $runtimeOutput "InterchangeBuilder.LaneConnections.Core.dll") -Destination $artifactsRoot -Force
Copy-Item -LiteralPath (Join-Path $runtimeOutput "0Harmony.dll") -Destination $artifactsRoot -Force
Copy-Item -LiteralPath (Join-Path $runtimeOutput "System.Numerics.Vectors.dll") -Destination $artifactsRoot -Force

dotnet build $runtimeSmokeProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0)
{
    throw "Runtime smoke harness build failed with exit code $LASTEXITCODE."
}
$runtimeSmokeExecutable = Join-Path $repositoryRoot "tools\InterchangeBuilder.RuntimeSmoke\bin\$Configuration\net8.0\InterchangeBuilder.RuntimeSmoke.exe"
& $runtimeSmokeExecutable $artifactsRoot $Cities2ManagedPath
if ($LASTEXITCODE -ne 0)
{
    throw "Runtime metadata smoke test failed with exit code $LASTEXITCODE."
}

$archivePath = $artifactsRoot + ".zip"
Compress-Archive -LiteralPath $artifactsRoot -DestinationPath $archivePath -CompressionLevel Optimal -Force
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$checksumPath = $archivePath + ".sha256"
$checksumLine = $archiveHash + " *" + [System.IO.Path]::GetFileName($archivePath) + [Environment]::NewLine
[System.IO.File]::WriteAllText(
    $checksumPath,
    $checksumLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Upgrade package built at $artifactsRoot"
Write-Host "Ready-to-install archive: $archivePath"
Write-Host "SHA-256: $archiveHash"
