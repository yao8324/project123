"""Diagnostic of a single open-feather cross section, not an acceptance test."""
import json,math
from pathlib import Path
P=Path(__file__).resolve().parents[2]/'Docs/SanctuaryR3Build'
rows=json.loads((P/'WingLayout.json').read_text());intervals=[]
for row in rows:
 if not row['name'].startswith('W01_R_Primary'):continue
 u=.35;dx=row['direction'][0];center=row['attachment'][0]+dx*row['length']*u;across=math.sqrt(1-dx*dx);width=row['width_m']*(.72+.28*math.sin(math.pi*min(u/.55,1)/2))*across;intervals.append((row['name'],center-width/2,center+width/2))
pairs=[]
for a,b in zip(intervals,intervals[1:]):
 overlap=max(0,min(a[2],b[2])-max(a[1],b[1]));pairs.append({'pair':[a[0],b[0]],'projected_overlap_fraction':overlap/min(a[2]-a[1],b[2]-b[1])})
(P/'WingPrimaryOverlapDiagnostic.json').write_text(json.dumps({'scope':'Rest-open right primary feather X-projection at35% length only; not full surface overlap/collision certification','pairs':pairs},indent=2))
print(pairs)
