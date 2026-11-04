using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using ArcaneArchive.Core;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    public static class SealedDuelArenaBuilder
    {
        public const string Root="Assets/Art/SealedDuelArena20261104";
        const string Original="Assets/Art/ForbiddenWarden/CombatV2/Prefabs/SealChamberRigged.prefab";
        const string Library="Assets/Scenes/MilanLibrary_Magic_V6.unity";
        static Material stone,edge,basalt,joints,bronze,dark,teal,amber,fabric,door,vault,wall;
        static Material[] floorMats;
        static Transform environment;
        static readonly Dictionary<string,List<CombineInstance>> batches=new Dictionary<string,List<CombineInstance>>();
        static readonly Dictionary<string,Material> batchMaterials=new Dictionary<string,Material>();
        static readonly List<Mesh> temporary=new List<Mesh>();
        static System.Random random;
        static GameObject Node(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
        static Matrix4x4 Pose(Vector3 p,Quaternion? rotation=null)=>Matrix4x4.TRS(p,rotation??Quaternion.identity,Vector3.one);
        static void Add(Mesh mesh,Material material,Matrix4x4 pose,int sector=0)
        {
            string key=sector.ToString("00")+"_"+material.name;
            if(!batches.TryGetValue(key,out var list)){list=new List<CombineInstance>();batches[key]=list;batchMaterials[key]=material;}
            list.Add(new CombineInstance{mesh=mesh,transform=pose});
        }
        static void Box(Vector3 size,Vector3 p,Material m,Matrix4x4 basis,int sector=0,float bevel=.025f,Quaternion? rotation=null)
        {Add(SealedDuelArenaGeometry.Box(size,bevel),m,basis*Pose(p,rotation),sector);}
        static void Line(Vector3[] points,float width,float depth,Vector3 p,Material m,Matrix4x4 basis,int sector=0)
        {var mesh=SealedDuelArenaGeometry.Stroke(points,width,depth);temporary.Add(mesh);Add(mesh,m,basis*Pose(p),sector);}
        static void Rod(Vector3 a,Vector3 b,float radius,Material m,int sector)
        {var mesh=SealedDuelArenaGeometry.Tube(new[]{a,b},radius);temporary.Add(mesh);Add(mesh,m,Matrix4x4.identity,sector);}
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Material Mat(string name,string texture,Color tint,float metal=0,float smooth=.25f,Color? emission=null)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            if(texture!=null)
            {
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Base.png"));
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",texture.StartsWith("PH_")?.48f:.12f);
                m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_MetalSmooth.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
                if(texture.StartsWith("PH_")){m.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Occlusion.png"));m.EnableKeyword("_OCCLUSIONMAP");m.SetFloat("_OcclusionStrength",.65f);m.SetTextureScale("_BaseMap",Vector2.one*(texture=="PH_Floor"?.82f:.8333f));}
            }
            if(emission.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);}else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
            EditorUtility.SetDirty(m);return m;
        }
        static void PrepareMaterials()
        {
            foreach(string path in Directory.GetFiles(Root+"/Textures","*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.maxTextureSize=path.Contains("PH_")?2048:1024;importer.mipmapEnabled=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                if(path.Contains("_Normal")){importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;}
                else {importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!path.Contains("_MetalSmooth")&&!path.Contains("_Occlusion");}
                importer.SaveAndReimport();
            }
            stone=Mat("01_WarmGrey_Limestone","Limestone",new Color(.79f,.81f,.84f));
            edge=Mat("02_Carved_EdgeStone","WeatheredStone",new Color(.92f,.93f,.94f));
            basalt=Mat("03_Dark_Basalt","Basalt",new Color(.86f,.91f,.96f));
            joints=Mat("04_Deep_Stone_Joints",null,new Color(.095f,.105f,.115f),0,.18f);
            bronze=Mat("05_Worn_Bronze","Bronze",new Color(1,.91f,.75f),.78f,.4f);
            dark=Mat("06_Recessed_DarkStone","Basalt",new Color(.78f,.80f,.82f));
            door=Mat("13_Gate_Aged_DoorMetal","Bronze",new Color(.92f,.88f,.82f),.58f,.22f);
            vault=Mat("14_Vault_CutStone","PH_Wall",new Color(.94f,.95f,.96f));
            wall=Mat("15_Wall_CutStone","PH_Wall",new Color(.79f,.82f,.87f));
            teal=Mat("07_Quiet_Seal_Inlay",null,new Color(.10f,.28f,.31f),.35f,.35f,new Color(.08f,.24f,.28f)*.55f);
            amber=Mat("08_Lantern_Glass",null,new Color(.92f,.60f,.24f),.05f,.35f,new Color(1,.59f,.24f)*2.4f);
            fabric=Mat("09_Faded_Archive_Banner",null,new Color(.11f,.065f,.08f),0,.12f);
            floorMats=new[]{Mat("10_Floor_CutStone_A","PH_Floor",new Color(.87f,.89f,.90f)),Mat("11_Floor_CutStone_B","PH_Floor",new Color(.80f,.83f,.86f)),Mat("12_Floor_CutStone_C","PH_Floor",new Color(.92f,.92f,.89f)),basalt};
        }
        static void Flush()
        {
            foreach(var pair in batches)
            {
                var mesh=new Mesh{name=pair.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                var saved=SaveMesh(mesh,pair.Key);var go=Node("Masonry_"+pair.Key,environment);go.AddComponent<MeshFilter>().sharedMesh=saved;
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=batchMaterials[pair.Key];r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;go.isStatic=true;
            }
            batches.Clear();batchMaterials.Clear();foreach(var mesh in temporary)if(mesh)Object.DestroyImmediate(mesh);temporary.Clear();SealedDuelArenaGeometry.ClearCache();
        }
        static void Floor()
        {
            var baseMesh=SealedDuelArenaGeometry.FloorTile(0,16.75f,0,360,-.012f,192);temporary.Add(baseMesh);Add(baseMesh,joints,Matrix4x4.identity,40);
            float[] rings={0,2.4f,4.6f,6.8f,9.0f,11.2f,13.4f,16.75f};
            for(int row=0;row<rings.Length-1;row++)
            {
                int count=row==0?24:48;float turn=row%2==0?0:3.75f;
                for(int i=0;i<count;i++)
                {
                    var slab=SealedDuelArenaGeometry.FloorTile(rings[row]+(row==0?0:.014f),rings[row+1]-.014f,i*360f/count+turn+.035f,(i+1)*360f/count+turn-.035f,0,6);
                    temporary.Add(slab);Add(slab,row==3?floorMats[3]:floorMats[(i*17+row*7)%3],Matrix4x4.identity,40);
                }
            }
            foreach(float r in new[]{2.4f,6.8f,8.85f,9.08f,11.9f,14.6f,15.2f})
            {var ring=SealedDuelArenaGeometry.Ring(r,r==8.85f?.055f:.022f,.006f);temporary.Add(ring);Add(ring,bronze,Matrix4x4.identity,41);}
            // The seal is entirely flush; no dais, central prop or raised collision.
            for(int i=0;i<48;i++)
            {
                float a=i*7.5f*Mathf.Deg2Rad;Vector3 p=new Vector3(Mathf.Sin(a)*8.98f,.009f,Mathf.Cos(a)*8.98f);
                var q=Quaternion.Euler(0,i*7.5f,0);Box(new Vector3(.022f,.008f,.12f),p,i%4==0?teal:bronze,Matrix4x4.identity,41,.002f,q);
                if(i%4==0){var r=SealedDuelArenaGeometry.Ring(9.0f,.018f,.01f,12,i*7.5f+.4f,i*7.5f+3.2f);temporary.Add(r);Add(r,teal,Matrix4x4.identity,41);}
            }
            for(int i=0;i<8;i++)
            {
                float a=i*45*Mathf.Deg2Rad;Vector3 p0=new Vector3(Mathf.Sin(a)*.5f,.01f,Mathf.Cos(a)*.5f),p1=new Vector3(Mathf.Sin(a)*11.85f,.01f,Mathf.Cos(a)*11.85f);
                Rod(p0,p1,.012f,bronze,41);
                var q=Quaternion.Euler(0,i*45,0);Box(new Vector3(.22f,.009f,.22f),p1,bronze,Matrix4x4.identity,41,.015f,q*Quaternion.Euler(0,45,0));
            }
            var diamond=new[]{new Vector3(0,.01f,.82f),new Vector3(.47f,.01f,0),new Vector3(0,.01f,-.82f),new Vector3(-.47f,.01f,0),new Vector3(0,.01f,.82f)};
            for(int i=0;i<4;i++)Rod(diamond[i],diamond[i+1],.023f,bronze,41);
            for(int i=0;i<21;i++)
            {
                float angle=(float)random.NextDouble()*360,r=12.5f+(float)random.NextDouble()*3.2f;Vector3 p=new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*r,.009f,Mathf.Cos(angle*Mathf.Deg2Rad)*r);
                for(int k=0;k<4;k++){var end=p+new Vector3((float)random.NextDouble()*.5f-.25f,0,(float)random.NextDouble()*.7f-.35f);Rod(p,end,.007f,joints,42);p=end;}
            }
            var collision=Node("Flat_CombatFloor_Collider",environment);var mc=collision.AddComponent<MeshCollider>();
            mc.sharedMesh=SaveMesh(SealedDuelArenaGeometry.FloorTile(0,16.75f,0,360,0,192),"Flat_CombatFloor_Collision");
        }
        static Vector2[] ColumnProfile(float h,float r)=>new[]{new Vector2(0,0),new Vector2(r*1.75f,0),new Vector2(r*1.75f,.12f),new Vector2(r*1.60f,.19f),new Vector2(r*1.60f,.36f),new Vector2(r*1.35f,.45f),new Vector2(r*1.25f,.62f),new Vector2(r,.78f),new Vector2(r,h-.47f),new Vector2(r*1.2f,h-.39f),new Vector2(r*1.45f,h-.28f),new Vector2(r*1.55f,h-.12f),new Vector2(r*1.65f,h),new Vector2(0,h)};
        static void Column(Matrix4x4 basis,int sector)
        {
            CapitalLeaves(basis,4.85f,.34f,sector);CapitalLeaves(basis,10.50f,.24f,sector);
            Add(SealedDuelArenaGeometry.Lathe("MainPier",ColumnProfile(5.48f,.35f),48,.012f),stone,basis,sector);
            foreach(float x in new[]{-.34f,.34f})foreach(float z in new[]{-.16f,.16f})
                Add(SealedDuelArenaGeometry.Lathe("PierShaft",ColumnProfile(5.48f,.14f),24),edge,basis*Pose(new Vector3(x,0,z)),sector);
            Box(new Vector3(1.24f,.16f,1.02f),new Vector3(0,.08f,0),edge,basis,sector,.045f);
            Box(new Vector3(1.12f,.12f,.94f),new Vector3(0,5.52f,0),bronze,basis,sector,.015f);
            Add(SealedDuelArenaGeometry.Lathe("UpperPier",ColumnProfile(5.45f,.27f),40,.009f),stone,basis*Pose(new Vector3(0,5.72f,0)),sector);
            Box(new Vector3(.89f,.14f,.82f),new Vector3(0,11.21f,0),bronze,basis,sector,.02f);
            for(int k=0;k<4;k++)
            {
                float y=k==0?.48f:k==1?5.13f:k==2?6.23f:10.84f;
                Box(new Vector3(.84f,.065f,.74f),new Vector3(0,y,-.03f),bronze,basis,sector,.01f);
            }
        }
        static void CapitalLeaves(Matrix4x4 basis,float y,float radius,int sector)
        {
            for(int n=0;n<10;n++)
            {
                var rotation=Quaternion.Euler(0,n*36,0);
                var position=rotation*new Vector3(0,0,-radius)+Vector3.up*y;
                Add(SealedDuelArenaGeometry.CarvedLeaf(),edge,basis*Pose(position,rotation),sector);
            }
        }
        static void Tracery(Matrix4x4 basis,float bottom,float spring,float rise,float half,int sector)
        {
            var face=SealedDuelArenaGeometry.ArchFace(half,bottom,spring,rise);temporary.Add(face);Add(face,dark,basis*Pose(new Vector3(0,0,-.10f)),sector);
            Line(SealedDuelArenaGeometry.Arch(half+.10f,bottom,spring,rise+.08f),.29f,.30f,new Vector3(0,0,-.02f),edge,basis,sector);
            Line(SealedDuelArenaGeometry.Arch(half-.07f,bottom+.10f,spring,rise-.12f),.045f,.08f,new Vector3(0,0,-.22f),bronze,basis,sector);
            foreach(int sign in new[]{-1,1}){
                var lancet=SealedDuelArenaGeometry.Arch(half*.40f,bottom+.17f,spring-.62f,.72f);
                Line(lancet,.065f,.10f,new Vector3(sign*half*.47f,0,-.18f),edge,basis,sector);
                Line(lancet,.018f,.035f,new Vector3(sign*half*.47f,0,-.245f),bronze,basis,sector);
            }
            // Individually cut arch stones provide real joint depth around the pointed head.
            var stonePath=SealedDuelArenaGeometry.Arch(half+.25f,bottom,spring,rise+.22f,20);
            for(int k=1;k<stonePath.Length-2;k++)
            {
                var a=Vector3.Lerp(stonePath[k],stonePath[k+1],.035f);var b=Vector3.Lerp(stonePath[k],stonePath[k+1],.965f);
                Line(new[]{a,b},.22f,.21f,new Vector3(0,0,-.19f),stone,basis,sector);
            }
            float center=spring+.15f;
            Line(new[]{new Vector3(-half+.18f,bottom+.9f,0),new Vector3(0,center,0),new Vector3(half-.18f,bottom+.9f,0)},.045f,.055f,new Vector3(0,0,-.16f),bronze,basis,sector);
            var diamond=new[]{new Vector3(0,center+.38f,0),new Vector3(.24f,center,0),new Vector3(0,center-.38f,0),new Vector3(-.24f,center,0),new Vector3(0,center+.38f,0)};
            Line(diamond,.055f,.07f,new Vector3(0,0,-.20f),bronze,basis,sector);
            Box(new Vector3(.035f,spring-bottom+.35f,.06f),new Vector3(0,(bottom+spring)*.5f,-.14f),bronze,basis,sector,.003f);
        }
        static void Gallery(int i,Matrix4x4 basis)
        {
            float start=i*15-7.5f,end=i*15+7.5f;
            foreach(var pair in new[]{new[]{5.50f,16.7f,.20f},new[]{5.70f,16.65f,.09f},new[]{5.39f,16.7f,.08f}})
            {
                var slab=SealedDuelArenaGeometry.FloorTile(14.83f,pair[1],start,end,pair[0],10);temporary.Add(slab);Add(slab,edge,Matrix4x4.identity,i);
                for(int n=0;n<10;n++)
                {float a=Mathf.Lerp(start,end,n/10f)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(n+1)/10f)*Mathf.Deg2Rad;Vector3 p=new Vector3(Mathf.Sin(a)*14.83f,pair[0],Mathf.Cos(a)*14.83f),q=new Vector3(Mathf.Sin(b)*14.83f,pair[0],Mathf.Cos(b)*14.83f);Rod(p,q,pair[2],pair[0]==5.70f?bronze:edge,i);}
            }
            for(int n=0;n<=6;n++)
            {
                float a=(start+(end-start)*n/6)*Mathf.Deg2Rad;var q=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                Box(new Vector3(n%6==0?.09f:.045f,1.12f,n%6==0?.09f:.045f),new Vector3(Mathf.Sin(a)*14.83f,6.25f,Mathf.Cos(a)*14.83f),bronze,Matrix4x4.identity,i,.008f,q);
            }
            foreach(float h in new[]{5.85f,6.72f})
            {
                var path=new List<Vector3>();for(int n=0;n<=12;n++){float a=Mathf.Lerp(start,end,n/12f)*Mathf.Deg2Rad;path.Add(new Vector3(Mathf.Sin(a)*14.83f,h,Mathf.Cos(a)*14.83f));}
                var tube=SealedDuelArenaGeometry.Tube(path.ToArray(),h>6?.048f:.022f);temporary.Add(tube);Add(tube,bronze,Matrix4x4.identity,i);
            }
            for(int n=0;n<6;n++){
                float angle=(start+(n+.5f)*(end-start)/6)*Mathf.Deg2Rad;
                var railBasis=Pose(new Vector3(Mathf.Sin(angle)*14.83f,0,Mathf.Cos(angle)*14.83f),Quaternion.Euler(0,angle*Mathf.Rad2Deg,0));
                Line(SealedDuelArenaGeometry.Arch(.24f,5.91f,6.22f,.34f,8),.032f,.045f,Vector3.zero,bronze,railBasis,i);
            }
            // Corbels remain at the perimeter and never enter the player clearance disk.
            foreach(float x in new[]{-1.2f,1.2f})
            {
                Box(new Vector3(.22f,.80f,.82f),new Vector3(x,5.0f,-.35f),stone,basis,i,.04f,Quaternion.Euler(-24,0,0));
                Line(SealedDuelArenaGeometry.Arch(.48f,4.35f,4.80f,.44f),.09f,.13f,new Vector3(x,0,-.67f),bronze,basis,i);
            }
        }
        static void Walls()
        {
            for(int i=0;i<24;i++)
            {
                float angle=i*15;var q=Quaternion.Euler(0,angle,0);Vector3 p=q*new Vector3(0,0,16.18f);var basis=Pose(p,q);
                // Main gate spans three bays. The outer closed boundary remains behind it.
                bool gate=i==0||i==1||i==23;
                Box(new Vector3(4.40f,11.25f,.65f),new Vector3(0,5.625f,.42f),wall,basis,i,.035f);
                if(!gate)
                {
                    Box(new Vector3(3.25f,1.06f,.19f),new Vector3(0,.64f,-.10f),edge,basis,i,.04f);
                    for(int k=0;k<3;k++)Box(new Vector3(.87f,.64f,.075f),new Vector3((k-1)*1.02f,.65f,-.23f),basalt,basis,i,.025f);
                    Tracery(basis,1.30f,3.50f,1.64f,1.47f,i);
                    Tracery(basis,6.79f,9.33f,1.68f,1.47f,i);
                    Gallery(i,basis);
                    if(i%2==0 && i!=4)Lantern(basis,new Vector3(0,3.15f,-.46f),i);
                }
                var pierBasis=Pose(Quaternion.Euler(0,angle+7.5f,0)*new Vector3(0,0,15.98f),Quaternion.Euler(0,angle+7.5f,0));Column(pierBasis,i);
                foreach(float y in new[]{1.18f,5.46f,11.10f})
                    Box(new Vector3(4.46f,.16f,.31f),new Vector3(0,y,-.03f),edge,basis,i,.035f);
                var boundary=Node("PerimeterCollider_"+i,environment);boundary.transform.SetPositionAndRotation(p,q);var c=boundary.AddComponent<BoxCollider>();c.center=new Vector3(0,5.6f,.27f);c.size=new Vector3(4.5f,11.2f,.75f);
            }
        }
        static void Gate()
        {
            var basis=Pose(new Vector3(0,0,15.74f));const int sector=30;
            Box(new Vector3(7.65f,10.6f,.55f),new Vector3(0,5.3f,.38f),wall,basis,sector,.09f);
            var face=SealedDuelArenaGeometry.ArchFace(3.12f,.10f,6.10f,3.66f);temporary.Add(face);Add(face,door,basis*Pose(new Vector3(0,0,-.14f)),sector);
            for(int j=0;j<4;j++)Line(SealedDuelArenaGeometry.Arch(3.15f+j*.16f,.04f,6.1f,3.66f+j*.11f),j%2==0?.19f:.065f,j%2==0?.29f:.10f,new Vector3(0,0,-.22f-j*.05f),j%2==0?edge:bronze,basis,sector);
            foreach(int sign in new[]{-1,1})
            {
                var column=Pose(new Vector3(sign*3.75f,0,-.03f));Add(SealedDuelArenaGeometry.Lathe("GateJamb",ColumnProfile(9.5f,.35f),40),stone,basis*column,sector);
                Box(new Vector3(.71f,8.55f,.23f),new Vector3(sign*3.4f,4.3f,-.50f),edge,basis,sector,.065f);
                // Small inset rosettes follow the jamb face; large diamonds read as broken joins at gameplay distance.
                for(int n=0;n<7;n++)
                {
                    var rosette=new List<Vector3>();
                    for(int k=0;k<=48;k++){float a=k*Mathf.PI*2/48;float r=.105f+.018f*Mathf.Cos(a*6);rosette.Add(new Vector3(sign*3.4f+Mathf.Cos(a)*r,.8f+n*1.08f+Mathf.Sin(a)*r,-.635f));}
                    Line(rosette.ToArray(),.025f,.025f,Vector3.zero,bronze,basis,sector);
                }
                for(int n=0;n<3;n++)
                    Line(SealedDuelArenaGeometry.Arch(.82f,.50f,3.50f,1.60f),.055f,.055f,new Vector3(sign*(.98f+n*.28f),2.70f-n*.55f,-.29f),bronze,basis,sector);
            }
            Box(new Vector3(.045f,9.35f,.10f),new Vector3(0,4.74f,-.36f),bronze,basis,sector,.004f);
            for(int n=0;n<4;n++)
            {
                float radius=1.32f+n*.16f;var path=new List<Vector3>();for(int k=0;k<=128;k++){float a=k*2*Mathf.PI/128;path.Add(new Vector3(Mathf.Cos(a)*radius,4.50f+Mathf.Sin(a)*radius,-.175f));}
                Line(path.ToArray(),n%2==0?.045f:.018f,.065f,Vector3.zero,n==1?teal:bronze,basis,sector);
            }
            var diamond=new[]{new Vector3(0,6.25f,0),new Vector3(1.34f,4.50f,0),new Vector3(0,2.75f,0),new Vector3(-1.34f,4.50f,0),new Vector3(0,6.25f,0)};
            Line(diamond,.055f,.08f,new Vector3(0,0,-.18f),bronze,basis,sector);
            for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;Line(new[]{new Vector3(Mathf.Sin(a)*.38f,4.5f+Mathf.Cos(a)*.38f,0),new Vector3(Mathf.Sin(a)*2.4f,4.5f+Mathf.Cos(a)*2.4f,0)},.027f,.05f,new Vector3(0,0,-.18f),bronze,basis,sector);}
            Box(new Vector3(.13f,.40f,.12f),new Vector3(-.17f,2.02f,-.21f),bronze,basis,sector,.04f);
            Box(new Vector3(.13f,.40f,.12f),new Vector3(.17f,2.02f,-.21f),bronze,basis,sector,.04f);
        }
        static bool Breach(int sector,int band)=>sector>=3&&sector<=5&&band>=1&&band<=3;
        static void Vault()
        {
            for(int i=0;i<24;i++)for(int j=0;j<6;j++)
            {
                
                var patch=SealedDuelArenaGeometry.VaultPatch(i*15,(i+1)*15,j*15,(j+1)*15,8,6);temporary.Add(patch);Add(patch,vault,Matrix4x4.identity,50+i/4);
            }
            for(int i=0;i<24;i++)
            {
                float angle=i*15*Mathf.Deg2Rad;var points=new List<Vector3>();
                Action flush=()=>{if(points.Count>1){var mesh=SealedDuelArenaGeometry.Tube(points.ToArray(),.13f,12);temporary.Add(mesh);Add(mesh,edge,Matrix4x4.identity,56);var gold=SealedDuelArenaGeometry.Tube(points.Select(p=>p-Vector3.up*.14f).ToArray(),.025f,8);temporary.Add(gold);Add(gold,bronze,Matrix4x4.identity,56);}points.Clear();};
                for(int j=0;j<=48;j++)
                {
                    float t=j/48f*Mathf.PI*.5f;bool gap=SealedDuelArenaGeometry.VaultOpening(i*15,j/48f*90);
                    if(gap){flush();continue;}
                    points.Add(new Vector3(Mathf.Sin(angle)*16.43f*Mathf.Cos(t),11.21f+6.25f*Mathf.Sin(t),Mathf.Cos(angle)*16.43f*Mathf.Cos(t)));
                }flush();
            }
            foreach(float degrees in new[]{0f,30f,60f})
            {
                float lat=degrees*Mathf.Deg2Rad;var path=new List<Vector3>();
                for(int i=0;i<=192;i++)
                {
                    float a=i*2*Mathf.PI/192;
                    if(SealedDuelArenaGeometry.VaultOpening(a*Mathf.Rad2Deg,degrees)){if(path.Count>1){var m=SealedDuelArenaGeometry.Tube(path.ToArray(),.07f);temporary.Add(m);Add(m,bronze,Matrix4x4.identity,56);}path.Clear();continue;}
                    path.Add(new Vector3(Mathf.Sin(a)*16.43f*Mathf.Cos(lat),11.17f+6.25f*Mathf.Sin(lat),Mathf.Cos(a)*16.43f*Mathf.Cos(lat)));
                }
                if(path.Count>1){var m=SealedDuelArenaGeometry.Tube(path.ToArray(),.07f);temporary.Add(m);Add(m,bronze,Matrix4x4.identity,56);}
            }
            // Exterior ruined buttresses are grounded beyond the arena shell, visible through the breach.
            for(int k=0;k<5;k++)
            {
                float angle=(48+k*8)*Mathf.Deg2Rad;float height=21.5f+(k%3)*1.2f;
                var outer=Pose(new Vector3(Mathf.Sin(angle)*22.5f,0,Mathf.Cos(angle)*22.5f),Quaternion.Euler(0,angle*Mathf.Rad2Deg,0));
                Box(new Vector3(1.65f,height,2.1f),new Vector3(0,height*.5f,0),wall,outer,58,.10f);
                for(int level=0;level<4;level++)Box(new Vector3(1.9f,.24f,2.3f),new Vector3(0,12+level*2.4f,0),stone,outer,58,.04f);
                Box(new Vector3(1.1f,.6f,1.7f),new Vector3(.17f,height+.15f,.05f),stone,outer,58,.06f,Quaternion.Euler(8,k*19,12));
            }
            for(int n=0;n<18;n++)
            {
                float angle=48+n*2.0f;float a=angle*Mathf.Deg2Rad;float arc=Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((angle-66)/18,2)));float lat=(36+(n%2==0?-1:1)*arc*23)*Mathf.Deg2Rad;
                Vector3 p=new Vector3(Mathf.Sin(a)*16.45f*Mathf.Cos(lat),11.25f+6.25f*Mathf.Sin(lat),Mathf.Cos(a)*16.45f*Mathf.Cos(lat));
                Box(new Vector3(.44f,.28f,.57f),p,edge,Matrix4x4.identity,57,.035f,Quaternion.Euler(n*13,n*21,n*7));
            }
        }
        static void Lantern(Matrix4x4 basis,Vector3 p,int sector,bool pedestal=false)
        {
            if(pedestal)
            {
                Add(SealedDuelArenaGeometry.Lathe("StoneLanternBase",new[]{new Vector2(0,0),new Vector2(.38f,0),new Vector2(.38f,.12f),new Vector2(.29f,.22f),new Vector2(.24f,.55f),new Vector2(.32f,.63f),new Vector2(.32f,.75f),new Vector2(0,.75f)},8),stone,basis,sector);p+=Vector3.up*.78f;
            }
            Box(new Vector3(.30f,.58f,.24f),p,amber,basis,sector,.035f);
            foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.15f,.15f})Box(new Vector3(.045f,.69f,.045f),p+new Vector3(x,0,z),bronze,basis,sector,.009f);
            foreach(float h in new[]{-.36f,.36f})Box(new Vector3(.47f,.10f,.39f),p+Vector3.up*h,bronze,basis,sector,.025f);
            if(!pedestal){Box(new Vector3(.17f,.85f,.09f),p+Vector3.forward*.30f,bronze,basis,sector,.02f);Box(new Vector3(.12f,.10f,.45f),p+new Vector3(0,.45f,.12f),bronze,basis,sector,.02f);}
            var lamp=Node("Amber_Practical_"+sector,environment);lamp.transform.position=basis.MultiplyPoint3x4(p+Vector3.back*.22f);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.63f,.32f);light.intensity=10;light.range=7;light.shadows=LightShadows.None;
        }
        static void Dressing()
        {
            foreach(int i in new[]{7,11,17,21})
            {
                var q=Quaternion.Euler(0,i*15,0);var basis=Pose(q*new Vector3(0,0,15.20f),q);Lantern(basis,Vector3.up*.35f,60+i,true);
            }
            // Two small edge rubble groups, kept beyond the combat radius.
            foreach(int sector in new[]{4,15})for(int n=0;n<9;n++)
            {
                float a=(sector*15+(float)random.NextDouble()*8-4)*Mathf.Deg2Rad,r=15.08f+(float)random.NextDouble()*.75f;
                float h=.14f+(float)random.NextDouble()*.25f;
                Box(new Vector3(.25f+(float)random.NextDouble()*.35f,h,.20f+(float)random.NextDouble()*.4f),new Vector3(Mathf.Sin(a)*r,h*.5f,Mathf.Cos(a)*r),n%2==0?stone:edge,Matrix4x4.identity,70+sector,.025f,Quaternion.Euler(n*11,n*39,n*13));
            }
            foreach(int i in new[]{2,9,16,22})
            {
                float a=i*15;var q=Quaternion.Euler(0,a,0);var basis=Pose(q*new Vector3(0,0,15.80f),q);
                var s=new SealedDuelArenaGeometry.Surface();int columns=10,rows=20;
                Func<int,int,Vector3> p=(x,y)=>new Vector3((x/(float)columns-.5f)*.85f,9.50f-y/(float)rows*2.65f-(y==rows?(x%3)*.12f:0),-.53f+.045f*Mathf.Sin(x*1.9f+y*.7f));
                for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)s.Quad(p(x,y),p(x,y+1),p(x+1,y+1),p(x+1,y));
                var cloth=s.Mesh("Hanging peripheral banner");temporary.Add(cloth);Add(cloth,fabric,basis,80+i);
                Box(new Vector3(1.02f,.08f,.12f),new Vector3(0,9.55f,-.50f),bronze,basis,80+i,.02f);
                Line(new[]{new Vector3(-.22f,8.5f,0),new Vector3(0,9.12f,0),new Vector3(.22f,8.5f,0),new Vector3(0,7.90f,0),new Vector3(-.22f,8.5f,0)},.021f,.026f,new Vector3(0,0,-.61f),bronze,basis,80+i);
            }
        }
        static void Lightshafts()
        {
            string texturePath=Root+"/Materials/SoftDustShaft.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture){texture=new Texture2D(64,128,TextureFormat.RGBA32,false,true);texture.name="Soft dust scattering gradient";AssetDatabase.CreateAsset(texture,texturePath);}
            texture.wrapMode=TextureWrapMode.Clamp;
            for(int x=0;x<64;x++)for(int y=0;y<128;y++){
                float u=(x+.5f)/64*2-1,v=(y+.5f)/128;float alpha=Mathf.Pow(Mathf.Clamp01(1-u*u),3)*Mathf.Pow(Mathf.Sin(v*Mathf.PI),.9f);
                texture.SetPixel(x,y,new Color(1,1,1,alpha));
            }texture.Apply();EditorUtility.SetDirty(texture);
            string path=Root+"/Materials/Breach_DustLight.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",new Color(.65f,.77f,1,.045f));mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3050;EditorUtility.SetDirty(mat);
            Vector3 from=new Vector3(12.1f,14.8f,5.4f),to=new Vector3(2.2f,.25f,1.5f),axis=(to-from).normalized;
            for(int i=0;i<2;i++){
                Vector3 side=Vector3.Cross(axis,i==0?Vector3.forward:Vector3.right).normalized;
                var surface=new SealedDuelArenaGeometry.Surface();surface.Quad(from-side*.8f,from+side*.8f,to+side*3.4f,to-side*3.4f,new Vector2(0,1),new Vector2(1,1),new Vector2(1,0),new Vector2(0,0));
                var go=Node("Breach_Subtle_DustShaft_"+i,environment);go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(surface.Mesh("Dust light shaft"),"DustShaft_"+i);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        }
        static Material BuildStarSky()
        {
            const int size=1024;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true);cube.filterMode=FilterMode.Trilinear;cube.name="Sparse midnight stars";
            var random=new System.Random(61104);
            for(int face=0;face<6;face++){
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                    float u=(x+.5f)/(size*.5f)-1,v=(y+.5f)/(size*.5f)-1;
                    float altitude=face==2?1/Mathf.Sqrt(1+u*u+v*v):face==3?-1/Mathf.Sqrt(1+u*u+v*v):-v/Mathf.Sqrt(1+u*u+v*v);
                    pixels[y*size+x]=Color.Lerp(new Color(.008f,.012f,.028f),new Color(.002f,.004f,.014f),Mathf.Clamp01(altitude));
                }
                if(face!=3)for(int n=0;n<145;n++){
                    int x=random.Next(2,size-2),y=random.Next(2,size-2);if(face!=2 && y>size/2)continue;
                    float brightness=.5f+(float)random.NextDouble()*1.7f;var color=Color.Lerp(new Color(.7f,.8f,1),new Color(1,.88f,.7f),(float)random.NextDouble())*brightness;
                    pixels[y*size+x]+=color;
                    if(n%7==0)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)pixels[(y+dy)*size+x+dx]+=color*.075f;
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true,false);string path=Root+"/Materials/StarSky.asset";var old=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(old){EditorUtility.CopySerialized(cube,old);Object.DestroyImmediate(cube);EditorUtility.SetDirty(old);cube=old;}else AssetDatabase.CreateAsset(cube,path);
            path=Root+"/Materials/StarSky.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){var shader=Shader.Find("Skybox/Cubemap");if(!shader)throw new Exception("Missing cubemap sky shader");material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_Tex",cube);material.SetColor("_Tint",new Color(.5f,.5f,.5f));material.SetFloat("_Exposure",1);material.SetFloat("_Rotation",0);EditorUtility.SetDirty(material);return material;
        }
        static void Lighting()
        {
            var key=Node("Cold_Breach_Key",environment);key.transform.rotation=Quaternion.Euler(48,-35,0);var l=key.AddComponent<Light>();l.type=LightType.Directional;l.color=new Color(.84f,.89f,1);l.intensity=1.35f;l.shadows=LightShadows.Soft;l.shadowStrength=.24f;l.shadowBias=.035f;
            var spot=Node("Breach_Lightshaft_Source",environment);spot.transform.position=new Vector3(12,16,7);spot.transform.LookAt(new Vector3(2,0,0));l=spot.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.70f,.84f,1);l.intensity=75;l.spotAngle=52;l.innerSpotAngle=25;l.range=38;l.shadows=LightShadows.None;
            var fill=Node("Soft_Gate_Bounce",environment);fill.transform.position=new Vector3(0,7,-9);fill.transform.LookAt(new Vector3(0,4,15));l=fill.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.71f,.77f,.88f);l.intensity=30;l.range=34;l.spotAngle=105;l.shadows=LightShadows.None;
            var bounce=Node("Soft_Interior_Bounce",environment);bounce.transform.position=new Vector3(0,9,0);var bounceLight=bounce.AddComponent<Light>();bounceLight.type=LightType.Point;bounceLight.color=new Color(.87f,.83f,.75f);bounceLight.intensity=8;bounceLight.range=30;bounceLight.shadows=LightShadows.None;
            var profile=new VolumeProfile();profile.name="SealedDuelArena_Grading";var bloom=profile.Add<Bloom>();bloom.intensity.Override(.12f);bloom.threshold.Override(1.4f);
            var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);var adjust=profile.Add<ColorAdjustments>();adjust.postExposure.Override(.15f);adjust.contrast.Override(8);adjust.saturation.Override(-8);
            string path=Root+"/Materials/ArenaGrading.asset";var old=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(old){foreach(var c in old.components.ToArray())Object.DestroyImmediate(c,true);old.components.Clear();foreach(var c in profile.components){old.components.Add(c);AssetDatabase.AddObjectToAsset(c,old);}Object.DestroyImmediate(profile);EditorUtility.SetDirty(old);profile=old;}
            else{AssetDatabase.CreateAsset(profile,path);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);}
            var vol=Node("Battle_Colour_Grading",environment).AddComponent<Volume>();vol.isGlobal=true;vol.priority=25;vol.sharedProfile=profile;
            // A soft authored reflection keeps aged metal legible in standalone and batch captures alike.
            // It has no runtime capture cost and is confined to this chamber's probe bounds.
            string cubePath=Root+"/Materials/ArenaSoftReflection.asset";var cube=new Cubemap(64,TextureFormat.RGBAHalf,true);cube.name="Arena soft stone and lamp reflection";
            for(int face=0;face<6;face++){
                var pixels=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                    float u=(x+.5f)/32-1,v=(y+.5f)/32-1;Vector3 d;
                    switch((CubemapFace)face){case CubemapFace.PositiveX:d=new Vector3(1,-v,-u);break;case CubemapFace.NegativeX:d=new Vector3(-1,-v,u);break;case CubemapFace.PositiveY:d=new Vector3(u,1,v);break;case CubemapFace.NegativeY:d=new Vector3(u,-1,-v);break;case CubemapFace.PositiveZ:d=new Vector3(u,-v,1);break;default:d=new Vector3(-u,-v,-1);break;}
                    d.Normalize();Color c=Color.Lerp(new Color(.22f,.20f,.17f),new Color(.29f,.34f,.41f),d.y*.5f+.5f);
                    float light=Mathf.Pow(Mathf.Max(0,Mathf.Cos(Mathf.Atan2(d.x,d.z)*6)),18)*Mathf.Exp(-Mathf.Pow((d.y-.06f)*5,2));c+=new Color(.30f,.17f,.065f)*light;pixels[y*64+x]=c;
                }cube.SetPixels(pixels,(CubemapFace)face);
            }cube.Apply(true,false);var oldCube=AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
            if(oldCube){EditorUtility.CopySerialized(cube,oldCube);Object.DestroyImmediate(cube);EditorUtility.SetDirty(oldCube);cube=oldCube;}else AssetDatabase.CreateAsset(cube,cubePath);
            var probe=Node("Local_Stone_Bronze_Reflection",environment).AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,4,0);probe.size=new Vector3(36,25,36);probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cube;probe.intensity=.85f;probe.boxProjection=true;

        }
        [MenuItem("IM Monster/Sealed Arena/Build approved Boss arena")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building arena.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save unsaved scenes before building.");
            foreach(string dir in new[]{"Meshes","Materials","Textures","Prefabs"})Directory.CreateDirectory(Root+"/"+dir);Directory.CreateDirectory(SealedDuelArenaAcceptance.Output);AssetDatabase.Refresh();
            batches.Clear();batchMaterials.Clear();random=new System.Random(11042026);PrepareMaterials();
            var root=PrefabUtility.LoadPrefabContents(Original);
            try
            {
                var arena=root.GetComponent<BossArena>();
                foreach(Transform child in root.transform.Cast<Transform>().ToArray())
                    if(child!=arena.spawn && child!=arena.boss && child!=arena.effectRoot && !arena.spawn.IsChildOf(child) && !arena.boss.IsChildOf(child) && !arena.effectRoot.IsChildOf(child))Object.DestroyImmediate(child.gameObject);
                root.name="SealedDuelArena";environment=Node("Sealed_Duel_Chamber_Architecture",root.transform).transform;
                Floor();Walls();Gate();Vault();Dressing();Flush();Lighting();Lightshafts();
                if(!root.GetComponent<SealedArenaAtmosphere>())root.AddComponent<SealedArenaAtmosphere>();
                root.GetComponent<SealedArenaAtmosphere>().arenaSky=BuildStarSky();
                PrefabUtility.SaveAsPrefabAsset(root,SealedDuelArenaAcceptance.ArenaPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Library);var encounter=Object.FindFirstObjectByType<BossEncounter>();if(!encounter)throw new Exception("Existing Boss encounter not found");
            encounter.arenaPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(SealedDuelArenaAcceptance.ArenaPath).GetComponent<BossArena>();EditorUtility.SetDirty(encounter);EditorSceneManager.MarkSceneDirty(encounter.gameObject.scene);EditorSceneManager.SaveScene(encounter.gameObject.scene);
            encounter.gameObject.AddComponent<SealedArenaShowcaseEntry>();encounter.gameObject.AddComponent<ArcaneArchive.Visuals.Acceptance.SealedArenaStandaloneProbe>();EditorSceneManager.MarkSceneDirty(encounter.gameObject.scene);EditorSceneManager.SaveScene(encounter.gameObject.scene,SealedDuelArenaAcceptance.ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("SEALED_ARENA_BUILD_COMPLETE "+SealedDuelArenaAcceptance.ArenaPath);
        }
        public static void BuildAndCheck(){Build();SealedDuelArenaAcceptance.RunStatic();}
        public static void BuildMacAndCheck(){Build();SealedDuelArenaAcceptance.RunStatic();BuildMac();}
        public static void BuildMac()
        {
            string output=Path.GetFullPath("Builds/SealedDuelArena/IM MONSTER - Sealed Arena.app");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{SealedDuelArenaAcceptance.ScenePath},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.CompressWithLz4HC});
            File.WriteAllText(SealedDuelArenaAcceptance.Output+"/MacBuild.txt",result.summary.result+" errors="+result.summary.totalErrors+" bytes="+result.summary.totalSize);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Mac arena build failed: "+result.summary.result);
        }
    }
}
