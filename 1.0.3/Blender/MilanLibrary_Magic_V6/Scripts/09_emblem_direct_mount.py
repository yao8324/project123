"""User-authorized direct curved-wall mounting of the supplied emblem and lettering."""
import bpy,bmesh,json,runpy,math
from pathlib import Path
from mathutils import Vector,Matrix
P=Path(__file__).resolve().parents[1];N=runpy.run_path(str(P/'Scripts/common.py'));G=N['Geo']
s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
assert not s.get('V6_emblem_direct_mount'), 'Emblem revision already applied.'
C=N['col']('V6_Emblem_GoldMount',s.collection)
original=list(bpy.data.collections['ASSET_Maynooth_Emblem'].all_objects)
before={o.name:N['signature'](o) for o in original}
removed=[]
for name in ['EX11_EmblemWall','COL_EX11_EmblemWall']:
    o=bpy.data.objects.get(name)
    if o:before[name]=N['signature'](o);removed.append(name);bpy.data.objects.remove(o,do_unlink=True)
gold=N['material']('Emblem_BurnishedGold',(.34,.145,.035),(.64,.365,.105),(6,6,6),rough=.31,metal=.82,bump=.00045)
shadow=bpy.data.materials['V5_Magic_AntiqueBrass']
def surface(x,z):return 42+math.sqrt(max(1,11.72**2-x*x-(z-12)**2))
def project(p,back=51.094,depth=1):
    return Vector((p.x,surface(p.x,p.z)-.018+(p.y-back)*depth,p.z))
def gold_slots(o):
    for i,m in enumerate(o.data.materials):
        if m and m.name.startswith(('MN_Gold_Edges','MN_Gold_Satin')):o.data.materials[i]=gold
def tag(o,zh):
    N['meta'](o,'MN_GOLD','R02',1);o['label_zh']=zh;o['source_asset']='Maynooth supplied emblem';return o
# Evaluated lettering becomes a thin wall-following mesh; editable source remains explicitly hidden.
text_source=bpy.data.objects['MN11_Maynooth_EditableText'];deps=bpy.context.evaluated_depsgraph_get()
md=bpy.data.meshes.new_from_object(text_source.evaluated_get(deps));label=bpy.data.objects.new('V6_Maynooth_GoldWallLetters',md);C.objects.link(label)
for v in md.vertices:v.co=project(text_source.matrix_world@v.co,back=50.9157028,depth=.32)
md.materials.clear();md.materials.append(gold);tag(label,'贴合弧面的 Maynooth 金色立体字');label['editable_source']=text_source.name;label['text']='Maynooth'
text_source.hide_render=True;text_source.hide_viewport=True;text_source['V6_role_zh']='可编辑原文字源，实体显示由弧面金字网格承担'
# Curved gold piping just outside the exact supplied shield outline, no rectangular plaque.
rim=bpy.data.objects['MN06_Shield_Rim'];rim_before=rim.data.copy();rim_world=rim.matrix_world.copy()
for o in original:
    if o.type=='MESH':
        o.data=o.data.copy()
        if o.name in ['MN01_Shield_Back','MN02_Panel_Book','MN03_Panel_Dial','MN04_Panel_Frond','MN05_Panel_Tower']:
            bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=12,use_grid_fill=True);bm.to_mesh(o.data);bm.free()
        inv=o.matrix_world.inverted()
        for v in o.data.vertices:v.co=inv@project(o.matrix_world@v.co)
        o.data.update();gold_slots(o)
    elif o.type=='CURVE':
        data=o.data.copy();o.data=data;inv=o.matrix_world.inverted()
        for sp in data.splines:
            if sp.type=='POLY':
                pts=[o.matrix_world@Vector(p.co[:3]) for p in sp.points];dense=[]
                for i in range(len(pts) if sp.use_cyclic_u else len(pts)-1):
                    a=pts[i];b=pts[(i+1)%len(pts)];cuts=max(1,math.ceil((b-a).length/.09))
                    dense.extend(a.lerp(b,k/cuts) for k in range(cuts))
                if not sp.use_cyclic_u:dense.append(pts[-1])
                sp.points.add(len(dense)-len(sp.points))
                for pt,p in zip(sp.points,dense):q=inv@project(p);pt.co=(*q,1)
            elif sp.type=='BEZIER':
                for bp in sp.bezier_points:
                    for k in ['co','handle_left','handle_right']:setattr(bp,k,inv@project(o.matrix_world@getattr(bp,k)))
        if o.name=='MN06_Shield_Rim':o.data.bevel_depth=.047
        gold_slots(o)
# Additional thin raised gold fillet follows the shield silhouette precisely.
g=G()
for sp in rim_before.splines:
    pts=[rim_world@Vector(p.co[:3]) for p in sp.points];outline=[]
    for i in range(len(pts)):
        a=pts[i];b=pts[(i+1)%len(pts)];cuts=max(1,math.ceil((b-a).length/.08))
        for k in range(cuts):
            p=a.lerp(b,k/cuts);p.x*=1.034;p.z=15.96+(p.z-15.96)*1.029;p.y+=.03;outline.append(project(p))
    g.tube(outline,.016,True,n=10)
o=g.object('V6_Emblem_OuterGoldFillet',C,gold,'MN_GOLD',smooth=True);tag(o,'沿原盾形轮廓的双层金色镶边')
# Small concealed brass spacers provide support to the existing ribbed interior wall.
g=G()
for x,z in [(-.85,15.3),(.85,15.3),(0,17.55)]:
    y=surface(x,z);g.tube([(x,y-.035,z),(x,y+.27,z)],.025,n=12)
o=g.object('V6_Emblem_ConcealedMounts',C,shadow,'MN_GOLD',smooth=True);tag(o,'校标贴壁安装的隐藏黄铜连接件')
# A dedicated real-scene evidence camera.
c=bpy.data.collections['V6_Inspection_Cameras'];d=bpy.data.cameras.new('CAM_V6_Emblem');o=bpy.data.objects.new(d.name,d);c.objects.link(o)
o.location=(0,40.0,15.8);o.rotation_euler=(Vector((0,53,15.35))-o.location).to_track_quat('-Z','Y').to_euler();o.data.lens=44;o.data.clip_end=200
bpy.context.view_layer.update()
expected={n:(N['signature'](bpy.data.objects[n]) if n in bpy.data.objects else None) for n in before}
changed=[n for n in before if expected[n]!=before[n]]
audit={'authorization':'maynooth和校标直接贴上去，不要后面的白版，做一个金色镶边的效果',
    'removed_white_panel_objects':removed,'modified_source_objects':changed,'supplied_crest_colors_retained':True,
    'construction':'Supplied crest X/Z silhouette retained; depth conforms to apse curvature. Original editable lettering source is hidden; thin curved gold lettering is an actual mesh. No rectangular replacement plaque.',
    'projection_radius_m':11.72,'expected_protected_after':expected,'before':before,
    'editable_text_source':{'body':text_source.data.body,'parent':text_source.parent.name,'matrix_basis':[round(v,6) for row in text_source.matrix_basis for v in row],'materials':[m.name for m in text_source.data.materials]},
    'new_objects':[o.name for o in C.objects]}
(P/'Audit/emblem_changes.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2))
s['V6_emblem_direct_mount']=True;N['save']('V6_direct_mount_gold_emblem')
print('White panel removed; curved crest and gold lettering saved.',len(changed),'source objects changed')
