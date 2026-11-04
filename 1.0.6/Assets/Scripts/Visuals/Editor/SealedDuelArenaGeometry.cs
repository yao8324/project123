using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    // Metre-space meshes with physical-scale UVs. All output is real geometry, not concept-image cards.
    public static class SealedDuelArenaGeometry
    {
        public sealed class Surface
        {
            readonly List<Vector3> v=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud,bool reverse=false)
            {
                int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{ua,ub,uc,ud});
                if(reverse)triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});else triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool reverse=false)
            {
                float w=Vector3.Distance(a,b)*.4f,h=Vector3.Distance(b,c)*.4f;
                Quad(a,b,c,d,new Vector2(0,0),new Vector2(w,0),new Vector2(w,h),new Vector2(0,h),reverse);
            }
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,bool reverse=false)
            {
                int n=v.Count;v.AddRange(new[]{a,b,c});uv.AddRange(new[]{new Vector2(a.x,a.y)*.35f,new Vector2(b.x,b.y)*.35f,new Vector2(c.x,c.y)*.35f});
                triangles.AddRange(reverse?new[]{n,n+2,n+1}:new[]{n,n+1,n+2});
            }
            public Mesh Mesh(string name)
            {
                var m=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
            }
        }
        static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        public static void ClearCache(){foreach(var m in cache.Values)if(m)UnityEngine.Object.DestroyImmediate(m);cache.Clear();}
        public static Mesh Box(Vector3 size,float bevel=.025f)
        {
            string key="Box"+size.ToString("F4")+bevel.ToString("F4");if(cache.TryGetValue(key,out var old))return old;
            Vector3 h=size*.5f;float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.65f);
            var rings=new List<Vector3[]>();
            for(int j=0;j<4;j++)
            {
                bool cap=j==0||j==3;float x=h.x-(cap?b:0),y=h.y-(cap?b:0),c=cap?b*.45f:b;
                float z=j==0?-h.z:j==1?-h.z+b:j==2?h.z-b:h.z;
                rings.Add(new[]{new Vector3(-x+c,-y,z),new Vector3(x-c,-y,z),new Vector3(x,-y+c,z),new Vector3(x,y-c,z),new Vector3(x-c,y,z),new Vector3(-x+c,y,z),new Vector3(-x,y-c,z),new Vector3(-x,-y+c,z)});
            }
            var s=new Surface();
            for(int j=0;j<3;j++)for(int i=0;i<8;i++){int k=(i+1)%8;s.Quad(rings[j][i],rings[j+1][i],rings[j+1][k],rings[j][k],true);}
            for(int i=0;i<8;i++){int k=(i+1)%8;s.Triangle(new Vector3(0,0,-h.z),rings[0][k],rings[0][i]);s.Triangle(new Vector3(0,0,h.z),rings[3][i],rings[3][k]);}
            var m=s.Mesh(key);cache[key]=m;return m;
        }
        public static Mesh Lathe(string name,Vector2[] profile,int sides=32,float flutes=0)
        {
            string key=name+"/"+sides+"/"+flutes;if(cache.TryGetValue(key,out var old))return old;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int j=0;j<profile.Length;j++)for(int i=0;i<=sides;i++){
                float angle=i*2*Mathf.PI/sides,r=profile[j].x+flutes*Mathf.Cos(angle*12);
                v.Add(new Vector3(Mathf.Cos(angle)*r,profile[j].y,Mathf.Sin(angle)*r));uv.Add(new Vector2(angle*profile[j].x*.45f,profile[j].y*.45f));
            }
            for(int j=0;j<profile.Length-1;j++)for(int i=0;i<sides;i++){
                int a=j*(sides+1)+i,b=a+sides+1;tris.AddRange(new[]{a,b,b+1,a,b+1,a+1});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
            var normals=mesh.normals;for(int j=0;j<profile.Length;j++){int a=j*(sides+1),b=a+sides;Vector3 average=(normals[a]+normals[b]).normalized;normals[a]=normals[b]=average;}mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();cache[key]=mesh;return mesh;
        }
        public static Vector3[] Arch(float half,float bottom,float spring,float rise,int steps=16)
        {
            var p=new List<Vector3>{new Vector3(-half,bottom,0),new Vector3(-half,spring,0)};
            for(int i=1;i<=steps;i++){float t=(float)i/steps;p.Add(new Vector3(-half+half*t*t,spring+rise*t,0));}
            for(int i=steps-1;i>=0;i--){float t=(float)i/steps;p.Add(new Vector3(half-half*t*t,spring+rise*t,0));}
            p.Add(new Vector3(half,bottom,0));return p.ToArray();
        }
        public static Mesh CarvedLeaf()
        {
            const string key="Capital acanthus leaf";if(cache.TryGetValue(key,out var old))return old;
            var s=new Surface();
            for(int i=0;i<12;i++)
            {
                float t=i/12f,u=(i+1)/12f;
                Func<float,float> width=q=>.17f*Mathf.Sin(Mathf.PI*q)*(.86f+.14f*Mathf.Cos(q*6*Mathf.PI));
                Func<float,float> curl=q=>-.10f-.23f*q*q;
                foreach(int sign in new[]{-1,1})
                {
                    Vector3 a=new Vector3(0,t*.62f,curl(t)-.055f*Mathf.Sin(Mathf.PI*t)),b=new Vector3(sign*width(t),t*.62f,curl(t)),c=new Vector3(sign*width(u),u*.62f,curl(u)),d=new Vector3(0,u*.62f,curl(u)-.055f*Mathf.Sin(Mathf.PI*u));
                    s.Quad(a,b,c,d,sign>0);s.Quad(a+Vector3.forward*.035f,d+Vector3.forward*.035f,c+Vector3.forward*.035f,b+Vector3.forward*.035f,sign>0);
                    s.Quad(b,b+Vector3.forward*.035f,c+Vector3.forward*.035f,c,sign>0);
                }
            }
            var mesh=s.Mesh(key);cache[key]=mesh;return mesh;
        }
        public static Mesh Stroke(Vector3[] points,float width,float depth,string name="Moulding")
        {
            var s=new Surface();var rings=new Vector3[points.Length][];float length=0;
            for(int i=0;i<points.Length;i++)
            {
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;
                var side=new Vector3(-tangent.y,tangent.x,0)*width*.5f;
                rings[i]=new[]{points[i]-side-Vector3.forward*depth*.5f,points[i]+side-Vector3.forward*depth*.5f,points[i]+side+Vector3.forward*depth*.5f,points[i]-side+Vector3.forward*depth*.5f};
                if(i==0)continue;
                float next=length+Vector3.Distance(points[i],points[i-1]);
                for(int j=0;j<4;j++){int k=(j+1)%4;s.Quad(rings[i-1][j],rings[i][j],rings[i][k],rings[i-1][k],new Vector2(length*.4f,0),new Vector2(next*.4f,0),new Vector2(next*.4f,width*.4f),new Vector2(length*.4f,width*.4f),true);}length=next;
            }
            s.Quad(rings[0][3],rings[0][2],rings[0][1],rings[0][0]);int last=rings.Length-1;s.Quad(rings[last][0],rings[last][1],rings[last][2],rings[last][3]);return s.Mesh(name);
        }
        public static Mesh ArchFace(float half,float bottom,float spring,float rise)
        {
            var path=Arch(half,bottom,spring,rise);var s=new Surface();var center=new Vector3(0,(bottom+spring)*.5f,0);
            for(int i=0;i<path.Length-1;i++)s.Triangle(center,path[i],path[i+1]);s.Triangle(center,path[path.Length-1],path[0]);return s.Mesh("Recessed arched door face");
        }
        public static Mesh FloorTile(float inner,float outer,float start,float end,float y,int steps=8)
        {
            var s=new Surface();
            for(int i=0;i<steps;i++)
            {
                float a=Mathf.Lerp(start,end,(float)i/steps)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(float)(i+1)/steps)*Mathf.Deg2Rad;
                Vector3 p0=new Vector3(Mathf.Sin(a)*inner,y,Mathf.Cos(a)*inner),p1=new Vector3(Mathf.Sin(a)*outer,y,Mathf.Cos(a)*outer),p2=new Vector3(Mathf.Sin(b)*outer,y,Mathf.Cos(b)*outer),p3=new Vector3(Mathf.Sin(b)*inner,y,Mathf.Cos(b)*inner);
                s.Quad(p0,p1,p2,p3,new Vector2(p0.x,p0.z)*.38f,new Vector2(p1.x,p1.z)*.38f,new Vector2(p2.x,p2.z)*.38f,new Vector2(p3.x,p3.z)*.38f);
            }return s.Mesh("Cut stone radial slab");
        }
        public static Mesh Ring(float radius,float width,float y,int steps=256,float start=0,float end=360)
        {return FloorTile(radius-width*.5f,radius+width*.5f,start,end,y,steps);}
        public static bool VaultOpening(float angle,float latitude)
        {
            float x=(angle-66)/18f,y=(latitude-36)/23f;
            return x*x+y*y < 1+.08f*Mathf.Sin(angle*1.7f)+.05f*Mathf.Sin(latitude*2.1f);
        }
        public static Mesh VaultPatch(float angle0,float angle1,float latitude0,float latitude1,int ax=8,int ay=8)
        {
            var s=new Surface();
            Func<float,float,Vector3> point=(angle,lat)=>new Vector3(Mathf.Sin(angle)*16.45f*Mathf.Cos(lat),11.25f+6.25f*Mathf.Sin(lat),Mathf.Cos(angle)*16.45f*Mathf.Cos(lat));
            for(int i=0;i<ax;i++)for(int j=0;j<ay;j++)
            {
                float amid=Mathf.Lerp(angle0,angle1,(i+.5f)/ax),lmid=Mathf.Lerp(latitude0,latitude1,(j+.5f)/ay);
                if(VaultOpening(amid,lmid))continue;
                float a=Mathf.Lerp(angle0,angle1,(float)i/ax)*Mathf.Deg2Rad,b=Mathf.Lerp(angle0,angle1,(float)(i+1)/ax)*Mathf.Deg2Rad;
                float c=Mathf.Lerp(latitude0,latitude1,(float)j/ay)*Mathf.Deg2Rad,d=Mathf.Lerp(latitude0,latitude1,(float)(j+1)/ay)*Mathf.Deg2Rad;
                s.Quad(point(a,c),point(a,d),point(b,d),point(b,c),new Vector2(a*8,c*5),new Vector2(a*8,d*5),new Vector2(b*8,d*5),new Vector2(b*8,c*5));
                Action<Vector3,Vector3> cap=(p,q)=>{
                    Vector3 normal=(p+q)*.5f-Vector3.up*11.25f;normal.y*=6.92f;normal=normal.normalized*.48f;
                    s.Quad(p,p+normal,q+normal,q);
                };
                float da=(angle1-angle0)/ax,dl=(latitude1-latitude0)/ay;
                if(VaultOpening(amid-da,lmid))cap(point(a,c),point(a,d));
                if(VaultOpening(amid+da,lmid))cap(point(b,d),point(b,c));
                if(VaultOpening(amid,lmid+dl))cap(point(a,d),point(b,d));
                if(VaultOpening(amid,lmid-dl))cap(point(b,c),point(a,c));
            }return s.Mesh("Masonry vault sector");
        }
        public static Mesh Tube(Vector3[] points,float radius,int sides=8)
        {
            var s=new Surface();
            for(int i=0;i<points.Length-1;i++)
            {
                Vector3 tangent=(points[i+1]-points[i]).normalized;Vector3 side=Vector3.Cross(tangent,Vector3.up).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;Vector3 up=Vector3.Cross(side,tangent).normalized;
                for(int j=0;j<sides;j++){
                    float a=j*2*Mathf.PI/sides,b=(j+1)*2*Mathf.PI/sides;
                    Vector3 da=(side*Mathf.Cos(a)+up*Mathf.Sin(a))*radius,db=(side*Mathf.Cos(b)+up*Mathf.Sin(b))*radius;
                    s.Quad(points[i]+da,points[i]+db,points[i+1]+db,points[i+1]+da,true);
                }
            }return s.Mesh("Round architectural moulding");
        }
    }
}
