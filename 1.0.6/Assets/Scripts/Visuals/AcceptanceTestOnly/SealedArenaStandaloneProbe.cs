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
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
namespace ArcaneArchive.Visuals.Acceptance
{
    // Explicit opt-in acceptance utility; dormant during ordinary play.
    public sealed class SealedArenaStandaloneProbe : MonoBehaviour
    {
        BossEncounter encounter;FirstPersonController player;RecoveryMenu menu;
        Keyboard keyboard;Mouse mouse;MouseState pointerState;Key[] keys=Array.Empty<Key>();
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        readonly List<float> frames=new List<float>();string output;bool running,finished;
        AmbientMode originalAmbient;FogMode originalFogMode;Color originalLight,originalFogColor;bool originalFog;float originalDensity;Material originalSky;
        [Serializable] class Report{public bool complete;public string failure,unity,device,cpu,scope;public string[] checks,errors;public int width,height,frameSamples;public float frameP50Ms,frameP95Ms;}
        void Awake()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-arena-smoke")){enabled=false;return;}
            int n=Array.IndexOf(args,"-arena-report");output=n>=0&&n+1<args.Length?args[n+1]:Path.Combine(Application.persistentDataPath,"SealedArenaAcceptance");Directory.CreateDirectory(output);
            originalAmbient=RenderSettings.ambientMode;originalLight=RenderSettings.ambientLight;originalFogMode=RenderSettings.fogMode;originalFog=RenderSettings.fog;originalFogColor=RenderSettings.fogColor;originalDensity=RenderSettings.fogDensity;originalSky=RenderSettings.skybox;
            running=true;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Application.logMessageReceived+=Log;
        }
        void Log(string condition,string trace,LogType type){if(running && (type==LogType.Error || type==LogType.Exception)){errors.Add(condition+"\n"+trace);Finish(false,condition);}}
        void Update(){if(running && encounter && encounter.IsActive && !encounter.IsPaused)frames.Add(Time.unscaledDeltaTime*1000);}
        void Check(bool ok,string label){if(!ok){Finish(false,label);throw new Exception(label);}checks.Add(label);}
        void Feed(){if(running && keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,pointerState);}}
        void Current(){if(keyboard!=null && keyboard.added)keyboard.MakeCurrent();if(mouse!=null && mouse.added)mouse.MakeCurrent();}
        IEnumerator Start()
        {
            if(!running)yield break;
            encounter=GetComponent<BossEncounter>();player=GetComponent<FirstPersonController>();menu=GetComponent<RecoveryMenu>();
            float until=Time.realtimeSinceStartup+45;while(!encounter.IsActive && Time.realtimeSinceStartup<until)yield return null;
            Check(encounter.IsActive && encounter.Arena,"Native player enters the new Boss arena automatically");
            keyboard=InputSystem.AddDevice<Keyboard>("SealedArenaTestKeyboard");mouse=InputSystem.AddDevice<Mouse>("SealedArenaTestMouse");
            var asset=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);asset.devices=new InputDevice[]{keyboard,mouse};asset.RemoveAllBindingOverrides();
            InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
            yield return new WaitForSecondsRealtime(3);Check(encounter.BattleTime>0,"Actual built battle clock advances");
            Check(encounter.Arena.GetComponent<SealedArenaAtmosphere>() && encounter.Arena.GetComponentsInChildren<MeshRenderer>().Length>50,"Built player contains the modeled arena and its own atmosphere");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01_native_battle.png"));yield return new WaitForSecondsRealtime(.5f);
            var position=player.transform.position;keys=new[]{Key.A};yield return new WaitForSecondsRealtime(.65f);keys=Array.Empty<Key>();yield return new WaitForSecondsRealtime(.15f);
            Check(Vector3.Distance(position,player.transform.position)>.6f,"Real controller keyboard movement traverses the new combat floor");
            menu.Open();float clock=encounter.BattleTime;yield return new WaitForSecondsRealtime(.6f);Check(encounter.IsPaused && Mathf.Abs(encounter.BattleTime-clock)<.001f,"Pause stops the native battle clock");
            var resumeButton=menu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.gameObject.activeInHierarchy && b.GetComponentInChildren<TMPro.TMP_Text>().text=="继续战斗");
            pointerState=new MouseState{position=RectTransformUtility.WorldToScreenPoint(null,resumeButton.transform.position)};
            yield return null;yield return null;
            pointerState=pointerState.WithButton(MouseButton.Left,true);yield return null;yield return null;
            pointerState=pointerState.WithButton(MouseButton.Left,false);yield return new WaitForSecondsRealtime(.7f);
            Check(!encounter.IsPaused && encounter.BattleTime>clock,"Actual UI mouse click on Continue resumes combat through EventSystem");
            until=Time.realtimeSinceStartup+20;while(encounter.CurrentState!=BossEncounter.State.Attack && Time.realtimeSinceStartup<until)yield return null;
            Check(encounter.CurrentState==BossEncounter.State.Attack,"A real production Boss attack runs in the rebuilt arena");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02_native_attack.png"));yield return new WaitForSecondsRealtime(.35f);
            encounter.DamageBoss(encounter.Definition.health*2,false);yield return new WaitForSecondsRealtime(.3f);Check(encounter.IsFinished,"Encounter can finish with the new environment active");
            encounter.Retry();yield return new WaitForSecondsRealtime(.1f);Check(encounter.CurrentState==BossEncounter.State.Intro && encounter.Phase==1 && encounter.Combat.Health==encounter.Definition.playerHealth,"Retry restores Boss, player health and safe spawn");
            menu.Open();yield return null;
            var returnButton=menu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.gameObject.activeInHierarchy && b.GetComponentInChildren<TMPro.TMP_Text>().text=="放弃并返回图书馆");
            pointerState=new MouseState{position=RectTransformUtility.WorldToScreenPoint(null,returnButton.transform.position)};
            yield return null;yield return null;pointerState=pointerState.WithButton(MouseButton.Left,true);yield return null;yield return null;
            pointerState=pointerState.WithButton(MouseButton.Left,false);yield return null;yield return null;
            Check(!encounter.IsActive,"Actual UI mouse click on Return exits the arena through EventSystem");
            Check(!encounter.IsActive && RenderSettings.ambientMode==originalAmbient && RenderSettings.ambientLight==originalLight && RenderSettings.fog==originalFog && RenderSettings.fogMode==originalFogMode && RenderSettings.fogColor==originalFogColor && Mathf.Abs(RenderSettings.fogDensity-originalDensity)<.00001f && RenderSettings.skybox==originalSky,"Returning restores the exact library atmosphere");
            Check(encounter.Begin(),"Reentering the rebuilt arena works after return");yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03_native_reentry.png"));yield return new WaitForSecondsRealtime(.5f);Finish(true,"");
        }
        void Finish(bool ok,string reason)
        {
            if(finished)return;finished=true;running=false;frames.Sort();Func<float,float> quantile=q=>frames.Count==0?0:frames[Mathf.Clamp(Mathf.CeilToInt(q*frames.Count)-1,0,frames.Count-1)];
            File.WriteAllText(Path.Combine(output,"NativeSmoke.json"),JsonUtility.ToJson(new Report{complete=ok,failure=reason,unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,checks=checks.ToArray(),errors=errors.ToArray(),width=Screen.width,height=Screen.height,frameSamples=frames.Count,frameP50Ms=quantile(.5f),frameP95Ms=quantile(.95f),scope="Actual native Mac executable at specified window resolution; isolated virtual input, real controller/encounter and pause UI; explicit damage injection only to exercise completion/retry. Includes screenshot/setup overhead, not a minimum-hardware benchmark or manager acceptance."},true));
            InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;Application.logMessageReceived-=Log;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);Application.Quit(ok?0:1);
        }
    }
}
