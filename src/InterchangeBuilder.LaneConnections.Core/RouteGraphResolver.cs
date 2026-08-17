using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace InterchangeBuilder.LaneConnections;

public sealed class RouteEdgeCandidate
{
    public RouteEdgeCandidate(long startNode, long endNode, IReadOnlyList<Vector2> samples)
    {
        if (samples == null || samples.Count < 2)
        {
            throw new ArgumentException("A route edge needs at least two geometry samples.", nameof(samples));
        }

        StartNode = startNode;
        EndNode = endNode;
        var copy = new Vector2[samples.Count];
        for (int i = 0; i < samples.Count; i++)
        {
            copy[i] = samples[i];
        }

        Samples = copy;
    }

    public long StartNode { get; }

    public long EndNode { get; }

    public IReadOnlyList<Vector2> Samples { get; }

    public Vector2 StartPosition => Samples[0];

    public Vector2 EndPosition => Samples[Samples.Count - 1];
}

/// <summary>
/// Resolves a generated road chain by geometry and graph connectivity. This
/// deliberately does not require the generated chain's first/last node to be
/// the original endpoint entity: offset ports in the native road tool can
/// legitimately create replacement nodes beside that original centre node.
/// </summary>
public static class RouteGraphResolver
{
    public static bool TryResolve(
        IReadOnlyList<Vector2> route,
        Vector2 startAnchor,
        Vector2 endAnchor,
        IReadOnlyList<RouteEdgeCandidate> candidates,
        float endpointTolerance,
        float corridorTolerance,
        out int[] orderedCandidateIndexes,
        out string diagnostic)
    {
        orderedCandidateIndexes = Array.Empty<int>();
        if (route == null || route.Count < 2)
        {
            diagnostic = "Route polyline is missing.";
            return false;
        }

        if (candidates == null || candidates.Count == 0)
        {
            diagnostic = "No generated road sections are available.";
            return false;
        }

        endpointTolerance = Math.Max(0.5f, endpointTolerance);
        corridorTolerance = Math.Max(endpointTolerance, corridorTolerance);
        float endpointToleranceSquared = endpointTolerance * endpointTolerance;

        var adjacency = new Dictionary<long, List<GraphEdge>>();
        var accepted = new bool[candidates.Count];
        int acceptedCount = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            RouteEdgeCandidate candidate = candidates[i];
            if (candidate.StartNode == candidate.EndNode ||
                !HasFiniteGeometry(candidate.Samples) ||
                MaximumDistanceToPolyline(candidate.Samples, route) > corridorTolerance)
            {
                continue;
            }

            accepted[i] = true;
            acceptedCount++;
            float length = PolylineLength(candidate.Samples);
            float deviation = AverageDistanceToPolyline(candidate.Samples, route);
            float weight = Math.Max(0.05f, length) + deviation * 2f;
            AddEdge(adjacency, candidate.StartNode, new GraphEdge(candidate.EndNode, i, weight));
            AddEdge(adjacency, candidate.EndNode, new GraphEdge(candidate.StartNode, i, weight));
        }

        if (acceptedCount == 0)
        {
            diagnostic = $"No generated section lies inside the {corridorTolerance:0.##} m route corridor.";
            return false;
        }

        var startNodes = new HashSet<long>();
        var endNodes = new HashSet<long>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!accepted[i])
            {
                continue;
            }

            RouteEdgeCandidate candidate = candidates[i];
            if (Vector2.DistanceSquared(candidate.StartPosition, startAnchor) <= endpointToleranceSquared)
            {
                startNodes.Add(candidate.StartNode);
            }

            if (Vector2.DistanceSquared(candidate.EndPosition, startAnchor) <= endpointToleranceSquared)
            {
                startNodes.Add(candidate.EndNode);
            }

            if (Vector2.DistanceSquared(candidate.StartPosition, endAnchor) <= endpointToleranceSquared)
            {
                endNodes.Add(candidate.StartNode);
            }

            if (Vector2.DistanceSquared(candidate.EndPosition, endAnchor) <= endpointToleranceSquared)
            {
                endNodes.Add(candidate.EndNode);
            }
        }

        if (startNodes.Count == 0)
        {
            diagnostic = $"No generated section starts within {endpointTolerance:0.##} m of the selected start port.";
            return false;
        }

        if (endNodes.Count == 0)
        {
            diagnostic = $"No generated section ends within {endpointTolerance:0.##} m of the selected end port.";
            return false;
        }

        if (!TryShortestConnectedPath(
                adjacency,
                startNodes,
                endNodes,
                out List<int> path,
                out long terminal))
        {
            diagnostic = $"Generated sections do not form a connected path between the selected ports " +
                         $"({acceptedCount} corridor section(s), terminal {terminal}).";
            return false;
        }

        if (path.Count == 0)
        {
            diagnostic = "The generated port path is empty.";
            return false;
        }

        orderedCandidateIndexes = path.ToArray();
        diagnostic = string.Empty;
        return true;
    }

    private static bool TryShortestConnectedPath(
        IReadOnlyDictionary<long, List<GraphEdge>> adjacency,
        IReadOnlyCollection<long> startNodes,
        IReadOnlyCollection<long> endNodes,
        out List<int> path,
        out long terminal)
    {
        path = new List<int>();
        terminal = 0;
        var distances = new Dictionary<long, float>();
        var previous = new Dictionary<long, PreviousStep>();
        var open = new HashSet<long>();
        var closed = new HashSet<long>();

        foreach (long start in startNodes)
        {
            distances[start] = 0f;
            open.Add(start);
        }

        while (open.Count > 0)
        {
            long current = 0;
            float currentDistance = float.MaxValue;
            foreach (long node in open)
            {
                float distance = distances[node];
                if (distance < currentDistance)
                {
                    current = node;
                    currentDistance = distance;
                }
            }

            open.Remove(current);
            closed.Add(current);
            if (endNodes.Contains(current) && previous.ContainsKey(current))
            {
                terminal = current;
                break;
            }

            if (!adjacency.TryGetValue(current, out List<GraphEdge>? edges))
            {
                continue;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                GraphEdge edge = edges[i];
                if (closed.Contains(edge.OtherNode))
                {
                    continue;
                }

                float nextDistance = currentDistance + edge.Weight;
                if (!distances.TryGetValue(edge.OtherNode, out float knownDistance) ||
                    nextDistance < knownDistance)
                {
                    distances[edge.OtherNode] = nextDistance;
                    previous[edge.OtherNode] = new PreviousStep(current, edge.CandidateIndex);
                    open.Add(edge.OtherNode);
                }
            }
        }

        if (terminal == 0 || !previous.ContainsKey(terminal))
        {
            return false;
        }

        long cursor = terminal;
        while (previous.TryGetValue(cursor, out PreviousStep step))
        {
            path.Add(step.CandidateIndex);
            cursor = step.PreviousNode;
        }

        path.Reverse();
        return path.Count > 0;
    }

    private static void AddEdge(
        IDictionary<long, List<GraphEdge>> adjacency,
        long node,
        GraphEdge edge)
    {
        if (!adjacency.TryGetValue(node, out List<GraphEdge>? edges))
        {
            edges = new List<GraphEdge>();
            adjacency[node] = edges;
        }

        edges.Add(edge);
    }

    private static float MaximumDistanceToPolyline(
        IReadOnlyList<Vector2> samples,
        IReadOnlyList<Vector2> route)
    {
        float maximum = 0f;
        for (int i = 0; i < samples.Count; i++)
        {
            maximum = Math.Max(maximum, DistanceToPolyline(samples[i], route));
        }

        return maximum;
    }

    private static float AverageDistanceToPolyline(
        IReadOnlyList<Vector2> samples,
        IReadOnlyList<Vector2> route)
    {
        float total = 0f;
        for (int i = 0; i < samples.Count; i++)
        {
            total += DistanceToPolyline(samples[i], route);
        }

        return total / samples.Count;
    }

    private static float DistanceToPolyline(Vector2 point, IReadOnlyList<Vector2> polyline)
    {
        float minimumSquared = float.MaxValue;
        for (int i = 1; i < polyline.Count; i++)
        {
            Vector2 start = polyline[i - 1];
            Vector2 end = polyline[i];
            Vector2 delta = end - start;
            float lengthSquared = delta.LengthSquared();
            float t = lengthSquared <= 0.000001f
                ? 0f
                : Math.Max(0f, Math.Min(1f, Vector2.Dot(point - start, delta) / lengthSquared));
            Vector2 closest = start + delta * t;
            minimumSquared = Math.Min(minimumSquared, Vector2.DistanceSquared(point, closest));
        }

        return (float)Math.Sqrt(minimumSquared);
    }

    private static float PolylineLength(IReadOnlyList<Vector2> polyline)
    {
        float length = 0f;
        for (int i = 1; i < polyline.Count; i++)
        {
            length += Vector2.Distance(polyline[i - 1], polyline[i]);
        }

        return length;
    }

    private static bool HasFiniteGeometry(IReadOnlyList<Vector2> samples)
    {
        for (int i = 0; i < samples.Count; i++)
        {
            if (float.IsNaN(samples[i].X) ||
                float.IsNaN(samples[i].Y) ||
                float.IsInfinity(samples[i].X) ||
                float.IsInfinity(samples[i].Y))
            {
                return false;
            }
        }

        return true;
    }

    private readonly struct GraphEdge
    {
        internal GraphEdge(long otherNode, int candidateIndex, float weight)
        {
            OtherNode = otherNode;
            CandidateIndex = candidateIndex;
            Weight = weight;
        }

        internal long OtherNode { get; }

        internal int CandidateIndex { get; }

        internal float Weight { get; }
    }

    private readonly struct PreviousStep
    {
        internal PreviousStep(long previousNode, int candidateIndex)
        {
            PreviousNode = previousNode;
            CandidateIndex = candidateIndex;
        }

        internal long PreviousNode { get; }

        internal int CandidateIndex { get; }
    }
}
