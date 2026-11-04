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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Opt-in editor verification. Uses virtual input and a separate automated checkpoint.</summary>
    [InitializeOnLoad]
    public static class ContestEndingAcceptance
    {
        const string Flag = "IMMonster.Ending.Test";
        const string PreviewFlag = "IMMonster.Ending.Preview";
        const string Out = "Logs/Ending20261104";
        static ContestStoryGame game;
        static Keyboard keyboard;
        static Mouse mouse;
        static InputSettings originalInputSettings,testInputSettings;
        static Key[] held = Array.Empty<Key>();
        static IEnumerator<Func<bool>> tests;
        static Func<bool> gate;
        static double deadline;
        static bool started;
        static bool originalMasterMute;
        static readonly List<string> checks = new List<string>(), errors = new List<string>();
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] sealed class Report { public bool passed; public string unity, failure; public string[] checks, errors; public string audioScope; }
        static ContestEndingAcceptance() { EditorApplication.update += Tick; }

        [MenuItem("IM Monster/Ending/Run ending checks")]
        public static void Run()
        {
            if (!Environment.GetCommandLineArgs().Contains("-ending-test"))
                throw new InvalidOperationException("Launch with -ending-test to isolate input and save data (do not enable the separate -contest-test player probe).");
            Directory.CreateDirectory(Out);
            ContestEndingBuilder.Build();
            EditorSceneManager.OpenScene(ContestSceneBuilder.ScenePath);
            SessionState.SetBool(Flag, true);
            SessionState.SetFloat(Flag + ".deadline", (float)EditorApplication.timeSinceStartup + 240);
            SessionState.SetBool(Flag+".background",Application.runInBackground);
            GameView(); Application.runInBackground = true; EditorApplication.isPlaying = true;
        }
        public static void FullRoute(){ContestEndingBuilder.Build();ContestAcceptance.Run();}

        [MenuItem("IM Monster/Ending/Preview ending (no save)")]
        public static void Preview()
        {
            if (EditorApplication.isPlaying)
            {
                var active = Object.FindFirstObjectByType<ContestStoryGame>();
                if (active) PreviewNow(active);
                return;
            }
            EditorSceneManager.OpenScene(ContestSceneBuilder.ScenePath);
            SessionState.SetBool(PreviewFlag, true); GameView(); EditorApplication.isPlaying = true;
        }
        static void GameView()
        {
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            view.maximized = false; view.position = new Rect(30, 35, 1440, 900); view.Focus();
        }
        static void PreviewNow(ContestStoryGame target)
        {
            // Restore() presents in-memory state only; never writes the player's actual checkpoint.
            typeof(ContestStoryGame).GetMethod("Restore", Private).Invoke(target, new object[] { CompleteState() });
            target.ReplayEnding();
        }
        static ContestStoryState CompleteState() => new ContestStoryState {
            stage=7, verifiedMask=7, sealedBatch=true, nodes=3, clues=7, pulses=3,
            incidentRead=true, identityKnown=true, rescued=true, tutorialDone=true, transferred=true, batchLocation=3 };

        static void Tick()
        {
            if (SessionState.GetBool(PreviewFlag, false) && EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                var target = Object.FindFirstObjectByType<ContestStoryGame>();
                if (target && target.Hud && target.player.Bindings != null)
                { SessionState.SetBool(PreviewFlag, false); PreviewNow(target); }
            }
            if (!SessionState.GetBool(Flag, false)) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Flag + ".deadline", 0)) throw new Exception("Global test timeout");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                EditorApplication.QueuePlayerLoopUpdate();
                if (!started)
                {
                    game = Object.FindFirstObjectByType<ContestStoryGame>();
                    if (!game || !game.Hud || game.player.Bindings == null) return;
                    started = true; checks.Clear(); errors.Clear();
                    originalMasterMute=EditorUtility.audioMasterMute;EditorUtility.audioMasterMute=false;
                    keyboard = InputSystem.AddDevice<Keyboard>("EndingTestKeyboard"); mouse = InputSystem.AddDevice<Mouse>("EndingTestMouse");
                    var actions = (InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions", Private).GetValue(game.player);
                    actions.devices = new InputDevice[] {keyboard, mouse}; actions.RemoveAllBindingOverrides();
                    originalInputSettings=InputSystem.settings;testInputSettings=Object.Instantiate(originalInputSettings);
                    testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings=testInputSettings;
                    InputSystem.onBeforeUpdate += Feed;InputSystem.onAfterUpdate+=MakeCurrent; Application.logMessageReceived += Log;
                    tests = Sequence(); gate = null;
                }
                if (gate == null) { if (!tests.MoveNext()) { Finish(null); return; } gate = tests.Current; }
                if (gate()) gate = null;
                else if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Step timed out after " + checks.LastOrDefault());
            }
            catch (Exception ex) { Finish(ex.ToString()); }
        }
        static void Feed()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(held)); InputSystem.QueueStateEvent(mouse, new MouseState());
            keyboard.MakeCurrent(); mouse.MakeCurrent();
        }
        static void Log(string message, string trace, LogType type)
        { if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search.SearchDatabase")) errors.Add(message + "\n" + trace); }
        static void MakeCurrent(){if(keyboard!=null && keyboard.added)keyboard.MakeCurrent();if(mouse!=null && mouse.added)mouse.MakeCurrent();}
        static Func<bool> Wait(double seconds)
        { double until = EditorApplication.timeSinceStartup + seconds; deadline = until + 8; return () => EditorApplication.timeSinceStartup >= until; }
        static Func<bool> Until(Func<bool> condition, double timeout = 35)
        { deadline = EditorApplication.timeSinceStartup + timeout; return condition; }
        static void Check(bool value, string label)
        { if (!value) throw new Exception(label); checks.Add(label); Debug.Log("[EndingTest] PASS " + label); }
        static void Click(string id)
        {
            var button = game.Hud.FindButton(id);
            Check(button && button.gameObject.activeInHierarchy, "Visible button " + id); button.onClick.Invoke();
        }
        static void Shot(string name) => ScreenCapture.CaptureScreenshot(Out + "/" + name + ".png");
        static void Aim(Vector3 target)
        {
            var rotation = Quaternion.LookRotation(target - game.view.transform.position);
            game.player.transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            float pitch = Mathf.DeltaAngle(0, rotation.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch", Private).SetValue(game.player, pitch);
            game.cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        static IEnumerator<Func<bool>> Sequence()
        {
            yield return Wait(1);
            Check(game.Mode == ContestStoryGame.ScreenMode.MainMenu, "Normal entry still opens main menu");
            Check(game.EndingCutscene.Ready, "Timeline resource included");
            var state = CompleteState(); state.stage=6; state.transferred=false; state.batchLocation=2;
            typeof(ContestStoryGame).GetMethod("Restore", Private).Invoke(game, new object[] {state});
            Check(game.player.TryReturnToSpawn(new Vector3(5,.08f,2.5f), Quaternion.identity), "Safe transfer approach with production controller");
            var target = Object.FindObjectsByType<ContestWorldTarget>(FindObjectsSortMode.None).Single(t=>t.id=="ARCHIVE_TRANSFER_CASE");
            Aim(target.Point); yield return Wait(.4);
            Check(game.FindTarget()==target, "Real interaction ray reaches transfer box");
            held=new[]{Key.E}; yield return Until(()=>game.InteractionCount>0,4); held=Array.Empty<Key>(); yield return Wait(.25);
            Check(game.State.transferred && game.EndingCutscene.Running && game.Mode==ContestStoryGame.ScreenMode.Cutscene, "One actual E press starts ending after transfer ("+game.Mode+", count="+game.InteractionCount+")");
            Check(game.SavePath.EndsWith("ending-acceptance-checkpoint.json"), "Automated checkpoint isolated from player progress");
            Check(ContestCheckpointStore.TryRead(game.SavePath,out var saved,out _) && saved.stage==7 && saved.transferred, "Finished checkpoint persisted before cutscene");
            Check(game.CompletionCount("STORY_RESCUE")==1 && game.CompletionCount("STORY_COMPLETE")==0, "Rescue committed once before visual completion");
            Check(!game.view.enabled && game.EndingCutscene.ShotCamera.enabled, "Only cinematic camera renders");
            var package=game.sealedBundle.transform; var packageParent=package.parent;
            Check(package.gameObject.activeInHierarchy && packageParent.name=="SingleBatchMotion", "Existing unique package animated without cloning");
            yield return Until(()=>game.EndingCutscene.Seconds>1.7); Shot("01_package_enters");
            Check(Mathf.Abs(Mathf.DeltaAngle(game.transferCase.transform.Find("Lid").localEulerAngles.x,68))<2,"Lid opens upwards before package settles");
            Check(game.EndingCutscene.ShotCamera.transform.localPosition.magnitude>1, "Native Timeline camera curves evaluate");
            yield return Until(()=>game.EndingCutscene.Seconds>3);
            held=new[]{Key.Escape}; yield return Wait(.12); held=Array.Empty<Key>(); yield return Wait(.15);
            Check(game.Mode==ContestStoryGame.ScreenMode.Pause, "Escape pauses ending");
            double time=game.EndingCutscene.Seconds; var cameraPosition=game.EndingCutscene.ShotCamera.transform.position;
            var packagePosition=package.position; yield return Wait(.8);
            Check(Math.Abs(game.EndingCutscene.Seconds-time)<.002 && Vector3.Distance(cameraPosition,game.EndingCutscene.ShotCamera.transform.position)<.0001f && Vector3.Distance(packagePosition,package.position)<.0001f, "Pause freezes Timeline, camera, package and lid");
            Check(AudioListener.pause && Time.timeScale==0, "Pause suspends audio and gameplay"); Shot("02_paused");
            Click("Settings"); yield return Wait(.2); Click("Back"); yield return Wait(.2); Click("Resume");
            yield return Wait(.6); Check(game.EndingCutscene.Seconds>time+.3, "Settings and resume preserve cutscene position");
            yield return Until(()=>game.EndingCutscene.Seconds>7); Shot("03_seal_stable");
            Check(Mathf.Abs(Mathf.DeltaAngle(game.transferCase.transform.Find("Lid").localEulerAngles.x,0))<1,"Lid fully closes after transfer");
            Check(game.EndingCutscene.Subtitle==game.StoryContent.dialogue.Single(l=>l.id=="D022").text, "Existing warden dialogue synchronized");
            var audio=game.EndingCutscene.ShotCamera.transform.parent.GetComponent<AudioSource>();
            var samples=new float[1024];audio.GetOutputData(samples,0);
            var mix=new float[1024];AudioListener.GetOutputData(mix,0);
            Debug.Log("[EndingTest] Audio diagnostics source="+samples.Max(v=>Mathf.Abs(v))+" listener="+mix.Max(v=>Mathf.Abs(v))+" pause="+AudioListener.pause+" volume="+AudioListener.volume+" playing="+audio.isPlaying+" mute="+EditorUtility.audioMasterMute+" listeners="+Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)+" DSP="+AudioSettings.dspTime);
            var director=game.EndingCutscene.GetComponent<PlayableDirector>();
            var audioTrack=((UnityEngine.Timeline.TimelineAsset)director.playableAsset).GetOutputTracks().OfType<UnityEngine.Timeline.AudioTrack>().Single();
            var audioClip=((UnityEngine.Timeline.AudioPlayableAsset)audioTrack.GetClips().Single().asset).clip;
            var decoded=new float[1024];Check(audioClip.GetData(decoded,Mathf.RoundToInt(audioClip.frequency*7)),"Soundtrack decodes correctly");
            Check(decoded.Any(v=>Mathf.Abs(v)>.00001f) && audioClip.length>=25.9f,"Original soundtrack contains non-silent samples and full ending duration");
            Check(director.GetGenericBinding(audioTrack)==audio && audio.isPlaying,"Timeline audio binding and native playback resume after pause");
            yield return Until(()=>game.EndingCutscene.Seconds>14.5); Shot("04_identity");
            Check(game.EndingCutscene.Subtitle==game.StoryContent.dialogue.Single(l=>l.id=="D023").text, "Existing identity dialogue synchronized");
            game.SendMessage("OnApplicationFocus",false); yield return Wait(.15);
            // Automated sessions suppress OS focus changes. Exercise the same pause entry explicitly.
            game.Pause(); yield return Wait(.2); time=game.EndingCutscene.Seconds; yield return Wait(.4);
            Check(Math.Abs(game.EndingCutscene.Seconds-time)<.002, "Second pause keeps middle shot frozen");
            Click("Resume"); yield return Wait(.3);
            yield return Until(()=>game.EndingCutscene.Seconds>20); Shot("05_exit_and_final_line");
            Check(game.EndingCutscene.Subtitle==game.StoryContent.dialogue.Single(l=>l.id=="D024").text, "Existing final choice dialogue synchronized");
            yield return Until(()=>game.Mode==ContestStoryGame.ScreenMode.Ending);
            Check(!game.EndingCutscene.Running && game.CompletionCount("STORY_COMPLETE")==1, "Natural playback reaches ending exactly once"); Shot("06_ending");
            yield return Until(()=>game.CanAcceptUi);Click("EndingCredits"); yield return Wait(.3); Click("Back"); yield return Wait(.3);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending, "Credits return to ending rather than pause menu");
            Click("ReplayEnding"); yield return Wait(1); Click("EndingSkip");
            game.EndingCutscene.Skip(); yield return Wait(.2);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending && game.CompletionCount("STORY_COMPLETE")==1, "Early and repeated skip are idempotent");
            Click("ReplayEnding"); yield return Wait(8); Click("EndingSkip"); yield return Wait(.2);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending && game.CompletionCount("STORY_RESCUE")==1, "Middle-shot skip preserves completion and unique batch");
            Click("ReplayEnding"); yield return Wait(19); Click("EndingSkip"); yield return Wait(.2);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending && game.CompletionCount("STORY_COMPLETE")==1, "Final-shot skip preserves one completion");
            Click("ReplayEnding"); yield return Wait(1); game.Pause(); yield return Wait(.2);
            Click("MainMenu"); yield return Wait(.2); Click("ConfirmYes"); yield return Wait(.3);
            Check(game.Mode==ContestStoryGame.ScreenMode.MainMenu && !game.EndingCutscene.Running && game.view.enabled && !game.EndingCutscene.ShotCamera, "Leaving paused movie cancels director and restores camera");
            Check(package.parent!=packageParent, "Leaving movie restores original unique package parent");
            Click("Continue"); yield return Wait(.3);
            Check(game.Mode==ContestStoryGame.ScreenMode.Ending, "Continue completed checkpoint enters ending without replaying automatically");
            Click("ReplayEnding"); yield return Wait(.7); game.NewGame(true); yield return Wait(.3);
            Check(game.State.stage==0 && game.view.enabled && !game.EndingCutscene.Running, "New game after replay restores scene and gameplay camera");
            Check(errors.Count==0, "No runtime Console errors across playback, pause, skip, replay and restart");
        }
        static void Finish(string failure)
        {
            SessionState.SetBool(Flag,false); InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=MakeCurrent; Application.logMessageReceived-=Log;
            if(originalInputSettings){InputSystem.settings=originalInputSettings;Object.DestroyImmediate(testInputSettings);}
            EditorUtility.audioMasterMute=originalMasterMute;
            Application.runInBackground=SessionState.GetBool(Flag+".background",false);
            held=Array.Empty<Key>();
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
            Directory.CreateDirectory(Out);
            File.WriteAllText(Out+"/Result.json",JsonUtility.ToJson(new Report {passed=failure==null,unity=Application.unityVersion,failure=failure,checks=checks.ToArray(),errors=errors.ToArray(),audioScope="Decoded soundtrack samples, duration, Timeline binding and native playback state verified. This editor session returned zero from mixed-output capture; physical listening quality is not verified."},true));
            Debug.Log("[EndingTest] "+(failure==null?"PASS "+checks.Count:"FAIL "+failure));
            started=false;
            EditorApplication.delayCall+=()=>EditorApplication.Exit(failure==null?0:1);
            EditorApplication.isPlaying=false;
        }
    }
}
