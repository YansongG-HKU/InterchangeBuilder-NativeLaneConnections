using System;
using System.Collections.Generic;
using System.Numerics;

namespace InterchangeBuilder.LaneConnections;

/// <summary>
/// Chooses the road arm that the pointer is actually aimed at.  Multi-arm
/// junctions deliberately remain unselected while the pointer is too close to
/// the node centre or exactly between two arms.
/// </summary>
public static class JunctionArmMath
{
    public static bool TryChoose(
        IReadOnlyList<Vector2> outwardDirections,
        Vector2 nodePosition,
        Vector2 hitPosition,
        float minimumPointerDistance,
        float minimumScoreGap,
        out int index)
    {
        index = -1;
        if (outwardDirections == null || outwardDirections.Count == 0)
        {
            return false;
        }

        if (outwardDirections.Count == 1)
        {
            index = 0;
            return HasDirection(outwardDirections[0]);
        }

        Vector2 pointer = hitPosition - nodePosition;
        float minimumDistance = Math.Max(0f, minimumPointerDistance);
        if (pointer.LengthSquared() < minimumDistance * minimumDistance)
        {
            return false;
        }

        pointer = Vector2.Normalize(pointer);
        float bestScore = float.NegativeInfinity;
        float secondScore = float.NegativeInfinity;
        int bestIndex = -1;

        for (int i = 0; i < outwardDirections.Count; i++)
        {
            Vector2 direction = outwardDirections[i];
            if (!HasDirection(direction))
            {
                continue;
            }

            direction = Vector2.Normalize(direction);
            float score = Vector2.Dot(direction, pointer);
            if (score > bestScore)
            {
                secondScore = bestScore;
                bestScore = score;
                bestIndex = i;
            }
            else if (score > secondScore)
            {
                secondScore = score;
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        if (!float.IsNegativeInfinity(secondScore) &&
            bestScore - secondScore < Math.Max(0f, minimumScoreGap))
        {
            return false;
        }

        index = bestIndex;
        return true;
    }

    private static bool HasDirection(Vector2 direction) =>
        !float.IsNaN(direction.X) &&
        !float.IsNaN(direction.Y) &&
        !float.IsInfinity(direction.X) &&
        !float.IsInfinity(direction.Y) &&
        direction.LengthSquared() > 0.000001f;
}
