using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class RouteGraphResolverTests
{
    [Fact]
    public void ResolvesOffsetGeneratedChainThatDoesNotUseOriginalCentreNodes()
    {
        Vector2[] route =
        {
            new Vector2(0f, 0f),
            new Vector2(50f, 0f),
            new Vector2(100f, 0f)
        };
        var candidates = new List<RouteEdgeCandidate>();
        for (int i = 0; i < 6; i++)
        {
            float start = i * (100f / 6f);
            float end = (i + 1) * (100f / 6f);
            candidates.Add(new RouteEdgeCandidate(
                startNode: 100 + i,
                endNode: 101 + i,
                samples: new[] { new Vector2(start, 4f), new Vector2(end, 4f) }));
        }

        bool resolved = RouteGraphResolver.TryResolve(
            route,
            startAnchor: new Vector2(0f, 4f),
            endAnchor: new Vector2(100f, 4f),
            candidates,
            endpointTolerance: 2f,
            corridorTolerance: 8f,
            out int[] ordered,
            out string diagnostic);

        Assert.True(resolved, diagnostic);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, ordered);
    }

    [Fact]
    public void IgnoresUnrelatedParallelRoadOutsideCorridor()
    {
        Vector2[] route = { Vector2.Zero, new Vector2(20f, 0f) };
        RouteEdgeCandidate[] candidates =
        {
            Edge(1, 2, 0f, 10f, 3f),
            Edge(2, 3, 10f, 20f, 3f),
            Edge(10, 11, 0f, 20f, 30f)
        };

        bool resolved = RouteGraphResolver.TryResolve(
            route,
            new Vector2(0f, 3f),
            new Vector2(20f, 3f),
            candidates,
            endpointTolerance: 2f,
            corridorTolerance: 6f,
            out int[] ordered,
            out string diagnostic);

        Assert.True(resolved, diagnostic);
        Assert.Equal(new[] { 0, 1 }, ordered);
    }

    [Fact]
    public void RejectsDisconnectedPartialGeneration()
    {
        Vector2[] route = { Vector2.Zero, new Vector2(30f, 0f) };
        RouteEdgeCandidate[] candidates =
        {
            Edge(1, 2, 0f, 10f, 4f),
            Edge(3, 4, 20f, 30f, 4f)
        };

        bool resolved = RouteGraphResolver.TryResolve(
            route,
            new Vector2(0f, 4f),
            new Vector2(30f, 4f),
            candidates,
            endpointTolerance: 2f,
            corridorTolerance: 8f,
            out int[] ordered,
            out string diagnostic);

        Assert.False(resolved);
        Assert.Empty(ordered);
        Assert.Contains("connected path", diagnostic);
    }

    private static RouteEdgeCandidate Edge(long startNode, long endNode, float start, float end, float y) =>
        new RouteEdgeCandidate(
            startNode,
            endNode,
            new[] { new Vector2(start, y), new Vector2(end, y) });
}
