using System;using System.Collections.Generic;using InterchangeBuilder.LaneConnections;
public static class SpatialChecks{
public static List<object> Run(){var rows=new List<object>();void Check(string name,bool value){rows.Add(new{name,expected=true,actual=value});if(!value)throw new Exception(name);}
var lower=SpatialEnvelopeProof.Straight("lower",new CurvePoint(-100,0),new CurvePoint(100,0),8,0,2);
var high=SpatialEnvelopeProof.Straight("upper",new CurvePoint(0,-100),new CurvePoint(0,100),8,12,2);
Check("full width underside clearance10",SpatialEnvelopeProof.Compare(lower,high,9).Status==SpatialProofStatus.Separated);
Check("clearance equality unknown",SpatialEnvelopeProof.Compare(lower,high,10).Status==SpatialProofStatus.Unknown);
var thick=SpatialEnvelopeProof.Straight("upper",new CurvePoint(0,-100),new CurvePoint(0,100),8,12,13);
Check("increasing thickness actual overlap",SpatialEnvelopeProof.Compare(lower,thick,0).Status==SpatialProofStatus.OverlapWitness);
var uncertain=new SpatialEnvelope("curve",-10,10,-10,10,-1,1);
Check("outer box overlap never geometry proof",SpatialEnvelopeProof.Compare(lower,uncertain,0).Status==SpatialProofStatus.Unknown);
var dir=new CurvePoint(1,0);var request=new CurveKernelRequest(new CurvePort("a",new CurvePoint(0,0),dir,0,0,8,CurveTravelSide.Middle),new CurvePort("b",new CurvePoint(200,0),dir,0,0,8,CurveTravelSide.Middle),40,8,true,1);
var route=new CurveGeometryDocument(request,8,new[]{new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(0,0),new CurvePoint(160,0)}),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(160,0),new CurvePoint(200,0)})});
Check("entire straight route exact split proof",SpatialEnvelopeProof.EntireSingleStraight(route,12,2).Status==SpatialProofStatus.Separated);
var candidate=ExperimentalCurveKernel.Generate(request,true);var c=candidate.Candidates[1];var bezier=new CurveGeometryDocument(request,8,new[]{new CurveGeometrySegment(CurveSegmentKind.Bezier,c.Controls),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{c.TailStart,c.TailEnd})});
Check("unproved curve global domain never verified",SpatialEnvelopeProof.EntireSingleStraight(bezier,12,2).Status==SpatialProofStatus.Unknown);
Check("tiny thickness cannot certify collapsed prism",SpatialEnvelopeProof.EntireSingleStraight(route,10000,1e-14).Status==SpatialProofStatus.InvalidInput);
Check("no placement permission",!SpatialEnvelopeProof.Compare(lower,high,9).CanApplyToGame);var low=new QuarterArcCandidate(50,8,40,0,0,0,2,1);var highArc=new QuarterArcCandidate(50,8,40,12,12,12,2,1);
Check("nonstraight whole arc tail domain certified",QuarterArcSpatialProof.Verify(low,1,.1).Status==SpatialProofStatus.Separated);
Check("curved full width crossing separation",QuarterArcSpatialProof.Compare(low,1,highArc,1,.1,9).Status==SpatialProofStatus.Separated);
var lowArc=new QuarterArcCandidate(50,8,40,1,1,1,2,1);
Check("curved actual overlap witness",QuarterArcSpatialProof.Compare(low,1,lowArc,1,.1,0).Status==SpatialProofStatus.OverlapWitness);
Check("arc straight strict G2 not silently passed",QuarterArcSpatialProof.Verify(low,1,.1,true).Status==SpatialProofStatus.Unknown);
Check("same segment radial fold unknown",QuarterArcSpatialProof.Verify(new QuarterArcCandidate(3,8,40,0,0,0,2,1),1,.1).Status==SpatialProofStatus.Unknown);
Check("stale exact candidate rejected",QuarterArcSpatialProof.Verify(low,2,.1).Status==SpatialProofStatus.InvalidInput);
Check("actual candidate selection rejects colliding then chooses high",QuarterArcSpatialProof.Select(new[]{lowArc,highArc},new[]{low},1,.1,9)==1);
Check("no collision cannot bypass proof",QuarterArcSpatialProof.Select(new[]{lowArc},new[]{low},1,.1,0,mode:CurvePlacementMode.NoCollision)==-1);
double arcL=50*Math.PI/2;var slope=new QuarterArcCandidate(50,8,40,0,arcL*.05,arcL*.05+2,2,1);
Check("curved station linear profile exact grade",Math.Abs(QuarterArcSpatialProof.Verify(slope,1,.08).MaximumGrade-(.05*50/46))<1e-12&&QuarterArcSpatialProof.Verify(slope,1,.08).Status==SpatialProofStatus.Unknown);
Check("vertical seam mismatch unknown",QuarterArcSpatialProof.Verify(new QuarterArcCandidate(50,8,40,0,5,5,2,1),1,.1).Status==SpatialProofStatus.Unknown);
var arcRequest=new CurveKernelRequest(new CurvePort("arc-start",new CurvePoint(50,0),new CurvePoint(0,1),.02,0,8,CurveTravelSide.Middle),new CurvePort("tail-end",new CurvePoint(-40,50),new CurvePoint(-1,0),0,0,8,CurveTravelSide.Middle),40,8,false,1);
Check("selected ports and actual arc candidate integrated",QuarterArcSpatialProof.SelectForRequest(arcRequest,new[]{lowArc,highArc},new[]{low},.1,9)==1);
Check("width changed cannot reuse candidate",QuarterArcSpatialProof.SelectForRequest(new CurveKernelRequest(arcRequest.Start,arcRequest.End,40,9,false,1),new[]{highArc},new[]{low},.1,9)==-1);
Check("folded Bezier whole domain remains unknown",SpatialEnvelopeProof.EntireSingleStraight(new CurveGeometryDocument(request,8,new[]{new CurveGeometrySegment(CurveSegmentKind.Bezier,new[]{new CurvePoint(0,0),new CurvePoint(200,100),new CurvePoint(-100,100),new CurvePoint(200,0)})}),0,2).Status!=SpatialProofStatus.Separated);
Check("invalid crossing clearance preserved",QuarterArcSpatialProof.Compare(low,1,lowArc,1,.1,double.NaN).Status==SpatialProofStatus.InvalidInput);
Check("underflow grade cannot certify nonconstant surface",QuarterArcSpatialProof.Verify(new QuarterArcCandidate(50,8,40,0,double.Epsilon,double.Epsilon,2,1),1,.1).Status==SpatialProofStatus.Unknown);
return rows;}}
