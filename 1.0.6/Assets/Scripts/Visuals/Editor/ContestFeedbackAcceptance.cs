using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Contest;
using ArcaneArchive.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class ContestFeedbackAcceptance
    {
        const string Flag="IMMonster.FeedbackAcceptance";
        const string Out="Docs/Feedback20261104";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static ContestStoryGame game;
        static IEnumerator<float> sequence;
        static readonly List<string> checks=new List<string>();
        static readonly List<string> errors=new List<string>();
        static float next;
        static bool running;
        static string mode;
        [Serializable] sealed class Report { public bool complete;public string mode,failure,unity;public string[] checks,errors; }
        static ContestFeedbackAcceptance(){EditorApplication.update+=Update;}
        public static void RunReticle()=>Run("reticle");
        public static void RunAudio()=>Run("audio");
        public static void RunAll()=>Run("all");
        static void Run(string value)
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch Unity project for these checks.");
            Directory.CreateDirectory(Out+"/Captures");
            EditorSceneManager.OpenScene("Assets/Scenes/IMMonsterContest20261107.unity");
            mode=value;SessionState.SetString(Flag+".Mode",value);SessionState.SetBool(Flag,true);
            SessionState.SetFloat(Flag+".Deadline",(float)EditorApplication.timeSinceStartup+180);
            Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        static void Update()
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Flag+".Deadline",0)){Finish("Feedback checks timed out");return;}
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if(!running)
                {
                    game=Object.FindFirstObjectByType<ContestStoryGame>();
                    if(!game || !game.Hud || game.player.Bindings==null)return;
                    mode=SessionState.GetString(Flag+".Mode","all");checks.Clear();errors.Clear();
                    Application.logMessageReceived+=Log;sequence=Sequence();next=0;running=true;
                }
                if(Time.realtimeSinceStartup<next)return;
                if(!sequence.MoveNext()){Finish(null);return;}
                next=Time.realtimeSinceStartup+sequence.Current;
            }
            catch(Exception e){Finish(e.ToString());}
        }
        static void Log(string text,string trace,LogType type)
        {if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search.SearchDatabase") && !text.StartsWith("[FeedbackCheck]"))errors.Add(text);}
        static void Check(bool pass,string label)
        {if(!pass)throw new Exception(label);checks.Add(label);Debug.Log("[FeedbackCheck] PASS "+label);}
        static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
        static object Feedback(string id)
        {
            var method=game.GetType().GetMethod("GetInteractionFeedback");
            Check(method!=null,"One shared interaction policy is exposed to prompts and activation");
            return method.Invoke(game,new object[]{id});
        }
        static void Allowed(string id,bool expected,string label)
        {Check((bool)Property(Feedback(id),"Available")==expected,label);}
        static void Explore()=>game.GetType().GetMethod("Explore",Private).Invoke(game,null);
        static void Aim(Vector3 point)
        {
            var rotation=Quaternion.LookRotation(point-game.view.transform.position);
            game.player.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",Private).SetValue(game.player,pitch);
            game.cameraPivot.localRotation=Quaternion.Euler(pitch,0,0);
        }
        static ContestWorldTarget Target(string id)=>Object.FindObjectsByType<ContestWorldTarget>(FindObjectsSortMode.None).Single(t=>t.id==id);
        static IEnumerator<float> Sequence()
        {
            yield return .3f;
            game.NewGame(true);yield return .3f;
            while(game.Mode==ContestStoryGame.ScreenMode.Dialogue){game.AdvanceDialogue();yield return .2f;}
            if(mode!="audio")
            {
                var reticle=game.Hud.GetComponentsInChildren<Graphic>(true).FirstOrDefault(g=>g.GetType().Name=="ContestReticle");
                Check(reticle && reticle.gameObject.activeInHierarchy,"Persistent reticle uses vector geometry instead of a font glyph");
                Check(!reticle.raycastTarget,"Reticle cannot intercept UI clicks");
                Aim(game.view.transform.position+Vector3.up*5);yield return .3f;
                Check(reticle.gameObject.activeInHierarchy,"Reticle stays visible when looking away from props");
                ScreenCapture.CaptureScreenshot(Out+"/Captures/01_neutral.png");yield return .25f;
                var record=Target("ARCHIVE_RECORD_SLOT_02");Aim(record.Point);yield return .25f;
                Check(game.FindTarget()==record,"Actual scene ray reaches the aimed archive");
                Allowed(record.id,true,"Unverified archive is available");
                Check((bool)Property(reticle,"Ready"),"Ready ring matches actual archive interaction");
                ScreenCapture.CaptureScreenshot(Out+"/Captures/02_ready.png");yield return .25f;
                game.Activate(record);yield return .25f;
                Check(game.Mode==ContestStoryGame.ScreenMode.Reader && !reticle.gameObject.activeInHierarchy,"Reader uses pointer and hides reticle");
                game.Hud.FindButton("ReaderClose").onClick.Invoke();yield return .3f;
                game.player.TryReturnToSpawn(new Vector3(6,.08f,1.4f),Quaternion.identity);
                var door=Target("ARCHIVE_TO_MAINTENANCE");Aim(door.Point);yield return .3f;
                Allowed(door.id,false,"Story prerequisites block the maintenance entrance");
                Check(!string.IsNullOrEmpty((string)Property(Feedback(door.id),"Reason")),"Blocked interaction gives a reason");
                Check((bool)Property(reticle,"Blocked"),"Blocked ring does not claim the door is ready");
                ScreenCapture.CaptureScreenshot(Out+"/Captures/03_blocked.png");yield return .25f;
                game.Activate(door);yield return .2f;
                Check(game.State.stage==0 && game.Mode==ContestStoryGame.ScreenMode.Explore,"Rejected door activation cannot advance story");
                game.State.sealedBatch=true;Allowed("ARCHIVE_TIE_CORD",false,"Already tied cord is completed, not actionable");
                game.State.stage=3;game.State.nodes=0;
                Allowed("MAINT_NODE_01",true,"First maintenance node is actionable");
                Allowed("MAINT_NODE_02",false,"Second node remains blocked until first finishes");
                game.State.nodes=1;
                Allowed("MAINT_NODE_01",false,"Completed node cannot signal ready");
                Allowed("MAINT_NODE_02",true,"Next node becomes actionable");
                game.State.stage=6;game.State.rescued=false;game.State.batchLocation=1;
                Allowed("ARCHIVE_TRANSFER_CASE",false,"Transfer requires actually carrying the bundle");
                game.State.rescued=true;Allowed("REVEAL_CORD",true,"Rescued bundle can be picked up");
                game.State.batchLocation=2;Allowed("ARCHIVE_TRANSFER_CASE",true,"Carried bundle unlocks transfer");
                Allowed("REVEAL_CORD",false,"Picked-up bundle cannot be collected again");
                string error;Check(game.player.Bindings.TryAssign("Interact","<Keyboard>/j",false,out error),"Interaction can be rebound");
                Check(game.Prompt("ARCHIVE_TRANSFER_CASE").Contains("J"),"Prompt follows the rebound interaction key");
                game.player.Bindings.RestoreDefaults();
                // Controlled geometry uses the real physics probe and production player dimensions.
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(500,-.1f,500);floor.transform.localScale=new Vector3(10,.2f,10);
                Physics.SyncTransforms();Check(game.player.TryReturnToSpawn(new Vector3(500,.08f,500),Quaternion.identity),"Physics fixture safely places the real player");
                game.State.stage=0;game.State.sealedBatch=false;
                var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.transform.localScale=Vector3.one*.15f;
                prop.transform.position=game.view.transform.position+Vector3.forward;
                var target=prop.AddComponent<ContestWorldTarget>();target.id="ARCHIVE_RECORD_SLOT_01";Aim(prop.transform.position);Physics.SyncTransforms();
                Check(game.FindTarget()==target,"Nearby prop is reachable through production ray");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=(game.view.transform.position+prop.transform.position)/2;wall.transform.localScale=Vector3.one*.2f;Physics.SyncTransforms();
                Check(game.FindTarget()==null,"A solid wall blocks interaction");Object.DestroyImmediate(wall);
                prop.transform.position=game.view.transform.position+Vector3.forward*3;Physics.SyncTransforms();
                Check(game.FindTarget()==null,"Out-of-range prop cannot activate");
                Object.DestroyImmediate(prop);Object.DestroyImmediate(floor);
                game.NewGame(true);yield return .3f;Explore();yield return .2f;
            }
            if(mode!="reticle")
            {
                var director=game.GetComponent("ContestAudioDirector");
                Check(director,"Contextual audio director replaces generic beep and drone");
                foreach(string clipName in new[]{"PaperOpen","PaperClose","Cloth","Hardware","Node","Door","Ui","PowerStart","PowerSuccess","Overload","Bundle","Transfer","Impact","Alarm","Calm","Tension","Maintenance","Restored","Investigation","Rescue","Ending","PowerLoop"})
                {
                    var clip=Resources.Load<AudioClip>("ContestAudio/"+clipName);
                    Check(clip && clip.length>.02f,"Bundled clip loads: "+clipName);
                    var data=new float[Mathf.Min(clip.samples*clip.channels,44100)];clip.GetData(data,0);
                    Check(data.Any(x=>Mathf.Abs(x)>.0001f),"Clip has actual audio signal: "+clipName);
                }
                game.State.stage=0;Explore();yield return 1.4f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Calm","Opening uses calm room ambience");
                game.State.stage=3;game.hall.SetActive(true);game.library.SetActive(false);game.State.nodes=0;yield return .3f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Maintenance","Unstable hall has a distinct ambience");
                game.State.nodes=3;yield return .3f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Restored","Restored nodes change hall ambience");
                game.State.stage=4;game.hall.SetActive(false);game.library.SetActive(true);yield return .3f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Investigation","Returned archive uses investigation ambience");
                game.State.stage=6;yield return 1.4f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Rescue","Rescue raises tension");
                game.Pause();yield return .2f;Check(AudioListener.pause,"Pause freezes environment audio");
                var pausedTimes=director.GetComponentsInChildren<AudioSource>().Where(s=>s.loop).Select(s=>s.timeSamples).ToArray();yield return .5f;
                Check(pausedTimes.SequenceEqual(director.GetComponentsInChildren<AudioSource>().Where(s=>s.loop).Select(s=>s.timeSamples)),"Loop playback positions remain frozen while paused");
                game.Resume();yield return .2f;Check(!AudioListener.pause,"Resume restores environment audio");
                var effectProp=game.GetType().GetProperty("EffectsVolume");var envProp=game.GetType().GetProperty("EnvironmentVolume");
                Check(effectProp!=null && envProp!=null,"Settings expose independent effects and environment buses");
                game.GetType().GetMethod("SetEffectsVolume").Invoke(game,new object[]{0f});
                game.GetType().GetMethod("SetEnvironmentVolume").Invoke(game,new object[]{0f});yield return .2f;
                Check(director.GetComponentsInChildren<AudioSource>().All(s=>s.volume==0),"Both buses mute actual sources at zero");
                game.GetType().GetMethod("SetEffectsVolume").Invoke(game,new object[]{float.NaN});
                game.GetType().GetMethod("SetEnvironmentVolume").Invoke(game,new object[]{2f});
                Check(!float.IsNaN((float)effectProp.GetValue(game)) && (float)envProp.GetValue(game)==1,"Invalid bus settings are sanitized");
                game.GetType().GetMethod("SetEffectsVolume").Invoke(game,new object[]{.75f});
                game.GetType().GetMethod("SetEnvironmentVolume").Invoke(game,new object[]{.65f});
                game.State.stage=7;yield return .2f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Ending","Completed story gets relieved ambience");
                game.ShowMainMenu();yield return .2f;
                Check((string)Property(director,"CurrentAmbienceKey")=="Calm","Returning to menu clears stale story ambience");
            }
            Check(errors.Count==0,"No gameplay runtime errors in targeted feedback checks");
        }
        static void Finish(string failure)
        {
            Application.logMessageReceived-=Log;
            var report=new Report{complete=failure==null,mode=mode,failure=failure??"",unity=Application.unityVersion,checks=checks.ToArray(),errors=errors.ToArray()};
            File.WriteAllText(Out+"/"+mode+"-checks.json",JsonUtility.ToJson(report,true));
            SessionState.SetBool(Flag,false);EditorApplication.isPlaying=false;
            if(failure!=null)Debug.LogError("[FeedbackCheck] "+failure);
            EditorApplication.delayCall+=()=>EditorApplication.Exit(failure==null?0:1);
        }
    }
}
