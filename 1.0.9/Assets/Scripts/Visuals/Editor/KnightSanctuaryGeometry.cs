using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    // Editor-only source geometry. Every generated mesh remains editable and independent of scene world coordinates.
    public static class KnightSanctuaryGeometry
    {
        static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        public static string MeshFolder;
        public static void Reset(string folder){MeshFolder=folder;cache.Clear();}
        public static GameObject Node(string name,Transform parent,Vector3 position=default)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
        public static T Save<T>(T obj,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing){EditorUtility.CopySerialized(obj,existing);Object.DestroyImmediate(obj);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(obj,path);return obj;
        }
        static Mesh Mesh(string key,List<Vector3> v,List<int> t,List<Vector2> uv)
        {
            var mesh=new Mesh{name=key,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            return Save(mesh,MeshFolder+"/"+key+".asset");
        }
        static void Q(List<int> t,int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
        public static GameObject Place(string name,Transform parent,Vector3 p,Mesh mesh,Material mat)
        {var g=Node(name,parent,p);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
        public static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,float bevel=.035f)
        {
            string key="Box_"+size.ToString("F3").Replace(" ","")+"_"+bevel.ToString("F3");
            if(!cache.TryGetValue(key,out var mesh)){
                float x=size.x/2,y=size.y/2,z=size.z/2,b=Mathf.Min(bevel,Mathf.Min(x,Mathf.Min(y,z))*.65f);
                Vector2[] shape={new Vector2(-x+b,-y),new Vector2(x-b,-y),new Vector2(x,-y+b),new Vector2(x,y-b),new Vector2(x-b,y),new Vector2(-x+b,y),new Vector2(-x,y-b),new Vector2(-x,-y+b)};
                var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
                for(int r=0;r<4;r++)foreach(var q in shape){float f=r==0||r==3?b:0;v.Add(new Vector3(q.x*(1-f/x),q.y*(1-f/y),r==0?-z:r==1?-z+b:r==2?z-b:z));}
                v.Add(new Vector3(0,0,-z));v.Add(new Vector3(0,0,z));
                for(int n=0;n<8;n++){int j=(n+1)%8;t.AddRange(new[]{32,j,n,33,24+n,24+j});for(int r=0;r<3;r++)Q(t,r*8+n,r*8+j,(r+1)*8+j,(r+1)*8+n);}
                // Split faces for stone-like hard edges and physical planar texture scale.
                var vv=new List<Vector3>();var tt=new List<int>();
                for(int i=0;i<t.Count;i+=3){var a=v[t[i]];var c=v[t[i+1]];var d=v[t[i+2]];var normal=Vector3.Cross(c-a,d-a).normalized;
                    foreach(var pt in new[]{a,c,d}){tt.Add(vv.Count);vv.Add(pt);uv.Add(Mathf.Abs(normal.y)>.65f?new Vector2(pt.x,pt.z):Mathf.Abs(normal.x)>.65f?new Vector2(pt.z,pt.y):new Vector2(pt.x,pt.y));}}
                mesh=Mesh(key,vv,tt,uv);cache[key]=mesh;
            }
            return Place(name,parent,p,mesh,mat);
        }
        public static GameObject Lathe(string name,Transform parent,Vector3 p,Vector2[] profile,Material mat,int segments=24)
        {
            string key="Lathe_"+string.Join("_",profile.Select(q=>q.ToString("F2")).ToArray()).Replace(" ","")+"_"+segments;
            if(!cache.TryGetValue(key,out var mesh)){
                var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
                for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++){float a=i*2*Mathf.PI/segments;v.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2(i/(float)segments*profile[j].x*6.28f,profile[j].y));}
                for(int j=0;j<profile.Length-1;j++)for(int i=0;i<segments;i++)Q(t,j*(segments+1)+i,(j+1)*(segments+1)+i,(j+1)*(segments+1)+i+1,j*(segments+1)+i+1);
                mesh=Mesh(key,v,t,uv);cache[key]=mesh;
            }
            return Place(name,parent,p,mesh,mat);
        }
        public static GameObject Tube(string name,Transform parent,Vector3[] points,float radius,Material mat,int sides=8)
        {
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();float distance=0;
            for(int i=0;i<points.Length;i++){
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var u=Vector3.Cross(tangent,Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.95f?Vector3.forward:Vector3.up).normalized;var w=Vector3.Cross(tangent,u);
                if(i>0)distance+=Vector3.Distance(points[i],points[i-1]);
                for(int j=0;j<=sides;j++){float a=j*2*Mathf.PI/sides;v.Add(points[i]+radius*(Mathf.Cos(a)*u+Mathf.Sin(a)*w));uv.Add(new Vector2(j/(float)sides*2*Mathf.PI*radius,distance));}
                if(i>0)for(int j=0;j<sides;j++)Q(t,(i-1)*(sides+1)+j,(i-1)*(sides+1)+j+1,i*(sides+1)+j+1,i*(sides+1)+j);
            }
            // Geometric data are baked once into a module or runtime region by Combine().
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            return Place(name,parent,Vector3.zero,mesh,mat);
        }
        public static Vector3[] ArchPath(float width,float spring,float rise,float z=0,int count=48)
        {
            var pts=new Vector3[count+1];
            for(int i=0;i<=count;i++){float x=Mathf.Lerp(-width,width,i/(float)count),u=Mathf.Abs(x)/width;float y=spring+rise*Mathf.Sqrt(Mathf.Max(0,4-(1+u)*(1+u)))/Mathf.Sqrt(3);pts[i]=new Vector3(x,y,z);}
            return pts;
        }
        public static GameObject Arch(string name,Transform parent,float w,float spring,float rise,float radius,float z,Material mat)
        {return Tube(name,parent,ArchPath(w,spring,rise,z),radius,mat,10);}
        public static void Ring(string name,Transform parent,Vector3 center,float radius,float tube,Material mat,bool vertical=false,float start=0,float span=360)
        {
            int count=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(span)*Mathf.Deg2Rad*radius/.07f),12,180);var pts=new Vector3[count+1];
            for(int i=0;i<=count;i++){float a=(start+span*i/count)*Mathf.Deg2Rad;pts[i]=center+(vertical?new Vector3(Mathf.Sin(a)*radius,Mathf.Cos(a)*radius,0):new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));}
            Tube(name,parent,pts,tube,mat);
        }
        public static void FlatArc(string name,Transform parent,Vector3 center,float radius,float halfWidth,float halfDepth,Material mat,float start,float span)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            int count=Mathf.Max(32,Mathf.CeilToInt(span/2));float bevel=Mathf.Min(.009f,halfDepth*.5f);
            var profile=new[]{new Vector2(-halfWidth+bevel,-halfDepth),new Vector2(halfWidth-bevel,-halfDepth),new Vector2(halfWidth,-halfDepth+bevel),new Vector2(halfWidth,halfDepth-bevel),new Vector2(halfWidth-bevel,halfDepth),new Vector2(-halfWidth+bevel,halfDepth),new Vector2(-halfWidth,halfDepth-bevel),new Vector2(-halfWidth,-halfDepth+bevel)};
            for(int i=0;i<=count;i++){
                float a=(start+span*i/count)*Mathf.Deg2Rad;
                foreach(var p in profile){v.Add(center+new Vector3(Mathf.Sin(a)*(radius+p.x),Mathf.Cos(a)*(radius+p.x),p.y));uv.Add(new Vector2(a*radius,p.x+halfWidth));}
                if(i>0)for(int j=0;j<8;j++){int n=(j+1)%8;Q(t,(i-1)*8+j,(i-1)*8+n,i*8+n,i*8+j);}
            }
            for(int j=1;j<7;j++){t.AddRange(new[]{0,j+1,j});int a=count*8;t.AddRange(new[]{a,a+j,a+j+1});}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            Place(name,parent,Vector3.zero,mesh,mat);
        }
        public static void Fan(string name,Transform parent,Vector3[] boundary,Material mat)
        {
            var v=new List<Vector3>(boundary);var uv=v.Select(x=>new Vector2(x.x,x.y)).ToList();var t=new List<int>();
            for(int i=1;i<v.Count-1;i++)t.AddRange(new[]{0,i,i+1});
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            Place(name,parent,Vector3.zero,mesh,mat);
        }
        public static void Combine(Transform parent,string label)
        {
            var fs=parent.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).ToArray();
            int n=0;
            foreach(var group in fs.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial)){
                var combines=group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{name=label+"_"+n,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
                var saved=Save(mesh,MeshFolder+"/"+label+"_"+n+".asset");Place(label+" / "+group.Key.name,parent,Vector3.zero,saved,group.Key);n++;
            }
            foreach(var f in fs){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
        }
    }
}
