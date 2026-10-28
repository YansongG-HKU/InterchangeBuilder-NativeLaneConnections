"""Compile repository Core sources against SDK net8 reference assemblies and run pure contracts.
Does not replace netstandard2.0/xUnit/runtime build validation. No package download or game DLL.
Usage: python3 scripts/test-curve-contract-no-nuget.py /absolute/dotnet-sdk-root
"""
from pathlib import Path
import hashlib,json,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
sdk=Path(sys.argv[1]);host=sdk/'dotnet'
compiler=next((sdk/'sdk').glob('8.*/Roslyn/bincore/csc.dll'))
ref=next((sdk/'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))
sources=sorted((root/'src/InterchangeBuilder.LaneConnections.Core').glob('*.cs'))+sorted((root/'tools/CurveKernelContractRunner').glob('*.cs'))
vectors=root/'tests/curve-contract-vectors.json'
with tempfile.TemporaryDirectory(prefix='cs2-curve-contract-') as tmp:
    path=Path(tmp);assembly=path/'CurveContracts.dll'
    rsp=path/'compile.rsp'
    rsp.write_text('\n'.join(['/nologo','/define:CURVE_CONTRACT_RUNNER','/target:exe','/langversion:latest','/nullable:enable',f'/out:{assembly}']+[f'/reference:{p}' for p in sorted(ref.glob('*.dll'))]+[str(p) for p in sources]))
    compiled=subprocess.run([str(host),str(compiler),'@'+str(rsp)],text=True,capture_output=True)
    if compiled.returncode:sys.stderr.write(compiled.stdout+compiled.stderr);sys.exit(compiled.returncode)
    (path/'CurveContracts.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
    run=subprocess.run([str(host),str(assembly),str(vectors)],text=True,capture_output=True)
    if run.returncode:sys.stderr.write(run.stdout+run.stderr);sys.exit(run.returncode)
    result=json.loads(run.stdout)
    result['scope']='Actual .NET8 compilation/execution of Core source; netstandard2.0/xUnit/game builds NOT_RUN'
    result['compiler']=str(compiler.relative_to(sdk))
    result['sourceHashes']={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources+[vectors,root/'tests/curve-verifier-vectors.json',root/'tests/curve-function-oracles.json',Path(__file__)]}
    print(json.dumps(result,indent=2))
