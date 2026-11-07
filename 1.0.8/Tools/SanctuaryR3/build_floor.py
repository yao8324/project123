import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_CrossVault.blend'))
s=bpy.context.scene;gold=bpy.data.materials['R3_Antique_Gold_Foundation'];floor=bpy.data.materials['R3_Bluegrey_Marble_Foundation']
next(n for n in floor.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Roughness'].default_value=.40
def mesh(name,v,f,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.data.materials.append(mat);return o
for o in list(s.objects):
 if o.name.startswith('E01_Inlay_'):bpy.data.objects.remove(o,do_unlink=True)
base=bpy.data.objects['E01_Floor_64x72'];base.location.z=-.176;base.dimensions.z=.348
# Combined slab mesh; joints are genuine 5mm openings with a recessed supporting surface 2mm below.
v=[];f=[];rng=random.Random(56);indices=[];count=0
for row in range(45):
 y0=-36+row*1.6;y1=y0+1.6
 edges=[-32]+[x for x in [(-32+(.8 if row%2 else 0)+i*1.6) for i in range(41)] if -32<x<32]+[32]
 for x0,x1 in zip(edges,edges[1:]):
  x0+=.0025;x1-=.0025;a=len(v);v.extend([(x0,y0+.0025,-.04),(x1,y0+.0025,-.04),(x1,y1-.0025,-.04),(x0,y1-.0025,-.04),(x0,y0+.0025,0),(x1,y0+.0025,0),(x1,y1-.0025,0),(x0,y1-.0025,0)])
  f.extend([tuple(a+i for i in face) for face in [(4,5,6,7),(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]]);indices.extend([rng.randrange(5)]*6);count+=1
slabs=mesh('E01_StaggeredMarbleSlabs',v,f,floor)
for k in range(1,5):
 mat=floor.copy();mat.name=f'R3_FloorVariation_{k}';bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.40+k*.003;slabs.data.materials.append(mat)
for p,idx in zip(slabs.data.polygons,indices):p.material_index=idx
bevel=slabs.modifiers.new('Stone edge microbevel','BEVEL');bevel.width=.0008;bevel.segments=2
# Five flat annular strips, top +0.5mm, lower face -7.5mm.
for r in [6,6.15,17.6,25.8,26.05]:
 width=.06 if r>=17.6 else .03;vv=[];ff=[];n=1024
 for i in range(n):
  a=i*math.tau/n
  for z in [-.0075,.0005]:
   for rr in [r-width/2,r+width/2]:vv.append((rr*math.cos(a),rr*math.sin(a),z))
 for i in range(n):
  a=4*i;b=4*((i+1)%n);ff.extend([(a+2,a+3,b+3,b+2),(a,b,b+1,a+1),(a,a+2,b+2,b),(a+1,b+1,b+3,a+3)])
 ob=mesh(f'E01_FlushGoldRing_{r}',vv,ff,gold);bev=ob.modifiers.new('1mm inlay bevel','BEVEL');bev.width=.001;bev.segments=2
# Eight tapered compass needles, flush with the same floor datum.
for i in range(8):
 a=i*math.tau/8;length=6 if i%2==0 else 4.4
 points=[(1.1,-.055),(length,0),(1.1,.055),(1.7,0)]
 vv=[(x*math.cos(a)-y*math.sin(a),x*math.sin(a)+y*math.cos(a),.0005) for x,y in points]
 ob=mesh('E01_CompassNeedle',vv,[(0,1,2,3)],gold);sol=ob.modifiers.new('8mm inlay','SOLIDIFY');sol.thickness=.008;sol.offset=-1
s['design_version']='R3 cross-vault + dimensioned floor, not accepted';s.cycles.samples=32
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Floor.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Floor_Wide.png');bpy.ops.render.render(write_still=True)
cam=s.camera;cam.location=(5,-8,2.8);cam.rotation_euler=(Vector((0,-3,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=45
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Floor_Detail.png');bpy.ops.render.render(write_still=True)
report={'stage':'floor construction, not final acceptance','stone_slabs':count,'slab_nominal_m':[1.6,1.6],'stagger_half_m':.8,'joint_width_m':.005,'joint_recess_m':.002,'gold_top_m':.0005,'gold_thickness_m':.008,'ring_radii_m':[6,6.15,17.6,25.8,26.05],'remaining':'PBR refinement, full scene scope, gameplay integration, independent final acceptance'}
(P/'Docs/SanctuaryR3Build/Floor.json').write_text(json.dumps(report,indent=2));print('R3_FLOOR_COMPLETE',report)
