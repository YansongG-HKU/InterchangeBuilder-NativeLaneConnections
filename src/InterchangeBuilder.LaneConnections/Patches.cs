using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Game.Net;
using Game.Tools;
using HarmonyLib;
using InterchangeBuilder.Geometry;
using InterchangeBuilder.Selection;
using Unity.Entities;
using Unity.Mathematics;

namespace InterchangeBuilder.LaneConnections;

internal static class ToolContextPatch
{
    private static readonly FieldInfo? SelectedRoadPrefabField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Systems.InterchangeBuilderToolSystem"),
        "_selectedRoadPrefab");

    private static readonly PropertyInfo? ActiveStateProperty = AccessTools.Property(
        AccessTools.TypeByName("InterchangeBuilder.Systems.InterchangeBuilderToolSystem"),
        "ActiveState");

    internal static Entity SelectedRoadPrefab { get; private set; }

    internal static Entity AlignmentRoadPrefab { get; private set; }

    internal static EndpointRole CurrentEndpointRole { get; private set; }

    public static void Prefix(object __instance)
    {
        UpgradeUiBindings.RefreshRuntimeStatus();
        string state = ActiveStateProperty?.GetValue(__instance)?.ToString() ?? string.Empty;
        CurrentEndpointRole = state == "SelectStartNode"
            ? EndpointRole.Start
            : state == "SelectEndNode"
                ? EndpointRole.End
                : EndpointRole.Unknown;
        RoadSelectionController.AttachAndEnforce(__instance);
        if (SelectedRoadPrefabField?.GetValue(__instance) is Entity prefab)
        {
            SelectedRoadPrefab = prefab;
            AlignmentRoadPrefab = RoadSelectionController.GetAlignmentPrefab(prefab);
        }
    }
}

internal static class NodeSelectionPatch
{
    private static readonly FieldInfo? EntityManagerField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "_entityManager");

    private static readonly FieldInfo? RaycastCandidateField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "_raycastCandidate");

    private static readonly PropertyInfo? LastEntityProperty = AccessTools.Property(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "LastEntity");

    private static readonly PropertyInfo? LastEdgeProperty = AccessTools.Property(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "LastEdge");

    private static readonly PropertyInfo? LastPrefabProperty = AccessTools.Property(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "LastPrefab");

    public static void Postfix(object __instance, ref bool __result, ref SelectedNode node)
    {
        if (!__result || node == null || node.IsFree)
        {
            return;
        }

        try
        {
            if (!(EntityManagerField?.GetValue(__instance) is EntityManager entityManager) ||
                !(LastEntityProperty?.GetValue(__instance) is Entity nodeEntity) ||
                !(LastEdgeProperty?.GetValue(__instance) is Entity edgeEntity))
            {
                return;
            }

            Entity nodePrefab = LastPrefabProperty?.GetValue(__instance) is Entity value
                ? value
                : Entity.Null;
            Entity selectionPrefab = RoadSelectionController.ResolveNodeAlignmentPrefab(
                ToolContextPatch.SelectedRoadPrefab,
                nodePrefab);
            if (selectionPrefab == Entity.Null)
            {
                return;
            }

            if (!EndpointAlignmentService.IsSelectableCompatibleEdge(
                    entityManager,
                    edgeEntity,
                    selectionPrefab))
            {
                node = null!;
                __result = false;
                return;
            }

            if (!TryGetHitPosition(__instance, out float3 hitPosition) ||
                !EndpointAlignmentService.TryChoose(
                    entityManager,
                    nodeEntity,
                    edgeEntity,
                    selectionPrefab,
                    hitPosition,
                    ToolContextPatch.CurrentEndpointRole,
                    out ConnectionSelection? selection) ||
                selection == null)
            {
                return;
            }

            node = new SelectedNode(
                node.Id,
                new RampEndpoint(selection.Position, node.Endpoint.Tangent),
                selection.FarPoint,
                node.ExistingOutwardGrade,
                isFree: false);
            ConnectionSelectionStore.Record(selection);
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn($"Endpoint alignment skipped: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static bool TryGetHitPosition(object instance, out float3 hitPosition)
    {
        hitPosition = default;
        if (!(RaycastCandidateField?.GetValue(instance) is Delegate callback))
        {
            return false;
        }

        object? candidate = callback.DynamicInvoke();
        if (candidate == null)
        {
            return false;
        }

        Type candidateType = candidate.GetType();
        PropertyInfo? hasHitProperty = candidateType.GetProperty(
            "HasHit",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo? hitPositionProperty = candidateType.GetProperty(
            "HitPosition",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (!(hasHitProperty?.GetValue(candidate) is bool hasHit) || !hasHit ||
            !(hitPositionProperty?.GetValue(candidate) is float3 value))
        {
            return false;
        }

        hitPosition = value;
        return true;
    }
}

internal static class ConnectedEdgeSelectionPatch
{
    private const float MinimumPointerDistance = 1.5f;
    private const float MinimumScoreGap = 0.08f;

    private static readonly FieldInfo? EntityManagerField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Selection.GameNetworkNodeSelectionService"),
        "_entityManager");

    public static bool Prefix(
        object __instance,
        Entity __0,
        float3 __1,
        DynamicBuffer<ConnectedEdge> __2,
        ref Entity __3,
        ref bool __result)
    {
        try
        {
            if (!(EntityManagerField?.GetValue(__instance) is EntityManager entityManager) ||
                !entityManager.Exists(__0) ||
                !entityManager.HasComponent<Node>(__0))
            {
                return true;
            }

            var edges = new List<Entity>();
            var directions = new List<Vector2>();
            for (int i = 0; i < __2.Length; i++)
            {
                Entity edge = __2[i].m_Edge;
                if (!EndpointAlignmentService.IsSelectableCompatibleEdge(
                        entityManager,
                        edge,
                        ToolContextPatch.AlignmentRoadPrefab) ||
                    !EndpointAlignmentService.TryGetOutwardDirection(entityManager, __0, edge, out Vector2 direction))
                {
                    continue;
                }

                edges.Add(edge);
                directions.Add(direction);
            }

            if (edges.Count == 0)
            {
                __3 = Entity.Null;
                __result = false;
                return false;
            }

            if (edges.Count == 1)
            {
                __3 = edges[0];
                __result = true;
                return false;
            }

            if (edges.Count == 2)
            {
                Node simpleNode = entityManager.GetComponentData<Node>(__0);
                if (JunctionArmMath.TryChoose(
                        directions,
                        new Vector2(simpleNode.m_Position.x, simpleNode.m_Position.z),
                        new Vector2(__1.x, __1.z),
                        minimumPointerDistance: 0.25f,
                        minimumScoreGap: 0.01f,
                        out int simpleIndex))
                {
                    __3 = edges[simpleIndex];
                }
                else
                {
                    __3 = edges[0].Index <= edges[1].Index ? edges[0] : edges[1];
                }

                __result = true;
                return false;
            }

            Node node = entityManager.GetComponentData<Node>(__0);
            if (!JunctionArmMath.TryChoose(
                    directions,
                    new Vector2(node.m_Position.x, node.m_Position.z),
                    new Vector2(__1.x, __1.z),
                    MinimumPointerDistance,
                    MinimumScoreGap,
                    out int index))
            {
                __3 = Entity.Null;
                __result = false;
                return false;
            }

            __3 = edges[index];
            __result = true;
            return false;
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn($"Junction arm selection fell back to the base mod: {exception.GetType().Name}: {exception.Message}");
            return true;
        }
    }
}

internal static class EndpointSelectionRadiusPatch
{
    public static void Prefix(ref float __1)
    {
        Entity selectedPrefab = ToolContextPatch.SelectedRoadPrefab;
        if (selectedPrefab == Entity.Null)
        {
            return;
        }

        try
        {
            // The base mod uses a fixed 18 m endpoint radius. Vanilla includes
            // the selected network half-width, which matters for very wide and
            // runtime-generated roads.
            if (World.DefaultGameObjectInjectionWorld?.EntityManager is EntityManager entityManager &&
                entityManager.Exists(selectedPrefab) &&
                entityManager.HasComponent<Game.Prefabs.NetGeometryData>(selectedPrefab))
            {
                float width = entityManager
                    .GetComponentData<Game.Prefabs.NetGeometryData>(selectedPrefab)
                    .m_DefaultWidth;
                if (!float.IsNaN(width) && !float.IsInfinity(width) && width > 0f)
                {
                    __1 = Math.Max(__1, 18f + width * 0.5f);
                }
            }
        }
        catch (Exception exception)
        {
            UpgradeLog.Warn($"Endpoint radius fell back to the base mod: {exception.GetType().Name}: {exception.Message}");
        }
    }
}

internal static class SnapshotConstructorPatch
{
    public static void Postfix(object __instance, object[] __args)
    {
        if (__args.Length < 6 ||
            !(__args[1] is Entity startNode) ||
            !(__args[2] is Entity endNode) ||
            !(__args[3] is Entity selectedPrefab) ||
            !(__args[4] is Vector3 startPosition) ||
            !(__args[5] is Vector3 endPosition))
        {
            return;
        }

        SnapshotConnections connections = ConnectionSelectionStore.AttachSnapshot(
            __instance,
            startNode,
            endNode,
            selectedPrefab,
            startPosition,
            endPosition,
            BuildRoutePolyline(__args));

        if (connections.Start != null || connections.End != null)
        {
            UpgradeUiBindings.SetSnapshot(connections);
            UpgradeLog.Info(
                "Native endpoint snapshot: " +
                $"start={Describe(connections.Start)}, end={Describe(connections.End)}");
        }
    }

    private static string Describe(ConnectionSelection? selection) => selection == null
        ? "none"
        : $"{selection.SlotName}@{selection.Offset:0.##}m " +
          $"({selection.SelectedWidth:0.##}->{selection.ExistingWidth:0.##}m, " +
          $"rule={(selection.UsesZoningGrid ? "cell+width" : "width")}, " +
           $"targetHalfAligned={selection.TargetHalfAligned})";

    private static IReadOnlyList<Vector2> BuildRoutePolyline(object[] arguments)
    {
        var result = new List<Vector2>();
        if (arguments.Length <= 13 ||
            !(arguments[13] is Colossal.Mathematics.Bezier4x3[] courses))
        {
            return result;
        }

        const int samplesPerCourse = 8;
        for (int courseIndex = 0; courseIndex < courses.Length; courseIndex++)
        {
            Colossal.Mathematics.Bezier4x3 course = courses[courseIndex];
            int firstSample = courseIndex == 0 ? 0 : 1;
            for (int sampleIndex = firstSample; sampleIndex <= samplesPerCourse; sampleIndex++)
            {
                float t = sampleIndex / (float)samplesPerCourse;
                float inverse = 1f - t;
                float3 point =
                    course.a * (inverse * inverse * inverse) +
                    course.b * (3f * inverse * inverse * t) +
                    course.c * (3f * inverse * t * t) +
                    course.d * (t * t * t);
                result.Add(new Vector2(point.x, point.z));
            }
        }

        return result;
    }
}

internal static class CourseCreationContextPatch
{
    [ThreadStatic]
    private static SnapshotConnections? _current;

    internal static SnapshotConnections? Current => _current;

    public static void Prefix(object[] __args)
    {
        _current = __args.Length > 0 && __args[0] != null &&
            ConnectionSelectionStore.TryGetSnapshot(__args[0], out SnapshotConnections connections)
                ? connections
                : null;
    }

    public static void Postfix() => _current = null;

    public static Exception? Finalizer(Exception? __exception)
    {
        _current = null;
        return __exception;
    }
}

internal static class CoursePositionPatch
{
    public static void Postfix(Entity entity, ref CoursePos __result)
    {
        SnapshotConnections? current = CourseCreationContextPatch.Current;
        if (current == null || current.NodeSplitOnly)
        {
            return;
        }

        ConnectionSelection? selection = Match(current, entity, __result.m_Position);
        if (selection == null)
        {
            return;
        }

        if ((__result.m_Flags & CoursePosFlags.IsParallel) == 0)
        {
            __result.m_Flags |= CoursePosFlags.IsLeft | CoursePosFlags.IsRight;
        }

        __result.m_Position.x = selection.Position.X;
        __result.m_Position.z = selection.Position.Z;
    }

    private static ConnectionSelection? Match(
        SnapshotConnections current,
        Entity entity,
        float3 position)
    {
        if (Matches(current.Start, entity, position))
        {
            return current.Start;
        }

        return Matches(current.End, entity, position) ? current.End : null;
    }

    private static bool Matches(ConnectionSelection? selection, Entity entity, float3 position)
    {
        if (selection == null || selection.Node != entity)
        {
            return false;
        }

        float dx = selection.Position.X - position.x;
        float dz = selection.Position.Z - position.z;
        return dx * dx + dz * dz <= 1f;
    }
}
