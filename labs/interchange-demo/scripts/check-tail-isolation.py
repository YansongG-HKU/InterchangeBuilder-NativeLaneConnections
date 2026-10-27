"""Read-only final-tree protection check; does not touch runtime files or remote refs."""
from pathlib import Path
import hashlib, json, subprocess, os
root = Path(__file__).resolve().parents[3]
lab = root / 'labs/interchange-demo'
base = '9f083061cdcf2bb08ed03c0527ebbd6f591c1179'
def git(*args):
    return subprocess.check_output(['git', '-C', str(root), *args])
rows = []
for path in git('ls-tree', '-r', '--name-only', base).decode().splitlines():
    if path in ('README.md', 'CHANGELOG.md', 'THIRD_PARTY_NOTICES.md'):
        continue
    expected = git('rev-parse', f'{base}:{path}').decode().strip()
    actual = git('hash-object', '--path='+path, str(root/path)).decode().strip()
    assert actual == expected, path
    rows.append({'path': path, 'baselineBlob': expected, 'actualBlob': actual})
assert not git('diff', 'a13fa7919abcd4689bff035f65bd6d618978cc2b', '--', 'labs/interchange-demo/evidence', 'labs/interchange-demo/src/core', 'labs/interchange-demo/src/view', 'labs/interchange-demo/src/adapters', 'labs/interchange-demo/src/ui.js', 'labs/interchange-demo/src/style.css')
tracked = git('ls-files','--cached','--others','--exclude-standard').decode().splitlines()
newfiles = [p for p in tracked if p.startswith('labs/interchange-demo/')]
assert not any(p.endswith(('.cs','.csproj','.props','.targets','.dll','.exe')) for p in newfiles)
report = {'baseline':base,'experimentBase':'a13fa7919abcd4689bff035f65bd6d618978cc2b','protectedFiles':rows,'staticFindings':['Existing project/build/CI blobs unchanged; explicit project and vendor/UI paths do not include labs.','No C# project, props/targets or game binary in lab.','Only nested Vite build adds curve-lab.html; original C# build/packaging unchanged.','Historic evidence and baseline solver/scene/UI module/adapter unchanged.','Static isolation, not MSBuild evaluation or game validation.'],'status':'VERIFIED_STATIC','game':'NOT_RUN'}
(lab/os.environ.get('EVIDENCE_DIR','evidence-tail')/'mod-protection.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'{len(rows)} original protected files verified; baseline solver/scene/evidence unchanged')
