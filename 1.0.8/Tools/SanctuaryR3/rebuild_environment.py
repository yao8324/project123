"""Rebuild recent environment refinement stages from the preserved props baseline."""
import subprocess
from pathlib import Path
P=Path(__file__).resolve().parents[2]
for stage in ['build_services','refine_surfaces','refine_arcades','check_arcade_clearance','refine_niches','apply_material_maps','refine_gate_gallery','export_environment']:
 print('START',stage,flush=True)
 with (P/'Logs'/f'R3_Rebuild_{stage}.log').open('w') as log:
  subprocess.run(['/Applications/Blender.app/Contents/MacOS/Blender','--background','--python-exit-code','1','--python',str(P/'Tools/SanctuaryR3'/f'{stage}.py')],cwd=P,stdout=log,stderr=subprocess.STDOUT,check=True)
 print('PASS',stage,flush=True)
print('ENVIRONMENT_REBUILD_COMPLETE',flush=True)
