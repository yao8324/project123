"""R8 hardware refinement applied after animation; retains all authored actions and feather bones."""
import bpy,math,json,bmesh,sys
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];new=[]
for o in list(s.objects):
 if o.name.startswith(('R01_BrokenHalo','W01_GoldSpar','W01_ArticulatedHinge','R01_R8_','W02_R8_')):bpy.data.objects.remove(o,do_unlink=True)
def mesh(name,v,f,m,bone):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(m);o.parent=rig
 vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(v))),1,'REPLACE');be=o.modifiers.new('Gold edge bevel','BEVEL');be.width=.001;be.segments=2;o.modifiers.new('Rigid hardware attachment','ARMATURE').object=rig;new.append(o);return o
def sweep(name,sections,m,bone):
 v=[p for sec in sections for p in sec];N=len(sections[0]);f=[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(len(sections)-1) for k in range(N)];f.extend([tuple(reversed(range(N))),tuple((len(sections)-1)*N+k for k in range(N))]);return mesh(name,v,f,m,bone)
# Single open crown:105..435deg leaves a30degree opening at the top.
secs=[]
for j in range(193):
 t=j/192;a=math.radians(105+330*t);width=.022*min(1,.04+min(t,1-t)*18);r=.326
 secs.append([(rr*math.cos(a),y,2.2+rr*math.sin(a)) for rr,y in [(r-width/2,-.132),(r+width/2,-.132),(r+width/2,-.152),(r-width/2,-.152)]])
sweep('R01_R8_TaperedCrownHalo',secs,gold,'Spine')
# Shallow repeated leaf-shaped relief on the front face of the flattened halo.
for k in range(22):
 a=math.radians(117+k*14.4);radial=Vector((math.cos(a),0,math.sin(a)));tangent=Vector((-math.sin(a),0,math.cos(a)));c=Vector((.326*math.cos(a),-.129,2.2+.326*math.sin(a)));v=[c+tangent*.012,c+radial*.006,c-tangent*.012,c-radial*.006,c+Vector((0,.003,0))];mesh('R01_R8_HaloLeaf',v,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],gold,'Spine')
for side in ['R','L']:
 for part in ['Root','Elbow','Wrist']:
  bone='Wing'+part+'.'+side;b=rig.data.bones[bone];a=b.head_local.copy();z=b.tail_local.copy();axis=(z-a).normalized();normal=Vector((0,1,0));normal=(normal-axis*normal.dot(axis)).normalized();across=axis.cross(normal).normalized();length=(z-a).length
  sections=[]
  for j in range(25):
   t=j/24;p=a+axis*(.065+(length-.13)*t);width=.035+.018*math.sin(math.pi*t);depth=.023
   sections.append([p+across*width+normal*depth,p-across*width+normal*depth,p-across*width-normal*depth,p+across*width-normal*depth])
  sweep('W02_R8_FacetedSparSheath',sections,gold,bone)
  # Central ridge gives each sheath a cast, leaf-like section.
  sections=[]
  for j in range(25):
   t=j/24;p=a+axis*(.08+(length-.16)*t)+normal*.025;w=.005*math.sin(math.pi*t)+.001
   sections.append([p+across*w,p-across*w,p-across*w+normal*.008,p+across*w+normal*.008])
  sweep('W02_R8_SparRaisedRidge',sections,gold,bone)
  # Raised annular pivot cap in the front-facing plane, with recessed central steel boss.
  for label,rin,rout,dep,mat in [('PivotGold',.040,.069,.043,gold),('PivotSteel',.008,.039,.038,steel),('PivotInnerGold',.018,.025,.046,gold)]:
   sections=[]
   for j in range(65):
    angle=j*math.tau/64;radial=across*math.cos(angle)+axis*math.sin(angle)
    sections.append([a+radial*rin+normal*(dep-.018),a+radial*rout+normal*(dep-.018),a+radial*rout+normal*dep,a+radial*rin+normal*dep])
   sweep('W02_R8_'+label,sections,mat,bone)
  for k in range(8):
   angle=k*math.tau/8;p=a+(across*math.cos(angle)+axis*math.sin(angle))*.053+normal*.047
   vv=[p+across*.004,p+axis*.004,p-across*.004,p-axis*.004,p+normal*.003];mesh('W02_R8_PivotFastener',vv,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],steel,bone)
assert len(rig.data.bones)==72
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Hardware.blend'),copy=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=rig
if '--skip-export' not in sys.argv:bpy.ops.export_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
(P/'Docs/SanctuaryR3Build/WingHardwareR8.json').write_text(json.dumps({'new_skinned_meshes':len(new),'halo_opening_degrees':30,'halo_radial_width_m':.022,'pivot_caps':6,'spar_sheaths':6,'rig_bones':72,'scope':'Hardware only; original feather counts and actions preserved. No envelope/clearance acceptance.'},indent=2))
s.cycles.samples=16;s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100
rig.animation_data.action=bpy.data.actions['A03_JumpAttack'];s.frame_set(40);bpy.context.view_layer.update();root=rig.pose.bones['Root'].matrix.translation;focus=root+Vector((0,0,2));s.camera.location=focus+Vector((3,8,2));s.camera.rotation_euler=(focus-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.type='ORTHO';s.camera.data.ortho_scale=6.5;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Wing_Hardware_R8.png');bpy.ops.render.render(write_still=True)
print('WING_HARDWARE_R8_COMPLETE',len(new))
