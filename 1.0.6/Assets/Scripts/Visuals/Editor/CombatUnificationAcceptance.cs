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
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
 [InitializeOnLoad]
 public static class CombatUnificationAcceptance
 {
  const string Flag="Combat108.Acceptance",Out="Docs/CombatUnification20261106";
  static BossEncounter e;static PlayerMelee melee;static FirstPersonController player;static RecoveryMenu menu;
  static Keyboard keyboard;static Mouse mouse;static Key[] held=Array.Empty<Key>();static MouseState pointer;
  static IEnumerator<Gate> sequence;static Gate gate;static bool running,finished;static double deadline;
  static readonly List<string> checks=new List<string>(),errors=new List<string>();
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  sealed class Gate{public Func<bool> ready;public float deadline;public Gate(Func<bool> r,float timeout){ready=r;deadline=Time.realtimeSinceStartup+timeout;}}
  [Serializable]class Report{public bool complete;public string failure,scope;public int checksPassed;public string[] checks,errors;}
  static CombatUnificationAcceptance(){EditorApplication.update+=Tick;}
  public static void FinalizeAndRun(){CombatUnificationBuilder.FinalizeView();Run();}
  public static void PatchEffectsAndRun(){CombatUnificationBuilder.PatchEffectMaterials();Run();}
  public static void PrepareAndRun(){CombatUnificationBuilder.Apply();Run();}
  public static void Run(){Directory.CreateDirectory(Out+"/Captures");EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);SessionState.SetBool(Flag,true);SessionState.SetFloat(Flag+"Deadline",(float)EditorApplication.timeSinceStartup+240);var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.position=new Rect(20,20,1300,790);gameView.Show();gameView.Focus();Application.runInBackground=true;EditorApplication.EnterPlaymode();}
  static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
  static void State(BossEncounter.State s)=>Call(e,"SetState",s);
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);Debug.Log("COMBAT108 PASS "+label);}
  static Gate Wait(float s){float until=Time.realtimeSinceStartup+s;return new Gate(()=>Time.realtimeSinceStartup>=until,s+8);}
  static Gate Until(Func<bool> r,float timeout=12)=>new Gate(r,timeout);
  static void Keys(params Key[] keys){held=keys;}
  static void Feed(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic || finished)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));InputSystem.QueueStateEvent(mouse,pointer);}
  static void Current(){if(keyboard!=null&&keyboard.added)keyboard.MakeCurrent();if(mouse!=null&&mouse.added)mouse.MakeCurrent();}
  static void Log(string text,string trace,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!trace.Contains("UnityEditor.Search.SearchDatabase"))errors.Add(text+"\n"+trace);}
  static void Tick()
  {
   if(!SessionState.GetBool(Flag,false))return;
   try{
    if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Flag+"Deadline",0))throw new Exception("Acceptance startup/global deadline");
    if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
    EditorApplication.QueuePlayerLoopUpdate();
    if(!running){e=Object.FindFirstObjectByType<BossEncounter>();if(!e||!e.IsActive)return;Start();}
    e.SendMessage("OnApplicationFocus",true);if(!e.enabled){if(!e.IsPaused)typeof(BossEncounter).GetField("combatTime",Private).SetValue(e,e.BattleTime+Mathf.Min(Time.deltaTime,.1f));e.Combat.Tick(Mathf.Min(Time.deltaTime,.1f));var hud=e.GetComponent<BossCombatHUD>();if(hud)hud.Refresh();}
    if(gate==null){if(!sequence.MoveNext()){Finish(true,"");return;}gate=sequence.Current;}
    if(gate.ready()){gate=null;return;}if(Time.realtimeSinceStartup>gate.deadline)throw new Exception("Timeout after "+checks.LastOrDefault()+" frame="+Time.frameCount+" swings="+melee.SwingSequence+" attack="+player.Bindings.AttackAction.IsPressed()+" cursor="+player.CursorIsLocked+" gameplay="+player.GameplayInputEnabled+" paused="+player.SimulationPaused+" action="+melee.Action+" stamina="+melee.Stamina);
   }catch(Exception ex){Finish(false,ex.ToString());}
  }
  static void Start()
  {
   running=true;finished=false;checks.Clear();errors.Clear();Application.logMessageReceived+=Log;
   player=e.GetComponent<FirstPersonController>();menu=e.GetComponent<RecoveryMenu>();melee=e.GetComponent<PlayerMelee>();
   keyboard=InputSystem.AddDevice<Keyboard>("Combat108Keyboard");mouse=InputSystem.AddDevice<Mouse>("Combat108Mouse");Current();
   var input=(InputActionAsset)typeof(FirstPersonController).GetField("runtimeInputActions",Private).GetValue(player);input.devices=new InputDevice[]{keyboard,mouse};input.RemoveAllBindingOverrides();
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
   QualitySettings.vSyncCount=0;Application.targetFrameRate=60;AudioListener.volume=0;e.definition=Object.Instantiate(e.definition);e.definition.moveSpeed=0;e.definition.repositionTime=10000;e.definition.meleeReach=e.definition.meleeAdvanceDistance=0;e.enabled=true;menu.Resume();State(BossEncounter.State.Observe);
   sequence=Sequence().GetEnumerator();gate=null;
  }
  static void Aim(Vector3 target)
  {
   var q=Quaternion.LookRotation(target-e.View.transform.position);player.transform.rotation=Quaternion.Euler(0,q.eulerAngles.y,0);
   float pitch=Mathf.DeltaAngle(0,q.eulerAngles.x);typeof(FirstPersonController).GetField("pitch",Private).SetValue(player,pitch);
   ((Transform)typeof(FirstPersonController).GetField("cameraPivot",Private).GetValue(player)).localRotation=Quaternion.Euler(pitch,0,0);
  }
  static void Spawn(Vector3 feet){Keys();pointer=new MouseState();Physics.SyncTransforms();Check(player.TryReturnToSpawn(feet,Quaternion.identity),"Safe player placement "+feet);player.SetCursorLocked(true);Physics.SyncTransforms();}
  static void Capture(string name){ScreenCapture.CaptureScreenshot(Out+"/Captures/"+name+".png");}
  static IEnumerable<Gate> Sequence()
  {
   yield return Wait(.6f);
   Check(melee&&melee.loadout&&melee.view,"Scene has 1.0.8 melee loadout and view");
   Check(melee.view.viewCamera==e.View&&melee.view.presentationCamera,"View camera references rewired to main");
   Check(melee.loadout.skills.Length==4&&melee.loadout.skills.All(s=>s.icon&&s.cooldownIcon),"All four skill icons present");
   Check(e.GetComponentsInChildren<SkillCooldownBadge>(true).Length==4,"HUD has exactly four skill badges");
   Check(melee.view.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader&&!m.shader.name.Contains("InternalError"))),"Sword shield hands have valid materials");
   Check(e.PowerNetwork&&e.PowerNetwork.LiveCount==3&&e.SelectedAudio,"Main three-crystal network and selected audio retained");
   yield return Wait(1);Capture("01_combat_hud");yield return Wait(.5f);
   float stamina=melee.Stamina;uint swings=melee.SwingSequence;pointer=new MouseState().WithButton(MouseButton.Left,true);
   yield return Until(()=>melee.SwingSequence>=swings+3,6);pointer=new MouseState();
   Check(melee.Stamina<stamina&&melee.Combo==2,"Held left mouse drives three-part combo and spends stamina");yield return Until(()=>!melee.Busy);
   for(int i=0;i<4;i++)
   {
    melee.ResetCombat();yield return Wait(.25f);Keys(new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4}[i]);
    int index=i;yield return Until(()=>melee.Action==index+1,3);Keys();
    Check(melee.Cooldown(i)>0&&melee.Stamina<melee.loadout.maxStamina,"Key "+(i+1)+" triggers skill with cooldown and shared cost");
    if(i==0){yield return Wait(.15f);Capture("02_skill_heavy");}
    yield return Until(()=>!melee.Busy);
   }
   melee.ResetCombat();yield return Wait(.25f);pointer=new MouseState().WithButton(MouseButton.Right,true);
   yield return Until(()=>e.Combat.Guarding,3);Check(melee.Stamina<100&&Mathf.Abs(e.Combat.Guard-melee.Stamina)<.001f,"Right mouse guard uses the same stamina pool");
   float health=e.Combat.Health;e.Combat.ReceiveHit(20,e.View.transform.position+e.View.transform.forward*2,true,-1,true);
   Check(e.Combat.Health==health&&e.CurrentState==BossEncounter.State.Stagger,"Timed right mouse parries a frontal melee hit");
   pointer=new MouseState();yield return Wait(.3f);Call(e,"TickBoss",e.Definition.staggerDuration+.05f);Check(e.CurrentState==BossEncounter.State.Recovery,"Ordinary melee stagger uses 1.0.8 recovery duration");State(BossEncounter.State.Observe);
   melee.ResetCombat();yield return Wait(.25f);pointer=new MouseState().WithButton(MouseButton.Right,true);yield return Wait(.4f);
   health=e.Combat.Health;stamina=melee.Stamina;e.Combat.ReceiveHit(10,e.View.transform.position+e.View.transform.forward*2,true,0,true);
   Check(e.Combat.Health==health&&melee.Stamina<stamina,"Held shield blocks frontal damage by consuming stamina");
   pointer=new MouseState();yield return Wait(.25f);
   Spawn(e.Arena.transform.position+new Vector3(0,.08f,-6));melee.ResetCombat();yield return Wait(.4f);
   var start=player.transform.position;Keys(Key.LeftShift);yield return Until(()=>player.Dash.DashSequence>0,3);Keys();yield return Wait(.4f);
   Check(Vector3.Distance(start,player.transform.position)>4.2f&&melee.Stamina<100,"Shift performs production collision-swept dash and spends stamina");
   Check(!player.Dash.SlowActive,"Ordinary dash alone grants no perfect-dodge slow time");
   player.Dash.Cancel(true);melee.ResetCombat();yield return Wait(.3f);uint dash=player.Dash.DashSequence;Keys(Key.LeftShift);yield return Until(()=>player.Dash.DashSequence>dash,3);Keys();yield return Wait(.06f);
   player.Dash.ConsiderNearMiss(true,false);player.Dash.ConfirmPerfect();Check(player.Dash.SlowActive&&Time.timeScale<.1f,"Verified near miss activates perfect-dodge slow time");
   yield return Wait(.06f);Capture("03_perfect_dodge");yield return Wait(.9f);Check(!player.Dash.SlowActive&&Mathf.Abs(Time.timeScale-1)<.001f,"Perfect dodge restores global time");
   melee.ResetCombat();yield return Wait(.3f);Keys(Key.Digit1);yield return Until(()=>melee.Cooldown(0)>0,3);Keys();yield return Wait(.2f);
   Keys(Key.I);yield return Until(()=>menu.CurrentPage==RecoveryMenu.Page.Equipment,3);Keys();yield return Wait(.2f);
   float cooldown=melee.Cooldown(0);stamina=melee.Stamina;var pos=player.transform.position;yield return Wait(.6f);
   Check(e.IsPaused&&melee.Cooldown(0)==cooldown&&melee.Stamina==stamina&&Vector3.Distance(pos,player.transform.position)<.001f,"I equipment pauses skill clock stamina and controller");
   Check(menu.GetComponentsInChildren<EquipmentPreview>().Length==1,"Equipment page has live sword and shield preview");Capture("04_equipment");yield return Wait(.4f);
   Keys(Key.I);yield return Until(()=>!menu.IsOpen,3);Keys();yield return Wait(.3f);Check(!e.IsPaused,"I resumes battle");
   Keys(Key.Escape);yield return Until(()=>menu.IsOpen,3);Keys();yield return Wait(.3f);cooldown=melee.Cooldown(0);yield return Wait(.3f);Check(melee.Cooldown(0)==cooldown,"Esc pause also freezes skill cooldown");menu.Resume();yield return Wait(.3f);
   // Real melee strike production path, isolated from autonomous boss attacks.
   var n=e.PowerNetwork;float max=e.Definition.health;health=e.BossHealth;e.DamageBoss(100,false);Check(Mathf.Abs(e.BossHealth-(health-70))<.1f,"Existing 30 percent crystal damage reduction retained");
   health=e.BossHealth;Call(e,"TickCrystalRegeneration",1f);Check(Mathf.Abs(e.BossHealth-health-max*.0045f)<.1f,"Existing crystal healing retained");
   for(int i=0;i<3;i++)
   {
    State(BossEncounter.State.Observe);var target=n.targets[i];var center=e.Arena.transform.position;
    Vector3 away=Vector3.ProjectOnPlane(target.transform.position-center,Vector3.up).normalized;
    Spawn(target.transform.position-away*2.5f-Vector3.up*target.transform.localPosition.y+Vector3.up*.08f);melee.ResetCombat();yield return Wait(.4f);Aim(target.transform.position);
    float chain=n.ChainRemaining(i);Check(e.TryMeleeHit(2.8f,105,30,16,false)&&Mathf.Abs(n.ChainRemaining(i)-(chain-30))<.01f,"Near melee reaches chain "+i);
    chain=n.ChainRemaining(i);Check(!e.TryMeleeHit(.1f,105,30,16,false)&&n.ChainRemaining(i)==chain,"Out-of-reach hit rejected "+i);
    Aim(e.View.transform.position-away*5);Check(!e.TryMeleeHit(2.8f,60,30,16,false)&&n.ChainRemaining(i)==chain,"Backward target outside attack arc rejected "+i);Aim(target.transform.position);
    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=Vector3.Lerp(e.View.transform.position,target.transform.position,.5f);wall.transform.localScale=new Vector3(.6f,3,.6f);Physics.SyncTransforms();
    Check(!e.TryMeleeHit(2.8f,105,30,16,false)&&n.ChainRemaining(i)==chain,"Melee cannot hit crystal through wall "+i);Object.DestroyImmediate(wall);Physics.SyncTransforms();
    pointer=new MouseState().WithButton(MouseButton.Left,true);yield return Until(()=>n.ChainRemaining(i)<=0,8);pointer=new MouseState();
    Check(n.CrystalRemaining(i)==n.crystalHealth,"Actual left mouse breaks chain without overflow "+i);yield return Until(()=>!melee.Busy);melee.RestoreStamina(100);yield return Wait(.3f);
    pointer=new MouseState().WithButton(MouseButton.Left,true);yield return Until(()=>n.IsBroken(i),10);pointer=new MouseState();
    Check(n.LiveCount==2-i&&!target.gameObject.activeSelf,"Actual left mouse destroys crystal and disables its volume "+i);
    if(i==0){Capture("05_crystal_broken");yield return Wait(.35f);}yield return Until(()=>!melee.Busy);
   }
   Check(n.AllBrokenCount==1&&e.CurrentState==BossEncounter.State.Stagger,"All three broken interrupt boss exactly once");
   health=e.BossHealth;Call(e,"TickCrystalRegeneration",5f);Check(e.BossHealth==health&&n.DamageReduction==0,"All broken removes healing and damage reduction");
   Call(e,"TickBoss",n.staggerDuration+.1f);Check(e.CurrentState==BossEncounter.State.Observe,"Crystal stagger preserves main recovery behavior");
   e.Finish(false);e.Retry();State(BossEncounter.State.Observe);yield return Wait(.3f);
   Check(n.LiveCount==3&&n.AllBrokenCount==0&&melee.Stamina==100&&Enumerable.Range(0,4).All(i=>melee.Cooldown(i)==0),"Retry resets crystals stamina skills and one-shot flag");
   Spawn(e.Arena.boss.position+Vector3.back*2.1f);melee.ResetCombat();yield return Wait(.4f);Aim(e.Arena.boss.GetComponent<Collider>().bounds.center);
   health=e.BossHealth;uint hits=melee.HitSequence;pointer=new MouseState().WithButton(MouseButton.Left,true);
   yield return Until(()=>melee.HitSequence>hits,4);pointer=new MouseState();Check(e.BossHealth<health,"Actual sword contact damages Warden through retained crystal DR");yield return Until(()=>!melee.Busy);
   melee.ResetCombat();yield return Wait(.3f);Keys(Key.Digit2);yield return Until(()=>e.CurrentState==BossEncounter.State.Stagger,4);Keys();
   Check(e.StaggerCount>0,"Actual shield skill interrupts Warden via melee stagger");yield return Wait(.3f);
   Check(e.ReturnToLibrary(),"Return to library succeeds with new melee controls");yield return Wait(.3f);Check(!e.PowerNetwork&&melee.view,"Return clears arena network and preserves equipment");
   Check(e.Begin(),"Reentry succeeds");yield return Wait(.4f);Check(e.PowerNetwork.LiveCount==3,"Reentry has fresh crystal network");
   Check(e.Arena.laserEffect.detailMaterial && e.Arena.laserEffect.particleMaterial && e.Arena.fractureEffect.detailMaterial && e.Arena.fractureEffect.particleMaterial,"Legacy laser and fracture prefabs contain all new detail materials");
   Check(e.Definition.spellRibbon && e.Definition.spellParticle && e.Definition.waveSurface && e.Definition.waveSurface.shader.isSupported,"All Warden cast materials and dedicated wave shader connected");
   // Exercise every live Boss attack path, including the newly migrated presentation assets.
   e.definition.playerHealth=10000;e.Combat.ResetCombat(e);State(BossEncounter.State.Observe);
   for(int i=0;i<5;i++)
   {
    Call(e,"ClearAttacks");State(BossEncounter.State.Observe);Spawn(e.Arena.boss.position+new Vector3(0,.08f,-8));yield return Wait(.3f);
    typeof(BossEncounter).GetField("pattern",Private).SetValue(e,i);
    typeof(BossEncounter).GetField("lastSeenAt",Private).SetValue(e,e.BattleTime);
    typeof(BossEncounter).GetField("lastKnownPlayer",Private).SetValue(e,player.transform.position);
    Call(e,"BeginWarning");int expected=new[]{0,3,2,4,1}[i];Check(e.CurrentAttack==expected,"Production attack selection "+expected);
    if(expected==0){yield return Until(()=>e.VolleyReleasedCount>=e.VolleyCount,6);Check(e.VolleyReleasedCount==e.VolleyCount,"Retreat and actual page volley release");}
    else if(expected==3){yield return Until(()=>e.LaserIsFiring,5);Check(e.LaserLength>0,"Actual charged laser path renders");}
    else if(expected==2){yield return Until(()=>e.FractureSpawnCount>0,5);Check(e.FractureSpawnCount>0,"Actual ground fracture emits warnings");}
    else if(expected==4){yield return Until(()=>e.RushTravel>0,5);Check(e.RushTravel>0,"Actual rush moves along swept path");}
    else {yield return Until(()=>e.WaveCharge && e.WaveCharge.ChargeProgress>.3f,5);Capture("06_boss_wave_charge");yield return Until(()=>e.CurrentState==BossEncounter.State.Attack,4);yield return Wait(.3f);Capture("07_boss_wave_release");yield return Wait(.3f);Check(e.ActiveAttackCount>0,"Actual engraved wave release uses dedicated surface");}
    Check(e.Arena.effectRoot.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m && m.shader && m.shader.isSupported)),"Active attack effects contain no missing materials "+expected);
   }
   Call(e,"ClearAttacks");State(BossEncounter.State.Observe);Spawn(e.Arena.boss.position+Vector3.back*2.5f);e.definition.meleeReach=4.8f;e.definition.meleeAdvanceDistance=1.2f;yield return Wait(.3f);
   typeof(BossEncounter).GetField("nextMeleeAt",Private).SetValue(e,0f);Call(e,"BeginWarning");Check(e.CurrentAttack==5,"Production close-range selection uses blade-staff melee");
   yield return Until(()=>e.MeleeHitCount>0,4);Check(e.MeleeHitCount>0,"Actual Warden melee impact resolves");yield return Wait(.3f);
   Check(errors.Count==0,"No runtime errors during combat acceptance");
  }
  static void Finish(bool ok,string failure)
  {
   if(finished)return;finished=true;SessionState.SetBool(Flag,false);
   InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;Application.logMessageReceived-=Log;
   Directory.CreateDirectory(Out);File.WriteAllText(Out+"/runtime-acceptance.json",JsonUtility.ToJson(new Report{complete=ok,failure=failure,checksPassed=checks.Count,checks=checks.ToArray(),errors=errors.ToArray(),scope="Unity Editor Play Mode with virtual Input System keyboard/mouse for combo, four skills, shield, dash, equipment/pause and crystal destruction. Boss movement and attack acquisition disabled on a runtime-only definition clone to isolate inputs. Parry, near-miss, network damage and lifecycle additionally use explicit production-method injection. Not manual playtesting."},true));
   Debug.Log("COMBAT_UNIFICATION_ACCEPTANCE "+ok+" "+failure);EditorApplication.Exit(ok?0:1);
  }
 }
}





