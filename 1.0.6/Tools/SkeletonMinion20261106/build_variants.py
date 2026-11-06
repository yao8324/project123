"""Derive the other three skeleton classes, a winged translucent undead, and props.
Uses the local shared skeleton; no downloaded models or edits to the Boss source.
"""
import bpy, ast, sys, json, math, bmesh
from pathlib import Path
from mathutils import Vector, Matrix
from math import sin, cos, pi
bpy.context.preferences.filepaths.save_version=0
ROOT=Path(sys.argv[sys.argv.index('--')+1])
DOC=ROOT/'Docs/SkeletonMinion20261106'; SRC=ROOT/'Blender/Minions/Skeleton20261106'
OUT=ROOT/'Assets/Art/Minions/Skeleton/Models'
source_path=SRC/'Skeleton_Machete.blend'
helper=ast.parse((ROOT/'Tools/SkeletonMinion20261106/build_minion.py').read_text(encoding='utf-8'))
exec(compile(ast.Module(body=[n for n in helper.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'<local_geometry_helpers>','exec'))
reports={}

def setup():
    global scene,source,game,current,groups,M,rig,studio
    bpy.ops.wm.open_mainfile(filepath=str(source_path))
    scene=bpy.context.scene; source=bpy.data.collections['01_EDITABLE_PARTS'];game=bpy.data.collections['02_GAME_EXPORT'];studio=bpy.data.collections['03_STUDIO_EXCLUDED_FROM_EXPORT']
    current=source;groups={};rig=bpy.data.objects['Skeleton_Rig'];rig.animation_data.action=bpy.data.actions['Skeleton_Idle'];scene.frame_set(1)
    M={k:bpy.data.materials[v] for k,v in {'bone':'Skeleton_AgedBone','teeth':'Skeleton_IvoryTeeth','dark':'Skeleton_SocketShadow','eye':'Skeleton_GhostFlame','iron':'Skeleton_WornIron','edge':'Skeleton_BladeEdge','rust':'Skeleton_Rust','leather':'Skeleton_Leather','gold':'Skeleton_TarnishedBrass'}.items()}

def merge_new():
    result=[]
    for name,items in groups.items():
        clones=[]
        for o in items:
            n=o.copy();n.data=o.data.copy();game.objects.link(n);clones.append(n)
        bpy.ops.object.select_all(action='DESELECT')
        for o in clones:o.select_set(True)
        bpy.context.view_layer.objects.active=clones[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
        o.parent=rig;mod=o.modifiers.new('Bone skin','ARMATURE');mod.object=rig;result.append(o)
    return result

def export(name,meshes,blend=True):
    bpy.ops.object.select_all(action='DESELECT')
    for o in [rig]+meshes:o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE')
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),use_selection=True,export_format='GLB',export_animations=False)
    reports[name]={'bones':len(rig.data.bones),'meshes':[o.name for o in meshes],'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'animations':[a.name for a in bpy.data.actions]}
    if blend:bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(name+'.blend')),compress=True)

def render(name):
    scene.camera=bpy.data.objects['CAM_Hero'];scene.render.filepath=str(DOC/'Renders'/f'{name}.png');bpy.ops.render.render(write_still=True)

setup()
# Archer: left-hand bow, back quiver and six arrows are separate export meshes.
bowpts=[(.58,-.05,.48),(.58,-.17,.63),(.58,-.21,.80),(.58,-.05,.90),(.58,-.21,1.03),(.58,-.17,1.23),(.58,-.05,1.36)]
tube('RecurveBow',bowpts,.016,'leather','Hand.L','Archer_Bow',10)
tube('BowString',[bowpts[0],bowpts[-1]],.002,'teeth','Hand.L','Archer_Bow',5)
tube('BowGrip',[(.58,-.05,.85),(.58,-.05,.96)],.022,'gold','Hand.L','Archer_Bow')
tube('LeatherQuiver',[(.10,.19,1.18),(.10,.19,1.56)],.065,'leather','Chest','Archer_Quiver',12)
for z in (1.20,1.51):
    tube('QuiverBand',[(.10+.067*cos(t),.19+.067*sin(t),z) for t in [i*2*pi/16 for i in range(17)]],.006,'gold','Chest','Archer_Quiver')
for i in range(6):
    x=.10+(i%3-1)*.027;y=.19+(i//3-.5)*.035;top=1.69+(i%2)*.035
    tube('QuiverArrow',[(x,y,1.25),(x,y,top)],.004,'leather','Chest','Archer_Quiver',6)
    mesh('ArrowFletching',[(x,y,top-.09),(x-.018,y,top-.035),(x,y,top),(x+.018,y,top-.035)],[(0,1,2),(0,2,3)],'teeth','Chest','Archer_Quiver')
tube('QuiverChestStrap',[(.12,.18,1.49),(-.15,-.09,1.42),(.12,-.11,1.18),(.12,.18,1.25)],.012,'leather','Chest','Archer_Quiver')
action('Skeleton_ArcherDraw_Study',90,[(1,{}),(22,{'UpperArm.R':(-50,0,-15),'Forearm.R':(-65,0,0)}),(55,{'UpperArm.L':(-65,0,20),'Forearm.L':(-8,0,0),'UpperArm.R':(-60,0,-30),'Forearm.R':(-100,0,0),'Head':(0,0,-8)}),(76,{'UpperArm.L':(-65,0,20),'UpperArm.R':(-45,0,-35),'Forearm.R':(-70,0,0)}),(90,{})])
rig.animation_data.action=bpy.data.actions['Skeleton_Idle'];scene.frame_set(1)
bpy.data.objects['Machete_Weapon'].hide_render=True;bpy.data.objects['Machete_Weapon'].hide_set(True)
new=merge_new();base=[bpy.data.objects[n] for n in ['Skeleton_Body','Skeleton_GhostEyes','Armor_Shoulder','Armor_Bracer','Armor_Shin']]
export('Skeleton_Archer',base+new);render('06_archer')

setup()
# Hammer variant: re-fit the rest arms to a central two-handed grip.
old={b.name:b.matrix_local.copy() for b in rig.data.bones}
select(rig);bpy.ops.object.mode_set(mode='EDIT')
for side,label,gripz in [(1,'L',.98),(-1,'R',1.12)]:
    shoulder=Vector((side*.20,0,1.48));elbow=Vector((side*.28,-.18,1.22));wrist=Vector((side*.022,-.23,gripz));palm=wrist+Vector((0,0,-.065))
    for name,a,b in [(f'UpperArm.{label}',shoulder,elbow),(f'Forearm.{label}',elbow,wrist),(f'Hand.{label}',wrist,palm)]:
        eb=rig.data.edit_bones[name];eb.head=a;eb.tail=b
    delta=wrist-Vector((side*.55,-.035,.955))
    for i in range(4):
        eb=rig.data.edit_bones[f'Fingers{i+1}.{label}'];eb.head+=delta;eb.tail+=delta
bpy.ops.object.mode_set(mode='OBJECT')
deltas={name:rig.data.bones[name].matrix_local@matrix.inverted() for name,matrix in old.items()}
for o in list(source.objects)+[o for o in game.objects if o.type=='MESH']:
    for v in o.data.vertices:
        if v.groups:
            name=o.vertex_groups[v.groups[0].group].name
            if name in deltas:v.co=deltas[name]@v.co
tube('LongHammerHaft',[(0,-.23,.68),(0,-.23,1.64)],.024,'leather','Hand.R','Hammer_Weapon',10)
for z in (.72,.94,1.08,1.42):
    tube('HammerGripBand',[(.026*cos(t),-.23+.026*sin(t),z) for t in [k*2*pi/16 for k in range(17)]],.006,'gold','Hand.R','Hammer_Weapon')
cube('HammerBlock',(0,-.23,1.61),(.45,.24,.24),'iron','Hand.R','Hammer_Weapon',bevel=.025)
for x in (-.235,.235):cube('HammerStrikePlate',(x,-.23,1.61),(.045,.27,.27),'edge','Hand.R','Hammer_Weapon',bevel=.012)
for x in (-.12,0,.12):cube('HammerHeadBand',(x,-.23,1.61),(.035,.255,.255),'gold','Hand.R','Hammer_Weapon',bevel=.004)
action('Skeleton_HammerSlam_Study',90,[(1,{}),(28,{'UpperArm.R':(-100,0,-8),'UpperArm.L':(-95,0,8),'Chest':(-8,0,0)}),(65,{'UpperArm.R':(-115,0,-8),'UpperArm.L':(-105,0,8),'Chest':(-12,0,0)}),(76,{'Chest':(40,0,0),'UpperArm.R':(12,0,0),'UpperArm.L':(12,0,0)}),(90,{})])
rig.animation_data.action=bpy.data.actions['Skeleton_Idle'];scene.frame_set(1)
rig.scale=(1.35,1.20,1.13)
bpy.data.objects['Machete_Weapon'].hide_render=True;bpy.data.objects['Machete_Weapon'].hide_set(True)
new=merge_new();base=[bpy.data.objects[n] for n in ['Skeleton_Body','Skeleton_GhostEyes','Armor_Shoulder','Armor_Bracer','Armor_Shin']]
export('Skeleton_Hammer',base+new);render('07_hammer')

setup()
# Sword-and-shield: worn kite shield with a grip side, separate straight blade.
outline=[(.58,-.16,1.30),(.78,-.16,1.20),(.76,-.16,.94),(.58,-.16,.73),(.40,-.16,.94),(.38,-.16,1.20)]
back=[(x,y+.05,z) for x,y,z in outline];n=len(outline)
faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh('KiteShield',outline+back,faces,'iron','Hand.L','Shield_Prop')
tube('ShieldRim',outline+[outline[0]],.012,'gold','Hand.L','Shield_Prop')
cube('ShieldBoss',(.58,-.20,1.08),(.13,.06,.16),'edge','Hand.L','Shield_Prop',bevel=.025)
tube('ShieldGrip',[(.54,-.08,.88),(.62,-.08,.88)],.015,'leather','Hand.L','Shield_Prop')
tube('ShieldCross',[(.58,-.19,.84),(.58,-.19,1.26)],.009,'gold','Hand.L','Shield_Prop')
tube('ShieldCrossbar',[(.44,-.19,1.13),(.72,-.19,1.13)],.009,'gold','Hand.L','Shield_Prop')
grip=Vector((-.58,-.05,.90)); up=Vector((-.3,0,.954)); across=Vector((up.z,0,-up.x))
def sw(x,y,z):return grip+across*x+Vector((0,y,0))+up*z
tube('SwordGrip',[sw(0,0,-.06),sw(0,0,.08)],.018,'leather','Hand.R','Sword_Weapon')
tube('SwordGuard',[sw(-.09,0,.085),sw(.09,0,.085)],.012,'gold','Hand.R','Sword_Weapon')
vertices=[sw(x,y,z) for y in (-.008,.008) for x,z in [(-.027,.10),(.027,.10),(.024,.62),(0,.72),(-.024,.62)]]
mesh('SwordBlade',vertices,[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],'edge','Hand.R','Sword_Weapon')
action('Skeleton_ShieldGuard_Study',45,[(1,{}),(12,{'UpperArm.L':(-45,0,15),'Forearm.L':(-30,0,0),'Chest':(0,0,-8)}),(33,{'UpperArm.L':(-45,0,15),'Forearm.L':(-30,0,0),'Chest':(0,0,-8)}),(45,{})])
rig.animation_data.action=bpy.data.actions['Skeleton_Idle'];scene.frame_set(1)
bpy.data.objects['Machete_Weapon'].hide_render=True;bpy.data.objects['Machete_Weapon'].hide_set(True)
new=merge_new();base=[bpy.data.objects[n] for n in ['Skeleton_Body','Skeleton_GhostEyes','Armor_Shoulder','Armor_Bracer','Armor_Shin']]
export('Skeleton_SwordShield',base+new);render('08_swordshield')

# Winged undead: all-new low-hover silhouette, translucent skin and separate fireball.
setup()
for o in list(game.objects):bpy.data.objects.remove(o,do_unlink=True)
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
M['ghost']=mat('Undead_TranslucentSkin',(.11,.36,.43),0,.38,.25)
M['membrane']=mat('Undead_TranslucentWing',(.08,.22,.32),0,.5,.08)
M['fire']=mat('Undead_SoulFire',(.40,.08,.95),0,.28,4)
for key,alpha in [('ghost',.48),('membrane',.32)]:
    material=M[key];material.diffuse_color=(*material.diffuse_color[:3],alpha)
    material.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=alpha
    material.surface_render_method='DITHERED'
current=source;groups={}
sphere('UndeadTorso',(0,0,1.02),(.20,.12,.30),'ghost','Chest','Undead_Body',segments=20,rings=14)
sphere('UndeadCranium',(0,-.01,1.47),(.16,.12,.18),'ghost','Head','Undead_Body',segments=24,rings=16)
for side in (-1,1):
    label='L' if side>0 else 'R'
    sphere('GhostSocket',(side*.068,-.113,1.48),(.055,.018,.047),'dark','Head','Undead_Eyes')
    sphere('GhostEye',(side*.068,-.132,1.48),(.022,.010,.023),'eye','Head','Undead_Eyes')
    tube('GhostBrow',[(side*.02,-.13,1.535),(side*.075,-.133,1.53),(side*.13,-.1,1.54)],.015,'ghost','Head','Undead_Body')
    tube('GhostHorn',[(side*.12,.0,1.58),(side*.19,.02,1.72),(side*.16,.06,1.82)],.025,'ghost','Head','Undead_Body')
    tube('GhostArm',[(side*.16,0,1.16),(side*.30,-.03,1.0),(side*.34,-.14,.86)],.043,'ghost',f'Arm.{label}','Undead_Body',10)
    sphere('GhostClawPalm',(side*.34,-.14,.84),(.055,.045,.07),'ghost',f'Hand.{label}','Undead_Body')
    for i in range(3):
        x=side*.34+(i-1)*.032
        tube('GhostFinger',[(x,-.16,.82),(x,-.21,.75),(x,-.23,.79)],.009,'ghost',f'Hand.{label}','Undead_Body')
    wing=[(side*.13,.06,1.2),(side*.36,.07,1.63),(side*.64,.09,1.82),(side*.79,.12,1.28),(side*.61,.10,1.37),(side*.54,.10,1.06),(side*.40,.09,1.20),(side*.28,.08,.99)]
    verts=[(side*.29,.10,1.30)]+wing
    mesh('BatWingMembrane',verts,[(0,i+1,(i+1)%len(wing)+1) for i in range(len(wing))],'membrane',f'Wing.{label}','Undead_Wings')
    for i in (1,2,3,5,7):tube('WingFinger',[(side*.13,.06,1.2),wing[i]],.012,'ghost',f'Wing.{label}','Undead_Body')
    tube('WingLeadingEdge',wing[:4],.020,'ghost',f'Wing.{label}','Undead_Body')
    mesh('Fang',[(side*.058,-.124,1.36),(side*.035,-.132,1.36),(side*.047,-.15,1.30)],[(0,1,2)],'teeth','Head','Undead_Body')
tube('GhostMouth',[(-.08,-.12,1.38),(0,-.14,1.35),(.08,-.12,1.38)],.012,'dark','Head','Undead_Eyes')
tube('GhostTail',[(0,.03,.85),(0,.10,.61),(.04,.12,.43),(.14,.15,.35)],.055,'ghost','Tail','Undead_Body',10)
ghostbones=[('Root',(0,0,0),(0,0,.2),None),('Chest',(0,0,.9),(0,0,1.26),'Root'),('Head',(0,0,1.27),(0,0,1.64),'Chest'),('Tail',(0,0,.9),(.1,.1,.35),'Chest')]
for side,label in [(1,'L'),(-1,'R')]:ghostbones.extend([(f'Arm.{label}',(side*.16,0,1.16),(side*.34,-.14,.86),'Chest'),(f'Hand.{label}',(side*.34,-.14,.86),(side*.34,-.19,.78),f'Arm.{label}'),(f'Wing.{label}',(side*.13,.06,1.2),(side*.64,.09,1.82),'Chest')])
arm=bpy.data.armatures.new('UndeadDeformRig');rig=bpy.data.objects.new('Undead_Rig',arm);game.objects.link(rig);select(rig);bpy.ops.object.mode_set(mode='EDIT')
for name,a,b,parent in ghostbones:
    eb=arm.edit_bones.new(name);eb.head=a;eb.tail=b
    if parent:eb.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
new=merge_new()
idle=action('Undead_Hover',60,[(1,{'Wing.L':(0,0,-12),'Wing.R':(0,0,12)}),(30,{'Chest':(3,0,0),'Wing.L':(0,0,15),'Wing.R':(0,0,-15),'Tail':(4,0,12)}),(60,{'Wing.L':(0,0,-12),'Wing.R':(0,0,12)})])
action('Undead_ThrowFireball',36,[(1,{}),(15,{'Arm.R':(-60,0,-20),'Head':(0,0,-10)}),(22,{'Arm.R':(-110,0,5),'Chest':(10,0,0)}),(36,{})])
action('Undead_Hit',18,[(1,{}),(6,{'Chest':(-20,0,8),'Head':(-15,0,0)}),(18,{})])
action('Undead_Death',40,[(1,{}),(20,{'Chest':(40,0,0),'Wing.L':(0,0,45),'Wing.R':(0,0,-45)}),(40,{'Root':(85,0,0),'Wing.L':(0,0,65),'Wing.R':(0,0,-65)})])
rig.animation_data.action=idle;scene.frame_set(1)
ghostout=ROOT/'Assets/Art/Minions/Undead/Models';ghostout.mkdir(parents=True,exist_ok=True)
oldout=OUT;OUT=ghostout;export('Undead_Winged',new,False);OUT=oldout
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Undead_Winged.blend'),compress=True);render('09_undead')
extra=[]
for key in ('ghost','membrane','fire'):
    material=M[key];p=material.node_tree.nodes.get('Principled BSDF')
    extra.append({'name':material.name,'color':list(material.diffuse_color),'metallic':0,'smoothness':1-p.inputs['Roughness'].default_value,'emission':p.inputs['Emission Strength'].default_value})
manifest=json.loads((DOC/'materials.json').read_text());manifest['materials']+=extra
(DOC/'materials-all.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

# Standalone projectiles and magic dagger, all centered at their own origins.
setup();props=ROOT/'Assets/Art/Minions/Projectiles/Models';props.mkdir(parents=True,exist_ok=True)
current=collection('04_PROJECTILE_EDITABLE_PARTS');groups={}
M['violet']=mat('Projectile_VioletSoul',(.38,.04,.72),.1,.32,2)
M['fire']=mat('Undead_SoulFire',(.40,.08,.95),0,.28,4)
def static_export(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(props/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS')
    reports[name]={'meshes':len(objects),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)}
tube('ArrowShaft',[(0,0,-.32),(0,0,.30)],.004,'leather',category='Arrow')
mesh('Arrowhead',[(-.022,0,.29),(.022,0,.29),(0,0,.36),(0,.008,.29)],[(0,1,2),(0,2,3),(1,3,2),(0,3,1)],'edge',category='Arrow')
for yaxis in (False,True):
    verts=[(-.022,0,-.28),(0,0,-.32),(.022,0,-.28),(0,0,-.19)]
    if yaxis:verts=[(y,x,z) for x,y,z in verts]
    mesh('ArrowFeathers',verts,[(0,1,3),(1,2,3)],'teeth',category='Arrow')
static_export('Skeleton_Arrow',groups['Arrow'])
sphere('SoulFireball',(0,0,0),(.14,.14,.14),'fire',category='Fireball',segments=16,rings=10)
for i in range(3):
    tube('FireballTrail',[(.06*cos(i*2*pi/3),.06*sin(i*2*pi/3),-.05),(.10*cos(i*2*pi/3),.10*sin(i*2*pi/3),-.20),(0,0,-.38)],.018,'violet',category='Fireball')
static_export('Undead_Fireball',groups['Fireball'])
# Extract just the skull and jaw from the shared body; no giant model dependency.
body=bpy.data.objects['Skeleton_Body'];keep=[];faces=[];mapping={}
for v in body.data.vertices:
    if v.groups and body.vertex_groups[v.groups[0].group].name in {'Head','Jaw'}:
        mapping[v.index]=len(keep);keep.append((v.co-Vector((0,0,1.735)))*1.7)
for p in body.data.polygons:
    if all(i in mapping for i in p.vertices):faces.append(tuple(mapping[i] for i in p.vertices))
mesh('PurpleSkullProjectile',keep,faces,'violet',category='SkullProjectile')
static_export('Boss_PurpleSkull',groups['SkullProjectile'])
tube('MagicDaggerGrip',[(0,0,-.09),(0,0,.03)],.016,'iron',category='MagicDagger')
tube('MagicDaggerGuard',[(-.065,0,.04),(.065,0,.04)],.01,'gold',category='MagicDagger')
mesh('MagicDaggerBlade',[(-.035,0,.055),(.035,0,.055),(0,0,.37),(0,-.012,.11),(0,.012,.11)],[(0,3,2),(3,1,2),(0,2,4),(4,2,1),(0,4,1,3)],'violet',category='MagicDagger')
static_export('Boss_MagicDagger',groups['MagicDagger'])
# Keep only the projectile collection visible in the prop source.
for col in (source,game,studio):col.hide_render=True;col.hide_viewport=True
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Undead_Projectiles.blend'),compress=True)
manifest=json.loads((DOC/'materials-all.json').read_text());p=M['violet'].node_tree.nodes.get('Principled BSDF')
manifest['materials'].append({'name':M['violet'].name,'color':list(M['violet'].diffuse_color),'metallic':.1,'smoothness':.68,'emission':2})
(DOC/'materials-all.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(DOC/'variants-report.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('ALL_MINION_VARIANTS_COMPLETE')
