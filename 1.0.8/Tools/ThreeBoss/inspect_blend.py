import bpy,json
from pathlib import Path
from mathutils import Vector
objects=[]
for o in bpy.context.scene.objects:
 if o.type in {'MESH','ARMATURE'}:
  coords=[o.matrix_world@Vector(c) for c in o.bound_box]
  objects.append(dict(name=o.name,type=o.type,parent=o.parent.name if o.parent else None,verts=len(o.data.vertices) if o.type=='MESH' else 0,visible=not o.hide_render,lo=[min(v[i] for v in coords) for i in range(3)],hi=[max(v[i] for v in coords) for i in range(3)]))
print('BLEND_INSPECT',json.dumps(dict(objects=objects,actions=[a.name for a in bpy.data.actions],materials=[m.name for m in bpy.data.materials])))
