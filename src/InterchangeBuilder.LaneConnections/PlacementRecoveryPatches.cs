using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Game.Common;
using Game.Net;
using HarmonyLib;
using Unity.Entities;
using Unity.Mathematics;

namespace InterchangeBuilder.LaneConnections;

internal static class SubdividedChainRecoveryPatch
{
    private static readonly FieldInfo? EntityManagerField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Tools.CurveRoadApplyService"),
        "_entityManager");

    public static void Postfix(
        object __instance,
        object __0,
        IReadOnlyList<Entity> __1,
        ref List<Entity> __2,
        ref string __3,
        ref bool __result)
    {
        if (__result ||
            __0 == null ||
            __1 == null ||
            !ConnectionSelectionStore.TryGetSnapshot(__0, out SnapshotConnections connections) ||
            !connections.HasOffsetPort ||
            connections.RoutePolyline.Count < 2)
        {
            return;
        }

        try
        {
            if (!(EntityManagerField?.GetValue(__instance) is EntityManager entityManager))
            {
                return;
            }

            var entities = new List<Entity>();
            var graphCandidates = new List<RouteEdgeCandidate>();
            for (int i = 0; i < __1.Count; i++)
            {
                Entity entity = __1[i];
                if (entity == Entity.Null ||
                    !entityManager.Exists(entity) ||
                    entityManager.HasComponent<Deleted>(entity) ||
                    entityManager.HasComponent<Game.Tools.Temp>(entity) ||
                    !entityManager.HasComponent<Edge>(entity) ||
                    !entityManager.HasComponent<Curve>(entity))
                {
                    continue;
                }

                Edge edge = entityManager.GetComponentData<Edge>(entity);
                Curve curve = entityManager.GetComponentData<Curve>(entity);
                entities.Add(entity);
                graphCandidates.Add(new RouteEdgeCandidate(
                    EntityKey(edge.m_Start),
                    EntityKey(edge.m_End),
                    SampleCurve(curve.m_Bezier)));
            }

            float maximumOffset = Math.Max(
                connections.Start == null ? 0f : Math.Abs(connections.Start.Offset),
                connections.End == null ? 0f : Math.Abs(connections.End.Offset));
            float endpointTolerance = 6f;
            float corridorTolerance = Math.Max(8f, maximumOffset + 5f);
            if (!RouteGraphResolver.TryResolve(
                    connections.RoutePolyline,
                    connections.StartAnchor,
                    connections.EndAnchor,
                    graphCandidates,
                    endpointTolerance,
                    corridorTolerance,
                    out int[] orderedIndexes,
                    out string diagnostic))
            {
                __3 = "Offset-port resolver: " + diagnostic +
                      (string.IsNullOrWhiteSpace(__3) ? string.Empty : " Base resolver: " + __3);
                return;
            }

            var ordered = new List<Entity>(orderedIndexes.Length);
            for (int i = 0; i < orderedIndexes.Length; i++)
            {
                ordered.Add(entities[orderedIndexes[i]]);
            }

            __2 = ordered;
            __3 = string.Empty;
            __result = true;
            UpgradeLog.Info(
                $"Recovered native offset-port road chain: {ordered.Count} generated edge(s), " +
                $"start={connections.StartAnchor.X:0.##},{connections.StartAnchor.Y:0.##}, " +
                $"end={connections.EndAnchor.X:0.##},{connections.EndAnchor.Y:0.##}.");
        }
        catch (Exception exception)
        {
            __3 = "Offset-port resolver exception: " + exception.GetType().Name + ": " + exception.Message +
                  (string.IsNullOrWhiteSpace(__3) ? string.Empty : " Base resolver: " + __3);
            UpgradeLog.Warn(__3);
        }
    }

    private static long EntityKey(Entity entity) =>
        ((long)(uint)entity.Index << 32) | (uint)entity.Version;

    private static IReadOnlyList<Vector2> SampleCurve(Colossal.Mathematics.Bezier4x3 curve)
    {
        const int sampleCount = 8;
        var result = new Vector2[sampleCount + 1];
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float inverse = 1f - t;
            float3 point =
                curve.a * (inverse * inverse * inverse) +
                curve.b * (3f * inverse * inverse * t) +
                curve.c * (3f * inverse * t * t) +
                curve.d * (t * t * t);
            result[i] = new Vector2(point.x, point.z);
        }

        return result;
    }
}

internal static class PlacementFailureSafetyPatch
{
    private const string DirectNetworkTimeout =
        "Timeout: the game did not generate a complete temporary road network.";

    private static readonly FieldInfo? PlacementSnapshotField = AccessTools.Field(
        AccessTools.TypeByName("InterchangeBuilder.Tools.CurveRoadApplyService"),
        "_placementSnapshot");

    public static void Prefix(
        object __instance,
        ref string __0,
        ref IReadOnlyList<Entity>? __2)
    {
        if (!__0.StartsWith(DirectNetworkTimeout, StringComparison.Ordinal) ||
            __2 == null ||
            __2.Count == 0)
        {
            return;
        }

        object? snapshot = PlacementSnapshotField?.GetValue(__instance);
        if (snapshot == null ||
            !ConnectionSelectionStore.TryGetSnapshot(snapshot, out SnapshotConnections connections) ||
            !connections.HasOffsetPort)
        {
            return;
        }

        int retainedCount = __2.Count;
        __2 = null;
        __0 += $" 已检测到偏移端点，并保留 {retainedCount} 条游戏已经生成的道路，" +
               "避免因归属匹配失败而误删；请在地图中检查后再决定是否撤销。";
        UpgradeUiBindings.SetPlacementSafetyNotice(retainedCount);
        UpgradeLog.Warn(
            $"Offset-port timeout safety retained {retainedCount} generated edge(s) instead of marking them Deleted.");
    }
}
