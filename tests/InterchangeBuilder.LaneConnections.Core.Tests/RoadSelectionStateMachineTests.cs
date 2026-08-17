using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class RoadSelectionMemoryTests
{
    private static readonly RoadKey TwoLane = new RoadKey(10, 1);
    private static readonly RoadKey FourLane = new RoadKey(20, 1);

    [Fact]
    public void PanelSelectionImmediatelyBecomesTheOutput()
    {
        var state = new RoadSelectionMemory();

        Assert.True(state.RememberPanelSelection(FourLane));
        Assert.Equal(FourLane, state.Selected);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void NewPanelSelectionReplacesThePreviousOutput()
    {
        var state = new RoadSelectionMemory();
        state.RememberPanelSelection(FourLane);

        Assert.True(state.RememberPanelSelection(TwoLane));

        Assert.Equal(TwoLane, state.Selected);
        Assert.Equal(TwoLane, state.ResolveOutput(FourLane));
    }

    [Fact]
    public void StartRoadNeverReplacesThePanelSelection()
    {
        var state = new RoadSelectionMemory();
        state.RememberPanelSelection(FourLane);

        state.RecordStartSource(TwoLane);

        Assert.Equal(TwoLane, state.StartSource);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void NewRouteKeepsThePanelSelection()
    {
        var state = new RoadSelectionMemory();
        state.RememberPanelSelection(FourLane);
        state.RecordStartSource(TwoLane);

        state.BeginRoute();

        Assert.Equal(RoadKey.None, state.StartSource);
        Assert.Equal(FourLane, state.Selected);
        Assert.Equal(FourLane, state.ResolveOutput(TwoLane));
    }

    [Fact]
    public void ResetClearsPanelAndStartSelections()
    {
        var state = new RoadSelectionMemory();
        state.RememberPanelSelection(FourLane);
        state.RecordStartSource(TwoLane);

        state.Reset();

        Assert.Equal(RoadKey.None, state.Selected);
        Assert.Equal(RoadKey.None, state.StartSource);
        Assert.Equal(TwoLane, state.ResolveOutput(TwoLane));
    }
}
