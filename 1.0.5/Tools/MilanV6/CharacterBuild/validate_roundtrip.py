import bpy,json,os,math
from mathutils import Vector
P=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=os.path.join(P,'MageHand_Generic.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
report={'imported_meshes':len(meshes),'imported_bones':list(rig.data.bones.keys()),'actions':{},'vertex_weights_complete':all(all(len(v.groups)>0 for v in o.data.vertices) for o in meshes),'unity_tested':False}
for act in bpy.data.actions:
 rig.animation_data.action=act
 start,end=map(int,act.frame_range);root_values=[];root_matrices=[];relative=[];motion=[]
 for f in range(start,end+1):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  root_values.append(list(rig.pose.bones['Root'].matrix.translation));root_matrices.append([v for row in rig.pose.bones['Root'].matrix for v in row])
  rel=rig.pose.bones['Hand_R'].matrix.inverted()@rig.pose.bones['Wand'].matrix;relative.append([v for row in rel for v in row]);motion.append(list(rig.pose.bones['Hand_R'].matrix.translation))
 maxroot=max((Vector(v)-Vector(root_values[0])).length for v in root_values)
 maxrel=max(abs(v-base) for r in relative for v,base in zip(r,relative[0]))
 maxmotion=max((Vector(v)-Vector(motion[0])).length for v in motion)
 report['actions'][act.name]={'frames':[start,end],'max_root_translation_delta_m':maxroot,'max_root_matrix_delta':max(abs(v-base) for r in root_matrices for v,base in zip(r,root_matrices[0])),'max_grip_relative_matrix_delta':maxrel,'max_hand_translation_delta_m':maxmotion}
report['six_actions_present']=len(report['actions'])==6
report['all_roots_stationary']=all(x['max_root_translation_delta_m']<1e-6 and x['max_root_matrix_delta']<1e-6 for x in report['actions'].values())
report['grip_locked_all_frames']=all(x['max_grip_relative_matrix_delta']<1e-5 for x in report['actions'].values())
report['all_clips_move_hand']=all(x['max_hand_translation_delta_m']>1e-5 for x in report['actions'].values())
report['total_triangles']=sum(len(o.data.polygons)*2 for o in [])
for ob in meshes:ob.data.calc_loop_triangles();report['total_triangles']+=len(ob.data.loop_triangles)
json.dump(report,open(os.path.join(P,'fbx_roundtrip_validation.json'),'w'),indent=2);print(json.dumps(report))
assert report['six_actions_present'] and report['all_roots_stationary'] and report['grip_locked_all_frames'] and report['all_clips_move_hand']
