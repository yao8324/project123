using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using ArcaneArchive.Player;

namespace ArcaneArchive.Contest
{
    // Opt-in diagnostics for the actual built player. In ordinary play this component disables itself.
    public sealed class ContestBuildProbe : MonoBehaviour
    {
        ContestStoryGame game;Keyboard keyboard;Mouse mouse;Key[] held=Array.Empty<Key>();
        string output;bool finished;float began;int capturedImages;
        InputSettings.BackgroundBehavior previousBackground;
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        readonly List<float> frames=new List<float>();
        [Serializable] sealed class Result {public bool complete,batchMode;public string unity,device,cpu,failure,scope,quality;public int width,height,frameSamples,capturedImages;public float seconds,p50FrameMs,p95FrameMs,p99FrameMs;public string[] checks,errors;}
        void Update(){if(!finished && game && game.Mode==ContestStoryGame.ScreenMode.Explore && frames.Count<50000)frames.Add(Time.unscaledDeltaTime*1000);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(!args.Contains("-contest-test")){enabled=false;yield break;}
            Application.runInBackground=true;
            previousBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            game=GetComponent<ContestStoryGame>();began=Time.realtimeSinceStartup;
            int n=Array.IndexOf(args,"-contest-report");output=n>=0 && n+1<args.Length?args[n+1]:Path.Combine(Application.persistentDataPath,"IMMonsterContest20261107","StandaloneAcceptance");Directory.CreateDirectory(output);
            keyboard=InputSystem.AddDevice<Keyboard>("ContestBuildKeyboard");mouse=InputSystem.AddDevice<Mouse>("ContestBuildMouse");
            yield return null;
            var asset=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.player);asset.devices=new InputDevice[]{keyboard,mouse};asset.RemoveAllBindingOverrides();
            InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;Application.logMessageReceived+=Log;
            yield return Flow();
        }
        void Feed(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic || finished)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));InputSystem.QueueStateEvent(mouse,new MouseState());}
        void Current(){if(keyboard!=null && keyboard.added)keyboard.MakeCurrent();if(mouse!=null && mouse.added)mouse.MakeCurrent();}
        void Log(string condition,string trace,LogType type)
        {if(!finished && (type==LogType.Error || type==LogType.Exception)){errors.Add(condition+"\n"+trace);Done(false,condition);}}
        void Assert(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
        void Click(string id){var b=game.Hud.FindButton(id);Assert(b && b.gameObject.activeInHierarchy,"Player UI "+id);b.onClick.Invoke();}
        IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
        IEnumerator Until(Func<bool> value,string label,float limit=25)
        {float until=Time.realtimeSinceStartup+limit;while(!value()){if(Time.realtimeSinceStartup>until)throw new Exception("Timeout "+label);yield return null;}}
        IEnumerator Dialogue()
        {int count=0;while(game.Mode==ContestStoryGame.ScreenMode.Dialogue){yield return Wait(.22f);Click("DialogueNext");yield return Wait(.12f);if(++count>16)throw new Exception("Dialogue loop");}}
        ContestWorldTarget Target(string id)=>FindObjectsByType<ContestWorldTarget>(FindObjectsSortMode.None).Single(t=>t.id==id && t.gameObject.activeInHierarchy);
        void Place(Vector3 point)
        {held=Array.Empty<Key>();Physics.SyncTransforms();Assert(game.player.TryReturnToSpawn(point,Quaternion.identity),"Built controller safe placement "+point);}
        void Aim(Vector3 point)
        {
            var rotation=Quaternion.LookRotation(point-game.view.transform.position);game.player.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);typeof(FirstPersonController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.player,pitch);
            var pivot=(Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.player);pivot.localRotation=Quaternion.Euler(pitch,0,0);
        }
        IEnumerator Interact(string id)
        {
            var target=Target(id);held=Array.Empty<Key>();Aim(target.Point);yield return Wait(.25f);
            Assert(game.FindTarget()==target,"Built scene ray "+id);int before=game.InteractionCount;held=new[]{Key.E};yield return Wait(.12f);held=Array.Empty<Key>();yield return Wait(.25f);
            Assert(game.InteractionCount==before+1,"Built interaction once "+id);
        }
        IEnumerator Shot(string name)
        {yield return Wait(.3f);if(!Application.isBatchMode && SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));capturedImages++;}yield return Wait(.35f);}
        IEnumerator Flow()
        {
            yield return Wait(2);Assert(game.Mode==ContestStoryGame.ScreenMode.MainMenu,"Built player boots normally");yield return Shot("01_main_menu");Click("NewGame");yield return Wait(.3f);yield return Dialogue();
            foreach(int index in new[]{1,2,3}){yield return Interact("ARCHIVE_RECORD_SLOT_0"+index);Click("ReaderConfirm");yield return Wait(.3f);}
            yield return Interact("ARCHIVE_TIE_CORD");yield return Interact("ARCHIVE_RECORD_SLOT_03");Click("Handover");yield return Wait(.25f);yield return Dialogue();yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Dialogue && game.State.stage==2,"built breach",25);yield return Dialogue();yield return Until(()=>game.State.stage==3 && game.Mode==ContestStoryGame.ScreenMode.Explore,"built stand-up");
            Assert(game.State.stage==3,"Built awakening restores play");Place(new Vector3(6,.08f,1.4f));yield return Wait(.3f);yield return Interact("ARCHIVE_TO_MAINTENANCE");yield return Until(()=>game.hall.activeSelf && game.Mode==ContestStoryGame.ScreenMode.Dialogue,"built hall");yield return Dialogue();
            for(int i=1;i<=3;i++)
            {Place(i==1?new Vector3(168.71f,.08f,8.71f):i==2?new Vector3(168.71f,.08f,-8.71f):new Vector3(147.68f,.08f,0));yield return Wait(.3f);yield return Interact("MAINT_NODE_0"+i);yield return Dialogue();}
            Assert(game.State.nodes==3,"Built nodes complete");Place(new Vector3(160,.08f,-13.3f));yield return Wait(.3f);yield return Interact("MAINTENANCE_TO_ARCHIVE");yield return Until(()=>game.State.stage==4 && game.Mode==ContestStoryGame.ScreenMode.Explore,"built return");
            Place(new Vector3(5.5f,.08f,9.85f));yield return Wait(.3f);yield return Shot("02_body");
            foreach(string id in new[]{"REVEAL_WOUND","REVEAL_BADGE","REVEAL_CORD"}){yield return Interact(id);yield return Dialogue();}
            Assert(game.State.stage==5 && !game.State.identityKnown,"Built first reveal precedes monster identity");Place(new Vector3(4.275f,.08f,9.85f));yield return Wait(.3f);yield return Interact("ARCHIVE_INCIDENT_RECORD");Click("ReaderConfirm");yield return Wait(.3f);
            held=new[]{Key.E};yield return Wait(.12f);held=Array.Empty<Key>();yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Dialogue,"built memory");yield return Dialogue();Assert(game.State.identityKnown && game.State.stage==6,"Built monster identity confirmed");
            Place(new Vector3(4,.08f,3.8f));yield return Wait(.3f);yield return Interact("RESCUE_CONDUIT");
            float deadline=Time.realtimeSinceStartup+30;
            while(!game.State.rescued)
            {
                if(Time.realtimeSinceStartup>deadline)throw new Exception("Built power failed to complete");Aim(Target("RESCUE_CONDUIT").Point);
                if(game.Power.NeedsRelease)held=Array.Empty<Key>();else if(game.Power.Strength<47)held=new[]{Key.E};else if(game.Power.Strength>62)held=Array.Empty<Key>();yield return null;
            }
            held=Array.Empty<Key>();Assert(game.State.pulses==3,"Built normal power challenge completes three pulses");yield return Shot("03_rescue");
            Place(new Vector3(5.5f,.08f,9.85f));yield return Wait(.3f);yield return Interact("REVEAL_CORD");Place(new Vector3(5,.08f,2.5f));yield return Wait(.3f);yield return Interact("ARCHIVE_TRANSFER_CASE");yield return Dialogue();
            Assert(game.Mode==ContestStoryGame.ScreenMode.Ending,"Actual executable reaches ending");Assert(ContestCheckpointStore.TryRead(game.SavePath,out var saved,out _) && saved.stage==7,"Actual executable saved completion");yield return Shot("04_ending");
            Click("MainMenu");yield return Wait(.3f);Click("Continue");yield return Wait(.3f);Assert(game.Mode==ContestStoryGame.ScreenMode.Ending,"Actual executable resumes completed checkpoint");
            Click("Quit");yield return Wait(.3f);Click("ConfirmNo");yield return Wait(.3f);Assert(game.Mode==ContestStoryGame.ScreenMode.Ending,"Cancel exit restores ending instead of restarting gameplay");Done(true,"");
        }
        void Done(bool complete,string reason)
        {
            if(finished)return;finished=true;
            frames.Sort();Func<float,float> quantile=q=>frames.Count==0?0:frames[Mathf.Clamp(Mathf.CeilToInt(q*frames.Count)-1,0,frames.Count-1)];
            File.WriteAllText(Path.Combine(output,"standalone-validation.json"),JsonUtility.ToJson(new Result{complete=complete,batchMode=Application.isBatchMode,capturedImages=capturedImages,unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,failure=reason,seconds=Time.realtimeSinceStartup-began,width=Screen.width,height=Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],frameSamples=frames.Count,p50FrameMs=quantile(.5f),p95FrameMs=quantile(.95f),p99FrameMs=quantile(.99f),checks=checks.ToArray(),errors=errors.ToArray(),scope="Actual built Windows executable. Isolated virtual input and actual UI callbacks; safe placements between targets. Frame samples are interactive portions of this automated route including setup/capture overhead. Not human playtesting, minimum-spec certification or worst-case benchmarking."},true));
            InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;Application.logMessageReceived-=Log;
            InputSystem.settings.backgroundBehavior=previousBackground;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
            Application.Quit(complete?0:1);
        }
    }
}
