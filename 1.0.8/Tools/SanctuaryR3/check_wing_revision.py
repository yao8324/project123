import bpy,json,math
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig']
parts=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('W01_','W02_R8_'))];feathers=[o for o in parts if any(x in o.name for x in ['_Primary_','_Secondary_','_Covert_'])]
result={'scope':'wing R8 including hardware sampled geometry and full walk envelope, not final clearance or resemblance acceptance','main_feathers':len(feathers),'poses':[]};maxscale=0;maxlength=0
for label,action,frame in [('walk_fold','A01_SlowWalk',1),('walk_breathe','A01_SlowWalk',34),('dash','A02_Dash',25),('jump_open','A03_JumpAttack',40)]:
 rig.animation_data.action=bpy.data.actions[action];s.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();root=rig.pose.bones['Root'].matrix.translation;verts=[]
 for o in parts:
  ev=o.evaluated_get(dg);me=ev.to_mesh();verts.extend((ev.matrix_world@v.co)-root for v in me.vertices);ev.to_mesh_clear()
 lo=[min(v[i] for v in verts) for i in range(3)];hi=[max(v[i] for v in verts) for i in range(3)]
 result['poses'].append({'name':label,'blender_min_xyz':lo,'blender_max_xyz':hi,'width':hi[0]-lo[0],'depth':hi[1]-lo[1],'low':lo[2],'high':hi[2]})
 for pb in rig.pose.bones:
  if pb.name.startswith(('Wing','Feather_')):
   maxscale=max(maxscale,max(abs(v-1) for v in pb.scale));maxlength=max(maxlength,abs((pb.tail-pb.head).length-pb.bone.length))
# All 67 slow-walk frames: evaluate the deformed meshes, not only two poses.
walk=[];rig.animation_data.action=bpy.data.actions['A01_SlowWalk']
for frame in range(1,68):
 s.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();xs=[]
 for o in parts:
  ev=o.evaluated_get(dg);me=ev.to_mesh();xs.extend((ev.matrix_world@v.co).x for v in me.vertices);ev.to_mesh_clear()
 walk.append({'frame':frame,'width_m':max(xs)-min(xs)})
result['walk_all_frames_max_width_m']=max(x['width_m'] for x in walk)
result['walk_width_within_1_65m']=result['walk_all_frames_max_width_m']<=1.65
result['walk_width_frames']=walk
result['calibration_note']='WingOpen timing unchanged; FK amount remapped 0..0.12 to 0..0.084 and reconnects to original curve at .40. Deliberate interpolation refinement, not original exact smoothstep implementation.'
result['maximum_scale_error']=maxscale;result['maximum_bone_length_error_m']=maxlength
result['remaining']='Final width/depth and hinge/halo/shoulder clearances need another full pass; these samples do not prove no pairwise intersections.'
(P/'Docs/SanctuaryR3Build/WingRevisionCheck.json').write_text(json.dumps(result,indent=2));assert result['walk_width_within_1_65m'],result['walk_all_frames_max_width_m'];assert len(feathers)==38;assert maxscale<1e-4 and maxlength<1e-4;print('WING_REVISION_CHECK',result)
