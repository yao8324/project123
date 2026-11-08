import bpy,json
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Services.blend'))
m=bpy.data.materials.new('R3_Warm_Limestone_Masonry');m.use_nodes=True;m.diffuse_color=(.72,.68,.59,1);n=m.node_tree.nodes;l=m.node_tree.links;bs=next(x for x in n if x.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.88,.85,.79,1);bs.inputs['Roughness'].default_value=.78
tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(P/'Assets/Art/Bosses/Knight/Sanctuary20261107/Textures/Limestone_Albedo.png'),check_existing=True);tex.projection='BOX';tex.projection_blend=.2;co=n.new('ShaderNodeTexCoord');l.new(co.outputs['Object'],tex.inputs['Vector']);l.new(tex.outputs['Color'],bs.inputs['Base Color'])
bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.07;bump.inputs['Distance'].default_value=.0005;l.new(tex.outputs['Color'],bump.inputs['Height']);l.new(bump.outputs['Normal'],bs.inputs['Normal'])
prefixes=('Side_wall','End_wall','E05_TrueCrossVault','E05_SideLunetteRoof','E05_LunetteBacking','E05_EndAisleRoof','E05_EndVault','E07_PointArchSpandrel','E06_PointArchSpandrel','E04_GalleryParapet')
changed=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH' and o.name.startswith(prefixes):
  for i,old in enumerate(o.data.materials):
   if old and 'Ivory_Marble' in old.name:o.data.materials[i]=m;changed.append(o.name)
assert len(changed)>=20,changed
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Surfaces.blend'))
(P/'Docs/SanctuaryR3Build/SurfaceSeparation.json').write_text(json.dumps({'limestone_objects':changed,'count':len(changed),'scope':'Material separation only. Masonry joints and revised marble vein asset remain pending.'},indent=2))
print('SURFACE_SEPARATION',len(changed))
