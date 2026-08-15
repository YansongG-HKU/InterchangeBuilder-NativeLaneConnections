using System;
using System.Linq;
using System.Reflection;
using Game;
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
        _ = toolType.GetMethod(
            "OnUpdate",
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(toolType.FullName, "OnUpdate");

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
        if (snapshotParameters.Length < 6 ||
            snapshotParameters[1].ParameterType != typeof(Unity.Entities.Entity) ||
            snapshotParameters[2].ParameterType != typeof(Unity.Entities.Entity) ||
            snapshotParameters[4].ParameterType != typeof(System.Numerics.Vector3) ||
            snapshotParameters[5].ParameterType != typeof(System.Numerics.Vector3))
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

        return "7 patch targets and their required fields/signatures resolved";
    }

    public static void Install(UpdateSystem updateSystem)
    {
        lock (Sync)
        {
            if (_harmony != null)
            {
                return;
            }

            try
            {
                var harmony = new Harmony(HarmonyId);
                PatchToolContext(harmony);
                PatchNodeSelection(harmony);
                PatchConnectedEdgeSelection(harmony);
                PatchEndpointSelectionRadius(harmony);
                PatchSnapshotConstructor(harmony);
                PatchDefinitionContext(harmony);
                PatchCoursePosition(harmony);
                _harmony = harmony;
                UpgradeLog.Info("Native CS2 endpoint alignment and Simplified Chinese upgrade 2.2.0 installed.");
            }
            catch (Exception exception)
            {
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

    private static Type RequireType(string name) =>
        AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);

    private static HarmonyMethod HarmonyMethod(Type type, string methodName) =>
        new HarmonyMethod(type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName));
}
