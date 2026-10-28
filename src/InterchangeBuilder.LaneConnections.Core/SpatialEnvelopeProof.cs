using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace InterchangeBuilder.LaneConnections;

public enum SpatialProofStatus { InvalidInput, OverlapWitness, Separated, Unknown }
public sealed class SpatialEnvelope
{
    // Certified enclosing AABB supplied by a geometry producer. Not a game mesh.
    public SpatialEnvelope(string owner,double x0,double x1,double y0,double y1,double z0,double z1):this(owner,x0,x1,y0,y1,z0,z1,false){}
    internal SpatialEnvelope(string owner,double x0,double x1,double y0,double y1,double z0,double z1,bool exactBox)
    {Owner=owner;X0=x0;X1=x1;Y0=y0;Y1=y1;Z0=z0;Z1=z1;ExactBox=exactBox;}
    public string Owner{get;} public double X0{get;} public double X1{get;} public double Y0{get;} public double Y1{get;} public double Z0{get;} public double Z1{get;} public bool ExactBox{get;}
    internal bool Valid=>!string.IsNullOrWhiteSpace(Owner)&&new[]{X0,X1,Y0,Y1,Z0,Z1}.All(v=>CurveFunctions.Finite(v)&&Math.Abs(v)<=20000)&&X0<X1&&Y0<Y1&&Z0<Z1;
}
public sealed class SpatialProof
{
    internal SpatialProof(SpatialProofStatus status,double separation,string reason){Status=status;SeparationLowerBound=separation;Reason=reason;}
    public SpatialProofStatus Status{get;} public double SeparationLowerBound{get;} public string Reason{get;} public bool CanApplyToGame=>false;
}
public static class SpatialEnvelopeProof
{
    // Width and underside thickness included in bounds; no sparse sampling.
    // Only exact horizontal axis-aligned straight road prisms are certified by this producer.
    public static SpatialEnvelope Straight(string owner,CurvePoint start,CurvePoint end,double width,double surfaceHeight,double thickness)
    {
        CurveFunctions.Point(start);CurveFunctions.Point(end);
        if(!CurveFunctions.Finite(width)||width<=0||width>50||!CurveFunctions.Finite(surfaceHeight)||Math.Abs(surfaceHeight)>10000||!CurveFunctions.Finite(thickness)||thickness<=0||thickness>100||((start.X==end.X)==(start.Y==end.Y)))throw new ArgumentException("Only nonzero horizontal axis-aligned straight prisms supported");
        bool alongX=start.Y==end.Y;
        var envelope=new SpatialEnvelope(owner,Math.Min(start.X,end.X)-(alongX?0:width/2),Math.Max(start.X,end.X)+(alongX?0:width/2),Math.Min(start.Y,end.Y)-(alongX?width/2:0),Math.Max(start.Y,end.Y)+(alongX?width/2:0),surfaceHeight-thickness,surfaceHeight,true);
        if(!envelope.Valid)throw new ArgumentException("Derived prism collapsed or outside domain");return envelope;
    }
    public static SpatialProof Compare(SpatialEnvelope? a,SpatialEnvelope? b,double clearance)
    {
        if(a==null||b==null||!a.Valid||!b.Valid||!CurveFunctions.Finite(clearance)||clearance<0||clearance>100)return new SpatialProof(SpatialProofStatus.InvalidInput,0,"INVALID_DOMAIN");
        double dx=Math.Max(a.X0-b.X1,b.X0-a.X1),dy=Math.Max(a.Y0-b.Y1,b.Y0-a.Y1),dz=Math.Max(a.Z0-b.Z1,b.Z0-a.Z1);
        double gap=Math.Max(dx,Math.Max(dy,dz));
        if(gap>clearance+1e-8)return new SpatialProof(SpatialProofStatus.Separated,gap,"CONTINUOUS_ENCLOSURE_SEPARATION");
        // Overlapping outer enclosures alone never prove actual geometry overlap.
        if(a.ExactBox&&b.ExactBox&&dx< -1e-8&&dy< -1e-8&&dz< -1e-8)return new SpatialProof(SpatialProofStatus.OverlapWitness,0,"EXACT_PRISM_INTERIOR_OVERLAP");
        return new SpatialProof(SpatialProofStatus.Unknown,Math.Max(0,gap),"CONTACT_OR_UNRESOLVED_ENCLOSURES");
    }
    public static SpatialProof EntireSingleStraight(CurveGeometryDocument? route,double surfaceHeight,double thickness)
    {
        if(route==null||route.Segments.Count==0)return new SpatialProof(SpatialProofStatus.InvalidInput,0,"MISSING_ROUTE");
        var request=route.CapturedRequest;var local=CurveGeometryVerifier.Verify(request,route);
        if(local.Status!=CurveGeometryStatus.VerifiedLocalPlanar)return new SpatialProof(SpatialProofStatus.Unknown,0,"LOCAL_GEOMETRY_NOT_VERIFIED");
        // A concatenated exact forward line has an injective rectangular parameterization.
        // No adjacency skip or sampling: all controls must lie exactly on one axis line, in travel order.
        var first=route.Segments[0]!.Controls[0];var last=route.Segments[route.Segments.Count-1]!.Controls.Last();bool x=first.Y==last.Y,y=first.X==last.X;
        if(x==y)return new SpatialProof(SpatialProofStatus.Unknown,0,"GLOBAL_CURVED_PARAMETER_DOMAIN_UNPROVED");
        double sign=Math.Sign(x?last.X-first.X:last.Y-first.Y),previous=x?first.X:first.Y;
        foreach(var part in route.Segments){if(part!.Kind!=CurveSegmentKind.Line)return new SpatialProof(SpatialProofStatus.Unknown,0,"GLOBAL_CURVE_DIAGONAL_UNPROVED");foreach(var point in part.Controls){double station=x?point.X:point.Y;if((x?point.Y!=first.Y:point.X!=first.X)||(station-previous)*sign<0)return new SpatialProof(SpatialProofStatus.Unknown,0,"NONMONOTONE_OR_NONCOLLINEAR");previous=station;}}
        try{Straight("self",first,last,route.Width,surfaceHeight,thickness);}catch(ArgumentException){return new SpatialProof(SpatialProofStatus.InvalidInput,0,"INVALID_PRISM");}
        return new SpatialProof(SpatialProofStatus.Separated,0,"ENTIRE_INJECTIVE_STRAIGHT_PRISM");
    }
}
