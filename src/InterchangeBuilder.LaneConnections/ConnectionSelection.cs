using System.Numerics;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal sealed class ConnectionSelection
{
    internal ConnectionSelection(
        Entity node,
        Entity edge,
        Entity selectedPrefab,
        Vector3 position,
        Vector3 farPoint,
        float offset,
        int candidateIndex,
        int candidateCount,
        float selectedWidth,
        float existingWidth,
        bool usesZoningGrid,
        bool targetHalfAligned)
    {
        Node = node;
        Edge = edge;
        SelectedPrefab = selectedPrefab;
        Position = position;
        FarPoint = farPoint;
        Offset = offset;
        CandidateIndex = candidateIndex;
        CandidateCount = candidateCount;
        SelectedWidth = selectedWidth;
        ExistingWidth = existingWidth;
        UsesZoningGrid = usesZoningGrid;
        TargetHalfAligned = targetHalfAligned;
    }

    internal Entity Node { get; }

    internal Entity Edge { get; }

    internal Entity SelectedPrefab { get; }

    internal Vector3 Position { get; }

    internal Vector3 FarPoint { get; }

    internal float Offset { get; }

    internal int CandidateIndex { get; }

    internal int CandidateCount { get; }

    internal float SelectedWidth { get; }

    internal float ExistingWidth { get; }

    internal bool UsesZoningGrid { get; }

    internal bool TargetHalfAligned { get; }

    internal string SlotName
    {
        get
        {
            if (CandidateCount <= 1 || System.Math.Abs(Offset) < 0.001f)
            {
                return "center";
            }

            if (CandidateIndex == 0)
            {
                return "left";
            }

            if (CandidateIndex == CandidateCount - 1)
            {
                return "right";
            }

            return $"intermediate-{CandidateIndex}";
        }
    }
}
