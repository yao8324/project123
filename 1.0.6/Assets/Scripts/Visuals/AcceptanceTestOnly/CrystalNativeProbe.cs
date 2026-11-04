using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
namespace ArcaneArchive.Visuals.Acceptance
{
 public sealed class CrystalNativeProbe:MonoBehaviour
 {
  bool running,done;string output;BossEncounter e;FirstPersonController player;Keyboard keyboard;Mouse mouse;MouseState pointer;Key[] keys=Array.Empty<Key>();
  readonly List<string> checks=new List<string>(),errors=new List<string>();
  [Serializable]class Report{public bool complete;public string failure,scope;public string[] checks,errors;}
  void Awake(){var args=Environment.GetCommandLineArgs();if(!args.Contains("-crystal-smoke")){enabled=false;return;}running=true;int i=Array.IndexOf(args,"-crystal-output");output=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"CrystalNative");Directory.CreateDirectory(output);Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Application.logMessageReceived+=Log;}
  void Log(string condition,string trace,LogType type){if(running&&(type==LogType.Error||type==LogType.Exception)){errors.Add(condition+"\n"+trace);Finish(false,condition);}}
  void Check(bool ok,string s){if(!ok){Finish(false,s);throw new Exception(s);}checks.Add(s);}
  void Feed(){if(running&&keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,pointer);}}
  void Current(){if(keyboard!=null&&keyboard.added)keyboard.MakeCurrent();if(mouse!=null&&mouse.added)mouse.MakeCurrent();}
  void Aim(Vector3 target){var q=Quaternion.LookRotation(target-e.View.transform.position);player.transform.rotation=Quaternion.Euler(0,q.eulerAngles.y,0);float pitch=Mathf.DeltaAngle(0,q.eulerAngles.x);typeof(FirstPersonController).GetField("pitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,pitch);((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)).localRotation=Quaternion.Euler(pitch,0,0);}
  IEnumerator Start()
  {
   if(!running)yield break;e=GetComponent<BossEncounter>();player=GetComponent<FirstPersonController>();
   float until=Time.realtimeSinceStartup+30;while(!e.IsActive&&Time.realtimeSinceStartup<until)yield return null;Check(e.IsActive&&e.PowerNetwork,"Native battle initializes crystal network");
   keyboard=InputSystem.AddDevice<Keyboard>("CrystalProbeKeyboard");mouse=InputSystem.AddDevice<Mouse>("CrystalProbeMouse");Current();
   var bindings=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);bindings.devices=new InputDevice[]{keyboard,mouse};bindings.RemoveAllBindingOverrides();InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
   // Isolate crystal interaction from enemy timing, without bypassing player input or projectile hit detection.
   e.definition=Instantiate(e.definition);e.definition.repositionTime=120;
   yield return new WaitForSecondsRealtime(3);e.SendMessage("OnApplicationFocus",true);GetComponent<RecoveryMenu>().Resume();Check(e.CanFight,"Native controls ready");
   var n=e.PowerNetwork;var sound=e.SelectedAudio;Check(sound && sound.ActiveChainLoops==3 && sound.ActiveCrystalLoops==3,"Six independent ambience loops start");ScreenCapture.CaptureScreenshot(Path.Combine(output,"01_intact_native.png"));yield return new WaitForSecondsRealtime(.3f);
   for(int i=0;i<3;i++){
    Aim(n.targets[i].transform.position);yield return null;pointer=new MouseState().WithButton(MouseButton.Left,true);until=Time.realtimeSinceStartup+12;
    while(n.ChainRemaining(i)>0&&Time.realtimeSinceStartup<until)yield return null;
    pointer=new MouseState();yield return null;
    Check(n.ChainRemaining(i)==0&&n.CrystalRemaining(i)>0,"Real mouse firing breaks protective chain first "+i);
    Check(sound.ActiveChainLoops==2-i && sound.ActiveCrystalLoops==3-i,"Chain break stops only its friction loop "+i);
    if(i==0){ScreenCapture.CaptureScreenshot(Path.Combine(output,"02_chain_broken_native.png"));yield return new WaitForSecondsRealtime(.2f);}
    Aim(n.targets[i].transform.position);pointer=new MouseState().WithButton(MouseButton.Left,true);until=Time.realtimeSinceStartup+12;
    while(!n.IsBroken(i)&&Time.realtimeSinceStartup<until)yield return null;pointer=new MouseState();yield return null;
    Check(n.IsBroken(i)&&n.LiveCount==2-i,"Real mouse firing destroys exposed crystal "+i);
    Check(sound.ActiveCrystalLoops==2-i,"Crystal break stops only its resonance "+i);
    Check(Mathf.Abs(n.DamageReduction-(2-i)*.1f)<.001f,"Native damage reduction steps down "+i);
   }
   Check(e.CurrentState==BossEncounter.State.Stagger&&n.AllBrokenCount==1,"Native all-broken triggers one stagger");ScreenCapture.CaptureScreenshot(Path.Combine(output,"03_all_broken_native.png"));yield return new WaitForSecondsRealtime(2.4f);Check(e.CurrentState==BossEncounter.State.Observe,"Native stagger recovers after duration");
   e.DamageBoss(e.Definition.health*2,false);Check(e.IsFinished,"Finish still works");e.Retry();yield return null;Check(e.PowerNetwork.LiveCount==3&&e.PowerNetwork.targets.All(t=>t.gameObject.activeSelf),"Native retry restores three targets");Check(e.SelectedAudio.ActiveChainLoops==3&&e.SelectedAudio.ActiveCrystalLoops==3,"Retry restores six ambience loops");Check(e.ReturnToLibrary(),"Native return succeeds");yield return null;Check(!e.PowerNetwork,"Native return clears network");Finish(true,"");
  }
  void Finish(bool ok,string reason){if(done)return;done=true;running=false;File.WriteAllText(Path.Combine(output,"NativeCrystal.json"),JsonUtility.ToJson(new Report{complete=ok,failure=reason,checks=checks.ToArray(),errors=errors.ToArray(),scope="Native Mac build, Input System virtual mouse held to fire normal production projectiles. Boss reposition delay extended to isolate interactions; end/retry use explicit damage injection. No manual acceptance implied."},true));InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;Application.logMessageReceived-=Log;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);Application.Quit(ok?0:1);}
 }
}
