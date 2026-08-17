using System.Numerics;
using System.Text;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal enum EndpointRole
{
    Unknown,
    Start,
    End
}

internal sealed class ConnectionSelection
{
    internal ConnectionSelection(
        Entity node,
        Entity edge,
        Entity selectedPrefab,
        Vector3 position,
        Vector3 farPoint,
        LanePortCandidate port,
        int candidateIndex,
        int candidateCount,
        float selectedWidth,
        float existingWidth,
        bool usesZoningGrid,
        bool targetHalfAligned,
        EndpointRole role)
    {
        Node = node;
        Edge = edge;
        SelectedPrefab = selectedPrefab;
        Position = position;
        FarPoint = farPoint;
        Port = port;
        CandidateIndex = candidateIndex;
        CandidateCount = candidateCount;
        SelectedWidth = selectedWidth;
        ExistingWidth = existingWidth;
        UsesZoningGrid = usesZoningGrid;
        TargetHalfAligned = targetHalfAligned;
        Role = role;
    }

    internal Entity Node { get; }

    internal Entity Edge { get; }

    internal Entity SelectedPrefab { get; }

    internal Vector3 Position { get; }

    internal Vector3 FarPoint { get; }

    internal LanePortCandidate Port { get; }

    internal float Offset => Port.Offset;

    internal int CandidateIndex { get; }

    internal int CandidateCount { get; }

    internal float SelectedWidth { get; }

    internal float ExistingWidth { get; }

    internal bool UsesZoningGrid { get; }

    internal bool TargetHalfAligned { get; }

    internal EndpointRole Role { get; }

    internal string SlotName
    {
        get
        {
            if (CandidateCount <= 1 || System.Math.Abs(Offset) < 0.001f)
            {
                return "center";
            }

            if (Offset > 0f)
            {
                return "left";
            }

            if (Offset < 0f)
            {
                return "right";
            }

            return $"intermediate-{CandidateIndex}";
        }
    }

    internal string ChineseDescription
    {
        get
        {
            string endpoint = Role == EndpointRole.Start
                ? "起点"
                : Role == EndpointRole.End
                    ? "终点"
                    : "端点";
            string side = System.Math.Abs(Offset) < 0.001f
                ? "中间"
                : Offset > 0f
                    ? "左侧"
                    : "右侧";
            string rule = Port.LaneAligned
                ? BuildLaneMapping()
                : Port.NativeAligned
                    ? "原生宽度/分区格对齐"
                    : "道路对齐";
            return $"{endpoint}：{side}（{Offset:+0.##;-0.##;0} 米）· {rule}";
        }
    }

    private string BuildLaneMapping()
    {
        string selected = FormatLaneOrdinals(Port.MatchedSelectedLanes, Port.SelectedLaneCount);
        string target = FormatLaneOrdinals(Port.MatchedTargetLanes, Port.TargetLaneCount);
        if (selected.Length == 0 || target.Length == 0)
        {
            return "实际行车道对齐";
        }

        return $"新路{selected} → 既有路{target}";
    }

    private static string FormatLaneOrdinals(
        System.Collections.Generic.IReadOnlyList<int> ordinals,
        int total)
    {
        if (ordinals.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("第");
        int runStart = ordinals[0];
        int previous = ordinals[0];
        for (int i = 1; i <= ordinals.Count; i++)
        {
            bool endsRun = i == ordinals.Count || ordinals[i] != previous + 1;
            if (!endsRun)
            {
                previous = ordinals[i];
                continue;
            }

            if (builder.Length > 1)
            {
                builder.Append('、');
            }

            builder.Append(runStart);
            if (previous != runStart)
            {
                builder.Append('–').Append(previous);
            }

            if (i < ordinals.Count)
            {
                runStart = ordinals[i];
                previous = ordinals[i];
            }
        }

        builder.Append("车道");
        if (total > 0)
        {
            builder.Append('/').Append(total);
        }

        return builder.ToString();
    }
}
