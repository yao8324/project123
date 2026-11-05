"""Audit the scoped Knight delivery; preserve existing project and user files."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

project = Path(__file__).resolve().parents[2]
docs = project / 'Docs/KnightDesign20261105'
output = docs / 'RefinementV2'
scopes = [
    'Assets/Art/Bosses', 'Assets/Scenes/BossTests',
    'Assets/Scripts/Core/Bosses', 'Assets/Scripts/Visuals/Bosses',
    'Assets/Scripts/Visuals/Editor/KnightTrialBuilder.cs',
    'Assets/Scripts/Visuals/Editor/KnightRefinementBuilder.cs',
]
owned = set()
for relative in scopes:
    path = project / relative
    owned.add(path)
    if path.is_dir():
        owned.update(path.rglob('*'))
missing_meta = sorted(str(p.relative_to(project)).replace('\\', '/')
                      for p in owned if not p.name.endswith('.meta')
                      and not Path(str(p) + '.meta').exists())

all_meta = subprocess.run(['rg', '--files', '-g', '*.meta', 'Assets'],
                          cwd=project, check=True, capture_output=True,
                          text=True, encoding='utf-8').stdout.splitlines()
guids = {}
for relative in all_meta:
    path = project / relative
    match = re.search(r'^guid: ([0-9a-f]+)$', path.read_text(encoding='utf-8'), re.M)
    if match:
        guids.setdefault(match[1], []).append(relative.replace('\\', '/'))
duplicates = {key: value for key, value in guids.items() if len(value) > 1
              and any('Knight' in item or 'BossTests' in item for item in value)}
broken_links = []
for path in docs.rglob('*.md'):
    for link in re.findall(r'\]\(([^)]+)\)', path.read_text(encoding='utf-8')):
        target = link.split('#', 1)[0].strip('<>')
        if not target or '://' in target:
            continue
        if not (path.parent / target).exists():
            broken_links.append({'document': str(path.relative_to(project)), 'target': link})

changed = [
    'Assets/Scripts/Core/BossEncounter.cs',
    'Assets/Scripts/Core/BossCombatBehaviours.cs',
    'Assets/Scripts/Core/BossMeleeCombat.cs',
    'Assets/Scripts/UI/BossCombatHUD.cs',
    'Assets/Scripts/UI/RecoveryMenu.cs', 'README.md', 'CHANGELOG.md',
]
files = {p for p in owned if p.is_file()}
files.update(Path(str(p) + '.meta') for p in owned if Path(str(p) + '.meta').exists())
files.update(project / p for p in changed)
files.update((project / 'Tools/KnightReference').glob('*.py'))
manifest = [dict(path=str(p.relative_to(project)).replace('\\', '/'),
                 bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest())
            for p in sorted(files)]
(output / 'SourceManifestV2.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
result = dict(missingMeta=missing_meta, duplicateKnightGuids=duplicates,
              brokenDocumentationLinks=broken_links, sourceFilesHashed=len(manifest),
              scope='Knight-owned Unity assets, changed shared source, and local reference tools')
(output / 'DeliveryAudit.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False, indent=2))
if missing_meta or duplicates or broken_links:
    raise SystemExit(1)
