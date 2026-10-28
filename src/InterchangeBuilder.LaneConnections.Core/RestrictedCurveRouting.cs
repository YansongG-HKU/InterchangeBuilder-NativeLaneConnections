using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace InterchangeBuilder.LaneConnections;

public sealed class RestrictedRectangle
{
    public RestrictedRectangle(string id,CurvePoint centre,CurvePoint axis,double halfLength,double halfWidth)
    {Id=id;Centre=centre;double norm=axis.Length;
      Axis=CurveFunctions.Finite(norm)&&Math.Abs(norm-1)<=1e-8?axis*(1/norm):axis;
      HalfLength=halfLength;HalfWidth=halfWidth;}
    public string Id { get; }
    public CurvePoint Centre { get; }
    public CurvePoint Axis { get; }
    public double HalfLength { get; }
    public double HalfWidth { get; }
    internal CurvePoint Local(CurvePoint p){var d=p-Centre;return new CurvePoint(d.X*Axis.X+d.Y*Axis.Y,-d.X*Axis.Y+d.Y*Axis.X);}
}
public enum RestrictedStatus { Disabled, InvalidInput, ConstraintConflict, VerifiedRestrictedPlanar, SearchExhausted, Unknown, CandidateFailed }
public sealed class RestrictedReport
{
    internal RestrictedReport(RestrictedStatus status,IEnumerable<string> issues,int leaves)
    {Status=status;Issues=Array.AsReadOnly(issues.ToArray());Leaves=leaves;}
    public RestrictedStatus Status { get; }
    public ReadOnlyCollection<string> Issues { get; }
    public int Leaves { get; }
    public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}

// Independent actual-output inspector. No routing flags, obstacle proximity exemptions or mode bypass.
public static class RestrictedCurveVerifier
{
    internal static bool Valid(IReadOnlyList<RestrictedRectangle>? boxes,double margin)
    {
        if(boxes==null||boxes.Count>8||!CurveFunctions.Finite(margin)||margin<0||margin>100)return false;
        var ids=new HashSet<string>();
        foreach(var b in boxes){if(b==null||string.IsNullOrWhiteSpace(b.Id)||!ids.Add(b.Id)||!CurveFunctions.Finite(b.Centre.X)||!CurveFunctions.Finite(b.Centre.Y)||Math.Abs(b.Centre.X)>9000||Math.Abs(b.Centre.Y)>9000||!CurveFunctions.Finite(b.Axis.X)||!CurveFunctions.Finite(b.Axis.Y)||Math.Abs(b.Axis.Length-1)>1e-8||!CurveFunctions.Finite(b.HalfLength)||!CurveFunctions.Finite(b.HalfWidth)||b.HalfLength<=0||b.HalfWidth<=0||b.HalfLength>1000||b.HalfWidth>1000)return false;}
        return true;
    }
    public static RestrictedReport Verify(CurveKernelRequest request,CurveGeometryDocument geometry,IReadOnlyList<RestrictedRectangle> boxes,double margin,int maxDepth=18,double minimumRadius=12)
    {
        if(!Valid(boxes,margin)||maxDepth<0||maxDepth>20||!CurveFunctions.Finite(minimumRadius)||minimumRadius<=0||minimumRadius>1000)return new RestrictedReport(RestrictedStatus.InvalidInput,new[]{"INVALID_RESTRICTIONS"},0);
        var local=CurveGeometryVerifier.Verify(request,geometry);
        if(local.Status==CurveGeometryStatus.InvalidInput)return new RestrictedReport(RestrictedStatus.InvalidInput,local.Issues,0);
        if(local.Status==CurveGeometryStatus.Failed)return new RestrictedReport(RestrictedStatus.CandidateFailed,local.Issues,0);
        bool unknown=local.Status==CurveGeometryStatus.Unknown,failed=false;int leaves=0;var issues=new List<string>(local.Issues);double radius=request.Width/2+margin;
        void Inspect(CurvePoint[] controls,RestrictedRectangle box,int depth){
            if(leaves>=16384){unknown=true;if(!issues.Contains("COLLISION_BUDGET"))issues.Add("COLLISION_BUDGET");return;}
            var localPoints=controls.Select(box.Local).ToArray();
            // Expanded rectangle encloses the disk-swept width, including its rounded corners.
            bool outside=localPoints.All(p=>p.X>box.HalfLength+radius+1e-8)||localPoints.All(p=>p.X< -box.HalfLength-radius-1e-8)||localPoints.All(p=>p.Y>box.HalfWidth+radius+1e-8)||localPoints.All(p=>p.Y< -box.HalfWidth-radius-1e-8);
            if(outside){leaves++;return;}
            // Actual curve endpoints give witnesses, not just control polygon collisions.
            foreach(var p in new[]{localPoints[0],localPoints[localPoints.Length-1]}){
                double dx=Math.Max(0,Math.Abs(p.X)-box.HalfLength),dy=Math.Max(0,Math.Abs(p.Y)-box.HalfWidth);
                if(Math.Sqrt(dx*dx+dy*dy)<radius-1e-8){failed=true;issues.Add("HARD_RESTRICTION:"+box.Id);leaves++;return;}
            }
            if(depth==maxDepth){unknown=true;issues.Add("RESTRICTION_UNCERTAIN:"+box.Id);leaves++;return;}
            int n=controls.Length;var q=(CurvePoint[])controls.Clone();var left=new CurvePoint[n];var right=new CurvePoint[n];left[0]=q[0];right[n-1]=q[n-1];
            for(int k=n-1;k>0;k--){for(int i=0;i<k;i++)q[i]=(q[i]+q[i+1])*.5;left[n-k]=q[0];right[k-1]=q[k-1];}
            Inspect(left,box,depth+1);Inspect(right,box,depth+1);
        }
        void Radius(CurvePoint[] c,int depth){
            if(leaves>=16384){unknown=true;issues.Add("RADIUS_BUDGET");return;}
            int degree=c.Length-1;var d=new CurvePoint[degree];for(int j=0;j<degree;j++)d[j]=(c[j+1]-c[j])*degree;
            var a=new CurvePoint[Math.Max(0,degree-1)];for(int j=0;j<a.Length;j++)a[j]=(d[j+1]-d[j])*(degree-1);
            var chord=c[c.Length-1]-c[0];double speed=double.PositiveInfinity,cross=0;
            foreach(var v in d)speed=Math.Min(speed,(v.X*chord.X+v.Y*chord.Y)/(chord.Length>0?chord.Length:1));
            foreach(var v in d)foreach(var acc in a)cross=Math.Max(cross,Math.Abs(v.X*acc.Y-v.Y*acc.X));
            foreach(int j in new[]{0,degree-1})if(a.Length>0){var v=d[j];var acc=a[j==0?0:a.Length-1];double k=Math.Abs(v.X*acc.Y-v.Y*acc.X)/Math.Pow(v.Length,3);if(k>1/minimumRadius+1e-10){failed=true;issues.Add("MINIMUM_RADIUS");leaves++;return;}}
            if(speed>1e-10&&cross/(speed*speed*speed)<1/minimumRadius-1e-10){leaves++;return;}
            if(depth==maxDepth){unknown=true;issues.Add("RADIUS_UNCERTAIN");leaves++;return;}
            int n=c.Length;var q=(CurvePoint[])c.Clone();var left=new CurvePoint[n];var right=new CurvePoint[n];left[0]=q[0];right[n-1]=q[n-1];
            for(int k=n-1;k>0;k--){for(int j=0;j<k;j++)q[j]=(q[j]+q[j+1])*.5;left[n-k]=q[0];right[k-1]=q[k-1];}
            Radius(left,depth+1);Radius(right,depth+1);
        }
        foreach(var segment in geometry.Segments)Radius(segment!.Controls.ToArray(),0);
        foreach(var segment in geometry.Segments)foreach(var box in boxes)Inspect(segment!.Controls.ToArray(),box,0);
        return new RestrictedReport(failed?RestrictedStatus.CandidateFailed:unknown?RestrictedStatus.Unknown:RestrictedStatus.VerifiedRestrictedPlanar,issues.Distinct(),leaves);
    }
}
public sealed class RestrictedRouteCandidate
{
    internal RestrictedRouteCandidate(string family,CurveGeometryDocument geometry,RestrictedReport report)
    {Family=family;Geometry=geometry;Report=report;}
    public string Family { get; }
    public CurveGeometryDocument Geometry { get; }
    public RestrictedReport Report { get; }
}
public sealed class RestrictedRouteResult
{
    internal RestrictedRouteResult(RestrictedStatus status,List<RestrictedRouteCandidate> candidates,int attempts,IReadOnlyList<RestrictedRectangle>? boxes,double margin,double minimumRadius)
    {Status=status;Candidates=candidates.AsReadOnly();Attempts=attempts;Restrictions=Array.AsReadOnly(boxes==null?Array.Empty<RestrictedRectangle>():boxes.ToArray());Margin=margin;MinimumRadius=minimumRadius;}
    public ReadOnlyCollection<RestrictedRectangle> Restrictions { get; }
    public double Margin { get; }
    public double MinimumRadius { get; }
    public bool MatchesRestrictions(IReadOnlyList<RestrictedRectangle>? boxes,double margin,double minimumRadius)
    {
        if(!RestrictedCurveVerifier.Valid(Restrictions,Margin)||!RestrictedCurveVerifier.Valid(boxes,margin)||margin!=Margin||minimumRadius!=MinimumRadius||boxes!.Count!=Restrictions.Count)return false;
        for(int i=0;i<boxes.Count;i++){var a=boxes[i];var b=Restrictions[i];if(a.Id!=b.Id||a.Centre.X!=b.Centre.X||a.Centre.Y!=b.Centre.Y||a.Axis.X!=b.Axis.X||a.Axis.Y!=b.Axis.Y||a.HalfLength!=b.HalfLength||a.HalfWidth!=b.HalfWidth)return false;}return true;
    }
    public RestrictedStatus Status { get; }
    public ReadOnlyCollection<RestrictedRouteCandidate> Candidates { get; }
    public int Attempts { get; }
    public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}
public static class RestrictedCurveRouter
{
    private static bool SegmentClear(CurvePoint a,CurvePoint b,RestrictedRectangle box,double padding)
    {
        var p=box.Local(a);var d=box.Local(b)-p;double lo=0,hi=1;
        bool Slab(double start,double direction,double half){if(Math.Abs(direction)<1e-12)return Math.Abs(start)>half;double x=(-half-start)/direction,y=(half-start)/direction;if(x>y){double z=x;x=y;y=z;}lo=Math.Max(lo,x);hi=Math.Min(hi,y);return lo>hi;}
        return Slab(p.X,d.X,box.HalfLength+padding)||Slab(p.Y,d.Y,box.HalfWidth+padding);
    }
    private static double TailDistance(CurvePoint a,CurvePoint b,RestrictedRectangle box)
    {
        if(!SegmentClear(a,b,box,0))return 0;
        var p=box.Local(a);var q=box.Local(b);var v=q-p;double length2=v.X*v.X+v.Y*v.Y;
        double best=double.PositiveInfinity;
        foreach(var endpoint in new[]{p,q}){double x=Math.Max(0,Math.Abs(endpoint.X)-box.HalfLength),y=Math.Max(0,Math.Abs(endpoint.Y)-box.HalfWidth);best=Math.Min(best,Math.Sqrt(x*x+y*y));}
        foreach(int sx in new[]{-1,1})foreach(int sy in new[]{-1,1}){var corner=new CurvePoint(sx*box.HalfLength,sy*box.HalfWidth);var diff=corner-p;double t=length2>0?Math.Max(0,Math.Min(1,(diff.X*v.X+diff.Y*v.Y)/length2)):0;best=Math.Min(best,(corner-(p+v*t)).Length);}
        return best;
    }
    public static RestrictedRouteResult Search(CurveKernelRequest request,IReadOnlyList<RestrictedRectangle> boxes,double margin,bool enabled=false,int maximumAttempts=99,double minimumRadius=12)
    {
        var candidates=new List<RestrictedRouteCandidate>();int attempts=0;
        RestrictedRouteResult Finish(RestrictedStatus status)=>new RestrictedRouteResult(status,candidates,attempts,boxes,margin,minimumRadius);
        if(!enabled)return Finish(RestrictedStatus.Disabled);
        if(!RestrictedCurveVerifier.Valid(boxes,margin)||maximumAttempts<3||maximumAttempts>99||!CurveFunctions.Finite(minimumRadius)||minimumRadius<=0||minimumRadius>1000)return Finish(RestrictedStatus.InvalidInput);
        var baseline=ExperimentalCurveKernel.Generate(request,true);
        if(baseline.Status==CurveKernelStatus.ConstraintConflict)return Finish(RestrictedStatus.ConstraintConflict);
        if(baseline.Status!=CurveKernelStatus.CandidateUnverified)return Finish(baseline.Status==CurveKernelStatus.InvalidInput?RestrictedStatus.InvalidInput:RestrictedStatus.Unknown);
        var a=request.Start.ActualPosition;var end=request.End.ActualPosition;var q=end-request.End.TravelDirection*request.TailLength;
        var tail=new CurveGeometrySegment(CurveSegmentKind.Line,new[]{q,end});
        void Add(string family,params CurveGeometrySegment[] parts){attempts++;var g=new CurveGeometryDocument(request,request.Width,parts);candidates.Add(new RestrictedRouteCandidate(family,g,RestrictedCurveVerifier.Verify(request,g,boxes,margin,18,minimumRadius)));}
        foreach(var c in baseline.Candidates)Add("baseline",new CurveGeometrySegment(CurveSegmentKind.Bezier,c.Controls),tail);
        // Exact endpoint disk inside a hard rectangle is a fixed-port conflict, independent of route search.
        foreach(var b in boxes)foreach(var endpoint in new[]{a,q,end}){var p=b.Local(endpoint);double dx=Math.Max(0,Math.Abs(p.X)-b.HalfLength),dy=Math.Max(0,Math.Abs(p.Y)-b.HalfWidth);if(Math.Sqrt(dx*dx+dy*dy)<request.Width/2+margin-1e-8)return Finish(RestrictedStatus.ConstraintConflict);}
        foreach(var b in boxes)if(TailDistance(q,end,b)<request.Width/2+margin-1e-8)return Finish(RestrictedStatus.ConstraintConflict);
        foreach(double reserve in new[]{12d,24d,48d}){
            double padding=request.Width/2+margin+reserve;
            var nodes=new List<CurvePoint>{a,q};
            foreach(var b in boxes)foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})nodes.Add(b.Centre+b.Axis*(x*(b.HalfLength+padding+.01))+new CurvePoint(-b.Axis.Y,b.Axis.X)*(y*(b.HalfWidth+padding+.01)));
            int n=nodes.Count;var edges=new double[n,n];
            for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(boxes.All(b=>SegmentClear(nodes[i],nodes[j],b,padding))){edges[i,j]=edges[j,i]=(nodes[j]-nodes[i]).Length;}
            int[] Parents(int source){var dist=Enumerable.Repeat(double.PositiveInfinity,n).ToArray();var prev=Enumerable.Repeat(-1,n).ToArray();var done=new bool[n];dist[source]=0;
                for(int step=0;step<n;step++){int best=-1;for(int i=0;i<n;i++)if(!done[i]&&(best<0||dist[i]<dist[best]))best=i;if(best<0||double.IsInfinity(dist[best]))break;done[best]=true;for(int j=0;j<n;j++)if(!done[j]&&edges[best,j]>0&&dist[best]+edges[best,j]<dist[j]){dist[j]=dist[best]+edges[best,j];prev[j]=best;}}return prev;}
            var from=Parents(0);var to=Parents(1);var seen=new HashSet<string>();
            for(int via=1;via<n&&attempts<maximumAttempts;via++){
                var indices=new List<int>();int v=via;while(v!=-1){indices.Add(v);if(v==0)break;v=from[v];}if(indices[indices.Count-1]!=0)continue;indices.Reverse();v=to[via];if(via!=1){while(v!=-1){indices.Add(v);if(v==1)break;v=to[v];}if(indices[indices.Count-1]!=1)continue;}
                if(indices.Distinct().Count()!=indices.Count||indices.Count>12||!seen.Add(string.Join(",",indices)))continue;
                var points=indices.Select(i=>nodes[i]).ToArray();var tangents=new CurvePoint[points.Length];tangents[0]=request.Start.TravelDirection;tangents[tangents.Length-1]=request.End.TravelDirection;bool valid=true;
                for(int i=1;i<points.Length-1;i++){var before=points[i]-points[i-1];var after=points[i+1]-points[i];var tangent=before*(1/before.Length)+after*(1/after.Length);if(tangent.Length<1e-6){valid=false;break;}tangents[i]=tangent*(1/tangent.Length);}if(!valid)continue;
                foreach(double scale in new[]{.5,.8,1.1}){
                    if(attempts>=maximumAttempts)break;
                    try{var pieces=new List<CurveGeometrySegment>();for(int i=1;i<points.Length;i++)pieces.Add(CurveFunctions.Hermite(points[i-1],points[i],tangents[i-1],tangents[i],i==1?request.Start.Curvature/(1-request.Start.Offset*request.Start.Curvature):0,0,(points[i]-points[i-1]).Length*scale));pieces.Add(tail);Add("visibility-"+reserve+"-"+scale,pieces.ToArray());}catch(ArgumentException){attempts++;}
                }
            }
        }
        if(candidates.Any(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar))return Finish(RestrictedStatus.VerifiedRestrictedPlanar);
        return Finish(candidates.Any(c=>c.Report.Status==RestrictedStatus.Unknown)?RestrictedStatus.Unknown:RestrictedStatus.SearchExhausted);
    }
}
