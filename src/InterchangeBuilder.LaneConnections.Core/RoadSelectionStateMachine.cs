using System;

namespace InterchangeBuilder.LaneConnections;

public enum RoadSelectionMode
{
    FollowStart,
    PendingConfirmation,
    Locked
}

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

public sealed class RoadSelectionStateMachine
{
    public RoadSelectionMode Mode { get; private set; } = RoadSelectionMode.FollowStart;

    public RoadKey Candidate { get; private set; } = RoadKey.None;

    public RoadKey Locked { get; private set; } = RoadKey.None;

    public RoadKey StartSource { get; private set; } = RoadKey.None;

    public int Revision { get; private set; }

    public bool CanConfirm => Mode == RoadSelectionMode.PendingConfirmation && Candidate.IsValid;

    public bool SelectCandidate(RoadKey road)
    {
        if (!road.IsValid)
        {
            return false;
        }

        if (Mode == RoadSelectionMode.Locked && Locked == road)
        {
            return false;
        }

        if (Mode == RoadSelectionMode.PendingConfirmation && Candidate == road)
        {
            return false;
        }

        Mode = RoadSelectionMode.PendingConfirmation;
        Candidate = road;
        Locked = RoadKey.None;
        Revision++;
        return true;
    }

    public bool Confirm()
    {
        if (!CanConfirm)
        {
            return false;
        }

        Locked = Candidate;
        Candidate = RoadKey.None;
        Mode = RoadSelectionMode.Locked;
        Revision++;
        return true;
    }

    public bool FollowStart()
    {
        if (Mode == RoadSelectionMode.FollowStart &&
            !Candidate.IsValid &&
            !Locked.IsValid)
        {
            return false;
        }

        Mode = RoadSelectionMode.FollowStart;
        Candidate = RoadKey.None;
        Locked = RoadKey.None;
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

    public bool RecordStart(RoadKey road)
    {
        if (!road.IsValid || StartSource == road)
        {
            return false;
        }

        StartSource = road;
        Revision++;
        return true;
    }

    public RoadKey ResolveOutput(RoadKey fallback)
    {
        switch (Mode)
        {
            case RoadSelectionMode.PendingConfirmation:
                return Candidate;
            case RoadSelectionMode.Locked:
                return Locked;
            default:
                return StartSource.IsValid ? StartSource : fallback;
        }
    }

    public void Reset()
    {
        Mode = RoadSelectionMode.FollowStart;
        Candidate = RoadKey.None;
        Locked = RoadKey.None;
        StartSource = RoadKey.None;
        Revision++;
    }
}
