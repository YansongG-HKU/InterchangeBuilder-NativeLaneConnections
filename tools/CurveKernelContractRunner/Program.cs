using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using InterchangeBuilder.LaneConnections;

public static class CurveKernelContractChecks
{
    public static List<object> Run(string vectors)
    {
        var records = new List<object>();
        void Check(string name, bool value) { records.Add(new { name, expected = true, actual = value }); if (!value) throw new Exception(name); }
        CurvePort Port(string key, double x, double y, double dx = 1, double dy = 0, double k = 0, double offset = 0) =>
            new CurvePort(key, new CurvePoint(x,y),new CurvePoint(dx,dy),k,offset,8,offset<0?CurveTravelSide.Right:offset>0?CurveTravelSide.Left:CurveTravelSide.Middle);
        CurveKernelRequest Request(CurvePort? a = null,CurvePort? b = null,long revision = 1,CurvePlacementMode mode = CurvePlacementMode.Normal) =>
            new CurveKernelRequest(a??Port("S",0,0),b??Port("T",200,0),40,8,true,revision,mode);
        bool Near(double a,double b) => Math.Abs(a-b)<1e-8;
        var request=Request();var result=ExperimentalCurveKernel.Generate(request,true);
        Check("default disabled and no game capability",ExperimentalCurveKernel.Generate(request).Status==CurveKernelStatus.Disabled&&!result.CanApplyToGame);
        var c=result.Candidates[1];
        Check("independent straight oracle controls x0,32,64,96,128,160",c.Controls.Select(p=>p.X).Zip(new[]{0d,32,64,96,128,160},Near).All(x=>x)&&c.Controls.All(p=>p.Y==0)&&Near(c.ActualTailLength,40));
        Check("numeric overflow invalid",ExperimentalCurveKernel.Generate(Request(Port("S",1e308,0)),true).Status==CurveKernelStatus.InvalidInput);
        Check("zero direction invalid",ExperimentalCurveKernel.Generate(Request(Port("S",0,0,0,0)),true).Status==CurveKernelStatus.InvalidInput);
        Check("zero transition degenerate",ExperimentalCurveKernel.Generate(Request(b:Port("T",40,0)),true).Status==CurveKernelStatus.Degenerate);
        Check("strict G2 and nonzero target curvature conflict",ExperimentalCurveKernel.Generate(Request(b:Port("T",200,0,1,0,.01)),true).Status==CurveKernelStatus.ConstraintConflict);
        Check("same snapshot matches",result.Matches(request));
        Check("changed node identity invalidates",!result.Matches(Request(b:Port("OTHER",200,0))));
        Check("changed revision invalidates",!result.Matches(Request(revision:2)));
        Check("mode switch invalidates",!result.Matches(Request(mode:CurvePlacementMode.NoCollision)));
        Check("same ids and revision but edited geometry invalidates",!result.Matches(Request(b:Port("T",201,0))));
        var loose=ExperimentalCurveKernel.Generate(Request(mode:CurvePlacementMode.NoCollision),true);
        Check("mode does not alter geometry or grant game permission",loose.Candidates[1].Controls.Zip(c.Controls,(a,b)=>Near(a.X,b.X)&&Near(a.Y,b.Y)).All(x=>x)&&!loose.CanApplyToGame);
        foreach(int sign in new[]{-1,1})foreach(int side in new[]{-1,0,1}){
            var r=Request(Port("S",0,0,1,0,.002*sign,side*2),Port("T",120,120*sign,0,sign,0,side*2));
            var q=ExperimentalCurveKernel.Generate(r,true);Check($"left/right and side {sign}/{side} fixed positions",q.Status==CurveKernelStatus.CandidateUnverified&&q.Candidates.All(x=>Near(x.Controls[0].X,r.Start.ActualPosition.X)&&Near(x.Controls[0].Y,r.Start.ActualPosition.Y)&&Near(x.TailEnd.X,r.End.ActualPosition.X)&&Near(x.TailEnd.Y,r.End.ActualPosition.Y)));
            foreach(var x in q.Candidates){var v=(x.Controls[1]-x.Controls[0])*5;var a=(x.Controls[2]-x.Controls[1]*2+x.Controls[0])*20;double k=(v.X*a.Y-v.Y*a.X)/Math.Pow(v.Length,3);Check($"independent endpoint jet {sign}/{side}/{x.Scale}",Near(k,r.Start.Curvature/(1-r.Start.Offset*r.Start.Curvature))&&Near(x.ActualTailLength,40));}
        }
        using var doc=JsonDocument.Parse(File.ReadAllText(vectors));
        foreach(var row in doc.RootElement.GetProperty("rows").EnumerateArray()){
            var i=row.GetProperty("input");
            CurvePort ReadPort(JsonElement p){var pos=p.GetProperty("position");var d=p.GetProperty("direction");return new CurvePort(p.GetProperty("id").GetString()!,new CurvePoint(pos[0].GetDouble(),pos[1].GetDouble()),new CurvePoint(d[0].GetDouble(),d[1].GetDouble()),p.GetProperty("curvature").GetDouble(),p.GetProperty("offset").GetDouble(),p.GetProperty("width").GetDouble(),Enum.Parse<CurveTravelSide>(p.GetProperty("side").GetString()!,true));}
            var r=new CurveKernelRequest(ReadPort(i.GetProperty("start")),ReadPort(i.GetProperty("end")),i.GetProperty("tail").GetDouble(),i.GetProperty("width").GetDouble(),i.GetProperty("continuity").GetString()=="G2",5);
            var q=ExperimentalCurveKernel.Generate(r,true);var expected=row.GetProperty("candidates");
            Check("JS/C# candidate count "+row.GetProperty("name").GetString(),q.Candidates.Count==expected.GetArrayLength());
            for(int n=0;n<q.Candidates.Count;n++){var points=expected[n].GetProperty("controls");var tail=expected[n].GetProperty("tail");bool ok=true;for(int j=0;j<6;j++)ok&=Near(q.Candidates[n].Controls[j].X,points[j][0].GetDouble())&&Near(q.Candidates[n].Controls[j].Y,points[j][1].GetDouble());ok&=Near(q.Candidates[n].TailEnd.X,tail[1][0].GetDouble())&&Near(q.Candidates[n].TailEnd.Y,tail[1][1].GetDouble());Check("JS/C# actual controls "+row.GetProperty("name").GetString()+"/"+n,ok);}
        }
        records.AddRange(VerifierChecks.Run(Path.Combine(Path.GetDirectoryName(vectors)!,"curve-verifier-vectors.json")));
        records.AddRange(FunctionChecks.Run(Path.Combine(Path.GetDirectoryName(vectors)!,"curve-function-oracles.json")));
        records.AddRange(RestrictedChecks.Run());
        records.AddRange(SpatialChecks.Run());
        return records;
    }
}
#if CURVE_CONTRACT_RUNNER
public static class Program
{
    public static int Main(string[] args)
    {
        try {var records=CurveKernelContractChecks.Run(args[0]);Console.WriteLine(JsonSerializer.Serialize(new { runtime=Environment.Version.ToString(),count=records.Count,records,comparisons=FunctionChecks.Comparisons,restricted=RestrictedChecks.Evidence },new JsonSerializerOptions{WriteIndented=true}));return 0;}
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}

#endif
