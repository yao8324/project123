"""Blender 4.3: editable Overlord/Ainz reference study for the Summoner Boss.
Creates new files only. Source parts, deform rig, separate grimoire and game exports.
Run: blender --background --factory-startup --python this_file.py -- PROJECT_ROOT
"""
import bpy, bmesh, math, json, sys, time
from pathlib import Path
from mathutils import Vector, Quaternion
from math import sin, cos, pi

ROOT=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Blender/Bosses/Summoner/OverlordStudy20261106'
EXPORT=ROOT/'Assets/Art/Bosses/Summoner/Models'
DOC=ROOT/'Docs/SummonerModel20261106'
for p in (SOURCE,EXPORT,DOC/'Renders'):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
def collection(name):
    c=bpy.data.collections.new(name);scene.collection.children.link(c);return c
parts=collection('01_EDITABLE_CHARACTER_PARTS')
bookparts=collection('02_EDITABLE_GRIMOIRE_PARTS')
rigcol=collection('03_DEFORM_RIG')
game=collection('04_GAME_EXPORT')
stage=collection('05_STUDIO_NOT_FOR_EXPORT')
current=parts
def link(o,col=None):
    col=col or current
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);return o
def select(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def bake(o):
    select(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
def bind(o,bone):
    if bone and o.type=='MESH':o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE')
    return o
def material(name,color,metal=0,rough=.5,emission=0,texture=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    if texture:
        n=m.node_tree.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=texture;n.inputs['Detail'].default_value=3
        b=m.node_tree.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.18;b.inputs['Distance'].default_value=.012 if texture<80 else .0015
        m.node_tree.links.new(n.outputs['Fac'],b.inputs['Height']);m.node_tree.links.new(b.outputs['Normal'],p.inputs['Normal'])
    return m
M={
 'robe':material('01_Obsidian_Violet_Velvet',(.018,.012,.032),.05,.75,texture=170),
 'purple':material('02_Royal_Amethyst_Lining',(.115,.025,.19),.05,.52,texture=150),
 'gold':material('03_Antique_Gold',(.57,.35,.095),.83,.28,texture=95),
 'goldlight':material('04_Engraved_Gold_Highlights',(.88,.62,.22),.72,.24),
 'bone':material('05_Aged_Ivory_Bone',(.69,.61,.43),.04,.42,texture=22),
 'tooth':material('06_Tooth_Edge_Ivory',(.86,.78,.59),.02,.35),
 'dark':material('07_Orbital_Shadow_And_Iron',(.009,.007,.012),.22,.44),
 'ruby':material('08_Blood_Ruby',(.30,.005,.022),.48,.19,.16),
 'eye':material('09_Crimson_Soul_Eye',(.80,.003,.012),.12,.24,5),
 'leather':material('10_Grimoire_Oxblood_Leather',(.105,.013,.027),.12,.53,texture=90),
 'paper':material('11_Old_Vellum',(.76,.65,.43),0,.78,texture=75),
 'ink':material('12_Grimoire_Ink',(.04,.019,.048),0,.65),
 'rune':material('13_Soul_Rune_Emission',(.12,.52,.37),.2,.27,2.3),
 'violet':material('14_Amethyst_Invocation',(.29,.035,.65),.2,.25,2.1),
}
def finish(o,name,mat,bone=None):
    o.name=name;link(o);o.data.materials.append(M[mat] if isinstance(mat,str) else mat)
    for f in o.data.polygons:f.use_smooth=True
    bake(o);bind(o,bone);return o
def uv(o):
    if o.type=='MESH' and not o.data.uv_layers:
        layer=o.data.uv_layers.new(name='UVMap')
        for poly in o.data.polygons:
            for i in poly.loop_indices:
                v=o.data.vertices[o.data.loops[i].vertex_index].co;layer.data[i].uv=(v.x*.4+.5,v.z*.3)
def mesh(name,verts,faces,mat,bone=None):
    d=bpy.data.meshes.new(name);d.from_pydata(verts,[],faces);d.update();o=bpy.data.objects.new(name,d);current.objects.link(o)
    d.materials.append(M[mat] if isinstance(mat,str) else mat)
    for f in d.polygons:f.use_smooth=True
    bind(o,bone);uv(o);return o
def sphere(name,p,s,mat,bone=None,seg=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=p);o=bpy.context.object;o.scale=s
    return finish(o,name,mat,bone)
def cube(name,p,s,mat,bone=None,bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s;bake(o)
    if bevel:
        mod=o.modifiers.new('Rounded handcrafted edges','BEVEL');mod.width=bevel;mod.segments=2;select(o);bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,mat,bone)
def tube(name,points,radius,mat,bone=None,res=2):
    d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.resolution_u=4;d.bevel_depth=radius;d.bevel_resolution=res
    s=d.splines.new('POLY');s.points.add(len(points)-1)
    for a,p in zip(s.points,points):a.co=(*p,1)
    o=bpy.data.objects.new(name,d);current.objects.link(o);select(o);bpy.ops.object.convert(target='MESH');o=bpy.context.object
    o.data.materials.append(M[mat] if isinstance(mat,str) else mat);bind(o,bone);uv(o)
    for f in o.data.polygons:f.use_smooth=True
    return o
def bezier(a,b,c,d,n=16):
    return [Vector(a)*(1-t)**3+Vector(b)*3*t*(1-t)**2+Vector(c)*3*t*t*(1-t)+Vector(d)*t**3 for t in [i/(n-1) for i in range(n)]]
def rod(name,a,b,r,mat,bone=None):
    a,b=Vector(a),Vector(b);d=b-a;bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r*1.05,radius2=r*.82,depth=d.length,location=(a+b)*.5)
    o=bpy.context.object;o.rotation_mode='QUATERNION';o.rotation_quaternion=d.to_track_quat('Z','Y');return finish(o,name,mat,bone)
def ring(name,p,r,minor,mat,bone=None,axis=(0,0,1),n=48):
    q=Vector(axis).to_track_quat('Z','Y');pts=[Vector(p)+q@Vector((r*cos(i*2*pi/n),r*sin(i*2*pi/n),0)) for i in range(n+1)]
    return tube(name,pts,minor,mat,bone,2)
def solidify(o,thick=.014):
    mod=o.modifiers.new('Real cloth thickness','SOLIDIFY');mod.thickness=thick
    select(o);bpy.ops.object.modifier_apply(modifier=mod.name)

# A fused, carved skull rather than intersecting primitive eyeballs.
skullbits=[sphere('Cranium',(0,.015,3.30),(.252,.205,.296),'bone'),sphere('Upper face',(0,-.112,3.17),(.205,.14,.157),'bone')]
for side in (-1,1):
    skullbits.append(sphere('Zygomatic bone',(side*.19,-.13,3.19),(.08,.105,.105),'bone'))
    skullbits.append(sphere('Temple',(side*.19,.015,3.235),(.082,.16,.15),'bone'))
bpy.ops.object.select_all(action='DESELECT')
for o in skullbits:o.select_set(True)
bpy.context.view_layer.objects.active=skullbits[0];bpy.ops.object.join();skull=bpy.context.object;skull.name='Skull_Carved_Orbital_Anatomy'
rem=skull.modifiers.new('Fuse cranial anatomy','REMESH');rem.mode='VOXEL';rem.voxel_size=.009;rem.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=rem.name)
smooth=skull.modifiers.new('Bone surface relaxation','SMOOTH');smooth.factor=.6;smooth.iterations=4;bpy.ops.object.modifier_apply(modifier=smooth.name)
for side in (-1,1):
    cutter=sphere('Temporary eye socket cutter',(side*.112,-.202,3.272),(.097,.122,.087),'dark',seg=32,rings=20)
    select(skull);mod=skull.modifiers.new('Deep open eye socket','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
noseverts=[(-.038,y,3.18) for y in (-.38,-.12)]+[(.038,y,3.18) for y in (-.38,-.12)]+[(0,y,3.09) for y in (-.38,-.12)]
cutter=mesh('Temporary nasal aperture',noseverts,[(0,2,4),(1,5,3),(0,1,3,2),(2,3,5,4),(4,5,1,0)],'dark')
bm=bmesh.new();bm.from_mesh(cutter.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(cutter.data);bm.free()
select(skull);mod=skull.modifiers.new('Triangular nasal cavity','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
dec=skull.modifiers.new('Game topology reduction','DECIMATE');dec.ratio=.68;bpy.ops.object.modifier_apply(modifier=dec.name);bind(skull,'Head');uv(skull)
mesh('Deep triangular nasal shadow',[(-.030,-.148,3.173),(.030,-.148,3.173),(0,-.148,3.106)],[(0,2,1)],'dark','Head')
for side in (-1,1):
    sphere('Recessed orbital darkness',(side*.113,-.138,3.27),(.080,.045,.069),'dark','Head')
    sphere('Crimson pin light',(side*.113,-.191,3.27),(.017,.014,.018),'eye','Head',20,12)
    tube('Heavy brow ridge',bezier((side*.025,-.228,3.341),(side*.08,-.239,3.36),(side*.18,-.208,3.36),(side*.217,-.164,3.30)),.017,'bone','Head')
    tube('Carved cheek arch',bezier((side*.205,-.19,3.24),(side*.24,-.18,3.19),(side*.20,-.16,3.12),(side*.14,-.21,3.105)),.018,'bone','Head')
    rod('Mandible ramus',(side*.177,-.04,3.17),(side*.159,-.12,3.005),.036,'bone','Jaw')
jawpts=[(.17*sin(t),-.05-.166*cos(t),3.005-.022*cos(t)) for t in [-pi/2+i*pi/24 for i in range(25)]]
tube('Mandible continuous horseshoe',jawpts,.029,'bone','Jaw')
for i in range(9):
    x=(i-4)*.032;y=-.235+abs(x)*.18
    cube('Upper tooth %02d'%i,(x,y,3.088),(.027,.042,.046),'tooth','Head',.006)
    cube('Lower tooth %02d'%i,(x,y+.012,3.037),(.025,.035,.035),'tooth','Jaw',.005)
for z in [2.91,2.95,2.99,3.025]:
    sphere('Cervical vertebra',(0,.014,z),(.061,.057,.022),'bone','Neck',20,10)
rod('Sternum',(0,-.185,2.79),(0,-.225,2.37),.031,'bone','Chest')
rod('Thoracic spine',(0,.08,2.20),(0,.08,2.91),.052,'bone','Chest')
for side in (-1,1):
    tube('Clavicle',bezier((0,-.12,2.83),(side*.14,-.20,2.87),(side*.29,-.13,2.84),(side*.39,-.02,2.80)),.029,'bone','Chest')
    for i in range(6):
        z=2.73-i*.065;width=.275-i*.012
        tube('Rib %d %d'%(side,i),bezier((0,-.206,z),(side*width,-.30,z+.02),(side*(width+.035),.08,z+.035),(side*.065,.12,z+.055),22),.019 if i>3 else .023,'bone','Chest')

# Pleated robe: open front, folded cross sections, physical thickness.
verts=[];faces=[];rows=18;cols=80
for j in range(rows):
    t=j/(rows-1);z=.07+t*1.91;rx=.89*(1-t)+.32*t;ry=.59*(1-t)+.275*t
    for i in range(cols+1):
        a=.24+(2*pi-.48)*i/cols;fold=(.038*(1-t)+.013*t)*cos(a*22+sin(t*pi)*.8)
        verts.append(((rx+fold)*sin(a),-(ry+fold)*cos(a),z+.018*(1-t)*sin(a*11)))
for j in range(rows-1):
    for i in range(cols):
        a=j*(cols+1)+i;faces.append((a,a+1,a+cols+2,a+cols+1))
robe=mesh('Robe_Continuous_22_Folds',verts,faces,'robe','Pelvis');solidify(robe,.018)
# Blend hem motion into four robe controls.
for v in robe.data.vertices:
    z=v.co.z;w=max(0,min(.78,(1.8-z)/1.6));side='L' if v.co.x>0 else 'R';name='Robe_Front.'+side if v.co.y<0 else 'Robe_Back.'+side
    g=robe.vertex_groups.get(name) or robe.vertex_groups.new(name=name);g.add([v.index],w,'REPLACE');robe.vertex_groups['Pelvis'].add([v.index],1-w,'REPLACE')
for s in (-1,1):
    path=[verts[j*(cols+1)+(0 if s==1 else cols)] for j in range(rows)];tube('Robe opening gold cord',path,.012,'gold','Pelvis')
tube('Robe hem brocade',verts[:cols+1],.016,'gold','Pelvis')
panelverts=[]
for j in range(18):
    t=j/17;z=.065+t*1.96;y=-.61*(1-t)-.297*t
    for i in range(13):
        u=(i/12-.5)*2;panelverts.append((u*(.34*(1-t)+.16*t),y-.009*cos(u*3*pi),z))
panel=mesh('Royal_Violet_Front_Tabard',panelverts,[(j*13+i,j*13+i+1,(j+1)*13+i+1,(j+1)*13+i) for j in range(17) for i in range(12)],'purple','Pelvis');solidify(panel,.012)
for s in (-1,1):
    tube('Tabard wide gold border',[(s*(.34*(1-t)+.16*t),-.616*(1-t)-.307*t,.065+t*1.96) for t in [i/24 for i in range(25)]],.022,'gold','Pelvis')
    for i in range(7):
        z=.27+i*.215;t=(z-.065)/1.96;xx=s*(.27*(1-t)+.12*t);y=-.628*(1-t)-.315*t
        tube('Tabard embroidered sigil',[(xx-.035,y,z),(xx,y-.006,z+.049),(xx+.035,y,z),(xx,y-.006,z-.049),(xx-.035,y,z)],.005,'goldlight','Pelvis',1)
# Back cape with sculpted channels; inner lining visible at the perimeter.
for lining in (False,True):
    vs=[];fs=[]
    for j in range(20):
        t=j/19;z=.055+t*2.75;width=1.02*(1-t)+.69*t
        for i in range(49):
            u=i/48*2-1;y=.59*(1-t)+.15*t+.08*cos(u*9*pi)*(1-t)+.12*(1-u*u)
            vs.append((u*width,y+(.014 if not lining else -.018),z+.06*(1-t)*cos(u*4*pi)))
    for j in range(19):
        for i in range(48):
            a=j*49+i;fs.append((a,a+1,a+50,a+49))
    o=mesh('Cape velvet outer' if not lining else 'Cape amethyst lining',vs,fs,'purple' if lining else 'robe','Chest');solidify(o,.014)
    if not lining:
        for i in (0,48):tube('Cape gold edge',[vs[j*49+i] for j in range(20)],.016,'gold','Chest')
        tube('Cape embroidered hem',vs[:49],.018,'gold','Chest')

# High flared amethyst collar, open around the skull.
vs=[];fs=[]
for j in range(7):
    t=j/6
    for i in range(49):
        a=.37+(2*pi-.74)*i/48;r=.285+.21*t;z=2.76+t*(.50+.13*abs(sin(a)))
        vs.append((r*sin(a),-r*cos(a)+.10,z))
for j in range(6):
    for i in range(48):
        a=j*49+i;fs.append((a,a+1,a+50,a+49))
collar=mesh('Flared_Overlord_Collar',vs,fs,'purple','Chest');solidify(collar,.025)
tube('Collar upper gold silhouette',vs[-49:],.022,'gold','Chest')
for i in (0,48):tube('Collar front golden rim',[vs[j*49+i] for j in range(7)],.020,'gold','Chest')

ARM={1:((.51,.03,2.78),(.78,.00,2.46),(1.00,-.40,2.32)), -1:((-.51,.03,2.78),(-.79,-.04,2.45),(-1.00,-.39,2.56))}
bones=[('Root',(0,0,0),(0,0,.35),None),('Pelvis',(0,0,1.76),(0,0,2.05),'Root'),('Spine',(0,0,2.05),(0,0,2.45),'Pelvis'),('Chest',(0,0,2.45),(0,0,2.83),'Spine'),('Neck',(0,.01,2.83),(0,.01,3.04),'Chest'),('Head',(0,.01,3.04),(0,.01,3.56),'Neck'),('Jaw',(0,-.08,3.14),(0,-.20,3.0),'Head')]
for side in (-1,1):
    suffix='L' if side>0 else 'R';shoulder,elbow,wrist=map(Vector,ARM[side]);hand=wrist+Vector((side*.11,-.06,.02 if side>0 else .055))
    bones += [('UpperArm.'+suffix,tuple(shoulder),tuple(elbow),'Chest'),('Forearm.'+suffix,tuple(elbow),tuple(wrist),'UpperArm.'+suffix),('Hand.'+suffix,tuple(wrist),tuple(hand),'Forearm.'+suffix)]
    # Lofted bell sleeve with a hollow purple inner cuff.
    centers=bezier(shoulder,elbow,elbow,wrist,13);axis=(wrist-elbow).normalized();q=axis.to_track_quat('Z','Y');v=[];f=[]
    for j,c in enumerate(centers):
        t=j/(len(centers)-1);r=.17+.115*t**3
        for i in range(33):
            a=i*2*pi/32;fold=.012*cos(a*12)*(t+.3);v.append(tuple(c+q@Vector(((r+fold)*cos(a),(r+fold)*sin(a),0))))
    for j in range(12):
        for i in range(32):
            a=j*33+i;f.append((a,a+1,a+34,a+33))
    sleeve=mesh('Bell sleeve.'+suffix,v,f,'robe','UpperArm.'+suffix);solidify(sleeve,.017)
    for vertex in sleeve.data.vertices:
        d=(vertex.co-elbow).dot(axis);weight=max(0,min(1,.50+d*2));g=sleeve.vertex_groups.get('Forearm.'+suffix) or sleeve.vertex_groups.new(name='Forearm.'+suffix);g.add([vertex.index],weight,'REPLACE');sleeve.vertex_groups['UpperArm.'+suffix].add([vertex.index],1-weight,'REPLACE')
    ring('Wide gold sleeve cuff',wrist,.28,.025,'gold','Forearm.'+suffix,axis)
    ring('Inner violet sleeve cuff',wrist-axis*.026,.248,.026,'purple','Forearm.'+suffix,axis)
    ring('Second engraved cuff',wrist-axis*.085,.254,.009,'goldlight','Forearm.'+suffix,axis)
    # Large wing-shaped black/gold pauldrons and iconic crimson shoulder orbs.
    vs=[];fs=[]
    for j in range(7):
        y=-.25+j*.5/6
        for i in range(13):
            t=i/12;x=side*(.34+.77*t);z=2.79+.08*sin(t*pi)+.40*t**3+.085*cos(j*pi/6)
            vs.append((x,y,z))
    for j in range(6):
        for i in range(12):
            a=j*13+i;fs.append((a,a+1,a+14,a+13))
    pauldron=mesh('Winged pauldron.'+suffix,vs,fs,'dark','UpperArm.'+suffix);solidify(pauldron,.05)
    for j in (0,6):tube('Pauldron sweeping gold blade',[vs[j*13+i] for i in range(13)],.033,'gold','UpperArm.'+suffix)
    for i in (0,12):tube('Pauldron cross rim',[vs[j*13+i] for j in range(7)],.026,'gold','UpperArm.'+suffix)
    for i in (3,6,9):tube('Gold pauldron ribs',[vs[j*13+i] for j in range(7)],.016,'gold','UpperArm.'+suffix)
    sphere('Crimson shoulder soul orb.'+suffix,(side*.65,-.12,2.92),(.184,.175,.184),'ruby','UpperArm.'+suffix,32,18)
    ring('Orb shoulder bezel',(side*.65,-.256,2.92),.133,.016,'gold','UpperArm.'+suffix,(0,1,0))
    # Bones of the hands and articulated finger chains.
    rod('Radius emerging from cuff',wrist,hand,.033,'bone','Hand.'+suffix)
    palm=hand+Vector((side*.025,-.015,.018));sphere('Skeletal palm.'+suffix,palm,(.091,.046,.050 if side>0 else .077),'bone','Hand.'+suffix)
    for finger in range(5):
        offset=(finger-2)*.041
        if side>0:pts=[palm+Vector((offset,-.015,0)),palm+Vector((offset,-.10,.024)),palm+Vector((offset,-.163,.05)),palm+Vector((offset,-.187,.085))]
        else:pts=[palm+Vector((offset,-.025,.035)),palm+Vector((offset*1.40,-.082,.145)),palm+Vector((offset*1.63,-.113,.208)),palm+Vector((offset*1.52,-.15,.228))]
        if finger==0:
            pts=[palm+Vector((-side*.07,-.01,-.015)),palm+Vector((-side*.12,-.06,.02)),palm+Vector((-side*.145,-.13,.05)),palm+Vector((-side*.12,-.17,.08))]
        for k in range(3):
            bn='Finger%d_%d.%s'%(finger,k,suffix);parent='Hand.'+suffix if k==0 else 'Finger%d_%d.%s'%(finger,k-1,suffix)
            bones.append((bn,tuple(pts[k]),tuple(pts[k+1]),parent));rod('Phalanx '+bn,pts[k],pts[k+1],.014 if finger!=0 else .017,'bone',bn)
            sphere('Knuckle '+bn,pts[k],(.019,.018,.019),'bone',bn,12,8)
        if side<0 and finger in (1,3):
            center=pts[1];ring('Sorcerer finger signet',center,.022,.006,'gold','Finger%d_1.%s'%(finger,suffix),pts[2]-pts[1],24)
            sphere('Signet ruby',center+Vector((0,-.017,.006)),(.014,.009,.016),'ruby','Finger%d_1.%s'%(finger,suffix),16,10)

# Red abdominal orb and ornate chain/pectoral jewellery.
sphere('Crimson phylactery heart',(0,-.326,2.04),(.20,.145,.20),'ruby','Pelvis',40,24)
ring('Heart golden setting',(0,-.403,2.04),.20,.026,'gold','Pelvis',(0,1,0))
for i in range(8):
    a=i*2*pi/8;p=Vector((.23*cos(a),-.405,2.04+.23*sin(a)));rod('Phylactery gilded claw',p,p+Vector((-.065*cos(a),-.025,-.065*sin(a))),.015,'gold','Pelvis')
for j in range(3):
    pts=[(.40*sin(a),-.235-.045*cos(a),2.78-.11*j-.15*cos(a)) for a in [-pi/2+i*pi/32 for i in range(33)]]
    tube('Layered golden pectoral chain',pts,.012,'gold','Chest')
for side in (-1,1):
    for i in range(6):
        z=2.74-i*.065;cube('Pectoral square setting',(side*(.33-.018*i),-.24,z),(.062,.024,.045),'gold','Chest',.007)

# Open, thick grimoire, separately skinned to the hand socket.
current=bookparts
book_origin=Vector((1.12,-.57,2.35));book_q=Quaternion((1,0,0),math.radians(25))
def BP(p):return book_origin+book_q@Vector(p)
BOOKBONE='Book_Socket';bones.append((BOOKBONE,tuple(book_origin),tuple(book_origin+Vector((0,0,.26))),'Hand.L'))
def bz(x,y):return .045+abs(x)*.31+.012*cos(y*9)
for side in (-1,1):
    # Genuine top/bottom page volume rather than a single flat rectangle.
    n=14;k=16;top=[]
    for j in range(k+1):
        y=-.30+.60*j/k
        for i in range(n+1):
            x=side*(.018+.43*i/n);top.append(tuple(BP((x,y,bz(x,y)))))
    bottom=[tuple(Vector(p)-book_q@Vector((0,0,.064))) for p in top];v=top+bottom;f=[];count=len(top)
    for j in range(k):
        for i in range(n):
            a=j*(n+1)+i;f.append((a,a+1,a+n+2,a+n+1));f.append((a+count,a+n+1+count,a+n+2+count,a+1+count))
    boundary=list(range(n+1))+[j*(n+1)+n for j in range(1,k+1)]+[k*(n+1)+i for i in range(n-1,-1,-1)]+[j*(n+1) for j in range(k-1,0,-1)]
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):f.append((a,b,b+count,a+count))
    mesh('Grimoire vellum page block '+str(side),v,f,'paper',BOOKBONE)
    cv=[BP((side*x,y,bz(side*x,y)-.083)) for x,y in [(0,-.325),(.48,-.325),(.48,.325),(0,.325)]]
    cover=mesh('Oxblood leather cover '+str(side),cv,[(0,1,2,3)],'leather',BOOKBONE);solidify(cover,.028)
    tube('Book cover golden binding',cv+[cv[0]],.010,'gold',BOOKBONE)
    for end in (-1,1):
        for x in (.06,.425):
            p=BP((side*x,end*.30,bz(side*x,end*.30)-.055));sphere('Bone cover corner',p,(.025,.025,.017),'bone',BOOKBONE,16,8)
    for layer in range(1,9):
        points=[BP((side*(.02+.43*i/24),-.302,bz(side*(.02+.43*i/24),-.302)-layer*.007)) for i in range(25)]
        tube('Visible layered page edges',points,.0016,'ink',BOOKBONE,1)
    # Central arcane diagram and small readable, engraved angular strokes.
    x=side*.245;y=.07
    ring('Page soul-binding circle',BP((x,y,bz(x,y)+.007)),.105,.0038,'rune',BOOKBONE,book_q@Vector((0,0,1)),48)
    ring('Page ink circle',BP((x,y,bz(x,y)+.005)),.075,.002,'ink',BOOKBONE,book_q@Vector((0,0,1)),32)
    for line in range(5):
        yy=-.21+line*.035
        for glyph in range(6):
            xx=side*(.075+glyph*.055);h=.009+.005*((line+glyph)%2)
            pts=[BP((xx-.012,yy,bz(xx,yy)+.009)),BP((xx,yy+h,bz(xx,yy)+.009)),BP((xx+.012,yy,bz(xx,yy)+.009)),BP((xx+.005,yy-h,bz(xx,yy)+.009))]
            tube('Engraved spell glyph',pts,.0019,'ink' if line%2 else 'rune',BOOKBONE,1)
    for i in range(5):
        yy=.205+i*.014;tube('Vellum header ink',[BP((side*.06,yy,bz(side*.06,yy)+.005)),BP((side*.43,yy,bz(side*.43,yy)+.005))],.0016,'ink',BOOKBONE,1)
tube('Rounded leather book spine',[BP((0,y,-.036)) for y in [-.33,-.15,0,.15,.33]],.041,'leather',BOOKBONE)
for y in (-.27,-.13,.13,.27):
    tube('Gold spine band',[BP((x,y,.002-.02*cos(x*30))) for x in [-.045,-.025,0,.025,.045]],.006,'gold',BOOKBONE)
for i in range(17):
    p=BP((.47+.014*sin(i*.45),-.22,.09-i*.028));axis=Vector((0,1,0)) if i%2 else Vector((1,0,0))
    q=axis.to_track_quat('Z','Y');tube('Hanging alternating chain link',[p+q@Vector((.017*cos(a),.025*sin(a),0)) for a in [k*2*pi/14 for k in range(15)]],.004,'gold',BOOKBONE,1)
# A removable invocation effect is exported separately, not fused into the character.
current=parts
spell=collection('06_OPTIONAL_INVOCATION_VFX');current=spell
for side in (-1,1):
    center=book_origin+Vector((0,-.035,.41)) if side>0 else Vector((-1.09,-.51,2.97))
    sphere('Invocation focus',center,(.065,.065,.09),'violet',BOOKBONE if side>0 else 'Hand.R',20,12)
    for k in range(2):
        ring('Invocation fine orbit',center,.12+k*.045,.0038,'rune' if side>0 else 'violet',BOOKBONE if side>0 else 'Hand.R',(0,0,1) if k==0 else (0,1,.35))
current=parts
for side in (-1,1):
    suffix='L' if side>0 else 'R'
    for name,y in [('Front',-.35),('Back',.35)]:bones.append(('Robe_'+name+'.'+suffix,(side*.23,y,1.65),(side*.40,y*1.6,.22),'Pelvis'))
    bones.append(('Cape.'+suffix,(side*.38,.22,2.72),(side*.55,.52,1.45),'Chest'))

# Actual deform armature. Finger, jaw, robe and book controls are all represented.
armdata=bpy.data.armatures.new('Soulbinder_Deform_Skeleton');rig=bpy.data.objects.new('Summoner_Rig',armdata);rigcol.objects.link(rig);select(rig);bpy.ops.object.mode_set(mode='EDIT')
for name,a,b,parent in bones:
    bone=armdata.edit_bones.new(name);bone.head=a;bone.tail=b
    if parent:bone.parent=armdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True;rig.data.display_type='OCTAHEDRAL'
for c in (parts,bookparts,spell):
    for o in c.objects:
        if o.type=='MESH':
            mod=o.modifiers.new('Deform using Summoner_Rig','ARMATURE');mod.object=rig
            o.parent=rig

def merged_export(col,name):
    clones=[]
    for o in col.objects:
        if o.type!='MESH':continue
        n=o.copy();n.data=o.data.copy();game.objects.link(n);n.parent=None;n.matrix_world=o.matrix_world.copy()
        for mod in list(n.modifiers):n.modifiers.remove(mod)
        clones.append(n)
    bpy.ops.object.select_all(action='DESELECT')
    for o in clones:o.select_set(True)
    bpy.context.view_layer.objects.active=clones[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;o.parent=rig
    uv(o);mod=o.modifiers.new('Game deform skin','ARMATURE');mod.object=rig
    return o
body=merged_export(parts,'Soulbinder_Body_Skinned')
grimoire=merged_export(bookparts,'Grimoire_Open_Separate_Prop')
fx=merged_export(spell,'Invocation_Optional_Separate_VFX')
parts.hide_render=True;parts.hide_viewport=True;bookparts.hide_render=True;bookparts.hide_viewport=True;spell.hide_render=True;spell.hide_viewport=True
rig['source_reference']='Overlord / Ainz Ooal Gown; official character art reference, fan-model study'
rig['front_axis']='Blender -Y; Z up; metres';rig['role']='Summoner Boss visual study, not connected to combat yet'
grimoire['attachment']='Book_Socket, parented under Hand.L; separate skinned object'

# Two small inspectable, non-combat animation studies.
for action_name,length,amount in [('Soulbinder_Idle_Study',120,.015),('Soulbinder_Invocation_Study',72,.065)]:
    rig.animation_data_create();rig.animation_data.action=bpy.data.actions.new(action_name)
    for frame,phase in [(1,0),(length//2,1),(length,0)]:
        scene.frame_set(frame)
        for bn,axis,factor in [('Chest',0,1),('Head',2,.45),('Hand.R',1,2),('Book_Socket',0,.7),('Cape.L',0,.6),('Cape.R',0,-.6)]:
            pb=rig.pose.bones[bn];pb.rotation_mode='XYZ';pb.rotation_euler[axis]=amount*phase*factor;pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=bn)
    rig.animation_data.action.use_fake_user=True
rig.animation_data.action=bpy.data.actions.get('Soulbinder_Idle_Study');scene.frame_start=1;scene.frame_end=120;scene.frame_set(1)

# Save game-only FBX and glTF, no pedestal, lights or cameras in either export.
bpy.ops.object.select_all(action='DESELECT')
for o in (rig,body,grimoire):o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'Soulbinder_OverlordStudy.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_step=2,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE',use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(EXPORT/'Soulbinder_OverlordStudy.glb'),use_selection=True,export_format='GLB',export_animations=False,export_yup=True)
lodcol=collection('07_LOD1_EXPORT_ALTERNATIVE');lods=[]
for original in (body,grimoire):
    o=original.copy();o.data=original.data.copy();lodcol.objects.link(o);o.name=original.name+'_Reduced'
    for mod in list(o.modifiers):o.modifiers.remove(mod)
    select(o);mod=o.modifiers.new('LOD1 geometry reduction','DECIMATE');mod.ratio=.42;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=o.modifiers.new('LOD1 skin','ARMATURE');mod.object=rig;lods.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in (rig,*lods):o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'Soulbinder_OverlordStudy_LOD1.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE',use_mesh_modifiers=True)
lodcol.hide_render=True;lodcol.hide_viewport=True
rig.animation_data.action=bpy.data.actions.get('Soulbinder_Idle_Study');scene.frame_set(1)

# Studio presentation is kept in an explicitly excluded collection.
current=stage
stone=material('STUDIO_Basalt',(.028,.034,.045),.32,.43,texture=40)
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=1.20,depth=.10,location=(0,0,-.055));pedestal=finish(bpy.context.object,'Studio turntable pedestal',stone)
bev=pedestal.modifiers.new('Stone chamfer','BEVEL');bev.width=.035;bev.segments=3
ring('Studio gold plinth rim',(0,0,-.008),1.17,.007,'gold')
floor=material('STUDIO_Floor',(.012,.017,.025),.12,.58)
cube('Studio floor',(0,0,-.15),(200,200,.10),floor,bevel=0)
def light(name,p,power,color,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);stage.objects.link(o);o.location=p;o.rotation_euler=(Vector((0,0,1.9))-o.location).to_track_quat('-Z','Y').to_euler()
light('Warm key',(-3.8,-4.8,6.5),1150,(1,.79,.55),4)
light('Cold book fill',(4,-3.3,4.8),900,(.55,.73,1),3)
light('Amethyst rim',(1.5,3.5,5),1550,(.56,.30,1),3)
light('Soft front skull',(-.2,-4,4.5),280,(1,.89,.71),2)
scene.world=bpy.data.worlds.new('Soulbinder studio world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.075,.115,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
def camera(name,p,target,lens=65):
    d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);stage.objects.link(o);o.location=p;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();d.lens=lens;return o
hero=camera('CAM_Hero_ThreeQuarter',(5.9,-10.4,4.9),(.04,0,1.83),89)
front=camera('CAM_Front',(0,-11.2,3.9),(0,0,1.80),85)
rear=camera('CAM_Rear',(-5.7,9,4.3),(0,0,1.8),84)
bookcam=camera('CAM_Grimoire_Detail',(2.7,-3.3,4.4),(1.10,-.55,2.40),76)
scene.camera=hero;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
gpu=False
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='CUDA';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='CUDA';gpu|=d.use
    if gpu:scene.cycles.device='GPU'
except Exception as e:print('GPU fallback:',e)
scene.render.resolution_x=1440;scene.render.resolution_y=1600;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
scene.use_nodes=True;nodes=scene.node_tree.nodes;nodes.clear();a=nodes.new('CompositorNodeRLayers');g=nodes.new('CompositorNodeGlare');g.glare_type='FOG_GLOW';g.quality='HIGH';g.threshold=1.8;g.size=7;c=nodes.new('CompositorNodeComposite');scene.node_tree.links.new(a.outputs['Image'],g.inputs['Image']);scene.node_tree.links.new(g.outputs['Image'],c.inputs['Image'])
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
            area.spaces.active.region_3d.view_distance=6.5;area.spaces.active.region_3d.view_location=(0,0,1.8);area.spaces.active.region_3d.view_rotation=hero.rotation_euler.to_quaternion()
select(body)
blendpath=SOURCE/'Soulbinder_OverlordStudy.blend';bpy.ops.wm.save_as_mainfile(filepath=str(blendpath),compress=True)
report={'name':'Soulbinder — Overlord reference study with open grimoire','reference':'https://overlord-anime.com/assets/character/1c2.webp','blender':bpy.app.version_string,'characterHeightMetres':round(max(v.co.z for v in body.data.vertices)-min(v.co.z for v in body.data.vertices),3),'bones':len(armdata.bones),'sourceObjects':len(parts.objects)+len(bookparts.objects),'gameMeshes':{},'animations':[a.name for a in bpy.data.actions],'gpuRender':gpu,'scope':'Editable fan-character visual study, basic deform rig and motion studies; no Boss AI, hand-authored production retopology, final skinning or Unity runtime validation claimed.'}
for o in (body,grimoire,fx,*lods):
    report['gameMeshes'][o.name]={'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'materialSlots':len(o.data.materials),'uvLayers':len(o.data.uv_layers),'vertexGroups':len(o.vertex_groups)}
(DOC/'model-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for name,cam,w,h in [('01_hero',hero,1440,1600),('02_front',front,1200,1500),('03_rear',rear,1200,1500),('04_grimoire',bookcam,1200,1200)]:
    scene.camera=cam;scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(DOC/'Renders'/f'{name}.png');bpy.ops.render.render(write_still=True)
scene.camera=hero;scene.render.resolution_x=1440;scene.render.resolution_y=1600;scene.render.filepath=str(DOC/'Renders/01_hero.png');bpy.ops.wm.save_as_mainfile(filepath=str(blendpath),compress=True)
print('SOULBINDER_MODEL_COMPLETE',json.dumps(report,ensure_ascii=False))
