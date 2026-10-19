import bpy,json,hashlib
from pathlib import Path
R=Path(__file__).resolve().parents[2];p=R/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_PlaytestReview.blend';before=hashlib.sha256(p.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(p),load_ui=False,use_scripts=False)
m=bpy.data.materials['V3_FiguralPaintedGlass'];nodes=[]
for n in m.node_tree.nodes:
 d={'name':n.name,'type':n.type}
 if n.type=='TEX_IMAGE':d.update(image=n.image.name,size=list(n.image.size),filepath=n.image.filepath)
 if n.type=='BSDF_PRINCIPLED':
  d['inputs']={s.name:{'value':list(s.default_value) if hasattr(s.default_value,'__len__')else s.default_value,'links':[{'node':l.from_node.name,'socket':l.from_socket.name}for l in s.links]}for s in n.inputs if s.name in ['Base Color','Alpha','Roughness','IOR','Transmission Weight','Emission Color','Emission Strength']}
 nodes.append(d)
report={'sourceSHA256':before,'material':m.name,'nodes':nodes,'render_method':m.surface_render_method,'use_nodes':m.use_nodes}
(R.parent/'Reports/GlassReview_20261007/native_material.json').write_text(json.dumps(report,indent=2));assert hashlib.sha256(p.read_bytes()).hexdigest()==before;print('READ_ONLY_GLASS_MATERIAL_AUDIT_COMPLETE')
