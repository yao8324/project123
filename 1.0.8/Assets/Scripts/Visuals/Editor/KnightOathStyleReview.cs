using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class KnightOathStyleReview
    {
        const string Key="KnightOathStyle.Review", Output=KnightOathStyleBuilder.Output;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static BossEncounter e;static KnightVisuals visual;static RecoveryMenu menu;static BossTestInputScope input;
        static IEnumerator sequence;static bool running;static double deadline;
        static readonly List<string> checks=new List<string>(),errors=new List<string>(),editorIssues=new List<string>();
        static float maxGrip,maxFeet;
        static KnightOathStyleReview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            Directory.CreateDirectory(Output+"/Captures");
            File.WriteAllText(Output+"/MechanicsBaseline.json",JsonUtility.ToJson(Hashes(),true));
            SessionState.SetString(Key,"before");Open();
        }
        public static void VerifyOnly(){File.Delete(Output+"/Verification.json");SessionState.SetString(Key,"after");Open();}
        public static void RebuildAndVerify(){File.Delete(Output+"/Verification.json");KnightOathStyleBuilder.Build();VerifyOnly();}
        static void Open(){EditorSceneManager.OpenScene(KnightProductionBuilder.TestScene);EditorApplication.EnterPlaymode();}
        static void Changed(PlayModeStateChange state)
        {
            string mode=SessionState.GetString(Key,"");
            if(state==PlayModeStateChange.EnteredPlayMode && (mode=="before" || mode=="after"))
            {
                sequence=null;e=null;visual=null;maxGrip=maxFeet=0;
                running=true;deadline=EditorApplication.timeSinceStartup+240;Application.runInBackground=true;
                checks.Clear();errors.Clear();editorIssues.Clear();Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode && mode=="before-done")EditorApplication.delayCall+=()=>
            {
                try{KnightOathStyleBuilder.Build();SessionState.SetString(Key,"after");Open();}
                catch(Exception ex){File.WriteAllText(Output+"/BuildFailure.txt",ex.ToString());SessionState.SetString(Key,"");EditorApplication.Exit(1);}
            };
            if(state==PlayModeStateChange.EnteredEditMode && mode=="finished")
            {SessionState.SetString(Key,"");int code=SessionState.GetInt(Key+".Code",1);EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
        }
        static void Log(string text,string stack,LogType type)
        {
            if(type!=LogType.Error && type!=LogType.Assert && type!=LogType.Exception)return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase"))editorIssues.Add(text+"\n"+stack);else errors.Add(text+"\n"+stack);
        }
        static void Tick()
        {
            try
            {
                if(!running)return;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Oath style review timed out");
                if(sequence==null){e=Object.FindFirstObjectByType<BossEncounter>();if(!e||!e.IsActive)return;sequence=Review();}
                if(!sequence.MoveNext())Finish(null);
            }
            catch(Exception ex){Finish(ex.GetBaseException().ToString());}
        }
        static void Set(object target,string field,object value){target.GetType().GetField(field,Private).SetValue(target,value);}
        static void Property(object target,string field,object value){Set(target,"<"+field+">k__BackingField",value);}
        static object Call(string method,params object[] args){return typeof(BossEncounter).GetMethod(method,Private).Invoke(e,args);}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        static void Step(float dt,bool combat=true)
        {
            if(e.IsPaused)return;Set(e,"combatTime",e.BattleTime+dt);
            if(combat){e.Combat.Tick(dt);Call("TickKnight",dt);Call("TickShots",dt);}
            Call("PresentBoss",dt);Physics.SyncTransforms();
            maxGrip=Mathf.Max(maxGrip,visual.productionRig.RightGripError);maxFeet=Mathf.Max(maxFeet,visual.productionRig.FootGroundError);
        }
        static void ResetPose()
        {
            Call("ClearAttacks");visual.CancelEffects();Call("SetState",BossEncounter.State.Observe);
            e.Arena.boss.localPosition=new Vector3(0,0,4);e.Arena.boss.localRotation=Quaternion.Euler(0,180,0);
            e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,-5));Physics.SyncTransforms();
            for(int i=0;i<90;i++)Step(1f/60,false);
        }
        static IEnumerator Review()
        {
            bool before=SessionState.GetString(Key,"")=="before";
            visual=e.Arena.boss.GetComponent<KnightVisuals>();menu=e.GetComponent<RecoveryMenu>();
            input=new BossTestInputScope();e.enabled=false;e.GetComponent<KnightTrialLauncher>().enabled=false;
            var player=e.GetComponent<FirstPersonController>();player.enabled=false;player.SetGameplayInputEnabled(false);menu.Resume();Set(e,"focusLost",false);
            Property(e.Combat,"Health",100000f);Property(e,"Phase",1);ResetPose();
            for(int i=0;i<4;i++)yield return null;
            string prefix=before?"Before":"After";
            Capture(prefix+"_Hero",new Vector3(3.6f,3.15f,-2.2f),new Vector3(0,1.8f,4),43);
            Capture(prefix+"_Forecourt",new Vector3(0,5.8f,-11.7f),new Vector3(0,2.6f,4),76);
            Capture(prefix+"_Armor",new Vector3(1.5f,2.95f,.5f),new Vector3(0,2.35f,4),42);
            if(!before)Capture("After_TransferCases",new Vector3(-1.5f,2,-8.2f),new Vector3(-3.3f,.8f,-12.6f),50);
            string signature=Colliders();
            if(before){File.WriteAllText(Output+"/ColliderBaseline.txt",signature);e.Finish(false);e.ReturnToLibrary();yield break;}
            Check(signature==File.ReadAllText(Output+"/ColliderBaseline.txt"),"All original collider shapes, transforms and enabled flags preserved");
            var baseline=JsonUtility.FromJson<FileHashes>(File.ReadAllText(Output+"/MechanicsBaseline.json"));var current=Hashes();
            Check(baseline.paths.SequenceEqual(current.paths)&&baseline.hashes.SequenceEqual(current.hashes),"Combat scripts, configuration, input and package locks are byte-identical to baseline");
            Check(e.KnightMode && visual.oathPresentation,"Actual production encounter loads the styled Knight");
            Check(e.Arena.GetComponentsInChildren<Light>().Length==3,"Three realtime lights retained");
            Check(visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==2,"Two existing skinned LOD renderers retained");
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Check(skin.bones.Length==100 && skin.sharedMesh.bindposes.Length==100,skin.name+": original 100-bone binding retained");
                Check(skin.sharedMesh.boneWeights.Length==skin.sharedMesh.vertexCount && skin.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f),skin.name+": normalized armor weights");
                Check(skin.sharedMesh.triangles.Length/3<15000,skin.name+": geometry below 15000 triangles");
                Check(skin.sharedMesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)),skin.name+": finite mesh positions");
            }
            var styleGeometry=e.Arena.transform.Find("Oath architecture");Check(styleGeometry && styleGeometry.GetComponentsInChildren<Collider>().Length==0,"Vault, floor inlay and evacuation cases add no collision obstacles");
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);
            var moves=config.moves.Where(m=>config.IsMoveEnabled(m.move)).Select(m=>m.move).Distinct().ToArray();Check(moves.Length==6,"Current six-move kit preserved");
            var cold=ReadSeal();Property(e,"Phase",2);for(int i=0;i<120;i++)Step(1f/60,false);var warm=ReadSeal();
            Check(warm.maxColorComponent>cold.maxColorComponent && visual.oathPresentation.PhaseWeight>.99f,"Phase two warms the quiet crest using encounter time");
            float clock=e.BattleTime;float weight=visual.oathPresentation.PhaseWeight;var color=ReadSeal();
            menu.ShowPage(RecoveryMenu.Page.Menu);Call("Update");visual.oathPresentation.Present(e);
            Check(e.IsPaused && e.BattleTime==clock && visual.oathPresentation.PhaseWeight==weight && ReadSeal()==color,"Pause freezes crest brightness and phase blend");menu.Resume();Set(e,"focusLost",false);
            for(int phase=1;phase<=2;phase++)foreach(var move in moves)
            {
                if(phase==1 && move==KnightMove.Heavenfall)continue;
                Property(e,"Phase",phase);ResetPose();e.BeginKnightMove(move);int frame=0;
                while(e.CurrentState!=BossEncounter.State.Recovery && frame++<600)
                {
                    Step(1f/60);
                    if((move==KnightMove.LeapCleave||move==KnightMove.BlinkBarrage||move==KnightMove.Heavenfall)&&e.CurrentState==BossEncounter.State.Attack && e.AttackElapsed>.22f && e.AttackElapsed<.25f)
                        Capture("After_P"+phase+"_"+move,new Vector3(-5,4.4f,-8),new Vector3(0,1.4f,3),72);
                    if(frame%15==0)yield return null;
                }
                Check(e.CurrentState==BossEncounter.State.Recovery,"Actual six-move presentation reaches recovery: phase "+phase+" "+move);
            }
            Check(maxGrip<.03f,"Styled armor preserves right-hand grip within 3 cm across both phases");
            Check(visual.sealRibbons.Length==12 && visual.productionRig.ribbons.All(r=>r.bones.Length==6),"Twelve six-segment ribbon chains retained");
            e.Finish(false);Check(e.ActiveAttackCount==0,"Defeat clears all hazards");var ambient=e.Arena.GetComponent<KnightArenaPresentation>().OriginalSky;
            Check(e.ReturnToLibrary(),"Return to library succeeds");yield return null;
            Check(RenderSettings.ambientSkyColor==ambient,"Library ambient light restored after leaving the styled arena");
            Check(e.BeginKnight(config),"Styled Knight can be entered again");yield return null;
            Check(e.Arena.boss.GetComponent<KnightOathPresentation>().PhaseWeight==0,"Re-entry resets heraldic phase state");e.Finish(false);Check(e.ReturnToLibrary(),"Second encounter cleans up normally");
        }
        static Color ReadSeal()
        {var target=visual.oathPresentation.seals[0];var b=new MaterialPropertyBlock();target.renderer.GetPropertyBlock(b,target.materialIndex);return b.GetColor("_EmissionColor");}
        static string Colliders()
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(KnightProductionBuilder.Root+"/KnightArenaProduction.prefab");
            return string.Join("\n",asset.GetComponentsInChildren<Collider>(true).Select(c=>
            {
                string shape=c is BoxCollider b?b.center.ToString("F5")+b.size.ToString("F5"):c is CapsuleCollider p?p.center.ToString("F5")+p.radius+"/"+p.height+"/"+p.direction:c is SphereCollider s?s.center.ToString("F5")+s.radius:c is MeshCollider m?AssetDatabase.GetAssetPath(m.sharedMesh):c.GetType().Name;
                return AnimationUtility.CalculateTransformPath(c.transform,asset.transform)+"|"+c.GetType().Name+"|"+c.enabled+"|"+c.isTrigger+"|"+c.transform.localPosition.ToString("F5")+"|"+c.transform.localRotation.ToString("F5")+"|"+c.transform.lossyScale.ToString("F5")+"|"+shape;
            }).OrderBy(s=>s));
        }
        [Serializable] class FileHashes{public string[] paths,hashes;}
        static FileHashes Hashes()
        {
            var paths=Directory.GetFiles("Assets/Scripts/Core","*.cs",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Scripts/Player","*.cs",SearchOption.AllDirectories))
                .Concat(new[]{KnightProductionBuilder.Config,"Assets/Scripts/UI/RecoveryMenu.cs","Assets/Scripts/UI/BossCombatHUD.cs","Packages/manifest.json","Packages/packages-lock.json"}).OrderBy(s=>s).ToArray();
            using(var sha=SHA256.Create())return new FileHashes{paths=paths,hashes=paths.Select(p=>BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","")).ToArray()};
        }
        static void Capture(string name,Vector3 position,Vector3 target,float fov)
        {
            var go=new GameObject("Oath review camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.fieldOfView=fov;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.028f,.032f,.045f);
            camera.transform.position=e.Arena.transform.TransformPoint(position);camera.transform.LookAt(e.Arena.transform.TransformPoint(target));go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Output+"/Captures/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(go);}
        }
        [Serializable] class Report{public bool complete;public string scope,unity;public string[] checks,errors,editorIssues;public float maximumGripError,maximumFootError;}
        static void Finish(string failure)
        {
            running=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;input?.Dispose();input=null;
            if(failure!=null)errors.Add(failure);bool before=SessionState.GetString(Key,"")=="before";
            if(!before && checks.Count<25 && failure==null)errors.Add("After-style checks did not execute completely");
            File.WriteAllText(Output+(before?"/BaselineCapture.json":"/Verification.json"),JsonUtility.ToJson(new Report{complete=errors.Count==0,scope="Real production scene, explicit high test health and battle-clock stepping. No human combat, aesthetic or device performance claim.",unity=Application.unityVersion,checks=checks.ToArray(),errors=errors.ToArray(),editorIssues=editorIssues.ToArray(),maximumGripError=maxGrip,maximumFootError=maxFeet},true));
            SessionState.SetInt(Key+".Code",errors.Count==0?0:1);SessionState.SetString(Key,before&&errors.Count==0?"before-done":"finished");EditorApplication.ExitPlaymode();
        }
    }
}
