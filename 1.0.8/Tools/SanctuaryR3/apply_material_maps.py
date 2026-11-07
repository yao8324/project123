"""Keep the native environment material inputs consistent with the R5 Unity maps."""
import bpy,json
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Niches.blend'))
changed=[]
for m in bpy.data.materials:
 name=m.name
 stem='Marble' if 'Ivory_Marble' in name else 'Slate' if 'Bluegrey' in name or 'FloorVariation' in name else 'Gold' if 'Gold' in name or 'Brass' in name else 'GateSteel' if 'Gunmetal' in name else None
 if not stem:continue
 m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
 out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');l.new(bs.outputs['BSDF'],out.inputs['Surface']);bs.inputs['Metallic'].default_value=1 if stem in ['Gold','GateSteel'] else 0
 coord=n.new('ShaderNodeNewGeometry');scale=n.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs[3].default_value=.34;l.new(coord.outputs['Position'],scale.inputs[0])
 maps={}
 for kind in ['Albedo','Mask','Normal']:
  t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(P/f'Assets/Art/Bosses/Knight/SanctuaryR3/Textures/{stem}_{kind}.png'),check_existing=True);t.image.colorspace_settings.name='sRGB' if kind=='Albedo' else 'Non-Color';t.projection='BOX';t.projection_blend=.15;l.new(scale.outputs['Vector'],t.inputs['Vector']);maps[kind]=t
 l.new(maps['Albedo'].outputs['Color'],bs.inputs['Base Color'])
 inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(maps['Mask'].outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],bs.inputs['Roughness'])
 # Microrelief only: color veins remain inside polished stone rather than raised ridges.
 bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.0002;l.new(maps['Mask'].outputs['Alpha'],bump.inputs['Height']);l.new(bump.outputs['Normal'],bs.inputs['Normal'])
 changed.append({'material':name,'maps':stem})
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Materials.blend'))
(P/'Docs/SanctuaryR3Build/MaterialRound5.json').write_text(json.dumps({'materials':changed,'texture_size':2048,'uv_cycles_per_m':.34,'scope':'Native inputs and Unity maps aligned; lighting/normal mapping differ by renderer. No final material acceptance.'},indent=2))
print('MATERIAL_MAPS_NATIVE_COMPLETE',len(changed))
