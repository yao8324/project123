using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Editable, deterministic art source. No external model, shader or package dependency.</summary>
    public static partial class BossEncounterBuilder
    {
        const string Root = "Assets/Art/ForbiddenWarden";
        static Material stone, darkStone, gold, cloth, paper, ink, violet, lightGold;
        static int meshNumber;
        [MenuItem("Arcane Archive/Boss/Build encounter assets and connect current library")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building assets.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "MilanLibrary_Magic_V6") throw new InvalidOperationException("Open MilanLibrary_Magic_V6 first.");
            foreach (var p in new[] {Root, Root+"/Meshes", Root+"/Materials", Root+"/Prefabs", Root+"/Source", "Assets/Audio/Boss", "Assets/UI/Fonts"}) Directory.CreateDirectory(p);
            AssetDatabase.Refresh(); meshNumber = 0;
            stone = Mat("Moonstone", new Color(.43f,.42f,.46f), 0, .18f);
            darkStone = Mat("Obsidian", new Color(.065f,.056f,.080f), .35f, .37f);
            gold = Mat("AgedGold", new Color(.57f,.39f,.16f), .8f, .58f);
            cloth = Mat("VioletVelvet", new Color(.055f,.022f,.080f), .05f, .18f);
            paper = Mat("AncientParchment", new Color(.70f,.61f,.42f), .05f, .26f);
            ink = Mat("BindingLeather", new Color(.105f,.032f,.048f), .25f, .4f);
            violet = Mat("VioletSeal", new Color(.60f,.22f,1), 0,.4f,true);
            lightGold = Mat("SealGold", new Color(.95f,.69f,.30f),0,.4f,true);
            BorrowSurface(stone,"IV4_Interior_AgedLimestone",new Vector2(2,4));
            BorrowSurface(gold,"IV4_Oxidised_Brass",Vector2.one);
            BorrowSurface(ink,"IV4_Oxblood_Leather",Vector2.one);
            BorrowSurface(paper,"MAT_Old_Paper",Vector2.one);
            var model = BuildWarden();
            ExportObj(model, Root+"/Source/ForbiddenWarden.obj");
            var modelPrefab = PrefabUtility.SaveAsPrefabAsset(model, Root+"/Prefabs/ForbiddenWarden.prefab");
            UnityEngine.Object.DestroyImmediate(model);
            var effects = BuildEffects();
            var arena = BuildArena(modelPrefab, effects);
            var arenaPrefab = PrefabUtility.SaveAsPrefabAsset(arena, Root+"/Prefabs/SealChamber.prefab");
            UnityEngine.Object.DestroyImmediate(arena);
            var definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(Root+"/WardenEncounter.asset");
            if (!definition) { definition = ScriptableObject.CreateInstance<BossDefinition>(); AssetDatabase.CreateAsset(definition, Root+"/WardenEncounter.asset"); }
            definition.shotSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/RunicBolt.wav");
            definition.warningSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/SealWarning.wav");
            definition.impactSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/SealImpact.wav");
            definition.absorbSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Absorb.wav");
            EditorUtility.SetDirty(definition);
            var font = BuildFont();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if (!player) throw new InvalidOperationException("No existing FirstPersonController found.");
            Undo.RecordObject(player.gameObject,"Connect boss encounter");
            if (!player.GetComponent<PlayerCombat>()) Undo.AddComponent<PlayerCombat>(player.gameObject);
            var encounter = player.GetComponent<BossEncounter>() ?? Undo.AddComponent<BossEncounter>(player.gameObject);
            encounter.definition = definition; encounter.arenaPrefab = arenaPrefab.GetComponent<BossArena>(); encounter.combatFont = font;
            encounter.libraryEnvironment = scene.GetRootGameObjects().Single(g => g.name == "Milan Library — Magic V6");
            var menu = new SerializedObject(player.GetComponent<RecoveryMenu>());
            menu.FindProperty("uiFont").objectReferenceValue = font; menu.ApplyModifiedProperties();
            EditorUtility.SetDirty(encounter); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            UpgradeCombatAssets();
            RefineWardenModel();
            BuildCombatPolish();
            Debug.Log("[Boss] Warden model, seal chamber, five effects, font and encounter references built and saved.");
        }
        [MenuItem("Arcane Archive/Boss/Update laser and guard assets")]
        public static void UpgradeCombatAssets()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before updating combat assets.");
            var purple=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/VioletSeal.mat");
            var warm=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SealGold.mat");
            var ring=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RunicRing.prefab");
            if(!purple || !warm || !ring)throw new InvalidOperationException("Build base encounter assets first.");
            var root=Node("ChargedLaser");
            var beam=root.AddComponent<LaserBeamVisuals>();
            var glow=BeamMaterial("LaserVioletGlow",new Color(.48f,.16f,.9f,.22f),3000);
            var core=BeamMaterial("LaserGoldCore",new Color(1,.83f,.49f,.88f),3001);
            beam.outerBeam=BeamLine("VioletEnvelope",root.transform,glow,.6f);
            beam.coreBeam=BeamLine("GoldCore",root.transform,core,.16f);beam.coreBeam.sortingOrder=1;
            var seal=(GameObject)PrefabUtility.InstantiatePrefab(ring);seal.name="ChargeSigil";seal.transform.SetParent(root.transform,false);beam.chargeRing=seal.transform;
            beam.chargeCore=Primitive("ChargeCore",PrimitiveType.Sphere,root.transform,Vector3.zero,Vector3.one*.15f,purple).transform;
            beam.impactCore=Primitive("ImpactCore",PrimitiveType.Sphere,root.transform,Vector3.forward*10,Vector3.one*.075f,core).transform;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ChargedLaser.prefab");UnityEngine.Object.DestroyImmediate(root);
            var chamber=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/SealChamber.prefab");
            try{chamber.GetComponent<BossArena>().laserEffect=prefab.GetComponent<LaserBeamVisuals>();PrefabUtility.SaveAsPrefabAsset(chamber,Root+"/Prefabs/SealChamber.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(chamber);}
            var data=AssetDatabase.LoadAssetAtPath<BossDefinition>(Root+"/WardenEncounter.asset");EditorUtility.SetDirty(data);
            BuildFont();AssetDatabase.SaveAssets();
            Debug.Log("[Boss] Charged laser prefab, guard parameters and Chinese glyphs saved.");
        }
        static LineRenderer BeamLine(string name,Transform parent,Material material,float width)
        {
            var line=Node(name,parent).AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;
            line.SetPosition(0,Vector3.zero);line.SetPosition(1,Vector3.forward*10);line.startWidth=line.endWidth=width;
            line.numCapVertices=4;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            return line;
        }
        static Material BeamMaterial(string name,Color color,int queue)
        {
            var path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Surface",1);m.SetFloat("_Blend",2);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.One);
            m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.SetOverrideTag("RenderType","Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=queue;EditorUtility.SetDirty(m);return m;
        }
        static Material Mat(string name, Color c, float metallic, float smoothness, bool unlit = false)
        {
            string path = Root+"/Materials/"+name+".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",c); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smoothness); m.SetFloat("_Cull",0);
            EditorUtility.SetDirty(m); return m;
        }
        static void BorrowSurface(Material target,string sourceName,Vector2 tiling)
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/"+sourceName+".mat");
            if(!source || source.shader.name!="Universal Render Pipeline/Lit")return;
            foreach(string property in new[]{"_BaseMap","_BumpMap","_OcclusionMap"})
                if(source.HasProperty(property) && source.GetTexture(property)){target.SetTexture(property,source.GetTexture(property));target.SetTextureScale(property,tiling);}
            if(target.GetTexture("_BumpMap"))target.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(target);
        }
        static GameObject Node(string name, Transform parent = null, Vector3 position = default)
        { var g = new GameObject(name); g.transform.SetParent(parent,false); g.transform.localPosition=position; return g; }
        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool solid = false)
        {
            var g = GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;
            if (!solid) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // The primitive's capsule is invalid for a flattened circular floor.
                UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                g.AddComponent<MeshCollider>().sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
            }
            return g;
        }
        static GameObject Shape(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos = default)
        {
            string path=Root+"/Meshes/"+(meshNumber++).ToString("D3")+"_"+name+".asset";
            mesh.name=Path.GetFileNameWithoutExtension(path);
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved) { EditorUtility.CopySerialized(mesh,saved); UnityEngine.Object.DestroyImmediate(mesh); mesh=saved; }
            else AssetDatabase.CreateAsset(mesh,path);
            var g=Node(name,parent,pos); g.AddComponent<MeshFilter>().sharedMesh=mesh; g.AddComponent<MeshRenderer>().sharedMaterial=mat; return g;
        }
        static Mesh Mesh(List<Vector3> v,List<int> t)
        { var m=new Mesh(); if(v.Count>65535)m.indexFormat=IndexFormat.UInt32; m.SetVertices(v); m.SetTriangles(t,0); m.SetUVs(0,v.Select(p=>new Vector2(p.x,p.y)).ToList()); m.RecalculateNormals(); m.RecalculateBounds(); return m; }
        static Mesh Loft(float[] ys, float[] rx, float[] rz, int sides=64, float folds=0, bool torn=false)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(int y=0;y<ys.Length;y++) for(int j=0;j<=sides;j++)
            {
                float a=j*Mathf.PI*2/sides; float ripple=1+folds*Mathf.Cos(a*16);
                float hem=torn&&y==0?.13f*(1+Mathf.Sin(a*7)):0;
                v.Add(new Vector3(Mathf.Cos(a)*rx[y]*ripple,ys[y]+hem,Mathf.Sin(a)*rz[y]*ripple));
                if(y==ys.Length-1||j==sides)continue;
                int i=y*(sides+1)+j; t.AddRange(new[]{i,i+sides+1,i+1,i+1,i+sides+1,i+sides+2});
            }
            return Mesh(v,t);
        }
        static Mesh Plate(Vector2[] outline,float depth)
        {
            var v=new List<Vector3>(); var t=new List<int>(); int n=outline.Length;
            foreach(float z in new[]{-depth/2,depth/2}) foreach(var p in outline)v.Add(new Vector3(p.x,p.y,z));
            for(int i=1;i<n-1;i++)t.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
            for(int i=0;i<n;i++) { int j=(i+1)%n;t.AddRange(new[]{i,j,n+i,j,n+j,n+i}); }
            return Mesh(v,t);
        }
        static Mesh Ring(float inner,float outer,float start=0,float end=360,int segments=128)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<=segments;i++) { float a=Mathf.Lerp(start,end,i/(float)segments)*Mathf.Deg2Rad; v.Add(new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner));v.Add(new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer)); if(i<segments){int k=i*2;t.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});} }
            return Mesh(v,t);
        }
        static void Rod(string name,Transform p,Vector3 a,Vector3 b,float radius,Material mat)
        { var g=Primitive(name,PrimitiveType.Cylinder,p,(a+b)/2,new Vector3(radius*2,(b-a).magnitude/2,radius*2),mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); }
        static void Diamond(string name,Transform p,Vector3 pos,float w,float h,Material mat)
        { Shape(name,p,Plate(new[]{new Vector2(0,h/2),new Vector2(w/2,0),new Vector2(0,-h/2),new Vector2(-w/2,0)},.035f),mat,pos); }
        static GameObject BuildWarden()
        {
            var root=Node("Forbidden Warden — Veyr"); var rig=root.AddComponent<WardenVisuals>();
            var body=Node("FloatingBody",root.transform).transform; rig.floatingBody=body;
            Shape("PleatedRobes",body,Loft(new[]{.28f,.65f,1.4f,2.15f,2.9f,3.1f},new[]{1.0f,.93f,.73f,.53f,.7f,.48f},new[]{.65f,.62f,.5f,.40f,.40f,.32f},96,.10f,true),cloth);
            Shape("GoldHem",body,Loft(new[]{.39f,.46f},new[]{1.02f,1.01f},new[]{.67f,.66f},96,.10f,true),gold);
            Shape("ShoulderMantle",body,Loft(new[]{2.75f,2.95f,3.15f,3.38f},new[]{1.05f,1.1f,.76f,.38f},new[]{.47f,.5f,.42f,.27f},48,.04f),ink);
            Shape("MantleTrim",body,Loft(new[]{2.75f,2.81f},new[]{1.055f,1.08f},new[]{.48f,.49f},48,.04f),gold);
            for(int s=-1;s<=1;s+=2)
            {
                var panel=Shape("Stole"+(s<0?"L":"R"),body,Plate(new[]{new Vector2(-.16f,0),new Vector2(.16f,0),new Vector2(.20f,-1.65f),new Vector2(0,-1.95f),new Vector2(-.20f,-1.65f)},.045f),ink,new Vector3(s*.38f,2.5f,.49f));
                panel.transform.localRotation=Quaternion.Euler(5,0,-s*10);
                for(int i=0;i<5;i++) Diamond("StoleGlyph",panel.transform,new Vector3(0,-.3f-i*.28f,.036f),.095f,.18f,gold);
                var shoulder=Shape("Pauldrons",body,Plate(new[]{new Vector2(-.34f,-.16f),new Vector2(.38f,-.20f),new Vector2(.28f,.25f),new Vector2(0,.65f),new Vector2(-.28f,.24f)},.14f),gold,new Vector3(s*.92f,3.02f,.05f));
                shoulder.transform.localRotation=Quaternion.Euler(0,0,-s*30);
                var arm=Node(s<0?"LeftArm":"RightArm",body,new Vector3(s*.85f,2.9f,0)).transform;
                if(s<0)rig.leftArm=arm;else rig.rightArm=arm;
                Shape("Sleeve",arm,Loft(new[]{-1.1f,-.85f,-.2f,0},new[]{.27f,.22f,.20f,.19f},new[]{.30f,.25f,.20f,.18f},32,.08f),cloth);
                Shape("Cuff",arm,Loft(new[]{-1.12f,-1.02f},new[]{.28f,.27f},new[]{.31f,.30f},32),gold);
                Primitive("Gauntlet",PrimitiveType.Cube,arm,new Vector3(0,-1.29f,.04f),new Vector3(.25f,.36f,.15f),gold);
                for(int k=0;k<4;k++) {float x=(k-1.5f)*.065f;Rod("Claw",arm,new Vector3(x,-1.46f,.04f),new Vector3(x,-1.72f,.09f),.025f,gold);Rod("ClawTip",arm,new Vector3(x,-1.72f,.09f),new Vector3(x,-1.81f,.2f),.017f,gold);}
                Rod("Thumb",arm,new Vector3(-s*.10f,-1.25f,.06f),new Vector3(-s*.23f,-1.38f,.14f),.03f,gold);
                Rod("ThumbTip",arm,new Vector3(-s*.23f,-1.38f,.14f),new Vector3(-s*.19f,-1.51f,.21f),.02f,gold);
                var wing=Node(s<0?"LeftPageWing":"RightPageWing",body,new Vector3(s*.72f,2.95f,-.35f)).transform;
                if(s<0)rig.leftWing=wing;else rig.rightWing=wing;
                for(int k=0;k<7;k++)
                {
                    float a=(-20+k*14)*Mathf.Deg2Rad;float length=2.5f-k*.12f;
                    Vector3 tip=new Vector3(s*Mathf.Cos(a)*length,Mathf.Sin(a)*length,.12f+k*.025f);
                    Rod("WingRib",wing,Vector3.zero,tip,.025f,gold);
                    var leaf=Shape("InscribedPage",wing,PageLeaf(s,length,.29f,k),paper);
                    leaf.transform.localRotation=Quaternion.Euler(0,0,s*(-20+k*14));
                    for(int line=0;line<4;line++)
                        Rod("PageWriting",leaf.transform,new Vector3(s*(.8f+line*.25f),-.09f,.06f),new Vector3(s*(.8f+line*.25f),.08f,.06f),.009f,ink);
                }
            }
            Primitive("Neck",PrimitiveType.Cylinder,body,new Vector3(0,3.35f,0),new Vector3(.29f,.35f,.29f),darkStone);
            rig.mask=Node("FacelessMask",body,new Vector3(0,3.72f,.17f)).transform;
            Shape("GothicMask",rig.mask,Plate(new[]{new Vector2(0,.52f),new Vector2(.34f,.30f),new Vector2(.29f,-.18f),new Vector2(0,-.50f),new Vector2(-.29f,-.18f),new Vector2(-.34f,.30f)},.22f),gold);
            Shape("MaskInlay",rig.mask,Plate(new[]{new Vector2(0,.42f),new Vector2(.26f,.24f),new Vector2(.22f,-.14f),new Vector2(0,-.37f),new Vector2(-.22f,-.14f),new Vector2(-.26f,.24f)},.025f),darkStone,new Vector3(0,0,.125f));
            Diamond("SealedSight",rig.mask,new Vector3(0,.03f,.151f),.045f,.58f,violet);
            for(int s=-1;s<=1;s+=2)Rod("Brow",rig.mask,new Vector3(0,.13f,.16f),new Vector3(s*.24f,.24f,.16f),.025f,gold);
            rig.crownSegments=new Transform[7];
            for(int i=0;i<7;i++)
            {
                var crown=Node("BrokenHalo_"+i,body,new Vector3(0,3.9f,-.23f)).transform; rig.crownSegments[i]=crown; crown.localRotation=Quaternion.Euler(0,0,i*360f/7);
                Shape("HaloArc",crown,Ring(.85f,.94f,6,43,24),gold).transform.localRotation=Quaternion.Euler(90,0,0);
                var thorn=Shape("HaloSpire",crown,Plate(new[]{new Vector2(-.09f,.90f),new Vector2(0,1.30f),new Vector2(.09f,.90f)},.07f),gold);
                thorn.transform.localRotation=Quaternion.Euler(0,0,-64);
                Diamond("HaloSeal",crown,new Vector3(.78f,.34f,.03f),.06f,.12f,violet);
            }
            var tome=Node("HeartCodex",body,new Vector3(0,2.55f,.53f)).transform;
            Primitive("BookPages",PrimitiveType.Cube,tome,new Vector3(0,0,.07f),new Vector3(.65f,.88f,.22f),paper);
            Primitive("RearCover",PrimitiveType.Cube,tome,new Vector3(0,0,-.06f),new Vector3(.76f,1,.06f),ink);
            Primitive("Spine",PrimitiveType.Cube,tome,new Vector3(-.37f,0,.07f),new Vector3(.07f,1,.3f),gold);
            rig.chestCover=Node("HingedCover",tome,new Vector3(-.36f,0,.22f)).transform;
            Primitive("Cover",PrimitiveType.Cube,rig.chestCover,new Vector3(.36f,0,0),new Vector3(.74f,1,.055f),ink);
            for(int s=-1;s<=1;s+=2) {Rod("CoverBorder",rig.chestCover,new Vector3(.03f,s*.46f,.04f),new Vector3(.69f,s*.46f,.04f),.018f,gold);Rod("CoverBorder",rig.chestCover,new Vector3(.36f+s*.32f,-.46f,.04f),new Vector3(.36f+s*.32f,.46f,.04f),.018f,gold);}
            Diamond("CoverSigil",rig.chestCover,new Vector3(.36f,0,.06f),.24f,.5f,gold);
            Diamond("HeartSeal",tome,new Vector3(0,0,.23f),.16f,.52f,violet);
            var hit=root.AddComponent<CapsuleCollider>();hit.center=new Vector3(0,2.2f,0);hit.radius=.64f;hit.height=3.9f;
            var core=Node("CoreHitbox",root.transform,new Vector3(0,2.55f,.88f));core.AddComponent<SphereCollider>().radius=.30f;core.GetComponent<SphereCollider>().isTrigger=true;
            return root;
        }
        static Mesh PageLeaf(int sign,float length,float width,int seed)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=16;
            for(int i=0;i<=n;i++) {float u=i/(float)n;float w=width*Mathf.Sin(u*Mathf.PI)*.5f+.025f;float curve=.17f*Mathf.Sin(u*Mathf.PI*1.4f+seed*.25f);v.Add(new Vector3(sign*u*length,-w,curve));v.Add(new Vector3(sign*u*length,w,curve));if(i<n){int k=i*2;t.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
            return Mesh(v,t);
        }
        static GameObject[] BuildEffects()
        {
            var all=new List<GameObject>();
            var page=Node("ForbiddenPage");Shape("Page",page.transform,Plate(new[]{new Vector2(-.13f,-.2f),new Vector2(.13f,-.18f),new Vector2(.14f,.2f),new Vector2(-.12f,.22f)},.016f),paper);Diamond("PageSeal",page.transform,new Vector3(0,0,.02f),.08f,.21f,violet);all.Add(page);
            var ring=Node("RunicRing");Shape("RingOuter",ring.transform,Ring(.986f,1.014f),lightGold);Shape("RingInner",ring.transform,Ring(.87f,.884f),violet);
            for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16;var mark=Shape("SealMark",ring.transform,Plate(new[]{new Vector2(0,.035f),new Vector2(.016f,0),new Vector2(0,-.035f),new Vector2(-.016f,0)},.005f),violet,new Vector3(Mathf.Cos(a)*.935f,.003f,Mathf.Sin(a)*.935f));mark.transform.localRotation=Quaternion.Euler(90,0,a*Mathf.Rad2Deg);}
            all.Add(ring);
            var seal=Node("SealPillars");for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;var spire=Shape("RuneObelisk",seal.transform,Loft(new[]{0f,.3f,2f,2.7f},new[]{.19f,.14f,.11f,.005f},new[]{.19f,.14f,.11f,.005f},6),violet,new Vector3(Mathf.Cos(a)*.8f,0,Mathf.Sin(a)*.8f));Rod("SealSpine",spire.transform,new Vector3(0,0,-.15f),new Vector3(0,2.3f,-.05f),.018f,lightGold);}all.Add(seal);
            var bolt=Node("RunicBolt");var shard=Shape("Shard",bolt.transform,Loft(new[]{-.5f,0f,.2f},new[]{0f,.075f,0f},new[]{0f,.075f,0f},6),violet);shard.transform.localRotation=Quaternion.Euler(90,0,0);all.Add(bolt);
            var impact=Node("SealImpact");for(int i=0;i<8;i++){var shard2=Shape("Fragment",impact.transform,Loft(new[]{.15f,.45f,.75f},new[]{.005f,.05f,0f},new[]{.005f,.05f,0f},4),i%2==0?gold:violet);shard2.transform.localRotation=Quaternion.Euler(i*33,i*61,i*83);}all.Add(impact);
            var prefabs=new GameObject[all.Count];for(int i=0;i<all.Count;i++){prefabs[i]=PrefabUtility.SaveAsPrefabAsset(all[i],Root+"/Prefabs/"+all[i].name+".prefab");UnityEngine.Object.DestroyImmediate(all[i]);}return prefabs;
        }
        static GameObject BuildArena(GameObject model,GameObject[] effects)
        {
            var root=Node("SealChamber");var arena=root.AddComponent<BossArena>();
            var architecture=Node("Architecture",root.transform).transform;
            Primitive("SealFloor",PrimitiveType.Cylinder,architecture,new Vector3(0,-.25f,0),new Vector3(30,.25f,30),darkStone,true);
            Shape("OuterFloorInlay",architecture,Ring(12.4f,12.48f),gold,new Vector3(0,.013f,0));
            Shape("InnerFloorInlay",architecture,Ring(8.8f,8.84f),gold,new Vector3(0,.014f,0));
            for(int i=0;i<24;i++)
            {
                float angle=i*15;float a=angle*Mathf.Deg2Rad;
                Vector3 radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                var wall=Primitive("Wall_"+i,PrimitiveType.Cube,architecture,radial*15.1f+Vector3.up*4,new Vector3(4.05f,8,.65f),stone,true);wall.transform.localRotation=Quaternion.Euler(0,angle,0);
                var panel=Node("BlindArch_"+i,architecture,radial*14.7f).transform;panel.localRotation=Quaternion.Euler(0,angle+180,0);
                Shape("ArchRecess",panel,Plate(new[]{new Vector2(-1.3f,1.2f),new Vector2(1.3f,1.2f),new Vector2(1.3f,4.8f),new Vector2(.65f,6.15f),new Vector2(0,6.8f),new Vector2(-.65f,6.15f),new Vector2(-1.3f,4.8f)},.05f),darkStone);
                var path=new[]{new Vector3(-1.33f,1.1f,.06f),new Vector3(-1.33f,4.8f,.06f),new Vector3(-.68f,6.17f,.06f),new Vector3(0,6.9f,.06f),new Vector3(.68f,6.17f,.06f),new Vector3(1.33f,4.8f,.06f),new Vector3(1.33f,1.1f,.06f)};
                for(int j=0;j<path.Length-1;j++)Rod("GothicArchMoulding",panel,path[j],path[j+1],.085f,gold);
                Rod("ArchMullion",panel,new Vector3(0,1.3f,.08f),new Vector3(0,6.4f,.08f),.04f,gold);
                Diamond("WindowSigil",panel,new Vector3(0,4.2f,.12f),.35f,1.2f,i%3==0?violet:gold);
                var col=Node("Pier_"+i,architecture,new Vector3(Mathf.Sin(a+7.5f*Mathf.Deg2Rad),0,Mathf.Cos(a+7.5f*Mathf.Deg2Rad))*14.5f).transform;
                Primitive("ColumnBase",PrimitiveType.Cylinder,col,new Vector3(0,.18f,0),new Vector3(1.2f,.18f,1.2f),stone,true);
                Primitive("Column",PrimitiveType.Cylinder,col,new Vector3(0,3.7f,0),new Vector3(.64f,3.4f,.64f),stone,true);
                Shape("Capital",col,Loft(new[]{7f,7.2f,7.4f,7.5f},new[]{.32f,.34f,.55f,.55f},new[]{.32f,.34f,.55f,.55f},16),gold);
                Rod("FloorRadialInlay",architecture,radial*9+Vector3.up*.018f,radial*12.3f+Vector3.up*.018f,.018f,gold);
            }
            Shape("Vault",architecture,Loft(new[]{8f,9.8f,11.2f,12f},new[]{15.3f,12f,7f,3.5f},new[]{15.3f,12f,7f,3.5f},96),cloth);
            Shape("Oculus",architecture,Ring(3.4f,3.6f),gold,Vector3.up*12);
            Primitive("OculusGlass",PrimitiveType.Cylinder,architecture,Vector3.up*12.1f,new Vector3(7,.04f,7),violet);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Vector3 r=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));Rod("VaultRib",architecture,r*15+Vector3.up*8,r*7+Vector3.up*11.2f,.08f,gold);Rod("VaultRib",architecture,r*7+Vector3.up*11.2f,r*3.5f+Vector3.up*12,.08f,gold);}
            var throne=Node("BrokenArchiveGate",architecture,new Vector3(0,0,13.9f)).transform;throne.localRotation=Quaternion.Euler(0,180,0);
            Primitive("SealedGate",PrimitiveType.Cube,throne,new Vector3(0,3.2f,.10f),new Vector3(2.5f,5.8f,.16f),ink,true);
            for(int s=-1;s<=1;s+=2)for(int i=0;i<4;i++)Diamond("GateSeal",throne,new Vector3(s*.55f,1.1f+i*1.25f,.22f),.45f,.85f,gold);
            var lighting=Node("Lighting",root.transform).transform;
            AddLight("MoonKey",lighting,LightType.Directional,new Vector3(0,8,0),new Color(.72f,.68f,.9f),1.4f,0,Quaternion.Euler(46,-28,0),true);
            AddLight("GoldFront",lighting,LightType.Point,new Vector3(0,5,-5),new Color(1,.72f,.42f),12,22,Quaternion.identity,false);
            AddLight("VioletRim",lighting,LightType.Point,new Vector3(0,5,9),new Color(.53f,.27f,1),15,18,Quaternion.identity,false);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Vector3 p=new Vector3(Mathf.Sin(a)*13,3,Mathf.Cos(a)*13);Primitive("SealLantern",PrimitiveType.Sphere,lighting,p,Vector3.one*.18f,lightGold);AddLight("WarmPool_"+i,lighting,LightType.Point,p,new Color(1,.58f,.27f),5,8,Quaternion.identity,false);}
            arena.spawn=Node("PlayerSpawn",root.transform,new Vector3(0,.08f,-10)).transform;
            var boss=(GameObject)PrefabUtility.InstantiatePrefab(model);boss.transform.SetParent(root.transform,false);boss.transform.localPosition=new Vector3(0,0,4);boss.transform.localRotation=Quaternion.Euler(0,180,0);
            arena.boss=boss.transform;arena.model=boss.GetComponent<WardenVisuals>();
            arena.pageProjectile=effects[0];arena.ringEffect=effects[1];arena.sealEffect=effects[2];arena.boltEffect=effects[3];arena.impactEffect=effects[4];arena.effectRoot=Node("CombatEffects",root.transform).transform;
            return root;
        }
        static void AddLight(string name,Transform parent,LightType type,Vector3 pos,Color color,float power,float range,Quaternion rotation,bool shadows)
        {var g=Node(name,parent,pos);g.transform.localRotation=rotation;var l=g.AddComponent<Light>();l.type=type;l.color=color;l.intensity=power;l.range=range;l.shadows=shadows?LightShadows.Soft:LightShadows.None;}
        static TMP_FontAsset BuildFont()
        {
            const string path="Assets/UI/Fonts/BossChineseSDF.asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            var font=existing ? existing : TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/msyh.ttc",0,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
            font.name="BossChineseSDF";font.isMultiAtlasTexturesEnabled=true;
            font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
            var chars=new HashSet<char>();
            foreach(string file in Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories))foreach(char c in File.ReadAllText(file))if(!char.IsControl(c))chars.Add(c);
            for(char c=' ';c<='~';c++)chars.Add(c);
            var old=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/ArchiveChineseSDF.asset");
            foreach(var c in old.characterTable)if(c.unicode<=char.MaxValue)chars.Add((char)c.unicode);
            string missing; font.TryAddCharacters(new string(chars.OrderBy(c=>c).ToArray()),out missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            if(!existing)AssetDatabase.CreateAsset(font,path);
            foreach(var atlas in font.atlasTextures){atlas.filterMode=FilterMode.Bilinear;if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);EditorUtility.SetDirty(atlas);}
            if(!AssetDatabase.Contains(font.material))AssetDatabase.AddObjectToAsset(font.material,font);EditorUtility.SetDirty(font);
            // Baking makes the game independent of this Windows font path at runtime.
            Debug.Log("[Boss] Baked font characters="+font.characterTable.Count+"; unavailable symbols="+missing);
            return font;
        }
        static void ExportObj(GameObject root,string path)
        {
            var b=new StringBuilder("# Arcane Archive Forbidden Warden — editable mesh export\n");int offset=1;
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                b.Append("g ").AppendLine(f.name.Replace(' ','_'));
                foreach(var p in f.sharedMesh.vertices){var v=root.transform.InverseTransformPoint(f.transform.TransformPoint(p));b.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0} {1} {2}\n",-v.x,v.y,v.z);}
                foreach(var n in f.sharedMesh.normals){var v=root.transform.InverseTransformDirection(f.transform.TransformDirection(n)).normalized;b.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"vn {0} {1} {2}\n",-v.x,v.y,v.z);}
                var t=f.sharedMesh.triangles;for(int i=0;i<t.Length;i+=3)b.AppendFormat("f {0}//{0} {1}//{1} {2}//{2}\n",t[i]+offset,t[i+2]+offset,t[i+1]+offset);
                offset+=f.sharedMesh.vertexCount;
            }
            File.WriteAllText(path,b.ToString(),new UTF8Encoding(false));
        }
    }
}
