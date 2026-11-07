import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Detail.blend'));s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];dg=bpy.context.evaluated_depsgraph_get()
def tree(name):
 ev=bpy.data.objects[name].evaluated_get(dg);me=ev.to_mesh();v=[ev.matrix_world@p.co for p in me.vertices];f=[tuple(p.vertices) for p in me.polygons];ev.to_mesh_clear();return BVHTree.FromPolygons(v,f),v
shell,verts=tree('R01_R7_OpenVisorHelmet');liner,_=tree('R01_R7_RecessedVisorLiner')
checks={};samples=[]
for label,x,z,opening in [('eye_right',.08,2.065,True),('eye_left',-.08,2.065,True),('central_slit',0,2.02,True),('cheek',.08,2.00,False),('forehead',.05,2.15,False)]:
 origin=Vector((x,1,z));direction=Vector((0,-1,0));hit=shell.ray_cast(origin,direction)[0];inner=liner.ray_cast(origin,direction)[0]
 # A ray through the front opening can hit the rear shell: count only front-facing half.
 front=hit is not None and hit.y>0
 checks[label]=(not front if opening else front);samples.append({'location':[x,z],'front_shell_hit':front,'liner_hit':inner is not None})
parts=[o for o in s.objects if '_R7_' in o.name and o.type=='MESH']
checks['detail_attachment_bones_exist']=all(o.vertex_groups and o.vertex_groups[0].name in rig.data.bones for o in parts)
checks['detail_armature_targets_correct']=all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in parts)
report={'scope':'Rest-pose apertures and attachment checks; no action collisions/final acceptance','checks':checks,'ray_samples':samples,'evaluated_helmet_top_m':max(v.z for v in verts),'detail_meshes':len(parts)}
(P/'Docs/SanctuaryR3Build/KnightDetailCheckR7.json').write_text(json.dumps(report,indent=2));print(report);assert all(checks.values())
