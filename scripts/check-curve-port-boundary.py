"""Current approved Core migration diff; historical 27-file reports remain unchanged."""
from pathlib import Path
import subprocess,json,hashlib
root=Path(__file__).resolve().parents[1]
base='9f083061cdcf2bb08ed03c0527ebbd6f591c1179'
def git(*args):return subprocess.check_output(['git','-C',str(root),*args],text=True).strip()
allowed={'README.md','CHANGELOG.md','THIRD_PARTY_NOTICES.md','tests/InterchangeBuilder.LaneConnections.Core.Tests/InterchangeBuilder.LaneConnections.Core.Tests.csproj'}
same=[];changed=[]
for p in git('ls-tree','-r','--name-only',base).splitlines():
    old=git('rev-parse',f'{base}:{p}');new=git('hash-object','--path='+p,str(root/p))
    if p in allowed:
        if old!=new:changed.append(p)
    else:
        assert old==new,p
        same.append({'path':p,'blob':new})
assert not git('diff','d487f8b24cc805d6dc380543b2149bda04491dbd','--','labs/interchange-demo/evidence','labs/interchange-demo/evidence-tail','labs/interchange-demo/evidence-tail-v2')
calls=[str(p.relative_to(root)) for p in (root/'src/InterchangeBuilder.LaneConnections').glob('*.cs') if any(name in p.read_text() for name in ('ExperimentalCurveKernel','CurveGeometryVerifier'))]
assert not calls,calls
tracked=git('ls-files','--cached','--others','--exclude-standard').splitlines()
assert not any(p.lower().endswith(('.dll','.exe','.nupkg')) for p in tracked)
print(json.dumps({'baseline':base,'unchangedOriginalFiles':same,'approvedChangedOriginalFiles':changed,'newCoreSources':['src/InterchangeBuilder.LaneConnections.Core/ExperimentalCurveKernel.cs','src/InterchangeBuilder.LaneConnections.Core/CurveGeometryVerifier.cs'],'runtimeCallSites':calls,'default':'Disabled; CanApplyToGame=false','game':'NOT_RUN','release':'No merge/push/deployment; existing runtime/build/package/CI files unchanged'},indent=2))
