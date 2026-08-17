using System;
using System.Collections.Generic;
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
        EndpointRole role,
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

        // Lateral signs are defined while looking from the selected node into
        // the attached road arm. This makes left/right stable at both ends of
        // an edge and independent of the edge entity's internal orientation.
        float3 nearForward = isStart
            ? curve.m_Bezier.b - curve.m_Bezier.a
            : curve.m_Bezier.c - curve.m_Bezier.d;
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

        IReadOnlyList<float> nativeOffsets = AlignmentMath.BuildOffsets(
            selectedWidth,
            existingWidth,
            useZoningGrid);
        List<LaneDescriptor> selectedLanes = ReadSelectedLanes(
            entityManager,
            selectedPrefab,
            role);
        List<LaneDescriptor> targetLanes = ReadTargetLanes(
            entityManager,
            edgeEntity,
            existingPrefab,
            isStart);
        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selectedLanes,
            targetLanes,
            nativeOffsets);
        LanePortChoice choice = LanePortMath.Choose(
            new Vector2(node.m_Position.x, node.m_Position.z),
            nearLeft,
            new Vector2(hitPosition.x, hitPosition.z),
            ports);

        float3 farPosition = isStart ? curve.m_Bezier.d : curve.m_Bezier.a;
        float3 farForward = isStart
            ? curve.m_Bezier.d - curve.m_Bezier.c
            : curve.m_Bezier.a - curve.m_Bezier.b;
        Vector2 farLeft = NormalizeLeft(farForward);
        Vector3 alignedPosition = new Vector3(choice.Position.X, node.m_Position.y, choice.Position.Y);
        Vector3 alignedFarPoint = new Vector3(
            farPosition.x + farLeft.X * choice.Candidate.Offset,
            farPosition.y,
            farPosition.z + farLeft.Y * choice.Candidate.Offset);
        bool targetHalfAligned = IsTargetHalfAligned(entityManager, edgeEntity, isStart);

        selection = new ConnectionSelection(
            nodeEntity,
            edgeEntity,
            selectedPrefab,
            alignedPosition,
            alignedFarPoint,
            choice.Candidate,
            choice.Index,
            choice.Count,
            selectedWidth,
            existingWidth,
            useZoningGrid,
            targetHalfAligned,
            role);
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

    private static List<LaneDescriptor> ReadSelectedLanes(
        EntityManager entityManager,
        Entity selectedPrefab,
        EndpointRole role)
    {
        var result = new List<LaneDescriptor>();
        if (role == EndpointRole.Unknown ||
            selectedPrefab == Entity.Null ||
            !entityManager.Exists(selectedPrefab))
        {
            return result;
        }

        bool atStart = role == EndpointRole.Start;
        AppendDefaultLanes(
            entityManager,
            selectedPrefab,
            atStart,
            expressInOpposingArmAxis: true,
            result);
        if (result.Count == 0)
        {
            AppendCompositionLanes(
                entityManager,
                selectedPrefab,
                atStart,
                expressInOpposingArmAxis: true,
                result);
        }

        return result;
    }

    private static List<LaneDescriptor> ReadTargetLanes(
        EntityManager entityManager,
        Entity edgeEntity,
        Entity targetPrefab,
        bool isStart)
    {
        var result = new List<LaneDescriptor>();
        if (entityManager.HasComponent<Composition>(edgeEntity))
        {
            Entity composition = entityManager.GetComponentData<Composition>(edgeEntity).m_Edge;
            if (composition != Entity.Null && entityManager.Exists(composition))
            {
                AppendCompositionLanes(
                    entityManager,
                    composition,
                    isStart,
                    expressInOpposingArmAxis: false,
                    result);
            }
        }

        // Old saves and some runtime-built custom roads may not yet expose an
        // actual edge composition. Their generated/default prefab lanes still
        // provide the same positions and flow flags.
        if (result.Count == 0)
        {
            AppendDefaultLanes(
                entityManager,
                targetPrefab,
                isStart,
                expressInOpposingArmAxis: false,
                result);
        }

        return result;
    }

    private static void AppendDefaultLanes(
        EntityManager entityManager,
        Entity prefab,
        bool atStart,
        bool expressInOpposingArmAxis,
        ICollection<LaneDescriptor> result)
    {
        if (!entityManager.HasBuffer<DefaultNetLane>(prefab))
        {
            return;
        }

        DynamicBuffer<DefaultNetLane> lanes = entityManager.GetBuffer<DefaultNetLane>(prefab, true);
        for (int i = 0; i < lanes.Length; i++)
        {
            DefaultNetLane lane = lanes[i];
            AppendLane(
                entityManager,
                lane.m_Lane,
                lane.m_Position.x,
                lane.m_Flags,
                lane.m_Carriageway,
                lane.m_Group,
                lane.m_Index,
                atStart,
                expressInOpposingArmAxis,
                result);
        }
    }

    private static void AppendCompositionLanes(
        EntityManager entityManager,
        Entity composition,
        bool atStart,
        bool expressInOpposingArmAxis,
        ICollection<LaneDescriptor> result)
    {
        if (!entityManager.HasBuffer<NetCompositionLane>(composition))
        {
            return;
        }

        DynamicBuffer<NetCompositionLane> lanes = entityManager.GetBuffer<NetCompositionLane>(composition, true);
        for (int i = 0; i < lanes.Length; i++)
        {
            NetCompositionLane lane = lanes[i];
            AppendLane(
                entityManager,
                lane.m_Lane,
                lane.m_Position.x,
                lane.m_Flags,
                lane.m_Carriageway,
                lane.m_Group,
                lane.m_Index,
                atStart,
                expressInOpposingArmAxis,
                result);
        }
    }

    private static void AppendLane(
        EntityManager entityManager,
        Entity lanePrefab,
        float localPosition,
        LaneFlags flags,
        byte carriageway,
        byte group,
        byte index,
        bool atStart,
        bool expressInOpposingArmAxis,
        ICollection<LaneDescriptor> result)
    {
        if (!IsDrivableLane(entityManager, lanePrefab, flags))
        {
            return;
        }

        float width = 3.5f;
        if (lanePrefab != Entity.Null &&
            entityManager.Exists(lanePrefab) &&
            entityManager.HasComponent<NetLaneData>(lanePrefab))
        {
            width = entityManager.GetComponentData<NetLaneData>(lanePrefab).m_Width;
        }

        int flow = LaneEndpointMath.ToArmFlow(
            inverted: (flags & LaneFlags.Invert) != 0,
            twoWay: (flags & LaneFlags.Twoway) != 0,
            atStart: atStart);

        // Local composition X is positive to the road's right. Target lanes
        // are expressed in the target arm's left axis. The new road leaves or
        // enters along the opposing arm, so its lateral axis is mirrored into
        // that same physical coordinate system before matching.
        float position = LaneEndpointMath.ToConnectionAxis(
            localPosition,
            atStart,
            expressInOpposingArmAxis);
        result.Add(new LaneDescriptor(position, flow, carriageway, group, index, width));
    }

    private static bool IsDrivableLane(
        EntityManager entityManager,
        Entity lanePrefab,
        LaneFlags flags)
    {
        if ((flags & (LaneFlags.Pedestrian |
                      LaneFlags.Parking |
                      LaneFlags.Utility |
                      LaneFlags.BicyclesOnly)) != 0)
        {
            return false;
        }

        if (lanePrefab != Entity.Null &&
            entityManager.Exists(lanePrefab) &&
            entityManager.HasComponent<CarLaneData>(lanePrefab))
        {
            return true;
        }

        return (flags & LaneFlags.Road) != 0;
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
