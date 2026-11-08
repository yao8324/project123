"""R11 curved, overlapping shoulder and hip armor; existing rig and actions preserved."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];new=[]
for o in list(s.objects):
 if o.name.startswith(('R03_R7_PointShoulderCrest','R03_R7_CrestGiltBorder','R03_R7_CrestLaurel','R05_CurvedTassetScale','R05_TassetGiltEdge','R03_R11_','R05_R11_')):bpy.data.objects.remove(o,do_unlink=True)
def mesh(name,v,f,mat,bone,thickness):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();bm=bmesh.new();bm.from_mesh(me);bm.normal_update()
 for face in bm.faces:
  if face.normal.y<0:face.normal_flip()
 bm.to_mesh(me);bm.free();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(mat);o.parent=r
 for p in me.polygons:p.use_smooth=True
 o.modifiers.new('Forged thickness','SOLIDIFY').thickness=thickness;be=o.modifiers.new('Edge bevel','BEVEL');be.width=.0006;be.segments=2;g=o.vertex_groups.new(name=bone);g.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Armor attachment','ARMATURE').object=r;new.append(o);return o
# Smooth closed silhouette, rather than a single fan of flat triangles.
def contour(points):
 out=[];N=len(points)
 for i in range(N):
  a,b,c,d=[Vector(points[k%N]) for k in [i-1,i,i+1,i+2]]
  for j in range(8):
   t=j/8;out.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
 return out
for side,sg in [('R',1),('L',-1)]:
 for kind,count in [('Shoulder',1),('Hip',4)]:
  for j in range(count):
   shoulder=kind=='Shoulder';bone='UpperArm.'+side if shoulder else 'Pelvis';cx=sg*(.405 if shoulder else .215+j*.015);cz=1.82 if shoulder else 1.19-j*.075;cy=.17 if shoulder else .135+.006*j;bulge=.035 if shoulder else .018
   points=[(-.11,.025),(-.085,.09),(0,.105),(.105,.045),(.115,-.015),(.06,-.085),(0,-.145),(-.075,-.075)] if shoulder else [(-.068,.045),(-.01,.055),(.065,.025),(.078,-.015),(.04,-.075),(-.01,-.125),(-.055,-.065)]
   edge=contour(points);N=len(edge);v=[(cx,cy+bulge,cz)];rings=10
   def pos(q,t,lift=0):return (cx+sg*q.x*t,cy+bulge*(1-t*t)+.012*q.y+lift,cz+q.y*t)
   for k in range(1,rings+1):v.extend(pos(q,k/rings) for q in edge)
   f=[(0,1+i,1+(i+1)%N) for i in range(N)]
   for k in range(rings-1):
    a=1+k*N;b=a+N
    for i in range(N):f.append((a+i,b+i,b+(i+1)%N,a+(i+1)%N))
   prefix='R03_R11_Shoulder' if shoulder else 'R05_R11_Hip'
   mesh(prefix+'Shell',v,f,ivory,bone,.004)
   vv=[]
   for q in edge:vv.extend([pos(q,.95,.002),pos(q,1,.002)])
   mesh(prefix+'GoldBorder',vv,[(2*i,2*i+1,2*((i+1)%N)+1,2*((i+1)%N)) for i in range(N)],gold,bone,.001)
   # Surface-following central keel: follows the same curved patch, rather than floating above it.
   crossings=[]
   for pa,pb in zip(edge,edge[1:]+edge[:1]):
    if pa.x*pb.x<=0 and abs(pb.x-pa.x)>1e-8:crossings.append(pa.y+(pb.y-pa.y)*(-pa.x)/(pb.x-pa.x))
   low,high=min(crossings),max(crossings);vv=[]
   for k in range(25):
    z=.9*(low+(high-low)*k/24);t=abs(z)/(abs(low) if z<0 else high);y=cy+bulge*(1-t*t)+.012*z+.002
    vv.extend([(cx-.003,y,cz+z),(cx+.003,y,cz+z)])
   mesh(prefix+'GoldKeel',vv,[(2*k,2*k+1,2*k+3,2*k+2) for k in range(24)],gold,bone,.001)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
# Review images before export, so geometry is available for visual inspection during FBX baking.
r.data.pose_position='REST';hidden=[]
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):hidden.append((o,o.hide_render));o.hide_render=True
s.cycles.samples=20;s.render.resolution_x=1000;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.55
s.camera.location=(3,6,1.6);s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_Armor_R11.png');bpy.ops.render.render(write_still=True)
for o,value in hidden:o.hide_render=value
r.data.pose_position='POSE';bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==r for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=r
bpy.ops.export_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
(P/'Docs/SanctuaryR3Build/CurvedArmorR11.json').write_text(json.dumps({'new_meshes':len(new),'shoulder_shells':2,'hip_shells':8,'thickness_m':.004,'scope':'Curved geometry and fitted border; not final visual/contact acceptance'},indent=2));print('CURVED_ARMOR_R11_COMPLETE',len(new))
