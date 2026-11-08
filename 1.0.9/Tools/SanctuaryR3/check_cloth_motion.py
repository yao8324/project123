import bpy,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];cloth=[o for o in s.objects if o.name.startswith('C01_FoldedPanel')];legs=[o for o in s.objects if o.name.startswith(('R05_ThighArmor','R05_PointKneeShield','R05_TaperedGreave','R05_CurvedTassetScale','R05_R11_HipShell'))];soles=[o for o in s.objects if o.name.startswith('R06_ClosedSole')];report=[]
def evaluated(o,dg):
 ev=o.evaluated_get(dg);me=ev.to_mesh();v=[ev.matrix_world@p.co for p in me.vertices];f=[list(p.vertices) for p in me.polygons];ev.to_mesh_clear();return v,BVHTree.FromPolygons(v,f)
for name,end in [('A01_SlowWalk',67),('A02_Dash',58),('A03_JumpAttack',97)]:
 rig.animation_data.action=bpy.data.actions[name];low=1e6;footlow=1e6;hits=[];angles=[];stance={};drift=0;bone_error=0
 for sample in range((end-1)*2+1):
  frame=1+sample/2
  s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ll=[(o.name,evaluated(o,dg)[1]) for o in legs]
  for o in cloth:
   v,tree=evaluated(o,dg);low=min(low,min(p.z for p in v))
   for label,other in ll:
    if tree.overlap(other):hits.append({'frame':frame,'cloth':o.name,'armor':label})
  for o in soles:
   v,_=evaluated(o,dg);footlow=min(footlow,min(p.z for p in v))
  for side,shift in [('R',0),('L',.55)]:
   for prefix in ['Thigh.','Shin.']:
    pb=rig.pose.bones[prefix+side];bone_error=max(bone_error,abs((pb.tail-pb.head).length-pb.bone.length))
   if name.startswith('A01'):
    t=(frame-1)/60;phase=((t+shift)%1.1)/1.1;cycle=math.floor((t+shift)/1.1+1e-8)
    if phase<.5:
     point=rig.pose.bones['Foot.'+side].head.copy();key=(side,cycle)
     if key not in stance:stance[key]=point
     drift=max(drift,(point-stance[key]).length)
  angles.append(float(rig.pose.bones['Cloth.Rear.R.1'].rotation_quaternion.angle))
 report.append({'action':name,'frames':end,'sampled_poses_120hz':(end-1)*2+1,'cloth_min_world_z_m':low,'sole_min_world_z_m':footlow,'cloth_armor_surface_overlap_count':len(hits),'overlap_examples':hits[:20],'rear_joint_angle_range_rad':[min(angles),max(angles)],'maximum_leg_length_error_m':bone_error,'walk_stance_max_head_drift_m':drift if name.startswith('A01') else None})
(P/'Docs/SanctuaryR3Build/ClothMotionCheck.json').write_text(json.dumps({'scope':'120Hz samples including half-frames of60fps clips, actual evaluated cloth/leg surfaces; BVH surface overlaps only, no containment or continuous-time certification','clips':report},indent=2));print('CLOTH_MOTION_CHECK',json.dumps(report))
