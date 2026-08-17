using System;
using System.Reflection;
using HarmonyLib;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

/// <summary>
/// Optional, reflection-only bridge. The upgrade never bundles, enables, or
/// hard-depends on Anarchy; it only registers this custom tool when Anarchy is
/// already installed and reports the user's actual runtime toggle state.
/// </summary>
internal static class AnarchyCompatibility
{
    private const string ToolId = "InterchangeBuilder.Curve";
    private const string UiSystemTypeName = "Anarchy.Systems.Common.AnarchyUISystem";

    private static Type? _uiSystemType;
    private static object? _registeredSystem;
    private static PropertyInfo? _enabledProperty;
    private static MethodInfo? _tryAddToolMethod;
    private static int _scanCountdown;
    private static bool _loggedAvailable;
    private static string? _lastError;

    internal static string RefreshStatus()
    {
        try
        {
            if (_uiSystemType == null)
            {
                if (_scanCountdown-- > 0)
                {
                    return "碰撞规则：原版（未安装 Anarchy）";
                }

                _scanCountdown = 120;
                _uiSystemType = AccessTools.TypeByName(UiSystemTypeName);
                if (_uiSystemType == null)
                {
                    return "碰撞规则：原版（未安装 Anarchy）";
                }

                _enabledProperty = AccessTools.Property(_uiSystemType, "AnarchyEnabled");
                _tryAddToolMethod = AccessTools.Method(_uiSystemType, "TryAddTool", new[] { typeof(string) });
                if (_enabledProperty == null || _tryAddToolMethod == null)
                {
                    return "碰撞规则：原版（Anarchy 版本接口不兼容）";
                }
            }

            World? world = World.DefaultGameObjectInjectionWorld;
            object? system = world?.GetExistingSystemManaged(_uiSystemType);
            if (system == null)
            {
                return "碰撞规则：原版（Anarchy 正在初始化）";
            }

            if (!ReferenceEquals(system, _registeredSystem))
            {
                _tryAddToolMethod?.Invoke(system, new object[] { ToolId });
                _registeredSystem = system;
                if (!_loggedAvailable)
                {
                    _loggedAvailable = true;
                    UpgradeLog.Info("Optional Anarchy integration detected; registered tool InterchangeBuilder.Curve.");
                }
            }

            bool enabled = _enabledProperty?.GetValue(system) is bool value && value;
            return enabled
                ? "碰撞规则：Anarchy 已开启（允许忽略碰撞/净空限制）"
                : "碰撞规则：原版（Anarchy 已安装但未开启）";
        }
        catch (Exception exception)
        {
            string error = $"{exception.GetType().Name}: {exception.Message}";
            if (error != _lastError)
            {
                _lastError = error;
                UpgradeLog.Warn("Optional Anarchy status check failed: " + error);
            }
            return "碰撞规则：原版（Anarchy 兼容状态读取失败）";
        }
    }
}
