using System;
using System.Reflection;
using Colossal.UI.Binding;
using Game.UI;
using HarmonyLib;

namespace InterchangeBuilder.LaneConnections;

internal static class UpgradeUiBindings
{
    private const string DefaultEndpointStatus =
        "端点端口：将鼠标移到道路端点的左、中、右或具体车道窗口上进行选择。";

    private static readonly Type? UiType = AccessTools.TypeByName(
        "InterchangeBuilder.Systems.InterchangeBuilderUISystem");
    private static readonly PropertyInfo? InstanceProperty = AccessTools.Property(UiType, "Instance");
    private static readonly MethodInfo? AddBindingMethod = AccessTools.Method(
        typeof(UISystemBase),
        "AddBinding",
        new[] { typeof(IBinding) });

    private static object? _owner;
    private static ValueBinding<string>? _endpointBinding;
    private static ValueBinding<string>? _anarchyBinding;
    private static string _endpointStatus = DefaultEndpointStatus;
    private static string _anarchyStatus = "碰撞规则：正在检测 Anarchy…";
    private static string? _startDescription;
    private static string? _endDescription;

    internal static void Attach(object uiSystem)
    {
        if (ReferenceEquals(_owner, uiSystem))
        {
            return;
        }

        if (AddBindingMethod == null)
        {
            UpgradeLog.Warn("Upgrade UI bindings were not added because UISystemBase.AddBinding was not found.");
            return;
        }

        try
        {
            var endpoint = new ValueBinding<string>(
                "InterchangeBuilder",
                "endpointStatus",
                _endpointStatus);
            var anarchy = new ValueBinding<string>(
                "InterchangeBuilder",
                "anarchyStatus",
                _anarchyStatus);
            AddBindingMethod.Invoke(uiSystem, new object[] { endpoint });
            AddBindingMethod.Invoke(uiSystem, new object[] { anarchy });
            _owner = uiSystem;
            _endpointBinding = endpoint;
            _anarchyBinding = anarchy;
            UpgradeLog.Info("Added live endpoint-port and Anarchy status bindings to the Chinese UI.");
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Upgrade UI bindings could not be added: {exception.GetType().Name}: {exception.Message}");
        }
    }

    internal static void Detach(object uiSystem)
    {
        if (!ReferenceEquals(_owner, uiSystem))
        {
            return;
        }

        _owner = null;
        _endpointBinding = null;
        _anarchyBinding = null;
    }

    internal static void RefreshRuntimeStatus()
    {
        EnsureAttached();
        string status = AnarchyCompatibility.RefreshStatus();
        if (status == _anarchyStatus)
        {
            return;
        }

        _anarchyStatus = status;
        _anarchyBinding?.Update(status);
    }

    internal static void SetEndpointSelection(ConnectionSelection selection)
    {
        if (selection.Role == EndpointRole.Start)
        {
            _startDescription = selection.ChineseDescription;
            _endDescription = null;
        }
        else if (selection.Role == EndpointRole.End)
        {
            _endDescription = selection.ChineseDescription;
        }
        else
        {
            _endpointStatus = selection.ChineseDescription;
            _endpointBinding?.Update(_endpointStatus);
            return;
        }

        PublishEndpointDescriptions();
    }

    internal static void SetSnapshot(SnapshotConnections connections)
    {
        _startDescription = connections.Start?.ChineseDescription;
        _endDescription = connections.End?.ChineseDescription;
        PublishEndpointDescriptions();
    }

    internal static void ClearEndpointStatus()
    {
        _startDescription = null;
        _endDescription = null;
        _endpointStatus = DefaultEndpointStatus;
        _endpointBinding?.Update(_endpointStatus);
    }

    internal static void SetPlacementSafetyNotice(int retainedEdges)
    {
        _endpointStatus = $"安全保护：已保留 {retainedEdges} 条游戏生成的道路，未因端点匹配超时而误删。";
        _endpointBinding?.Update(_endpointStatus);
    }

    private static void PublishEndpointDescriptions()
    {
        if (!string.IsNullOrWhiteSpace(_startDescription) && !string.IsNullOrWhiteSpace(_endDescription))
        {
            _endpointStatus = _startDescription + "；" + _endDescription;
        }
        else
        {
            _endpointStatus = _endDescription ?? _startDescription ?? DefaultEndpointStatus;
        }

        _endpointBinding?.Update(_endpointStatus);
    }

    private static void EnsureAttached()
    {
        if (_owner != null)
        {
            return;
        }

        object? instance = InstanceProperty?.GetValue(null);
        if (instance != null)
        {
            Attach(instance);
        }
    }
}

internal static class UpgradeUiCreatePatch
{
    public static void Postfix(object __instance) => UpgradeUiBindings.Attach(__instance);
}

internal static class UpgradeUiDestroyPatch
{
    public static void Prefix(object __instance) => UpgradeUiBindings.Detach(__instance);
}
