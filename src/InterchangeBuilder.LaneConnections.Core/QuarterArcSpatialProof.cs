using System;
namespace InterchangeBuilder.LaneConnections;

// Exact restricted candidate: counter-clockwise quarter annular strip, then westbound tail.
// Cross-sections are radial on the arc, perpendicular on the tail. Height is linear in horizontal station.
public sealed class QuarterArcCandidate
{
    public QuarterArcCandidate(double radius,double width,double tail,double startHeight,double joinHeight,double endHeight,double thickness,long revision)
    {Radius=radius;Width=width;Tail=tail;StartHeight=startHeight;JoinHeight=joinHeight;EndHeight=endHeight;Thickness=thickness;Revision=revision;}
    public double Radius{get;} public double Width{get;} public double Tail{get;} public double StartHeight{get;} public double JoinHeight{get;} public double EndHeight{get;} public double Thickness{get;} public long Revision{get;}
    public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}
public sealed class QuarterArcCertificate
{
    internal QuarterArcCertificate(SpatialProofStatus status,string reason,double grade,SpatialEnvelope? envelope){Status=status;Reason=reason;MaximumGrade=grade;Envelope=envelope;}
    public SpatialProofStatus Status{get;} public string Reason{get;} public double MaximumGrade{get;} public SpatialEnvelope? Envelope{get;} public bool CanApplyToGame=>false;
    public NumericalEvidenceKind NumericalEvidence => NumericalEvidenceKind.EngineeringChecksOnly;
    public string NumericalEvidenceLabel => "EngineeringChecksOnly";
    public bool IsMachineCertified => false;
    public string NumericalLimitation => NumericalAssurance.Note;
}
public static class QuarterArcSpatialProof
{
    public static QuarterArcCertificate Verify(QuarterArcCandidate? candidate,long currentRevision,double maximumGrade,bool requireG2=false)
    {
        QuarterArcCertificate Result(SpatialProofStatus s,string reason,double g=0,SpatialEnvelope? e=null)=>new QuarterArcCertificate(s,reason,g,e);
        if(candidate==null)return Result(SpatialProofStatus.InvalidInput,"MISSING_CANDIDATE");var c=candidate;
        foreach(double x in new[]{c.Radius,c.Width,c.Tail,c.StartHeight,c.JoinHeight,c.EndHeight,c.Thickness,maximumGrade})if(!CurveFunctions.Finite(x)||Math.Abs(x)>10000)return Result(SpatialProofStatus.InvalidInput,"NUMERIC_DOMAIN");
        if(c.Revision!=currentRevision||c.Revision<0||c.Radius<=0||c.Width<=0||c.Tail<=0||c.Thickness<=0||maximumGrade<=0)return Result(SpatialProofStatus.InvalidInput,"STALE_OR_DEGENERATE");
        foreach(double z in new[]{c.StartHeight,c.JoinHeight,c.EndHeight})if(z-c.Thickness>=z)return Result(SpatialProofStatus.InvalidInput,"COLLAPSED_THICKNESS");
        if(c.Radius<=c.Width/2+1e-8)return Result(SpatialProofStatus.Unknown,"RADIAL_MAP_NOT_INJECTIVE");
        if(requireG2)return Result(SpatialProofStatus.Unknown,"ARC_LINE_CURVATURE_JUMP");
        double arcLength=c.Radius*Math.PI/2,arcGrade=(c.JoinHeight-c.StartHeight)/arcLength,tailGrade=(c.EndHeight-c.JoinHeight)/c.Tail,peak=Math.Max(Math.Abs(arcGrade)*c.Radius/(c.Radius-c.Width/2),Math.Abs(tailGrade));
        if(Math.Abs(arcGrade-tailGrade)>1e-10)return Result(SpatialProofStatus.Unknown,"VERTICAL_TANGENT_JUMP",peak);
        if(c.StartHeight!=c.JoinHeight||c.JoinHeight!=c.EndHeight)return Result(SpatialProofStatus.Unknown,"FULL_WIDTH_VERTICAL_SEAM_NOT_G1",peak);
        if(peak>maximumGrade-1e-10)return Result(SpatialProofStatus.Unknown,"GRADE_LIMIT_OR_CONTACT",peak);
        double min=Math.Min(c.StartHeight,Math.Min(c.JoinHeight,c.EndHeight))-c.Thickness,max=Math.Max(c.StartHeight,Math.Max(c.JoinHeight,c.EndHeight));
        var box=new SpatialEnvelope("quarter-arc-plus-tail",-c.Tail,c.Radius+c.Width/2,0,c.Radius+c.Width/2,min,max);
        if(!box.Valid)return Result(SpatialProofStatus.InvalidInput,"COLLAPSED_ENVELOPE",peak);
        // Polar map is injective for positive radial coordinate and angle[0,pi/2].
        // Arc interior x>0, tail interior x<0; they share exactly the radial endpoint cross-section x=0.
        // Height/thickness cannot create new XY intersections. This covers all station pairs, including the seam.
        return Result(SpatialProofStatus.Separated,"ENTIRE_POLAR_STRIP_AND_TAIL_INJECTIVE_G1",peak,box);
    }
    // Actual exact-arc candidates are selected only after whole-route and all obstacle checks.
    // No conversion to the existing quintic/game representation is implied.
    public static int Select(QuarterArcCandidate[] candidates,QuarterArcCandidate[] obstacles,long revision,double gradeLimit,double clearance,bool requireG2=false,CurvePlacementMode mode=CurvePlacementMode.Normal)
    {
        if(candidates==null||obstacles==null||(mode!=CurvePlacementMode.Normal&&mode!=CurvePlacementMode.NoCollision)||!CurveFunctions.Finite(clearance)||clearance<0||clearance>100)return -1;
        for(int i=0;i<candidates.Length;i++){
            if(Verify(candidates[i],revision,gradeLimit,requireG2).Status!=SpatialProofStatus.Separated)continue;
            bool separated=true;foreach(var other in obstacles)if(other==null||Compare(candidates[i],revision,other,revision,gradeLimit,clearance).Status!=SpatialProofStatus.Separated){separated=false;break;}
            if(separated)return i;
        }
        return -1;
    }
    public static int SelectForRequest(CurveKernelRequest? request,QuarterArcCandidate[] candidates,QuarterArcCandidate[] obstacles,double gradeLimit,double clearance)
    {
        if(request==null||candidates==null||obstacles==null||request.Start==null||request.End==null)return -1;
        for(int i=0;i<candidates.Length;i++){
            var c=candidates[i];if(c==null)continue;
            var a=request.Start;var b=request.End;
            if(a.Offset!=0||b.Offset!=0||a.Side!=CurveTravelSide.Middle||b.Side!=CurveTravelSide.Middle||a.Width!=c.Width||b.Width!=c.Width||request.Width!=c.Width||request.TailLength!=c.Tail||string.IsNullOrWhiteSpace(a.Key)||string.IsNullOrWhiteSpace(b.Key))continue;
            if(a.Position.X!=c.Radius||a.Position.Y!=0||a.TravelDirection.X!=0||a.TravelDirection.Y!=1||a.Curvature!=1/c.Radius||b.Position.X!= -c.Tail||b.Position.Y!=c.Radius||b.TravelDirection.X!= -1||b.TravelDirection.Y!=0||b.Curvature!=0)continue;
            if(Select(new[]{c},obstacles,request.Revision,gradeLimit,clearance,request.RequireG2,request.Mode)==0)return i;
        }
        return -1;
    }
    public static SpatialProof Compare(QuarterArcCandidate a,long revisionA,QuarterArcCandidate b,long revisionB,double gradeLimit,double clearance)
    {
        var first=Verify(a,revisionA,gradeLimit);var second=Verify(b,revisionB,gradeLimit);
        if(first.Status!=SpatialProofStatus.Separated||second.Status!=SpatialProofStatus.Separated)return new SpatialProof(SpatialProofStatus.Unknown,0,"CANDIDATE_NOT_CERTIFIED");
        var proof=SpatialEnvelopeProof.Compare(first.Envelope,second.Envelope,clearance);
        if(proof.Status==SpatialProofStatus.Separated||proof.Status==SpatialProofStatus.InvalidInput)return proof;
        // Same XY path family: at a common interior station, vertical interval overlap is a real witness.
        foreach(double fraction in new[]{0.25,0.5,0.75}){
            double za=a.StartHeight+(a.JoinHeight-a.StartHeight)*fraction,zb=b.StartHeight+(b.JoinHeight-b.StartHeight)*fraction;
            if(Math.Abs(a.Radius-b.Radius)<(a.Width+b.Width)/2-1e-8&&Math.Max(za-a.Thickness,zb-b.Thickness)<Math.Min(za,zb)-1e-8)return new SpatialProof(SpatialProofStatus.OverlapWitness,0,"INTERIOR_RADIAL_AND_VERTICAL_WITNESS");
        }
        return new SpatialProof(SpatialProofStatus.Unknown,0,"ENCLOSURE_NOT_SEPARATED");
    }
}
