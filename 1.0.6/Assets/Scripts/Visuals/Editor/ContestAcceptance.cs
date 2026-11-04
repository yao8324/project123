using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using ArcaneArchive.Contest;
using ArcaneArchive.Player;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class ContestAcceptance
    {
        const string Flag="IMMonster.ContestAcceptance";
        static string Out=>Environment.GetCommandLineArgs().Contains("-ending-test")?"Logs/Ending20261104/FullRoute":"Docs/Contest20261107/Acceptance";
        static ContestStoryGame game;
        static Keyboard keyboard;static Mouse mouse;static Key[] held=Array.Empty<Key>();
        static InputSettings originalInputSettings,testInputSettings;
        static IEnumerator<Gate> sequence;static Gate gate;
        static bool running,finished;static float startedAt,lastVideoFrame;static int screenshots,videoFrames;static StreamWriter videoTimes;
        static string failure,scenario;
        static readonly List<string> checks=new List<string>(),errors=new List<string>(),editorErrors=new List<string>();
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        sealed class Gate
        {
            public Func<bool> ready;public Action tick;public float deadline;
            public Gate(Func<bool> ready,float seconds=12,Action tick=null){this.ready=ready;this.tick=tick;deadline=Time.realtimeSinceStartup+seconds;}
        }
        [Serializable] sealed class Report
        {
            public bool complete;public string failure,scenario,unity,device,cpu,scope;
            public int checksPassed,stage,pulses,screenshots,videoFrames;public float seconds;public string[] checks,runtimeErrors,editorSearchErrors;
        }
        static ContestAcceptance(){EditorApplication.update+=Update;}
        public static void PrepareAndRun(){ContestSceneBuilder.Prepare();Run();}
        public static void RunAndBuild(){SessionState.SetBool(Flag+".BuildAfter",true);PrepareAndRun();}
        [MenuItem("IM Monster/Run contest acceptance")]
        public static void Run()
        {
            Directory.CreateDirectory(Out+"/Captures");
            EditorSceneManager.OpenScene(ContestSceneBuilder.ScenePath);SessionState.SetBool(Flag,true);SessionState.SetFloat(Flag+".deadline",(float)EditorApplication.timeSinceStartup+480);
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();
            view.maximized=false;view.position=new Rect(20,20,1300,790);
            SessionState.SetBool(Flag+".background",Application.runInBackground);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        static void Update()
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Flag+".deadline",0)){Fail(new Exception("Global acceptance deadline"));return;}
            if(EditorApplication.isPlaying)
            {
                if(EditorApplication.isPaused)EditorApplication.isPaused=false;EditorApplication.QueuePlayerLoopUpdate();
                if(!running && !EditorApplication.isCompiling)
                {
                    game=Object.FindFirstObjectByType<ContestStoryGame>();
                    if(game && game.Hud && game.player.Bindings!=null){running=true;Start();}
                }
                if(running && !finished)Tick();
            }
        }
        static void Start()
        {
            startedAt=Time.realtimeSinceStartup;checks.Clear();errors.Clear();editorErrors.Clear();screenshots=0;failure=null;
            keyboard=InputSystem.AddDevice<Keyboard>("ContestAcceptanceKeyboard");mouse=InputSystem.AddDevice<Mouse>("ContestAcceptanceMouse");keyboard.MakeCurrent();mouse.MakeCurrent();
            var asset=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",Private).GetValue(game.player);
            asset.devices=new InputDevice[]{keyboard,mouse};asset.RemoveAllBindingOverrides();
            originalInputSettings=InputSystem.settings;testInputSettings=Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testInputSettings;
            InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=MakeCurrent;Application.logMessageReceived+=Log;
            AudioListener.volume=0;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            sequence=Sequence();gate=null;Write(false);
        }
        static void Feed(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic || finished)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));InputSystem.QueueStateEvent(mouse,new MouseState());}
        static void MakeCurrent(){if(keyboard!=null && keyboard.added)keyboard.MakeCurrent();if(mouse!=null && mouse.added)mouse.MakeCurrent();}
        static void Log(string condition,string trace,LogType type)
        {
            if(type!=LogType.Exception && type!=LogType.Error)return;
            if(trace.Contains("UnityEditor.Search.SearchDatabase")){editorErrors.Add(condition+"\n"+trace);return;}
            if(condition.StartsWith("[ContestTest]"))return;
            errors.Add(condition+"\n"+trace);
        }
        static void Tick()
        {
            try
            {
                if(videoTimes!=null && Time.realtimeSinceStartup-lastVideoFrame>=.25f)
                {
                    string path=Out+"/Frames/frame-"+videoFrames.ToString("D5")+".png";ScreenCapture.CaptureScreenshot(path);
                    videoTimes.WriteLine(videoFrames+","+(Time.realtimeSinceStartup-startedAt).ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+","+game.State.stage);videoTimes.Flush();lastVideoFrame=Time.realtimeSinceStartup;videoFrames++;
                }
                if(gate==null)
                {if(!sequence.MoveNext()){Finish();return;}gate=sequence.Current;}
                gate.tick?.Invoke();
                if(gate.ready()){gate=null;return;}
                if(Time.realtimeSinceStartup>gate.deadline)throw new Exception("Timeout: "+scenario);
            }
            catch(Exception ex){Fail(ex);}
        }
        static Gate Wait(float seconds,Action update=null)
        {float until=Time.realtimeSinceStartup+seconds;return new Gate(()=>Time.realtimeSinceStartup>=until,seconds+6,update);}
        static Gate Until(Func<bool> condition,string label,float timeout=20){scenario=label;return new Gate(condition,timeout);}
        static void Check(bool condition,string label)
        {if(!condition)throw new Exception(label);checks.Add(label);Debug.Log("[ContestTest] PASS "+label);Write(false);}
        static void Keys(params Key[] value){held=value;}
        static void Click(string id)
        {var button=game.Hud.FindButton(id);Check(button && button.gameObject.activeInHierarchy,"UI button exists "+id);button.onClick.Invoke();}
        static void Spawn(Vector3 feet)
        {
            Keys();Physics.SyncTransforms();Check(game.player.TryReturnToSpawn(feet,Quaternion.identity),"Safe production-controller placement "+feet);
            Check(Vector2.Distance(new Vector2(feet.x,feet.z),new Vector2(game.player.transform.position.x,game.player.transform.position.z))<.15f,"No fallback offset at "+feet);
        }
        static ContestWorldTarget Target(string id)
            =>Object.FindObjectsByType<ContestWorldTarget>(FindObjectsSortMode.None).Single(t=>t.id==id && t.gameObject.activeInHierarchy);
        static IEnumerable<Gate> WalkTo(Vector3 feet)
        {
            Keys();yield return Wait(.25f);scenario="Controller walk to "+feet;
            yield return new Gate(()=>{var d=feet-game.player.transform.position;d.y=0;if(d.magnitude<.15f){Keys();return true;}return false;},18,()=>{Aim(feet+Vector3.up*1.45f);Keys(Key.W);});
            Keys();yield return Wait(.16f);Check(game.player.IsGrounded,"Production controller walked and remained grounded at "+feet);
        }
        static void Aim(Vector3 point)
        {
            var rotation=Quaternion.LookRotation(point-game.view.transform.position);game.player.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);typeof(FirstPersonController).GetField("pitch",Private).SetValue(game.player,pitch);
            var pivot=(Transform)typeof(FirstPersonController).GetField("cameraPivot",Private).GetValue(game.player);pivot.localRotation=Quaternion.Euler(pitch,0,0);
        }
        static IEnumerable<Gate> Press(string id)
        {
            scenario="Interact "+id;Keys();var target=Target(id);Aim(target.Point);yield return Wait(.25f,()=>Aim(target.Point));
            Check(game.FindTarget()==target,"Actual unobstructed story ray reaches "+id);
            int count=game.InteractionCount;Keys(Key.E);yield return Wait(.12f,()=>Aim(target.Point));Keys();yield return Wait(.24f);
            Check(game.InteractionCount==count+1 && game.LastInteraction==id,"One virtual E press requests exactly once "+id);
        }
        static IEnumerable<Gate> CloseDialogue()
        {
            int limit=0;
            while(game.Mode==ContestStoryGame.ScreenMode.Dialogue)
            {yield return Wait(.18f);Click("DialogueNext");yield return Wait(.12f);if(++limit>16)throw new Exception("Dialogue did not finish");}
        }
        static void Capture(string name)
        {ScreenCapture.CaptureScreenshot(Out+"/Captures/"+name+".png");screenshots++;}
        static IEnumerable<Gate> Shot(string name)
        {yield return Wait(.3f);Capture(name);yield return Wait(.35f);}
        static IEnumerator<Gate> Sequence()
        {
            UnitChecks();yield return Wait(2);
            // Keep high-DPI PNG encoding out of the ending delivery's controller timing checks.
            // Video export is a separate fixed-time render; legacy recording remains opt-in here.
            if(!Environment.GetCommandLineArgs().Contains("-ending-test") || Environment.GetCommandLineArgs().Contains("-contest-record-video"))
            {
                Directory.CreateDirectory(Out+"/Frames");
                if((File.GetAttributes(Out+"/Frames")&FileAttributes.ReparsePoint)!=0)throw new IOException("Frame output must be an ordinary owned directory");
                foreach(string oldFrame in Directory.GetFiles(Out+"/Frames","frame-*.png",SearchOption.TopDirectoryOnly))File.Delete(oldFrame);
                videoTimes=new StreamWriter(Out+"/capture-times.csv");videoTimes.WriteLine("frame,realtimeSeconds,storyStage");videoFrames=0;lastVideoFrame=Time.realtimeSinceStartup;
            }
            Check(game.Mode==ContestStoryGame.ScreenMode.MainMenu,"Normal entry opens main menu");foreach(var g in Shot("01_main_menu"))yield return g;
            Click("Help");yield return Wait(.3f);Check(game.Mode==ContestStoryGame.ScreenMode.Help,"Controls available from main menu");Click("Back");yield return Wait(.25f);
            Click("NewGame");yield return Wait(.3f);foreach(var g in CloseDialogue())yield return g;
            Check(game.State.stage==0 && game.Mode==ContestStoryGame.ScreenMode.Explore,"New game begins at interactive desk");foreach(var g in Shot("02_work_desk"))yield return g;
            foreach(var g in Press("ARCHIVE_RECORD_SLOT_02"))yield return g;
            foreach(var g in Shot("03_record_reader"))yield return g;Click("ReaderClose");yield return Wait(.25f);Check(game.State.verifiedMask==0,"Closing unread record never verifies it");
            foreach(int n in new[]{2,1,3})
            {foreach(var g in Press("ARCHIVE_RECORD_SLOT_0"+n))yield return g;Click("ReaderConfirm");yield return Wait(.28f);}
            Check(game.State.verifiedMask==7,"Three records verified in non-numerical order");
            foreach(var g in Press("ARCHIVE_RECORD_SLOT_02"))yield return g;Click("ReaderConfirm");yield return Wait(.25f);Check(game.State.verifiedMask==7,"Re-reading does not duplicate record progress");
            foreach(var g in Press("ARCHIVE_TIE_CORD"))yield return g;Check(game.State.sealedBatch && game.workstation.IsArchiveSealed,"Actual cord interaction changes sealed presentation");
            foreach(var g in Press("ARCHIVE_RECORD_SLOT_03"))yield return g;yield return Wait(.2f);var handover=game.Hud.FindButton("Handover");Check(handover,"Handover unlocked only after records and seal");handover.onClick.Invoke();handover.onClick.Invoke();
            yield return Wait(.2f);foreach(var g in CloseDialogue())yield return g;
            yield return Wait(.45f);Check(game.Mode==ContestStoryGame.ScreenMode.Cutscene && game.CompletionCount("STORY_ARCHIVE_START")==1,"Double confirm starts breach once");
            Keys(Key.Escape);yield return Wait(.12f);Keys();yield return Wait(.15f);Check(game.Mode==ContestStoryGame.ScreenMode.Pause,"Escape pauses actual breach");
            var pausedPosition=game.player.transform.position;yield return Wait(.6f);Check(Vector3.Distance(pausedPosition,game.player.transform.position)<.001f,"Pause freezes real controller position including gravity");Click("Resume");
            yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Dialogue && game.State.stage==2,"Breach completes into rescue dialogue",22);
            foreach(var g in CloseDialogue())yield return g;
            yield return Until(()=>game.State.stage==3 && game.Mode==ContestStoryGame.ScreenMode.Explore,"Awakening stand-up restores safe controls");
            Check(game.State.stage==3 && game.State.batchLocation==1 && game.Checkpoint.stage==3,"Awakening saves safe story checkpoint and moved single bundle");
            Check(game.workstation.IsDisturbed && game.workstation.IsArchiveSealed,"Breach preserves sealed state in disturbed desk");foreach(var g in Shot("04_awakened"))yield return g;
            Spawn(new Vector3(6,.08f,1.4f));yield return Wait(.3f);foreach(var g in Press("ARCHIVE_TO_MAINTENANCE"))yield return g;
            yield return Until(()=>game.hall.activeSelf && !game.library.activeSelf && game.Mode==ContestStoryGame.ScreenMode.Dialogue,"Enter story hall through door");foreach(var g in CloseDialogue())yield return g;
            Check(!Object.FindObjectsByType<ArcaneArchive.Core.BossEncounter>(FindObjectsSortMode.None).Any(),"Story scene has no hostile encounter component");
            Check(game.hall.GetComponentsInChildren<Light>().Count(l=>l.type==LightType.Point && l.enabled)==6,"Story maintenance hall has six authored lights");foreach(var g in Shot("05_maintenance_entry"))yield return g;
            Spawn(new Vector3(168.71f,.08f,-8.71f));yield return Wait(.25f);foreach(var g in Press("MAINT_NODE_02"))yield return g;Check(game.State.nodes==0,"Out-of-order node is rejected without locking route");
            foreach(int n in new[]{1,2,3})
            {
                Spawn(n==1?new Vector3(168.71f,.08f,8.71f):n==2?new Vector3(168.71f,.08f,-8.71f):new Vector3(147.68f,.08f,0));yield return Wait(.3f);
                foreach(var g in Press("MAINT_NODE_0"+n))yield return g;foreach(var g in CloseDialogue())yield return g;Check(game.State.nodes==n,"Node completion "+n);
                foreach(var g in Press("MAINT_NODE_0"+n))yield return g;Check(game.State.nodes==n,"Stable node cannot duplicate progress "+n);
            }
            foreach(var g in Shot("06_all_nodes_stable"))yield return g;Spawn(new Vector3(160,.08f,-13.3f));yield return Wait(.25f);foreach(var g in Press("MAINTENANCE_TO_ARCHIVE"))yield return g;
            yield return Until(()=>game.State.stage==4 && game.Mode==ContestStoryGame.ScreenMode.Explore,"Return via actual story exit");
            Check(game.library.activeSelf && !game.hall.activeSelf && game.Checkpoint.stage==4,"Return retains one region and writes return checkpoint");
            Check(Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None).Length==1,"Only one player survives story return");
            foreach(var point in new[]{new Vector3(7.325f,.08f,6.9f),new Vector3(7.325f,.08f,10),new Vector3(5.5f,.08f,9.85f)})foreach(var g in WalkTo(point))yield return g;
            Aim(Target("REVEAL_WOUND").Point);foreach(var g in Shot("07_body_evidence"))yield return g;
            foreach(string id in new[]{"REVEAL_WOUND","REVEAL_BADGE","REVEAL_CORD"})
            {foreach(var g in Press(id))yield return g;foreach(var g in CloseDialogue())yield return g;}
            Check(game.State.stage==5 && game.State.clues==7 && !game.State.identityKnown,"First reveal confirms not Elaine before monster identity");
            Spawn(new Vector3(4.275f,.08f,9.85f));yield return Wait(.3f);foreach(var g in Press("ARCHIVE_INCIDENT_RECORD"))yield return g;Click("ReaderConfirm");yield return Wait(.3f);
            Check(game.State.incidentRead && game.palm.activeSelf,"Incident confirmation enables actual left-palm evidence");foreach(var g in Shot("08_palm_echo"))yield return g;
            Keys(Key.E);yield return Wait(.12f);Keys();yield return Wait(.3f);Check(game.Mode==ContestStoryGame.ScreenMode.Cutscene,"Palm confirmation starts own-memory flash");foreach(var g in Shot("09_identity_flash"))yield return g;
            yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Dialogue,"Identity flash ends",9);foreach(var g in CloseDialogue())yield return g;
            Check(game.State.stage==6 && game.State.identityKnown && game.Checkpoint.stage==6,"Monster identity follows physical proof and writes rescue checkpoint");
            game.Pause();yield return Wait(.2f);Click("Retry");yield return Wait(.2f);Click("ConfirmYes");yield return Wait(.3f);
            Check(game.State.stage==6 && game.State.clues==7 && game.Mode==ContestStoryGame.ScreenMode.Explore,"Real checkpoint restore preserves identity without duplicate story completion");
            foreach(var point in new[]{new Vector3(4,.08f,2.5f),new Vector3(4,.08f,3.8f)})foreach(var g in WalkTo(point))yield return g;
            foreach(var g in Press("RESCUE_CONDUIT"))yield return g;Check(game.PowerActive,"Rescue target starts actual power mechanic");
            Aim(Target("RESCUE_CONDUIT").Point);yield return Wait(.3f);
            for(int i=0;i<3;i++)
            {
                int before=game.Power.Failures;Keys(Key.E);yield return Until(()=>game.Power.Failures>before,"Overload retry "+i,7);Keys();yield return Wait(.25f);
            }
            Check(game.State.pulses==0 && !game.State.rescued && game.Power.Failures==3,"Repeated overcharge retries current pulse without destroying archives");
            game.Pause();yield return Wait(.25f);Click("Settings");yield return Wait(.25f);
            var assist=game.Hud.GetComponentsInChildren<Toggle>().Single(t=>t.GetComponentInChildren<Text>().text.StartsWith("辅助"));assist.isOn=true;Check(game.State.assist,"Assist option is reachable from actual settings UI");
            Click("Back");yield return Wait(.25f);Click("Resume");yield return Wait(.3f);Aim(Target("RESCUE_CONDUIT").Point);
            foreach(var g in Shot("10_power_feedback"))yield return g;
            float safetyUntil=Time.realtimeSinceStartup+35;
            yield return new Gate(()=>game.State.rescued,40,()=>{
                Aim(Target("RESCUE_CONDUIT").Point);
                if(game.Power.NeedsRelease)Keys();
                else if(game.Power.Strength<47)Keys(Key.E);
                else if(game.Power.Strength>62)Keys();
                if(Time.realtimeSinceStartup>safetyUntil)throw new Exception("Power test control stalled");
            });Keys();yield return Wait(.3f);
            Check(game.State.tutorialDone && game.State.pulses==3 && game.State.rescued,"Tutorial plus three real power pulses complete rescue");foreach(var g in Shot("11_rescue_complete"))yield return g;
            foreach(var point in new[]{new Vector3(3.1f,.08f,3.8f),new Vector3(3.1f,.08f,6.9f),new Vector3(4.275f,.08f,6.9f),new Vector3(4.275f,.08f,9.85f),new Vector3(5.5f,.08f,9.85f)})foreach(var g in WalkTo(point))yield return g;
            foreach(var g in Press("REVEAL_CORD"))yield return g;
            Check(game.State.batchLocation==2 && !game.sealedBundle.activeSelf,"Picking up moves the single batch instead of cloning it");
            foreach(var point in new[]{new Vector3(4.275f,.08f,9.85f),new Vector3(4.275f,.08f,6.9f),new Vector3(3.1f,.08f,6.9f),new Vector3(3.1f,.08f,3),new Vector3(5,.08f,2.5f)})foreach(var g in WalkTo(point))yield return g;
            foreach(var g in Press("ARCHIVE_TRANSFER_CASE"))yield return g;foreach(var g in CloseDialogue())yield return g;
            yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Ending,"Ending Timeline completes",40);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending && game.State.transferred,"Normal route reaches explicit completed-game ending");foreach(var g in Shot("12_ending"))yield return g;
            foreach(string id in new[]{"STORY_ARCHIVE_START","STORY_BREACH","STORY_AWAKEN","STORY_MAINTENANCE_ENTER","STORY_ARCHIVE_RETURN","STORY_IDENTITY_CONFIRM","STORY_RESCUE","STORY_COMPLETE"})Check(game.CompletionCount(id)==1,"Story event completed exactly once "+id);
            Check(ContestCheckpointStore.TryRead(game.SavePath,out var saved,out _) && saved.stage==7 && saved.transferred,"Atomic disk checkpoint contains finished game");
            yield return Wait(.3f);Click("MainMenu");yield return Wait(.3f);Click("Continue");yield return Wait(.3f);Check(game.Mode==ContestStoryGame.ScreenMode.Ending,"Continue completed checkpoint safely restores ending");
            Check(errors.Count==0,"No runtime exceptions or errors during full contest route");yield return Wait(.7f);
        }
        static void UnitChecks()
        {
            string fixtures=Out+"/Fixtures";Directory.CreateDirectory(fixtures);
            var p=new ContestPowerChallenge();p.Reset(false);float elapsed=0;
            while(elapsed<8){bool held=p.Strength<50;int result=p.Step(1f/60,held,true,false);elapsed+=1f/60;if(result==1)break;}
            Check(elapsed<8,"Normal power settings are completable with hold/release control");
            p.Reset(false);p.Step(1,true,true,false);float strength=p.Strength;p.Step(3,true,true,true);Check(Mathf.Abs(p.Strength-strength)<.001f,"Power does not advance while paused");
            Check(p.NeedsRelease,"Suspended power requires explicit release");
            foreach(int fps in new[]{20,60,144})
            {p.Reset(false);bool done=false;for(int n=0;n<fps*8;n++){int result=p.Step(1f/fps,p.Strength<50,true,false);if(result==1){done=true;break;}}Check(done,"Power substep integration works at "+fps+" FPS");}
            string path=fixtures+"/checkpoint.json";var valid=new ContestStoryState();Check(ContestCheckpointStore.TryWrite(path,valid,out _),"Checkpoint atomically writes valid state");
            Check(ContestCheckpointStore.TryRead(path,out var copy,out _) && copy.stage==0,"Checkpoint roundtrip preserves start state");
            var opened=new System.Threading.ManualResetEvent(false);
            var locker=new System.Threading.Thread(()=>{using(var handle=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)){opened.Set();System.Threading.Thread.Sleep(55);}});
            locker.Start();opened.WaitOne();var retryState=valid.Copy();retryState.assist=true;
            bool retried=ContestCheckpointStore.TryWrite(path,retryState,out _);locker.Join();opened.Dispose();
            Check(retried && ContestCheckpointStore.TryRead(path,out var retriedState,out _) && retriedState.assist,"Checkpoint survives a transient exclusive file lock");
            bool rejected;
            using(var handle=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))rejected=!ContestCheckpointStore.TryWrite(path,valid,out _);
            Check(rejected && ContestCheckpointStore.TryRead(path,out var retainedState,out _) && retainedState.assist,"Persistent checkpoint lock preserves previous saved state");
            File.WriteAllText(path,"{broken json");Check(!ContestCheckpointStore.TryRead(path,out _,out _),"Corrupt checkpoint is rejected safely");
            Check(!ContestCheckpointStore.TryWrite(path,new ContestStoryState{stage=7},out _),"Inconsistent completion cannot overwrite checkpoint");
        }
        static void Write(bool complete)
        {
            var r=new Report{complete=complete,failure=failure??"",scenario=scenario,unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,checksPassed=checks.Count,stage=game?game.State.stage:0,pulses=game?game.State.pulses:0,screenshots=screenshots,videoFrames=videoFrames,seconds=Time.realtimeSinceStartup-startedAt,checks=checks.ToArray(),runtimeErrors=errors.ToArray(),editorSearchErrors=editorErrors.ToArray(),scope="Full contest story in actual candidate scene/controller; isolated virtual keyboard for world interaction and real UI button callbacks. New prop routes are walked with the controller; other setup points use safe test placements. Not a human playtest or standalone performance acceptance. Known Editor Search errors are separately retained, not counted as game runtime errors."};
            File.WriteAllText(Out+"/runtime-validation.json",JsonUtility.ToJson(r,true));
        }
        static void Finish()
        {
            finished=true;Keys();Write(true);Cleanup();SessionState.SetBool(Flag,false);
            if(SessionState.GetBool(Flag+".BuildAfter",false))EditorApplication.update+=BuildAfterStop;
            else EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
            EditorApplication.isPlaying=false;
        }
        static void BuildAfterStop()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
            EditorApplication.update-=BuildAfterStop;SessionState.SetBool(Flag+".BuildAfter",false);
            try{ContestSceneBuilder.BuildWindows();EditorApplication.Exit(0);}
            catch(Exception ex){Debug.LogError("[ContestTest] Build after acceptance failed: "+ex);EditorApplication.Exit(2);}
        }
        static void Fail(Exception ex)
        {
            if(finished)return;finished=true;failure=ex.ToString();Debug.LogError("[ContestTest] "+failure);
            try{Capture("FAILURE");Write(false);}catch(Exception){}
            Cleanup();SessionState.SetBool(Flag,false);EditorApplication.delayCall+=()=>EditorApplication.Exit(1);EditorApplication.isPlaying=false;
        }
        static void Cleanup()
        {
            if(originalInputSettings){InputSystem.settings=originalInputSettings;Object.DestroyImmediate(testInputSettings);}
            Application.runInBackground=SessionState.GetBool(Flag+".background",false);
            InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=MakeCurrent;Application.logMessageReceived-=Log;
            videoTimes?.Dispose();videoTimes=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        }
    }
}
