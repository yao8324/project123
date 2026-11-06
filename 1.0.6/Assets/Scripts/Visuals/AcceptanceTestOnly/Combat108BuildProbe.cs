using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
namespace ArcaneArchive.Visuals.Acceptance
{
 // Opt-in native executable verification; never runs during normal play.
 public sealed class Combat108BuildProbe:MonoBehaviour
 {
  BossEncounter encounter;FirstPersonController player;PlayerMelee melee;RecoveryMenu menu;
  Keyboard keyboard;Mouse mouse;Key[] keys=Array.Empty<Key>();MouseState pointer;
  readonly List<string> checks=new List<string>(),errors=new List<string>();string output;float deadline;bool done;
  [Serializable]class Report{public bool complete;public string failure,scope;public int checksPassed;public string[] checks,errors;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch()
  {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-combat108-smoke")>=0){var go=new GameObject("Combat108BuildProbe");DontDestroyOnLoad(go);go.AddComponent<Combat108BuildProbe>();}}
  void Awake()
  {
   var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-combat108-output");output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"Combat108Smoke");
   Directory.CreateDirectory(output);Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   Application.targetFrameRate=60;QualitySettings.vSyncCount=0;AudioListener.volume=0;deadline=Time.realtimeSinceStartup+90;Application.logMessageReceived+=Log;
  }
  void Log(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(text+"\n"+trace);}
  void Update(){if(done)return;if(encounter){encounter.SendMessage("OnApplicationFocus",true);if(menu&&!menu.IsOpen)player.SetCursorLocked(true);}if(Time.realtimeSinceStartup>deadline)Finish(false,"Native input/render smoke timed out");}
  void Feed(){if(done||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,pointer);}
  void Current(){if(keyboard!=null&&keyboard.added)keyboard.MakeCurrent();if(mouse!=null&&mouse.added)mouse.MakeCurrent();}
  bool Check(bool ok,string label){if(!ok){Finish(false,label);return false;}checks.Add(label);return true;}
  IEnumerator Start()
  {
   yield return null;SceneManager.LoadScene("SealedDuelArena_Showcase");yield return null;
   while(!(encounter=FindFirstObjectByType<BossEncounter>())||!encounter.IsActive)yield return null;
   player=encounter.GetComponent<FirstPersonController>();melee=encounter.GetComponent<PlayerMelee>();menu=encounter.GetComponent<RecoveryMenu>();
   encounter.definition=Instantiate(encounter.definition);encounter.definition.moveSpeed=0;encounter.definition.repositionTime=10000;encounter.definition.meleeReach=encounter.definition.meleeAdvanceDistance=0;
   keyboard=InputSystem.AddDevice<Keyboard>("Combat108NativeKeyboard");mouse=InputSystem.AddDevice<Mouse>("Combat108NativeMouse");Current();
   var input=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);input.devices=new InputDevice[]{keyboard,mouse};input.RemoveAllBindingOverrides();InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
   while(encounter.BattleTime<2.7f)yield return null;yield return new WaitForSecondsRealtime(.5f);
   if(!Check(melee&&melee.view&&encounter.PowerNetwork&&encounter.PowerNetwork.LiveCount==3,"Native arena loads sword/shield and three crystals"))yield break;
   if(!Check(encounter.GetComponentsInChildren<SkillCooldownBadge>().Length==4,"Native HUD contains four skill badges"))yield break;
   if(!Check(melee.view.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported)),"Native sword/shield materials and shaders supported"))yield break;
   ScreenCapture.CaptureScreenshot(Path.Combine(output,"01_native_hud.png"));yield return new WaitForSecondsRealtime(.4f);
   uint swing=melee.SwingSequence;pointer=new MouseState().WithButton(MouseButton.Left,true);yield return new WaitForSecondsRealtime(.75f);pointer=new MouseState();
   if(!Check(melee.SwingSequence>swing&&melee.Stamina<100,"Native left mouse swings sword and spends stamina"))yield break;while(melee.Busy)yield return null;yield return new WaitForSecondsRealtime(.2f);
   for(int i=0;i<4;i++)
   {
    melee.RestoreStamina(100);keys=new[]{new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4}[i]};yield return new WaitForSecondsRealtime(.12f);keys=Array.Empty<Key>();
    if(!Check(melee.Cooldown(i)>0,"Native keyboard skill "+(i+1)+" starts cooldown"))yield break;while(melee.Busy)yield return null;yield return new WaitForSecondsRealtime(.2f);
   }
   keys=new[]{Key.I};yield return new WaitForSecondsRealtime(.12f);keys=Array.Empty<Key>();yield return new WaitForSecondsRealtime(.3f);
   if(!Check(menu.CurrentPage==RecoveryMenu.Page.Equipment,"Native I key opens equipment"))yield break;float cooldown=melee.Cooldown(3);yield return new WaitForSecondsRealtime(.5f);
   if(!Check(encounter.IsPaused&&melee.Cooldown(3)==cooldown,"Native equipment freezes skill cooldown"))yield break;ScreenCapture.CaptureScreenshot(Path.Combine(output,"02_native_equipment.png"));yield return new WaitForSecondsRealtime(.3f);
   keys=new[]{Key.I};yield return new WaitForSecondsRealtime(.12f);keys=Array.Empty<Key>();yield return new WaitForSecondsRealtime(.3f);if(!Check(!menu.IsOpen,"Native I key resumes combat"))yield break;
   melee.RestoreStamina(100);var start=player.transform.position;keys=new[]{Key.LeftShift};yield return new WaitForSecondsRealtime(.12f);keys=Array.Empty<Key>();yield return new WaitForSecondsRealtime(.4f);
   if(!Check(player.Dash.DashSequence>0&&Vector3.Distance(start,player.transform.position)>4,"Native Shift performs dash"))yield break;
   if(!Check(errors.Count==0,"Native smoke has no runtime errors"))yield break;Finish(true,"");
  }
  void Finish(bool ok,string failure)
  {
   if(done)return;done=true;InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;Application.logMessageReceived-=Log;
   File.WriteAllText(Path.Combine(output,"native-smoke.json"),JsonUtility.ToJson(new Report{complete=ok,failure=failure,checksPassed=checks.Count,checks=checks.ToArray(),errors=errors.ToArray(),scope="Windows standalone executable, opt-in automated virtual Input System keyboard/mouse and rendered screenshots. Runtime-only AI settings isolate control smoke. No manual playtest claimed."},true));Application.Quit(ok?0:1);
  }
 }
}
