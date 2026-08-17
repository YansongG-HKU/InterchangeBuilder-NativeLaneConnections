using System;
using System.Linq;
using System.Reflection;
using Game;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;

namespace InterchangeBuilder.LaneConnections;

public static class Bootstrap
{
    private const string HarmonyId = "com.codex.interchangebuilder.native-lane-connections";
    private static readonly object Sync = new object();
    private static Harmony? _harmony;

    public static string ValidateTargets()
    {
        Type toolType = RequireType("InterchangeBuilder.Systems.InterchangeBuilderToolSystem");
        _ = toolType.GetField("_selectedRoadPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(toolType.FullName, "_selectedRoadPrefab");
        _ = toolType.GetField("_activeMode", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(toolType.FullName, "_activeMode");
        _ = toolType.GetField("_roadChoices", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(toolType.FullName, "_roadChoices");
        _ = toolType.GetMethod(
            "OnUpdate",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(toolType.FullName, "OnUpdate");
        foreach (string methodName in new[]
        {
            "SelectRoad",
            "CycleRoad",
            "SetSelectedRoadPrefab",
            "CompleteEndpointSelection",
            "HandleFreeDrawInteraction",
            "PrepareEndpointSelectionMode",
            "RestartActiveMode",
            "ConfirmCurrentPreview",
            "EnsureRoundaboutRoadPrefab",
            "OnGamePreload"
        })
        {
            _ = toolType.GetMethod(
                methodName,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(toolType.FullName, methodName);
        }
        _ = toolType.GetMethod(
            "TrySetPrefab",
            BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException(toolType.FullName, "TrySetPrefab");
        RequireParameters(toolType, "SelectRoad", typeof(int));
        RequireParameters(toolType, "CycleRoad", typeof(int));
        RequireParameters(toolType, "SetSelectedRoadPrefab", typeof(Unity.Entities.Entity));
        RequireParameterCount(toolType, "CompleteEndpointSelection", 2);
        RequireParameterCount(toolType, "HandleFreeDrawInteraction", 3);

        Type nativeNetToolType = typeof(NetToolSystem);
        FieldInfo nativeSelectedPrefab = nativeNetToolType.GetField(
            "m_SelectedPrefab",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nativeNetToolType.FullName, "m_SelectedPrefab");
        FieldInfo nativeActivePrefab = nativeNetToolType.GetField(
            "m_Prefab",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nativeNetToolType.FullName, "m_Prefab");
        if (nativeSelectedPrefab.FieldType != typeof(NetPrefab) ||
            nativeActivePrefab.FieldType != typeof(NetPrefab))
        {
            throw new InvalidOperationException("NetToolSystem road-prefab field types changed.");
        }

        _ = nativeNetToolType.GetProperty(
            "prefab",
            BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(nativeNetToolType.FullName, "prefab");
        MethodInfo activatePrefabTool = typeof(ToolSystem).GetMethod(
            "ActivatePrefabTool",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(PrefabBase) },
            null)
            ?? throw new MissingMethodException(typeof(ToolSystem).FullName, "ActivatePrefabTool");
        if (activatePrefabTool.ReturnType != typeof(bool))
        {
            throw new InvalidOperationException("ActivatePrefabTool return type changed.");
        }

        Type uiType = RequireType("InterchangeBuilder.Systems.InterchangeBuilderUISystem");
        _ = uiType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(uiType.FullName, "Instance");
        _ = uiType.GetMethod(
            "SetValidationMessage",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(uiType.FullName, "SetValidationMessage");
        _ = uiType.GetMethod(
            "OnCreate",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(uiType.FullName, "OnCreate");
        _ = uiType.GetMethod(
            "OnDestroy",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(uiType.FullName, "OnDestroy");

        Type selectionType = RequireType("InterchangeBuilder.Selection.GameNetworkNodeSelectionService");
        MethodInfo selectionMethod = selectionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "TrySelectNode" &&
                method.GetParameters().Length == 2 &&
                method.GetParameters()[0].ParameterType == typeof(bool));
        if (!selectionMethod.GetParameters()[1].IsOut ||
            selectionMethod.GetParameters()[1].ParameterType.GetElementType() != typeof(InterchangeBuilder.Selection.SelectedNode))
        {
            throw new InvalidOperationException("TrySelectNode no longer exposes the expected SelectedNode out parameter.");
        }

        MethodInfo connectedEdgeMethod = selectionType.GetMethod(
            "TryChooseConnectedEdge",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(selectionType.FullName, "TryChooseConnectedEdge");
        if (connectedEdgeMethod.GetParameters().Length != 4)
        {
            throw new InvalidOperationException("TryChooseConnectedEdge signature changed.");
        }
        MethodInfo resolveNodeMethod = selectionType.GetMethod(
            "TryResolveNodeAndEdge",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(selectionType.FullName, "TryResolveNodeAndEdge");
        if (resolveNodeMethod.GetParameters().Length != 5)
        {
            throw new InvalidOperationException("TryResolveNodeAndEdge signature changed.");
        }

        Type snapshotType = RequireType("InterchangeBuilder.Tools.RoadPlacementSnapshot");
        ConstructorInfo snapshotConstructor = snapshotType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        ParameterInfo[] snapshotParameters = snapshotConstructor.GetParameters();
        if (snapshotParameters.Length < 14 ||
            snapshotParameters[1].ParameterType != typeof(Unity.Entities.Entity) ||
            snapshotParameters[2].ParameterType != typeof(Unity.Entities.Entity) ||
            snapshotParameters[4].ParameterType != typeof(System.Numerics.Vector3) ||
            snapshotParameters[5].ParameterType != typeof(System.Numerics.Vector3) ||
            snapshotParameters[13].ParameterType != typeof(Colossal.Mathematics.Bezier4x3[]))
        {
            throw new InvalidOperationException("RoadPlacementSnapshot constructor layout changed.");
        }

        Type applyType = RequireType("InterchangeBuilder.Tools.CurveRoadApplyService");
        MethodInfo definitionMethod = applyType.GetMethod(
            "CreateDefinitionEntities",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(applyType.FullName, "CreateDefinitionEntities");
        if (definitionMethod.GetParameters().Length != 1)
        {
            throw new InvalidOperationException("CreateDefinitionEntities signature changed.");
        }

        MethodInfo coursePositionMethod = applyType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "CreateCoursePosition" && method.GetParameters().Length == 7);
        if (coursePositionMethod.ReturnType != typeof(Game.Tools.CoursePos))
        {
            throw new InvalidOperationException("CreateCoursePosition return type changed.");
        }

        MethodInfo chainMethod = applyType.GetMethod(
            "TryResolveSubdividedChain",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(applyType.FullName, "TryResolveSubdividedChain");
        ParameterInfo[] chainParameters = chainMethod.GetParameters();
        if (chainMethod.ReturnType != typeof(bool) ||
            chainParameters.Length != 4 ||
            chainParameters[0].ParameterType != snapshotType ||
            chainParameters[1].ParameterType != typeof(System.Collections.Generic.IReadOnlyList<Unity.Entities.Entity>) ||
            !chainParameters[2].IsOut ||
            chainParameters[2].ParameterType.GetElementType() != typeof(System.Collections.Generic.List<Unity.Entities.Entity>) ||
            !chainParameters[3].IsOut ||
            chainParameters[3].ParameterType.GetElementType() != typeof(string))
        {
            throw new InvalidOperationException("TryResolveSubdividedChain signature changed.");
        }

        MethodInfo failureMethod = applyType.GetMethod(
            "BeginFailure",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(applyType.FullName, "BeginFailure");
        ParameterInfo[] failureParameters = failureMethod.GetParameters();
        if (failureParameters.Length != 3 ||
            failureParameters[0].ParameterType != typeof(string) ||
            failureParameters[1].ParameterType != typeof(int) ||
            failureParameters[2].ParameterType != typeof(System.Collections.Generic.IReadOnlyList<Unity.Entities.Entity>))
        {
            throw new InvalidOperationException("BeginFailure signature changed.");
        }

        return "vanilla-panel road memory, all-lane endpoint ports, and placement-recovery patch targets resolved";
    }

    public static void Install(UpdateSystem updateSystem)
    {
        lock (Sync)
        {
            if (_harmony != null)
            {
                return;
            }

            Harmony? installingHarmony = null;
            try
            {
                var harmony = new Harmony(HarmonyId);
                installingHarmony = harmony;
                PatchToolContext(harmony);
                PatchUpgradeUiBindings(harmony);
                PatchRoadSelection(harmony);
                PatchNodeSelection(harmony);
                PatchConnectedEdgeSelection(harmony);
                PatchEndpointSelectionRadius(harmony);
                PatchSnapshotConstructor(harmony);
                PatchDefinitionContext(harmony);
                PatchCoursePosition(harmony);
                PatchPlacementRecovery(harmony);
                _harmony = harmony;
                UpgradeLog.Info("Native CS2 all-lane endpoint ports, placement recovery, vanilla-panel road memory, and Simplified Chinese upgrade installed.");
            }
            catch (Exception exception)
            {
                installingHarmony?.UnpatchAll(HarmonyId);
                UpgradeLog.Error($"Upgrade installation failed; base mod remains available. {exception}");
            }
        }
    }

    public static void Uninstall()
    {
        lock (Sync)
        {
            if (_harmony == null)
            {
                return;
            }

            _harmony.UnpatchAll(HarmonyId);
            _harmony = null;
            RoadSelectionController.Dispose();
            UpgradeLog.Info("Native CS2 endpoint and lane-alignment upgrade removed.");
        }
    }

    private static void PatchToolContext(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Systems.InterchangeBuilderToolSystem");
        MethodInfo original = type.GetMethod(
            "OnUpdate",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            ?? throw new MissingMethodException(type.FullName, "OnUpdate");
        harmony.Patch(original, prefix: HarmonyMethod(typeof(ToolContextPatch), nameof(ToolContextPatch.Prefix)));
    }

    private static void PatchRoadSelection(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Systems.InterchangeBuilderToolSystem");

        PatchWithState(
            harmony,
            type.GetMethod("SelectRoad", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(type.FullName, "SelectRoad"),
            typeof(PanelRoadSelectionPatch));
        PatchWithState(
            harmony,
            type.GetMethod("CycleRoad", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(type.FullName, "CycleRoad"),
            typeof(CycleRoadSelectionPatch));
        PatchWithState(
            harmony,
            type.GetMethod("TrySetPrefab", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new MissingMethodException(type.FullName, "TrySetPrefab"),
            typeof(ExternalRoadSelectionPatch));

        MethodInfo setSelected = type.GetMethod(
            "SetSelectedRoadPrefab",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "SetSelectedRoadPrefab");
        harmony.Patch(
            setSelected,
            prefix: HarmonyMethod(typeof(SelectedRoadPrefabPatch), nameof(SelectedRoadPrefabPatch.Prefix)));

        PatchWithState(
            harmony,
            type.GetMethod("CompleteEndpointSelection", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(type.FullName, "CompleteEndpointSelection"),
            typeof(EndpointRoadSourcePatch));
        PatchWithState(
            harmony,
            type.GetMethod("HandleFreeDrawInteraction", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(type.FullName, "HandleFreeDrawInteraction"),
            typeof(FreeDrawRoadSourcePatch));

        MethodInfo prepare = type.GetMethod(
            "PrepareEndpointSelectionMode",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "PrepareEndpointSelectionMode");
        harmony.Patch(
            prepare,
            postfix: HarmonyMethod(typeof(ModePreparationPatch), nameof(ModePreparationPatch.Postfix)));
        MethodInfo restart = type.GetMethod(
            "RestartActiveMode",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "RestartActiveMode");
        harmony.Patch(
            restart,
            postfix: HarmonyMethod(typeof(RouteRestartPatch), nameof(RouteRestartPatch.Postfix)));

        MethodInfo confirm = type.GetMethod(
            "ConfirmCurrentPreview",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "ConfirmCurrentPreview");
        harmony.Patch(
            confirm,
            prefix: HarmonyMethod(typeof(RoadBuildSelectionPatch), nameof(RoadBuildSelectionPatch.Prefix)));

        MethodInfo preload = type.GetMethod(
            "OnGamePreload",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            ?? throw new MissingMethodException(type.FullName, "OnGamePreload");
        harmony.Patch(
            preload,
            postfix: HarmonyMethod(typeof(RoadSelectionWorldPatch), nameof(RoadSelectionWorldPatch.Postfix)));

        MethodInfo activatePrefabTool = typeof(ToolSystem).GetMethod(
            "ActivatePrefabTool",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(PrefabBase) },
            null)
            ?? throw new MissingMethodException(typeof(ToolSystem).FullName, "ActivatePrefabTool");
        harmony.Patch(
            activatePrefabTool,
            prefix: HarmonyMethod(typeof(VanillaPrefabActivationPatch), nameof(VanillaPrefabActivationPatch.Prefix)));
    }

    private static void PatchUpgradeUiBindings(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Systems.InterchangeBuilderUISystem");
        MethodInfo onCreate = type.GetMethod(
            "OnCreate",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "OnCreate");
        MethodInfo onDestroy = type.GetMethod(
            "OnDestroy",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "OnDestroy");
        harmony.Patch(
            onCreate,
            postfix: HarmonyMethod(typeof(UpgradeUiCreatePatch), nameof(UpgradeUiCreatePatch.Postfix)));
        harmony.Patch(
            onDestroy,
            prefix: HarmonyMethod(typeof(UpgradeUiDestroyPatch), nameof(UpgradeUiDestroyPatch.Prefix)));
    }

    private static void PatchWithState(Harmony harmony, MethodInfo original, Type patchType)
    {
        harmony.Patch(
            original,
            prefix: HarmonyMethod(patchType, "Prefix"),
            postfix: HarmonyMethod(patchType, "Postfix"),
            finalizer: HarmonyMethod(patchType, "Finalizer"));
    }

    private static void PatchNodeSelection(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Selection.GameNetworkNodeSelectionService");
        MethodInfo original = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "TrySelectNode" &&
                method.GetParameters().Length == 2 &&
                method.GetParameters()[0].ParameterType == typeof(bool));
        harmony.Patch(original, postfix: HarmonyMethod(typeof(NodeSelectionPatch), nameof(NodeSelectionPatch.Postfix)));
    }

    private static void PatchSnapshotConstructor(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Tools.RoadPlacementSnapshot");
        ConstructorInfo constructor = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        harmony.Patch(constructor, postfix: HarmonyMethod(typeof(SnapshotConstructorPatch), nameof(SnapshotConstructorPatch.Postfix)));
    }

    private static void PatchConnectedEdgeSelection(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Selection.GameNetworkNodeSelectionService");
        MethodInfo original = type.GetMethod(
            "TryChooseConnectedEdge",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "TryChooseConnectedEdge");
        harmony.Patch(
            original,
            prefix: HarmonyMethod(typeof(ConnectedEdgeSelectionPatch), nameof(ConnectedEdgeSelectionPatch.Prefix)));
    }

    private static void PatchEndpointSelectionRadius(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Selection.GameNetworkNodeSelectionService");
        MethodInfo original = type.GetMethod(
            "TryResolveNodeAndEdge",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "TryResolveNodeAndEdge");
        harmony.Patch(
            original,
            prefix: HarmonyMethod(typeof(EndpointSelectionRadiusPatch), nameof(EndpointSelectionRadiusPatch.Prefix)));
    }

    private static void PatchDefinitionContext(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Tools.CurveRoadApplyService");
        MethodInfo original = type.GetMethod(
            "CreateDefinitionEntities",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "CreateDefinitionEntities");
        harmony.Patch(
            original,
            prefix: HarmonyMethod(typeof(CourseCreationContextPatch), nameof(CourseCreationContextPatch.Prefix)),
            postfix: HarmonyMethod(typeof(CourseCreationContextPatch), nameof(CourseCreationContextPatch.Postfix)),
            finalizer: HarmonyMethod(typeof(CourseCreationContextPatch), nameof(CourseCreationContextPatch.Finalizer)));
    }

    private static void PatchCoursePosition(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Tools.CurveRoadApplyService");
        MethodInfo original = type
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "CreateCoursePosition" && method.GetParameters().Length == 7);
        harmony.Patch(original, postfix: HarmonyMethod(typeof(CoursePositionPatch), nameof(CoursePositionPatch.Postfix)));
    }

    private static void PatchPlacementRecovery(Harmony harmony)
    {
        Type type = RequireType("InterchangeBuilder.Tools.CurveRoadApplyService");
        MethodInfo chainResolver = type.GetMethod(
            "TryResolveSubdividedChain",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "TryResolveSubdividedChain");
        harmony.Patch(
            chainResolver,
            postfix: HarmonyMethod(typeof(SubdividedChainRecoveryPatch), nameof(SubdividedChainRecoveryPatch.Postfix)));

        MethodInfo beginFailure = type.GetMethod(
            "BeginFailure",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, "BeginFailure");
        harmony.Patch(
            beginFailure,
            prefix: HarmonyMethod(typeof(PlacementFailureSafetyPatch), nameof(PlacementFailureSafetyPatch.Prefix)));
    }

    private static Type RequireType(string name) =>
        AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);

    private static void RequireParameters(Type type, string methodName, params Type[] parameterTypes)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != parameterTypes.Length ||
            parameters.Where((parameter, index) => parameter.ParameterType != parameterTypes[index]).Any())
        {
            throw new InvalidOperationException(methodName + " signature changed.");
        }
    }

    private static void RequireParameterCount(Type type, string methodName, int count)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        if (method.GetParameters().Length != count)
        {
            throw new InvalidOperationException(methodName + " signature changed.");
        }
    }

    private static HarmonyMethod HarmonyMethod(Type type, string methodName) =>
        new HarmonyMethod(type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName));
}
