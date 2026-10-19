"""V4 common geometry. Native metres, Z up, front -Y. Original mesh construction."""
import bpy, math, json, hashlib, array
from pathlib import Path
from mathutils import Vector, Matrix
from math import sin,cos,pi,sqrt
OUT=Path(__file__).resolve().parents[1]
LABELS={'W01':'胡桃木书墙雕花顶冠','W02':'上传柜台木作','W03':'检索柜台木作','W04':'阅读桌椅','W05':'尖拱木护墙板','L01':'双层枝形吊灯','L02':'卷叶双臂壁灯','L03':'绿玻璃阅读灯','L04':'尖拱悬灯','C01':'四叶木栏板与卷叶托座','C02':'立体叶饰柱头','C03':'书本圆章与植物边饰'}
def enum(o,k,v):
    valid=[x.identifier for x in o.bl_rna.properties[k].enum_items]
    if v not in valid:raise ValueError((k,v,valid))
    setattr(o,k,v)
def col(name,parent=None):
    c=bpy.data.collections.get(name)
    if not c:
        c=bpy.data.collections.new(name)
        if parent:parent.children.link(c)
    return c
def meta(o,aid,zone='R02',floor=0):
    o['asset_id']='V6_'+aid;o['instance_id']=o.name;o['zone_id']=zone;o['floor']=floor
    o['label_zh']=LABELS.get(aid.split('_')[0],aid);o['revision']='Magic_V6';return o
def material(name,c1,c2=None,scale=(1,1,1),rough=.5,metal=0,bump=.001,emit=0,transmission=0):
    name='V6_'+name
    if name in bpy.data.materials:return bpy.data.materials[name]
    m=bpy.data.materials.new(name);m.diffuse_color=(*c1,1);m.use_nodes=True
    nt=m.node_tree;p=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    for k,v in [('Base Color',(*c1,1)),('Roughness',rough),('Metallic',metal),('Emission Color',(*c1,1)),('Emission Strength',emit),('Transmission Weight',transmission)]:
        p.inputs[k].default_value=v
    if c2:
        tc=nt.nodes.new('ShaderNodeTexCoord');mul=nt.nodes.new('ShaderNodeVectorMath');enum(mul,'operation','MULTIPLY');mul.inputs[1].default_value=scale
        noise=nt.nodes.new('ShaderNodeTexNoise');enum(noise,'noise_dimensions','3D');enum(noise,'noise_type','FBM')
        noise.inputs['Scale'].default_value=3;noise.inputs['Detail'].default_value=4;noise.inputs['Roughness'].default_value=.7
        nt.links.new(tc.outputs['Generated'],mul.inputs[0]);nt.links.new(mul.outputs[0],noise.inputs['Vector'])
        ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.23;ramp.color_ramp.elements[0].color=(*c1,1)
        ramp.color_ramp.elements[1].position=.78;ramp.color_ramp.elements[1].color=(*c2,1)
        nt.links.new(noise.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs['Color'],p.inputs['Base Color'])
        b=nt.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.22;b.inputs['Distance'].default_value=bump
        nt.links.new(noise.outputs['Fac'],b.inputs['Height']);nt.links.new(b.outputs['Normal'],p.inputs['Normal'])
    return m
def materials():
    return {
      'wood':material('Walnut_QuarterSawn',(.026,.010,.0045),(.115,.051,.019),(7,9,.30),.37,bump=.0018),
      'carved':material('Walnut_RubbedCarving',(.045,.020,.009),(.18,.088,.034),(6,7,.48),.4,bump=.0012),
      'brass':material('Oxidised_Brass',(.047,.027,.011),(.27,.15,.056),(3,3,3),.39,.8,.0006),
      'edge':material('Brass_HandledEdges',(.31,.17,.062),(.53,.34,.14),(6,6,6),.31,.82,.0003),
      'leather':material('Oxblood_Leather',(.038,.008,.005),(.11,.029,.015),(50,50,50),.49,bump=.0006),
      'teal':material('Peacock_Plaster',(.018,.062,.064),(.058,.16,.155),(4,4,4),.72,bump=.001),
      'stone':material('Interior_AgedLimestone',(.28,.29,.27),(.56,.54,.47),(5,5,5),.61,bump=.002),
      'amber':material('Amber_Glass',(.46,.21,.045),None,rough=.21,metal=.1,transmission=.55,emit=.16),
      'green':material('Green_Glass',(.013,.085,.035),(.03,.21,.072),(3,3,3),.24,transmission=.22),
      'ivory':material('Wax_Ivory',(.64,.48,.28),None,rough=.48),
      'flame':material('Warm_Flame', (1,.46,.095),None,rough=.35,emit=5),
      'paper':material('Aged_Parchment',(.42,.31,.16),(.7,.58,.35),(15,15,15),.76,bump=.0002)
    }
class Geo:
    def __init__(self):self.v=[];self.f=[]
    def add(self,v,f):
        i=len(self.v);self.v.extend([tuple(p) for p in v]);self.f.extend([tuple(i+j for j in face) for face in f]);return self
    def box(self,c,d):
        x,y,z=[q/2 for q in d];cx,cy,cz=c
        self.add([(cx+a*x,cy+b*y,cz+e*z) for a,b,e in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]);return self
    def tube(self,pts,r,closed=False,n=8):
        pts=[Vector(p) for p in pts];vs=[];fs=[];k=len(pts)
        for i,p in enumerate(pts):
            t=(pts[(i+1)%k]-pts[i-1]) if closed else pts[min(i+1,k-1)]-pts[max(i-1,0)]
            t.normalize();a=t.cross(Vector((0,1,0)))
            if a.length<.01:a=t.cross(Vector((1,0,0)))
            a.normalize();b=t.cross(a).normalized()
            rr=r[i] if isinstance(r,list) else r
            vs.extend(p+rr*(a*cos(j*2*pi/n)+b*sin(j*2*pi/n)) for j in range(n))
        for i in range(k if closed else k-1):
            q=(i+1)%k
            for j in range(n):fs.append((i*n+j,i*n+(j+1)%n,q*n+(j+1)%n,q*n+j))
        if not closed:fs.extend([tuple(reversed(range(n))),tuple((k-1)*n+j for j in range(n))])
        return self.add(vs,fs)
    def ring(self,c,r,t,plane='XY',n=64):
        pts=[]
        for i in range(n):
            a=i*2*pi/n
            if plane=='XY':p=(r*cos(a),r*sin(a),0)
            elif plane=='XZ':p=(r*cos(a),0,r*sin(a))
            else:p=(0,r*cos(a),r*sin(a))
            pts.append(tuple(c[j]+p[j] for j in range(3)))
        return self.tube(pts,t,True)
    def lathe(self,c,profile,n=32):
        vs=[(c[0]+r*cos(i*2*pi/n),c[1]+r*sin(i*2*pi/n),c[2]+z) for r,z in profile for i in range(n)]
        fs=[]
        for k in range(len(profile)-1):
            for i in range(n):fs.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
        if profile[0]!=profile[-1]:
            fs.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))])
        return self.add(vs,fs)
    def extrude(self,outline,y0,y1):
        k=len(outline);v=[(x,y,z) for y in (y0,y1) for x,z in outline]
        return self.add(v,[tuple(reversed(range(k))),tuple(range(k,2*k))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)])
    def leaf(self,c,w,h,angle=0,depth=.035):
        # Sculpted leaf: lobed edges, convex midrib, closed back. Local front -Y.
        vs=[];nr=16;nc=6
        for back in (False,True):
            for i in range(nr+1):
                t=i/nr;ww=w*.5*sin(pi*t)**.72*(.79+.21*cos(10*pi*t))
                for j in range(nc+1):
                    u=2*j/nc-1;x=u*ww;z=h*t
                    y=.008 if back else -depth*(1-u*u)*sin(pi*t)-.006*sin(7*pi*t)*abs(u)
                    vs.append((c[0]+x*cos(angle)-z*sin(angle),c[1]+y,c[2]+x*sin(angle)+z*cos(angle)))
        fs=[];stride=nc+1;off=(nr+1)*stride
        for q in (0,1):
            for i in range(nr):
                for j in range(nc):
                    a=q*off+i*stride+j;face=(a,a+1,a+stride+1,a+stride)
                    fs.append(face if q==0 else tuple(reversed(face)))
        perimeter=list(range(stride))+[i*stride+nc for i in range(1,nr+1)]+[nr*stride+j for j in range(nc-1,-1,-1)]+[i*stride for i in range(nr-1,0,-1)]
        for i,a in enumerate(perimeter):
            b=perimeter[(i+1)%len(perimeter)];fs.append((a,b,b+off,a+off))
        return self.add(vs,fs)
    def scroll(self,c,w,h,side=1,r=.015):
        pts=[]
        for i in range(46):
            t=i/45;a=pi*.15+t*2*pi*1.15;rad=(1-t)*.46+.035
            pts.append((c[0]+side*w*(rad*cos(a)),c[1]-.018*sin(pi*t),c[2]+h*(.50+rad*sin(a))))
        return self.tube(pts,[r*(1-.55*i/45) for i in range(46)])
    def arch(self,c,w,h,r):
        # Pointed two-centre arch, smooth and proportional.
        pts=[]
        for i in range(33):
            t=i/32;x=-w/2+w*t;z=h*(1-(abs(2*t-1))**1.65)
            pts.append((c[0]+x,c[1],c[2]+z))
        return self.tube(pts,r)
    def quatre(self,c,r,t):
        return self.tube([(c[0]+r*(.78+.22*cos(4*a))*cos(a),c[1],c[2]+r*(.78+.22*cos(4*a))*sin(a)) for a in [i*2*pi/64 for i in range(64)]],t,True)
    def transformed(self,xf):
        g=Geo();g.v=[tuple(xf@Vector(v)) for v in self.v];g.f=list(self.f);return g
    def merge(self,g):return self.add(g.v,g.f)
    def object(self,name,c,mat,aid,bevel=0,smooth=False):
        d=bpy.data.meshes.new(name);d.from_pydata(self.v,[],self.f);d.update()
        o=bpy.data.objects.new(name,d);c.objects.link(o);d.materials.append(mat);meta(o,aid)
        for p in d.polygons:p.use_smooth=smooth
        if bevel:
            m=o.modifiers.new('Hand_Rounded_Edges','BEVEL');m.width=bevel;m.segments=2
        return o
def asset(code,parts):
    root=col('V6_Master_'+code)
    if root.objects:return root
    for part,g,m,b,s in parts:
        if g.v:g.object('V6_'+code+'_'+part,root,m,code,bevel=b,smooth=s)
    root['asset_id']='V6_'+code;root['label_zh']=LABELS.get(code,code)
    return root
def place(code,xf,name=None,zone='R02',floor=0):
    scene=bpy.context.scene
    collection_name='V6_'+code if scene.name=='MilanLibrary_Final' else scene.name+'_'+code
    master=bpy.data.collections['V6_Master_'+code];c=col(collection_name,scene.collection)
    r=bpy.data.objects.new(name or 'V6_'+code+'_Root',None);c.objects.link(r);meta(r,code,zone,floor);r.matrix_world=xf;r['component']='interior_asset_root'
    for p in master.objects:
        o=p.copy();o.data=p.data;c.objects.link(o);o.parent=r;o.matrix_parent_inverse=Matrix.Identity(4);o.matrix_basis=p.matrix_basis.copy();meta(o,code,zone,floor)
    return r
def xf(loc=(0,0,0),rot=0,scale=(1,1,1)):
    return Matrix.Translation(Vector(loc))@Matrix.Rotation(rot,4,'Z')@Matrix.Diagonal((*scale,1))
def signature(o):
    rec={'matrix':[round(v,6) for row in o.matrix_world for v in row],'materials':[s.material.name if s.material else None for s in o.material_slots],'hide_render':o.hide_render}
    if o.type=='MESH':
        a=array.array('f',[0])*(len(o.data.vertices)*3);o.data.vertices.foreach_get('co',a)
        idx=array.array('i',[0])*len(o.data.loops);o.data.loops.foreach_get('vertex_index',idx)
        rec['geometry_sha256']=hashlib.sha256(a.tobytes()+idx.tobytes()).hexdigest()
    return rec
def save(stage):
    bpy.context.view_layer.update()
    bpy.context.scene['interior_stage']=stage;bpy.context.scene['interior_concept_approved']=True
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MilanLibrary_Magic_V6_Assembly.blend'),compress=True)
    (OUT/'Audit/progress.json').write_text(json.dumps({'stage':stage,'objects':len(bpy.context.scene.objects)},ensure_ascii=False,indent=2))
    print('V6 SAVED',stage,len(bpy.context.scene.objects))
