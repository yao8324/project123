"""R3 armor construction round: separate plate layers and solid gold borders."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_WingLayout.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel']
bs=next(n for n in ivory.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.31
new=[]
def mesh(n,v,f,m,b,thick=0):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m)
 if thick:mod=o.modifiers.new('Plate thickness','SOLIDIFY');mod.thickness=thick
 bevel=o.modifiers.new('Crafted bevel','BEVEL');bevel.width=.0008;bevel.segments=2
 vg=o.vertex_groups.new(name=b);vg.add(list(range(len(v))),1,'REPLACE');mod=o.modifiers.new('Rigid armor attachment','ARMATURE');mod.object=rig;o.parent=rig;new.append(o);return o
def ribbon(n,pts,width,b,m=gold):
 # Surface strip with real width and thickness, not round gold piping.
 v=[]
 for i,p in enumerate(pts):
  p=Vector(p);d=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(i-1,0)]);t=Vector((-d.z,0,d.x)).normalized()*width*.5;v.extend([p-t,p+t])
 return mesh(n,v,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(pts)-1)],m,b,.002)
def ellipse_band(n,z,rx,ry,cx,cy,b):
 v=[]
 for i in range(65):
  a=i*math.tau/64
  for zz in [z-.006,z+.006]:v.append((cx+rx*math.cos(a),cy+ry*math.sin(a),zz))
 return mesh(n,v,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(64)],gold,b,.002)
# Remove rounded shoulder bubbles and old raised tube chest ornament.
for o in list(s.objects):
 if o.name.startswith(('R03_PauldronLame','R02_ChestGoldLeaf')):bpy.data.objects.remove(o,do_unlink=True)
for sign,side in [(1,'R'),(-1,'L')]:
 bone='UpperArm.'+side
 for j in range(3):
  v=[];rows=7;cols=17
  for a in range(rows):
   t=a/(rows-1);x=sign*(.29+.22*t);crest=1.88-j*.063+.035*math.sin(t*math.pi)
   for k in range(cols):
    angle=-math.pi/2+math.pi*k/(cols-1);y=.19*math.sin(angle);z=crest-.095*(1-math.cos(angle))-.07*t
    v.append((x,y,z))
  faces=[(a*cols+k,a*cols+k+1,(a+1)*cols+k+1,(a+1)*cols+k) for a in range(rows-1) for k in range(cols-1)]
  mesh('R03_LayeredShoulderShell',v,faces,ivory,bone,.004)
  for k in [0,cols-1]:
   pts=[Vector(v[a*cols+k])+Vector((0,.001 if k else -.001,.002)) for a in range(rows)];ribbon('R03_ShoulderGoldEdge',pts,.012,bone)
  # Along outer cut, narrow gold surface band follows the shell curvature.
  vv=[]
  for k in range(cols):
   vv.extend([Vector(v[(rows-1)*cols+k])+Vector((0,0,.002)),Vector(v[(rows-1)*cols+k])+Vector((-sign*.012,0,.002))])
  mesh('R03_ShoulderOuterGoldBand',vv,[(2*k,2*k+1,2*k+3,2*k+2) for k in range(cols-1)],gold,bone,.002)
 # Vambrace and greave rims follow their elliptical plate profiles.
 for z,rx,ry in [(1.265,.077,.077),(1.475,.101,.101)]:ellipse_band('R03_VambraceGiltRim',z,rx,ry,sign*.575,.055,'Forearm.'+side)
 for z,rx,ry in [(.17,.082,.088),(.565,.11,.122)]:ellipse_band('R05_GreaveGiltRim',z,rx,ry,sign*.18,0,'Shin.'+side)
 ribbon('R05_GreaveCentralRidge',[(sign*.18,.09,.17),(sign*.18,.122,.48),(sign*.18,.124,.565)],.014,'Shin.'+side)
 for j in range(4):
  x=sign*(.26+j*.015);z=1.18-j*.07
  ribbon('R05_TassetGoldHem',[(x-.079,.153,z-.044),(x+.079,.153,z-.044)],.010,'Pelvis')
 # Raised tapered elbow side plate, separate from spherical hinge.
 x=sign*.54
 mesh('R03_ElbowArticulatedWing',[(x,.08,1.58),(x+sign*.12,.045,1.52),(x+sign*.09,.04,1.43),(x,.08,1.46)],[(0,1,2,3)],ivory,'Forearm.'+side,.004)
 ribbon('R03_ElbowWingGold',[(x,.083,1.58),(x+sign*.12,.048,1.52),(x+sign*.09,.043,1.43)],.012,'Forearm.'+side)
 # Helmet cheek contour at visor edges.
 ribbon('R01_CheekGoldBorder',[(sign*.10,.137,2.14),(sign*.125,.123,2.06),(sign*.12,.11,2.00),(sign*.075,.12,1.94)],.010,'Head')
# Chest leaf has surface-following branches with tapered tips.
for sign in [-1,1]:
 ribbon('R02_CentralLeafBorder',[(0,.157,1.33),(sign*.08,.184,1.50),(sign*.16,.185,1.64),(sign*.22,.166,1.75)],.014,'Spine')
 for j in range(4):
  z=1.47+j*.065;x=sign*(.04+j*.034);y=.196 if j<3 else .183
  ribbon('R02_LeafVein',[(x,y,z),(x+sign*.045,y-.009,z+.025),(x+sign*.07,y-.021,z+.065)],.008,'Spine')
for z,rx,ry in [(1.315,.225,.154),(1.855,.275,.149)]:ellipse_band('R02_ChestGoldBoundary',z,rx,ry,0,0,'Spine')
for j in range(3):ellipse_band('R02_WaistLameRim',1.205+j*.06,.234+j*.012,.154,0,0,'Pelvis')
# Shield three shallow reinforcing ribs on the convex front.
for dx in [-.15,0,.15]:
 ribbon('S02_ShieldReinforcingRib',[(-.66+dx,.285,1.68),(-.66+dx*.8,.355,1.18),(-.66+dx*.3,.28,.72)],.010,'Forearm.L',steel)
# Ensure all added detail is skinned to a valid existing bone; inspect evaluated dimensions separately.
assert all(o.vertex_groups and o.vertex_groups[0].name in rig.data.bones for o in new)
bpy.context.view_layer.update();s['armor_round']='Layered pauldrons, solid gilt edging, elbow wings, chest leaves, shield ribs. Not final acceptance.'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Armor.blend'))
(P/'Docs/SanctuaryR3Build/ArmorRound.json').write_text(json.dumps({'added_objects':len(new),'rig_bones':len(rig.data.bones),'gilt_strip_width_m':[.008,.014],'gilt_thickness_m':.002,'ivory_metallic':0,'pending':'Full silhouette against master, clipping under all poses, plate curvature, hands/grip, knee details, cloth, final dimensions and all-part >=90 evaluation.'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1200;s.render.resolution_y=1400;s.render.resolution_percentage=100
# Close view keeps the face, chest and shoulders readable; wing layout remains unchanged.
s.camera.location=(2.4,5,2.35);s.camera.rotation_euler=(Vector((0,0,1.5))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.45;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Armor_Round2.png');bpy.ops.render.render(write_still=True)
print('ARMOR_ROUND_COMPLETE',len(new))
