using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace InterchangeBuilder.LaneConnections;

// Opt-in pure math only. No Harmony hook, entity creation, or game placement permission.
public enum CurveKernelStatus { Disabled, InvalidInput, Unsupported, ConstraintConflict, Degenerate, CandidateUnverified }
public enum CurveTravelSide { Left, Middle, Right }
public enum CurvePlacementMode { Normal, NoCollision }

public readonly struct CurvePoint
{
    public CurvePoint(double x, double y) { X = x; Y = y; }
    public double X { get; }
    public double Y { get; }
    public static CurvePoint operator +(CurvePoint a, CurvePoint b) => new CurvePoint(a.X + b.X, a.Y + b.Y);
    public static CurvePoint operator -(CurvePoint a, CurvePoint b) => new CurvePoint(a.X - b.X, a.Y - b.Y);
    public static CurvePoint operator *(CurvePoint a, double k) => new CurvePoint(a.X * k, a.Y * k);
    public double Length => Math.Sqrt(X * X + Y * Y);
}

public sealed class CurvePort
{
    public CurvePort(string key, CurvePoint position, CurvePoint travelDirection, double curvature,
        double offset, double width, CurveTravelSide side)
    { Key = key; Position = position; TravelDirection = travelDirection; Curvature = curvature; Offset = offset; Width = width; Side = side; }
    // Opaque caller-supplied identity, not guessed from location. Entity/version extraction is NOT implemented.
    public string Key { get; }
    public CurvePoint Position { get; }
    public CurvePoint TravelDirection { get; }
    public double Curvature { get; }
    public double Offset { get; }
    public double Width { get; }
    public CurveTravelSide Side { get; }
    public CurvePoint ActualPosition => Position + new CurvePoint(-TravelDirection.Y, TravelDirection.X) * Offset;
}

public sealed class CurveKernelRequest
{
    public CurveKernelRequest(CurvePort start, CurvePort end, double tailLength, double width,
        bool requireG2, long revision, CurvePlacementMode mode = CurvePlacementMode.Normal)
    { Start = start; End = end; TailLength = tailLength; Width = width; RequireG2 = requireG2; Revision = revision; Mode = mode; }
    public CurvePort Start { get; }
    public CurvePort End { get; }
    public double TailLength { get; }
    public double Width { get; }
    public bool RequireG2 { get; }
    public long Revision { get; }
    public CurvePlacementMode Mode { get; }
}

public sealed class CurveCandidate
{
    internal CurveCandidate(CurvePoint[] controls, CurvePoint tailStart, CurvePoint tailEnd, double scale)
    { Controls = Array.AsReadOnly((CurvePoint[])controls.Clone()); TailStart = tailStart; TailEnd = tailEnd; Scale = scale; }
    public ReadOnlyCollection<CurvePoint> Controls { get; }
    public CurvePoint TailStart { get; }
    public CurvePoint TailEnd { get; }
    public double Scale { get; }
    public double ActualTailLength => (TailEnd - TailStart).Length;
}

public sealed class CurveKernelResult
{
    private readonly CurveKernelRequest? original;
    internal CurveKernelRequest? CapturedRequest => original;
    internal CurveKernelResult(CurveKernelStatus status, CurveKernelRequest? request, CurveCandidate[]? candidates = null)
    { original = request; Status = status; Revision = request?.Revision ?? -1; StartKey = request?.Start?.Key; EndKey = request?.End?.Key;
      Mode = request?.Mode ?? CurvePlacementMode.Normal; Candidates = Array.AsReadOnly(candidates ?? Array.Empty<CurveCandidate>()); }
    public CurveKernelStatus Status { get; }
    public long Revision { get; }
    public string? StartKey { get; }
    public string? EndKey { get; }
    public CurvePlacementMode Mode { get; }
    public ReadOnlyCollection<CurveCandidate> Candidates { get; }
    public bool CanApplyToGame => false;
    public bool Matches(CurveKernelRequest request) => request != null && Revision == request.Revision &&
        StartKey == request.Start?.Key && EndKey == request.End?.Key && Mode == request.Mode && original != null &&
        request.TailLength == original.TailLength && request.Width == original.Width && request.RequireG2 == original.RequireG2 &&
        SamePort(request.Start, original.Start) && SamePort(request.End, original.End);
    private static bool SamePort(CurvePort? a, CurvePort? b) => a != null && b != null && a.Key == b.Key &&
        a.Position.X == b.Position.X && a.Position.Y == b.Position.Y && a.TravelDirection.X == b.TravelDirection.X &&
        a.TravelDirection.Y == b.TravelDirection.Y && a.Curvature == b.Curvature && a.Offset == b.Offset && a.Width == b.Width && a.Side == b.Side;
}

public static class ExperimentalCurveKernel
{
    private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    private static bool Point(CurvePoint p, double bound) => Finite(p.X) && Finite(p.Y) && Math.Abs(p.X) <= bound && Math.Abs(p.Y) <= bound;
    private static bool Port(CurvePort? p) => p != null && !string.IsNullOrWhiteSpace(p.Key) && Point(p.Position, 10000) &&
        Point(p.TravelDirection, 1) && Math.Abs(p.TravelDirection.Length - 1) <= 1e-8 &&
        Finite(p.Curvature) && Math.Abs(p.Curvature) <= 1 && Finite(p.Offset) && Math.Abs(p.Offset) <= 100 &&
        Finite(p.Width) && p.Width > 0 && p.Width <= 50 &&
        ((p.Side == CurveTravelSide.Middle && p.Offset == 0) || (p.Side == CurveTravelSide.Left && p.Offset > 0) || (p.Side == CurveTravelSide.Right && p.Offset < 0));

    // Default false is intentional, and no existing runtime caller invokes this method.
    public static CurveKernelResult Generate(CurveKernelRequest? request, bool enabled = false)
    {
        if (!enabled) return new CurveKernelResult(CurveKernelStatus.Disabled, request);
        if (request == null || !Port(request.Start) || !Port(request.End) ||
            !Finite(request.TailLength) || request.TailLength <= 0 || request.TailLength > 500 ||
            !Finite(request.Width) || request.Width <= 0 || request.Width > 50 || request.Revision < 0 ||
            (request.Mode != CurvePlacementMode.Normal && request.Mode != CurvePlacementMode.NoCollision))
            return new CurveKernelResult(CurveKernelStatus.InvalidInput, request);
        var a = request.Start; var b = request.End;
        if (a.Width != request.Width || b.Width != request.Width || 1 - a.Offset * a.Curvature <= .05 || 1 - b.Offset * b.Curvature <= .05)
            return new CurveKernelResult(CurveKernelStatus.Unsupported, request);
        if (request.RequireG2 && Math.Abs(b.Curvature) > 1e-9)
            return new CurveKernelResult(CurveKernelStatus.ConstraintConflict, request);
        CurvePoint start = a.ActualPosition, end = b.ActualPosition, q = end - b.TravelDirection * request.TailLength;
        double distance = (q - start).Length;
        if (distance < 1e-6) return new CurveKernelResult(CurveKernelStatus.Degenerate, request);
        var candidates = new List<CurveCandidate>();
        foreach (double factor in new[] { .8, 1.0, 1.2 })
        {
            double speed = distance * factor;
            CurvePoint v = a.TravelDirection * speed, w = b.TravelDirection * speed;
            CurvePoint acceleration = new CurvePoint(-a.TravelDirection.Y, a.TravelDirection.X) *
                (a.Curvature / (1 - a.Offset * a.Curvature) * speed * speed);
            var controls = new[] { start, start + v * .2, start + v * .4 + acceleration * .05, q - w * .4, q - w * .2, q };
            foreach (var p in controls) if (!Point(p, 10000)) return new CurveKernelResult(CurveKernelStatus.Unsupported, request);
            candidates.Add(new CurveCandidate(controls, q, end, factor));
        }
        // Local endpoint construction is not corridor, vertical, lane, or engine verification.
        return new CurveKernelResult(CurveKernelStatus.CandidateUnverified, request, candidates.ToArray());
    }

    public static CurvePoint Evaluate(IReadOnlyList<CurvePoint> controls, double t)
    {
        if (controls == null || controls.Count < 2 || controls.Count > 6 || !Finite(t) || t < 0 || t > 1) throw new ArgumentException("Invalid curve evaluation input");
        var q = new CurvePoint[controls.Count];
        for (int i = 0; i < q.Length; i++) { if (!Point(controls[i], 10000)) throw new ArgumentException("Invalid control point"); q[i] = controls[i]; }
        for (int n = q.Length - 1; n > 0; n--) for (int i = 0; i < n; i++) q[i] = q[i] * (1 - t) + q[i + 1] * t;
        return q[0];
    }

    // Coordinates only: not a SelectedNode/road builder adapter.
    public static CurvePoint WorldXZ(Vector3 p) => new CurvePoint(p.X, p.Z);
}
