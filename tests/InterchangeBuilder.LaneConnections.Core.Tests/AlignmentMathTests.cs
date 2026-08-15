using System.Numerics;
using System.Linq;
using Xunit;

namespace InterchangeBuilder.LaneConnections.Tests;

public sealed class AlignmentMathTests
{
    [Fact]
    public void EqualWidthsProduceOnlyTheCenterCandidate()
    {
        var offsets = AlignmentMath.BuildOffsets(16f, 16f, useZoningGrid: false);

        Assert.Equal(new[] { 0f }, offsets);
    }

    [Fact]
    public void DifferentHighwayWidthsProduceLeftCenterAndRightCandidates()
    {
        var offsets = AlignmentMath.BuildOffsets(8f, 24f, useZoningGrid: false);

        Assert.Equal(new[] { -8f, 0f, 8f }, offsets);
    }

    [Fact]
    public void MousePositionChoosesTheNearestLateralCandidate()
    {
        AlignmentChoice choice = AlignmentMath.Choose(
            Vector2.Zero,
            Vector2.UnitX,
            new Vector2(7.8f, 0f),
            selectedWidth: 8f,
            existingWidth: 24f,
            useZoningGrid: false);

        Assert.Equal(8f, choice.Offset);
        Assert.Equal("right", choice.SlotName);
        Assert.Equal(new Vector2(8f, 0f), choice.Position);
    }

    [Fact]
    public void ZoningRoadsCombineCellLengthAndFreeWidthCandidates()
    {
        var offsets = AlignmentMath.BuildOffsets(8f, 32f, useZoningGrid: true);

        Assert.Equal(new[] { -12f, -4f, 0f, 4f, 12f }, offsets);
    }

    [Fact]
    public void ZoningCandidateMatrixContainsBothNativeSnapBranches()
    {
        for (int selectedCells = 1; selectedCells <= 8; selectedCells++)
        {
            for (int existingCells = 1; existingCells <= 8; existingCells++)
            {
                var offsets = AlignmentMath.BuildOffsets(
                    selectedCells * 8f,
                    existingCells * 8f,
                    useZoningGrid: true);
                int cellDifference = System.Math.Abs(existingCells - selectedCells);
                int expectedCount = 1 + cellDifference + cellDifference % 2;

                Assert.Equal(expectedCount, offsets.Count);
                Assert.Contains(0f, offsets);
                float firstCellOffset = cellDifference * -4f;
                for (int i = 0; i <= cellDifference; i++)
                {
                    Assert.Contains(firstCellOffset + 8f * i, offsets);
                }
            }
        }
    }

    [Fact]
    public void OddCellDifferenceAlsoIncludesTheNativeFreeWidthCenter()
    {
        var offsets = AlignmentMath.BuildOffsets(8f, 32f, useZoningGrid: true);

        Assert.Equal(new[] { -12f, -4f, 0f, 4f, 12f }, offsets);
    }

    [Fact]
    public void EvenCellDifferenceIncludesTheVanillaCenterCandidate()
    {
        var offsets = AlignmentMath.BuildOffsets(8f, 24f, useZoningGrid: true);

        Assert.Equal(new[] { -8f, 0f, 8f }, offsets);
    }

    [Fact]
    public void FourLaneToTwoLaneIncludesLeftCenterAndRight()
    {
        var offsets = AlignmentMath.BuildOffsets(24f, 16f, useZoningGrid: true);

        Assert.Equal(new[] { -4f, 0f, 4f }, offsets);
    }

    [Fact]
    public void MouseCanChooseCenterForFourLaneToTwoLane()
    {
        AlignmentChoice choice = AlignmentMath.Choose(
            Vector2.Zero,
            Vector2.UnitX,
            new Vector2(0.2f, 0f),
            selectedWidth: 24f,
            existingWidth: 16f,
            useZoningGrid: true);

        Assert.Equal(0f, choice.Offset);
        Assert.Equal("center", choice.SlotName);
        Assert.Equal(new Vector2(0f, 0f), choice.Position);
    }

    [Fact]
    public void MultiArmJunctionChoosesTheAimedAtRoadArm()
    {
        Vector2[] arms = { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY };

        bool selected = JunctionArmMath.TryChoose(
            arms,
            Vector2.Zero,
            new Vector2(8f, 0.5f),
            minimumPointerDistance: 1.5f,
            minimumScoreGap: 0.08f,
            out int index);

        Assert.True(selected);
        Assert.Equal(0, index);
    }

    [Fact]
    public void MultiArmJunctionRejectsTheAmbiguousCentre()
    {
        Vector2[] arms = { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY };

        bool selected = JunctionArmMath.TryChoose(
            arms,
            Vector2.Zero,
            new Vector2(0.25f, 0.25f),
            minimumPointerDistance: 1.5f,
            minimumScoreGap: 0.08f,
            out int index);

        Assert.False(selected);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void MultiArmJunctionRejectsABisectorTie()
    {
        Vector2[] arms = { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX };

        bool selected = JunctionArmMath.TryChoose(
            arms,
            Vector2.Zero,
            new Vector2(5f, 5f),
            minimumPointerDistance: 1.5f,
            minimumScoreGap: 0.08f,
            out int index);

        Assert.False(selected);
        Assert.Equal(-1, index);
    }
}
