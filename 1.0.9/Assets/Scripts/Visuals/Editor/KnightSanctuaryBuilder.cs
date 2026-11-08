using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ArcaneArchive.Core;
using ArcaneArchive.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
using G=ArcaneArchive.Visuals.Editor.KnightSanctuaryGeometry;

namespace ArcaneArchive.Visuals.Editor
{
    public static partial class KnightSanctuaryBuilder
    {
        public const string Root="Assets/Art/Bosses/Knight/Sanctuary20261107";
        public const string Output="Docs/KnightSanctuary20261107";
        public const string Scene="Assets/Scenes/BossTests/Knight_Sanctuary_Test.unity";
        public const string ArtScene="Assets/Scenes/BossTests/Knight_Sanctuary_Art.unity";
        public const string Config=Root+"/SanctuaryKnight.asset";
        public const string Arena=Root+"/SanctuaryArena.prefab";
        static Material stone,stoneDark,ivory,slate,slate2,brass,steel,wood,leather,blue,paper,glass,flame,inlay,black;
        static Material Surface(string name,Color color,string texture,float metal,float smooth,bool doubleSided=false,float emission=0)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            if(doubleSided)m.SetFloat("_Cull",0);
            if(!string.IsNullOrEmpty(texture)){
                var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Albedo.png");
                var n=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Normal.png");
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_Mask.png");
                m.SetTexture("_BaseMap",t);m.SetTexture("_BumpMap",n);m.SetFloat("_BumpScale",.23f);m.EnableKeyword("_NORMALMAP");
                if(mask){m.SetTexture("_MetallicGlossMap",mask);m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);m.SetTexture("_OcclusionMap",mask);m.SetFloat("_OcclusionStrength",.3f);m.EnableKeyword("_OCCLUSIONMAP");}
            }
            if(texture=="Marble" || texture=="Slate")m.SetTextureScale("_BaseMap",new Vector2(.34f,.34f));
            if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}
            return G.Save(m,Root+"/Materials/"+name+".mat");
        }
        static void Materials()
        {
            foreach(string path in Directory.GetFiles(Root+"/Textures","*.png")){
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)continue;
                importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.maxTextureSize=2048;
                if(path.Contains("_Normal"))importer.textureType=TextureImporterType.NormalMap;
                if(path.Contains("_Mask")){importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;}
                importer.SaveAndReimport();
            }
            stone=Surface("01_Warm_Limestone",Color.white,"Limestone",0,.28f);
            stoneDark=Surface("02_Recess_Stone",new Color(.58f,.61f,.65f),"Limestone",0,.22f);
            ivory=Surface("03_Carved_Ivory",Color.white,"Marble",0,.42f);
            slate=Surface("04_Blue_Slate",Color.white,"Slate",0,.32f);
            slate2=Surface("05_Slate_Variation",new Color(.86f,.9f,.97f),"Slate",0,.3f);
            brass=Surface("06_Aged_Brass",Color.white,"Brass",.78f,.43f);
            steel=Surface("07_Sealed_Gunmetal",Color.white,"Steel",.65f,.3f);
            wood=Surface("08_Archive_Walnut",Color.white,"Wood",0,.28f);
            leather=Surface("09_Archive_Leather",Color.white,"Leather",0,.22f);
            blue=Surface("10_Blue_Sealing_Cord",new Color(.1f,.23f,.38f),null,0,.25f);
            paper=Surface("11_Inventory_Parchment",new Color(.68f,.56f,.37f),"Limestone",0,.15f,true);
            glass=Surface("12_Cold_Window_Glass",new Color(.52f,.60f,.63f),null,.18f,.48f,true,.24f);
            flame=Surface("13_Maintenance_Lantern",new Color(1,.69f,.36f),null,0,.35f,false,1.8f);
            inlay=Surface("14_Quiet_Brass_Inlay",new Color(.45f,.31f,.15f),null,.65f,.4f);
            black=Surface("15_Dark_Recess",new Color(.045f,.052f,.065f),null,0,.2f);
        }
        static void Box(string n,Transform t,float x,float y,float z,float sx,float sy,float sz,Material m,float b=.035f)
        {G.Box(n,t,new Vector3(x,y,z),new Vector3(sx,sy,sz),m,b);}
        static void Shaft(Transform t,float x,float z,float height,float radius,bool ornate=true)
        {
            G.Lathe("Carved clustered shaft",t,new Vector3(x,0,z),new[]{new Vector2(radius*1.65f,0),new Vector2(radius*1.65f,.13f),new Vector2(radius*1.42f,.18f),new Vector2(radius*1.42f,.34f),new Vector2(radius,.48f),new Vector2(radius,height-.55f),new Vector2(radius*1.4f,height-.4f),new Vector2(radius*1.65f,height-.18f),new Vector2(radius*1.65f,height)},ivory);
            G.Ring("Brass collar",t,new Vector3(x,height-.53f,z),radius*1.06f,.025f,brass);
            if(ornate)for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var pts=new[]{new Vector3(x+Mathf.Sin(a)*radius*1.12f,height-.45f,z+Mathf.Cos(a)*radius*1.12f),new Vector3(x+Mathf.Sin(a)*radius*1.75f,height-.18f,z+Mathf.Cos(a)*radius*1.75f),new Vector3(x+Mathf.Sin(a)*radius*1.1f,height-.1f,z+Mathf.Cos(a)*radius*1.1f)};G.Tube("Capital acanthus leaf",t,pts,.035f,ivory);}
        }
        static void Frame(Transform t,float width,float bottom,float top,float z,Material mat,float thick=.055f)
        {
            Box("Panel rail",t,0,bottom,z,width,thick,.06f,mat,.01f);Box("Panel rail",t,0,top,z,width,thick,.06f,mat,.01f);
            Box("Panel stile",t,-width/2,(bottom+top)/2,z,thick,top-bottom,.06f,mat,.01f);Box("Panel stile",t,width/2,(bottom+top)/2,z,thick,top-bottom,.06f,mat,.01f);
        }
        static void Lantern(Transform t,Vector3 p)
        {
            var l=G.Node("Brass maintenance lantern",t,p).transform;
            Box("Lamp bracket",l,0,.28f,.24f,.08f,.65f,.1f,brass);
            Box("Glass chamber",l,0,0,0,.27f,.46f,.24f,flame);
            Box("Lantern roof",l,0,.27f,0,.44f,.1f,.4f,brass);Box("Lantern foot",l,0,-.27f,0,.36f,.08f,.32f,brass);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.15f,.15f})Box("Lantern cage",l,x,0,z,.025f,.56f,.025f,brass,.006f);
            G.Ring("Lantern suspension ring",l,new Vector3(0,.4f,0),.09f,.018f,brass,true);
        }
        static void Voussoirs(Transform t,float width,float spring,float rise,float z,float thickness)
        {
            var path=G.ArchPath(width,spring,rise,z,40);
            for(int i=0;i<path.Length-1;i++){
                var d=path[i+1]-path[i];
                var block=G.Box("Individually dressed arch stone",t,(path[i]+path[i+1])*.5f,
                    new Vector3(d.magnitude*.96f,thickness,.30f),ivory,.025f);
                block.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            }
        }
        static GameObject Bay(bool window,bool evacuation=false,int pierMask=0)
        {
            var g=new GameObject(evacuation?"Evacuation recess module":window?"Window arcade module":"Archive service arcade module");var t=g.transform;
            Box("Thick masonry bay",t,0,4.8f,.48f,3.8f,9.6f,.8f,stone,.09f);
            Box("Recess backing",t,0,3.4f,-.015f,2.95f,5.7f,.12f,stoneDark);
            Box("Stone plinth",t,0,.27f,-.05f,3.9f,.54f,.9f,ivory,.06f);
            if(!window)Box("Lower cornice",t,0,6.5f,-.11f,3.9f,.19f,.8f,ivory);
            Box("Upper cornice",t,0,9.4f,.12f,3.95f,.2f,1.04f,ivory);
            for(int side=-1;side<=1;side+=2){
                if((pierMask & (side<0?1:2))!=0)continue;
                Shaft(t,side*1.86f,-.28f,9.45f,.23f);
                Shaft(t,side*1.64f,-.5f,6.58f,.105f);
                Shaft(t,side*1.95f,-.5f,6.58f,.105f);
            }
            Voussoirs(t,1.70f,window?6.3f:4.4f,window?2.35f:2.05f,-.34f,.30f);
            G.Arch("Principal pointed arch",t,1.65f,window?6.3f:4.4f,window?2.3f:2f,.09f,-.53f,ivory);
            G.Arch("Recessed brass archivolt",t,1.46f,window?6.28f:4.38f,window?2.12f:1.82f,.038f,-.49f,brass);
            G.Arch("Inner carved archivolt",t,1.32f,window?6.25f:4.32f,window?2.05f:1.74f,.065f,-.44f,ivory);
            for(int course=1;course<10;course++){
                float y=course*.94f;
                Box("Masonry horizontal mortar",t,0,y,.067f,3.72f,.009f,.006f,stoneDark,.001f);
                for(int j=-1;j<=1;j++)Box("Staggered stone joint",t,j*1.25f+(course%2)*.61f,y-.45f,.067f,.009f,.88f,.006f,stoneDark,.001f);
            }
            if(window){
                // The reference has tall ground-level lancets between full-height stone shafts.
                var cap=G.ArchPath(1.28f,6.25f,2.0f,-.18f).ToList();cap.Add(new Vector3(1.28f,.7f,-.18f));cap.Add(new Vector3(-1.28f,.7f,-.18f));G.Fan("Tall pointed opal glazing",t,cap.ToArray(),glass);
                foreach(float x in new[]{-1.3f,1.3f})Box("Carved tall window jamb",t,x,3.45f,-.28f,.12f,5.5f,.23f,ivory);
                for(int j=-2;j<=2;j++)Box("Tall window mullion",t,j*.43f,3.65f,-.25f,j%2==0?.052f:.028f,5.9f,.07f,j%2==0?ivory:brass,.008f);
                for(int j=0;j<10;j++)G.Tube("Window lead crossbar",t,new[]{new Vector3(-1.23f,1.0f+j*.52f,-.27f),new Vector3(1.23f,1.0f+j*.52f,-.27f)},.015f,brass,6);
                Quatrefoil(t,new Vector3(0,7.12f,-.34f),.47f);
                G.Ring("Window tracery rose",t,new Vector3(0,7.12f,-.32f),.66f,.055f,ivory,true);
            }else{
                Box("Shelf recess",t,0,2.15f,-.14f,2.1f,2.7f,.15f,black);
                for(int level=0;level<(evacuation?0:3);level++){
                    float y=.85f+level*.72f;Box("Walnut maintenance shelf",t,0,y,-.42f,2.05f,.09f,.55f,wood);
                    for(int n=0;n<6;n++){float x=-.8f+n*.29f;Box("Recorded service volume",t,x,y+.28f,-.46f,.18f,.44f+(n%2)*.1f,.27f,n%3==0?leather:wood,.015f);Box("Book brass label",t,x,y+.23f,-.607f,.11f,.06f,.012f,brass,.003f);}
                }
                Lantern(t,new Vector3(1.12f,3.1f,-.5f));
            }
            for(int k=0;k<2;k++){
                float y=.65f+k*.18f;G.Tube("Outer supply conduit",t,new[]{new Vector3(-1.9f,y,-.49f),new Vector3(1.9f,y,-.49f)},.045f,brass,10);
                foreach(float x in new[]{-1.2f,0,1.2f})Box("Conduit clamp",t,x,y,-.53f,.08f,.16f,.13f,steel,.012f);
            }
            if(!window){
                G.Ring("Upper wall authority seal",t,new Vector3(0,7.85f,-.17f),.56f,.042f,brass,true,20,285);
                Box("Seal hanging stem",t,0,8.65f,-.17f,.035f,.6f,.04f,brass,.006f);
            }
            G.Combine(t,(evacuation?"Arcade_Evacuation":window?"Arcade_Window":"Arcade_Service")+"_P"+pierMask);
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Modules/"+(evacuation?"EvacuationBay":window?"WindowBay":"ServiceBay")+"_P"+pierMask+".prefab");Object.DestroyImmediate(g);return prefab;
        }
        static void Vault(Transform parent)
        {
            var t=G.Node("Ribbed stone vault",parent).transform;
            const int seg=96, rings=14;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int j=0;j<=rings;j++){float u=j/(float)rings;float r=14.9f*(1-u),y=9.5f+5.2f*Mathf.Sin(u*Mathf.PI*.5f);
                for(int i=0;i<=seg;i++){float a=i*2*Mathf.PI/seg;v.Add(new Vector3(r*Mathf.Sin(a),y,r*Mathf.Cos(a)));uv.Add(new Vector2(a*3,u*6));}}
            for(int j=0;j<rings;j++)for(int i=0;i<seg;i++){int a=j*(seg+1)+i,b=(j+1)*(seg+1)+i;tris.AddRange(new[]{a,b,a+1,b,b+1,a+1});}
            var mesh=new Mesh{name="Continuous vault panels"};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();
            G.Place("Continuous vault panels",t,Vector3.zero,mesh,stone);
            for(int i=0;i<24;i++){
                float a=(i*15+7.5f)*Mathf.Deg2Rad;var pts=new Vector3[41];
                for(int j=0;j<pts.Length;j++){float u=j/40f,r=14.3f*(1-u);pts[j]=new Vector3(r*Mathf.Sin(a),9.35f+5.15f*Mathf.Sin(u*Mathf.PI*.5f),r*Mathf.Cos(a));}
                G.Tube("Structural vault rib",t,pts,.105f,ivory,10);
                if(i%2==0){var pts2=pts.Select(pt=>pt-Vector3.up*.14f).ToArray();G.Tube("Brass rib fillet",t,pts2,.019f,brass);}
            }
            G.Ring("Vault crown moulding",t,new Vector3(0,14.43f,0),.9f,.11f,ivory);
            G.Ring("Vault crown brass",t,new Vector3(0,14.32f,0),.7f,.034f,brass);
            for(int bay=0;bay<12;bay++){
                float a0=(bay*30+7.5f)*Mathf.Deg2Rad;
                foreach(int side in new[]{-1,1}){
                    var curve=new Vector3[33];
                    for(int j=0;j<curve.Length;j++){
                        float u=j/32f,rad=14.22f*(1-u*.7f),angle=a0+side*(15-30*u)*Mathf.Deg2Rad;
                        float h=9.35f+5.15f*Mathf.Sin(u*.7f*Mathf.PI*.5f);
                        curve[j]=new Vector3(Mathf.Sin(angle)*rad,h-.075f,Mathf.Cos(angle)*rad);
                    }
                    G.Tube("Intersecting tierceron vault rib",t,curve,.095f,ivory,10);
                }
            }
            G.Ring("Tierceron bearing ring",t,new Vector3(0,9.35f+5.15f*Mathf.Sin(.7f*Mathf.PI*.5f)-.075f,0),14.22f*.3f,.12f,ivory);
            foreach(float u in new[]{.22f,.45f,.7f}){
                float radius=14.9f*(1-u),height=9.5f+5.2f*Mathf.Sin(u*Mathf.PI*.5f)-.05f;
                G.Ring("Vault stone course",t,new Vector3(0,height,0),radius,.025f,ivory);
            }
            G.Combine(t,"Vault");
        }
        static void Gate(Transform parent,bool south=false)
        {
            var t=G.Node(south?"South entrance threshold":"North sealing gate",parent,new Vector3(0,0,south?-14.1f:14.1f)).transform;
            if(south)t.localRotation=Quaternion.Euler(0,180,0);
            Box("Gate bearing masonry",t,0,5,.65f,10.4f,10,1.3f,stone,.1f);
            foreach(int side in new[]{-1,1}){
                Box("Gate arcade junction buttress",t,side*5.45f,4.8f,-.30f,1.05f,9.6f,1.95f,stone,.08f);
                Box("Junction buttress cornice",t,side*5.45f,9.45f,-.30f,1.20f,.22f,2.05f,ivory,.05f);
            }
            Box("Iron double seal",t,0,4.15f,-.07f,6.4f,8.3f,.4f,steel,.08f);
            for(int i=0;i<3;i++){
                float w=3.5f+i*.43f,z=-.4f+i*.13f;
                Voussoirs(t,w,6.65f,3.7f+i*.16f,z+.05f,.33f);
                G.Arch("Layered load-bearing archivolt",t,w,6.65f,3.7f+i*.16f,.09f,z-.16f,ivory);
                G.Arch("Archivolt gold fillet",t,w-.1f,6.65f,3.64f+i*.16f,.025f,z-.16f,brass);
                Shaft(t,-w,z,6.65f,.18f);Shaft(t,w,z,6.65f,.18f);
            }
            // The arched upper door is real fill, not an opening showing sky.
            var upper=G.ArchPath(3.25f,6.6f,3.5f,-.29f).ToList();upper.Add(new Vector3(3.25f,4,-.29f));upper.Add(new Vector3(-3.25f,4,-.29f));G.Fan("Arched door crown",t,upper.ToArray(),steel);
            foreach(int s in new[]{-1,1}){
                var leaf=G.Node("Thick recessed door leaf",t,new Vector3(s*1.65f,0,-.32f)).transform;
                Frame(leaf,2.95f,.28f,6.6f,-.08f,brass,.10f);
                for(int panel=0;panel<3;panel++)Frame(leaf,2.54f,.62f+panel*1.93f,2.23f+panel*1.93f,-.10f,brass,.032f);
                foreach(float x in new[]{-1.42f,1.42f})for(int j=0;j<18;j++)Box("Door rivet",leaf,x,.55f+j*.34f,-.16f,.045f,.045f,.05f,brass,.015f);
                Box("Forged gate crossbar",leaf,0,3.72f,-.25f,3.16f,.20f,.19f,brass,.045f);
                Box("Gate lock socket",leaf,s*.92f,3.72f,-.36f,.32f,.48f,.18f,steel);
            }
            G.FlatArc("Large authority ring",t,new Vector3(0,4.65f,-.57f),2.1f,.07f,.018f,brass,16,148);
            G.FlatArc("Large authority ring",t,new Vector3(0,4.65f,-.57f),2.1f,.07f,.018f,brass,196,148);
            G.FlatArc("Inner authority ring",t,new Vector3(0,4.65f,-.58f),1.74f,.025f,.014f,brass,18,144);
            G.FlatArc("Inner authority ring",t,new Vector3(0,4.65f,-.58f),1.74f,.025f,.014f,brass,198,144);
            Box("Cold observation seam",t,0,6.7f,-.56f,.055f,4.4f,.025f,glass,.005f);
            Box("Upper seam closure",t,0,4.5f,-.58f,.08f,.9f,.05f,brass);
            // Stress cracks, with repair stitches connecting credible load-bearing regions.
            foreach(int side in new[]{-1,1}){
                var pts=new[]{new Vector3(side*4.55f,2.1f,-.72f),new Vector3(side*4.38f,3.1f,-.72f),new Vector3(side*4.59f,4.0f,-.72f),new Vector3(side*4.39f,4.55f,-.72f),new Vector3(side*4.55f,5.2f,-.72f)};
                G.Tube("Old pressure fracture",t,pts,.012f,black,5);
                for(int j=0;j<3;j++){var clamp=G.Box("Riveted repair stitch",t,new Vector3(side*4.5f,2.8f+j*.82f,-.77f),new Vector3(.43f,.07f,.06f),brass,.015f);clamp.transform.localRotation=Quaternion.Euler(0,0,side*18);}
                Lantern(t,new Vector3(side*4.78f,2.1f,-.65f));
            }
            // Compensate the monumental portal scaling so its authority emblem stays circular.
            foreach(var f in t.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Large authority ring"||f.name=="Inner authority ring")){
                var mesh=f.sharedMesh;mesh.vertices=mesh.vertices.Select(v=>new Vector3(v.x,4.65f+(v.y-4.65f)*1.65f/1.55f,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            }
            G.Combine(t,south?"SouthGate":"NorthGate");
        }
        static void Floor(Transform parent)
        {
            var t=G.Node("Flush slate floor and brass inlays",parent).transform;
            // A circle clipped tile mosaic keeps the original flat collider and smooth walking surface.
            for(int row=-8;row<=8;row++)for(int col=-8;col<=8;col++){
                float z=row*1.7f,x=col*1.7f+(row%2)*.85f;
                if(new Vector2(x,z).magnitude>15.3f)continue;
                var poly=new List<Vector2>{new Vector2(x-.843f,z-.843f),new Vector2(x-.843f,z+.843f),new Vector2(x+.843f,z+.843f),new Vector2(x+.843f,z-.843f)};
                for(int edge=0;edge<96 && poly.Count>0;edge++){
                    float a=edge*2*Mathf.PI/96;var n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var next=new List<Vector2>();
                    for(int i=0;i<poly.Count;i++){var p=poly[i];var q=poly[(i+1)%poly.Count];float dp=Vector2.Dot(p,n)-14.08f,dq=Vector2.Dot(q,n)-14.08f;if(dp<=0)next.Add(p);if((dp<=0)!=(dq<=0))next.Add(Vector2.Lerp(p,q,dp/(dp-dq)));}
                    poly=next;
                }
                if(poly.Count<3)continue;
                var v=poly.Select(q=>new Vector3(q.x,.006f,q.y)).ToList();var tri=new List<int>();for(int i=1;i<v.Count-1;i++)tri.AddRange(new[]{0,i,i+1});
                var m=new Mesh{name="Slate slab"};m.SetVertices(v);m.SetTriangles(tri,0);m.SetUVs(0,poly.Select(q=>q*.65f).ToList());m.RecalculateNormals();m.RecalculateTangents();
                G.Place("Slate slab",t,Vector3.zero,m,(row+col)%3==0?slate2:slate);
            }
            foreach(float r in new[]{3.15f,3.25f,8.8f,12.9f,13.02f})G.Ring("Flush ceremonial brass",t,new Vector3(0,.008f,0),r,.012f,inlay);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));G.Tube("Radial stone joint inlay",t,new[]{dir*8.8f+Vector3.up*.008f,dir*12.9f+Vector3.up*.008f},.008f,inlay,6);}
            for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var side=new Vector3(dir.z,0,-dir.x);G.Tube("Quiet compass engraving",t,new[]{dir*.7f+Vector3.up*.01f,dir*2.65f+side*.18f+Vector3.up*.01f,dir*3.1f+Vector3.up*.01f,dir*2.65f-side*.18f+Vector3.up*.01f,dir*.7f+Vector3.up*.01f},.012f,inlay,6);}
            G.Combine(t,"Floor");
        }
        static void Case(Transform t,Vector3 p,float yaw)
        {
            var c=G.Node("Blue-cord legacy transfer case",t,p).transform;c.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Walnut case",c,0,.4f,0,1.25f,.8f,.7f,wood,.07f);
            var collision=c.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,.48f,0);collision.size=new Vector3(1.25f,.96f,.72f);
            Box("Leather padded lid",c,0,.85f,0,1.26f,.18f,.72f,leather,.07f);
            foreach(float x in new[]{-.48f,.48f}){
                Box("Case brass binding",c,x,.43f,-.365f,.07f,.84f,.035f,brass,.012f);Box("Case top strap",c,x,.95f,0,.075f,.02f,.72f,brass,.008f);
                for(int k=0;k<5;k++)Box("Strap rivet",c,x,.12f+k*.16f,-.395f,.025f,.025f,.02f,steel,.005f);
            }
            G.Ring("Case seal",c,new Vector3(0,.45f,-.39f),.11f,.021f,brass,true,25,290);
            G.Tube("Blue cord wrapping",c,new[]{new Vector3(.18f,0,-.37f),new Vector3(.18f,.95f,-.37f),new Vector3(.18f,.96f,.37f),new Vector3(.18f,0,.37f)},.018f,blue);
            G.Tube("Blue tied bow",c,new[]{new Vector3(.18f,.65f,-.4f),new Vector3(.06f,.72f,-.42f),new Vector3(.04f,.58f,-.42f),new Vector3(.18f,.65f,-.4f),new Vector3(.3f,.73f,-.42f),new Vector3(.32f,.58f,-.42f),new Vector3(.18f,.65f,-.4f),new Vector3(.26f,.31f,-.43f)},.016f,blue);
            Box("Inventory label",c,-.23f,.52f,-.388f,.19f,.13f,.015f,paper,.008f);
        }
        static void Props(Transform parent)
        {
            var t=G.Node("Evacuation alcove",parent,new Vector3(-9.86f,0,-9.86f)).transform;t.localRotation=Quaternion.Euler(0,225,0);
            Case(t,new Vector3(-.35f,0,0),0);Case(t,new Vector3(-.3f,.98f,.02f),-3);Case(t,new Vector3(1.25f,0,0),8);
            var desk=G.Node("Maintenance bench",t,new Vector3(-.25f,0,-.65f)).transform;
            Box("Bench top",desk,0,.81f,0,1.5f,.12f,.55f,wood);
            var collision=desk.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,.44f,0);collision.size=new Vector3(1.5f,.88f,.55f);
            // Linen hangs over the front edge; geometry supplies folds rather than a flat colour patch.
            var cloth=Surface("16_Maintenance_Linen",new Color(.92f,.88f,.76f),"Limestone",0,.12f,true);
            var cv=new List<Vector3>();var ct=new List<int>();var cuv=new List<Vector2>();
            for(int row=0;row<=12;row++)for(int col=0;col<=12;col++){
                float x=-.12f+col/12f*.54f,u=row/12f;
                float z=u<.48f?.24f-u*1.15625f:-.315f-.02f*Mathf.Sin((u-.48f)*3);
                float y=u<.48f?.882f:.882f-(u-.48f)*1.38f;
                y+=.008f*Mathf.Sin(col*1.35f);z+=u>.48f?.012f*Mathf.Sin(col*1.35f):0;
                cv.Add(new Vector3(x,y,z));cuv.Add(new Vector2(col/12f,u));
            }
            for(int row=0;row<12;row++)for(int col=0;col<12;col++){int a=row*13+col;ct.AddRange(new[]{a,a+1,a+13,a+1,a+14,a+13});}
            var clothMesh=new Mesh{name="Folded maintenance linen"};clothMesh.SetVertices(cv);clothMesh.SetTriangles(ct,0);clothMesh.SetUVs(0,cuv);clothMesh.RecalculateNormals();clothMesh.RecalculateTangents();
            G.Place("Folded maintenance linen",desk,Vector3.zero,clothMesh,cloth);
            foreach(float x in new[]{-.62f,.62f})foreach(float z in new[]{-.19f,.19f})Box("Bench leg",desk,x,.4f,z,.09f,.8f,.09f,wood);
            for(int i=0;i<3;i++){var tool=G.Node("Repair tool",desk,new Vector3(-.45f+i*.35f,.91f,0)).transform;tool.localRotation=Quaternion.Euler(0,i*23,0);Box("Handle",tool,0,0,0,.045f,.04f,.31f,wood,.01f);Box("Forged head",tool,0,.015f,.15f,.16f,.055f,.07f,steel,.015f);}
            var stand=G.Node("Evacuation inventory stand",t,new Vector3(1.1f,0,-.65f)).transform;
            var standCollider=stand.gameObject.AddComponent<BoxCollider>();standCollider.center=new Vector3(0,.85f,0);standCollider.size=new Vector3(.62f,1.7f,.24f);
            Box("Stand base",stand,0,.08f,0,.5f,.16f,.45f,wood);Box("Stand post",stand,0,.65f,0,.08f,1.15f,.08f,wood);
            var board=G.Box("Inventory board",stand,new Vector3(0,1.3f,0),new Vector3(.62f,.8f,.045f),wood);board.transform.localRotation=Quaternion.Euler(18,0,0);
            Box("Pinned parchment",board.transform,0,0,-.031f,.52f,.68f,.012f,paper,.005f);
            for(int i=0;i<10;i++)Box("Inventory ruled line",board.transform,.02f,.25f-i*.051f,-.041f,.38f,.006f,.004f,inlay,.001f);
            for(int i=0;i<7;i++)Box("Recorded transfer tick",board.transform,-.21f,.25f-i*.051f,-.041f,.015f,.012f,.004f,blue,.001f);
            // Vacant cradle refers to an evacuated case without creating a new quest object.
            Box("Empty transfer cradle",t,2.2f,.12f,-.5f,.8f,.24f,.7f,wood);
            foreach(float x in new[]{1.86f,2.54f})Box("Cradle restraint",t,x,.32f,-.5f,.09f,.4f,.7f,wood);
            G.Combine(t,"EvacuationProps");
        }
        static Light Light(string name,Transform root,Vector3 position,Color color,float intensity,float range,LightType type,Vector3 target,bool shadows=false)
        {
            var g=G.Node(name,root,position);var l=g.AddComponent<Light>();l.type=type;l.color=color;l.intensity=intensity;l.range=range;
            l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.35f;l.shadowNormalBias=.65f;l.spotAngle=95;
            if(type!=LightType.Point)g.transform.LookAt(root.TransformPoint(target));
            return l;
        }
        static void Lighting(Transform root)
        {
            var t=G.Node("Sanctuary lighting",root).transform;
            Light("Soft clerestory daylight",t,new Vector3(-8,14,-7),new Color(.94f,.95f,1),480f,45,LightType.Spot,new Vector3(2,0,5),true);
            Light("Warm reflected stone bounce",t,new Vector3(9,6,-4),new Color(1,.81f,.61f),68,34,LightType.Point,Vector3.zero);
            Light("Cool gate threshold",t,new Vector3(0,8,12.5f),new Color(.62f,.77f,1),16,19,LightType.Point,Vector3.zero);
            Light("Ceiling diffuse fill",t,new Vector3(0,17,0),new Color(1,.91f,.76f),115,32,LightType.Point,Vector3.zero);
            foreach(float a in new[]{45f,105f,225f,285f}){
                var pos=new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*13.1f,3,Mathf.Cos(a*Mathf.Deg2Rad)*13.1f);
                Light("Warm maintenance alcove",t,pos,new Color(1,.72f,.46f),4,6,LightType.Point,Vector3.zero);
            }
            var probe=G.Node("Sanctuary reflection capture",root,new Vector3(0,4,0)).AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution=128;probe.size=new Vector3(34,26,34);probe.center=new Vector3(0,7,0);probe.boxProjection=true;probe.intensity=.8f;probe.cullingMask=1;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.22f,.24f,.28f);
            var presentation=root.gameObject.AddComponent<KnightSanctuaryLighting>();
            presentation.gateLight=t.Find("Cool gate threshold").GetComponent<Light>();
            var source=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Art/MilanLibraryV6/Settings/MilanV6_URP.asset");
            var pipe=Object.Instantiate(source);pipe.name="Sanctuary URP";
            pipe.shadowDistance=50;pipe.msaaSampleCount=2;
            var rendererSource=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Art/MilanLibraryV6/Settings/MilanV6_Renderer.asset");
            var renderer=G.Save(Object.Instantiate(rendererSource),Root+"/SanctuaryRenderer.asset");
            var ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Sanctuary contact shadows";
            var aoSettings=new SerializedObject(ao);
            aoSettings.FindProperty("m_Settings.Intensity").floatValue=1.6f;
            aoSettings.FindProperty("m_Settings.Radius").floatValue=.5f;
            aoSettings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.35f;
            aoSettings.FindProperty("m_Settings.Downsample").boolValue=false;
            aoSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);EditorUtility.SetDirty(renderer);
            var pipeSettings=new SerializedObject(pipe);
            pipeSettings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            pipeSettings.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
            pipeSettings.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            pipeSettings.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;
            pipeSettings.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=8;
            pipeSettings.ApplyModifiedPropertiesWithoutUndo();
            presentation.pipeline=G.Save(pipe,Root+"/SanctuaryURP.asset");
            var volume=G.Node("Sanctuary tone and bloom",root).AddComponent<Volume>();volume.isGlobal=true;volume.priority=35;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var ca=profile.Add<ColorAdjustments>();ca.postExposure.Override(.55f);ca.contrast.Override(4);ca.saturation.Override(-4);
            var bloom=profile.Add<Bloom>();bloom.intensity.Override(.09f);bloom.threshold.Override(1.2f);
            var savedProfile=G.Save(profile,Root+"/SanctuaryVolume.asset");
            foreach(var component in savedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,savedProfile);
            volume.sharedProfile=savedProfile;
        }
        [MenuItem("Arcane Archive/Knight/Open sanctuary art scene")]
        public static void OpenArtScene(){EditorSceneManager.OpenScene(ArtScene);}
        [MenuItem("Arcane Archive/Knight/Open sanctuary battle scene")]
        public static void OpenBattleScene(){EditorSceneManager.OpenScene(Scene);}
        static void BuildArtScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var arena=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Arena));
            arena.name="Last Seal Sanctuary — edit prefab to update battle";
            var camera=new GameObject("Sanctuary preview camera").AddComponent<Camera>();
            camera.tag="MainCamera";camera.transform.position=new Vector3(0,2.05f,-11.8f);
            camera.transform.LookAt(new Vector3(0,7.8f,11));camera.fieldOfView=75;
            camera.nearClipPlane=.08f;camera.farClipPlane=90;camera.allowHDR=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.22f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            camera.gameObject.AddComponent<AudioListener>();
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.5f,.52f,.55f);
            RenderSettings.ambientEquatorColor=new Color(.31f,.30f,.29f);
            RenderSettings.ambientGroundColor=new Color(.23f,.22f,.21f);
            RenderSettings.ambientIntensity=1;RenderSettings.fog=false;RenderSettings.reflectionIntensity=.8f;
            EditorSceneManager.SaveScene(scene,ArtScene);
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,4,0),Quaternion.Euler(9,0,0),19);
        }
        [MenuItem("Arcane Archive/Knight/Build concept sanctuary")]
        public static void Build()
        {
            Directory.CreateDirectory(Output);
            foreach(string sub in new[]{"Materials","Meshes","Modules","Textures"})Directory.CreateDirectory(Root+"/"+sub);
            AssetDatabase.Refresh();G.Reset(Root+"/Meshes");Materials();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Bosses/Knight/ProductionV5/KnightArenaProduction.prefab");
            var arena=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));arena.name="Last Seal Sanctuary";var runtime=arena.GetComponent<BossArena>();
            foreach(var r in arena.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(runtime.boss))r.enabled=false;
            foreach(var l in arena.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l.gameObject);
            var old=arena.GetComponent<KnightArenaPresentation>();if(old)Object.DestroyImmediate(old);
            var art=G.Node("Sanctuary architecture",arena.transform).transform;
            var bays=new Dictionary<string,GameObject>();
            for(int i=0;i<24;i++){
                if(i==0||i==1||i==23||i==11||i==12||i==13)continue;
                float a=i*15*Mathf.Deg2Rad;int pierMask=0;
                var position=new Vector3(Mathf.Sin(a)*14.5f,0,Mathf.Cos(a)*14.5f);var rotation=Quaternion.Euler(0,i*15,0);
                foreach(int side in new[]{-1,1}){
                    var column=position+rotation*new Vector3(side*1.86f,0,-.28f);
                    foreach(float z in new[]{-12f,-6f,0f,6f,12f})foreach(int edge in new[]{-1,1})
                        if(Vector3.Distance(column,new Vector3(edge*NaveWidth(z),0,z)*.98f)<1.35f)pierMask|=side<0?1:2;
                }
                bool isWindow=i%3!=0,isEvacuation=i==15;string key=isWindow+"_"+isEvacuation+"_"+pierMask;
                if(!bays.TryGetValue(key,out var module)){module=Bay(isWindow,isEvacuation,pierMask);bays.Add(key,module);}
                var bay=(GameObject)PrefabUtility.InstantiatePrefab(module,art);
                bay.name="Arcade bay "+i.ToString("D2");bay.transform.localPosition=new Vector3(Mathf.Sin(a)*14.5f,0,Mathf.Cos(a)*14.5f);bay.transform.localRotation=Quaternion.Euler(0,i*15,0);
            }
            Gate(art);Gate(art,true);GrandArchitecture(art);Floor(art);Props(art);Lighting(arena.transform);
            var saved=PrefabUtility.SaveAsPrefabAsset(arena,Arena).GetComponent<BossArena>();PrefabUtility.UnloadPrefabContents(arena);
            var original=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);
            var config=Object.Instantiate(original);config.name="Sanctuary Knight";config.arena=saved;config=G.Save(config,Config);
            var scene=EditorSceneManager.OpenScene(KnightProductionBuilder.TestScene);
            var launcher=Object.FindFirstObjectByType<KnightTrialLauncher>();launcher.definition=config;
            foreach(var menu in Object.FindObjectsByType<RecoveryMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))menu.bookKnight=config;
            EditorSceneManager.SaveScene(scene,Scene);
            BuildArtScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Output+"/Build.txt","Generated sanctuary prefab, materials, modular meshes, standalone test scene. Source combat config cloned; original resources retained.\n"+DateTime.UtcNow.ToString("o"));
        }
    }
}
