using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace InterchangeBuilder.LaneConnections;

public enum CurveGeometryStatus { InvalidInput, Failed, Unknown, VerifiedLocalPlanar }
public enum CurveSegmentKind { Bezier, Line }

public sealed class CurveGeometrySegment
{
    public CurveGeometrySegment(CurveSegmentKind kind, IReadOnlyList<CurvePoint>? controls)
    {
        Kind = kind;
        var copy = new CurvePoint[controls?.Count ?? 0];
        for (int i = 0; i < copy.Length; i++) copy[i] = controls![i];
        Controls = Array.AsReadOnly(copy);
    }
    public CurveSegmentKind Kind { get; }
    public ReadOnlyCollection<CurvePoint> Controls { get; }
}

public sealed class CurveGeometryDocument
{
    public CurveGeometryDocument(CurveKernelRequest? capturedRequest, double width, IReadOnlyList<CurveGeometrySegment?>? segments)
    {
        CapturedRequest = capturedRequest; Width = width;
        var copy = new CurveGeometrySegment?[segments?.Count ?? 0];
        for (int i = 0; i < copy.Length; i++) copy[i] = segments![i];
        Segments = Array.AsReadOnly(copy);
    }
    public CurveKernelRequest? CapturedRequest { get; }
    public double Width { get; }
    public ReadOnlyCollection<CurveGeometrySegment?> Segments { get; }
}

public sealed class CurveGeometryReport
{
    internal CurveGeometryReport(CurveGeometryStatus status, List<string> issues, Dictionary<string,double> metrics)
    { Status = status; Issues = issues.AsReadOnly(); Metrics = new ReadOnlyDictionary<string,double>(metrics); }
    public CurveGeometryStatus Status { get; }
    public ReadOnlyCollection<string> Issues { get; }
    public ReadOnlyDictionary<string,double> Metrics { get; }
    public bool CanApplyToGame => false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
    public string Scope => "Fixed-width local planar endpoint/join/tail/regularity/self-corridor checks only; external obstacles, vertical/lane/engine validation NOT_RUN";
}

// Independent inspector. Never calls Generate/Evaluate/Matches or trusts candidate status.
public static class CurveGeometryVerifier
{
    private const double Epsilon = 1e-7;
    private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    private static double Dot(CurvePoint a, CurvePoint b) => a.X*b.X+a.Y*b.Y;
    private static double Cross(CurvePoint a, CurvePoint b) => a.X*b.Y-a.Y*b.X;
    private static double Length(CurvePoint a) => Math.Sqrt(Dot(a,a));
    private static bool Point(CurvePoint p) => Finite(p.X)&&Finite(p.Y)&&Math.Abs(p.X)<=10000&&Math.Abs(p.Y)<=10000;
    private static CurvePoint Actual(CurvePort p) => new CurvePoint(p.Position.X-p.TravelDirection.Y*p.Offset,p.Position.Y+p.TravelDirection.X*p.Offset);
    private static bool Port(CurvePort? p) => p!=null&&!string.IsNullOrWhiteSpace(p.Key)&&Point(p.Position)&&Point(p.TravelDirection)&&
        Math.Abs(Length(p.TravelDirection)-1)<=1e-8&&Finite(p.Curvature)&&Math.Abs(p.Curvature)<=1&&Finite(p.Offset)&&Math.Abs(p.Offset)<=100&&Finite(p.Width)&&p.Width>0&&p.Width<=50&&
        ((p.Side==CurveTravelSide.Middle&&p.Offset==0)||(p.Side==CurveTravelSide.Left&&p.Offset>0)||(p.Side==CurveTravelSide.Right&&p.Offset<0));
    private static bool Request(CurveKernelRequest? r) => r!=null&&Port(r.Start)&&Port(r.End)&&Finite(r.Width)&&r.Width>0&&r.Width<=50&&Finite(r.TailLength)&&r.TailLength>0&&r.TailLength<=500&&r.Revision>=0&&(r.Mode==CurvePlacementMode.Normal||r.Mode==CurvePlacementMode.NoCollision);
    private static bool SamePort(CurvePort a,CurvePort b) => a.Key==b.Key&&a.Position.X==b.Position.X&&a.Position.Y==b.Position.Y&&a.TravelDirection.X==b.TravelDirection.X&&a.TravelDirection.Y==b.TravelDirection.Y&&a.Curvature==b.Curvature&&a.Offset==b.Offset&&a.Width==b.Width&&a.Side==b.Side;
    private static bool Same(CurveKernelRequest a,CurveKernelRequest b) => a.Revision==b.Revision&&a.Mode==b.Mode&&a.RequireG2==b.RequireG2&&a.Width==b.Width&&a.TailLength==b.TailLength&&SamePort(a.Start,b.Start)&&SamePort(a.End,b.End);
    private static CurvePoint Value(IReadOnlyList<CurvePoint> c,double t)
    {
        CurvePoint p=new CurvePoint(0,0);double choose=1;int n=c.Count-1;
        for(int i=0;i<=n;i++){if(i>0)choose=choose*(n-i+1)/i;p+=c[i]*(choose*Math.Pow(t,i)*Math.Pow(1-t,n-i));}return p;
    }
    private static CurvePoint[] Derivative(IReadOnlyList<CurvePoint> c)
    {var d=new CurvePoint[c.Count-1];for(int i=0;i<d.Length;i++)d[i]=(c[i+1]-c[i])*(c.Count-1);return d;}
    private static (CurvePoint p,CurvePoint d,double k) Jet(IReadOnlyList<CurvePoint> c,double t)
    {var d=Value(Derivative(c),t);var dd=c.Count>2?Value(Derivative(Derivative(c)),t):new CurvePoint(0,0);return(Value(c,t),d,Cross(d,dd)/Math.Pow(Length(d),3));}
    private static double Angle(CurvePoint a,CurvePoint b) => Math.Abs(Math.Atan2(Cross(a,b),Dot(a,b)));
    private static double PointDistance(CurvePoint p,CurvePoint a,CurvePoint b)
    {var d=b-a;double u=Math.Max(0,Math.Min(1,Dot(p-a,d)/(Dot(d,d)==0?1:Dot(d,d))));return Length(p-(a+d*u));}
    private static double Distance(CurvePoint a,CurvePoint b,CurvePoint c,CurvePoint d)
    {var r=b-a;var s=d-c;double den=Cross(r,s);if(Math.Abs(den)>1e-12){double u=Cross(c-a,s)/den,v=Cross(c-a,r)/den;if(u>=0&&u<=1&&v>=0&&v<=1)return 0;}return Math.Min(Math.Min(PointDistance(a,c,d),PointDistance(b,c,d)),Math.Min(PointDistance(c,a,b),PointDistance(d,a,b)));}
    private sealed class Leaf { public CurvePoint A,B; public double Error,Lower,Upper,Station; }
    private static void Split(IReadOnlyList<CurvePoint> c,out CurvePoint[] left,out CurvePoint[] right)
    {int n=c.Count;left=new CurvePoint[n];right=new CurvePoint[n];var q=new CurvePoint[n];for(int i=0;i<n;i++)q[i]=c[i];left[0]=q[0];right[n-1]=q[n-1];for(int k=n-1;k>0;k--){for(int i=0;i<k;i++)q[i]=(q[i]+q[i+1])*.5;left[n-k]=q[0];right[k-1]=q[k-1];}}

    public static CurveGeometryReport VerifyGenerated(CurveKernelRequest? request,CurveKernelResult? result,int index)
    {
        if(result==null||index<0||index>=result.Candidates.Count)return Verify(request,null);
        var c=result.Candidates[index];
        return Verify(request,new CurveGeometryDocument(result.CapturedRequest,result.CapturedRequest?.Width??double.NaN,new CurveGeometrySegment?[]{new CurveGeometrySegment(CurveSegmentKind.Bezier,c.Controls),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{c.TailStart,c.TailEnd})}));
    }

    public static CurveGeometryReport Verify(CurveKernelRequest? request,CurveGeometryDocument? geometry,double tolerance=.02,int maxDepth=16)
    {
        var issues=new List<string>();var metrics=new Dictionary<string,double>();bool failed=false,unknown=false;
        void Fail(string code){failed=true;if(!issues.Contains(code))issues.Add(code);}
        void Unknown(string code){unknown=true;if(!issues.Contains(code))issues.Add(code);}
        CurveGeometryReport Invalid(string code)=>new CurveGeometryReport(CurveGeometryStatus.InvalidInput,new List<string>{code},new Dictionary<string,double>());
        if(!Request(request)||geometry==null||!Request(geometry.CapturedRequest)||!Finite(tolerance)||tolerance<=0||tolerance>1||maxDepth<0||maxDepth>18)return Invalid("INVALID_REQUEST_OR_RESOLUTION");
        var r=request!;
        if(!Same(r,geometry.CapturedRequest!))return Invalid("STALE_REQUEST");
        if(!Finite(geometry.Width)||geometry.Width!=r.Width)return Invalid("WIDTH_MISMATCH");
        if(geometry.Segments.Count==0||geometry.Segments.Count>16)return Invalid("INVALID_SEGMENTS");
        foreach(var s in geometry.Segments){if(s==null||s.Controls.Count<2||s.Controls.Count>6||(s.Kind!=CurveSegmentKind.Line&&s.Kind!=CurveSegmentKind.Bezier)||(s.Kind==CurveSegmentKind.Line&&s.Controls.Count!=2))return Invalid("INVALID_SEGMENT");foreach(var p in s.Controls)if(!Point(p))return Invalid("NONFINITE_OR_OUT_OF_DOMAIN");}
        if(r.Start.Width!=r.Width||r.End.Width!=r.Width||1-r.Start.Offset*r.Start.Curvature<=.05||1-r.End.Offset*r.End.Curvature<=.05)Unknown("WIDTH_OR_OFFSET_UNSUPPORTED");
        var first=Jet(geometry.Segments[0]!.Controls,0);var last=Jet(geometry.Segments[geometry.Segments.Count-1]!.Controls,1);
        void Endpoint(string key,(CurvePoint p,CurvePoint d,double k) jet,CurvePort port){double pos=Length(jet.p-Actual(port)),angle=Angle(jet.d,port.TravelDirection),k=Math.Abs(jet.k-port.Curvature/(1-port.Offset*port.Curvature));if(!Finite(pos)||!Finite(angle)||!Finite(k)){Fail("DEGENERATE_ENDPOINT");return;}metrics[key+"PositionError"]=pos;metrics[key+"DirectionError"]=angle;metrics[key+"CurvatureError"]=k;if(pos>Epsilon)Fail("ENDPOINT_POSITION");if(angle>Epsilon)Fail("ENDPOINT_DIRECTION");if(r.RequireG2&&k>Epsilon)Fail("ENDPOINT_CURVATURE");}
        Endpoint("start",first,r.Start);Endpoint("end",last,r.End);
        for(int i=1;i<geometry.Segments.Count;i++){var a=Jet(geometry.Segments[i-1]!.Controls,1);var b=Jet(geometry.Segments[i]!.Controls,0);double pos=Length(a.p-b.p),angle=Angle(a.d,b.d),k=Math.Abs(a.k-b.k);if(!Finite(pos)||!Finite(angle)||!Finite(k)||pos>Epsilon||angle>Epsilon||k>Epsilon)Fail("JOIN_DISCONTINUITY");}
        double tail=0;var end=Actual(r.End);
        for(int i=geometry.Segments.Count-1;i>=0;i--){var s=geometry.Segments[i]!;if(s.Kind!=CurveSegmentKind.Line||Dot(s.Controls[1]-s.Controls[0],r.End.TravelDirection)<=0)break;bool collinear=true;foreach(var p in s.Controls)if(Math.Abs(Cross(p-end,r.End.TravelDirection))>Epsilon)collinear=false;if(!collinear)break;tail+=Length(s.Controls[1]-s.Controls[0]);}
        metrics["straightTail"]=tail;if(tail+Epsilon<r.TailLength)Fail("TAIL_TOO_SHORT");
        var leaves=new List<Leaf>();double station=0,totalUpper=0;
        void Walk(IReadOnlyList<CurvePoint> c,int depth){
            if(leaves.Count>=2048){Unknown("INSPECTION_BUDGET");return;}
            var a=c[0];var b=c[c.Count-1];double lower=Length(b-a),upper=0,error=0;
            for(int i=0;i<c.Count;i++){error=Math.Max(error,PointDistance(c[i],a,b));if(i>0)upper+=Length(c[i]-c[i-1]);}
            if((error<=tolerance&&upper-lower<=tolerance)||depth==maxDepth){
                double speed=double.PositiveInfinity;foreach(var d in Derivative(c))speed=Math.Min(speed,Dot(d,b-a)/(lower==0?1:lower));
                if(error>tolerance||upper-lower>tolerance||speed<=1e-10)Unknown("RESOLUTION_OR_REGULARITY");
                double crossBound=0;
                if(c.Count>2)foreach(var v in Derivative(c))foreach(var acc in Derivative(Derivative(c)))crossBound=Math.Max(crossBound,Math.Abs(Cross(v,acc)));
                double curvature=speed>1e-10?crossBound/(speed*speed*speed):double.PositiveInfinity;if(curvature*r.Width/2>=1)Unknown("OFFSET_REGULARITY_UNCERTAIN");
                leaves.Add(new Leaf{A=a,B=b,Error=error,Lower=lower,Upper=upper,Station=station});station+=lower;totalUpper+=upper;return;
            }
            Split(c,out var l,out var rr);Walk(l,depth+1);Walk(rr,depth+1);
        }
        foreach(var s in geometry.Segments){double before=station;Walk(s!.Controls,0);if(station-before<1e-8)Fail("DEGENERATE_SEGMENT");}
        metrics["lengthLower"]=station;metrics["lengthUpper"]=totalUpper;
        for(int i=0;i<leaves.Count;i++)for(int j=i+2;j<leaves.Count;j++){var a=leaves[i];var b=leaves[j];double d=Distance(a.A,a.B,b.A,b.B);if(d<1e-9&&a.Error+b.Error<1e-10)Fail("SELF_INTERSECTION");else if(b.Station-a.Station-a.Lower>r.Width*2){if(d<r.Width-a.Error-b.Error)Fail("NONLOCAL_CORRIDOR_OVERLAP");else if(d<=r.Width+a.Error+b.Error)Unknown("SELF_CORRIDOR_UNCERTAIN");}}
        foreach(var value in metrics.Values)if(!Finite(value))return Invalid("NONFINITE_DERIVED");
        return new CurveGeometryReport(failed?CurveGeometryStatus.Failed:unknown?CurveGeometryStatus.Unknown:CurveGeometryStatus.VerifiedLocalPlanar,issues,metrics);
    }
}
