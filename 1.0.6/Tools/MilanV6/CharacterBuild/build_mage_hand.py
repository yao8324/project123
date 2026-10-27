import bpy, math, json, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for d in list(bpy.data.materials): bpy.data.materials.remove(d)
def mat(name,color,metal=0,rough=.65):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
skin=mat('Skin_Stylized_Warm',(0.50,.265,.135),0,.72)
cloth=mat('Robe_Charcoal',(0.025,.031,.04),0,.94)
cuffmat=mat('Cuff_Deep_Slate',(.047,.053,.063),0,.9)
brass=mat('Trim_Muted_Brass',(.23,.15,.055),.55,.48)
wood=mat('Wand_Walnut',(.092,.041,.017),0,.57)
woodedge=mat('Walnut_Carved_Ridges',(.155,.077,.031),0,.58)
meshes=[]
def mesh(name,verts,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);ob.data.materials.append(material);return ob
def tube(name,points,radii,material,sides=10):
 vs=[];fs=[]
 for i,pt in enumerate(points):
  p=Vector(pt); tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
  tangent.normalize();ref=Vector((0,1,0)) if abs(tangent.y)<.95 else Vector((1,0,0));u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized()
  r=radii[i];rx,ry=r if isinstance(r,tuple) else (r,r)
  for j in range(sides):
   a=j*math.tau/sides;vs.append(p+u*rx*math.cos(a)+v*ry*math.sin(a))
 for i in range(len(points)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.append(tuple(reversed(range(sides))));fs.append(tuple((len(points)-1)*sides+j for j in range(sides)))
 return mesh(name,vs,fs,material)
def soft_digit(name,points,radii,material,sides=14):
 # Catmull-Rom centreline retains anatomical bends without block-like elbows.
 out=[];rr=[]
 for i in range(len(points)-1):
  p0,p1,p2,p3=[Vector(points[max(0,min(len(points)-1,j))]) for j in [i-1,i,i+1,i+2]]
  for k in range(3):
   t=k/3;out.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));rr.append(radii[i]*(1-t)+radii[i+1]*t)
 out.append(points[-1]);rr.append(radii[-1]);return tube(name,out,rr,material,sides)
def ell(name,loc,scale):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=loc);ob=bpy.context.object;ob.name=name;ob.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);ob.data.materials.append(skin);return ob
# Slightly more natural low-poly anatomy: tapered curled digits, subtle knuckles,
# and a recessed thenar volume that merges into the palm instead of a large lump.
parts=[ell('Palm',(.049,.029,-.035),(.047,.032,.082)),ell('Palm_Wrist_Blend',(.069,.021,-.093),(.036,.031,.048)),ell('Thenar',(.068,.012,-.010),(.030,.026,.049)),tube('Wrist',[(.062,.025,-.092),(.075,.020,-.116),(.089,.012,-.143),(.101,.001,-.170),(.108,-.006,-.187)],[.035,.034,.034,.037,.039],skin,16)]
for name,z,r,angle in [('Index',.031,.0155,.003),('Middle',-.005,.0165,0),('Ring',-.041,.015,-.002),('Little',-.074,.0125,-.004)]:
 pts=[(.063,.036,z),(.029,.040,z+.004),(-.011,.035,z+.004),(-.029,.018,z),(-.031,-.007,z-.002),(-.021,-.025,z-.004),(.003,-.030,z-.003+angle),(.019,-.023,z+angle)]
 parts.append(soft_digit(name,pts,[r*.99,r,r*1.02,r*1.06,r*.96,r*.88,r*.72,r*.40],skin,14))
# Opposing thumb has an angled basal joint and a flatter, tapered fingertip.
parts.append(soft_digit('Thumb',[(.082,.010,-.029),(.080,-.001,.006),(.065,-.022,.038),(.039,-.028,.059),(.013,-.026,.055),(.003,-.021,.047)],[.020,.023,.020,.017,.014,.008],skin,14))
for ob in bpy.context.selected_objects:ob.select_set(False)
for ob in parts:ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();hand=bpy.context.object;hand.name='Hand_R_Stylized_FixedGrip'
rem=hand.modifiers.new('Unified_Sculpt','REMESH');rem.mode='VOXEL';rem.voxel_size=.0025;bpy.ops.object.modifier_apply(modifier=rem.name)
s=hand.modifiers.new('Soft_Animation_Style','SMOOTH');s.factor=.7;s.iterations=6;bpy.ops.object.modifier_apply(modifier=s.name)
hand.data.calc_loop_triangles();n=len(hand.data.loop_triangles);d=hand.modifiers.new('LowPoly','DECIMATE');d.ratio=min(1,4100/n);bpy.ops.object.modifier_apply(modifier=d.name)
for p in hand.data.polygons:p.use_smooth=True
meshes.append(hand)
# Robe forearm runs back toward the lower-right of the first-person view.
a=Vector((.096,.006,-.155));b=Vector((.27,-.15,-.405));axis=(b-a).normalized()
# A few broad, uneven fabric folds; no noisy cloth texture or dense sculpt.
fold_t=[-.011,0,.024,.043,.062,.085,.112,.145,.184,.228,.276,.324,.37,.47,.62,.82]
pts=[a+axis*t for t in fold_t]
rads=[(.047,.045),(.052,.049),(.054,.052),(.055,.053),(.058,.053),(.057,.055),(.063,.058),(.063,.060),(.071,.065),(.075,.067),(.080,.073),(.086,.078),(.091,.083),(.100,.090),(.112,.101),(.125,.115)]
sleeve=tube('Robe_Forearm_R',pts,rads,cloth,20);meshes.append(sleeve)
for i,t in enumerate(fold_t):
 center=Vector(pts[i]);strength=(1-math.exp(-max(t-.026,0)*28))
 for j in range(20):
  vert=sleeve.data.vertices[i*20+j];radial=vert.co-center;ang=j*math.tau/20
  fold=.0018*math.sin(3*ang+t*20)+.0012*math.cos(2*ang-t*29)
  vert.co=center+radial.normalized()*(radial.length+fold*strength)
for p in sleeve.data.polygons:p.use_smooth=True
cuff=tube('Tailored_Cuff_R',[a+axis*t for t in [-.018,-.012,.023,.03]],[(.047,.046),(.054,.051),(.057,.054),(.053,.051)],cuffmat,16);meshes.append(cuff)
trim=tube('Brass_Cuff_Trim',[a+axis*t for t in [.012,.014,.021,.023]],[(.0557,.053),(.057,.054),(.057,.054),(.056,.053)],brass,16);meshes.append(trim)
# Long walnut shaft, thick grip, carved collar, taper to a blunt safety tip.
wand=tube('Wand_Walnut',[(0,0,z if z<=.089 else .089+(z-.089)*(.731/.516)) for z in [-.11,-.103,-.08,.032,.069,.078,.089,.108,.20,.34,.48,.60,.605]],[.012,.020,.020,.017,.016,.022,.020,.013,.011,.0085,.0055,.0025,.001],wood,12);meshes.append(wand)
for z in [-.091,-.063,-.035,-.007,.022]:
 ring=tube('Walnut_Grip_Ridge',[(0,0,z-.0025),(0,0,z),(0,0,z+.0025)],[.020,.021,.020],woodedge,12);meshes.append(ring)
# Bones are deliberately simple Generic, not Humanoid. Grip remains fixed.
armdata=bpy.data.armatures.new('MageHand_Generic');rig=bpy.data.objects.new('MageHand_Rig',armdata);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
 e=armdata.edit_bones.new(name);e.head=head;e.tail=tail
 if parent:e.parent=armdata.edit_bones[parent]
 return e
bone('Root',(0,0,-.52),(0,0,-.44));bone('Forearm_R',b,a,'Root');bone('Hand_R',a,(.045,.026,.018),'Forearm_R');bone('Wand',(0,0,-.08),(0,0,.30),'Hand_R')
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
for ob in meshes:
 bn='Hand_R' if ob==hand else 'Forearm_R' if ob in [sleeve,cuff,trim] else 'Wand'
 vg=ob.vertex_groups.new(name=bn);vg.add(list(range(len(ob.data.vertices))),1,'REPLACE');mod=ob.modifiers.new('Generic_Skin','ARMATURE');mod.object=rig;ob.parent=rig
# Two skinned renderers: skin, plus combined robe/wand with separate rigid groups.
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes[1:]: ob.select_set(True)
bpy.context.view_layer.objects.active=sleeve
bpy.ops.object.join();gear=bpy.context.object;gear.name='Robe_And_Wand_Weighted'
meshes=[hand,gear]
# Root stays identity at every frame. Modest wrist/forearm motion, all clips in-place.
rig.animation_data_create();fps=30;clips={}
for name,length,amp,cycles,pitch in [('Idle',90,.006,1,0),('Walk',30,.021,1,0),('Sprint',22,.035,1,-.17),('Jump',30,.012,.5,.07),('Land',20,.025,.5,-.05),('Crouch',30,.008,1,-.12)]:
 act=bpy.data.actions.new(name);rig.animation_data.action=act
 for f in range(1,length+1):
  t=(f-1)/(length-1);wave=math.sin(t*math.tau*cycles)
  fore=rig.pose.bones['Forearm_R'];fore.rotation_mode='XYZ';fore.rotation_euler=(pitch+amp*wave,amp*.65*math.cos(t*math.tau),amp*.5*wave);fore.keyframe_insert('rotation_euler',frame=f)
  h=rig.pose.bones['Hand_R'];h.rotation_mode='XYZ';h.rotation_euler=(amp*.4*wave,0,amp*.3*wave);h.keyframe_insert('rotation_euler',frame=f)
  root=rig.pose.bones['Root'];root.location=(0,0,0);root.rotation_mode='XYZ';root.rotation_euler=(0,0,0);root.keyframe_insert('location',frame=f);root.keyframe_insert('rotation_euler',frame=f)
 act.use_fake_user=True;clips[name]={'frames':[1,length],'fps':fps,'root_motion':False}
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
scene=bpy.context.scene;scene.render.fps=fps;scene.frame_start=1;scene.frame_end=90
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
# Export only the skinned rig and meshes, keeping stage/cameras out of FBX.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in meshes:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'MageHand_Generic.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
# Asset-only mode skips studio renders for the independent acceptance rebuild.
if os.environ.get('MAGE_ASSET_ONLY') != '1':
 # Neutral studio: actual geometry renders, no generated imagery.
 scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 scene.world.color=(.11,.11,.11);scene.view_settings.view_transform='AgX';scene.view_settings.exposure=-1.0
 def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
 def area(name,loc,power,size,color):
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=color;aim(o,(0,0,.0))
 area('Warm_Key',(1,-1.6,1.6),170,2,(1,.84,.67));area('Cool_Fill',(-1,-.2,.5),85,1.4,(.65,.78,1));area('Edge_Light',(.4,1,1),200,1,(1,.92,.8))
 bpy.ops.object.camera_add(location=(.9,-1.8,.75));cam=bpy.context.object;cam.name='Inspection_Front';cam.data.type='ORTHO';cam.data.ortho_scale=1.25;aim(cam,(.075,0,-.015));scene.camera=cam
 scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(OUT,'render_inspection.png');bpy.ops.render.render(write_still=True)
 # First-person crop: lower-right forearm, wand pointing to upper-left.
 bpy.ops.object.camera_add(location=(.15,-1.12,.36));fp=bpy.context.object;fp.name='FirstPerson_Preview';fp.data.type='PERSP';fp.data.lens=38;aim(fp,(-.15,.04,.06));fp.rotation_euler.rotate_axis('Z',-.20);scene.camera=fp
 scene.render.filepath=os.path.join(OUT,'render_firstperson.png');bpy.ops.render.render(write_still=True)
 # Close-up for checking finger taper, thumb-palm transition and the cuff seam.
 bpy.ops.object.camera_add(location=(.35,-.70,.24));detail=bpy.context.object;detail.name='Grip_Detail';detail.data.type='ORTHO';detail.data.ortho_scale=.44;aim(detail,(.048,0,-.045));scene.camera=detail
 scene.render.filepath=os.path.join(OUT,'render_grip_detail.png');bpy.ops.render.render(write_still=True);scene.camera=fp
report={'blender_version':bpy.app.version_string,'reference':'01_第一人称视高_AI概念_v2.png inspected before modeling','mesh_count':len(meshes),'bones':[b.name for b in rig.data.bones],'clips':clips,'mesh_triangles':{},'validation':{},'unity_tested':False,'notes':['Stylized fixed-grip right hand only; no whole body','Generic armature; Unity import and in-engine validation not run','All clips are in-place; no Root translation or rotation','Wand uses shared Hand_R descendant to preserve grip','No pores or veins; tapered fingers and subtle knuckles; low-poly broad sleeve folds']}
for ob in meshes:ob.data.calc_loop_triangles();report['mesh_triangles'][ob.name]=len(ob.data.loop_triangles)
report['total_triangles']=sum(report['mesh_triangles'].values());report['validation']['under_8000_triangles']=report['total_triangles']<=8000
report['validation']['all_meshes_skinned']=all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes)
report['validation']['all_vertices_weighted']=all(all(len(v.groups)>0 for v in o.data.vertices) for o in meshes)
report['validation']['no_empty_meshes']=all(len(o.data.vertices)>0 for o in meshes)
report['validation']['six_animation_clips']=len(clips)==6
json.dump(report,open(os.path.join(OUT,'validation_manifest.json'),'w'),indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'MageHand_Stylized.blend'))
print('BUILD_REPORT',json.dumps(report))
