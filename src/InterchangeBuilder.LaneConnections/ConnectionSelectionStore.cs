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
    }

    internal static SnapshotConnections AttachSnapshot(
        object snapshot,
        Entity startNode,
        Entity endNode,
        Entity selectedPrefab,
        Vector3 startPosition,
        Vector3 endPosition)
    {
        var connections = new SnapshotConnections
        {
            Start = TryResolve(startNode, selectedPrefab, startPosition),
            End = TryResolve(endNode, selectedPrefab, endPosition),
            NodeSplitOnly = ReadNodeSplitOnly(snapshot)
        };

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
    }

    private static ConnectionSelection? TryResolve(Entity node, Entity selectedPrefab, Vector3 position)
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
