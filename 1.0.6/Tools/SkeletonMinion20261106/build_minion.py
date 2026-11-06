"""Blender 4.3: reusable skeleton, separate machete/armor, rigid bone skinning.
Run in a background process, never in the user's current Blender session.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector
from math import sin, cos, pi

ROOT = Path(sys.argv[sys.argv.index('--') + 1])
SRC = ROOT / 'Blender/Minions/Skeleton20261106'
OUT = ROOT / 'Assets/Art/Minions/Skeleton/Models'
DOC = ROOT / 'Docs/SkeletonMinion20261106'
for d in (SRC, OUT, DOC / 'Renders'): d.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.render.fps = 30

def collection(name):
    c = bpy.data.collections.new(name); scene.collection.children.link(c); return c
source = collection('01_EDITABLE_PARTS')
game = collection('02_GAME_EXPORT')
studio = collection('03_STUDIO_EXCLUDED_FROM_EXPORT')
current = source
groups = {}

def select(o):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o

def mat(name, color, metal=0, rough=.5, emission=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color, 1); m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal; p.inputs['Roughness'].default_value = rough
    p.inputs['Emission Color'].default_value = (*color, 1); p.inputs['Emission Strength'].default_value = emission
    return m
M = {'bone': mat('Skeleton_AgedBone', (.63,.56,.39), 0,.58),
     'teeth': mat('Skeleton_IvoryTeeth', (.85,.78,.58),0,.45),
     'dark': mat('Skeleton_SocketShadow', (.012,.023,.025),0,.85),
     'eye': mat('Skeleton_GhostFlame', (.08,.85,.48),0,.3,3.5),
     'iron': mat('Skeleton_WornIron', (.12,.17,.18),.8,.48),
     'edge': mat('Skeleton_BladeEdge', (.45,.53,.52),.85,.27),
     'rust': mat('Skeleton_Rust', (.25,.095,.035),.35,.8),
     'leather': mat('Skeleton_Leather', (.075,.042,.027),0,.85),
     'gold': mat('Skeleton_TarnishedBrass', (.38,.26,.09),.7,.46)}

def finish(o, name, material, bone=None, category='Skeleton_Body'):
    o.name = name
    for c in list(o.users_collection): c.objects.unlink(o)
    current.objects.link(o)
    select(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if not o.data.materials: o.data.materials.append(M.get(material, material))
    for p in o.data.polygons: p.use_smooth = True
    if not o.data.uv_layers:
        uv = o.data.uv_layers.new(name='UVMap')
        for loop in o.data.loops:
            v = o.data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv = (v.x * .3 + .5, v.z * .5)
    if bone: o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE')
    if category: groups.setdefault(category, []).append(o)
    return o

def sphere(name,p,s,material='bone',bone=None,category='Skeleton_Body',segments=16,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=p)
    o=bpy.context.object; o.scale=s
    return finish(o,name,material,bone,category)

def cube(name,p,s,material,bone=None,category='Skeleton_Body',bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=bpy.context.object; o.scale=s
    select(o); bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if bevel:
        m=o.modifiers.new('Worn bevel','BEVEL'); m.width=bevel; m.segments=1; bpy.ops.object.modifier_apply(modifier=m.name)
    return finish(o,name,material,bone,category)

def mesh(name,vertices,faces,material,bone=None,category='Skeleton_Body'):
    d=bpy.data.meshes.new(name); d.from_pydata(vertices,[],faces); d.update()
    o=bpy.data.objects.new(name,d); current.objects.link(o)
    bm=bmesh.new(); bm.from_mesh(d); bmesh.ops.recalc_face_normals(bm,faces=bm.faces); bm.to_mesh(d); bm.free()
    return finish(o,name,material,bone,category)

def tube(name,points,radius,material='bone',bone=None,category='Skeleton_Body',sides=7):
    pts=[Vector(p) for p in points]; verts=[]
    for i,p in enumerate(pts):
        tangent=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized()
        q=tangent.to_track_quat('Z','Y')
        for k in range(sides): verts.append(p+q@Vector((radius*cos(k*2*pi/sides),radius*sin(k*2*pi/sides),0)))
    faces=[]
    for i in range(len(pts)-1):
        for k in range(sides):
            a=i*sides+k; b=i*sides+(k+1)%sides; faces.append((a,b,b+sides,a+sides))
    faces += [tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+k for k in range(sides))]
    return mesh(name,verts,faces,material,bone,category)

def limb(name,a,b,r,bone):
    a,b=Vector(a),Vector(b); delta=b-a
    tube(name,[a,a+delta*.15,a+delta*.45,a+delta*.8,b],r,'bone',bone)
    for p in (a,b): sphere(name+'_Joint',p,(r*1.6,r*1.35,r*1.3),'bone',bone,segments=12,rings=8)

bones=[('Root',(0,0,0),(0,0,.18),None),('Pelvis',(0,0,1),(0,0,1.1),'Root'),
       ('Spine',(0,0,1.1),(0,0,1.3),'Pelvis'),('Chest',(0,0,1.3),(0,0,1.5),'Spine'),
       ('Neck',(0,0,1.5),(0,0,1.62),'Chest'),('Head',(0,0,1.62),(0,0,1.84),'Neck'),
       ('Jaw',(0,-.01,1.66),(0,-.09,1.61),'Head')]

# Carved cranium with real recessed orbital cavities and a separate jaw.
skull=sphere('Carved_Cranium',(0,0,1.735),(.115,.095,.135),segments=28,rings=18,category=None)
for side in (-1,1):
    cutter=sphere('TEMP_EyeCut',(side*.051,-.088,1.755),(.045,.06,.043),category=None,segments=24,rings=16)
    select(skull); mod=skull.modifiers.new('Hollow orbital socket','BOOLEAN'); mod.operation='DIFFERENCE'; mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name); bpy.data.objects.remove(cutter,do_unlink=True)
    sphere('Orbital_Interior',(side*.051,-.051,1.755),(.035,.013,.033),'dark','Head')
    sphere('Green_GhostEye',(side*.051,-.074,1.755),(.012,.010,.014),'eye','Head','Skeleton_GhostEyes')
    flame=[(side*.051,-.072,1.755),(side*.057,-.069,1.786),(side*.047,-.067,1.807)]
    tube('EyeFlame',flame,.006,'eye','Head','Skeleton_GhostEyes',5)
    tube('Angry_Brow',[(side*.015,-.105,1.805),(side*.056,-.113,1.801),(side*.097,-.080,1.792)],.012,'bone','Head')
    tube('CheekBone',[(side*.10,-.067,1.74),(side*.104,-.083,1.71),(side*.069,-.10,1.685)],.013,'bone','Head')
    tube('Jaw_Ramus',[(side*.08,-.004,1.70),(side*.078,-.016,1.626),(side*.055,-.09,1.61)],.016,'bone','Jaw')
cutter=mesh('TEMP_Nose',[(-.018,-.17,1.738),(.018,-.17,1.738),(0,-.17,1.696),(-.018,0,1.738),(.018,0,1.738),(0,0,1.696)],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],'dark',category=None)
select(skull); mod=skull.modifiers.new('Nasal opening','BOOLEAN'); mod.operation='DIFFERENCE'; mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name); bpy.data.objects.remove(cutter,do_unlink=True)
skull.vertex_groups.new(name='Head').add(list(range(len(skull.data.vertices))),1,'REPLACE'); groups.setdefault('Skeleton_Body',[]).append(skull)
mesh('Nose_Shadow',[(-.016,-.045,1.735),(.016,-.045,1.735),(0,-.045,1.70)],[(0,2,1)],'dark','Head')
tube('Lower_Jaw_Arc',[(.078*sin(t),-.015-.082*cos(t),1.615) for t in [-pi/2+i*pi/16 for i in range(17)]],.014,'bone','Jaw')
for i in range(7):
    x=(i-3)*.017
    cube('UpperTooth_%d'%i,(x,-.097+abs(x)*.22,1.666),(.014,.025,.024),'teeth','Head',bevel=.002)
    cube('LowerTooth_%d'%i,(x,-.087+abs(x)*.20,1.634),(.013,.022,.020),'teeth','Jaw',bevel=.002)
for z in (1.54,1.57,1.60): sphere('NeckVertebra',(0,.012,z),(.03,.027,.014),'bone','Neck',segments=12,rings=8)

# Open rib cage: individual ribs, clavicles, scapulae and articulated spine.
for i in range(11):
    z=1.075+i*.039; bn='Spine' if z<1.3 else 'Chest'
    sphere('Vertebra_%02d'%i,(0,.045,z),(.029,.032,.016),'bone',bn,segments=12,rings=8)
    tube('SpinousProcess_%02d'%i,[(0,.05,z),(0,.09,z-.006)],.009,'bone',bn)
tube('Sternum',[(0,-.085,1.47),(0,-.103,1.37),(0,-.084,1.24)],.015,'bone','Chest')
for side in (-1,1):
    tube('Clavicle',[(0,-.05,1.49),(side*.09,-.068,1.50),(side*.19,-.015,1.48)],.016,'bone','Chest')
    for i in range(7):
        z=1.45-i*.032; w=.17-(i/7)**2*.06
        pts=[(side*w*sin(t),.015-.115*cos(t),z+.028*sin(t)) for t in [j*pi/18 for j in range(19)]]
        tube('Rib_%d_%d'%(side,i),pts,.011 if i<5 else .0085,'bone','Chest')
    mesh('Scapula',[(side*.045,.078,1.46),(side*.17,.065,1.46),(side*.10,.088,1.32),(side*.045,.09,1.46),(side*.17,.077,1.46),(side*.10,.10,1.32)],[(0,1,2),(3,5,4),(0,3,4,1),(1,4,5,2),(2,5,3,0)],'bone','Chest')
    tube('Iliac_Crest',[(side*.024,.04,1.08),(side*.12,.035,1.11),(side*.16,-.015,1.055),(side*.105,-.07,1.0),(side*.035,-.065,.975)],.024,'bone','Pelvis')
    tube('Pelvic_Arch',[(side*.03,.04,1.03),(side*.12,.015,.97),(side*.08,-.04,.925),(0,-.055,.95)],.018,'bone','Pelvis')

for side,label in [(1,'L'),(-1,'R')]:
    hip=(side*.105,0,.99); knee=(side*.13,-.015,.565); ankle=(side*.145,.015,.13)
    shoulder=(side*.20,0,1.48); elbow=(side*.40,.005,1.20); wrist=(side*.55,-.035,.955); palm=(side*.57,-.04,.89)
    bones.extend([(f'Thigh.{label}',hip,knee,'Pelvis'),(f'Shin.{label}',knee,ankle,f'Thigh.{label}'),
        (f'Foot.{label}',ankle,(side*.145,-.17,.06),f'Shin.{label}'),
        (f'UpperArm.{label}',shoulder,elbow,'Chest'),(f'Forearm.{label}',elbow,wrist,f'UpperArm.{label}'),
        (f'Hand.{label}',wrist,palm,f'Forearm.{label}')])
    limb('Femur',hip,knee,.023,f'Thigh.{label}')
    limb('Tibia',knee,ankle,.017,f'Shin.{label}')
    limb('Fibula',(side*.16,.005,.54),(side*.175,.013,.15),.009,f'Shin.{label}')
    sphere('Patella',(side*.13,-.045,.565),(.025,.018,.033),'bone',f'Shin.{label}')
    sphere('Heel',(side*.145,.02,.07),(.043,.06,.045),'bone',f'Foot.{label}')
    for toe in range(4):
        x=side*.145+(toe-1.5)*.020
        tube('Metatarsal',[(x,.01,.08),(x,-.09,.055),(x,-.16+(toe*.007),.035)],.009,'bone',f'Foot.{label}')
    limb('Humerus',shoulder,elbow,.020,f'UpperArm.{label}')
    limb('Radius',(side*.405,-.006,1.19),wrist,.012,f'Forearm.{label}')
    limb('Ulna',(side*.42,.026,1.18),(side*.567,-.009,.96),.010,f'Forearm.{label}')
    sphere('WristCarpals',wrist,(.027,.022,.027),'bone',f'Hand.{label}')
    for finger in range(4):
        y=-.067+finger*.016; x=side*(.55+finger*.005)
        points=[(x,y,.94),(side*.59,y-.005,.90),(side*.60,y-.012,.875),(side*.58,y-.025,.865)]
        bn=f'Fingers{finger+1}.{label}'; bones.append((bn,points[0],points[1],f'Hand.{label}'))
        tube('Finger_%d_%s'%(finger,label),points,.0065,'bone',bn)
        for p in points[1:-1]: sphere('Knuckle',p,(.009,.009,.009),'bone',bn,segments=10,rings=6)
    tube('Thumb',[(side*.55,-.015,.93),(side*.53,-.053,.90),(side*.565,-.081,.89)],.008,'bone',f'Hand.{label}')

# Separate machete. Grip sits inside the right fist; chipped blade remains one mesh.
grip=Vector((-.58,-.05,.90)); up=Vector((-.38,0,.925)).normalized(); across=Vector((up.z,0,-up.x)); depth=Vector((0,1,0))
def wp(x,y,z): return grip+across*x+depth*y+up*z
tube('Machete_LeatherGrip',[wp(0,0,-.055),wp(0,0,.075)],.019,'leather','Hand.R','Machete_Weapon',10)
for i in range(7):
    z=-.045+i*.016
    tube('GripWrap',[wp(.020*cos(t),.020*sin(t),z) for t in [k*2*pi/12 for k in range(13)]],.003,'gold','Hand.R','Machete_Weapon',5)
tube('Crossguard',[wp(-.065,0,.082),wp(.065,0,.082)],.013,'iron','Hand.R','Machete_Weapon')
outline=[(-.023,.10),(.038,.10),(.054,.28),(.080,.48),(.052,.61),(-.024,.65),(-.047,.59),(-.050,.51),(-.038,.485),(-.052,.467),(-.053,.29)]
verts=[wp(x,y,z) for y in (-.009,.009) for x,z in outline]; n=len(outline)
faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh('Chipped_Machete',verts,faces,'iron','Hand.R','Machete_Weapon')
tube('Blade_HonedEdge',[wp(x,-.010,z) for x,z in [outline[i] for i in range(3,11)]],.004,'edge','Hand.R','Machete_Weapon',5)
for i in range(5):
    z=.16+i*.075
    tube('Blade_Scratch',[wp(-.02,-.0105,z),wp(.024,-.0105,z+.025)],.0014,'rust','Hand.R','Machete_Weapon',4)

# Three armor pieces remain independently switchable, not welded to the body.
def armor_panel(name,center,rx,rz,bone,category):
    cx,cy,cz=center; verts=[]; cols=9; rows=4
    for j in range(rows):
        for k in range(cols):
            t=-1.20+k*2.4/(cols-1)
            bottom=j/(rows-1)
            jag=.015*sin(k*3.7) if j==rows-1 else 0
            verts.append((cx+rx*sin(t),cy-rx*.52*cos(t),cz+rz*(.5-bottom)+jag))
    faces=[]
    for j in range(rows-1):
        for k in range(cols-1):
            a=j*cols+k; faces.append((a,a+1,a+1+cols,a+cols))
    o=mesh(name,verts,faces,'iron',bone,category); select(o)
    mod=o.modifiers.new('Forged metal thickness','SOLIDIFY'); mod.thickness=.009; bpy.ops.object.modifier_apply(modifier=mod.name)
    for k in (1,4,7):
        p=Vector(verts[k]); p.y-=.007
        sphere('Armor_Rivet',p,(.007,.006,.007),'gold',bone,category,segments=10,rings=6)
    for i in range(3):
        p=Vector(verts[cols+i*2+1]); p.y-=.008
        tube('Armor_RustScar',[p,p+Vector((.025,-.001,-.03))],.003,'rust',bone,category,5)
    return o
armor_panel('Battered_LeftPauldron',(.205,-.006,1.465),.12,.16,'UpperArm.L','Armor_Shoulder')
armor_panel('Broken_RightBracer',(-.48,-.025,1.06),.055,.12,'Forearm.R','Armor_Bracer')
armor_panel('Dented_LeftGreave',(.145,-.022,.34),.048,.25,'Shin.L','Armor_Shin')

# Rig every component rigidly to its anatomical bone; skeletons need no soft skin blend.
arm=bpy.data.armatures.new('Skeleton_Shared_DeformRig'); rig=bpy.data.objects.new('Skeleton_Rig',arm); game.objects.link(rig)
select(rig); bpy.ops.object.mode_set(mode='EDIT')
for name,a,b,parent in bones:
    eb=arm.edit_bones.new(name); eb.head=a; eb.tail=b
    if parent: eb.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
exports=[]
for name,objects in groups.items():
    clones=[]
    for o in objects:
        clone=o.copy(); clone.data=o.data.copy(); game.objects.link(clone); clones.append(clone)
    bpy.ops.object.select_all(action='DESELECT')
    for o in clones:o.select_set(True)
    bpy.context.view_layer.objects.active=clones[0]; bpy.ops.object.join(); merged=bpy.context.object; merged.name=name
    merged.parent=rig; mod=merged.modifiers.new('Rigid bone deformation','ARMATURE'); mod.object=rig
    exports.append(merged)
source.hide_render=True; source.hide_viewport=True
rig['front_axis']='Blender -Y, Z up; metres; feet at ground origin'
rig['armor_note']='Optional visual parts, not damage reduction or HP logic'

def action(name, length, frames):
    rig.animation_data_create(); a=bpy.data.actions.new(name); rig.animation_data.action=a; a.use_fake_user=True
    for frame,pose in frames:
        scene.frame_set(frame)
        for pb in rig.pose.bones:
            pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0); pb.location=(0,0,0)
            if pb.name in pose: pb.rotation_euler=[math.radians(v) for v in pose[pb.name]]
            pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=pb.name)
            pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
    return a
idle=action('Skeleton_Idle',90,[(1,{}),(46,{'Chest':(1.8,0,0),'Head':(0,0,-3),'Jaw':(4,0,0)}),(90,{})])
action('Skeleton_Walk',30,[(1,{'Thigh.L':(22,0,0),'Thigh.R':(-22,0,0),'UpperArm.L':(-10,0,0),'UpperArm.R':(10,0,0)}),(8,{'Shin.R':(-25,0,0)}),(16,{'Thigh.L':(-22,0,0),'Thigh.R':(22,0,0),'UpperArm.L':(10,0,0),'UpperArm.R':(-10,0,0)}),(23,{'Shin.L':(-25,0,0)}),(30,{'Thigh.L':(22,0,0),'Thigh.R':(-22,0,0),'UpperArm.L':(-10,0,0),'UpperArm.R':(10,0,0)})])
action('Skeleton_MacheteSlash',36,[(1,{}),(12,{'Chest':(0,0,-20),'UpperArm.R':(-95,0,-25),'Forearm.R':(-55,0,0)}),(19,{'Chest':(12,0,24),'UpperArm.R':(45,0,32),'Forearm.R':(-15,0,0)}),(36,{})])
action('Skeleton_Hit',18,[(1,{}),(5,{'Chest':(-16,0,7),'Head':(-14,0,0),'Jaw':(12,0,0)}),(18,{})])
action('Skeleton_ArmorBreakStun',75,[(1,{}),(15,{'Chest':(12,0,-10),'Head':(8,0,18),'Jaw':(10,0,0)}),(30,{'Chest':(7,0,10),'Head':(6,0,-20),'Jaw':(15,0,0)}),(45,{'Chest':(12,0,-10),'Head':(8,0,18)}),(60,{'Chest':(7,0,10),'Head':(6,0,-20)}),(75,{})])
action('Skeleton_Death',45,[(1,{}),(12,{'Chest':(-20,0,0),'Head':(-20,0,0),'Jaw':(20,0,0)}),(30,{'Root':(-87,0,12),'UpperArm.L':(0,0,-20),'UpperArm.R':(0,0,25),'Jaw':(24,0,0)}),(45,{'Root':(-87,0,12),'UpperArm.L':(0,0,-20),'UpperArm.R':(0,0,25),'Jaw':(24,0,0)})])
rig.animation_data.action=idle; scene.frame_start=1; scene.frame_end=90; scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for o in [rig]+exports:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Skeleton_Machete.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=1,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE')
bpy.ops.export_scene.gltf(filepath=str(OUT/'Skeleton_Machete.glb'),use_selection=True,export_format='GLB',export_animations=False,export_yup=True)

# A clean, separate studio for actual visual review.
current=studio
floor=mat('STUDIO_Ground',(.025,.035,.045),.2,.5)
cube('StudioFloor',(0,0,-.07),(200,200,.10),floor,category=None,bevel=0)
scene.world=bpy.data.worlds.new('StudioWorld'); scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.06,.09,.12,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
for name,pos,power,color,size in [('Key',(-3,-4,5),650,(1,.83,.62),3),('Fill',(3,-2,3),420,(.5,.8,1),2.5),('Rim',(1,3,4),850,(.3,1,.7),2.5)]:
    d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.color=color; d.shape='DISK'; d.size=size
    o=bpy.data.objects.new(name,d); studio.objects.link(o); o.location=pos; o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
def camera(name,pos,target,ortho):
    d=bpy.data.cameras.new(name); o=bpy.data.objects.new(name,d); studio.objects.link(o)
    o.location=pos; o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler(); d.type='ORTHO'; d.ortho_scale=ortho; return o
hero=camera('CAM_Hero',(3,-6,2.8),(-.08,0,.96),2.5)
front=camera('CAM_Front',(0,-7,1.1),(-.10,0,.96),2.25)
rear=camera('CAM_Rear',(-3,6,2.6),(0,0,.96),2.35)
scene.camera=hero; scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences; prefs.compute_device_type='CUDA'; prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='CUDA'
    if any(d.use for d in prefs.devices):scene.cycles.device='GPU'
except Exception:pass
scene.render.resolution_x=1200; scene.render.resolution_y=1400; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'; scene.render.image_settings.file_format='PNG'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'; area.spaces.active.overlay.show_overlays=False
            area.spaces.active.region_3d.view_location=(0,0,.95); area.spaces.active.region_3d.view_distance=3.4
            area.spaces.active.region_3d.view_rotation=hero.rotation_euler.to_quaternion()
select(exports[0])
path=SRC/'Skeleton_Machete.blend'; bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
report={'bones':len(arm.bones),'animations':[a.name for a in bpy.data.actions],'sourceParts':len(source.objects),'meshes':{},'scope':'Reusable skeleton and machete visual prototype. Basic FK animation studies, no combat AI or damage logic.'}
for o in exports:
    report['meshes'][o.name]={'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'unweightedVertices':sum(not v.groups for v in o.data.vertices)}
(DOC/'model-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
manifest=[]
for material in M.values():
    p=material.node_tree.nodes.get('Principled BSDF')
    manifest.append({'name':material.name,'color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'smoothness':1-p.inputs['Roughness'].default_value,'emission':p.inputs['Emission Strength'].default_value})
(DOC/'materials.json').write_text(json.dumps({'materials':manifest},indent=2),encoding='utf-8')
for name,cam in [('01_hero',hero),('02_front',front),('03_rear',rear)]:
    scene.camera=cam; scene.render.filepath=str(DOC/'Renders'/f'{name}.png'); bpy.ops.render.render(write_still=True)
rig.animation_data.action=bpy.data.actions['Skeleton_MacheteSlash']; scene.frame_set(12)
scene.camera=hero; scene.render.filepath=str(DOC/'Renders/04_attack_windup.png'); bpy.ops.render.render(write_still=True)
rig.animation_data.action=idle; scene.frame_set(1); scene.camera=hero
bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
print('SKELETON_MODEL_COMPLETE',json.dumps(report))
