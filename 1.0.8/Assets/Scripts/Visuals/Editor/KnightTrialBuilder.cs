using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ArcaneArchive.Visuals.Editor
{
    public static class KnightTrialBuilder
    {
        const string Root="Assets/Art/Bosses/Knight";
        public const string ScenePath="Assets/Scenes/BossTests/Knight_Test.unity";
        static GameObject Node(string name,Transform parent=null)
        {var go=new GameObject(name);if(parent)go.transform.SetParent(parent,false);return go;}
        static Material Mat(string name,Color color,bool glow=false)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(glow?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);if(!glow){m.SetFloat("_Metallic",.35f);m.SetFloat("_Smoothness",.35f);}
            EditorUtility.SetDirty(m);return m;
        }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 size,Material mat,bool collision=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static KnightMoveSpec Move(KnightMove move,string title,string hint,float warning,float locked,float duration,float recovery,float damage,float reach,float width,float arc,params float[] impacts)
        {return new KnightMoveSpec{move=move,title=title,hint=hint,warning=warning,lockTime=locked,duration=duration,recovery=recovery,damage=damage,reach=reach,width=width,arc=arc,impacts=impacts};}
        [MenuItem("Arcane Archive/Knight/Build minimal trial")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save your current scene first.");
            foreach(var path in new[]{Root,Root+"/Materials",Root+"/Prefabs","Assets/Scenes/BossTests"})Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            var ivory=Mat("Ivory",new Color(.74f,.72f,.65f));var gold=Mat("OldGold",new Color(.64f,.43f,.15f));
            var steel=Mat("DarkSteel",new Color(.085f,.1f,.14f));var cloth=Mat("MutedViolet",new Color(.12f,.065f,.16f));
            var floor=Mat("Floor",new Color(.14f,.15f,.18f));
            var white=Mat("ParryWhite",new Color(1,.91f,.68f),true);var amber=Mat("GroundAmber",new Color(1,.4f,.08f),true);var blue=Mat("AbsorbBlue",new Color(.3f,.65f,1),true);
            var boss=Node("Sealing Paladin");var visual=boss.AddComponent<KnightVisuals>();
            var body=boss.AddComponent<CapsuleCollider>();body.center=new Vector3(0,1.68f,0);body.radius=.58f;body.height=3.2f;
            visual.torso=Node("Torso",boss.transform).transform;
            Shape("Cuirass",PrimitiveType.Cube,visual.torso,new Vector3(0,1.95f,0),new Vector3(1.15f,.95f,.64f),ivory);
            Shape("Seal",PrimitiveType.Cube,visual.torso,new Vector3(0,2.02f,.34f),new Vector3(.12f,.68f,.06f),gold);
            Shape("Helmet",PrimitiveType.Cube,visual.torso,new Vector3(0,2.86f,0),new Vector3(.53f,.59f,.56f),ivory);
            Shape("Visor",PrimitiveType.Cube,visual.torso,new Vector3(0,2.91f,.29f),new Vector3(.41f,.065f,.035f),steel);
            Shape("Seal tabard",PrimitiveType.Cube,visual.torso,new Vector3(0,1.2f,.25f),new Vector3(.65f,.9f,.07f),cloth);
            foreach(float sign in new[]{-1f,1f})
            {
                Shape("Greave",PrimitiveType.Cube,boss.transform,new Vector3(sign*.33f,.69f,0),new Vector3(.4f,1.25f,.45f),steel);
                Shape("Boot",PrimitiveType.Cube,boss.transform,new Vector3(sign*.33f,.12f,.13f),new Vector3(.45f,.24f,.7f),ivory);
            }
            visual.rightArm=Node("Sword arm",visual.torso).transform;visual.rightArm.localPosition=new Vector3(-.8f,2.4f,0);
            visual.leftArm=Node("Shield arm",visual.torso).transform;visual.leftArm.localPosition=new Vector3(.8f,2.4f,0);
            foreach(var arm in new[]{visual.rightArm,visual.leftArm})
            {
                Shape("Pauldron",PrimitiveType.Cube,arm,Vector3.zero,new Vector3(.55f,.4f,.7f),gold);
                Shape("Arm",PrimitiveType.Cube,arm,new Vector3(0,-.55f,0),new Vector3(.29f,1,.35f),ivory);
                Shape("Gauntlet",PrimitiveType.Cube,arm,new Vector3(0,-1.1f,0),new Vector3(.31f,.3f,.38f),steel);
            }
            var sword=Node("Sword",visual.rightArm);sword.transform.localPosition=new Vector3(0,-1.15f,.1f);
            Shape("Grip",PrimitiveType.Cube,sword.transform,Vector3.zero,new Vector3(.13f,.4f,.13f),steel);
            Shape("Crossguard",PrimitiveType.Cube,sword.transform,new Vector3(0,-.24f,0),new Vector3(.65f,.12f,.18f),gold);
            Shape("Blade",PrimitiveType.Cube,sword.transform,new Vector3(0,-.95f,0),new Vector3(.25f,1.35f,.09f),ivory);
            Shape("Blade rune",PrimitiveType.Cube,sword.transform,new Vector3(0,-.95f,.055f),new Vector3(.045f,1.2f,.02f),white);
            visual.swordTip=Node("Sword tip",sword.transform).transform;visual.swordTip.localPosition=new Vector3(0,-1.65f,0);
            var shield=Shape("Gate shield",PrimitiveType.Cube,visual.leftArm,new Vector3(0,-.75f,.34f),new Vector3(.9f,1.3f,.17f),ivory);
            Shape("Shield upright",PrimitiveType.Cube,shield.transform,new Vector3(0,0,.55f),new Vector3(.13f,.83f,.14f),gold);
            visual.halo=Node("Complete seal halo",visual.torso).transform;visual.halo.localPosition=new Vector3(0,2.9f,-.4f);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;var seg=Shape("Halo segment",PrimitiveType.Cube,visual.halo,new Vector3(Mathf.Cos(a)*.78f,Mathf.Sin(a)*.78f,0),new Vector3(.36f,.08f,.08f),gold);
                seg.transform.localRotation=Quaternion.Euler(0,0,i*30+90);
            }
            var model=PrefabUtility.SaveAsPrefabAsset(boss,Root+"/Prefabs/Knight.prefab");UnityEngine.Object.DestroyImmediate(boss);
            var arena=Node("Knight Arena");var runtime=arena.AddComponent<BossArena>();runtime.radius=14;
            var platform=Shape("Flat circular floor",PrimitiveType.Cylinder,arena.transform,new Vector3(0,-.18f,0),new Vector3(28,.18f,28),floor);
            platform.AddComponent<MeshCollider>().sharedMesh=platform.GetComponent<MeshFilter>().sharedMesh;
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16;var wall=Shape("Visible boundary",PrimitiveType.Cube,arena.transform,new Vector3(Mathf.Sin(a)*14,1.5f,Mathf.Cos(a)*14),new Vector3(2.85f,3,.5f),steel,true);
                wall.transform.localRotation=Quaternion.Euler(0,i*11.25f,0);
            }
            Shape("Sealed door",PrimitiveType.Cube,arena.transform,new Vector3(0,2.4f,13.6f),new Vector3(4.4f,4.8f,.7f),ivory);
            Shape("Door seam",PrimitiveType.Cube,arena.transform,new Vector3(0,2.4f,13.2f),new Vector3(.1f,4.4f,.08f),gold);
            runtime.spawn=Node("PlayerSpawn",arena.transform).transform;runtime.spawn.localPosition=new Vector3(0,.08f,-5);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,arena.transform);runtime.boss=instance.transform;runtime.boss.localPosition=new Vector3(0,0,4);runtime.boss.localRotation=Quaternion.Euler(0,180,0);
            runtime.effectRoot=Node("Effects",arena.transform).transform;
            var light=Node("Warm key",arena.transform).AddComponent<Light>();light.type=LightType.Point;light.range=35;light.intensity=4;light.transform.localPosition=new Vector3(-4,6,-2);light.color=new Color(1,.87f,.68f);
            var fill=Node("Cool fill",arena.transform).AddComponent<Light>();fill.type=LightType.Point;fill.range=30;fill.intensity=2;fill.transform.localPosition=new Vector3(4,6,7);fill.color=new Color(.6f,.7f,1);
            runtime.pageProjectile=EffectPrefab("SealSpear",blue,new Vector3(.13f,.13f,.8f));
            runtime.boltEffect=EffectPrefab("PlayerBolt",white,Vector3.one*.13f);
            runtime.impactEffect=EffectPrefab("Impact",white,Vector3.one*.12f);
            var ring=Node("GuardRing");var line=ring.AddComponent<LineRenderer>();line.sharedMaterial=white;line.useWorldSpace=false;line.positionCount=49;line.startWidth=line.endWidth=.025f;
            for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));}
            runtime.ringEffect=PrefabUtility.SaveAsPrefabAsset(ring,Root+"/Prefabs/GuardRing.prefab");UnityEngine.Object.DestroyImmediate(ring);
            var arenaPrefab=PrefabUtility.SaveAsPrefabAsset(arena,Root+"/Prefabs/KnightArena.prefab").GetComponent<BossArena>();UnityEngine.Object.DestroyImmediate(arena);
            string configPath=Root+"/KnightDefinition.asset";var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(configPath);
            if(!config){config=ScriptableObject.CreateInstance<KnightDefinition>();AssetDatabase.CreateAsset(config,configPath);}
            string combatPath=Root+"/KnightCombat.asset";var combat=AssetDatabase.LoadAssetAtPath<BossDefinition>(combatPath);
            if(!combat){combat=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BossDefinition>("Assets/Art/ForbiddenWarden/WardenEncounter.asset"));AssetDatabase.CreateAsset(combat,combatPath);}
            combat.displayName="封印圣骑士";combat.health=1400;combat.bossScale=1;combat.poiseCapacity=120;combat.staggerDuration=1.25f;combat.bodyTurnSpeed=100;EditorUtility.SetDirty(combat);
            config.combat=combat;config.arena=arenaPrefab;config.blockable=white;config.unblockable=amber;config.absorbable=blue;
            config.moves=new[]{
                Move(KnightMove.OathCombo,"誓约连斩","誓约连斩 · 弹反或绕侧，留意后续斩",.9f,.25f,1.25f,1.15f,18,3.9f,0,120,.18f,.95f),
                Move(KnightMove.SealThrust,"贯门突刺","贯门突刺 · 金线锁定后侧闪",1.15f,.35f,.65f,1.35f,30,6.4f,1.2f,0,.44f),
                Move(KnightMove.SealBolts,"封印飞矛","封印飞矛 · 侧移或正面吞印",1,.3f,.95f,1.1f,12,0,0,360,.10f,.42f,.74f),
                Move(KnightMove.GateCleave,"门扉裁断","门扉裁断 · 离开金色地带或起跳",1.25f,.5f,.55f,1.5f,28,12,1.35f,0,.15f),
                Move(KnightMove.Bastion,"守门盾誓","守门盾誓 · 绕侧或盾击，连续正面攻击会反制",.65f,.25f,1.8f,.8f,0,2.3f,0,120),
                Move(KnightMove.ShieldBash,"退界盾击","退界盾击 · 抬盾后退，保持距离",.65f,.3f,.5f,1,16,2.8f,0,110,.2f)
            };EditorUtility.SetDirty(config);
            var font=BuildFont();AssetDatabase.SaveAssets();
            if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) && !AssetDatabase.CopyAsset("Assets/Scenes/MilanLibrary_Magic_V6.unity",ScenePath))throw new InvalidOperationException("Cannot copy trial scene.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var encounter=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();var launcher=encounter.GetComponent<KnightTrialLauncher>();
            if(!launcher)launcher=encounter.gameObject.AddComponent<KnightTrialLauncher>();launcher.definition=config;launcher.font=font;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Minimal knight assets built. Open "+ScenePath+" and press Play. Main scene and build settings are unchanged.";
        }
        static GameObject EffectPrefab(string name,Material mat,Vector3 size)
        {
            var go=Node(name);Shape("Core",PrimitiveType.Cube,go.transform,Vector3.zero,size,mat);
            var asset=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return asset;
        }
        static TMP_FontAsset BuildFont()
        {
            string path=Root+"/KnightChinese.asset";var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing)return existing;
            var font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/msyh.ttc",0,36,5,GlyphRenderMode.SDFAA,2048,2048);
            if(!font)throw new InvalidOperationException("Cannot bake Knight glyphs from the local Chinese font.");
            font.name="KnightChinese";
            string text="封印圣骑士守门誓约断流正面盾防绕侧或用击打连斩反留意后续贯突刺金线锁定闪飞矛移吞扉裁离开色地带起跳连续攻击会制退界抬保持距离源石不能位外层闭前回的也有人失去独立招式原型本次结果提交主剧情结局观察向弹和寻找机窗口还没有全部撤他握住剑却再一步挑战此处接入点算完成突破未成";
            string missing;if(!font.TryAddCharacters(text,out missing))Debug.LogWarning("Knight glyphs missing: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            font.fallbackFontAssetTable=new List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/BossChineseSDF.asset")};
            AssetDatabase.CreateAsset(font,path);
            foreach(var tex in font.atlasTextures)AssetDatabase.AddObjectToAsset(tex,font);AssetDatabase.AddObjectToAsset(font.material,font);
            EditorUtility.SetDirty(font);return font;
        }
    }
}
