using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class KnightSanctuaryAcceptance
    {
        const string Key="KnightSanctuary.Acceptance";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static BossEncounter e;static IEnumerator sequence;static double deadline;static BossTestInputScope input;
        static AmbientMode beforeMode;static Color beforeSky;static RenderPipelineAsset beforePipeline;
        [Serializable] class Report{public bool complete;public string failure,scope;public string[] checks,errors;public int renderers,triangles,lights;}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        static string Signature(BossArena a)
        {
            var art=a.transform.Find("Sanctuary architecture");
            return string.Join("\n",a.GetComponentsInChildren<Collider>(true).Where(c=>!art || !c.transform.IsChildOf(art)).Select(c=>{
                string shape=c is BoxCollider b?b.center.ToString("F5")+b.size.ToString("F5"):c is CapsuleCollider cap?cap.center.ToString("F5")+cap.radius+"/"+cap.height+"/"+cap.direction:c is MeshCollider mesh?AssetDatabase.GetAssetPath(mesh.sharedMesh):"";
                return c.name+"|"+c.GetType().Name+"|"+c.enabled+"|"+c.isTrigger+"|"+a.transform.InverseTransformPoint(c.transform.position).ToString("F5")+"|"+c.transform.localRotation.ToString("F5")+"|"+c.transform.lossyScale.ToString("F5")+"|"+shape;
            }).OrderBy(x=>x));
        }
        public static void ValidateAssets()
        {
            Directory.CreateDirectory(KnightSanctuaryBuilder.Output);checks.Clear();
            File.WriteAllText(KnightSanctuaryBuilder.Output+"/AssetValidation.json",JsonUtility.ToJson(new Report{complete=false,failure="Validation started; final success not yet recorded"},true));
            var a=AssetDatabase.LoadAssetAtPath<GameObject>(KnightSanctuaryBuilder.Arena);
            Check(a,"Sanctuary arena asset exists");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Bosses/Knight/ProductionV5/KnightArenaProduction.prefab");
            var runtime=a.GetComponent<BossArena>();
            Check(Signature(runtime)==Signature(source.GetComponent<BossArena>()),"All original collider shapes and local positions preserved");
            Check(a.GetComponentsInChildren<Collider>(true).Length==source.GetComponentsInChildren<Collider>(true).Length+5,"Five solid prop colliders added outside circulation ring");
            Check(!a.transform.Find("Sanctuary architecture/Arcade bay 15").GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Recorded service volume"),"Evacuation bay is clear of intersecting fixed shelving");
            var grandVault=a.transform.Find("Sanctuary architecture/Five-bay pointed crossing vault");
            Check(grandVault && grandVault.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh.bounds.max.y>23),"Revision 2 pointed nave vault exceeds 23m");
            Check(a.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Monumental bundled pier")==10,"Ten monumental bundled piers support five main arches");
            var piers=a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Monumental bundled pier").ToArray();
            foreach(var bay in a.transform.Find("Sanctuary architecture").Cast<Transform>().Where(t=>t.name.StartsWith("Arcade bay ")))
                foreach(var shaft in bay.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Carved clustered shaft"))
                    Check(piers.All(pier=>Vector2.Distance(new Vector2(shaft.position.x,shaft.position.z),new Vector2(pier.position.x,pier.position.z))>1.05f),"Arcade colonette is clear of monumental pier: "+bay.name);
            Check(runtime.radius==14 && runtime.spawn && runtime.boss && runtime.effectRoot,"Arena radius, player, boss and effect anchors valid");
            Check(a.GetComponent<KnightSanctuaryLighting>().pipeline,"Independent shadow-enabled rendering pipeline assigned");
            foreach(var r in a.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled)){
                Check(r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported),r.name+": valid supported materials");
            }
            foreach(var f in a.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled)){
                Check(f.sharedMesh && f.sharedMesh.vertexCount>0,f.name+": persistent mesh");
                Check(!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh)),f.name+": saved mesh reference");
            }
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightSanctuaryBuilder.Config);
            Check(config&&config.arena==runtime,"New encounter config references sanctuary prefab");
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(KnightSanctuaryBuilder.Scene),"Playable scene saved");
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(KnightSanctuaryBuilder.ArtScene),"Editable art scene saved");
            File.WriteAllText(KnightSanctuaryBuilder.Output+"/AssetValidation.json",JsonUtility.ToJson(new Report{complete=true,checks=checks.ToArray(),errors=Array.Empty<string>()},true));
        }
        static KnightSanctuaryAcceptance(){EditorApplication.playModeStateChanged+=Changed;}
        public static void BuildAndReview(){KnightSanctuaryBuilder.Build();Run();}
        public static void Run()
        {
            File.WriteAllText(KnightSanctuaryBuilder.Output+"/RuntimeValidation.json",JsonUtility.ToJson(new Report{complete=false,failure="Validation started; final success not yet recorded"},true));
            ValidateAssets();SessionState.SetBool(Key+".Art",false);SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Exit",1);
            EditorSceneManager.OpenScene(KnightSanctuaryBuilder.Scene);
            beforeMode=RenderSettings.ambientMode;beforeSky=RenderSettings.ambientSkyColor;beforePipeline=QualitySettings.renderPipeline;
            SessionState.SetString(Key+".QualityPipeline",AssetDatabase.GetAssetPath(QualitySettings.renderPipeline));
            SessionState.SetString(Key+".GraphicsPipeline",AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline));
            SessionState.SetInt(Key+".Mode",(int)beforeMode);SessionState.SetVector3(Key+".Sky",new Vector3(beforeSky.r,beforeSky.g,beforeSky.b));
            EditorApplication.EnterPlaymode();
        }
        public static void RunArt()
        {
            File.WriteAllText(KnightSanctuaryBuilder.Output+"/ArtValidation.json",JsonUtility.ToJson(new Report{complete=false,failure="Validation started; final success not yet recorded"},true));
            ValidateAssets();SessionState.SetBool(Key+".Art",true);SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Exit",1);
            EditorSceneManager.OpenScene(KnightSanctuaryBuilder.ArtScene);EditorApplication.EnterPlaymode();
        }
        static IEnumerator ReviewArtScene()
        {
            var arena=Object.FindFirstObjectByType<BossArena>();
            Check(arena && arena.GetComponent<KnightSanctuaryLighting>(),"Editable art scene contains sanctuary prefab");
            Check(Camera.main,"Preview camera exists");
            for(int frame=0;frame<60;frame++)yield return null;
            Check(QualitySettings.renderPipeline==arena.GetComponent<KnightSanctuaryLighting>().pipeline,"Art preview uses sanctuary pipeline in Play");
            Check(arena.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Art hierarchy has no missing scripts");
            SaveCamera(Camera.main,"09_ArtScene");
            Check(errors.Count==0,"Art preview has no runtime errors");
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){sequence=null;e=null;errors.Clear();checks.Clear();deadline=EditorApplication.timeSinceStartup+240;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.delayCall+=()=>EditorApplication.Exit(SessionState.GetInt(Key+".Exit",1));}
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){if(!stack.Contains("UnityEditor.Search"))errors.Add(text+"\n"+stack);}}
        static void Tick()
        {
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Sanctuary review timed out");
                if(sequence==null){if(SessionState.GetBool(Key+".Art",false))sequence=ReviewArtScene();else{e=Object.FindFirstObjectByType<BossEncounter>();if(!e||!e.IsActive)return;sequence=Review();}}
                if(!sequence.MoveNext())Finish(null);
            }catch(Exception ex){Finish(ex.GetBaseException().ToString());}
        }
        static void Set(string field,object value){typeof(BossEncounter).GetField(field,Private).SetValue(e,value);}
        static void Prop(string property,object value){Set("<"+property+">k__BackingField",value);}
        static object Call(string method,params object[] args){return typeof(BossEncounter).GetMethod(method,Private).Invoke(e,args);}
        static void Step(float dt)
        {
            Set("combatTime",e.BattleTime+dt);e.Combat.Tick(dt);Call("TickKnight",dt);Call("TickShots",dt);Call("PresentBoss",dt);
            e.Arena.GetComponent<KnightSanctuaryLighting>().Present(e.Phase,e.BattleTime);Physics.SyncTransforms();
        }
        static void Reset()
        {
            Call("ClearAttacks");e.Arena.boss.localPosition=new Vector3(0,0,4);e.Arena.boss.localRotation=Quaternion.Euler(0,180,0);
            e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,-5));Call("SetState",BossEncounter.State.Observe);Physics.SyncTransforms();
        }
        static IEnumerator Review()
        {
            input=new BossTestInputScope();e.enabled=false;
            e.GetComponent<FirstPersonController>().enabled=false;e.GetComponent<KnightTrialLauncher>().enabled=false;
            e.GetComponent<RecoveryMenu>().Resume();Set("focusLost",false);
            typeof(PlayerCombat).GetField("<Health>k__BackingField",Private).SetValue(e.Combat,100000f);
            Check(e.KnightMode,"Actual test scene starts knight combat with original player");
            Check(e.Arena.GetComponent<KnightSanctuaryLighting>(),"New sanctuary is instantiated by actual BossEncounter");
            for(int i=0;i<90;i++){Step(1/60f);if(i%10==0)yield return null;}
            Reset();
            Capture("01_Entrance",new Vector3(0,2.05f,-11.8f),new Vector3(0,7.8f,11),75);
            Capture("02_Gate",new Vector3(-4.2f,2.2f,4),new Vector3(0,9.5f,13.4f),72);
            Capture("03_Arcade",new Vector3(4,2.2f,-5),new Vector3(13,8,1),75);
            Capture("04_Evacuation",new Vector3(-7,2.1f,-7.4f),new Vector3(-9.1f,1,-11.2f),63);
            Capture("05_Vault",new Vector3(0,3,-8),new Vector3(0,20.1f,3),90);
            CaptureView("06_PlayerView");
            Capture("15_Marble_Gold_Capital",new Vector3(-10.2f,8.2f,-6),new Vector3(-13.7f,9.0f,-5.88f),50);
            Capture("16_Marble_Pier_Base",new Vector3(-11.5f,1.3f,-4.8f),new Vector3(-13.8f,1.2f,-5.8f),55);
            Capture("17_Reference_Proportions",new Vector3(-3,2.3f,-11),new Vector3(0,5.6f,12),69);
            Capture("12_Comparison_LockedCamera",new Vector3(0,2.05f,-11.8f),new Vector3(0,4.6f,11),67);
            Capture("13_Pier_Gallery_Detail",new Vector3(-6,3,-6),new Vector3(-13,11.5f,0),69);
            Capture("14_Grand_Hall",new Vector3(-8,2.2f,-10),new Vector3(1,8.5f,12),75);
            int clear=0;
            for(int i=0;i<48;i++){
                float a=i*2*Mathf.PI/48;var p=e.Arena.transform.position+new Vector3(Mathf.Sin(a)*12.5f,0,Mathf.Cos(a)*12.5f);
                var hits=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.32f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                Check(!hits.Any(c=>!c.transform.IsChildOf(e.transform)&&!c.transform.IsChildOf(e.Arena.boss)),"12.5m circulation clear at sample "+i);clear++;
            }
            var cc=e.GetComponent<CharacterController>();
            cc.enabled=false;e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,12.5f));cc.enabled=true;Physics.SyncTransforms();
            for(int i=1;i<=192;i++){
                float angle=i*2*Mathf.PI/192;var target=e.Arena.transform.TransformPoint(new Vector3(Mathf.Sin(angle)*12.5f,0,Mathf.Cos(angle)*12.5f));
                var delta=Vector3.ProjectOnPlane(target-e.transform.position,Vector3.up);
                var flags=cc.Move(delta+Vector3.down*.03f);
                Check(Vector3.ProjectOnPlane(e.transform.position-target,Vector3.up).magnitude<.12f && (flags&CollisionFlags.Sides)==0,"CharacterController completes near-wall traversal step "+i);
                if(i%24==0)yield return null;
            }
            Reset();
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightSanctuaryBuilder.Config);
            foreach(int phase in new[]{1,2}){
                Prop("Phase",phase);
                foreach(var move in config.moves.Where(m=>config.IsMoveEnabled(m.move)).Select(m=>m.move).Distinct()){
                    if(phase==1&&move==KnightMove.Heavenfall)continue;
                    Reset();e.BeginKnightMove(move);
                    for(int frame=0;frame<610;frame++){
                        Step(1/60f);
                        if(frame==35 && phase==2 && move==KnightMove.BlinkBarrage){Capture("07_Phase2_Barrage",new Vector3(-4,2.2f,-8),new Vector3(0,2.8f,6),72);CaptureView("10_Player_Phase2_Barrage");}
                        if(frame==92 && phase==2 && move==KnightMove.LeapCleave){Capture("08_Phase2_Leap",new Vector3(-4,2.2f,-8),new Vector3(0,2.3f,3),72);CaptureView("11_Player_Phase2_Leap");}
                        if(frame%90==0)yield return null;
                    }
                    Check(float.IsFinite(e.Arena.boss.position.x)&&Vector3.ProjectOnPlane(e.Arena.boss.position-e.Arena.transform.position,Vector3.up).magnitude<14,"Phase "+phase+" "+move+" runs within arena");
                    Check(e.Combat.Health>0,"Player remains active during controlled move checks");
                }
            }
            var lighting=e.Arena.GetComponent<KnightSanctuaryLighting>();lighting.Present(2,e.BattleTime+3);
            float pressure=lighting.Pressure;lighting.Present(1,e.BattleTime+3);Check(Mathf.Abs(pressure-lighting.Pressure)<.00001f,"Presentation holds when battle clock is paused");
            lighting.Present(1,0);Check(lighting.Pressure==0,"Presentation resets for retry clock");
            e.Finish(false);yield return null;e.Retry();yield return null;
            Check(e.IsActive&&!e.IsFinished&&e.Phase==1,"Defeat and retry restore battle");
            var mode=(AmbientMode)SessionState.GetInt(Key+".Mode",0);var sky=SessionState.GetVector3(Key+".Sky",Vector3.zero);
            Check(e.ReturnToLibrary(),"Return to library succeeds");yield return null;
            Check(RenderSettings.ambientMode==mode && Vector3.Distance(new Vector3(RenderSettings.ambientSkyColor.r,RenderSettings.ambientSkyColor.g,RenderSettings.ambientSkyColor.b),sky)<.001f,"Library ambient lighting restored");
            Check(AssetDatabase.GetAssetPath(QualitySettings.renderPipeline)==SessionState.GetString(Key+".QualityPipeline","") && AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline)==SessionState.GetString(Key+".GraphicsPipeline",""),"Original quality and graphics pipelines restored");
            Check(e.BeginKnight(config),"Can re-enter sanctuary after return");yield return null;
            Check(e.ActiveAttackCount==0,"New encounter has no stale attacks");
            Check(errors.Count==0,"No runtime errors during sanctuary acceptance");
        }
        public static void Capture(string name,Vector3 pos,Vector3 target,float fov)
        {
            var g=new GameObject("Sanctuary documentation camera");var c=g.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.08f;c.farClipPlane=90;c.fieldOfView=fov;c.allowHDR=true;
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.16f,.18f,.22f);
            c.transform.position=e.Arena.transform.TransformPoint(pos);c.transform.LookAt(e.Arena.transform.TransformPoint(target));
            c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var playerRenderers=e.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            try{foreach(var r in playerRenderers)r.enabled=false;SaveCamera(c,name);}
            finally{foreach(var r in playerRenderers)if(r)r.enabled=true;Object.DestroyImmediate(g);}
        }
        static void CaptureView(string name){SaveCamera(e.View,name);}
        static void SaveCamera(Camera camera,string name)
        {
            Directory.CreateDirectory(KnightSanctuaryBuilder.Output+"/Captures");
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,2);
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(KnightSanctuaryBuilder.Output+"/Captures/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        }
        static void Finish(string failure)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;if(input!=null){input.Dispose();input=null;}
            var report=new Report{complete=failure==null&&errors.Count==0,failure=failure,scope=SessionState.GetBool(Key+".Art",false)?"Editable art scene in Play mode with real Unity rendering; no combat simulation. Triangle metric includes MeshFilter geometry only.":"Actual production player/encounter, high health and controlled clock stepping plus CharacterController traversal. Real Unity rendering; not human difficulty acceptance. Triangle metric includes MeshFilter geometry only.",checks=checks.ToArray(),errors=errors.ToArray()};
            var metricsArena=e&&e.Arena?e.Arena:Object.FindFirstObjectByType<BossArena>();
            if(metricsArena){report.renderers=metricsArena.GetComponentsInChildren<Renderer>().Count(r=>r.enabled);report.lights=metricsArena.GetComponentsInChildren<Light>().Length;report.triangles=metricsArena.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).Sum(f=>f.sharedMesh.triangles.Length/3);}
            File.WriteAllText(KnightSanctuaryBuilder.Output+(SessionState.GetBool(Key+".Art",false)?"/ArtValidation.json":"/RuntimeValidation.json"),JsonUtility.ToJson(report,true));
            SessionState.SetInt(Key+".Exit",report.complete?0:1);EditorApplication.ExitPlaymode();
        }
    }
}
