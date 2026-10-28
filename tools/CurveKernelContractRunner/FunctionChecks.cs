using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.Json;
using InterchangeBuilder.LaneConnections;

public static class FunctionChecks
{
    public static readonly List<object> Comparisons=new List<object>();
    private static double? SampledRadius(CurveGeometryDocument geometry)
    {
        double maximum=0;
        try {foreach(var segment in geometry.Segments)for(int i=0;i<=100;i++)maximum=Math.Max(maximum,Math.Abs(CurveFunctions.Bezier(segment!.Controls,i/100d).Curvature));}
        catch(ArgumentException){return null;}
        return maximum>1e-12?1/maximum:(double?)null; // Diagnostic sample, never a certified radius constraint.
    }
    public static List<object> Run(string oracleFile)
    {
        Comparisons.Clear();var records=new List<object>();
        void Check(string name,bool value){records.Add(new{name,expected=true,actual=value});if(!value)throw new Exception(name);}
        bool Near(double a,double b,double e=1e-7)=>Math.Abs(a-b)<=e;
        void Reject(string name,Action action){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(name,rejected);}
        var origin=new CurvePoint(0,0);var east=new CurvePoint(1,0);var north=new CurvePoint(0,1);
        var line=CurveFunctions.Line(origin,new CurvePoint(3,4),.5);
        Check("analytic line midpoint unit tangent",Near(line.Position.X,1.5)&&Near(line.Position.Y,2)&&Near(line.Tangent.X,.6)&&line.Curvature==0);
        foreach(int sign in new[]{-1,1}){
            var arc=CurveFunctions.Arc(origin,20,0,sign*Math.PI/2,1);
            Check("analytic quarter circle "+sign,Near(arc.Position.X,0)&&Near(arc.Position.Y,20*sign)&&Near(arc.Curvature,sign*.05)&&Near(CurveFunctions.ArcLength(20,sign*Math.PI/2),10*Math.PI));
        }
        var h=CurveFunctions.Hermite(origin,new CurvePoint(100,80),east,north,.003,-.004,100);
        Check("both signed Hermite endpoint curvatures",Near(CurveFunctions.Bezier(h.Controls,0).Curvature,.003)&&Near(CurveFunctions.Bezier(h.Controls,1).Curvature,-.004));
        // Independent cubic parabola x=t, y=t²: exact length (sqrt5/2+asinh2/4).
        var parabola=new[]{origin,new CurvePoint(1d/3,0),new CurvePoint(2d/3,1d/3),new CurvePoint(1,1)};
        double exact=Math.Sqrt(5)/2+Math.Log(2+Math.Sqrt(5))/4;
        var bounds=CurveFunctions.Measure(parabola,.0001);var jet=CurveFunctions.Bezier(parabola,.5);
        Check("independent parabola jet",Near(jet.Position.X,.5)&&Near(jet.Position.Y,.25)&&Near(jet.Curvature,2/Math.Pow(2,1.5)));
        Check("analytic arc length enclosed",bounds.Resolved&&bounds.Lower<=exact&&bounds.Upper>=exact&&bounds.Upper-bounds.Lower<=.0001);
        Check("insufficient subdivision unresolved",!CurveFunctions.Measure(parabola,.00001,0).Resolved);
        var inverse=CurveFunctions.LocateStation(new[]{origin,new CurvePoint(200,0)},100);
        Check("actual station inverse exact line",inverse.Resolved&&inverse.LowerParameter==.5&&inverse.UpperParameter==.5);
        Check("station inverse budget exhausted",!CurveFunctions.LocateStation(parabola,.7,.0001,0).Resolved);
        using(var oracle=JsonDocument.Parse(File.ReadAllText(oracleFile)))foreach(var row in oracle.RootElement.GetProperty("rows").EnumerateArray()){
            double t=row.GetProperty("t").GetDouble();var actual=CurveFunctions.Bezier(parabola,t);
            Check("independent JS polynomial oracle "+t,Near(actual.Position.X,row.GetProperty("x").GetDouble())&&Near(actual.Position.Y,row.GetProperty("y").GetDouble())&&Near(actual.Curvature,row.GetProperty("curvature").GetDouble())&&Near(CurveVerticalFunctions.Height(0,12,0,0,200,200*t),row.GetProperty("height").GetDouble())&&Near(CurveVerticalFunctions.Grade(0,12,0,0,200,200*t),row.GetProperty("grade").GetDouble()));
        }
        // Exact de Casteljau split of the quadratic parabola at one half.
        var firstHalf=new[]{origin,new CurvePoint(.25,0),new CurvePoint(.5,.25)};
        var secondHalf=new[]{new CurvePoint(.5,.25),new CurvePoint(.75,.5),new CurvePoint(1,1)};
        var l1=CurveFunctions.Measure(firstHalf,.00005);var l2=CurveFunctions.Measure(secondHalf,.00005);
        Check("split parameterization length enclosure",l1.Lower+l2.Lower<=exact&&l1.Upper+l2.Upper>=exact&&Math.Abs((l1.Lower+l2.Lower)-bounds.Lower)<.0001);
        var combined=new CurveLengthBounds(l1.Lower+l2.Lower,l1.Upper+l2.Upper,l1.Resolved&&l2.Resolved);
        Check("split parameterization grade envelope",Math.Abs(CurveVerticalFunctions.Inspect(0,.01,0,0,combined,.1).MaximumAbsoluteGrade-CurveVerticalFunctions.Inspect(0,.01,0,0,bounds,.1).MaximumAbsoluteGrade)<.00001);
        var profile=CurveVerticalFunctions.Inspect(0,12,0,0,new CurveLengthBounds(200,200,true),.08);
        Check("independent 200m 12m peak9percent",Near(profile.MaximumAbsoluteGrade,.09)&&!profile.WithinLimit);
        Check("actual horizontal station height",Near(CurveVerticalFunctions.Height(0,12,0,0,200,100),6));
        Check("datum invariance",Near(CurveVerticalFunctions.Inspect(50,62,0,0,new CurveLengthBounds(200,200,true),.1).MaximumAbsoluteGrade,.09));
        Check("reversed profile grade magnitude",Near(CurveVerticalFunctions.Inspect(12,0,0,0,new CurveLengthBounds(200,200,true),.1).MaximumAbsoluteGrade,.09));
        Check("unresolved length never allowed",!CurveVerticalFunctions.Inspect(0,1,0,0,new CurveLengthBounds(199,201,false),.1).WithinLimit);
        Reject("zero length line",()=>CurveFunctions.Line(origin,origin,0));
        Reject("nonfinite arc",()=>CurveFunctions.Arc(origin,double.NaN,0,1,0));
        Reject("zero length profile",()=>CurveVerticalFunctions.Height(0,0,0,0,0,0));
        Reject("invalid bounds",()=>CurveVerticalFunctions.Inspect(0,1,0,0,new CurveLengthBounds(201,199,true),.1));
        Reject("degenerate Bezier tangent",()=>CurveFunctions.Bezier(new[]{origin,origin},0));
        Reject("out of domain Hermite",()=>CurveFunctions.Hermite(origin,new CurvePoint(100,0),east,east,1,1,10000));
        CurvePort Port(string id,CurvePoint p,CurvePoint d,double k=0)=>new CurvePort(id,p,d,k,0,8,CurveTravelSide.Middle);
        var scenarios=new[]{
            ("straight",new CurvePoint(240,0),east,0d,0d,true),
            ("near-collinear",new CurvePoint(240,.01),east,0d,0d,true),
            ("epsilon-positive",new CurvePoint(240,0),new CurvePoint(Math.Cos(1e-8),Math.Sin(1e-8)),0d,0d,true),
            ("epsilon-negative",new CurvePoint(240,0),new CurvePoint(Math.Cos(1e-8),-Math.Sin(1e-8)),0d,0d,true),
            ("pi-positive",new CurvePoint(10,5),new CurvePoint(-Math.Cos(1e-8),Math.Sin(1e-8)),0d,0d,true),
            ("pi-negative",new CurvePoint(10,5),new CurvePoint(-Math.Cos(1e-8),-Math.Sin(1e-8)),0d,0d,true),
            ("shift",new CurvePoint(240,60),east,0d,0d,true),
            ("left",new CurvePoint(160,180),north,0d,0d,true),
            ("right",new CurvePoint(160,-180),new CurvePoint(0,-1),0d,0d,true),
            ("near-reverse",new CurvePoint(100,100),new CurvePoint(-Math.Cos(.01),Math.Sin(.01)),0d,0d,true),
            ("short-turn",new CurvePoint(8,10),north,0d,0d,true),
            ("same-curvature-G1",new CurvePoint(160,180),north,.003,.003,false),
            ("opposite-curvature-G1",new CurvePoint(160,180),north,.003,-.003,false),
            ("strict-G2-conflict",new CurvePoint(160,180),north,.003,.003,true)};
        foreach(var s in scenarios){
            var request=new CurveKernelRequest(Port("A",origin,east,s.Item4),Port("B",s.Item2,s.Item3,s.Item5),40,8,s.Item6,1);
            var timer=Stopwatch.StartNew();var result=CurveCandidateSearch.Search(request,true);timer.Stop();
            Check("search never grants game permission "+s.Item1,!result.CanApplyToGame&&result.Candidates.All(c=>!c.CanApplyToGame));
            if(s.Item1=="strict-G2-conflict")Check("search preserves strict conflict",result.Status==CurveKernelStatus.ConstraintConflict&&result.Candidates.Count==0);
            else Check("fixed budget actual output inspected "+s.Item1,result.Candidates.Count==13&&result.Candidates.All(c=>c.Report.Status==CurveGeometryVerifier.Verify(request,c.Geometry).Status));
            Comparisons.Add(new{scenario=s.Item1,status=result.Status.ToString(),milliseconds=timer.Elapsed.TotalMilliseconds,candidates=result.Candidates.Select(c=>new{family=c.Family,status=c.Report.Status.ToString(),segments=c.Geometry.Segments.Count,actualOutput=c.Geometry.Segments,metrics=c.Report.Metrics,issues=c.Report.Issues, sampledMinimumRadius=SampledRadius(c.Geometry)}).ToArray()});
            foreach(double angle in new[]{Math.PI/2,37*Math.PI/180}){
                CurvePoint Rotate(CurvePoint p)=>new CurvePoint(p.X*Math.Cos(angle)-p.Y*Math.Sin(angle),p.X*Math.Sin(angle)+p.Y*Math.Cos(angle));
                var transformed=new CurveKernelRequest(Port("A",new CurvePoint(123,-75),Rotate(east),s.Item4),Port("B",Rotate(s.Item2)+new CurvePoint(123,-75),Rotate(s.Item3),s.Item5),40,8,s.Item6,1);
                var other=CurveCandidateSearch.Search(transformed,true);
                Check("rotation translation categories "+s.Item1+"/"+angle,result.Candidates.Select(c=>c.Report.Status).SequenceEqual(other.Candidates.Select(c=>c.Report.Status)));
            }
        }
        var shift=new CurveKernelRequest(Port("A",origin,east),Port("B",new CurvePoint(240,60),east),40,8,true,1);
        var shiftResult=CurveCandidateSearch.Search(shift,true);var baseShift=shiftResult.Candidates[1].Geometry.Segments[0]!;
        var signs=Enumerable.Range(1,99).Select(i=>CurveFunctions.Bezier(baseShift.Controls,i/100d).Curvature).ToArray();
        Check("necessary S bend has both curvature signs",signs.Min()<0&&signs.Max()>0&&shiftResult.Candidates[1].Report.Status==CurveGeometryStatus.VerifiedLocalPlanar);
        var mirrored=new CurveKernelRequest(Port("A",origin,east),Port("B",new CurvePoint(240,-60),east),40,8,true,1);
        Check("S mirror preserves candidate categories",shiftResult.Candidates.Select(c=>c.Report.Status).SequenceEqual(CurveCandidateSearch.Search(mirrored,true).Candidates.Select(c=>c.Report.Status)));
        var folded=new CurveGeometryDocument(shift,8,new[]{new CurveGeometrySegment(CurveSegmentKind.Line,new[]{origin,new CurvePoint(100,0)}),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(100,0),origin}),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{origin,new CurvePoint(240,60)})});
        Check("extra reversing loop rejected",CurveGeometryVerifier.Verify(shift,folded).Status==CurveGeometryStatus.Failed);
        var small=CurveFunctions.Hermite(origin,new CurvePoint(10,8),east,north,.03,-.04,10);
        Check("scaled Hermite curvature law",Near(CurveFunctions.Bezier(small.Controls,.3).Curvature,10*CurveFunctions.Bezier(h.Controls,.3).Curvature));
        Reject("Bezier extreme control rejected",()=>CurveFunctions.Measure(new[]{origin,new CurvePoint(double.MaxValue,0)}));
        Check("search defaults disabled",CurveCandidateSearch.Search(new CurveKernelRequest(Port("A",origin,east),Port("B",new CurvePoint(240,0),east),40,8,true,1)).Status==CurveKernelStatus.Disabled);
        return records;
    }
}
