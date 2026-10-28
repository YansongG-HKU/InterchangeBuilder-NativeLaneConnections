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
Check("no placement permission",!SpatialEnvelopeProof.Compare(lower,high,9).CanApplyToGame);return rows;}}
