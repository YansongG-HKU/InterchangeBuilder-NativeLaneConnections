using System;
using System.Numerics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace InterchangeBuilder.LaneConnections;

internal static class EndpointAlignmentService
{
    internal static bool TryChoose(
        EntityManager entityManager,
        Entity nodeEntity,
        Entity edgeEntity,
        Entity selectedPrefab,
        float3 hitPosition,
        out ConnectionSelection? selection)
    {
        selection = null;
        if (nodeEntity == Entity.Null || edgeEntity == Entity.Null || selectedPrefab == Entity.Null)
        {
            return false;
        }

        if (!entityManager.Exists(nodeEntity) ||
            !entityManager.Exists(edgeEntity) ||
            !entityManager.Exists(selectedPrefab) ||
            entityManager.HasComponent<Deleted>(edgeEntity) ||
            entityManager.HasComponent<Game.Tools.Temp>(edgeEntity) ||
            !entityManager.HasComponent<Node>(nodeEntity) ||
            !entityManager.HasComponent<Edge>(edgeEntity) ||
            !entityManager.HasComponent<Curve>(edgeEntity))
        {
            return false;
        }

        if (!TryGetTargetPrefab(entityManager, edgeEntity, out Entity existingPrefab) ||
            !AreCompatibleNetworkPrefabs(entityManager, selectedPrefab, existingPrefab) ||
            !TryGetWidth(entityManager, selectedPrefab, out float selectedWidth) ||
            !TryGetExistingWidth(entityManager, edgeEntity, out float existingWidth))
        {
            return false;
        }

        Edge edge = entityManager.GetComponentData<Edge>(edgeEntity);
        Curve curve = entityManager.GetComponentData<Curve>(edgeEntity);
        Node node = entityManager.GetComponentData<Node>(nodeEntity);
        bool isStart = edge.m_Start == nodeEntity;
        if (!isStart && edge.m_End != nodeEntity)
        {
            return false;
        }

        float3 nearForward = isStart
            ? curve.m_Bezier.b - curve.m_Bezier.a
            : curve.m_Bezier.d - curve.m_Bezier.c;
        Vector2 nearLeft = NormalizeLeft(nearForward);
        if (nearLeft.LengthSquared() < 0.000001f)
        {
            return false;
        }

        NetGeometryData selectedGeometry = entityManager.GetComponentData<NetGeometryData>(selectedPrefab);
        bool useZoningGrid =
            (selectedGeometry.m_Flags & GeometryFlags.StrictNodes) == 0 &&
            entityManager.HasComponent<Game.Prefabs.RoadData>(selectedPrefab) &&
            (entityManager.GetComponentData<Game.Prefabs.RoadData>(selectedPrefab).m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;

        // Vanilla uses composition-area snapping for these special networks.
        // A width-based offset would be incorrect, so leave their original
        // endpoint untouched until an area candidate is supplied by the game.
        if ((selectedGeometry.m_Flags & GeometryFlags.SnapToNetAreas) != 0)
        {
            return false;
        }

        var choice = AlignmentMath.Choose(
            new Vector2(node.m_Position.x, node.m_Position.z),
            nearLeft,
            new Vector2(hitPosition.x, hitPosition.z),
            selectedWidth,
            existingWidth,
            useZoningGrid);

        float3 farPosition = isStart ? curve.m_Bezier.d : curve.m_Bezier.a;
        float3 farForward = isStart
            ? curve.m_Bezier.d - curve.m_Bezier.c
            : curve.m_Bezier.b - curve.m_Bezier.a;
        Vector2 farLeft = NormalizeLeft(farForward);
        Vector3 alignedPosition = new Vector3(choice.Position.X, node.m_Position.y, choice.Position.Y);
        Vector3 alignedFarPoint = new Vector3(
            farPosition.x + farLeft.X * choice.Offset,
            farPosition.y,
            farPosition.z + farLeft.Y * choice.Offset);
        bool targetHalfAligned = IsTargetHalfAligned(entityManager, edgeEntity, isStart);

        selection = new ConnectionSelection(
            nodeEntity,
            edgeEntity,
            selectedPrefab,
            alignedPosition,
            alignedFarPoint,
            choice.Offset,
            choice.Index,
            choice.Count,
            selectedWidth,
            existingWidth,
            useZoningGrid,
            targetHalfAligned);
        return true;
    }

    internal static bool IsSelectableCompatibleEdge(
        EntityManager entityManager,
        Entity edgeEntity,
        Entity selectedPrefab)
    {
        if (edgeEntity == Entity.Null ||
            !entityManager.Exists(edgeEntity) ||
            entityManager.HasComponent<Deleted>(edgeEntity) ||
            entityManager.HasComponent<Game.Tools.Temp>(edgeEntity) ||
            !entityManager.HasComponent<Edge>(edgeEntity) ||
            !entityManager.HasComponent<Curve>(edgeEntity) ||
            !TryGetTargetPrefab(entityManager, edgeEntity, out Entity existingPrefab) ||
            !IsSupportedNetworkPrefab(entityManager, existingPrefab))
        {
            return false;
        }

        return selectedPrefab == Entity.Null ||
            AreCompatibleNetworkPrefabs(entityManager, selectedPrefab, existingPrefab);
    }

    internal static bool TryGetOutwardDirection(
        EntityManager entityManager,
        Entity nodeEntity,
        Entity edgeEntity,
        out Vector2 direction)
    {
        direction = Vector2.Zero;
        if (!entityManager.Exists(nodeEntity) ||
            !entityManager.Exists(edgeEntity) ||
            !entityManager.HasComponent<Edge>(edgeEntity) ||
            !entityManager.HasComponent<Curve>(edgeEntity))
        {
            return false;
        }

        Edge edge = entityManager.GetComponentData<Edge>(edgeEntity);
        Curve curve = entityManager.GetComponentData<Curve>(edgeEntity);
        float3 outward;
        if (edge.m_Start == nodeEntity)
        {
            outward = curve.m_Bezier.b - curve.m_Bezier.a;
        }
        else if (edge.m_End == nodeEntity)
        {
            outward = curve.m_Bezier.c - curve.m_Bezier.d;
        }
        else
        {
            return false;
        }

        direction = new Vector2(outward.x, outward.z);
        float lengthSquared = direction.LengthSquared();
        if (lengthSquared < 0.000001f)
        {
            direction = Vector2.Zero;
            return false;
        }

        direction /= (float)Math.Sqrt(lengthSquared);
        return true;
    }

    private static bool AreCompatibleNetworkPrefabs(
        EntityManager entityManager,
        Entity selectedPrefab,
        Entity existingPrefab)
    {
        if (!IsSupportedNetworkPrefab(entityManager, selectedPrefab) ||
            !IsSupportedNetworkPrefab(entityManager, existingPrefab) ||
            !entityManager.HasComponent<NetData>(selectedPrefab) ||
            !entityManager.HasComponent<NetData>(existingPrefab) ||
            IsMarkerNetwork(entityManager, selectedPrefab) ||
            IsMarkerNetwork(entityManager, existingPrefab))
        {
            return false;
        }

        NetData selected = entityManager.GetComponentData<NetData>(selectedPrefab);
        NetData existing = entityManager.GetComponentData<NetData>(existingPrefab);
        return NetUtils.CanConnect(existing, selected);
    }

    private static bool IsSupportedNetworkPrefab(EntityManager entityManager, Entity prefab)
    {
        if (prefab == Entity.Null || !entityManager.Exists(prefab))
        {
            return false;
        }

        if (entityManager.HasComponent<Game.Prefabs.RoadData>(prefab) ||
            entityManager.HasComponent<PathwayData>(prefab))
        {
            return true;
        }

        if (!entityManager.HasComponent<TrackData>(prefab))
        {
            return false;
        }

        TrackTypes type = entityManager.GetComponentData<TrackData>(prefab).m_TrackType;
        return (type & (TrackTypes.Train | TrackTypes.Tram | TrackTypes.Subway)) != 0;
    }

    private static bool IsMarkerNetwork(EntityManager entityManager, Entity prefab) =>
        entityManager.HasComponent<NetGeometryData>(prefab) &&
        (entityManager.GetComponentData<NetGeometryData>(prefab).m_Flags & GeometryFlags.Marker) != 0;

    private static bool TryGetTargetPrefab(EntityManager entityManager, Entity edgeEntity, out Entity prefab)
    {
        prefab = Entity.Null;
        if (!entityManager.HasComponent<PrefabRef>(edgeEntity))
        {
            return false;
        }

        prefab = entityManager.GetComponentData<PrefabRef>(edgeEntity).m_Prefab;
        return prefab != Entity.Null && entityManager.Exists(prefab);
    }

    private static bool IsTargetHalfAligned(EntityManager entityManager, Entity edgeEntity, bool isStart)
    {
        if (!entityManager.HasComponent<Game.Net.Road>(edgeEntity))
        {
            return false;
        }

        Game.Net.Road road = entityManager.GetComponentData<Game.Net.Road>(edgeEntity);
        Game.Net.RoadFlags flag = isStart
            ? Game.Net.RoadFlags.StartHalfAligned
            : Game.Net.RoadFlags.EndHalfAligned;
        return (road.m_Flags & flag) != 0;
    }

    private static bool TryGetExistingWidth(EntityManager entityManager, Entity edgeEntity, out float width)
    {
        width = 0f;
        if (entityManager.HasComponent<PrefabRef>(edgeEntity))
        {
            Entity prefab = entityManager.GetComponentData<PrefabRef>(edgeEntity).m_Prefab;
            if (TryGetWidth(entityManager, prefab, out width))
            {
                return true;
            }
        }

        if (entityManager.HasComponent<Composition>(edgeEntity))
        {
            Entity composition = entityManager.GetComponentData<Composition>(edgeEntity).m_Edge;
            if (composition != Entity.Null &&
                entityManager.Exists(composition) &&
                entityManager.HasComponent<NetCompositionData>(composition))
            {
                width = entityManager.GetComponentData<NetCompositionData>(composition).m_Width;
                return IsUsableWidth(width);
            }
        }

        return false;
    }

    private static bool TryGetWidth(EntityManager entityManager, Entity prefab, out float width)
    {
        width = 0f;
        if (prefab == Entity.Null ||
            !entityManager.Exists(prefab) ||
            !entityManager.HasComponent<NetGeometryData>(prefab))
        {
            return false;
        }

        width = entityManager.GetComponentData<NetGeometryData>(prefab).m_DefaultWidth;
        return IsUsableWidth(width);
    }

    private static bool IsUsableWidth(float width) =>
        !float.IsNaN(width) && !float.IsInfinity(width) && width > 0.1f;

    private static Vector2 NormalizeLeft(float3 forward)
    {
        var left = new Vector2(-forward.z, forward.x);
        float lengthSquared = left.LengthSquared();
        if (lengthSquared < 0.000001f)
        {
            return Vector2.Zero;
        }

        return left / (float)Math.Sqrt(lengthSquared);
    }
}
