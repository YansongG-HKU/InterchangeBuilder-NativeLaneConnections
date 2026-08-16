using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal readonly struct RoadSelectionUiSnapshot
{
    internal RoadSelectionUiSnapshot(
        int revision,
        string mode,
        string sourceName,
        string status,
        bool canConfirm)
    {
        Revision = revision;
        Mode = mode;
        SourceName = sourceName;
        Status = status;
        CanConfirm = canConfirm;
    }

    internal int Revision { get; }

    internal string Mode { get; }

    internal string SourceName { get; }

    internal string Status { get; }

    internal bool CanConfirm { get; }
}

internal static class RoadSelectionController
{
    private const string WaitingForStart = "尚未选择起点";

    private static readonly object Sync = new object();
    private static readonly RoadSelectionStateMachine State = new RoadSelectionStateMachine();
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
    private static readonly MethodInfo? EnsureDefaultRoadMethod = AccessTools.Method(
        ToolType,
        "EnsureRoundaboutRoadPrefab");
    private static readonly Type? UiType = AccessTools.TypeByName(
        "InterchangeBuilder.Systems.InterchangeBuilderUISystem");
    private static readonly PropertyInfo? UiInstanceProperty = AccessTools.Property(UiType, "Instance");
    private static readonly MethodInfo? SetValidationMessageMethod = AccessTools.Method(
        UiType,
        "SetValidationMessage");

    private static object? _tool;
    private static string _sourceName = WaitingForStart;
    private static int _uiRevision;

    [ThreadStatic]
    private static int _userSelectionDepth;

    [ThreadStatic]
    private static int _requestedPrefabIndex;

    [ThreadStatic]
    private static Entity _selectionBefore;

    [ThreadStatic]
    private static int _sourceSelectionDepth;

    [ThreadStatic]
    private static Entity _sourceRequested;

    [ThreadStatic]
    private static int _restoreDepth;

    internal static RoadSelectionMode Mode
    {
        get
        {
            lock (Sync)
            {
                return State.Mode;
            }
        }
    }

    internal static void AttachAndEnforce(object tool)
    {
        bool attached;
        lock (Sync)
        {
            attached = !ReferenceEquals(_tool, tool);
            if (attached)
            {
                _tool = tool;
                State.Reset();
                _sourceName = WaitingForStart;
                _uiRevision++;
            }
        }

        if (!attached)
        {
            RestoreManualSelection(tool);
        }
    }

    internal static Entity GetAlignmentPrefab(Entity currentPrefab)
    {
        lock (Sync)
        {
            if (State.Mode == RoadSelectionMode.FollowStart && !State.StartSource.IsValid)
            {
                return Entity.Null;
            }

            return FromKey(State.ResolveOutput(ToKey(currentPrefab)));
        }
    }

    internal static Entity ResolveNodeAlignmentPrefab(Entity currentPrefab, Entity nodePrefab)
    {
        lock (Sync)
        {
            if (State.Mode == RoadSelectionMode.FollowStart &&
                !State.StartSource.IsValid &&
                nodePrefab != Entity.Null)
            {
                return nodePrefab;
            }

            return FromKey(State.ResolveOutput(ToKey(currentPrefab)));
        }
    }

    internal static bool BeginUserSelection(object tool, int requestedPrefabIndex)
    {
        AttachAndEnforce(tool);
        _userSelectionDepth++;
        if (_userSelectionDepth != 1)
        {
            return false;
        }

        _requestedPrefabIndex = requestedPrefabIndex;
        _selectionBefore = ReadSelectedPrefab(tool);
        return true;
    }

    internal static void EndUserSelection(object tool, bool outermost)
    {
        if (_userSelectionDepth > 0)
        {
            _userSelectionDepth--;
        }

        if (!outermost)
        {
            return;
        }

        Entity selected = ReadSelectedPrefab(tool);
        bool requestApplied = selected != Entity.Null &&
            (_requestedPrefabIndex < 0
                ? selected != _selectionBefore
                : selected.Index == _requestedPrefabIndex);
        _requestedPrefabIndex = -1;

        if (!requestApplied)
        {
            return;
        }

        bool changed;
        lock (Sync)
        {
            changed = State.SelectCandidate(ToKey(selected));
            if (changed)
            {
                _uiRevision++;
            }
        }

        if (!changed)
        {
            return;
        }

        string name = DescribePrefab(tool, selected);
        UpgradeLog.Info($"Road selected as pending output: {name} ({DescribeEntity(selected)}). Confirmation required.");
        if (selected != _selectionBefore)
        {
            InvalidatePreview(tool);
        }

        ShowMessage("已选择候选输出道路，请点击“确认并锁定”；如已开始预览，请重新选择起点和终点。");
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

        string name = DescribePrefab(tool, source);
        RoadSelectionMode mode;
        bool changed;
        lock (Sync)
        {
            changed = State.RecordStart(ToKey(source));
            mode = State.Mode;
            _sourceName = name;
            if (changed)
            {
                _uiRevision++;
            }
        }

        if (changed)
        {
            UpgradeLog.Info(
                mode == RoadSelectionMode.FollowStart
                    ? $"Start road resolved as output: {name} ({DescribeEntity(source)})."
                    : $"Start source recorded without replacing the manual output: {name} ({DescribeEntity(source)}).");
        }
    }

    internal static void FilterSelectedPrefab(object tool, ref Entity requested)
    {
        if (_sourceSelectionDepth > 0 && requested != Entity.Null)
        {
            _sourceRequested = requested;
        }

        if (_userSelectionDepth > 0 || _restoreDepth > 0)
        {
            return;
        }

        Entity desired;
        lock (Sync)
        {
            if (State.Mode == RoadSelectionMode.FollowStart)
            {
                return;
            }

            desired = FromKey(State.ResolveOutput(ToKey(requested)));
        }

        if (desired != Entity.Null)
        {
            requested = desired;
        }
    }

    internal static void BeginNewRoute(object tool)
    {
        bool changed;
        lock (Sync)
        {
            changed = State.BeginRoute();
            _sourceName = WaitingForStart;
            if (changed)
            {
                _uiRevision++;
            }
        }

        ConnectionSelectionStore.ClearPending();
        RestoreManualSelection(tool);
    }

    internal static void ResetForWorld(object? tool)
    {
        lock (Sync)
        {
            _tool = tool;
            State.Reset();
            _sourceName = WaitingForStart;
            _uiRevision++;
        }

        ConnectionSelectionStore.ClearPending();
    }

    internal static bool ConfirmSelection()
    {
        object? tool;
        Entity locked;
        lock (Sync)
        {
            tool = _tool;
            if (tool == null || !State.Confirm())
            {
                return false;
            }

            locked = FromKey(State.Locked);
            _uiRevision++;
        }

        RestoreSelectedPrefab(tool, locked);
        string name = DescribePrefab(tool, locked);
        UpgradeLog.Info($"Road output locked: {name} ({DescribeEntity(locked)}).");
        ShowMessage($"输出道路已锁定为“{name}”；选择起点只会确定连接分支，不会再替换道路。");
        return true;
    }

    internal static bool FollowStart()
    {
        object? tool;
        Entity source;
        bool changed;
        lock (Sync)
        {
            tool = _tool;
            source = FromKey(State.StartSource);
            changed = State.FollowStart();
            if (changed)
            {
                _uiRevision++;
            }
        }

        if (!changed || tool == null)
        {
            return false;
        }

        if (source != Entity.Null)
        {
            RestoreSelectedPrefab(tool, source);
        }

        UpgradeLog.Info("Road output changed to follow-start mode.");
        InvalidatePreview(tool);
        ShowMessage("已改为跟随起点道路。请重新选择起点；自由起点将使用当前显示的默认道路。");
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

        RoadSelectionMode mode;
        Entity output;
        Entity source;
        lock (Sync)
        {
            mode = State.Mode;
            output = FromKey(State.ResolveOutput(ToKey(ReadSelectedPrefab(tool))));
            source = FromKey(State.StartSource);
        }

        if (mode == RoadSelectionMode.FollowStart && output == Entity.Null)
        {
            try
            {
                EnsureDefaultRoadMethod?.Invoke(tool, null);
                output = ReadSelectedPrefab(tool);
            }
            catch (Exception exception)
            {
                UpgradeLog.Warn(
                    $"Default road resolution failed: {exception.GetType().Name}: {exception.Message}");
            }
        }

        if (mode == RoadSelectionMode.PendingConfirmation)
        {
            ShowMessage("当前道路仍处于待确认状态。请先点击“确认并锁定”，再执行建造。");
            UpgradeLog.Warn("Road placement blocked because the selected output road was not confirmed.");
            return false;
        }

        if (mode == RoadSelectionMode.Locked)
        {
            if (output == Entity.Null || !EntityExists(output))
            {
                ShowMessage("锁定的输出道路已经失效，请重新选择并确认道路。");
                UpgradeLog.Warn("Road placement blocked because the locked output prefab is no longer valid.");
                return false;
            }

            RestoreSelectedPrefab(tool, output);
        }

        UpgradeLog.Info(
            "Road selection build snapshot: " +
            $"mode={ModeKey(mode)}, output={DescribePrefab(tool, output)} ({DescribeEntity(output)}), " +
            $"start={DescribePrefab(tool, source)} ({DescribeEntity(source)}).");
        return true;
    }

    internal static RoadSelectionUiSnapshot GetUiSnapshot()
    {
        lock (Sync)
        {
            string status;
            switch (State.Mode)
            {
                case RoadSelectionMode.PendingConfirmation:
                    status = "候选道路尚未确认。确认前不会允许最终建造。";
                    break;
                case RoadSelectionMode.Locked:
                    status = "输出道路已锁定；起点只决定连接道路分支和方向。";
                    break;
                default:
                    status = State.StartSource.IsValid
                        ? "正在跟随起点道路；最终输出与起点来源一致。"
                        : "等待起点道路；自由起点将使用上方当前显示的默认道路。";
                    break;
            }

            return new RoadSelectionUiSnapshot(
                _uiRevision,
                ModeKey(State.Mode),
                State.StartSource.IsValid ? _sourceName : WaitingForStart,
                status,
                State.CanConfirm);
        }
    }

    internal static void Dispose()
    {
        ResetForWorld(null);
        _userSelectionDepth = 0;
        _sourceSelectionDepth = 0;
        _restoreDepth = 0;
    }

    private static void RestoreManualSelection(object tool)
    {
        Entity desired;
        lock (Sync)
        {
            if (State.Mode == RoadSelectionMode.FollowStart)
            {
                return;
            }

            desired = FromKey(State.ResolveOutput(ToKey(ReadSelectedPrefab(tool))));
        }

        if (desired != Entity.Null && ReadSelectedPrefab(tool) != desired)
        {
            RestoreSelectedPrefab(tool, desired);
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
            return "未解析";
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
            // Entity identity is still sufficient for diagnostics if the
            // private catalog layout changes between game versions.
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

    private static string ModeKey(RoadSelectionMode mode)
    {
        switch (mode)
        {
            case RoadSelectionMode.PendingConfirmation:
                return "pending";
            case RoadSelectionMode.Locked:
                return "locked";
            default:
                return "auto";
        }
    }

    private static string DescribeEntity(Entity entity) =>
        entity == Entity.Null ? "none" : $"{entity.Index}:{entity.Version}";
}
