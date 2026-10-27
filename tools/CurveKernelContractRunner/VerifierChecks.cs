using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using InterchangeBuilder.LaneConnections;

public static class VerifierChecks
{
    public static List<object> Run(string vectors)
    {
        var records=new List<object>();
        void Check(string name,bool value){records.Add(new{name,expected=true,actual=value});if(!value)throw new Exception(name);}
        CurvePort Port(string key,double x,double y,double dx=1,double dy=0,double width=8)=>new CurvePort(key,new CurvePoint(x,y),new CurvePoint(dx,dy),0,0,width,CurveTravelSide.Middle);
        var request=new CurveKernelRequest(Port("S",0,0),Port("T",200,0),40,8,true,1);
        var generated=ExperimentalCurveKernel.Generate(request,true);var c=generated.Candidates[1];
        var good=new CurveGeometryDocument(request,8,new CurveGeometrySegment?[]{new CurveGeometrySegment(CurveSegmentKind.Bezier,c.Controls),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{c.TailStart,c.TailEnd})});
        var report=CurveGeometryVerifier.Verify(request,good);
        Check("independent straight oracle actual output locally verified",report.Status==CurveGeometryStatus.VerifiedLocalPlanar&&report.Metrics["straightTail"]==40&&report.Metrics["lengthLower"]==200&&!report.CanApplyToGame);
        Check("generated actual output is independently verified",CurveGeometryVerifier.VerifyGenerated(request,generated,1).Status==CurveGeometryStatus.VerifiedLocalPlanar);
        Check("invalid candidate index rejected",CurveGeometryVerifier.VerifyGenerated(request,generated,9).Status==CurveGeometryStatus.InvalidInput);
        var edited=new CurveKernelRequest(request.Start,request.End,41,8,true,1);
        Check("same revision changed tail invalidates captured request",CurveGeometryVerifier.Verify(edited,good).Issues.Contains("STALE_REQUEST"));
        var mode=new CurveKernelRequest(request.Start,request.End,40,8,true,1,CurvePlacementMode.NoCollision);
        Check("mode switch invalidates captured request",CurveGeometryVerifier.Verify(mode,good).Status==CurveGeometryStatus.InvalidInput);
        Check("fresh mode verification preserves geometric facts",CurveGeometryVerifier.Verify(mode,new CurveGeometryDocument(mode,8,good.Segments)).Status==report.Status);
        Check("null geometry invalid",CurveGeometryVerifier.Verify(request,null).Status==CurveGeometryStatus.InvalidInput);
        Check("null segment invalid",CurveGeometryVerifier.Verify(request,new CurveGeometryDocument(request,8,new CurveGeometrySegment?[]{null})).Status==CurveGeometryStatus.InvalidInput);
        Check("invalid resolution rejects",CurveGeometryVerifier.Verify(request,good,double.NaN).Status==CurveGeometryStatus.InvalidInput);
        Check("actual output width mismatch rejects",CurveGeometryVerifier.Verify(request,new CurveGeometryDocument(request,9,good.Segments)).Status==CurveGeometryStatus.InvalidInput);
        var unsupported=new CurveKernelRequest(Port("S",0,0,width:4),request.End,40,8,true,1);
        Check("unsupported width does not become local pass",CurveGeometryVerifier.Verify(unsupported,new CurveGeometryDocument(unsupported,8,good.Segments)).Status==CurveGeometryStatus.Unknown);
        var controls=c.Controls.ToArray();var frozen=new CurveGeometrySegment(CurveSegmentKind.Bezier,controls);controls[0]=new CurvePoint(100,100);
        Check("raw output copied immutably",frozen.Controls[0].X==0&&frozen.Controls[0].Y==0);
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,double.MaxValue}) {
            var raw=c.Controls.ToArray();raw[1]=new CurvePoint(bad,0);
            var badDocument=new CurveGeometryDocument(request,8,new CurveGeometrySegment?[]{new CurveGeometrySegment(CurveSegmentKind.Bezier,raw),good.Segments[1]});
            Check("nonfinite or extreme actual output rejected "+bad,CurveGeometryVerifier.Verify(request,badDocument).Status==CurveGeometryStatus.InvalidInput);
        }
        double u=1/Math.Sqrt(2);var crossingRequest=new CurveKernelRequest(Port("S",0,0,u,u),Port("T",10,0,u,-u),4,2,false,1);
        var crossing=new CurveGeometryDocument(crossingRequest,2,new CurveGeometrySegment?[]{new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(0,0),new CurvePoint(10,10)}),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(10,10),new CurvePoint(0,10)}),new CurveGeometrySegment(CurveSegmentKind.Line,new[]{new CurvePoint(0,10),new CurvePoint(10,0)})});
        Check("independent nonadjacent self-crossing fault detected",CurveGeometryVerifier.Verify(crossingRequest,crossing).Issues.Contains("SELF_INTERSECTION"));
        using var doc=JsonDocument.Parse(File.ReadAllText(vectors));
        foreach(var row in doc.RootElement.GetProperty("rows").EnumerateArray()){
            var i=row.GetProperty("input");
            CurvePort ReadPort(JsonElement p){var a=p.GetProperty("position");var d=p.GetProperty("direction");return new CurvePort(p.GetProperty("id").GetString()!,new CurvePoint(a[0].GetDouble(),a[1].GetDouble()),new CurvePoint(d[0].GetDouble(),d[1].GetDouble()),p.GetProperty("curvature").GetDouble(),p.GetProperty("offset").GetDouble(),p.GetProperty("width").GetDouble(),Enum.Parse<CurveTravelSide>(p.GetProperty("side").GetString()!,true));}
            var r=new CurveKernelRequest(ReadPort(i.GetProperty("start")),ReadPort(i.GetProperty("end")),i.GetProperty("tail").GetDouble(),i.GetProperty("width").GetDouble(),i.GetProperty("continuity").GetString()=="G2",7);
            var raw=row.GetProperty("output");var parts=new List<CurveGeometrySegment?>();
            foreach(var s in raw.GetProperty("segments").EnumerateArray())parts.Add(new CurveGeometrySegment(s.GetProperty("kind").GetString()=="line"?CurveSegmentKind.Line:CurveSegmentKind.Bezier,s.GetProperty("controls").EnumerateArray().Select(p=>new CurvePoint(p[0].GetDouble(),p[1].GetDouble())).ToArray()));
            var resolution=row.GetProperty("resolution");double tolerance=resolution.TryGetProperty("tolerance",out var tol)?tol.GetDouble():.02;int depth=resolution.TryGetProperty("maxDepth",out var dep)?dep.GetInt32():16;
            var actual=CurveGeometryVerifier.Verify(r,new CurveGeometryDocument(r,raw.GetProperty("width").GetDouble(),parts),tolerance,depth);
            string category=actual.Status==CurveGeometryStatus.VerifiedLocalPlanar?"verified":actual.Status==CurveGeometryStatus.Unknown?"unknown":"rejected";
            Check("JS/C# actual-output acceptance "+row.GetProperty("name").GetString(),category==row.GetProperty("expectedCategory").GetString()&&!actual.CanApplyToGame);
            var alternate=new CurveKernelRequest(r.Start,r.End,r.TailLength,r.Width,r.RequireG2,r.Revision,CurvePlacementMode.NoCollision);
            Check("no-collision does not relax actual output "+row.GetProperty("name").GetString(),CurveGeometryVerifier.Verify(alternate,new CurveGeometryDocument(alternate,raw.GetProperty("width").GetDouble(),parts),tolerance,depth).Status==actual.Status);
            // Reconstruct from actual output (JSON roundtrip representation) and revalidate, never a generator flag.
            var roundtrip=new CurveGeometryDocument(r,raw.GetProperty("width").GetDouble(),parts.Select(p=>new CurveGeometrySegment(p!.Kind,p.Controls)).ToArray());
            Check("reconstructed actual output acceptance "+row.GetProperty("name").GetString(),CurveGeometryVerifier.Verify(r,roundtrip,tolerance,depth).Status==actual.Status);
        }
        return records;
    }
}
