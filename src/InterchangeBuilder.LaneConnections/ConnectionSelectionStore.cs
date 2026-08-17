using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal sealed class SnapshotConnections
{
    internal ConnectionSelection? Start { get; set; }

    internal ConnectionSelection? End { get; set; }

    internal bool NodeSplitOnly { get; set; }

    internal IReadOnlyList<Vector2> RoutePolyline { get; set; } = Array.Empty<Vector2>();

    internal Vector2 StartAnchor { get; set; }

    internal Vector2 EndAnchor { get; set; }

    internal bool HasOffsetPort =>
        (Start != null && Math.Abs(Start.Offset) > 0.05f) ||
        (End != null && Math.Abs(End.Offset) > 0.05f);
}

internal static class ConnectionSelectionStore
{
    private static readonly object Sync = new object();
    private static readonly Dictionary<Entity, ConnectionSelection> LatestByNode = new Dictionary<Entity, ConnectionSelection>();
    private static readonly ConditionalWeakTable<object, SnapshotConnections> BySnapshot = new ConditionalWeakTable<object, SnapshotConnections>();

    internal static void Record(ConnectionSelection selection)
    {
        lock (Sync)
        {
            LatestByNode[selection.Node] = selection;
        }

        UpgradeUiBindings.SetEndpointSelection(selection);
    }

    internal static SnapshotConnections AttachSnapshot(
        object snapshot,
        Entity startNode,
        Entity endNode,
        Entity selectedPrefab,
        Vector3 startPosition,
        Vector3 endPosition,
        IReadOnlyList<Vector2>? routePolyline)
    {
        var connections = new SnapshotConnections
        {
            Start = TryResolve(startNode, selectedPrefab, startPosition, EndpointRole.Start),
            End = TryResolve(endNode, selectedPrefab, endPosition, EndpointRole.End),
            NodeSplitOnly = ReadNodeSplitOnly(snapshot),
            RoutePolyline = routePolyline ?? Array.Empty<Vector2>()
        };
        connections.StartAnchor = connections.Start != null
            ? new Vector2(connections.Start.Position.X, connections.Start.Position.Z)
            : new Vector2(startPosition.X, startPosition.Z);
        connections.EndAnchor = connections.End != null
            ? new Vector2(connections.End.Position.X, connections.End.Position.Z)
            : new Vector2(endPosition.X, endPosition.Z);

        BySnapshot.Remove(snapshot);
        BySnapshot.Add(snapshot, connections);
        return connections;
    }

    internal static bool TryGetSnapshot(object snapshot, out SnapshotConnections connections) =>
        BySnapshot.TryGetValue(snapshot, out connections!);

    internal static void ClearPending()
    {
        lock (Sync)
        {
            LatestByNode.Clear();
        }

        UpgradeUiBindings.ClearEndpointStatus();
    }

    private static ConnectionSelection? TryResolve(
        Entity node,
        Entity selectedPrefab,
        Vector3 position,
        EndpointRole role)
    {
        if (node == Entity.Null)
        {
            return null;
        }

        lock (Sync)
        {
            if (!LatestByNode.TryGetValue(node, out ConnectionSelection selection))
            {
                return null;
            }

            if (selectedPrefab != Entity.Null && selection.SelectedPrefab != selectedPrefab)
            {
                return null;
            }

            if (selection.Role != EndpointRole.Unknown && selection.Role != role)
            {
                return null;
            }

            float dx = selection.Position.X - position.X;
            float dz = selection.Position.Z - position.Z;
            if (dx * dx + dz * dz > 1f)
            {
                return null;
            }

            LatestByNode.Remove(node);
            return selection;
        }
    }

    private static bool ReadNodeSplitOnly(object snapshot)
    {
        PropertyInfo? property = snapshot.GetType().GetProperty(
            "NodeSplitOnly",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(snapshot) is bool value && value;
    }
}
