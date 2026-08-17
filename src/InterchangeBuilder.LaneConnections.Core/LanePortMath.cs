using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace InterchangeBuilder.LaneConnections;

/// <summary>
/// A drivable lane expressed in the lateral coordinate system of a road arm.
/// Positive positions use one common physical axis at the connection. Flow is
/// +1 away from the shared node along that lane's own road arm, -1 toward the
/// node, and 0 for a two-way lane. Two one-way lanes therefore connect when
/// their non-zero flow signs are opposite.
/// </summary>
public readonly struct LaneDescriptor
{
    public LaneDescriptor(
        float position,
        int flow,
        int carriageway = 0,
        int group = 0,
        int index = 0,
        float width = 3.5f)
    {
        Position = position;
        Flow = flow > 0 ? 1 : flow < 0 ? -1 : 0;
        Carriageway = carriageway;
        Group = group;
        Index = index;
        Width = IsFinite(width) && width > 0.1f ? width : 3.5f;
    }

    public float Position { get; }

    public int Flow { get; }

    public int Carriageway { get; }

    public int Group { get; }

    public int Index { get; }

    public float Width { get; }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);
}

public sealed class LanePortCandidate
{
    internal LanePortCandidate(
        float offset,
        bool nativeAligned,
        bool laneAligned,
        int selectedLaneCount,
        int targetLaneCount,
        int[] matchedSelectedLanes,
        int[] matchedTargetLanes)
    {
        Offset = offset;
        NativeAligned = nativeAligned;
        LaneAligned = laneAligned;
        SelectedLaneCount = selectedLaneCount;
        TargetLaneCount = targetLaneCount;
        MatchedSelectedLanes = matchedSelectedLanes;
        MatchedTargetLanes = matchedTargetLanes;
    }

    public float Offset { get; }

    /// <summary>Candidate originates from the game's width/cell alignment rules.</summary>
    public bool NativeAligned { get; }

    /// <summary>Candidate aligns the largest compatible set of actual driving lanes.</summary>
    public bool LaneAligned { get; }

    public int SelectedLaneCount { get; }

    public int TargetLaneCount { get; }

    /// <summary>One-based lane ordinals, ordered from physical left to right.</summary>
    public IReadOnlyList<int> MatchedSelectedLanes { get; }

    /// <summary>One-based lane ordinals, ordered from physical left to right.</summary>
    public IReadOnlyList<int> MatchedTargetLanes { get; }

    public int MatchedLaneCount => MatchedSelectedLanes.Count;
}

public readonly struct LanePortChoice
{
    public LanePortChoice(Vector2 position, LanePortCandidate candidate, int index, int count)
    {
        Position = position;
        Candidate = candidate;
        Index = index;
        Count = count;
    }

    public Vector2 Position { get; }

    public LanePortCandidate Candidate { get; }

    public int Index { get; }

    public int Count { get; }
}

/// <summary>
/// Builds endpoint ports for arbitrary road layouts. Native width/cell
/// candidates are always retained. Actual lane metadata adds intermediate
/// candidates only when they align the maximum possible compatible lane set.
/// </summary>
public static class LanePortMath
{
    private const float ExactOffsetTolerance = 0.05f;
    private const float NativeMergeTolerance = 1.75f;
    private const float MinimumLaneMatchTolerance = 1f;
    private const float MaximumLaneMatchTolerance = 1.75f;

    public static IReadOnlyList<LanePortCandidate> BuildCandidates(
        IReadOnlyList<LaneDescriptor>? selectedLanes,
        IReadOnlyList<LaneDescriptor>? targetLanes,
        IReadOnlyList<float>? nativeOffsets)
    {
        List<LaneDescriptor> selected = Sanitize(selectedLanes);
        List<LaneDescriptor> target = Sanitize(targetLanes);
        var seeds = new List<OffsetSeed>();

        if (nativeOffsets != null)
        {
            for (int i = 0; i < nativeOffsets.Count; i++)
            {
                AddSeed(seeds, nativeOffsets[i], nativeAligned: true, laneAligned: false);
            }
        }

        // Centre is a safety invariant even when a custom prefab exposes no
        // usable lane metadata or the native candidate source changes.
        AddSeed(seeds, 0f, nativeAligned: true, laneAligned: false);

        if (selected.Count > 0 && target.Count > 0)
        {
            var laneSeeds = new List<float>();
            for (int i = 0; i < selected.Count; i++)
            {
                for (int j = 0; j < target.Count; j++)
                {
                    if (FlowsAreCompatible(selected[i].Flow, target[j].Flow))
                    {
                        AddUnique(laneSeeds, target[j].Position - selected[i].Position);
                    }
                }
            }

            int maximumMatches = 0;
            var matchesByOffset = new List<MatchResult>(laneSeeds.Count);
            for (int i = 0; i < laneSeeds.Count; i++)
            {
                MatchResult match = MatchAtOffset(selected, target, laneSeeds[i]);
                matchesByOffset.Add(match);
                maximumMatches = Math.Max(maximumMatches, match.Count);
            }

            // Requiring the maximum match count prevents misleading extreme
            // candidates that line up only one lane when a full lane window
            // can be aligned elsewhere.
            if (maximumMatches > 0)
            {
                var completeLaneOffsets = new List<float>();
                for (int i = 0; i < laneSeeds.Count; i++)
                {
                    if (matchesByOffset[i].Count == maximumMatches)
                    {
                        completeLaneOffsets.Add(laneSeeds[i]);
                    }
                }

                AddLaneSeeds(seeds, completeLaneOffsets);
            }
        }

        seeds.Sort((left, right) => left.Offset.CompareTo(right.Offset));
        var result = new List<LanePortCandidate>(seeds.Count);
        for (int i = 0; i < seeds.Count; i++)
        {
            MatchResult match = MatchAtOffset(selected, target, seeds[i].Offset);
            result.Add(new LanePortCandidate(
                seeds[i].Offset,
                seeds[i].NativeAligned,
                seeds[i].LaneAligned,
                selected.Count,
                target.Count,
                match.SelectedOrdinals,
                match.TargetOrdinals));
        }

        return result;
    }

    public static LanePortChoice Choose(
        Vector2 nodePosition,
        Vector2 normalizedLeft,
        Vector2 hitPosition,
        IReadOnlyList<LanePortCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            throw new ArgumentException("At least one endpoint port is required.", nameof(candidates));
        }

        normalizedLeft = NormalizeOrDefault(normalizedLeft);
        int bestIndex = 0;
        float bestDistanceSquared = float.MaxValue;
        Vector2 bestPosition = nodePosition;
        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2 position = nodePosition + normalizedLeft * candidates[i].Offset;
            float distanceSquared = Vector2.DistanceSquared(position, hitPosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestIndex = i;
                bestDistanceSquared = distanceSquared;
                bestPosition = position;
            }
        }

        return new LanePortChoice(bestPosition, candidates[bestIndex], bestIndex, candidates.Count);
    }

    private static List<LaneDescriptor> Sanitize(IReadOnlyList<LaneDescriptor>? lanes)
    {
        var result = new List<LaneDescriptor>();
        if (lanes == null)
        {
            return result;
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            LaneDescriptor lane = lanes[i];
            if (float.IsNaN(lane.Position) || float.IsInfinity(lane.Position))
            {
                continue;
            }

            bool duplicate = result.Any(existing =>
                Math.Abs(existing.Position - lane.Position) <= ExactOffsetTolerance &&
                existing.Flow == lane.Flow);
            if (!duplicate)
            {
                result.Add(lane);
            }
        }

        // Lane ordinals used by the UI are always physical left-to-right.
        result.Sort((left, right) => right.Position.CompareTo(left.Position));
        return result;
    }

    private static MatchResult MatchAtOffset(
        IReadOnlyList<LaneDescriptor> selected,
        IReadOnlyList<LaneDescriptor> target,
        float offset)
    {
        if (selected.Count == 0 || target.Count == 0)
        {
            return MatchResult.Empty;
        }

        var pairs = new List<LanePair>();
        for (int i = 0; i < selected.Count; i++)
        {
            for (int j = 0; j < target.Count; j++)
            {
                if (!FlowsAreCompatible(selected[i].Flow, target[j].Flow))
                {
                    continue;
                }

                float distance = Math.Abs(selected[i].Position + offset - target[j].Position);
                float tolerance = Math.Min(
                    MaximumLaneMatchTolerance,
                    Math.Max(
                        MinimumLaneMatchTolerance,
                        (selected[i].Width + target[j].Width) * 0.2f + 0.25f));
                if (distance <= tolerance)
                {
                    pairs.Add(new LanePair(i, j, distance));
                }
            }
        }

        pairs.Sort((left, right) =>
        {
            int distance = left.Distance.CompareTo(right.Distance);
            if (distance != 0)
            {
                return distance;
            }

            int selectedIndex = left.SelectedIndex.CompareTo(right.SelectedIndex);
            return selectedIndex != 0
                ? selectedIndex
                : left.TargetIndex.CompareTo(right.TargetIndex);
        });

        var usedSelected = new bool[selected.Count];
        var usedTarget = new bool[target.Count];
        var selectedOrdinals = new List<int>();
        var targetOrdinals = new List<int>();
        for (int i = 0; i < pairs.Count; i++)
        {
            LanePair pair = pairs[i];
            if (usedSelected[pair.SelectedIndex] || usedTarget[pair.TargetIndex])
            {
                continue;
            }

            usedSelected[pair.SelectedIndex] = true;
            usedTarget[pair.TargetIndex] = true;
            selectedOrdinals.Add(pair.SelectedIndex + 1);
            targetOrdinals.Add(pair.TargetIndex + 1);
        }

        selectedOrdinals.Sort();
        targetOrdinals.Sort();
        return new MatchResult(selectedOrdinals.ToArray(), targetOrdinals.ToArray());
    }

    private static bool FlowsAreCompatible(int selectedFlow, int targetFlow) =>
        selectedFlow == 0 || targetFlow == 0 || selectedFlow == -targetFlow;

    private static void AddLaneSeeds(List<OffsetSeed> seeds, IReadOnlyList<float> laneOffsets)
    {
        var nearestNativeIndexes = new int[laneOffsets.Count];
        var nativeUseCounts = new Dictionary<int, int>();
        for (int laneIndex = 0; laneIndex < laneOffsets.Count; laneIndex++)
        {
            nearestNativeIndexes[laneIndex] = -1;
            float laneOffset = laneOffsets[laneIndex];
            if (!IsFinite(laneOffset))
            {
                continue;
            }

            int nearestNative = -1;
            float nearestDistance = float.MaxValue;
            for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                if (!seeds[seedIndex].NativeAligned)
                {
                    continue;
                }

                float distance = Math.Abs(seeds[seedIndex].Offset - laneOffset);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNative = seedIndex;
                }
            }

            if (nearestNative >= 0 && nearestDistance <= NativeMergeTolerance)
            {
                nearestNativeIndexes[laneIndex] = nearestNative;
                nativeUseCounts.TryGetValue(nearestNative, out int count);
                nativeUseCounts[nearestNative] = count + 1;
            }
        }

        for (int laneIndex = 0; laneIndex < laneOffsets.Count; laneIndex++)
        {
            float laneOffset = laneOffsets[laneIndex];
            if (!IsFinite(laneOffset))
            {
                continue;
            }

            int nearestNative = nearestNativeIndexes[laneIndex];
            if (nearestNative >= 0 && nativeUseCounts[nearestNative] == 1)
            {
                OffsetSeed seed = seeds[nearestNative];
                seeds[nearestNative] = new OffsetSeed(seed.Offset, nativeAligned: true, laneAligned: true);
                continue;
            }

            // If two distinct lane windows would collapse into one native
            // point, keep both exact lane ports. This is essential for odd
            // lane-count transitions such as one lane into either side of a
            // two-lane one-way road.
            AddSeed(seeds, laneOffset, nativeAligned: false, laneAligned: true);
        }
    }

    private static void AddSeed(
        List<OffsetSeed> seeds,
        float offset,
        bool nativeAligned,
        bool laneAligned)
    {
        if (!IsFinite(offset))
        {
            return;
        }

        for (int i = 0; i < seeds.Count; i++)
        {
            if (Math.Abs(seeds[i].Offset - offset) <= ExactOffsetTolerance)
            {
                OffsetSeed existing = seeds[i];
                seeds[i] = new OffsetSeed(
                    existing.Offset,
                    existing.NativeAligned || nativeAligned,
                    existing.LaneAligned || laneAligned);
                return;
            }
        }

        seeds.Add(new OffsetSeed(offset, nativeAligned, laneAligned));
    }

    private static void AddUnique(List<float> values, float value)
    {
        if (!IsFinite(value))
        {
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (Math.Abs(values[i] - value) <= ExactOffsetTolerance)
            {
                return;
            }
        }

        values.Add(value);
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

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private readonly struct OffsetSeed
    {
        internal OffsetSeed(float offset, bool nativeAligned, bool laneAligned)
        {
            Offset = Math.Abs(offset) <= ExactOffsetTolerance ? 0f : offset;
            NativeAligned = nativeAligned;
            LaneAligned = laneAligned;
        }

        internal float Offset { get; }

        internal bool NativeAligned { get; }

        internal bool LaneAligned { get; }
    }

    private readonly struct LanePair
    {
        internal LanePair(int selectedIndex, int targetIndex, float distance)
        {
            SelectedIndex = selectedIndex;
            TargetIndex = targetIndex;
            Distance = distance;
        }

        internal int SelectedIndex { get; }

        internal int TargetIndex { get; }

        internal float Distance { get; }
    }

    private readonly struct MatchResult
    {
        internal static readonly MatchResult Empty = new MatchResult(Array.Empty<int>(), Array.Empty<int>());

        internal MatchResult(int[] selectedOrdinals, int[] targetOrdinals)
        {
            SelectedOrdinals = selectedOrdinals;
            TargetOrdinals = targetOrdinals;
        }

        internal int[] SelectedOrdinals { get; }

        internal int[] TargetOrdinals { get; }

        internal int Count => SelectedOrdinals.Length;
    }
}
