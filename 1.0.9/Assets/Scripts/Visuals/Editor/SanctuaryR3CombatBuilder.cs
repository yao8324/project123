using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ArcaneArchive.Core;
using ArcaneArchive.Visuals;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
    public static class SanctuaryR3CombatBuilder
    {
        public const string Root="Assets/Art/Bosses/Knight/SanctuaryR3";
        public const string Scene="Assets/Scenes/BossTests/Knight_Sanctuary_R3_Combat.unity";
        public const string Config=Root+"/KnightR3.asset";
        static T Save<T>(T value,string path) where T:Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;}
        [MenuItem("Tools/Knight/R3/Build Combat First Pass")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Animations");AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/BossTests/Knight_Sanctuary_R3_Preview.unity");
            var environment=GameObject.Find("R3_56m_Environment_FirstPass");if(!environment)throw new Exception("Build R3 preview first");
            var arenaRoot=PrefabUtility.LoadPrefabContents("Assets/Art/Bosses/Knight/ProductionV5/KnightArenaProduction.prefab");
            var arena=arenaRoot.GetComponent<BossArena>();arena.radius=28;
            foreach(var r in arenaRoot.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var c in arenaRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(arena.boss))Object.DestroyImmediate(c);
            foreach(var l in arenaRoot.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l);
            var oldPresentation=arenaRoot.GetComponent<KnightArenaPresentation>();if(oldPresentation)Object.DestroyImmediate(oldPresentation);
            var art=Object.Instantiate(environment,arenaRoot.transform);art.name="R3_Architecture";art.transform.localPosition=Vector3.zero;
            arena.spawn.localPosition=new Vector3(0,.08f,-22);arena.spawn.localRotation=Quaternion.identity;
            var marker=new GameObject("R3_BossSpawn").transform;marker.SetParent(arenaRoot.transform,false);marker.localPosition=new Vector3(0,0,25);arena.bossSpawn=marker;
            arena.boss.localScale=Vector3.one;arena.boss.localPosition=marker.localPosition;
            var capsule=arena.boss.GetComponent<CapsuleCollider>();capsule.height=2.28f;capsule.radius=.42f;capsule.center=new Vector3(0,1.22f,0);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Paladin_R3_FirstPass.prefab"),arena.boss);
            visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.Euler(0,180,0);visual.transform.localScale=Vector3.one;
            var adapter=arena.boss.gameObject.AddComponent<SanctuaryR3CombatVisuals>();adapter.actor=visual.GetComponent<Animator>();
            var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"/Paladin_R3_Animated.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            int removed=0;adapter.clips=new[]{"A01_SlowWalk","A02_Dash","A03_JumpAttack"}.Select(name=>{
                var clip=Object.Instantiate(clips.First(c=>c.name.Contains(name)));clip.name=name+"_InPlace";
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))if((binding.path=="Root"||binding.path.EndsWith("/Root")) && binding.propertyName.StartsWith("m_LocalPosition")){AnimationUtility.SetEditorCurve(clip,binding,null);removed++;}
                return Save(clip,Root+"/Animations/"+clip.name+".anim");
            }).ToArray();if(removed<9)throw new Exception("Root-position stripping incomplete: "+removed);
            var old=arena.boss.GetComponent<KnightVisuals>();old.sanctuaryR3=adapter;
            var floor=new GameObject("R3_CollisionFloor");floor.transform.SetParent(arenaRoot.transform,false);var floorBox=floor.AddComponent<BoxCollider>();floorBox.center=new Vector3(0,-.175f,0);floorBox.size=new Vector3(64,.35f,72);
            for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;var wall=new GameObject("R3_CombatBoundary_"+i);wall.transform.SetParent(arenaRoot.transform,false);wall.transform.localPosition=new Vector3(Mathf.Sin(a)*28.15f,2,Mathf.Cos(a)*28.15f);wall.transform.localRotation=Quaternion.Euler(0,i*360f/96,0);wall.AddComponent<BoxCollider>().size=new Vector3(1.85f,4,.3f);}
            var atmosphere=GameObject.Find("R3_Atmosphere");
            if(atmosphere){var copy=Object.Instantiate(atmosphere,arenaRoot.transform);copy.name="R3_Atmosphere";var childOwner=copy.GetComponent<KnightSanctuaryLighting>();var owner=arenaRoot.AddComponent<KnightSanctuaryLighting>();owner.pipeline=childOwner.pipeline;Object.DestroyImmediate(childOwner);}
            // Runtime-instantiated prefabs cannot retain scene lightmap probe bindings.
            var roomReflection=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Lighting/R3RoomReflection.exr");
            if(!roomReflection)throw new Exception("Missing baked room reflection asset");
            foreach(var probe in arenaRoot.GetComponentsInChildren<ReflectionProbe>()){probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Custom;probe.customBakedTexture=roomReflection;}
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.gameObject.scene==environment.scene && (!atmosphere || !l.transform.IsChildOf(atmosphere.transform))).ToArray()){var copy=Object.Instantiate(light.gameObject,arenaRoot.transform);copy.name=light.name;}
            var saved=PrefabUtility.SaveAsPrefabAsset(arenaRoot,Root+"/KnightR3Arena.prefab").GetComponent<BossArena>();PrefabUtility.UnloadPrefabContents(arenaRoot);
            var source=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);var config=Object.Instantiate(source);config.arena=saved;config.walkSpeed=2;config.sanctuaryDash=true;config.thrustTravel=8;config.pursuitLeapTravel=10;config.pursuitLeapHeight=4;config.leapFlightDuration=.9f;
            var combat=Object.Instantiate(source.combat);combat.bossScale=1;config.combat=Save(combat,Root+"/KnightR3Combat.asset");
            var dash=config.Get(KnightMove.SealThrust);dash.warning=.18f;dash.lockTime=.12f;dash.duration=.5f;dash.recovery=.27f;dash.impacts=new[]{.5f};
            var jump=config.Get(KnightMove.LeapCleave);jump.warning=.2f;jump.lockTime=.15f;jump.duration=.9f;jump.recovery=.5f;jump.impacts=new[]{.9f};
            config=Save(config,Config);
            var scene=EditorSceneManager.OpenScene(KnightProductionBuilder.TestScene);Object.FindFirstObjectByType<KnightTrialLauncher>().definition=config;EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/SanctuaryR3Build/CombatBuild.txt","R3 first combat wiring; NOT final acceptance.\nRadius=28, player=-22Z, boss=25Z, scale=1.\nRemoved root position curves="+removed+"\nCombat alone owns movement.\nOther attack presentation, folded wings and VFX sockets need refinement.\n");Debug.Log("R3_COMBAT_BUILD_COMPLETE");
        }
    }
}
