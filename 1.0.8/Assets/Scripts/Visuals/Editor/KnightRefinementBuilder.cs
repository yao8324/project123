using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneArchive.Visuals.Editor
{
    // Editable original geometry. The reference video supplies proportion and motion, not meshes.
    public static class KnightRefinementBuilder
    {
        const string Root="Assets/Art/Bosses/Knight/Refined";
        static int serial;
        static Material silver,dark,gold,ivory,cloth,light,soft;
        static GameObject Node(string name,Transform parent=null,Vector3 position=default(Vector3))
        {var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
        static Material Material(string name,Color color,float metallic,float smooth,bool luminous=false)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(luminous?"Arcane Archive/Blade Light":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);if(!luminous){m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Cull",0);}
            EditorUtility.SetDirty(m);return m;
        }
        static void Quad(List<int> triangles,int a,int b,int c,int d){triangles.AddRange(new[]{a,b,c,a,c,d});}
        static GameObject MeshObject(string name,Transform parent,Vector3 position,Mesh mesh,Material mat)
        {
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var uv=mesh.vertices.Select(v=>new Vector2(v.x*.7f+.5f,v.y*.4f+.5f)).ToArray();mesh.uv=uv;
            mesh.name=name;string path=Root+"/Meshes/"+(serial++).ToString("D3")+"_"+name.Replace(" ","_")+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}
            else AssetDatabase.CreateAsset(mesh,path);
            var go=Node(name,parent,position);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }
        static GameObject Plate(string name,Transform parent,Vector3 position,Vector2[] outline,float depth,Material mat)
        {
            float area=0;for(int i=0;i<outline.Length;i++){var a=outline[i];var b=outline[(i+1)%outline.Length];area+=a.x*b.y-b.x*a.y;}
            if(area<0)outline=outline.Reverse().ToArray();int n=outline.Length;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int r=0;r<3;r++)foreach(var point in outline)vertices.Add(new Vector3(point.x*(r==2?.88f:1),point.y*(r==2?.94f:1),r==0?-depth*.5f:r==1?depth*.24f:depth*.5f));
            vertices.Add(new Vector3(0,0,-depth*.5f));vertices.Add(new Vector3(0,0,depth*.5f));
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;triangles.AddRange(new[]{n*3,j,i,n*3+1,n*2+i,n*2+j});
                Quad(triangles,i,j,n+j,n+i);Quad(triangles,n+i,n+j,n*2+j,n*2+i);
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);return MeshObject(name,parent,position,mesh,mat);
        }
        static GameObject Lathe(string name,Transform parent,Vector3 position,Vector2[] profile,Material mat,int segments=16)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            foreach(var p in profile)for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x));}
            for(int row=0;row<profile.Length-1;row++)for(int i=0;i<segments;i++){int j=(i+1)%segments;Quad(triangles,row*segments+i,(row+1)*segments+i,(row+1)*segments+j,row*segments+j);}
            int bottom=vertices.Count;vertices.Add(new Vector3(0,profile[0].y,0));int top=vertices.Count;vertices.Add(new Vector3(0,profile[profile.Length-1].y,0));
            for(int i=0;i<segments;i++){int j=(i+1)%segments;triangles.AddRange(new[]{bottom,i,j,top,(profile.Length-1)*segments+j,(profile.Length-1)*segments+i});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);return MeshObject(name,parent,position,mesh,mat);
        }
        static GameObject Ring(string name,Transform parent,Vector3 center,float radius,float tube,Material mat,int segments=64)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=segments;i++)for(int j=0;j<8;j++)
            {float a=i*Mathf.PI*2/segments,b=j*Mathf.PI/4;vertices.Add(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(b)*tube));}
            for(int i=0;i<segments;i++)for(int j=0;j<8;j++)Quad(triangles,i*8+j,(i+1)*8+j,(i+1)*8+(j+1)%8,i*8+(j+1)%8);
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);return MeshObject(name,parent,center,mesh,mat);
        }
        static Vector2[] Leaf(float width,float height)
        {return new[]{new Vector2(0,height*.6f),new Vector2(width*.5f,height*.22f),new Vector2(width*.42f,-height*.3f),new Vector2(0,-height*.5f),new Vector2(-width*.42f,-height*.3f),new Vector2(-width*.5f,height*.22f)};}
        static Transform Arm(Transform torso,float sign,out Transform forearm,out Transform hand)
        {
            var upper=Node(sign<0?"Right shoulder joint":"Left shoulder joint",torso,new Vector3(sign*.55f,1.05f,0)).transform;
            Lathe("Upper arm articulated underlay",upper,Vector3.zero,new[]{new Vector2(.14f,-.5f),new Vector2(.18f,-.15f),new Vector2(.17f,0)},dark);
            Plate("Upper arm shield plate",upper,new Vector3(0,-.24f,.15f),Leaf(.29f,.45f),.07f,silver);
            for(int i=0;i<3;i++)
            {
                var p=Plate("Layered pointed pauldron "+i,upper,new Vector3(sign*(.03f+i*.065f),.055f-i*.11f,.02f+i*.025f),Leaf(.56f-i*.04f,.4f),.18f,i==0?gold:silver);
                p.transform.localRotation=Quaternion.Euler(-12,sign*12,sign*(24+i*8));
            }
            forearm=Node("Elbow joint",upper,new Vector3(0,-.5f,0)).transform;
            Lathe("Elbow and vambrace",forearm,Vector3.zero,new[]{new Vector2(.14f,-.46f),new Vector2(.18f,-.2f),new Vector2(.19f,-.07f),new Vector2(.13f,.04f)},dark);
            Plate("Vambrace bevel shell",forearm,new Vector3(0,-.23f,.15f),Leaf(.30f,.46f),.08f,silver);
            Plate("Elbow gold fin",forearm,new Vector3(sign*.16f,0,-.01f),Leaf(.12f,.4f),.05f,gold);
            hand=Node("Wrist grip",forearm,new Vector3(0,-.48f,0)).transform;
            Plate("Gauntlet metacarpal plate",hand,new Vector3(0,-.08f,.025f),Leaf(.23f,.27f),.13f,silver);
            for(int i=0;i<4;i++)Lathe("Armored curled finger "+i,hand,new Vector3((i-1.5f)*.053f,-.2f,.055f),new[]{new Vector2(.024f,-.10f),new Vector2(.03f,-.045f),new Vector2(.028f,0)},dark,8);
            var thumb=Plate("Opposing thumb",hand,new Vector3(sign*.13f,-.12f,.07f),Leaf(.07f,.17f),.06f,silver);thumb.transform.localRotation=Quaternion.Euler(0,0,sign*35);
            return upper;
        }
        static Transform Leg(Transform root,float sign,out Transform shin)
        {
            var thigh=Node(sign<0?"Right hip joint":"Left hip joint",root,new Vector3(sign*.24f,1.35f,0)).transform;
            Lathe("Thigh flexible joint",thigh,Vector3.zero,new[]{new Vector2(.14f,-.58f),new Vector2(.19f,-.18f),new Vector2(.18f,0)},dark);
            Plate("Thigh directional silver armor",thigh,new Vector3(0,-.28f,.15f),Leaf(.35f,.60f),.10f,silver);
            Plate("Thigh outer gold edge",thigh,new Vector3(sign*.16f,-.22f,.03f),Leaf(.10f,.43f),.05f,gold);
            shin=Node("Knee joint",thigh,new Vector3(0,-.61f,0)).transform;
            Lathe("Greave understructure",shin,Vector3.zero,new[]{new Vector2(.10f,-.58f),new Vector2(.15f,-.25f),new Vector2(.15f,.02f)},dark);
            Plate("Pointed knee guard",shin,new Vector3(0,.01f,.19f),Leaf(.37f,.35f),.13f,silver);
            Plate("Greave front shell",shin,new Vector3(0,-.34f,.13f),Leaf(.28f,.58f),.1f,silver);
            var foot=Lathe("Sabatons beveled boot",shin,new Vector3(0,-.59f,.12f),new[]{new Vector2(.12f,-.10f),new Vector2(.17f,-.05f),new Vector2(.15f,.07f)},silver);foot.transform.localScale=new Vector3(1,1,1.8f);
            return thigh;
        }
        static Transform Ribbon(Transform parent,int index)
        {
            var root=Node("Ivory oath streamer "+index,parent,new Vector3((index-5.5f)*.041f,1.62f,-.20f)).transform;
            var vertices=new List<Vector3>();var triangles=new List<int>();int count=18;
            for(int i=0;i<=count;i++)
            {
                float t=i/(float)count;float length=1.72f+(index%3)*.14f;
                Vector3 p=new Vector3((index-5.5f)*.075f*t+Mathf.Sin(t*4+index)*.08f*t,-t*length,-Mathf.Sin(t*Mathf.PI*.7f)*(.25f+(index%4)*.08f));
                float width=(.075f+Mathf.Sin(t*Mathf.PI)*.045f)*(1-t*.85f);
                vertices.Add(p-Vector3.right*width);vertices.Add(p+Vector3.right*width);
                if(i<count)Quad(triangles,i*2,i*2+1,i*2+3,i*2+2);
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);MeshObject("Flowing carved seal ribbon",root,Vector3.zero,mesh,ivory);return root;
        }
        static GameObject BuildKnight()
        {
            var boss=Node("Oathbound Paladin Refined");var visual=boss.AddComponent<KnightVisuals>();visual.refinedRig=true;
            var hit=boss.AddComponent<CapsuleCollider>();hit.radius=.48f;hit.height=3.2f;hit.center=new Vector3(0,1.68f,0);
            visual.visualRoot=Node("Visual rig",boss.transform).transform;
            visual.visualRoot.localScale=new Vector3(.88f,1.06f,.94f);
            visual.torso=Node("Pelvis and spine",visual.visualRoot,new Vector3(0,1.35f,0)).transform;
            Lathe("Fitted torso flexible cuirass",visual.torso,Vector3.zero,new[]{new Vector2(.26f,0),new Vector2(.30f,.30f),new Vector2(.45f,.64f),new Vector2(.39f,.89f)},dark,20);
            Plate("Cuirass silver bevel",visual.torso,new Vector3(0,.61f,.27f),new[]{new Vector2(0,.46f),new Vector2(.42f,.28f),new Vector2(.39f,-.1f),new Vector2(.22f,-.49f),new Vector2(-.22f,-.49f),new Vector2(-.39f,-.1f),new Vector2(-.42f,.28f)},.17f,silver);
            Plate("Breastplate ivory inlay",visual.torso,new Vector3(0,.65f,.37f),Leaf(.36f,.66f),.045f,ivory);
            Plate("Cuirass oath rune",visual.torso,new Vector3(0,.64f,.4f),Leaf(.08f,.55f),.025f,light);
            for(int i=0;i<4;i++)Plate("Overlapping abdominal lames "+i,visual.torso,new Vector3(0,.22f-i*.09f,.24f),Leaf(.45f+i*.03f,.17f),.07f,i%2==0?silver:gold);
            Lathe("Neck collar",visual.torso,new Vector3(0,1.07f,0),new[]{new Vector2(.15f,-.08f),new Vector2(.20f,.04f),new Vector2(.14f,.12f)},gold);
            var helm=Lathe("Faceted sealed helmet",visual.torso,new Vector3(0,1.52f,0),new[]{new Vector2(.11f,-.25f),new Vector2(.21f,-.16f),new Vector2(.24f,.04f),new Vector2(.19f,.20f),new Vector2(.07f,.29f)},silver,16);helm.transform.localScale=new Vector3(.9f,1,1);
            Plate("Anonymous ivory faceplate",visual.torso,new Vector3(0,1.53f,.20f),Leaf(.32f,.42f),.10f,ivory);
            foreach(float sign in new[]{-1f,1f})
            {
                var eye=Plate("Recessed eye slit",visual.torso,new Vector3(sign*.075f,1.56f,.256f),Leaf(.105f,.045f),.016f,dark);eye.transform.localRotation=Quaternion.Euler(0,0,sign*12);
                var crest=Plate("Swept coronet crest",visual.torso,new Vector3(sign*.20f,1.76f,-.03f),Leaf(.13f,.64f),.07f,gold);crest.transform.localRotation=Quaternion.Euler(-18,0,-sign*25);
                for(int i=0;i<3;i++){var skirt=Plate("Segmented tasset "+i,visual.torso,new Vector3(sign*(.29f+i*.09f),-.10f-i*.13f,.01f),Leaf(.29f,.5f),.08f,silver);skirt.transform.localRotation=Quaternion.Euler(-5,sign*20,sign*(18+i*8));}
            }
            visual.rightArm=Arm(visual.torso,-1,out visual.rightForearm,out visual.rightHand);
            visual.leftArm=Arm(visual.torso,1,out visual.leftForearm,out visual.leftHand);
            visual.rightThigh=Leg(visual.visualRoot,-1,out visual.rightShin);visual.leftThigh=Leg(visual.visualRoot,1,out visual.leftShin);
            visual.sealRibbons=Enumerable.Range(0,12).Select(i=>Ribbon(visual.torso,i)).ToArray();
            visual.halo=Node("Authority halo",visual.torso,new Vector3(0,1.58f,-.38f)).transform;
            Ring("Engraved outer authority ring",visual.halo,Vector3.zero,.56f,.016f,gold);
            Ring("Fine internal light ring",visual.halo,new Vector3(0,0,.015f),.48f,.008f,light);
            for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9;var glyph=Plate("Halo glyph",visual.halo,new Vector3(Mathf.Cos(a)*.59f,Mathf.Sin(a)*.59f,0),Leaf(.055f,.15f),.025f,gold);glyph.transform.localRotation=Quaternion.Euler(0,0,i*40-90);}
            visual.weapon=Node("Two ended oath glaive",visual.rightHand,new Vector3(0,-.14f,.08f)).transform;
            Lathe("Glaive dark shaft",visual.weapon,Vector3.zero,new[]{new Vector2(.032f,-1.10f),new Vector2(.045f,-.95f),new Vector2(.038f,1.26f),new Vector2(.06f,1.34f)},dark,12);
            for(int i=0;i<7;i++)Lathe("Glaive grip ferrule",visual.weapon,new Vector3(0,-.75f+i*.30f,0),new[]{new Vector2(.049f,-.035f),new Vector2(.054f,0),new Vector2(.049f,.035f)},gold,12);
            var blade=Plate("Gothic spear blade",visual.weapon,new Vector3(0,1.87f,0),new[]{new Vector2(0,.98f),new Vector2(.14f,.32f),new Vector2(.27f,.15f),new Vector2(.15f,.02f),new Vector2(.18f,-.47f),new Vector2(.07f,-.65f),new Vector2(-.07f,-.65f),new Vector2(-.18f,-.47f),new Vector2(-.15f,.02f),new Vector2(-.27f,.15f),new Vector2(-.14f,.32f)},.09f,silver);
            Plate("Glaive golden central fuller",visual.weapon,new Vector3(0,1.88f,.053f),Leaf(.055f,1.6f),.018f,light);
            foreach(float sign in new[]{-1f,1f})
            {var hook=Plate("Glaive gothic hooked quillon",visual.weapon,new Vector3(sign*.20f,1.36f,0),Leaf(.15f,.65f),.055f,gold);hook.transform.localRotation=Quaternion.Euler(0,0,-sign*45);}
            Plate("Rear counterweight blade",visual.weapon,new Vector3(0,-1.2f,0),Leaf(.16f,.36f),.055f,silver);
            visual.swordTip=Node("BladeTip",visual.weapon,new Vector3(0,2.82f,0)).transform;
            var bladeBase=Node("BladeBase",visual.weapon,new Vector3(0,1.2f,0)).transform;
            var ward=Node("Folded wrist oath ward",visual.leftHand,new Vector3(0,-.05f,.15f));visual.wardShield=ward;
            Plate("Deployable ivory ward",ward.transform,Vector3.zero,Leaf(.62f,.90f),.10f,ivory);
            Ring("Ward authority border",ward.transform,new Vector3(0,0,.09f),.33f,.013f,gold,48);
            Plate("Ward seal axis",ward.transform,new Vector3(0,0,.12f),Leaf(.08f,.65f),.02f,light);ward.SetActive(false);
            var effects=boss.AddComponent<KnightRefinedEffects>();visual.refinedEffects=effects;effects.gold=light;effects.softGold=soft;effects.ivory=ivory;effects.bladeBase=bladeBase;effects.bladeTip=visual.swordTip;
            effects.readyBlades=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                var item=Node("Suspended oath blade "+i,visual.visualRoot,new Vector3((i-1)*.68f,3.7f,-.15f));
                Plate("Suspended gold blade",item.transform,Vector3.zero,Leaf(.11f,.70f),.025f,light);item.SetActive(false);effects.readyBlades[i]=item;
            }
            var model=PrefabUtility.SaveAsPrefabAsset(boss,Root+"/KnightRefined.prefab");UnityEngine.Object.DestroyImmediate(boss);return model;
        }
        static void RefineArena(BossArena arena,GameObject model)
        {
            UnityEngine.Object.DestroyImmediate(arena.boss.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,arena.transform);arena.boss=instance.transform;arena.boss.localPosition=new Vector3(0,0,4);arena.boss.localRotation=Quaternion.Euler(0,180,0);
            foreach(var l in arena.GetComponentsInChildren<Light>())l.intensity*=12;
            var architecture=Node("Refined sealing gate",arena.transform).transform;
            for(int side=-1;side<=1;side+=2)
            {
                Lathe("Gate ribbed pier",architecture,new Vector3(side*2.8f,0,13.25f),new[]{new Vector2(.48f,0),new Vector2(.54f,.2f),new Vector2(.30f,.4f),new Vector2(.30f,4.4f),new Vector2(.44f,4.55f),new Vector2(.40f,4.8f)},ivory);
                for(int n=0;n<5;n++){var arch=Plate("Ascending gate arch segment",architecture,new Vector3(side*(2.6f-n*.42f),4.8f+n*.42f,13.25f),Leaf(.55f,1.1f),.40f,ivory);arch.transform.localRotation=Quaternion.Euler(0,0,-side*42);}
            }
            var seal=Ring("Gate concentric authority seal",architecture,new Vector3(0,2.8f,13.12f),1.3f,.025f,gold);
            Ring("Gate inner seal glow",architecture,new Vector3(0,2.8f,13.10f),1.12f,.012f,light);
            for(int n=0;n<16;n++)
            {
                float a=n*Mathf.PI/8;var mark=Plate("Gate seal radial glyph",architecture,new Vector3(Mathf.Cos(a)*1.38f,2.8f+Mathf.Sin(a)*1.38f,13.08f),Leaf(.1f,.24f),.025f,gold);mark.transform.localRotation=Quaternion.Euler(0,0,n*22.5f-90);
            }
            foreach(float radius in new[]{10.6f,11.0f,13.0f})
            {var circle=Ring("Floor authority inlay",architecture,new Vector3(0,.018f,0),radius,.012f,gold,128);circle.transform.localRotation=Quaternion.Euler(90,0,0);}
            for(int n=0;n<12;n++)
            {
                float a=n*Mathf.PI/6;var pier=Lathe("Outer buttress",architecture,new Vector3(Mathf.Sin(a)*14.5f,0,Mathf.Cos(a)*14.5f),new[]{new Vector2(.55f,0),new Vector2(.45f,.3f),new Vector2(.25f,.5f),new Vector2(.25f,4.1f),new Vector2(.4f,4.3f),new Vector2(.2f,4.6f)},silver);
            }
        }
        [MenuItem("Arcane Archive/Knight/Apply Loshide reference refinement")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before replacing assets.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Current scene has unsaved edits.");
            foreach(string path in new[]{Root,Root+"/Meshes",Root+"/Materials"})Directory.CreateDirectory(path);AssetDatabase.Refresh();serial=0;
            silver=Material("SilverArmor",new Color(.52f,.57f,.63f),.76f,.56f);dark=Material("BlackJointSteel",new Color(.035f,.045f,.065f),.52f,.38f);
            gold=Material("OldGoldEdges",new Color(.62f,.40f,.13f),.72f,.55f);ivory=Material("IvoryOathRibbons",new Color(.82f,.78f,.65f),.15f,.36f);
            cloth=Material("VioletUndercloth",new Color(.08f,.045f,.12f),0,.25f);light=Material("OathGoldLight",new Color(2.2f,1.35f,.38f,.92f),0,0,true);soft=Material("OathGoldVeil",new Color(1.45f,.77f,.20f,.25f),0,0,true);
            var model=BuildKnight();
            var source=AssetDatabase.LoadAssetAtPath<BossArena>("Assets/Art/Bosses/Knight/Prefabs/KnightArena.prefab");
            var arena=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));var data=arena.GetComponent<BossArena>();RefineArena(data,model);
            var arenaPrefab=PrefabUtility.SaveAsPrefabAsset(arena,Root+"/KnightArenaRefined.prefab").GetComponent<BossArena>();PrefabUtility.UnloadPrefabContents(arena);
            var original=AssetDatabase.LoadAssetAtPath<KnightDefinition>("Assets/Art/Bosses/Knight/KnightDefinition.asset");
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(Root+"/KnightDefinitionRefined.asset");
            if(!config){config=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(config,Root+"/KnightDefinitionRefined.asset");}
            config.arena=arenaPrefab;config.danceStyle=true;
            config.moves=original.moves.Select(spec=>JsonUtility.FromJson<KnightMoveSpec>(JsonUtility.ToJson(spec))).Concat(new[]{
                new KnightMoveSpec{move=KnightMove.OrbitSweep,title="回身旋锋",hint="回身旋锋 · 两次回旋，绕到身后或弹反",warning=1,lockTime=.3f,duration=1.4f,recovery=1.25f,damage=17,reach=4.4f,arc=280,impacts=new[]{.24f,1.06f}},
                new KnightMoveSpec{move=KnightMove.Heavenfall,title="悬誓落雷",hint="悬誓落雷 · 三处落点依次爆发，离开光圈",warning=1.7f,lockTime=.55f,duration=1.95f,recovery=1.55f,damage=24,reach=1.45f,impacts=new[]{.16f,.86f,1.56f}}
            }).ToArray();
            config.moves[0].title="誓舞连锋";config.moves[0].hint="誓舞连锋 · 连续扫斩，弹反或绕侧";
            config.moves[3].title="地脉雷裁";config.moves[3].hint="地脉雷裁 · 离开金色地带或起跳";
            config.moves[4].title="收锋格御";config.moves[4].hint="收锋格御 · 绕侧或盾击，连打正面会反制";
            KnightPressureSetup.Tune(config);
            EditorUtility.SetDirty(config);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/KnightRefinedChinese.asset");
            if(!font)
            {
                font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/msyh.ttc",0,36,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024);
                string missing;font.TryAddCharacters("回身旋锋两次绕到后或弹反悬誓落雷三处点依爆发离开光圈舞连续扫斩地脉裁金色带起跳收格御侧盾击连打正面会制",out missing);
                font.atlasPopulationMode=AtlasPopulationMode.Static;font.fallbackFontAssetTable=new List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Bosses/Knight/KnightChinese.asset")};
                AssetDatabase.CreateAsset(font,Root+"/KnightRefinedChinese.asset");foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);
            }
            var scene=EditorSceneManager.OpenScene(KnightTrialBuilder.ScenePath);var launcher=UnityEngine.Object.FindFirstObjectByType<KnightTrialLauncher>();launcher.enabled=true;launcher.definition=config;launcher.font=font;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Refined articulated knight, glaive, streamers, authority gate and eight-move configuration built; original minimal assets retained.";
        }
    }
}
