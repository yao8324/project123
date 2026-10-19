"""Verify actual scene changes, geometric clearances, and sampled native animation."""
import bpy, json, runpy, math, hashlib, collections
from pathlib import Path
from mathutils import Matrix, Vector
P=Path(__file__).resolve().parents[1]
N=runpy.run_path(str(P/'Scripts/common.py'))
s=bpy.data.scenes['MilanLibrary_Final']; bpy.context.window.scene=s
base=json.loads((P/'Audit/baseline.json').read_text())
removed=json.loads((P/'Audit/removed_obsolete_objects.json').read_text())
s.frame_set(35); bpy.context.view_layer.update()
emblem=json.loads((P/'Audit/emblem_changes.json').read_text()) if (P/'Audit/emblem_changes.json').exists() else {}
protected=[];authorized_emblem=[]
for n,sig in base['protected'].items():
    now=N['signature'](bpy.data.objects[n]) if n in bpy.data.objects else None
    if now!=sig:
        if n=='MN11_Maynooth_EditableText' and emblem.get('editable_text_source'):
            # Excluded source objects do not have a stable evaluated world matrix after reopen.
            # Compare the actual stored authoring transform and text, and both hide flags.
            o=bpy.data.objects[n]
            stored={'body':o.data.body,'parent':o.parent.name,'matrix_basis':[round(v,6) for row in o.matrix_basis for v in row],'materials':[m.name for m in o.data.materials]}
            if o.hide_render and o.hide_viewport and stored==emblem['editable_text_source']:authorized_emblem.append(n)
            else:protected.append(n)
        elif n in emblem.get('expected_protected_after',{}) and now==emblem['expected_protected_after'][n]:authorized_emblem.append(n)
        else:protected.append(n)
expected_anchors={}
for side,zone,prefix in [(-1,'R04','F03'),(1,'R03','F04')]:
    for key in ('UI','BookSpawn','Interaction'):
        expected_anchors[prefix+'_'+key]=[side*5.8,11.8,0] if key=='Interaction' else [side*5.8,14.13,2.65]
    expected_anchors['ZONE_'+zone]=[side*5.8,15.2,0]
layout=[]; intentional=[]
for n,m in base['layout'].items():
    o=bpy.data.objects.get(n); now=[round(v,6) for row in o.matrix_world for v in row] if o else None
    if now!=m:
        want=list(m)
        if n in expected_anchors:
            for i,v in zip((3,7,11),expected_anchors[n]):want[i]=v
        if n in expected_anchors and now and max(abs(a-b) for a,b in zip(want,now))<.00002:intentional.append(n)
        else:layout.append(n)
effect_errors={}
for n,rec in base['effects'].items():
    o=bpy.data.objects[n]; side=-1 if o.get('asset_id')=='V5_FX01' else 1
    old=Vector((side*5.5,13.50 if side<0 else 13.52,1.066))
    target=Vector((side*5.8,14.13,1.066))
    want=Matrix.Translation(target)@Matrix.Diagonal((1.7,1.7,1.7,1))@Matrix.Translation(-old)@Matrix(rec['matrix'])
    effect_errors[n]=max(abs(a-b) for row,wr in zip(o.matrix_world,want) for a,b in zip(row,wr))
def box(o):
    vv=[o.matrix_world@Vector(v) for v in o.bound_box]
    return [[min(v[i] for v in vv),max(v[i] for v in vv)] for i in range(3)]
def overlap(a,b,tol=.005):return all(a[i][0]<b[i][1]-tol and a[i][1]>b[i][0]+tol for i in range(3))
visible=[o for o in s.objects if o.type=='MESH' and not o.hide_render and not o.name.startswith(('COL_','COLLISION_'))]
boxes={o.name:box(o) for o in visible}
def blocks(o,b):
    if not overlap(boxes[o.name],b):return False
    # Four merged rune meshes span many disconnected books. Check their real vertices,
    # since their aggregate AABB includes the empty central aisle.
    if o.name.startswith('V6_EnchantedSpineTooling_'):
        return any(all(b[i][0]+.005<p[i]<b[i][1]-.005 for i in range(3)) for p in (o.matrix_world@v.co for v in o.data.vertices))
    return True
aisle=[o.name for o in visible if blocks(o,[[-2.999,2.999],[12,42],[.1,2.2]])]
counters=[]
for n in ('R04_WalnutCounter','R03_WalnutCounter'):
    r=bpy.data.objects[n]; parts=set(r.children_recursive); bb=[box(o) for o in parts if o.type=='MESH']
    b=[[min(a[i][0] for a in bb),max(a[i][1] for a in bb)] for i in range(3)]; x=r.matrix_world.translation.x
    front=[[x-.9,x+.9],[b[1][0]-1.8,b[1][0]],[.1,2.1]]
    rear=[[x-.52,x+.52],[b[1][1],b[1][1]+1.8],[.1,2.1]]
    column=[o.name for o in visible if o not in parts and 'Column' in o.name and overlap(boxes[o.name],b)]
    counters.append({'name':n,'body_bounds_m':b,'dimensions_m':[round(hi-lo,4) for lo,hi in b],
        'front_area_dimensions_m':[1.8,1.8],'front_obstacles':[o.name for o in visible if o not in parts and blocks(o,front)],
        'rear_staff_approach_m':1.8,'rear_obstacles':[o.name for o in visible if o not in parts and blocks(o,rear)],
        'rear_opening_m':r.get('staff_opening_width_m'),'column_intersections':column})
roots=[o for o in s.objects if o.get('component')=='shelf_root']
slots=[o for o in bpy.data.collections['InteractionAnchors'].objects if o.get('asset_id')=='B08']
preview=list(bpy.data.collections['Preview_FullCollection'].objects)
new=[o for o in s.objects if o.get('revision')=='Magic_V6']
duplicates=[n for n,c in collections.Counter(o.get('instance_id') for o in new).items() if c>1]
missing_meta=[o.name for o in new if any(k not in o for k in ('asset_id','instance_id','zone_id','floor','label_zh'))]
animated=[o for o in s.objects if o.animation_data and o.animation_data.drivers]
light_driven=[o for o in s.objects if o.type=='LIGHT' and o.data.animation_data and o.data.animation_data.drivers]
mat_driven=[m for m in bpy.data.materials if m.users and m.node_tree and m.node_tree.animation_data and m.node_tree.animation_data.drivers]
samples={}; invalid=set(); frames=[1,31,61,91,121,151,181,211,241]
for frame in frames:
    s.frame_set(frame); bpy.context.view_layer.update()
    vals={o.name:[float(v) for row in o.matrix_world for v in row] for o in animated}
    vals.update({o.name+'::energy':[float(o.data.energy)] for o in light_driven})
    for mat in mat_driven:
        for d in mat.node_tree.animation_data.drivers:
            val=mat.node_tree.path_resolve(d.data_path)
            vals[mat.name+'::'+d.data_path]=[float(val)]
            if not d.driver.is_valid:invalid.add(mat.name+'::'+d.data_path)
    for data in [*animated,*[o.data for o in light_driven]]:
        for d in data.animation_data.drivers:
            if not d.driver.is_valid:invalid.add(data.name+'::'+d.data_path)
    samples[frame]=vals
loop=max([abs(a-b) for n,vals in samples[1].items() for a,b in zip(vals,samples[241][n])] or [0])
finite=all(math.isfinite(v) for vals in samples.values() for row in vals.values() for v in row)
changed=[n for n,vals in samples[1].items() if any(max(abs(a-b) for a,b in zip(vals,samples[f][n]))>.00001 for f in frames[1:-1])]
s.frame_set(35); bpy.context.view_layer.update()
textures=[{'name':im.name,'packed':bool(im.packed_file),'size':list(im.size)} for im in bpy.data.images if im.source=='FILE' and im.users]
source_ok=hashlib.sha256(Path(base['source']).read_bytes()).hexdigest()==base['source_sha256']
remaining=[r['name'] for r in removed if r['name'] in bpy.data.objects]
report={'scope':'V6 changes only. Static visual AABB clearance at frame 35; merged spine glyphs checked by actual vertices. Original transforms and mesh signatures compared. Animation sampled across nine frames. This is not a navigation bake, physics test, exhaustive triangle collision test or external-engine test.',
    'source_V5_sha256_unchanged':source_ok,'removed_obsolete_object_count':len(removed),'obsolete_objects_still_present':remaining,
    'protected_exterior_and_emblem_objects':len(base['protected']),'protected_changes':protected,
    'authorized_emblem_changes':authorized_emblem,'unchanged_protected_objects':len(base['protected'])-len(authorized_emblem)-len(protected),
    'layout_objects_checked':len(base['layout']),'unexpected_layout_changes':layout,'intentional_service_marker_moves':intentional,
    'magic_uniform_scale':1.7,'magic_transform_comparisons':len(effect_errors),'magic_transform_max_error':max(effect_errors.values()),
    'shelf_groups':len(roots),'shelf_ids_unique':len({o.get('instance_id') for o in roots})==len(roots),
    'slot_markers':len(slots),'preview_book_objects':len(preview),'interactive_user_books':len(bpy.data.collections['Runtime_UserBooks'].objects),
    'added_floating_display_books':8,'new_object_count':len(new),'new_duplicate_ids':duplicates,'new_missing_metadata':missing_meta,
    'aisle_6m_headroom_volume_obstacles':aisle,'counter_checks':counters,
    'animation':{'sample_frames':frames,'object_driver_owners':len(animated),'light_driver_owners':len(light_driven),'material_driver_owners':len(mat_driven),
        'all_samples_finite':finite,'invalid_drivers':sorted(invalid),'loop_endpoint_max_error':loop,'animated_owners_with_observed_change':len(changed),'duration_s':10,'fps':24},
    'used_file_images':textures,'external_engine_import_tested':False,'device_performance_tested':False}
report['pass']=all([source_ok,not remaining,not protected,not layout,len(intentional)==8,max(effect_errors.values())<.0001,
    len(roots)==72,len(slots)==10688,len(preview)==14336,not duplicates,not missing_meta,not aisle,not invalid,finite,loop<.001,
    all(not c['front_obstacles'] and not c['rear_obstacles'] and not c['column_intersections'] for c in counters),all(t['packed'] for t in textures)])
(P/'Audit/geometry_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
(P/'Audit/animation_samples.json').write_text(json.dumps(samples,indent=2))
(P/'Audit/effect_transform_errors.json').write_text(json.dumps(effect_errors,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='used_file_images'},ensure_ascii=False))
