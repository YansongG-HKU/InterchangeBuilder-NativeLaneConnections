using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class LanePortMathTests
{
    [Fact]
    public void OneWayTwoToFourExposesLeftCentreAndRightLaneWindows()
    {
        LaneDescriptor[] selected = OneWayLanes(2, +1);
        LaneDescriptor[] target = OneWayLanes(4, -1);

        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selected,
            target,
            AlignmentMath.BuildOffsets(12f, 20f, useZoningGrid: false));

        Assert.Equal(new[] { -4f, 0f, 4f }, ports.Select(port => port.Offset));
        Assert.Equal(new[] { 3, 4 }, ports[0].MatchedTargetLanes);
        Assert.Equal(new[] { 2, 3 }, ports[1].MatchedTargetLanes);
        Assert.Equal(new[] { 1, 2 }, ports[2].MatchedTargetLanes);
        Assert.All(ports, port => Assert.True(port.LaneAligned));
    }

    [Fact]
    public void WideOneWayRoadExposesEveryFullLaneWindowWithoutDuplicateShoulderPorts()
    {
        LaneDescriptor[] selected = OneWayLanes(2, +1);
        LaneDescriptor[] target = OneWayLanes(8, -1);

        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selected,
            target,
            AlignmentMath.BuildOffsets(8f, 32f, useZoningGrid: false));

        Assert.Equal(7, ports.Count);
        Assert.Equal(new[] { -12f, -7f, -3.5f, 0f, 3.5f, 7f, 12f }, ports.Select(port => port.Offset));
        Assert.Equal(new[] { 7, 8 }, ports[0].MatchedTargetLanes);
        Assert.Equal(new[] { 1, 2 }, ports[6].MatchedTargetLanes);
    }

    [Fact]
    public void DirectionCompatibilityHandlesAsymmetricRoads()
    {
        LaneDescriptor[] selected =
        {
            new LaneDescriptor(1.75f, +1),
            new LaneDescriptor(-1.75f, +1)
        };
        LaneDescriptor[] target =
        {
            new LaneDescriptor(5.25f, +1),
            new LaneDescriptor(1.75f, -1),
            new LaneDescriptor(-1.75f, -1),
            new LaneDescriptor(-5.25f, +1)
        };

        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selected,
            target,
            new[] { -4f, 0f, 4f });

        LanePortCandidate centre = Assert.Single(ports, port => port.LaneAligned);
        Assert.Equal(0f, centre.Offset);
        Assert.Equal(new[] { 2, 3 }, centre.MatchedTargetLanes);
    }

    [Fact]
    public void TwoWayLaneCanConnectToEitherFlow()
    {
        LaneDescriptor[] selected = { new LaneDescriptor(0f, 0) };
        LaneDescriptor[] target =
        {
            new LaneDescriptor(1.75f, +1),
            new LaneDescriptor(-1.75f, -1)
        };

        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selected,
            target,
            new[] { -2f, 0f, 2f });

        Assert.Contains(ports, port => port.LaneAligned && port.MatchedTargetLanes.SequenceEqual(new[] { 1 }));
        Assert.Contains(ports, port => port.LaneAligned && port.MatchedTargetLanes.SequenceEqual(new[] { 2 }));
    }

    [Fact]
    public void MetadataFreeRoadKeepsEveryNativeCandidateAndCentre()
    {
        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            selectedLanes: null,
            targetLanes: null,
            nativeOffsets: new[] { -8f, 8f });

        Assert.Equal(new[] { -8f, 0f, 8f }, ports.Select(port => port.Offset));
        Assert.All(ports, port => Assert.True(port.NativeAligned));
    }

    [Fact]
    public void PointerChoosesPhysicalLeftUsingPositiveOffset()
    {
        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            OneWayLanes(2, +1),
            OneWayLanes(4, -1),
            new[] { -4f, 0f, 4f });

        LanePortChoice choice = LanePortMath.Choose(
            Vector2.Zero,
            Vector2.UnitX,
            new Vector2(3.9f, 0f),
            ports);

        Assert.Equal(4f, choice.Candidate.Offset);
        Assert.Equal(new Vector2(4f, 0f), choice.Position);
    }

    [Fact]
    public void SameDirectionOneWayArmsDoNotClaimThroughLaneAlignment()
    {
        IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
            OneWayLanes(2, +1),
            OneWayLanes(4, +1),
            new[] { -4f, 0f, 4f });

        Assert.DoesNotContain(ports, port => port.LaneAligned);
        Assert.Contains(ports, port => port.Offset == 0f && port.NativeAligned);
    }

    [Fact]
    public void EveryOneWayLaneCountPairGetsEveryCompleteCompatibleWindow()
    {
        for (int selectedCount = 1; selectedCount <= 10; selectedCount++)
        {
            for (int targetCount = 1; targetCount <= 10; targetCount++)
            {
                IReadOnlyList<LanePortCandidate> ports = LanePortMath.BuildCandidates(
                    OneWayLanes(selectedCount, +1),
                    OneWayLanes(targetCount, -1),
                    nativeOffsets: null);
                LanePortCandidate[] lanePorts = ports.Where(port => port.LaneAligned).ToArray();

                int expectedWindows = System.Math.Abs(targetCount - selectedCount) + 1;
                Assert.True(
                    lanePorts.Length == expectedWindows,
                    $"{selectedCount}->{targetCount} expected {expectedWindows} lane windows, got " +
                    $"{lanePorts.Length}: {string.Join(",", lanePorts.Select(port => port.Offset))}");
                Assert.All(
                    lanePorts,
                    port => Assert.Equal(
                        System.Math.Min(selectedCount, targetCount),
                        port.MatchedLaneCount));
                Assert.Contains(ports, port => port.Offset == 0f);
            }
        }
    }

    [Fact]
    public void StartAndEndTransformsAlignPhysicalLanesAndOpposeThroughFlows()
    {
        // New road starts where an existing non-inverted edge ends.
        float selectedStartPosition = LaneEndpointMath.ToConnectionAxis(
            localPosition: 1.75f,
            atStart: true,
            expressInOpposingArmAxis: true);
        float targetEndPosition = LaneEndpointMath.ToConnectionAxis(
            localPosition: 1.75f,
            atStart: false,
            expressInOpposingArmAxis: false);
        int selectedStartFlow = LaneEndpointMath.ToArmFlow(false, false, atStart: true);
        int targetEndFlow = LaneEndpointMath.ToArmFlow(false, false, atStart: false);

        Assert.Equal(targetEndPosition, selectedStartPosition);
        Assert.Equal(-targetEndFlow, selectedStartFlow);

        // New road ends where another non-inverted edge starts.
        float selectedEndPosition = LaneEndpointMath.ToConnectionAxis(
            localPosition: 1.75f,
            atStart: false,
            expressInOpposingArmAxis: true);
        float targetStartPosition = LaneEndpointMath.ToConnectionAxis(
            localPosition: 1.75f,
            atStart: true,
            expressInOpposingArmAxis: false);
        int selectedEndFlow = LaneEndpointMath.ToArmFlow(false, false, atStart: false);
        int targetStartFlow = LaneEndpointMath.ToArmFlow(false, false, atStart: true);

        Assert.Equal(targetStartPosition, selectedEndPosition);
        Assert.Equal(-targetStartFlow, selectedEndFlow);
    }

    private static LaneDescriptor[] OneWayLanes(int count, int flow)
    {
        var lanes = new LaneDescriptor[count];
        float first = (count - 1) * 1.75f;
        for (int i = 0; i < count; i++)
        {
            lanes[i] = new LaneDescriptor(first - i * 3.5f, flow, index: i);
        }

        return lanes;
    }
}
