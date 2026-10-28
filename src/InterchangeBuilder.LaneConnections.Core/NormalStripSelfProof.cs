using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace InterchangeBuilder.LaneConnections;

public sealed class NormalStripCertificate
{
    internal NormalStripCertificate(SpatialProofStatus status,string reason,double cone,double curvature,double margin,int pieces)
    {Status=status;Reason=reason;ConeAngle=cone;CurvatureUpper=curvature;InjectionMargin=margin;Pieces=pieces;}
    public SpatialProofStatus Status{get;} public string Reason{get;} public double ConeAngle{get;} public double CurvatureUpper{get;} public double InjectionMargin{get;} public int Pieces{get;}
    public CurveGeometryDocument? SourceGeometry {get;internal set;}
    public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}
public static class NormalStripSelfProof
{
    // Entire parameter square proof for a fixed-width normal strip F(s,u)=C(s)+u*N(s).
    // If all tangents occupy an angular interval beta<pi/2 and r*K<cos(beta), F is injective.
    // Exact dyadic orientation: do not turn a rounded zero cross product into a seam proof.
    private static BigInteger Dyadic(double x){long bits=BitConverter.DoubleToInt64Bits(x);int exponent=(int)((bits>>52)&2047);long mantissa=bits&0xfffffffffffffL;if(exponent!=0)mantissa|=1L<<52;var value=new BigInteger(mantissa);if(bits<0)value= -value;return value<<(exponent==0?0:exponent-1);}
    private static bool ExactSeam(CurveGeometrySegment a,CurveGeometrySegment b){
        var p=a.Controls[a.Controls.Count-1];var q=b.Controls[0];if(p.X!=q.X||p.Y!=q.Y)return false;
        var previous=a.Controls[a.Controls.Count-2];var next=b.Controls[1];
        var ax=Dyadic(p.X)-Dyadic(previous.X);var ay=Dyadic(p.Y)-Dyadic(previous.Y);var bx=Dyadic(next.X)-Dyadic(q.X);var by=Dyadic(next.Y)-Dyadic(q.Y);
        return ax*by-ay*bx==BigInteger.Zero&&ax*bx+ay*by>BigInteger.Zero;
    }
    public static NormalStripCertificate Verify(CurveKernelRequest request,CurveGeometryDocument geometry)
    {
        NormalStripCertificate Result(SpatialProofStatus s,string reason,double cone=0,double curvature=0,double margin=0)=>new NormalStripCertificate(s,reason,cone,curvature,margin,geometry?.Segments.Count??0){SourceGeometry=geometry};
        var local=CurveGeometryVerifier.Verify(request,geometry);
        if(local.Status==CurveGeometryStatus.InvalidInput)return Result(SpatialProofStatus.InvalidInput,"INVALID_ACTUAL_REQUEST");
        if(local.Status!=CurveGeometryStatus.VerifiedLocalPlanar)return Result(SpatialProofStatus.Unknown,"LOCAL_GEOMETRY_UNRESOLVED");
        for(int i=1;i<geometry.Segments.Count;i++)if(!ExactSeam(geometry.Segments[i-1]!,geometry.Segments[i]!))return Result(SpatialProofStatus.Unknown,"EXACT_G0_G1_SEAM_NOT_PROVED");
        var direction=request.Start.TravelDirection;var axis=direction*(1/direction.Length);double low=double.PositiveInfinity,high=double.NegativeInfinity,maximumK=0;
        foreach(var segment in geometry.Segments){
            var c=segment!.Controls;int n=c.Count-1;var d=new CurvePoint[n];for(int i=0;i<n;i++)d[i]=(c[i+1]-c[i])*n;
            double minimumProjection=double.PositiveInfinity;
            foreach(var v in d){double x=v.X*axis.X+v.Y*axis.Y,y=axis.X*v.Y-axis.Y*v.X;if(x<=1e-7)return Result(SpatialProofStatus.Unknown,"TANGENT_CONE_NOT_FORWARD");minimumProjection=Math.Min(minimumProjection,x-1e-7);double angle=Math.Atan2(y,x),angleGuard=2e-7/x;low=Math.Min(low,angle-angleGuard);high=Math.Max(high,angle+angleGuard);}
            var dd=new CurvePoint[Math.Max(0,n-1)];for(int i=0;i<dd.Length;i++)dd[i]=(d[i+1]-d[i])*(n-1);
            double cross=0;foreach(var v in d)foreach(var a in dd)cross=Math.Max(cross,Math.Abs(v.X*a.Y-v.Y*a.X)+1e-7*(v.Length+a.Length+1));
            maximumK=Math.Max(maximumK,cross/Math.Pow(minimumProjection,3));
        }
        double beta=high-low+1e-8,k=maximumK+1e-8,margin=Math.Cos(beta)-request.Width/2*k;
        if(!CurveFunctions.Finite(beta)||!CurveFunctions.Finite(k)||beta>=Math.PI/2||margin<=1e-7)return Result(SpatialProofStatus.Unknown,"GLOBAL_CONE_CURVATURE_CONDITION_UNPROVED",beta,k,margin);
        return Result(SpatialProofStatus.Separated,"ENTIRE_NORMAL_STRIP_INJECTIVE",beta,k,margin);
    }
}
