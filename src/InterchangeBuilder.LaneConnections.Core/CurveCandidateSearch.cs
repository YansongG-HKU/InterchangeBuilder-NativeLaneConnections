using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace InterchangeBuilder.LaneConnections;

public sealed class InspectedCurveCandidate
{
    internal InspectedCurveCandidate(string family,CurveGeometryDocument geometry)
    {Family=family;Geometry=geometry;Report=CurveGeometryVerifier.Verify(geometry.CapturedRequest,geometry);}
    public string Family { get; }
    public CurveGeometryDocument Geometry { get; }
    public CurveGeometryReport Report { get; }
    public bool CanApplyToGame=>false;
}
public sealed class CurveSearchResult
{
    internal CurveSearchResult(CurveKernelStatus status,List<InspectedCurveCandidate> candidates,int outOfDomain)
    {Status=status;Candidates=candidates.AsReadOnly();OutOfDomainCandidates=outOfDomain;}
    public CurveKernelStatus Status { get; }
    public ReadOnlyCollection<InspectedCurveCandidate> Candidates { get; }
    public int OutOfDomainCandidates { get; }
    public bool CanApplyToGame=>false;
}
public static class CurveCandidateSearch
{
    // Fixed finite search, not an optimizer or infeasibility proof. All actual outputs inspected.
    public static CurveSearchResult Search(CurveKernelRequest? request,bool enabled=false)
    {
        var baseline=ExperimentalCurveKernel.Generate(request,enabled);var candidates=new List<InspectedCurveCandidate>();int rejected=0;
        if(baseline.Status!=CurveKernelStatus.CandidateUnverified)return new CurveSearchResult(baseline.Status,candidates,0);
        var r=request!;var a=r.Start.ActualPosition;var end=r.End.ActualPosition;var q=end-r.End.TravelDirection*r.TailLength;
        void Add(string family,params CurveGeometrySegment[] pieces)=>candidates.Add(new InspectedCurveCandidate(family,new CurveGeometryDocument(r,r.Width,pieces)));
        var tail=new CurveGeometrySegment(CurveSegmentKind.Line,new[]{q,end});
        foreach(var c in baseline.Candidates)Add("baseline-"+c.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture),new CurveGeometrySegment(CurveSegmentKind.Bezier,c.Controls),tail);
        var chord=q-a;double distance=chord.Length;var direction=chord*(1/distance);var normal=new CurvePoint(-direction.Y,direction.X);
        // Straight specialization is inspected like any other output, not selected by a validity flag.
        Add("line",new CurveGeometrySegment(CurveSegmentKind.Line,new[]{a,q}),tail);
        foreach(double offset in new[]{-.2,0,.2})foreach(double factor in new[]{.8,1.0,1.2}){
            var middle=(a+q)*.5+normal*(distance*offset);
            try{
                var left=CurveFunctions.Hermite(a,middle,r.Start.TravelDirection,direction,r.Start.Curvature/(1-r.Start.Offset*r.Start.Curvature),0,(middle-a).Length*factor);
                var right=CurveFunctions.Hermite(middle,q,direction,r.End.TravelDirection,0,0,(q-middle).Length*factor);
                Add("split-"+offset.ToString(System.Globalization.CultureInfo.InvariantCulture)+"-"+factor.ToString(System.Globalization.CultureInfo.InvariantCulture),left,right,tail);
            }catch(ArgumentException){rejected++;}
        }
        return new CurveSearchResult(CurveKernelStatus.CandidateUnverified,candidates,rejected);
    }
}
