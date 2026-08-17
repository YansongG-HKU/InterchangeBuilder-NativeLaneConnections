using System;

namespace InterchangeBuilder.LaneConnections;

public readonly struct RoadKey : IEquatable<RoadKey>
{
    public static RoadKey None { get; } = new RoadKey(-1, -1);

    public RoadKey(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public int Index { get; }

    public int Version { get; }

    public bool IsValid => Index >= 0;

    public bool Equals(RoadKey other) => Index == other.Index && Version == other.Version;

    public override bool Equals(object? obj) => obj is RoadKey other && Equals(other);

    public override int GetHashCode() => (Index * 397) ^ Version;

    public static bool operator ==(RoadKey left, RoadKey right) => left.Equals(right);

    public static bool operator !=(RoadKey left, RoadKey right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"{Index}:{Version}" : "none";
}

public sealed class RoadSelectionMemory
{
    public RoadKey Selected { get; private set; } = RoadKey.None;

    public RoadKey StartSource { get; private set; } = RoadKey.None;

    public int Revision { get; private set; }

    public bool RememberPanelSelection(RoadKey road)
    {
        if (!road.IsValid || Selected == road)
        {
            return false;
        }

        Selected = road;
        Revision++;
        return true;
    }

    public bool BeginRoute()
    {
        if (!StartSource.IsValid)
        {
            return false;
        }

        StartSource = RoadKey.None;
        Revision++;
        return true;
    }

    public bool RecordStartSource(RoadKey road)
    {
        if (!road.IsValid || StartSource == road)
        {
            return false;
        }

        StartSource = road;
        Revision++;
        return true;
    }

    public RoadKey ResolveOutput(RoadKey fallback) => Selected.IsValid ? Selected : fallback;

    public void Reset()
    {
        Selected = RoadKey.None;
        StartSource = RoadKey.None;
        Revision++;
    }
}
