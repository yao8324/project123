"""Re-author the existing in-place rig with continuous curves and staged follow-through.
Run Blender background with Blender/WardenCombatV2/SilentWarden_Combat_v2.blend.
Only the 17 AA_ actions are replaced; meshes, weights and rest skeleton are preserved.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Matrix, Quaternion
ROOT = Path(__file__).resolve().parents[2]
scene = bpy.context.scene
rig = next(o for o in scene.objects if o.type == 'ARMATURE')
scene.render.fps = 30
rig['arm_IK_L'] = rig['arm_IK_R'] = 0.0
rig.animation_data_create()
rig.animation_data.action = None
for track in list(rig.animation_data.nla_tracks): rig.animation_data.nla_tracks.remove(track)
for a in list(bpy.data.actions):
    if a.name.startswith('AA_'): bpy.data.actions.remove(a)
base = {'upper_arm.L':(-9,0,-15),'upper_arm.R':(-7,0,13),'forearm.L':(-18,0,0),'forearm.R':(-12,0,0),'book.cover':(0,-28,0)}
def pose(**kw):
    p=dict(base); p.update(kw); return p
def plus(p, **kw):
    q=dict(p); q.update(kw); return q
def arms(x,z,fore,wing=12):
    return pose(**{'upper_arm.L':(x,0,-z),'upper_arm.R':(x+4,0,z-5),'forearm.L':(fore,0,0),'forearm.R':(fore+5,0,0),'wing.L':(0,0,wing),'wing.R':(0,0,-wing)})
draw=pose(**{'pelvis':(0,0,-5),'spine':(-3,0,-9),'chest':(3,0,-23),'head':(-2,0,12),'upper_arm.R':(18,-16,52),'forearm.R':(-57,0,0),'upper_arm.L':(-24,0,-48),'wing.R':(0,0,-21)})
throw=pose(**{'pelvis':(0,0,5),'spine':(3,0,10),'chest':(4,0,29),'head':(0,0,-13),'upper_arm.R':(-48,-18,105),'forearm.R':(-8,0,0),'hand.R':(-3,0,-10),'upper_arm.L':(-22,0,-33),'wing.R':(0,0,-34)})
summon=pose(**{'pelvis':(0,0,-3),'chest':(-5,0,-12),'head':(-8,0,9),'upper_arm.L':(-12,0,-132),'forearm.L':(-28,0,0),'hand.L':(0,0,-18),'upper_arm.R':(4,-8,35),'forearm.R':(-35,0,0),'wing.L':(0,0,26),'wing.R':(0,0,-22),'book.cover':(0,-90,0)})
edict=plus(throw,**{'upper_arm.L':(-48,0,-93),'forearm.L':(-7,0,0),'hand.L':(-10,0,12),'book.cover':(0,-108,0)})
laser=arms(-15,125,-35,20); laser.update({'spine':(-3,0,0),'chest':(-5,0,0),'head':(-5,0,0),'book.cover':(0,-105,0)})
channel=arms(-26,109,-38,24); channel.update({'chest':(6,0,0),'book.cover':(0,-115,0),'head':(3,0,0)})
low=arms(-13,38,-32,7); low.update({'pelvis':(3,0,0),'chest':(13,0,0),'head':(9,0,0)})
raised=arms(3,122,-29,24); raised.update({'chest':(-6,0,0),'book.cover':(0,-88,0)})
wave=arms(-20,90,-25,24); wave['book.cover']=(0,-75,0)
push=arms(4,48,-10,30); push.update({'chest':(10,0,0),'head':(-7,0,0),'book.cover':(0,-95,0)})
rush=pose(**{'pelvis':(4,0,0),'spine':(10,0,0),'chest':(19,0,-8),'head':(-22,0,7),'upper_arm.L':(-32,0,-70),'forearm.L':(-25,0,0),'upper_arm.R':(18,5,18),'wing.L':(0,-12,-15),'wing.R':(0,12,15)})
rest=plus(base,chest=(3,0,0),head=(2,0,0))
phase=arms(6,105,-15,38); phase.update({'chest':(-10,0,0),'head':(-14,0,0),'book.cover':(0,-115,0)})
death=pose(**{'pelvis':(8,0,8),'spine':(18,0,0),'chest':(28,0,-12),'neck':(15,0,0),'head':(25,0,0),'upper_arm.L':(8,0,-18),'upper_arm.R':(7,0,12),'forearm.L':(6,0,0),'forearm.R':(-10,0,0),'wing.L':(0,-12,-24),'wing.R':(0,12,24)})
# Cubic interpolation retains velocity through internal keys; no per-key stop/start easing.
specs=[
 ('Idle',2.8,True,[(0,base,0),(.25,plus(base,chest=(1,0,-1)),.018),(.5,plus(base,chest=(-1,0,0)),.035),(.75,plus(base,chest=(0,0,1)),.015),(1,base,0)]),
 ('Glide',1.2,True,[(0,plus(base,chest=(7,0,-3)),.02),(.25,plus(base,chest=(8,0,0)),.035),(.5,plus(base,chest=(7,0,3)),.045),(.75,plus(base,chest=(6,0,0)),.03),(1,plus(base,chest=(7,0,-3)),.02)]),
 ('Intro',2.2,False,[(0,plus(base,head=(18,0,0)),0),(.35,plus(base,head=(2,0,-7)),.035),(.72,plus(base,**{'book.cover':(0,-58,0),'upper_arm.L':(-20,0,-52)}),.065),(1,base,0)]),
 ('VolleyWindup',1.25,False,[(0,base,0),(.20,plus(draw,**{'upper_arm.L':(-25,0,-55),'forearm.L':(-45,0,0)}),-.055),(.43,summon,.08),(.66,plus(summon,**{'chest':(-7,0,-9),'hand.L':(0,0,-9)}),.10),(.86,plus(summon,**{'head':(-2,0,5),'forearm.L':(-36,0,0)}),.07),(1,summon,.06)]),
 ('VolleyRelease',1.15,False,[(0,summon,.06),(.18,edict,.015),(.38,plus(edict,**{'chest':(7,0,33),'hand.L':(-4,0,5)}),.04),(.64,plus(rest,**{'chest':(4,0,12),'upper_arm.L':(-23,0,-59),'upper_arm.R':(-17,0,45)}),.015),(1,rest,0)]),
 ('LaserCharge',1.8,False,[(0,base,0),(.25,arms(-32,55,-46,10),-.035),(.62,arms(-22,107,-40,18),.07),(.86,plus(laser,chest=(-7,0,0)),.115),(1,laser,.10)]),
 ('LaserChannel',1.3,False,[(0,laser,.10),(.12,channel,.045),(.36,plus(channel,chest=(3,0,1)),.065),(.65,plus(channel,chest=(6,0,-1)),.05),(1,plus(channel,**{'upper_arm.L':(-28,0,-103),'upper_arm.R':(-22,0,99)}),.04)]),
 ('FractureRaise',1.6,False,[(0,base,0),(.24,low,-.13),(.52,arms(-9,76,-40,15),-.04),(.82,arms(2,114,-32,22),.075),(1,raised,.09)]),
 ('FractureRelease',1.4,True,[(0,raised,.09),(.16,plus(raised,**{'chest':(-10,0,-3),'upper_arm.L':(0,0,-131),'forearm.L':(-20,0,0)}),.13),(.42,plus(raised,**{'chest':(-4,0,2),'upper_arm.R':(7,0,113)}),.075),(.66,plus(raised,**{'chest':(-9,0,3),'upper_arm.R':(7,0,127),'forearm.R':(-22,0,0)}),.12),(.9,plus(raised,chest=(-5,0,-1)),.075),(1,raised,.09)]),
 ('WaveWindup',1.05,False,[(0,base,0),(.34,arms(-25,58,-45,12),-.085),(.76,plus(wave,chest=(-7,0,0)),-.07),(1,wave,-.06)]),
 ('WaveRelease',1.25,True,[(0,wave,-.06),(.16,push,-.14),(.34,plus(push,chest=(7,0,0)),-.11),(.65,arms(-10,62,-25,22),-.04),(.88,plus(wave,chest=(-3,0,0)),-.045),(1,wave,-.06)]),
 ('RushWindup',1.35,False,[(0,base,0),(.28,plus(rush,chest=(6,0,-3)),-.05),(.72,plus(rush,chest=(15,0,-7)),-.15),(1,rush,-.17)]),
 ('RushDrive',.6,True,[(0,rush,-.17),(.25,plus(rush,**{'chest':(22,0,-8),'wing.L':(0,-18,-20),'wing.R':(0,18,20)}),-.14),(.5,plus(rush,chest=(20,0,-6)),-.12),(.75,plus(rush,chest=(18,0,-8)),-.15),(1,rush,-.17)]),
 ('Recovery',1.15,False,[(0,rest,0),(.42,plus(base,chest=(1,0,0)),.018),(1,base,0)]),
 ('Stagger',.65,False,[(0,base,0),(.18,pose(**{'chest':(-17,0,9),'head':(13,0,-4),'upper_arm.L':(5,0,-38)}),-.07),(.55,plus(rest,chest=(7,0,-3)),-.025),(1,rest,0)]),
 ('PhaseBreak',2.2,False,[(0,rest,0),(.2,low,-.11),(.54,phase,.2),(.76,plus(phase,chest=(-7,0,2)),.17),(1,base,0)]),
 ('Defeat',3.2,False,[(0,rest,0),(.17,plus(rest,chest=(-8,0,0)),.04),(.55,death,-.40),(1,death,-.72)])]

def sample(keys,t,name,component=None):
    i=next((i for i in range(len(keys)-1) if t<=keys[i+1][0]),len(keys)-2)
    i=max(0,i); a,b=keys[i],keys[i+1]; t=max(a[0],min(b[0],t)); u=(t-a[0])/(b[0]-a[0])
    def value(k): return k[2] if name=='height' else k[1].get(name,(0,0,0))[component]
    p0=value(a);p1=value(b)
    prev=keys[max(0,i-1)];nxt=keys[min(len(keys)-1,i+2)]
    m0=(value(b)-value(prev))/(b[0]-prev[0]) if i>0 else 0
    m1=(value(nxt)-value(a))/(nxt[0]-a[0]) if i+2<len(keys) else 0
    # Limit cubic overshoot at the extreme arm/staff poses.
    val=(2*u**3-3*u*u+1)*p0+(u**3-2*u*u+u)*m0*(b[0]-a[0])+(-2*u**3+3*u*u)*p1+(u**3-u*u)*m1*(b[0]-a[0])
    return max(min(p0,p1)-abs(p1-p0)*.12,min(max(p0,p1)+abs(p1-p0)*.12,val))

manifest=[];actions=[]
for name,seconds,loop,keys in specs:
    rig.animation_data.action=None;last=round(seconds*30)+1
    for f in range(1,last+1):
        t=(f-1)/(last-1)
        for b in rig.pose.bones:
            b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
            delay=.028 if b.name.startswith('forearm') else .045 if b.name.startswith('hand') else -.018 if b.name in ('pelvis','spine') else 0
            bt=max(0,min(1,t-delay*math.sin(math.pi*t)))
            b.rotation_euler=tuple(math.radians(sample(keys,bt,b.name,c)) for c in range(3))
        strength=0 if name=='Defeat' else (4.5 if 'Rush' in name else 2.2)
        for k in range(8):
            phase=k*math.tau/8
            sway=math.sin(t*math.tau+phase)*strength
            rig.pose.bones['robe%02d'%k].rotation_euler.x=math.radians(sway)
            rig.pose.bones['hem%02d'%k].rotation_euler.x=math.radians(math.sin(t*math.tau+phase-.45)*strength*1.45)
        for side,s in [('L',1),('R',-1)]:
            for k in range(6):
                rig.pose.bones['page%d.%s'%(k,side)].rotation_euler.z=math.radians(s*(k*1.3+math.sin(t*math.tau-k*.45)*2))
        rig.pose.bones['pelvis'].location.y=sample(keys,t,'height')
        bpy.context.view_layer.update()
        staff=rig.pose.bones['staff'];head=staff.head.copy()
        tilt=0
        if name=='VolleyWindup':tilt=-18*math.sin(t*math.pi/2)
        elif name=='VolleyRelease':tilt=-18+55*math.sin(min(1,t/.45)*math.pi/2)
        elif 'Rush' in name:tilt=55 if name=='RushDrive' else 55*t*t*(3-2*t)
        elif name=='Defeat':tilt=35*t
        # Smooth staff tilt with torso-led swing while preserving its hand attachment.
        restq=rig.data.bones['staff'].matrix_local.to_quaternion()
        target=Quaternion((0,1,0),math.radians(tilt))@restq
        staff.matrix=Matrix.Translation(head)@target.to_matrix().to_4x4()
        for b in rig.pose.bones:
            b.keyframe_insert('rotation_euler',frame=f,group=b.name);b.keyframe_insert('location',frame=f,group=b.name)
    a=rig.animation_data.action;a.name='AA_'+name;a.use_fake_user=True;a['loop']=loop
    for layer in a.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:kp.interpolation='LINEAR'
    actions.append(a);manifest.append(dict(name=a.name,seconds=(last-1)/30,loop=loop,frames=last))
rig.animation_data.action=actions[0];scene.frame_set(1);scene.frame_start=1;scene.frame_end=85
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for o in scene.objects:
    if o.type=='MESH':o.select_set(True)
dest=ROOT/'Assets/Art/ForbiddenWarden/CombatV2/Models/SilentWarden_Combat_v2.fbx'
bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='AUTO')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/WardenCombatV2/SilentWarden_Combat_v2.blend'),compress=True)
out=ROOT/'Docs/BossDesign/ArcaneEffects20261027';out.mkdir(parents=True,exist_ok=True)
(out/'AnimationManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(dict(clips=len(actions),bones=len(rig.data.bones),export=str(dest))))
