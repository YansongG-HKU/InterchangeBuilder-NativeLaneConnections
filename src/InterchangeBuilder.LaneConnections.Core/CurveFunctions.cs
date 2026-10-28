using System;
using System.Collections.Generic;

namespace InterchangeBuilder.LaneConnections;

public readonly struct CurveJet
{
    public CurveJet(CurvePoint position, CurvePoint tangent, double curvature)
    { Position=position; Tangent=tangent; Curvature=curvature; }
    public CurvePoint Position { get; }
    public CurvePoint Tangent { get; }
    public double Curvature { get; }
}
public readonly struct CurveLengthBounds
{
    public CurveLengthBounds(double lower,double upper,bool resolved)
    { Lower=lower; Upper=upper; Resolved=resolved; }
    public double Lower { get; }
    public double Upper { get; }
    public bool Resolved { get; }
}

public readonly struct CurveStationBracket
{
    public CurveStationBracket(double lowerParameter,double upperParameter,bool resolved)
    { LowerParameter=lowerParameter; UpperParameter=upperParameter; Resolved=resolved; }
    public double LowerParameter { get; }
    public double UpperParameter { get; }
    public bool Resolved { get; }
}

// Pure primitives; no engine representation or placement permission.
public static class CurveFunctions
{
    internal static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    internal static void Point(CurvePoint p)
    { if(!Finite(p.X)||!Finite(p.Y)||Math.Abs(p.X)>10000||Math.Abs(p.Y)>10000)throw new ArgumentException("Point outside mathematical domain"); }
    private static void Parameter(double t)
    { if(!Finite(t)||t<0||t>1)throw new ArgumentException("Parameter must be in [0,1]"); }
    private static CurvePoint Unit(CurvePoint p)
    {double n=p.Length;if(!Finite(n)||n<1e-10)throw new ArgumentException("Degenerate tangent");return p*(1/n);}
    public static CurveJet Line(CurvePoint a,CurvePoint b,double t)
    {Point(a);Point(b);Parameter(t);return new CurveJet(a+(b-a)*t,Unit(b-a),0);}
    // Signed sweep in [-pi,pi], excludes near-zero; exact centreline length = radius*abs(sweep).
    public static CurveJet Arc(CurvePoint centre,double radius,double startAngle,double sweep,double t)
    {
        Point(centre);Parameter(t);
        if(!Finite(radius)||radius<=1e-6||radius>1000||!Finite(startAngle)||Math.Abs(startAngle)>2*Math.PI||!Finite(sweep)||Math.Abs(sweep)<1e-8||Math.Abs(sweep)>Math.PI)throw new ArgumentException("Arc outside supported domain");
        double angle=startAngle+t*sweep,sign=Math.Sign(sweep);
        var p=centre+new CurvePoint(Math.Cos(angle),Math.Sin(angle))*radius;Point(p);
        return new CurveJet(p,new CurvePoint(-Math.Sin(angle)*sign,Math.Cos(angle)*sign),sign/radius);
    }
    public static double ArcLength(double radius,double sweep)
    {Arc(new CurvePoint(0,0),radius,0,sweep,0);return radius*Math.Abs(sweep);}
    public static CurveJet Bezier(IReadOnlyList<CurvePoint> controls,double t)
    {
        Parameter(t);Validate(controls);
        var d=Derivative(controls);var v=Value(d,t);var tangent=Unit(v);
        var a=d.Length>1?Value(Derivative(d),t):new CurvePoint(0,0);
        return new CurveJet(Value(controls,t),tangent,(v.X*a.Y-v.Y*a.X)/Math.Pow(v.Length,3));
    }
    private static void Validate(IReadOnlyList<CurvePoint> c)
    {if(c==null||c.Count<2||c.Count>6)throw new ArgumentException("Bezier degree must be 1..5");foreach(var p in c)Point(p);}
    private static CurvePoint Value(IReadOnlyList<CurvePoint> c,double t)
    {var p=new CurvePoint[c.Count];for(int i=0;i<p.Length;i++)p[i]=c[i];for(int n=p.Length-1;n>0;n--)for(int i=0;i<n;i++)p[i]=p[i]*(1-t)+p[i+1]*t;return p[0];}
    private static CurvePoint[] Derivative(IReadOnlyList<CurvePoint> c)
    {var d=new CurvePoint[c.Count-1];for(int i=0;i<d.Length;i++)d[i]=(c[i+1]-c[i])*(c.Count-1);return d;}
    public static CurveLengthBounds Measure(IReadOnlyList<CurvePoint> controls,double error=0.01,int maxDepth=16)
    {
        Validate(controls);if(!Finite(error)||error<=0||error>1||maxDepth<0||maxDepth>18)throw new ArgumentException("Invalid length budget");
        double lower=0,upper=0;bool resolved=true;int leaves=0;
        void Visit(IReadOnlyList<CurvePoint> c,double budget,int depth){
            double chord=(c[c.Count-1]-c[0]).Length,poly=0;for(int i=1;i<c.Count;i++)poly+=(c[i]-c[i-1]).Length;
            if(poly-chord<=budget||depth==maxDepth||leaves>=4096){lower+=chord;upper+=poly;resolved&=poly-chord<=budget;leaves++;return;}
            int n=c.Count;var q=new CurvePoint[n];var l=new CurvePoint[n];var r=new CurvePoint[n];for(int i=0;i<n;i++)q[i]=c[i];l[0]=q[0];r[n-1]=q[n-1];
            for(int k=n-1;k>0;k--){for(int i=0;i<k;i++)q[i]=(q[i]+q[i+1])*.5;l[n-k]=q[0];r[k-1]=q[k-1];}Visit(l,budget/2,depth+1);Visit(r,budget/2,depth+1);
        }
        Visit(controls,error,0);return new CurveLengthBounds(lower,upper,resolved&&upper-lower<=error*(1+1e-8));
    }
    // Conservative inverse station bracket. Never substitutes t=s/estimatedLength.
    public static CurveStationBracket LocateStation(IReadOnlyList<CurvePoint> controls,double station,double spatialError=.01,int iterations=40)
    {
        Validate(controls);
        if(!Finite(station)||station<0||!Finite(spatialError)||spatialError<=0||spatialError>1||iterations<0||iterations>60)throw new ArgumentException("Invalid station budget");
        var total=Measure(controls,spatialError/16);
        if(total.Upper<=1e-8||station>total.Upper)throw new ArgumentException("Station outside curve");
        if(station==0)return new CurveStationBracket(0,0,true);
        if(station>total.Lower)return new CurveStationBracket(0,1,false);
        double lo=0,hi=1,maxSpeed=0;foreach(var v in Derivative(controls))maxSpeed=Math.Max(maxSpeed,v.Length);
        for(int step=0;step<iterations;step++){
            if((hi-lo)*maxSpeed<=spatialError)return new CurveStationBracket(lo,hi,true);
            double t=(lo+hi)/2;int n=controls.Count;var q=new CurvePoint[n];var left=new CurvePoint[n];
            for(int j=0;j<n;j++)q[j]=controls[j];left[0]=q[0];
            for(int k=n-1;k>0;k--){for(int j=0;j<k;j++)q[j]=q[j]*(1-t)+q[j+1]*t;left[n-k]=q[0];}
            var prefix=Measure(left,spatialError/32);
            if(station<prefix.Lower)hi=t;
            else if(station>prefix.Upper)lo=t;
            else if(prefix.Lower==prefix.Upper&&station==prefix.Lower)return new CurveStationBracket(t,t,true);
            else return new CurveStationBracket(lo,hi,false);
        }
        return new CurveStationBracket(lo,hi,(hi-lo)*maxSpeed<=spatialError);
    }
    // Quintic Hermite with zero tangential acceleration, signed endpoint curvature.
    public static CurveGeometrySegment Hermite(CurvePoint a,CurvePoint b,CurvePoint da,CurvePoint db,double ka,double kb,double speed)
    {
        Point(a);Point(b);Point(da);Point(db);
        if(Math.Abs(da.Length-1)>1e-8||Math.Abs(db.Length-1)>1e-8||!Finite(ka)||!Finite(kb)||Math.Abs(ka)>1||Math.Abs(kb)>1||!Finite(speed)||speed<=1e-6||speed>10000)throw new ArgumentException("Invalid Hermite jets");
        var v=da*speed;var w=db*speed;var aa=new CurvePoint(-da.Y,da.X)*(ka*speed*speed);var ab=new CurvePoint(-db.Y,db.X)*(kb*speed*speed);
        var c=new[]{a,a+v*.2,a+v*.4+aa*.05,b-w*.4+ab*.05,b-w*.2,b};Validate(c);return new CurveGeometrySegment(CurveSegmentKind.Bezier,c);
    }
}

public readonly struct GradeEnvelope
{
    public GradeEnvelope(double maximumAbsoluteGrade,bool resolved,bool withinLimit)
    { MaximumAbsoluteGrade=maximumAbsoluteGrade; Resolved=resolved; WithinLimit=withinLimit; }
    public double MaximumAbsoluteGrade { get; }
    public bool Resolved { get; }
    public bool WithinLimit { get; }
    public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}
public static class CurveVerticalFunctions
{
    // s/L is horizontal arc station, never a Bezier parameter. No clearance assertion.
    public static double Height(double z0,double z1,double g0,double g1,double length,double station)
    {
        Validate(z0,z1,g0,g1,length);if(!CurveFunctions.Finite(station)||station<0||station>length)throw new ArgumentException("Invalid horizontal station");
        double t=station/length;return (2*t*t*t-3*t*t+1)*z0+(t*t*t-2*t*t+t)*length*g0+(-2*t*t*t+3*t*t)*z1+(t*t*t-t*t)*length*g1;
    }
    public static double Grade(double z0,double z1,double g0,double g1,double length,double station)
    {
        Validate(z0,z1,g0,g1,length);if(!CurveFunctions.Finite(station)||station<0||station>length)throw new ArgumentException("Invalid horizontal station");
        double t=station/length,d=(z1-z0)/length;
        return (-6*d+3*g0+3*g1)*t*t+(6*d-4*g0-2*g1)*t+g0;
    }
    private static void Validate(double z0,double z1,double g0,double g1,double length)
    {if(!CurveFunctions.Finite(z0)||!CurveFunctions.Finite(z1)||Math.Abs(z0)>10000||Math.Abs(z1)>10000||!CurveFunctions.Finite(g0)||!CurveFunctions.Finite(g1)||Math.Abs(g0)>1||Math.Abs(g1)>1||!CurveFunctions.Finite(length)||length<=1e-6||length>1e6)throw new ArgumentException("Invalid profile domain");}
    public static GradeEnvelope Inspect(double z0,double z1,double g0,double g1,CurveLengthBounds length,double limit)
    {
        Validate(z0,z1,g0,g1,length.Lower);Validate(z0,z1,g0,g1,length.Upper);
        if(length.Upper<length.Lower||!CurveFunctions.Finite(limit)||limit<=0||limit>1)throw new ArgumentException("Invalid interval or grade limit");
        double peak=0;
        foreach(double l in new[]{length.Lower,length.Upper}){
            double d=(z1-z0)/l,a=-6*d+3*g0+3*g1,b=6*d-4*g0-2*g1;
            peak=Math.Max(peak,Math.Max(Math.Abs(g0),Math.Abs(g1)));
            if(Math.Abs(a)>1e-15){double t=-b/(2*a);if(t>0&&t<1)peak=Math.Max(peak,Math.Abs(a*t*t+b*t+g0));}
        }
        // At fixed t grade is affine in 1/L: endpoint lengths bound every possible length.
        return new GradeEnvelope(peak,length.Resolved,length.Resolved&&peak<=limit-1e-10);
    }
}
