using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    // Editable mesh authoring for the user's sealed-guardian reference, on the production skeleton.
    public static class KnightReferenceBuilder
    {
        public const string Root="Assets/Art/Bosses/Knight/SealedGuardian";
        public const string Output="Docs/KnightReference20261107";
        const string Model=KnightProductionBuilder.Root+"/KnightProduction.prefab";
        static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
        static T Save<T>(T asset,string name) where T:Object
        {
            string path=Root+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old)
            {
                if(asset is Mesh source && old is Mesh target)
                {
                    target.Clear();target.indexFormat=source.indexFormat;target.vertices=source.vertices;target.normals=source.normals;target.uv=source.uv;target.tangents=source.tangents;target.bindposes=source.bindposes;
                    if(source.boneWeights.Length>0)target.boneWeights=source.boneWeights;
                    target.subMeshCount=source.subMeshCount;for(int i=0;i<source.subMeshCount;i++)target.SetTriangles(source.GetTriangles(i),i);target.bounds=source.bounds;target.UploadMeshData(false);
                }
                else EditorUtility.CopySerialized(asset,old);
                Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;
            }
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static Material Mat(string name,Color c,float metal,float smooth)
        {
            string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);return m;
        }
        sealed class Geo
        {
            public Matrix4x4 matrix=Matrix4x4.identity;public int bone;public int segments=16;
            public readonly List<Vector3> v=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<BoneWeight> weights=new List<BoneWeight>();
            readonly Dictionary<int,Vector3> smoothNormals=new Dictionary<int,Vector3>();
            readonly List<int>[] tri=Enumerable.Range(0,7).Select(i=>new List<int>()).ToArray();
            public Func<Vector3,BoneWeight> clothWeight;
            int V(Vector3 p)
            {
                Vector3 world=matrix.MultiplyPoint3x4(p);int index=v.Count;v.Add(world);uv.Add(new Vector2(p.x+p.z*.7f,p.y));
                weights.Add(clothWeight!=null?clothWeight(world):new BoneWeight{boneIndex0=bone,weight0=1});return index;
            }
            public void T(Vector3 a,Vector3 b,Vector3 c,int m){tri[m].Add(V(a));tri[m].Add(V(b));tri[m].Add(V(c));}
            public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int m){T(a,b,c,m);T(a,c,d,m);}
            public void Tube(Vector3 a,Vector3 b,float radius,int m,int sides=6)
            {
                if(segments<=10 && radius<.011f)return; // Subpixel engraving is omitted in the distant LOD.
                Vector3 axis=(b-a).normalized;Vector3 x=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized*radius,y=Vector3.Cross(axis,x);
                for(int i=0;i<sides;i++){float u=i*Mathf.PI*2/sides,w=(i+1)*Mathf.PI*2/sides;Vector3 p=x*Mathf.Cos(u)+y*Mathf.Sin(u),q=x*Mathf.Cos(w)+y*Mathf.Sin(w);Q(a+p,a+q,b+q,b+p,m);T(a,a+q,a+p,m);T(b,b+p,b+q,m);}
            }
            public void Path(IEnumerable<Vector3> points,float r,int m){var p=points.ToArray();for(int i=1;i<p.Length;i++)Tube(p[i-1],p[i],r,m);}
            public void Ring(Vector3 c,float r,float width,float start,float sweep,int m)
            {int count=Mathf.Max(3,Mathf.CeilToInt(segments*Mathf.Abs(sweep)/180));Path(Enumerable.Range(0,count+1).Select(i=>c+P(Mathf.Cos((start+sweep*i/count)*Mathf.Deg2Rad)*r,Mathf.Sin((start+sweep*i/count)*Mathf.Deg2Rad)*r,0)),width,m);}
            public void Ellipsoid(Vector3 c,Vector3 size,int m)
            {
                int first=v.Count;
                int rows=segments/2;Func<int,int,Vector3> p=(i,j)=>{float a=i*Mathf.PI*2/segments,b=-Mathf.PI/2+j*Mathf.PI/rows;return c+Vector3.Scale(P(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b)),size);};
                for(int j=0;j<rows;j++)for(int i=0;i<segments;i++)Q(p(i,j),p(i,j+1),p(i+1,j+1),p(i+1,j),m);
                for(int i=first;i<v.Count;i++){var q=matrix.inverse.MultiplyPoint3x4(v[i])-c;smoothNormals[i]=matrix.inverse.transpose.MultiplyVector(P(q.x/(size.x*size.x),q.y/(size.y*size.y),q.z/(size.z*size.z))).normalized;}
            }
            public void Shell(Vector3 c,float[] ys,float[] xs,float[] zs,int m)
            {
                Func<int,int,Vector3> p=(i,j)=>{float a=i*Mathf.PI*2/segments;return c+P(Mathf.Cos(a)*xs[j],ys[j],Mathf.Sin(a)*zs[j]);};
                for(int j=0;j<ys.Length-1;j++)for(int i=0;i<segments;i++)Q(p(i,j),p(i,j+1),p(i+1,j+1),p(i+1,j),m);
            }
            public void Plate(Vector3 c,Vector2[] outline,float depth,int m,bool trim=true)
            {
                // A raised central ridge with bevels, closed on the reverse.
                Vector3 center=c+P(0,0,depth);
                for(int i=0;i<outline.Length;i++)
                {
                    var a=c+P(outline[i].x,outline[i].y,0);var q=outline[(i+1)%outline.Length];var b=c+P(q.x,q.y,0);
                    var ai=c+P(outline[i].x*.85f,outline[i].y*.85f,depth*.7f);var bi=c+P(q.x*.85f,q.y*.85f,depth*.7f);
                    Q(a,ai,bi,b,m);T(ai,center,bi,m);T(c-P(0,0,.035f),a,b,m);
                    if(trim)Tube(a,b,.009f,1);
                }
            }
            public void Arch(Vector3 p,float half,float height,float r,int m)
            {
                var points=new List<Vector3>{p+P(-half,0,0),p+P(-half,height*.52f,0)};
                for(int i=0;i<=segments;i++){float x=-1+2f*i/segments;points.Add(p+P(half*x,height*(.52f+.48f*Mathf.Pow(1-Mathf.Abs(x),.7f)),0));}
                points.Add(p+P(half,0,0));Path(points,r,m);
            }
            public void Seal(Vector3 c,float r)
            {Ring(c,r,.012f,0,360,1);Tube(c+P(0,-r*1.5f,0),c+P(0,r*1.5f,0),.012f,1);Tube(c+P(-r*1.5f,0,0),c+P(r*1.5f,0,0),.012f,1);}
            public void Box(Vector3 c,Vector3 s,int m)
            {
                var a=c-P(s.x,s.y,s.z)*.5f;var b=c+P(s.x,s.y,s.z)*.5f;
                Q(P(a.x,a.y,b.z),P(b.x,a.y,b.z),P(b.x,b.y,b.z),P(a.x,b.y,b.z),m);Q(P(b.x,a.y,a.z),P(a.x,a.y,a.z),P(a.x,b.y,a.z),P(b.x,b.y,a.z),m);
                Q(P(a.x,a.y,a.z),P(a.x,a.y,b.z),P(a.x,b.y,b.z),P(a.x,b.y,a.z),m);Q(P(b.x,a.y,b.z),P(b.x,a.y,a.z),P(b.x,b.y,a.z),P(b.x,b.y,b.z),m);
                Q(P(a.x,b.y,b.z),P(b.x,b.y,b.z),P(b.x,b.y,a.z),P(a.x,b.y,a.z),m);Q(P(a.x,a.y,a.z),P(b.x,a.y,a.z),P(b.x,a.y,b.z),P(a.x,a.y,b.z),m);
            }
            public Mesh Mesh(string name,Matrix4x4[] binds=null)
            {
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=7;
                for(int i=0;i<7;i++)mesh.SetTriangles(tri[i],i);
                if(binds!=null){mesh.boneWeights=weights.ToArray();mesh.bindposes=binds;}
                mesh.RecalculateNormals();var ns=mesh.normals;foreach(var pair in smoothNormals)ns[pair.Key]=pair.Value;mesh.normals=ns;mesh.RecalculateBounds();mesh.RecalculateTangents();Weld(mesh);return mesh;
            }
            static void Weld(Mesh mesh)
            {
                var vs=mesh.vertices;var ns=mesh.normals;var uvs=mesh.uv;var ws=mesh.boneWeights;
                bool skinned=ws.Length==vs.Length;var remap=new int[vs.Length];
                var unique=new Dictionary<Tuple<Vector3,Vector3,Vector2,BoneWeight>,int>();
                var outV=new List<Vector3>();var outN=new List<Vector3>();var outUV=new List<Vector2>();var outW=new List<BoneWeight>();
                for(int i=0;i<vs.Length;i++)
                {
                    var w=skinned?ws[i]:default(BoneWeight);var key=Tuple.Create(vs[i],ns[i],uvs[i],w);
                    if(!unique.TryGetValue(key,out int index)){index=outV.Count;unique.Add(key,index);outV.Add(vs[i]);outN.Add(ns[i]);outUV.Add(uvs[i]);outW.Add(w);}remap[i]=index;
                }
                var triangles=Enumerable.Range(0,mesh.subMeshCount).Select(s=>mesh.GetTriangles(s).Select(i=>remap[i]).ToArray()).ToArray();var binds=mesh.bindposes;
                mesh.Clear();mesh.SetVertices(outV);mesh.SetNormals(outN);mesh.SetUVs(0,outUV);mesh.subMeshCount=triangles.Length;
                if(skinned){mesh.boneWeights=outW.ToArray();mesh.bindposes=binds;}
                for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();
            }
        }
        static Vector2[] Shield(float w,float h)=>new[]{new Vector2(0,h*.58f),new Vector2(w,h*.25f),new Vector2(w*.8f,-h*.28f),new Vector2(0,-h*.55f),new Vector2(-w*.8f,-h*.28f),new Vector2(-w,h*.25f)};
        static void Body(Geo g,KnightVisuals k,SkinnedMeshRenderer skin)
        {
            Action<Transform> bind=t=>{g.matrix=skin.transform.worldToLocalMatrix*t.localToWorldMatrix;g.bone=Array.IndexOf(skin.bones,t);};
            bind(k.torso);
            g.Shell(P(0,0,0),new[]{-.06f,.15f,.48f,.88f,1.05f},new[]{.29f,.32f,.36f,.44f,.33f},new[]{.22f,.25f,.28f,.28f,.22f},2);
            g.Shell(P(0,0,0),new[]{.26f,.48f,.85f,1.02f},new[]{.31f,.39f,.45f,.33f},new[]{.26f,.31f,.32f,.25f},0);
            g.Plate(P(0,.66f,.31f),Shield(.33f,.81f),.07f,0);
            g.Arch(P(0,.31f,.392f),.27f,.73f,.014f,1);
            for(int i=-1;i<=1;i++)g.Arch(P(i*.145f,.40f,.409f),.066f,.43f,.009f,1);
            g.Seal(P(0,.63f,.435f),.10f);
            g.Shell(P(0,1.05f,0),new[]{-.06f,0,.13f,.17f},new[]{.30f,.29f,.21f,.20f},new[]{.26f,.25f,.19f,.18f},0);
            for(int i=0;i<3;i++)g.Shell(P(0,.23f-i*.1f,0),new[]{-.06f,.04f,.09f},new[]{.36f+i*.017f,.35f+i*.015f,.32f+i*.012f},new[]{.27f,.26f,.24f},0);
            // Door-shaped dorsal reliquary with pointed tracery and a locking crossbar.
            var front=g.matrix;g.matrix*=Matrix4x4.Rotate(Quaternion.Euler(0,180,0));
            g.Plate(P(0,.60f,.30f),Shield(.33f,.95f),.04f,2);g.Arch(P(0,.12f,.35f),.30f,.98f,.026f,1);
            for(int i=-1;i<=1;i++)g.Arch(P(i*.16f,.2f,.37f),.065f,.64f,.012f,1);
            g.Tube(P(-.29f,.51f,.39f),P(.29f,.51f,.39f),.025f,1);g.Seal(P(0,.51f,.42f),.11f);g.matrix=front;
            // Small archive cases, leather straps and visibly braided blue transfer cords.
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 box=P(side*.43f,-.02f,.17f);g.Box(box,P(.25f,.30f,.23f),5);
                for(int i=-1;i<=1;i++)g.Box(box+P(i*.10f,0,.122f),P(.024f,.32f,.024f),1);
                for(int i=-1;i<=1;i+=2)g.Box(box+P(0,i*.14f,.13f),P(.27f,.025f,.025f),1);
                for(int strand=0;strand<2;strand++)
                {
                    var pts=new List<Vector3>();for(int i=0;i<=32;i++){float t=i/32f;pts.Add(P(side*(.27f+.31f*t),.27f-.37f*t,.32f)+P(Mathf.Cos(t*40+strand*Mathf.PI)*.011f,0,Mathf.Sin(t*40+strand*Mathf.PI)*.011f));}g.Path(pts,.012f,4);
                }
                g.Plate(P(side*.28f,-.27f,.24f),Shield(.19f,.44f),.035f,0);
            }
            bind(k.productionRig.head);
            g.Shell(Vector3.zero,new[]{-.22f,-.09f,.17f,.30f,.43f},new[]{.16f,.23f,.23f,.16f,0},new[]{.18f,.21f,.22f,.14f,0},0);
            g.Plate(P(0,.05f,.225f),Shield(.19f,.60f),.055f,0);
            g.Arch(P(0,-.19f,.278f),.16f,.57f,.013f,1);g.Seal(P(0,.09f,.294f),.062f);
            for(int side=-1;side<=1;side+=2){g.Tube(P(side*.04f,.055f,.30f),P(side*.145f,.075f,.273f),.012f,2);g.Tube(P(side*.17f,-.18f,.19f),P(side*.24f,.24f,.09f),.014f,1);}
            // Segmented, solid old-gold halo; large gaps are part of the silhouette.
            bind(k.halo);
            for(int i=0;i<7;i++){float a=8+i*51.43f;g.Ring(P(0,-.02f,0),.55f,.025f,a,34,1);g.Ring(P(0,-.02f,0),.59f,.010f,a,34,1);for(int j=0;j<3;j++){float t=(a+2+j*15)*Mathf.Deg2Rad;g.Tube(P(Mathf.Cos(t)*.53f,Mathf.Sin(t)*.53f-.02f,0),P(Mathf.Cos(t)*.61f,Mathf.Sin(t)*.61f-.02f,0),.009f,1);}}
            foreach(var arm in new[]{k.leftArm,k.rightArm})
            {
                int side=arm==k.leftArm?1:-1;
                // Shoulder caps stay on the shoulder girdle; upper-arm plates rotate below them.
                bind(k.torso);g.matrix*=Matrix4x4.Translate(k.torso.InverseTransformPoint(arm.position));
                for(int i=2;i>=0;i--)
                {
                    float y=.03f-i*.115f;
                    if(i==0)g.Ellipsoid(P(side*.04f,y,0),P(.30f,.13f,.29f),0);
                    else g.Shell(P(side*(.04f+i*.025f),y,0),new[]{-.07f,0,.075f},new[]{.29f-i*.02f,.29f-i*.02f,.23f-i*.02f},new[]{.27f-i*.015f,.28f-i*.015f,.23f-i*.015f},0);
                    g.Plate(P(side*.04f,y-.04f,.245f-i*.018f),Shield(.27f-i*.025f,.22f),.025f,0);
                }
                for(int i=-1;i<=1;i++)g.Arch(P(i*.145f,-.10f,.293f),.065f,.21f,.009f,1);
                bind(arm);g.Ellipsoid(P(0,-.13f,0),P(.15f,.26f,.16f),2);
                g.Shell(P(0,-.32f,0),new[]{-.12f,0,.12f},new[]{.13f,.17f,.18f},new[]{.14f,.19f,.18f},0);
            }
            foreach(var forearm in new[]{k.leftForearm,k.rightForearm})
            {
                bind(forearm);g.Ellipsoid(P(0,0,0),P(.15f,.14f,.15f),2);
                for(int i=0;i<3;i++)g.Plate(P(0,-.08f-i*.115f,.14f),Shield(.16f-i*.012f,.25f),.045f,0);
                g.Shell(P(0,-.2f,0),new[]{-.26f,-.05f,.07f},new[]{.105f,.14f,.14f},new[]{.11f,.15f,.13f},0);
            }
            foreach(var hand in new[]{k.leftHand,k.rightHand})
            {
                bind(hand);g.Ellipsoid(P(0,-.055f,.02f),P(.105f,.085f,.05f),2);g.Plate(P(0,-.045f,.075f),Shield(.10f,.14f),.015f,0);
                // The curled fingers surround a local X-axis grip at (0,-.145,-.045).
                // Mirrored wrist frames turn that channel onto the sword's Y-axis.
                for(int f=0;f<4;f++)for(int j=0;j<5;j++)
                {
                    float a=(45+j*48)*Mathf.Deg2Rad,b=(45+(j+1)*48)*Mathf.Deg2Rad;
                    Vector3 from=P((f-1.5f)*.048f,-.145f+.065f*Mathf.Cos(a),-.045f+.065f*Mathf.Sin(a));
                    Vector3 to=P((f-1.5f)*.048f,-.145f+.065f*Mathf.Cos(b),-.045f+.065f*Mathf.Sin(b));
                    g.Tube(from,to,.022f,j%2==0?0:2,8);g.Ellipsoid(from,P(.024f,.025f,.025f),0);
                }
                float thumb=hand==k.rightHand?1:-1;
                g.Tube(P(thumb*.095f,-.04f,.02f),P(thumb*.115f,-.11f,-.025f),.034f,0);
                g.Tube(P(thumb*.115f,-.11f,-.025f),P(thumb*.055f,-.15f,-.095f),.029f,0);
            }
            foreach(var thigh in new[]{k.leftThigh,k.rightThigh})
            {bind(thigh);g.Shell(Vector3.zero,new[]{-.57f,-.26f,0},new[]{.135f,.17f,.17f},new[]{.14f,.19f,.19f},2);g.Plate(P(0,-.27f,.18f),Shield(.16f,.48f),.055f,0);}
            foreach(var shin in new[]{k.leftShin,k.rightShin})
            {
                bind(shin);g.Ellipsoid(Vector3.zero,P(.15f,.13f,.16f),2);g.Plate(P(0,0,.17f),Shield(.18f,.25f),.08f,0);
                g.Shell(Vector3.zero,new[]{-.56f,-.30f,-.08f},new[]{.11f,.145f,.13f},new[]{.12f,.18f,.16f},0);
                g.Plate(P(0,-.32f,.155f),Shield(.12f,.49f),.065f,0);g.Arch(P(0,-.51f,.20f),.065f,.36f,.008f,1);
            }
            foreach(var foot in new[]{k.productionRig.leftFoot,k.productionRig.rightFoot})
            {bind(foot);g.Ellipsoid(P(0,-.015f,.03f),P(.145f,.10f,.25f),2);for(int i=0;i<4;i++)g.Ellipsoid(P(0,.025f-i*.012f,.01f+i*.065f),P(.14f-i*.01f,.075f,.09f),0);}
            // Broad torn cloth panels follow the existing segmented ribbon skeleton.
            g.matrix=Matrix4x4.identity;
            var candidates=skin.bones.Select((b,i)=>new{b,i}).Where(x=>k.sealRibbons.Any(r=>x.b==r||x.b.IsChildOf(r))).ToArray();
            g.clothWeight=p=>{if(p.y>1.58f)return new BoneWeight{boneIndex0=Array.IndexOf(skin.bones,k.torso),weight0=1};var nearest=candidates.OrderBy(x=>(skin.transform.InverseTransformPoint(x.b.position)-p).sqrMagnitude).Take(2).ToArray();float a=Vector3.Distance(skin.transform.InverseTransformPoint(nearest[0].b.position),p),b=Vector3.Distance(skin.transform.InverseTransformPoint(nearest[1].b.position),p);float w=b/Mathf.Max(.0001f,a+b);return new BoneWeight{boneIndex0=nearest[0].i,weight0=w,boneIndex1=nearest[1].i,weight1=1-w};};
            for(int panel=0;panel<10;panel++)
            {
                float angle=(40+panel*28)*Mathf.Deg2Rad;
                Func<int,int,Vector3> p=(row,col)=>{float t=row/9f,a=angle+(col/4f-.5f)*.47f,r=.30f+t*.28f+.018f*Mathf.Sin(col*2.6f+row*.5f);float hem=row==9?.10f*Mathf.Sin(panel*8+col*3):0;return P(Mathf.Sin(a)*r,1.58f-t*1.25f+hem,Mathf.Cos(a)*r);};
                for(int row=0;row<9;row++)for(int col=0;col<4;col++)g.Q(p(row,col),p(row,col+1),p(row+1,col+1),p(row+1,col),panel%3==0?6:3);
            }
            // Pair of ivory shoulder stoles, plus central tabard; leave the door chest visible.
            for(int facing=-1;facing<=1;facing+=2)for(int side=-1;side<=1;side+=2)
            {
                Func<int,int,Vector3> p=(row,col)=>{float t=row/12f;return P(side*(.31f+.07f*t)+(col/4f-.5f)*.14f,2.45f-t*1.92f+(row==12?.035f*Mathf.Sin(col*5):0),facing*(.35f+.07f*t+.012f*Mathf.Sin(row*.6f+col)));};
                for(int row=0;row<12;row++)for(int col=0;col<4;col++)g.Q(p(row,col),p(row+1,col),p(row+1,col+1),p(row,col+1),3);
                g.Seal(P(side*.35f,1.85f,facing*.405f),.047f);
                for(int edge=0;edge<=4;edge+=4)g.Path(Enumerable.Range(0,13).Select(row=>p(row,edge)+P(0,0,facing*.003f)),.003f,1);
            }
            for(int row=0;row<8;row++)for(int col=0;col<4;col++)
            {
                Func<int,int,Vector3> p=(r,c)=>P((c/4f-.5f)*.29f,1.39f-r*.115f+(r==8?.04f*Mathf.Sin(c*7):0),.32f+r*.009f+.01f*Mathf.Cos(c*2));
                g.Q(p(row,col),p(row+1,col),p(row+1,col+1),p(row,col+1),3);
            }
            g.clothWeight=null;
        }
        static void Sword(Geo g)
        {
            g.Tube(P(0,-.1f,0),P(0,.88f,0),.048f,2,12);
            for(int i=0;i<24;i++)g.Tube(P(-.045f,i*.036f,0),P(.045f,i*.036f+.045f,0),.012f,4);
            g.Ellipsoid(P(0,-.17f,0),P(.095f,.13f,.075f),1);
            g.Plate(P(0,1.66f,0),new[]{new Vector2(-.17f,-.72f),new Vector2(-.21f,-.45f),new Vector2(-.19f,.63f),new Vector2(0,.98f),new Vector2(.19f,.63f),new Vector2(.21f,-.45f),new Vector2(.17f,-.72f)},.055f,0);
            g.Tube(P(0,1.05f,.062f),P(0,2.46f,.062f),.014f,1);
            for(int s=-1;s<=1;s+=2){g.Tube(P(s*.08f,1.12f,.05f),P(s*.12f,2.25f,.05f),.008f,1);g.Plate(P(s*.27f,.9f,0),Shield(.20f,.18f),.045f,1);}
            g.Seal(P(0,.93f,.08f),.14f);g.Seal(P(0,1.83f,.077f),.065f);
        }
        public static void Studio()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
            var k=model.GetComponent<KnightVisuals>();k.wardShield.SetActive(false);foreach(var blade in k.refinedEffects.readyBlades)blade.SetActive(false);
            model.GetComponent<LODGroup>().ForceLOD(0);
            var cameraObject=new GameObject("Studio camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();
            camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2.22f;camera.nearClipPlane=.05f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.17f,.20f);
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            for(int i=0;i<3;i++)
            {
                var obj=new GameObject("Studio area approximation");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);var light=obj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=i==0?2.6f:i==1?1.4f:1.2f;light.color=i==0?new Color(1,.94f,.84f):new Color(.77f,.86f,1);obj.transform.rotation=Quaternion.Euler(i==0?32:18,i==0?-35:i==1?145:35,0);
            }
            Directory.CreateDirectory(Output+"/Captures");
            try
            {
                for(int view=0;view<3;view++)
                {
                    camera.transform.position=view==0?P(4,2.8f,8):view==1?P(0,2.3f,9):P(-4,2.7f,-8);camera.transform.LookAt(P(0,1.94f,0));
                    var rt=RenderTexture.GetTemporary(1400,1600,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var active=RenderTexture.active;var image=new Texture2D(1400,1600,TextureFormat.RGB24,false);
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1600),0,0);image.Apply();File.WriteAllBytes(Output+"/Captures/Studio_"+(view==0?"Hero":view==1?"Front":"Back")+".png",image.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
                }
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        [MenuItem("Arcane Archive/Knight/Build sealed guardian reference")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before building.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var mats=new[]{Mat("PlanishedSilver",new Color(.48f,.52f,.55f),.78f,.40f),Mat("AgedGold",new Color(.48f,.34f,.17f),.72f,.35f),Mat("DarkJoints",new Color(.055f,.065f,.075f),.5f,.29f),Mat("IvoryLinen",new Color(.72f,.68f,.57f),0,.18f),Mat("IndigoCord",new Color(.055f,.13f,.23f),0,.24f),Mat("ArchiveLeather",new Color(.16f,.10f,.06f),0,.25f),Mat("WeatheredLinen",new Color(.40f,.38f,.32f),0,.14f)};
            var model=PrefabUtility.LoadPrefabContents(Model);
            try
            {
                var k=model.GetComponent<KnightVisuals>();var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                // Author hand geometry in its neutral bind frame on every rebuild.
                k.rightHand.localRotation=Quaternion.identity;k.leftHand.localRotation=Quaternion.identity;
                foreach(var skin in skins)
                {
                    var g=new Geo{segments=skin.name.Contains("LOD1")?10:16};Body(g,k,skin);
                    skin.sharedMesh=Save(g.Mesh("SealedGuardian_"+skin.name,skin.bones.Select(b=>b.worldToLocalMatrix*skin.transform.localToWorldMatrix).ToArray()),"SealedGuardian_"+skin.name);
                    skin.sharedMaterials=mats;skin.localBounds=new Bounds(P(0,1.7f,0),P(6,7,6));
                }
                foreach(var r in k.weapon.GetComponentsInChildren<MeshRenderer>(true)){Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);}
                var existing=k.weapon.Find("Seal Greatsword");var sword=existing?existing.gameObject:new GameObject("Seal Greatsword");sword.transform.SetParent(k.weapon,false);
                var geometry=new Geo();Sword(geometry);sword.AddComponent<MeshFilter>().sharedMesh=Save(geometry.Mesh("SealGreatsword"),"SealGreatsword");sword.AddComponent<MeshRenderer>().sharedMaterials=mats;
                k.swordTip.localPosition=P(0,2.64f,0);k.refinedEffects.bladeBase.localPosition=P(0,1.02f,0);
                k.sealedGreatswordGrip=true;
                k.productionRig.rightGrip.SetLocalPositionAndRotation(P(-.145f,.70f,.045f),Quaternion.Euler(0,0,90));
                k.productionRig.leftGrip.SetLocalPositionAndRotation(P(.145f,.32f,.045f),Quaternion.Euler(0,0,-90));
                k.weapon.localRotation=Quaternion.Inverse(k.productionRig.rightGrip.localRotation);
                k.weapon.localPosition=-(k.weapon.localRotation*k.productionRig.rightGrip.localPosition);
                k.rightHand.localRotation=k.productionRig.rightGrip.localRotation;
                k.oathPresentation.seals=skins.Select(s=>new KnightOathPresentation.SealSlot{renderer=s,materialIndex=1}).ToArray();
                PrefabUtility.SaveAsPrefabAsset(model,Model);
            }
            finally{PrefabUtility.UnloadPrefabContents(model);}
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);config.AddMobilityMoves();EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();Debug.Log("SEALED_GUARDIAN_BUILT");
        }
        public static void CompleteFont()
        {
            string path=Root+"/GuardianChineseSupplement.asset";var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
            if(!font)
            {
                font=TMPro.TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/msyh.ttc",0,36,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024);
                var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);
                string text=string.Concat(config.moves.Select(m=>m.title+m.hint))+"侧闪后释放一轮封印弹连续三次横移或吞印预告扇形穿隙越过扩散";
                if(!font.TryAddCharacters(text,out var missing))throw new InvalidOperationException("Missing glyphs: "+missing);
                font.atlasPopulationMode=TMPro.AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font,path);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);
            }
            var primary=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/Bosses/Knight/Refined/KnightRefinedChinese.asset");
            if(!primary.fallbackFontAssetTable.Contains(font)){primary.fallbackFontAssetTable.Add(font);EditorUtility.SetDirty(primary);}
            AssetDatabase.SaveAssets();
        }
    }
}
