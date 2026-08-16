using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class RoadSelectionStateMachineTests
{
    private static readonly RoadKey TwoLane = new RoadKey(10, 1);
    private static readonly RoadKey FourLane = new RoadKey(20, 1);

    [Fact]
    public void ManualCandidateMustBeConfirmedBeforeItBecomesLocked()
    {
        var state = new RoadSelectionStateMachine();

        Assert.True(state.SelectCandidate(FourLane));
        Assert.Equal(RoadSelectionMode.PendingConfirmation, state.Mode);
        Assert.True(state.CanConfirm);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));

        Assert.True(state.Confirm());
        Assert.Equal(RoadSelectionMode.Locked, state.Mode);
        Assert.False(state.CanConfirm);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void LockedOutputDoesNotChangeWhenStartRoadIsRecorded()
    {
        var state = new RoadSelectionStateMachine();
        state.SelectCandidate(FourLane);
        state.Confirm();

        state.RecordStart(TwoLane);

        Assert.Equal(TwoLane, state.StartSource);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void FollowStartUsesTheStartRoadAndKeepsFallbackForFreeEndpoints()
    {
        var state = new RoadSelectionStateMachine();

        Assert.Equal(FourLane, state.ResolveOutput(FourLane));

        state.RecordStart(TwoLane);

        Assert.Equal(TwoLane, state.ResolveOutput(FourLane));
    }

    [Fact]
    public void NewRouteClearsOnlyTheStartSource()
    {
        var state = new RoadSelectionStateMachine();
        state.SelectCandidate(FourLane);
        state.Confirm();
        state.RecordStart(TwoLane);

        state.BeginRoute();

        Assert.Equal(RoadKey.None, state.StartSource);
        Assert.Equal(RoadSelectionMode.Locked, state.Mode);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void FollowStartExplicitlyReleasesTheManualLock()
    {
        var state = new RoadSelectionStateMachine();
        state.SelectCandidate(FourLane);
        state.Confirm();
        state.RecordStart(TwoLane);

        Assert.True(state.FollowStart());

        Assert.Equal(RoadSelectionMode.FollowStart, state.Mode);
        Assert.Equal(TwoLane, state.ResolveOutput(FourLane));
    }
}
