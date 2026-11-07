import bpy
for a in bpy.data.actions:
 print('ACTION',a.name,'range',list(a.frame_range),'slots',[(s.identifier,s.target_id_type) for s in a.slots],'layers',len(a.layers))
 for layer in a.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    print('CURVES',len(bag.fcurves),[(f.data_path,len(f.keyframe_points)) for f in list(bag.fcurves)[:3]])
