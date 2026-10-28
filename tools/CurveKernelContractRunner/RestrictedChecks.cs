using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InterchangeBuilder.LaneConnections;

public static class RestrictedChecks
{
    public static readonly List<object> Evidence=new List<object>();
    public static List<object> Run()
    {
        Evidence.Clear();var checks=new List<object>();
        void Check(string name,bool value){checks.Add(new{name,expected=true,actual=value});if(!value)throw new Exception(name);}
        var east=new CurvePoint(1,0);var zero=new CurvePoint(0,0);
        CurveKernelRequest Request(CurvePoint end,CurvePoint direction,CurvePlacementMode mode=CurvePlacementMode.Normal,double curvature=0)=>new CurveKernelRequest(new CurvePort("start",zero,east,curvature,0,8,CurveTravelSide.Middle),new CurvePort("end",end,direction,0,0,8,CurveTravelSide.Middle),40,8,true,1,mode);
        RestrictedRectangle Box(string id,double x,double y,double hx,double hy)=>new RestrictedRectangle(id,new CurvePoint(x,y),east,hx,hy);
        var r=Request(new CurvePoint(400,0),east);var obstacle=new[]{Box("central",200,0,40,80)};
        CurveGeometrySegment Line(double x0,double y0,double x1,double y1)=>new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(x0,y0),new CurvePoint(x1,y1)});
        // Independent polynomial reference: x grows linearly, y is smoothstep5. No routing/generator calls.
        var up=new CurveGeometrySegment(CurveSegmentKind.Bezier,new[]{new CurvePoint(40,0),new CurvePoint(62,0),new CurvePoint(84,0),new CurvePoint(106,160),new CurvePoint(128,160),new CurvePoint(150,160)});
        var down=new CurveGeometrySegment(CurveSegmentKind.Bezier,new[]{new CurvePoint(250,160),new CurvePoint(272,160),new CurvePoint(294,160),new CurvePoint(316,0),new CurvePoint(338,0),new CurvePoint(360,0)});
        var reference=new CurveGeometryDocument(r,8,new[]{Line(0,0,40,0),up,Line(150,160,250,160),down,Line(360,0,400,0)});
        var independent=RestrictedCurveVerifier.Verify(r,reference,obstacle,1);
        Check("independent constructed detour reference",independent.Status==RestrictedStatus.VerifiedRestrictedPlanar);
        var straight=new CurveGeometryDocument(r,8,new[]{Line(0,0,400,0)});
        Check("continuous straight crossing rejected",RestrictedCurveVerifier.Verify(r,straight,obstacle,1).Status==RestrictedStatus.CandidateFailed);
        Check("width not centreline only",RestrictedCurveVerifier.Verify(r,straight,new[]{Box("edge",200,4.5,20,1)},0).Status==RestrictedStatus.CandidateFailed);
        Check("exact width tangency never passes",RestrictedCurveVerifier.Verify(r,straight,new[]{Box("tangent",200,5,20,1)},0,8).Status==RestrictedStatus.Unknown);
        Check("low collision subdivision unresolved",RestrictedCurveVerifier.Verify(r,straight,obstacle,1,0).Status==RestrictedStatus.Unknown);
        var cases=new[]{
            ("central",r,obstacle),
            ("single-side",r,new[]{Box("lower",180,-10,50,35)}),
            ("narrow",r,new[]{Box("top",200,15,80,12),Box("bottom",200,-15,80,12)}),
            ("L",r,new[]{Box("stem",190,0,20,60),Box("foot",240,-50,70,10)}),
            ("U",r,new[]{Box("base",210,-50,70,10),Box("left",150,-10,10,50),Box("right",270,-10,10,50)}),
            ("multi-channel",r,new[]{Box("first",130,-10,20,30),Box("second",260,35,25,30)}),
            ("tail-blocked",r,new[]{Box("tail",380,0,10,10)}),
            ("start-blocked",r,new[]{Box("start",0,0,5,5)}),
            ("shift",Request(new CurvePoint(400,80),east),new[]{Box("centre",200,40,30,20)}),
            ("left",Request(new CurvePoint(300,240),new CurvePoint(0,1)),new[]{Box("left-obstacle",180,80,35,30)}),
            ("right",Request(new CurvePoint(300,-240),new CurvePoint(0,-1)),new[]{Box("right-obstacle",180,-80,35,30)}),
            ("reverse",Request(new CurvePoint(200,160),new CurvePoint(-1,0)),new[]{Box("reverse-obstacle",150,60,30,20)}),
            ("curved-start",Request(new CurvePoint(400,0),east,curvature:.002),obstacle)};
        foreach(var item in cases){
            var timer=Stopwatch.StartNew();var result=RestrictedCurveRouter.Search(item.Item2,item.Item3,1,true);timer.Stop();
            var baseline=result.Candidates.Where(c=>c.Family=="baseline").ToArray();var routed=result.Candidates.Where(c=>c.Family!="baseline").ToArray();
            if(item.Item1=="central")Check("genuine baseline-all-fail routing gain",baseline.All(c=>c.Report.Status==RestrictedStatus.CandidateFailed)&&routed.Any(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar));
            if(item.Item1=="tail-blocked"||item.Item1=="start-blocked")Check("fixed obstruction conflict "+item.Item1,result.Status==RestrictedStatus.ConstraintConflict);
            Check("bounded no game permission "+item.Item1,result.Attempts<=99&&!result.CanApplyToGame);
            foreach(var c in routed.Where(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar)){
                var rebuilt=new CurveGeometryDocument(item.Item2,8,c.Geometry.Segments.Select(s=>new CurveGeometrySegment(s!.Kind,s.Controls.ToArray())).ToArray());
                Check("rebuilt geometry rechecked "+item.Item1+"/"+c.Family,RestrictedCurveVerifier.Verify(item.Item2,rebuilt,item.Item3,1).Status==RestrictedStatus.VerifiedRestrictedPlanar);
                // Enlarging obstacle cannot make THIS previously rejected geometry pass; checked below for all candidates.
            }
            foreach(var c in result.Candidates.Where(c=>c.Report.Status==RestrictedStatus.CandidateFailed)){
                var expanded=item.Item3.Select(b=>new RestrictedRectangle(b.Id,b.Centre,b.Axis,b.HalfLength+5,b.HalfWidth+5)).ToArray();
                Check("enlargement does not release rejected output "+item.Item1+"/"+c.Family,RestrictedCurveVerifier.Verify(item.Item2,c.Geometry,expanded,1).Status!=RestrictedStatus.VerifiedRestrictedPlanar);
            }
            Evidence.Add(new{scenario=item.Item1,input=item.Item2,restrictions=item.Item3,margin=1,status=result.Status.ToString(),attempts=result.Attempts,milliseconds=timer.Elapsed.TotalMilliseconds,baselinePass=baseline.Count(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar),routedPass=routed.Count(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar),candidates=result.Candidates.Select(c=>new{family=c.Family,status=c.Report.Status.ToString(),issues=c.Report.Issues,output=c.Geometry.Segments}).ToArray()});
        }
        var old13=CurveCandidateSearch.Search(r,true).Candidates.Select(c=>RestrictedCurveVerifier.Verify(r,c.Geometry,obstacle,1)).ToArray();
        var sameBudget=RestrictedCurveRouter.Search(r,obstacle,1,true,13);
        var extraBudget=RestrictedCurveRouter.Search(r,obstacle,1,true,99);
        Check("original13 all rejected by same inspector",old13.All(c=>c.Status==RestrictedStatus.CandidateFailed));
        Check("same13 budget demonstrates obstacle-aware gain",sameBudget.Candidates.Any(c=>c.Report.Status==RestrictedStatus.VerifiedRestrictedPlanar));
        Evidence.Add(new{budgetComparison=true,oldBudget=13,oldStatuses=old13.Select(c=>c.Status.ToString()).ToArray(),sameBudgetStatus=sameBudget.Status.ToString(),sameBudgetAttempts=sameBudget.Attempts,extraBudgetStatus=extraBudget.Status.ToString(),extraBudgetAttempts=extraBudget.Attempts});
        foreach(double gap in new[]{9.8,10,10.2}){
            var corridor=new[]{Box("upper-wall",200,gap/2+20,300,20),Box("lower-wall",200,-gap/2-20,300,20)};
            var check=RestrictedCurveVerifier.Verify(r,straight,corridor,1);
            Check("full width corridor "+gap,gap<10?check.Status==RestrictedStatus.CandidateFailed:gap==10?check.Status==RestrictedStatus.Unknown:check.Status==RestrictedStatus.VerifiedRestrictedPlanar);
        }
        foreach(double height in new[]{4.9,5.0,5.1}){
            var check=RestrictedCurveVerifier.Verify(r,straight,new[]{Box("edge-distance",200,height+1,20,1)},1);
            Check("width8 margin1 threshold "+height,height<5?check.Status==RestrictedStatus.CandidateFailed:height==5?check.Status==RestrictedStatus.Unknown:check.Status==RestrictedStatus.VerifiedRestrictedPlanar);
        }
        Check("radius hard constraint not topology",RestrictedCurveVerifier.Verify(r,reference,obstacle,1,18,200).Status==RestrictedStatus.CandidateFailed);
        foreach(double angle in new[]{Math.PI/2,37*Math.PI/180}){
            CurvePoint Rotate(CurvePoint p)=>new CurvePoint(p.X*Math.Cos(angle)-p.Y*Math.Sin(angle),p.X*Math.Sin(angle)+p.Y*Math.Cos(angle));
            var shift=new CurvePoint(123,-77);var transformed=new CurveKernelRequest(new CurvePort("start",shift,Rotate(east),0,0,8,CurveTravelSide.Middle),new CurvePort("end",Rotate(new CurvePoint(400,0))+shift,Rotate(east),0,0,8,CurveTravelSide.Middle),40,8,true,1);
            var boxes=obstacle.Select(box=>new RestrictedRectangle(box.Id,Rotate(box.Centre)+shift,Rotate(box.Axis),box.HalfLength,box.HalfWidth)).ToArray();
            var geometry=new CurveGeometryDocument(transformed,8,reference.Segments.Select(part=>new CurveGeometrySegment(part!.Kind,part.Controls.Select(p=>Rotate(p)+shift).ToArray())).ToArray());
            Check("rigid transformed independent reference "+angle,RestrictedCurveVerifier.Verify(transformed,geometry,boxes,1).Status==RestrictedStatus.VerifiedRestrictedPlanar);
            var search=RestrictedCurveRouter.Search(transformed,boxes,1,true,13);
            Check("rigid transformed routing feasibility "+angle,search.Status==sameBudget.Status);
        }
        var noCollision=Request(new CurvePoint(400,0),east,CurvePlacementMode.NoCollision);
        var a=RestrictedCurveRouter.Search(r,obstacle,1,true);var b=RestrictedCurveRouter.Search(noCollision,obstacle,1,true);
        Check("hard restrictions invariant under no collision mode",a.Candidates.Select(c=>c.Report.Status).SequenceEqual(b.Candidates.Select(c=>c.Report.Status)));
        Check("restriction snapshot matches",a.MatchesRestrictions(obstacle,1,12));
        Check("same ID edited region invalidates",!a.MatchesRestrictions(new[]{Box("central",200,0,41,80)},1,12));
        Check("margin or radius change invalidates",!a.MatchesRestrictions(obstacle,2,12)&&!a.MatchesRestrictions(obstacle,1,13));
        var copied=(RestrictedRectangle[])obstacle.Clone();var frozen=RestrictedCurveRouter.Search(r,copied,1,true,13);copied[0]=Box("changed",200,0,40,80);
        Check("caller collection mutation cannot rewrite snapshot",frozen.Restrictions[0].Id=="central"&&!frozen.MatchesRestrictions(copied,1,12));
        Check("different named object never exempt",RestrictedCurveVerifier.Verify(r,reference,new[]{Box("unrelated-B",95,80,2,2)},1).Status==RestrictedStatus.CandidateFailed);
        Check("hard restriction remains in nocollision",RestrictedCurveVerifier.Verify(noCollision,new CurveGeometryDocument(noCollision,8,straight.Segments),obstacle,1).Status==RestrictedStatus.CandidateFailed);
        Check("small budget search exhausted not no-solution",RestrictedCurveRouter.Search(r,obstacle,1,true,3).Status==RestrictedStatus.SearchExhausted);
        var shortTail=Request(new CurvePoint(100,0),east);
        Check("tail interior conflict even with clear endpoints",RestrictedCurveRouter.Search(shortTail,new[]{Box("tail-interior",77.5,0,2.5,2)},1,true).Status==RestrictedStatus.ConstraintConflict);
        var mirrorBoxes=new[]{Box("central",200,0,40,80)};
        var mirroredGeometry=new CurveGeometryDocument(r,8,reference.Segments.Select(part=>new CurveGeometrySegment(part!.Kind,part.Controls.Select(p=>new CurvePoint(p.X,-p.Y)).ToArray())).ToArray());
        Check("mirrored actual detour independently clear",RestrictedCurveVerifier.Verify(r,mirroredGeometry,mirrorBoxes,1).Status==RestrictedStatus.VerifiedRestrictedPlanar);
        Check("null restrictions rejected",RestrictedCurveRouter.Search(r,null!,1,true).Status==RestrictedStatus.InvalidInput);
        Check("null request rejected",RestrictedCurveRouter.Search(null!,obstacle,1,true).Status==RestrictedStatus.InvalidInput);
        var nearEdge=new CurveKernelRequest(new CurvePort("a",new CurvePoint(-100,9-4e-8),east,0,0,8,CurveTravelSide.Middle),new CurvePort("b",new CurvePoint(100,9-4e-8),east,0,0,8,CurveTravelSide.Middle),40,8,true,1);
        var nearGeometry=new CurveGeometryDocument(nearEdge,8,new[]{Line(-100,9-4e-8,100,9-4e-8)});
        foreach(double scale in new[]{1d,1+9e-9,1-9e-9})Check("axis norm error cannot erase collision "+scale,RestrictedCurveVerifier.Verify(nearEdge,nearGeometry,new[]{new RestrictedRectangle("edge",zero,new CurvePoint(scale,0),20,5)},0).Status==RestrictedStatus.CandidateFailed);
        Check("empty identity invalid",RestrictedCurveRouter.Search(r,new[]{Box("",200,0,40,20)},1,true).Status==RestrictedStatus.InvalidInput);
        Check("invalid margin",RestrictedCurveRouter.Search(r,obstacle,double.NaN,true).Status==RestrictedStatus.InvalidInput);
        Check("router default disabled",RestrictedCurveRouter.Search(r,obstacle,1).Status==RestrictedStatus.Disabled);
        return checks;
    }
}
