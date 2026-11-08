"""Host-side ordered rebuild; stops immediately if any Blender stage fails."""
import subprocess
from pathlib import Path
P=Path(__file__).resolve().parents[2]
blender='/Applications/Blender.app/Contents/MacOS/Blender'
for stage in ['refine_wing_layout','refine_armor','refine_silhouette','refine_lower_body','refine_knight_detail','refine_feather_layers','animate_paladin','refine_wing_hardware','refine_body_proportions','refine_curved_plates','refine_gauntlets','refine_armor_form','refine_helmet_cloth','refine_dash_cloth_clearance','export_character','check_wing_revision','check_cloth_relief','check_character_export']:
 print('START',stage,flush=True)
 with (P/'Logs'/f'R3_Rebuild_{stage}.log').open('w') as log:
  subprocess.run([blender,'--background','--python-exit-code','1','--python',str(P/'Tools/SanctuaryR3'/f'{stage}.py')]+(['--','--skip-export'] if stage=='refine_wing_hardware' else []),cwd=P,stdout=log,stderr=subprocess.STDOUT,check=True)
 print('PASS',stage,flush=True)
print('CHARACTER_REBUILD_COMPLETE',flush=True)
