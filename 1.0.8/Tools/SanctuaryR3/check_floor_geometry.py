import bpy,json,math
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Floor.blend'))
s=bpy.context.scene
for m in bpy.data.materials:
 if m.name=='R3_Bluegrey_Marble_Foundation' or m.name.startswith('R3_FloorVariation_'):
  k=int(m.name.rsplit('_',1)[1]) if m.name.startswith('R3_FloorVariation_') else 0
  next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Roughness'].default_value=.40+k*.003
slabs=bpy.data.objects['E01_StaggeredMarbleSlabs'];verts=[slabs.matrix_world@v.co for v in slabs.data.vertices]
rings=[o for o in s.objects if o.name.startswith('E01_FlushGoldRing_')];measure=[]
for o in rings:
 vs=[o.matrix_world@v.co for v in o.data.vertices];rs=[math.hypot(v.x,v.y) for v in vs];zs=[v.z for v in vs]
 measure.append({'name':o.name,'r_min':min(rs),'r_max':max(rs),'top':max(zs),'bottom':min(zs)})
checks={'five_ring_meshes':len(rings)==5,'stone_top_at_zero':abs(max(v.z for v in verts))<1e-6,'rings_less_than_1mm_step':all(0<=r['top']<=.001 for r in measure),'rings_8mm_thick':all(abs(r['top']-r['bottom']-.008)<1e-6 for r in measure),'five_cross_vaults':sum(o.name.startswith('E05_TrueCrossVault') for o in s.objects)==5,'ten_side_lunette_roofs':sum(o.name.startswith('E05_SideLunetteRoof') for o in s.objects)==10}
report={'scope':'Measured base mesh dimensions only. Not overall art acceptance, collision or export validation.','checks':checks,'ring_measurements':measure,'slab_mesh_bounds':{'min':[min(v[i] for v in verts) for i in range(3)],'max':[max(v[i] for v in verts) for i in range(3)]}}
(P/'Docs/SanctuaryR3Build/GeometryCheck.json').write_text(json.dumps(report,indent=2));assert all(checks.values()),report
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Floor.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Floor_Wide.png');bpy.ops.render.render(write_still=True)
print('R3_GEOMETRY_CHECK',checks)
