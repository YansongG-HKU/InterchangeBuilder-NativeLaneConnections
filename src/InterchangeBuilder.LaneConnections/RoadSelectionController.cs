using System;
using System.Collections;
using System.Reflection;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal static class RoadSelectionController
{
    private static readonly object Sync = new object();
    private static readonly RoadSelectionMemory State = new RoadSelectionMemory();
    private static readonly Type? ToolType = AccessTools.TypeByName(
        "InterchangeBuilder.Systems.InterchangeBuilderToolSystem");
    private static readonly FieldInfo? SelectedRoadPrefabField = AccessTools.Field(
        ToolType,
        "_selectedRoadPrefab");
    private static readonly FieldInfo? RoadChoicesField = AccessTools.Field(ToolType, "_roadChoices");
    private static readonly FieldInfo? ActiveModeField = AccessTools.Field(ToolType, "_activeMode");
    private static readonly MethodInfo? SetSelectedRoadPrefabMethod = AccessTools.Method(
        ToolType,
        "SetSelectedRoadPrefab");
    private static readonly MethodInfo? ClearPreviewMethod = AccessTools.Method(ToolType, "ClearPreview");
    private static readonly MethodInfo? RestartActiveModeMethod = AccessTools.Method(
        ToolType,
        "RestartActiveMode");
    private static readonly FieldInfo? NativeSelectedPrefabField = AccessTools.Field(
        typeof(NetToolSystem),
        "m_SelectedPrefab");
    private static readonly FieldInfo? NativeActivePrefabField = AccessTools.Field(
        typeof(NetToolSystem),
        "m_Prefab");
    private static readonly Type? UiType = AccessTools.TypeByName(
        "InterchangeBuilder.Systems.InterchangeBuilderUISystem");
    private static readonly PropertyInfo? UiInstanceProperty = AccessTools.Property(UiType, "Instance");
    private static readonly MethodInfo? SetValidationMessageMethod = AccessTools.Method(
        UiType,
        "SetValidationMessage");

    [ThreadStatic]
    private static int _explicitSelectionDepth;

    [ThreadStatic]
    private static int _requestedPrefabIndex;

    [ThreadStatic]
    private static Entity _selectionBefore;

    [ThreadStatic]
    private static string? _selectionOrigin;

    [ThreadStatic]
    private static int _sourceSelectionDepth;

    [ThreadStatic]
    private static Entity _sourceRequested;

    [ThreadStatic]
    private static int _restoreDepth;

    internal static void AttachAndEnforce(object tool)
    {
        ApplyRememberedSelection(tool);
    }

    internal static Entity GetAlignmentPrefab(Entity currentPrefab)
    {
        lock (Sync)
        {
            return FromKey(State.ResolveOutput(ToKey(currentPrefab)));
        }
    }

    internal static Entity ResolveNodeAlignmentPrefab(Entity currentPrefab, Entity nodePrefab)
    {
        lock (Sync)
        {
            Entity selected = FromKey(State.Selected);
            if (selected != Entity.Null)
            {
                return selected;
            }

            return currentPrefab != Entity.Null ? currentPrefab : nodePrefab;
        }
    }

    internal static bool BeginExplicitSelection(
        object tool,
        int requestedPrefabIndex,
        string origin)
    {
        _explicitSelectionDepth++;
        if (_explicitSelectionDepth != 1)
        {
            return false;
        }

        _requestedPrefabIndex = requestedPrefabIndex;
        _selectionBefore = ReadSelectedPrefab(tool);
        _selectionOrigin = origin;
        return true;
    }

    internal static void EndExplicitSelection(object tool, bool outermost)
    {
        if (_explicitSelectionDepth > 0)
        {
            _explicitSelectionDepth--;
        }

        if (!outermost)
        {
            return;
        }

        Entity selected = ReadSelectedPrefab(tool);
        int requestedPrefabIndex = _requestedPrefabIndex;
        string origin = _selectionOrigin ?? "explicit selection";
        _requestedPrefabIndex = -1;
        _selectionOrigin = null;

        if (selected == Entity.Null ||
            (requestedPrefabIndex >= 0 && selected.Index != requestedPrefabIndex))
        {
            return;
        }

        bool changed;
        lock (Sync)
        {
            changed = State.RememberPanelSelection(ToKey(selected));
        }

        if (!changed)
        {
            return;
        }

        string name = DescribePrefab(tool, selected);
        UpgradeLog.Info(
            $"Road remembered from {origin}: {name} ({DescribeEntity(selected)}). " +
            "Start-node inheritance is disabled.");
        if (selected != _selectionBefore)
        {
            InvalidatePreview(tool);
        }

        ShowMessage($"当前道路已切换为“{name}”。起点只决定连接位置，不会改变道路类型。");
    }

    internal static int BeginSourceSelection(bool inheritRoadFromNode)
    {
        if (!inheritRoadFromNode)
        {
            return 0;
        }

        _sourceSelectionDepth++;
        if (_sourceSelectionDepth != 1)
        {
            return 1;
        }

        _sourceRequested = Entity.Null;
        return 2;
    }

    internal static void EndSourceSelection(object tool, int context)
    {
        if (context == 0)
        {
            return;
        }

        if (_sourceSelectionDepth > 0)
        {
            _sourceSelectionDepth--;
        }

        if (context != 2)
        {
            return;
        }

        Entity source = _sourceRequested;
        _sourceRequested = Entity.Null;
        if (source == Entity.Null)
        {
            return;
        }

        bool changed;
        Entity output;
        lock (Sync)
        {
            changed = State.RecordStartSource(ToKey(source));
            output = FromKey(State.Selected);
        }

        if (changed)
        {
            UpgradeLog.Info(
                $"Start source observed without changing output: " +
                $"start={DescribePrefab(tool, source)} ({DescribeEntity(source)}), " +
                $"output={DescribePrefab(tool, output)} ({DescribeEntity(output)}).");
        }
    }

    internal static void FilterSelectedPrefab(object tool, ref Entity requested)
    {
        if (_sourceSelectionDepth > 0 && requested != Entity.Null)
        {
            _sourceRequested = requested;
        }

        if (_explicitSelectionDepth > 0 || _restoreDepth > 0)
        {
            return;
        }

        Entity desired;
        lock (Sync)
        {
            desired = FromKey(State.Selected);
        }

        if (desired != Entity.Null)
        {
            requested = desired;
        }
        else if (_sourceSelectionDepth > 0)
        {
            requested = Entity.Null;
        }
    }

    internal static void BeginModeFromVanilla(object tool)
    {
        BeginNewRoute(tool);

        if (!TryGetVanillaSelection(out Entity selected))
        {
            bool hasRemembered;
            lock (Sync)
            {
                hasRemembered = State.Selected.IsValid;
            }

            if (!hasRemembered)
            {
                ClearCurrentSelection(tool);
                ShowMessage("请先在游戏原生道路面板选择道路，再使用立交道路生成器。");
                UpgradeLog.Warn("No road is selected in the vanilla road panel.");
            }

            return;
        }

        bool changed;
        lock (Sync)
        {
            changed = State.RememberPanelSelection(ToKey(selected));
        }

        RestoreSelectedPrefab(tool, selected);
        string name = DescribePrefab(tool, selected);
        UpgradeLog.Info(
            $"Vanilla panel road synchronized for mode start: {name} ({DescribeEntity(selected)})." +
            (changed ? string.Empty : " Selection unchanged."));
    }

    internal static void BeginNewRoute(object tool)
    {
        lock (Sync)
        {
            State.BeginRoute();
        }

        ConnectionSelectionStore.ClearPending();
        ApplyRememberedSelection(tool);
    }

    internal static void ResetForWorld()
    {
        lock (Sync)
        {
            State.Reset();
        }

        ConnectionSelectionStore.ClearPending();
    }

    internal static bool TryKeepInterchangeBuilderActive(
        ToolSystem toolSystem,
        PrefabBase prefab)
    {
        ToolBaseSystem? activeTool = toolSystem.activeTool;
        if (activeTool == null || activeTool.GetType() != ToolType || !(prefab is NetPrefab))
        {
            return false;
        }

        NetToolSystem? nativeTool = activeTool.World.GetExistingSystemManaged<NetToolSystem>();
        nativeTool?.TrySetPrefab(prefab);

        if (!activeTool.TrySetPrefab(prefab))
        {
            return false;
        }

        UpgradeLog.Info("Vanilla toolbar road selection was handed to the active InterchangeBuilder tool.");
        return true;
    }

    internal static bool AllowBuild(object tool)
    {
        string activeMode = ActiveModeField?.GetValue(tool) as string ?? string.Empty;
        if (string.Equals(activeMode, "grade", StringComparison.Ordinal) ||
            string.Equals(activeMode, "nodeEdit", StringComparison.Ordinal))
        {
            return true;
        }

        Entity output;
        Entity source;
        lock (Sync)
        {
            output = FromKey(State.Selected);
            source = FromKey(State.StartSource);
        }

        if (output == Entity.Null || !EntityExists(output))
        {
            ShowMessage("没有可用的道路。请先在游戏原生道路面板选择道路。");
            UpgradeLog.Warn("Road placement blocked because the vanilla road panel has no valid selection.");
            return false;
        }

        RestoreSelectedPrefab(tool, output);
        UpgradeLog.Info(
            "Road selection build snapshot: " +
            $"mode=vanilla-panel, output={DescribePrefab(tool, output)} ({DescribeEntity(output)}), " +
            $"start={DescribePrefab(tool, source)} ({DescribeEntity(source)}).");
        return true;
    }

    internal static void Dispose()
    {
        ResetForWorld();
        _explicitSelectionDepth = 0;
        _sourceSelectionDepth = 0;
        _restoreDepth = 0;
        _selectionOrigin = null;
    }

    private static void ApplyRememberedSelection(object tool)
    {
        Entity desired;
        lock (Sync)
        {
            desired = FromKey(State.Selected);
        }

        if (desired != Entity.Null && ReadSelectedPrefab(tool) != desired)
        {
            RestoreSelectedPrefab(tool, desired);
        }
    }

    private static bool TryGetVanillaSelection(out Entity selected)
    {
        selected = Entity.Null;
        try
        {
            World? world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return false;
            }

            NetToolSystem? nativeTool = world.GetExistingSystemManaged<NetToolSystem>();
            PrefabSystem? prefabSystem = world.GetExistingSystemManaged<PrefabSystem>();
            if (nativeTool == null || prefabSystem == null)
            {
                return false;
            }

            NetPrefab? prefab = NativeSelectedPrefabField?.GetValue(nativeTool) as NetPrefab;
            if (prefab == null)
            {
                prefab = NativeActivePrefabField?.GetValue(nativeTool) as NetPrefab;
            }

            if (prefab == null)
            {
                prefab = nativeTool.GetPrefab() as NetPrefab;
            }

            return prefab != null &&
                prefabSystem.TryGetEntity(prefab, out selected) &&
                selected != Entity.Null &&
                world.EntityManager.Exists(selected);
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Vanilla road selection could not be read: " +
                $"{exception.GetType().Name}: {exception.Message}");
            selected = Entity.Null;
            return false;
        }
    }

    private static void RestoreSelectedPrefab(object tool, Entity prefab)
    {
        if (prefab == Entity.Null || SetSelectedRoadPrefabMethod == null)
        {
            return;
        }

        try
        {
            _restoreDepth++;
            SetSelectedRoadPrefabMethod.Invoke(tool, new object[] { prefab });
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Road selection restore failed: {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            _restoreDepth--;
        }
    }

    private static void ClearCurrentSelection(object tool)
    {
        try
        {
            SelectedRoadPrefabField?.SetValue(tool, Entity.Null);
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Empty road selection could not be applied: " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void InvalidatePreview(object tool)
    {
        try
        {
            ConnectionSelectionStore.ClearPending();
            ClearPreviewMethod?.Invoke(tool, null);
            RestartActiveModeMethod?.Invoke(tool, null);
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Road-change preview reset failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static Entity ReadSelectedPrefab(object tool) =>
        SelectedRoadPrefabField?.GetValue(tool) is Entity entity ? entity : Entity.Null;

    private static string DescribePrefab(object? tool, Entity prefab)
    {
        if (prefab == Entity.Null)
        {
            return "未选择";
        }

        try
        {
            if (tool != null && RoadChoicesField?.GetValue(tool) is IEnumerable choices)
            {
                foreach (object? choice in choices)
                {
                    if (choice == null)
                    {
                        continue;
                    }

                    Type choiceType = choice.GetType();
                    PropertyInfo? prefabProperty = choiceType.GetProperty(
                        "Prefab",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    PropertyInfo? nameProperty = choiceType.GetProperty(
                        "Name",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prefabProperty?.GetValue(choice) is Entity candidate &&
                        candidate == prefab &&
                        nameProperty?.GetValue(choice) is string name &&
                        !string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }
            }
        }
        catch
        {
            // Entity identity remains sufficient for diagnostics if the
            // private road catalog changes between game versions.
        }

        return $"网络 #{prefab.Index}";
    }

    private static bool EntityExists(Entity entity)
    {
        try
        {
            World? world = World.DefaultGameObjectInjectionWorld;
            return world != null && world.EntityManager.Exists(entity);
        }
        catch
        {
            return false;
        }
    }

    private static void ShowMessage(string message)
    {
        try
        {
            object? instance = UiInstanceProperty?.GetValue(null);
            if (instance != null)
            {
                SetValidationMessageMethod?.Invoke(instance, new object[] { message });
            }
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn(
                $"Road selection UI message failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static RoadKey ToKey(Entity entity) =>
        entity == Entity.Null ? RoadKey.None : new RoadKey(entity.Index, entity.Version);

    private static Entity FromKey(RoadKey key) => key.IsValid
        ? new Entity { Index = key.Index, Version = key.Version }
        : Entity.Null;

    private static string DescribeEntity(Entity entity) =>
        entity == Entity.Null ? "none" : $"{entity.Index}:{entity.Version}";
}
