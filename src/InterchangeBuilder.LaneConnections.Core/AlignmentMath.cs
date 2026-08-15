using System;
using System.Collections.Generic;
using System.Numerics;

namespace InterchangeBuilder.LaneConnections;

public readonly struct AlignmentChoice
{
    public AlignmentChoice(Vector2 position, float offset, int index, int count)
    {
        Position = position;
        Offset = offset;
        Index = index;
        Count = count;
    }

    public Vector2 Position { get; }

    public float Offset { get; }

    public int Index { get; }

    public int Count { get; }

    public string SlotName
    {
        get
        {
            if (Count <= 1 || Math.Abs(Offset) < 0.001f)
            {
                return "center";
            }

            if (Index == 0)
            {
                return "left";
            }

            if (Index == Count - 1)
            {
                return "right";
            }

            return $"intermediate-{Index}";
        }
    }
}

public static class AlignmentMath
{
    private const float MinimumWidthDifference = 1.6f;

    public static IReadOnlyList<float> BuildOffsets(float selectedWidth, float existingWidth, bool useZoningGrid)
    {
        selectedWidth = SanitizeWidth(selectedWidth);
        existingWidth = SanitizeWidth(existingWidth);

        if (useZoningGrid)
        {
            // The vanilla NetToolSystem has two endpoint branches controlled
            // by Snap.CellLength.  InterchangeBuilder has no equivalent snap
            // toggle, so expose the union of both native candidate sets:
            // cell-aligned positions plus free-width left/centre/right.
            return MergeOffsets(
                BuildCellLengthOffsets(selectedWidth, existingWidth),
                BuildWidthOffsets(selectedWidth, existingWidth));
        }

        return BuildWidthOffsets(selectedWidth, existingWidth);
    }

    private static IReadOnlyList<float> BuildCellLengthOffsets(float selectedWidth, float existingWidth)
    {
        int selectedCells = GetCellWidth(selectedWidth);
        int existingCells = GetCellWidth(existingWidth);
        int count = 1 + Math.Abs(existingCells - selectedCells);
        float first = (count - 1) * -4f;
        var offsets = new float[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = first + 8f * i;
        }

        return offsets;
    }

    private static IReadOnlyList<float> BuildWidthOffsets(float selectedWidth, float existingWidth)
    {
        float difference = Math.Abs(existingWidth - selectedWidth);
        if (difference <= MinimumWidthDifference)
        {
            return new[] { 0f };
        }

        return new[] { difference * -0.5f, 0f, difference * 0.5f };
    }

    private static IReadOnlyList<float> MergeOffsets(
        IReadOnlyList<float> first,
        IReadOnlyList<float> second)
    {
        var combined = new List<float>(first.Count + second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            combined.Add(first[i]);
        }

        for (int i = 0; i < second.Count; i++)
        {
            combined.Add(second[i]);
        }

        combined.Sort();
        var unique = new List<float>(combined.Count);
        for (int i = 0; i < combined.Count; i++)
        {
            if (unique.Count == 0 || Math.Abs(unique[unique.Count - 1] - combined[i]) >= 0.001f)
            {
                unique.Add(combined[i]);
            }
        }

        return unique.ToArray();
    }

    public static AlignmentChoice Choose(
        Vector2 nodePosition,
        Vector2 normalizedLeft,
        Vector2 hitPosition,
        float selectedWidth,
        float existingWidth,
        bool useZoningGrid)
    {
        normalizedLeft = NormalizeOrDefault(normalizedLeft);
        IReadOnlyList<float> offsets = BuildOffsets(selectedWidth, existingWidth, useZoningGrid);
        int bestIndex = 0;
        float bestDistanceSquared = float.MaxValue;
        Vector2 bestPosition = nodePosition;

        for (int i = 0; i < offsets.Count; i++)
        {
            Vector2 candidate = nodePosition + normalizedLeft * offsets[i];
            float distanceSquared = Vector2.DistanceSquared(candidate, hitPosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestIndex = i;
                bestPosition = candidate;
            }
        }

        return new AlignmentChoice(bestPosition, offsets[bestIndex], bestIndex, offsets.Count);
    }

    public static int GetCellWidth(float roadWidth)
    {
        roadWidth = SanitizeWidth(roadWidth);
        return (int)Math.Ceiling(roadWidth / 8f - 0.01f);
    }

    private static float SanitizeWidth(float width)
    {
        if (float.IsNaN(width) || float.IsInfinity(width) || width < 0f)
        {
            return 0f;
        }

        return width;
    }

    private static Vector2 NormalizeOrDefault(Vector2 value)
    {
        float lengthSquared = value.LengthSquared();
        if (lengthSquared < 0.000001f)
        {
            return Vector2.UnitY;
        }

        return value / (float)Math.Sqrt(lengthSquared);
    }
}
