using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    public static class SwordShieldSetup
    {
        const string Root="Assets/Art/PlayerSwordShield";
        static GameObject Node(string name,Transform parent=null){var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);return g;}
        static Material Surface(string source)
        {
            string key=source.Replace("Arms _ ","").Split('.')[0];string path=Root+"/Materials/"+key+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=key;
            Color color=new Color(.09f,.08f,.11f);float metal=.8f,smooth=.56f,emission=.03f;
            switch(key)
            {
                case "HonedSteel":color=new Color(.60f,.65f,.73f);metal=.88f;smooth=.72f;break;
                case "BlackenedSteel":color=new Color(.065f,.078f,.105f);metal=.76f;break;
                case "AntiqueGold":color=new Color(.50f,.29f,.095f);metal=.78f;break;
                case "PolishedGold":color=new Color(.80f,.55f,.21f);metal=.83f;smooth=.7f;break;
                case "IvorySeal":color=new Color(.79f,.72f,.57f);metal=.05f;smooth=.30f;break;
                case "ObsidianAmethyst":color=new Color(.17f,.035f,.32f);metal=.65f;smooth=.85f;emission=.16f;break;
                case "VioletSeal":color=new Color(.36f,.08f,.85f);metal=.1f;emission=1.7f;break;
                case "OxbloodLeather":color=new Color(.12f,.032f,.036f);metal=0;smooth=.27f;break;
                case "CharcoalLeather":color=new Color(.041f,.03f,.028f);metal=0;smooth=.20f;break;
                case "Recess":color=new Color(.018f,.014f,.022f);metal=.1f;smooth=.1f;break;
                case "WaxedThread":color=new Color(.43f,.30f,.18f);metal=0;smooth=.2f;break;
            }
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);AssetDatabase.CreateAsset(m,path);return m;
        }
        static Material LightMaterial(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Arcane Archive/Blade Light"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Weapon(string fbx,string name)
        {
            string path=Root+"/Models/"+fbx+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            var root=Node(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Surface(m.name)).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        [MenuItem("Arcane Archive/Combat/Connect sword shield and melee")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.name!="MilanLibrary_Magic_V6" || scene.isDirty)throw new InvalidOperationException("The saved MilanLibrary_Magic_V6 scene is required.");
            foreach(string sub in new[]{"Materials","Prefabs","Animations"})Directory.CreateDirectory(Root+"/"+sub);
            AssetDatabase.Refresh();
            var sword=Weapon("Oathbreaker_Sword_v1","Oathbreaker_Sword");var shield=Weapon("VeiledEye_Shield_v1","VeiledEye_Shield");
            var grip=OathboundHandsBuilder.BuildPrefabs();
            var loadout=AssetDatabase.LoadAssetAtPath<MeleeLoadout>(Root+"/OathboundLoadout.asset");
            if(!loadout){loadout=ScriptableObject.CreateInstance<MeleeLoadout>();AssetDatabase.CreateAsset(loadout,Root+"/OathboundLoadout.asset");}
            loadout.swordPrefab=sword;loadout.shieldPrefab=shield;
            loadout.goldEffect=LightMaterial("SwordGoldLight",new Color(1.8f,1.20f,.55f,1));
            loadout.violetEffect=LightMaterial("SealVioletLight",new Color(.85f,.19f,2.3f,1));EditorUtility.SetDirty(loadout);
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();var camera=player.GetComponentInChildren<Camera>();
            var melee=player.GetComponent<PlayerMelee>();if(!melee)melee=Undo.AddComponent<PlayerMelee>(player.gameObject);melee.loadout=loadout;
            var old=camera.transform.Find("MageHand_ViewModel");if(old){Undo.RecordObject(old.gameObject,"Equip sword and shield");old.gameObject.SetActive(false);}
            var existing=camera.transform.Find("SwordShield_ViewModel");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var root=Node("SwordShield_ViewModel",camera.transform);var view=root.AddComponent<SwordShieldView>();view.melee=melee;melee.view=view;
            view.viewCamera=camera;view.presentationCamera=camera.transform.Find("FirstPersonPresentationCamera").GetComponent<Camera>();
            view.swordPivot=Node("Left sword grip",root.transform).transform;view.shieldPivot=Node("Right shield grip",root.transform).transform;
            var right=(GameObject)PrefabUtility.InstantiatePrefab(sword,view.swordPivot);var left=(GameObject)PrefabUtility.InstantiatePrefab(shield,view.shieldPivot);
            var swordHand=(GameObject)PrefabUtility.InstantiatePrefab(grip.left,view.swordPivot);swordHand.name="Left sword gripping hand";
            var shieldHand=(GameObject)PrefabUtility.InstantiatePrefab(grip.right,view.shieldPivot);shieldHand.name="Right shield gripping hand";
            view.swordPivot.localScale=Vector3.one*.82f;view.shieldPivot.localScale=Vector3.one*.90f;
            view.bladeBase=right.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket_BladeBase");
            view.bladeTip=right.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket_BladeTip");view.ResetPose();
            int layer=LayerMask.NameToLayer("FirstPersonViewModel");foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;
            BuildBossClips();
            EditorUtility.SetDirty(melee);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            return "Sword/shield meshes, grip hands, materials, 4 skills, equipment page and Warden melee connected.";
        }
        static void BuildBossClips()
        {
            const string path="Assets/Art/ForbiddenWarden/CombatV2/Prefabs/ForbiddenWardenRigged.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var driver=prefab.GetComponent<WardenAnimationDriver>();var animator=driver.animator;
                var source=driver.clips.First(c=>c.name=="AA_Idle");
                var wind=CreateMeleeClip(animator,source,"AA_MeleeWindup",.9f,true);
                var strike=CreateMeleeClip(animator,source,"AA_MeleeStrike",.72f,false);
                driver.clips=driver.clips.Where(c=>c.name!="AA_MeleeWindup" && c.name!="AA_MeleeStrike").Concat(new[]{wind,strike}).ToArray();
                // A blade-tip pair on the rigid staff follows the existing hand/staff hierarchy.
                var staff=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="staff");
                var staffRender=animator.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r=>r.name.ToLowerInvariant().Contains("staff"));
                if(staffRender)staffRender.updateWhenOffscreen=true;
                driver.meleeTrailMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForbiddenWarden/CombatV2/Materials/WardenSpellRibbon.mat") ?? AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SealVioletLight.mat");
                driver.meleeParticleMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SkillSpellMote.mat");
                var bladeBase=staff.Find("MeleeBladeBase");if(!bladeBase)bladeBase=Node("MeleeBladeBase",staff).transform;
                var bladeTip=staff.Find("MeleeBladeTip");if(!bladeTip)bladeTip=Node("MeleeBladeTip",staff).transform;
                bladeBase.position=staff.position+prefab.transform.up*1.03f;bladeTip.position=staff.position+prefab.transform.up*1.88f;
                driver.meleeBladeBase=bladeBase;driver.meleeBladeTip=bladeTip;
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        static void SolveArm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 pole)
        {
            Vector3 root=upper.position,delta=target-root;float a=Vector3.Distance(root,lower.position),b=Vector3.Distance(lower.position,hand.position);
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.01f,a+b-.01f);Vector3 forward=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole,forward).normalized;float along=(a*a-b*b+distance*distance)/(2*distance);
            Vector3 elbow=root+forward*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-root,elbow-root)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
        }
        static AnimationClip CreateMeleeClip(Animator animator,AnimationClip idle,string name,float duration,bool windup)
        {
            string path=Root+"/Animations/"+name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip{name=name,frameRate=30};AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
            // Sample the complete imported pose: every bone keeps the existing bind hierarchy,
            // including rigid hand/staff attachment, fingers, pages and robe chains.
            var bones=animator.GetComponentsInChildren<Transform>().Where(t=>t!=animator.transform).ToArray();
            var curves=new Dictionary<string,AnimationCurve[]>();
            foreach(var b in bones)curves[AnimationUtility.CalculateTransformPath(b,animator.transform)]=Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray();
            int frames=Mathf.CeilToInt(duration*30);
            for(int frame=0;frame<=frames;frame++)
            {
                float u=frame/(float)frames,time=u*duration;idle.SampleAnimation(animator.gameObject,0);
                float twist,arm,forearm,lean;
                if(windup){float w=Mathf.SmoothStep(0,1,u);twist=-42*w;arm=-56*w;forearm=-24*w;lean=-9*w;}
                else
                {
                    float cut=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.48f));float release=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.65f)/.35f));
                    twist=Mathf.Lerp(Mathf.Lerp(-42,54,cut),6,release);arm=Mathf.Lerp(Mathf.Lerp(-56,78,cut),12,release);
                    forearm=Mathf.Lerp(Mathf.Lerp(-24,22,cut),0,release);lean=Mathf.Lerp(Mathf.Lerp(-9,13,cut),2,release);
                }
                foreach(var b in bones)
                {
                    switch(b.name)
                    {
                        case "spine":b.localRotation*=Quaternion.Euler(lean*.35f,twist*.35f,0);break;
                        case "chest":b.localRotation*=Quaternion.Euler(lean,twist,0);break;
                        case "head":b.localRotation*=Quaternion.Euler(-lean*.4f,-twist*.42f,0);break;
                        case "upper_arm.R":b.localRotation*=Quaternion.Euler(-18*Mathf.Sin(u*Mathf.PI),arm*.6f,arm*.55f);break;
                        case "forearm.R":b.localRotation*=Quaternion.Euler(forearm,0,-forearm*.45f);break;
                        case "upper_arm.L":b.localRotation*=Quaternion.Euler(-8,twist*.14f,-twist*.2f);break;
                        case "wing.L":case "wing.R":b.localRotation*=Quaternion.Euler(lean*.2f,0,twist*.12f);break;
                    }
                    if(b.name.StartsWith("robe"))b.localRotation*=Quaternion.Euler(0,0,Mathf.Sin(Mathf.Max(0,u-.12f)*Mathf.PI)*twist*.12f);
                }
                var boss=animator.transform.parent.parent;
                var upper=bones.First(b=>b.name=="upper_arm.R");var lower=bones.First(b=>b.name=="forearm.R");var hand=bones.First(b=>b.name=="hand.R");var staff=bones.First(b=>b.name=="staff");
                float slash=windup?0:Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.54f));
                float settle=windup?0:Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.72f)/.28f));
                Vector3 windHand=new Vector3(1.22f,2.38f,-.18f),endHand=new Vector3(-.50f,1.75f,.86f);
                Vector3 desiredHand=windup?Vector3.Lerp(boss.InverseTransformPoint(hand.position),windHand,Mathf.SmoothStep(0,1,u)):Vector3.Lerp(Vector3.Lerp(windHand,endHand,slash),new Vector3(.80f,2.22f,.40f),settle);
                SolveArm(upper,lower,hand,boss.TransformPoint(desiredHand),boss.right);
                // Rotate the wrist and attached staff together, so the grip stays closed.
                Vector3 windBlade=(Quaternion.AngleAxis(118,Vector3.up)*Vector3.forward+Vector3.up*.14f).normalized;
                // An explicit yaw sweep crosses the player's chest; slerping nearly opposite
                // endpoints can instead arc the blade over the player's head.
                Vector3 sweptBlade=Quaternion.AngleAxis(Mathf.Lerp(118,-70,slash),Vector3.up)*Vector3.forward;
                sweptBlade.y=Mathf.Lerp(.14f,-.10f,slash);
                Vector3 blade=windup?Vector3.Slerp(Vector3.up,windBlade,Mathf.SmoothStep(0,1,u)):Vector3.Slerp(sweptBlade.normalized,new Vector3(.10f,.72f,.68f).normalized,settle);
                hand.rotation=Quaternion.FromToRotation(staff.up,boss.TransformDirection(blade))*hand.rotation;
                foreach(var b in bones)
                {
                    string key=AnimationUtility.CalculateTransformPath(b,animator.transform);var c=curves[key];var pos=b.localPosition;var q=b.localRotation;
                    float[] values={pos.x,pos.y,pos.z,q.x,q.y,q.z,q.w};
                    for(int i=0;i<7;i++)c[i].AddKey(time,values[i]);
                }
            }
            string[] names={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
            foreach(var pair in curves)for(int i=0;i<7;i++)clip.SetCurve(pair.Key,typeof(Transform),names[i],pair.Value[i]);
            clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);idle.SampleAnimation(animator.gameObject,0);return clip;
        }
    }
}
