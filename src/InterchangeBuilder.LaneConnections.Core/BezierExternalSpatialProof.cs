using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace InterchangeBuilder.LaneConnections;

public sealed class ExternalSpatialCertificate
{
    internal ExternalSpatialCertificate(SpatialProofStatus status,List<SpatialEnvelope> leaves,int pairs,int separated,double stationLower,double stationUpper,string reason)
    {Status=status;Leaves=leaves.AsReadOnly();Pairs=pairs;SeparatedPairs=separated;StationLower=stationLower;StationUpper=stationUpper;Reason=reason;}
    public SpatialProofStatus Status{get;} public ReadOnlyCollection<SpatialEnvelope> Leaves{get;} public int Pairs{get;} public int SeparatedPairs{get;} public double StationLower{get;} public double StationUpper{get;} public string Reason{get;}
    public CurveGeometryDocument? SourceGeometry { get; internal set; }
    public ReadOnlyCollection<SpatialEnvelope>? Obstacles { get; internal set; }
    public int Depth { get; internal set; }
    public int ExpectedLeaves { get; internal set; }
    public double Datum { get; internal set; }
    public double Grade { get; internal set; }
    public double Thickness { get; internal set; }
    public double Clearance { get; internal set; }
    public SpatialProofStatus GlobalSelfStatus=>SpatialProofStatus.Unknown;
    public bool CanApplyToGame=>false;
}
public static class BezierExternalSpatialProof
{
    // Model: fixed-width horizontal disk sweep, vertical interval [z(s)-thickness,z(s)].
    // z(s)=datum+grade*s uses true horizontal arc station, never Bezier t.
    // It encloses a normal-width ribbon; not a certified regular road surface or vehicle mesh.
    public static ExternalSpatialCertificate Verify(CurveKernelRequest request,CurveGeometryDocument geometry,SpatialEnvelope[] obstacles,double datum,double grade,double thickness,double clearance,int depth=8,int maximumLeaves=4096)
    {
        var leaves=new List<SpatialEnvelope>();int pairs=0,separated=0;double lower=0,upper=0;bool unknown=false;
        ExternalSpatialCertificate Done(SpatialProofStatus status,string reason)=>new ExternalSpatialCertificate(status,leaves,pairs,separated,lower,upper,reason){SourceGeometry=geometry,Obstacles=obstacles==null?null:Array.AsReadOnly((SpatialEnvelope[])obstacles.Clone()),Depth=depth,ExpectedLeaves=geometry==null||depth<0||depth>12?0:geometry.Segments.Count*(1<<depth),Datum=datum,Grade=grade,Thickness=thickness,Clearance=clearance};
        if(obstacles==null||obstacles.Length>32||obstacles.Any(o=>o==null||!o.Valid)||!CurveFunctions.Finite(datum)||Math.Abs(datum)>1000||!CurveFunctions.Finite(grade)||Math.Abs(grade)>1||!CurveFunctions.Finite(thickness)||thickness<=0||thickness>100||!CurveFunctions.Finite(clearance)||clearance<0||clearance>100||depth<0||depth>12||maximumLeaves<1||maximumLeaves>4096)return Done(SpatialProofStatus.InvalidInput,"INVALID_MODEL_OR_BUDGET");
        var local=CurveGeometryVerifier.Verify(request,geometry);
        if(local.Status==CurveGeometryStatus.InvalidInput)return Done(SpatialProofStatus.InvalidInput,"INVALID_ACTUAL_GEOMETRY");
        if(local.Status!=CurveGeometryStatus.VerifiedLocalPlanar)return Done(SpatialProofStatus.Unknown,"LOCAL_GEOMETRY_NOT_CERTIFIED");
        void Visit(CurvePoint[] controls,int level){
            if(leaves.Count>=maximumLeaves){unknown=true;return;}
            if(level>0){int n=controls.Length;var q=(CurvePoint[])controls.Clone();var left=new CurvePoint[n];var right=new CurvePoint[n];left[0]=q[0];right[n-1]=q[n-1];for(int k=n-1;k>0;k--){for(int i=0;i<k;i++)q[i]=(q[i]+q[i+1])*.5;left[n-k]=q[0];right[k-1]=q[k-1];}Visit(left,level-1);Visit(right,level-1);return;}
            // Chord/control polygon bound exact arc length independently of parameterization.
            double chord=(controls.Last()-controls[0]).Length,polygon=0;for(int i=1;i<controls.Length;i++)polygon+=(controls[i]-controls[i-1]).Length;
            chord=Math.Max(0,chord-1e-7);polygon+=1e-7;
            double z0=datum+grade*lower,z1=datum+grade*(upper+polygon);double zMin=Math.Min(z0,z1)-thickness-1e-7,zMax=Math.Max(z0,z1)+1e-7;
            double padding=request.Width/2+1e-7;var envelope=new SpatialEnvelope("curve-leaf-"+leaves.Count,controls.Min(p=>p.X)-padding,controls.Max(p=>p.X)+padding,controls.Min(p=>p.Y)-padding,controls.Max(p=>p.Y)+padding,zMin,zMax);
            if(!envelope.Valid){unknown=true;return;}leaves.Add(envelope);lower+=chord;upper+=polygon;
        }
        foreach(var segment in geometry.Segments)Visit(segment!.Controls.ToArray(),depth);
        foreach(var leaf in leaves)foreach(var obstacle in obstacles){pairs++;var proof=SpatialEnvelopeProof.Compare(leaf,obstacle,clearance);if(proof.Status==SpatialProofStatus.Separated)separated++;else unknown=true;}
        // Uncovered leaves cannot be hidden by a successful prefix.
        return Done(unknown?SpatialProofStatus.Unknown:SpatialProofStatus.Separated,unknown?"UNCOVERED_OR_UNSEPARATED_DOMAIN":"ALL_EXTERNAL_LEAF_OBSTACLE_PAIRS_SEPARATED");
    }
}
