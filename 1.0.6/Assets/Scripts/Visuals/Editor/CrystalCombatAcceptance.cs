using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ArcaneArchive.Core;
using ArcaneArchive.UI;
namespace ArcaneArchive.Visuals.Editor
{
 [InitializeOnLoad]
 public static class CrystalCombatAcceptance
 {
  const string D="Docs/SealedDuelArena/CrystalCombat";static double deadline;static readonly List<string> checks=new List<string>();
  [Serializable]class Report{public bool complete;public string failure,scope;public string[] checks;}
  static CrystalCombatAcceptance(){EditorApplication.update+=Tick;}
  public static void Run(){SealedDuelArenaAcceptance.GuardSceneWork();SessionState.SetBool("CrystalCombatAcceptance",true);EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);deadline=EditorApplication.timeSinceStartup+120;EditorApplication.EnterPlaymode();}
  static void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
  static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
  static void State(BossEncounter e,BossEncounter.State state)=>Call(e,"SetState",state);
  static void Tick()
  {
   if(!SessionState.GetBool("CrystalCombatAcceptance",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   var e=ObjectFind();if(!e || !e.IsActive)return;
   SessionState.SetBool("CrystalCombatAcceptance",false);
   try{Test(e);Finish(true,"");}catch(Exception ex){Finish(false,ex.ToString());}
  }
  static BossEncounter ObjectFind()=>UnityEngine.Object.FindFirstObjectByType<BossEncounter>();
  static void Test(BossEncounter e)
  {
   e.enabled=false;e.GetComponent<RecoveryMenu>().Resume();typeof(BossEncounter).GetField("focusLost",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(e,false);State(e,BossEncounter.State.Observe);
   var n=e.PowerNetwork;Check(n && n.targets.Length==3,"Production Begin initializes exactly three attackable targets");
   float max=e.Definition.health;Check(n.LiveCount==3 && Mathf.Abs(n.DamageReduction-.3f)<.001f,"Three crystals confer additive 30 percent DR");
   e.DamageBoss(100,false);Check(Mathf.Abs(e.BossHealth-(max-70))<.01f,"Boss actual damage path reduces 100 to 70");
   float hp=e.BossHealth;Call(e,"TickCrystalRegeneration",1f);Check(Mathf.Abs(e.BossHealth-hp-max*.0045f)<.01f,"Three crystals regenerate 0.45 percent max health per second");
   e.GetComponent<RecoveryMenu>().Open();hp=e.BossHealth;Call(e,"TickCrystalRegeneration",10f);Check(e.BossHealth==hp&&!n.Hit(0,1000),"Pause blocks regeneration and target damage");e.GetComponent<RecoveryMenu>().Resume();
   State(e,BossEncounter.State.Observe);Call(e,"TickCrystalRegeneration",10000f);Check(e.BossHealth==max,"Regeneration clamps to maximum health");
   Check(!n.Hit(0,float.NaN)&&!n.Hit(-1,20)&&!n.Hit(3,20)&&!n.Hit(0,-20),"Invalid target or damage cannot mutate network");
   // Fire actual production projectiles, step their real physics segment collision against prefab volumes.
   var target=n.targets[0];var origin=e.View.transform.position;var previous=e.View.transform.rotation;e.View.transform.LookAt(target.transform.position);Physics.SyncTransforms();
   e.FirePlayerBolt(10000,false);for(int k=0;k<100;k++)Call(e,"TickShots",.02f);
   Check(n.ChainRemaining(0)==0 && n.CrystalRemaining(0)==n.crystalHealth,"Real projectile breaks chain without damage overflow into crystal");
   Check(n.LiveCount==3 && n.visuals.supply[0].enabled && n.visuals.crystalLights[0].enabled,"Chain break preserves supply, light, regen and DR");
   e.FirePlayerBolt(10000,false);for(int k=0;k<100;k++)Call(e,"TickShots",.02f);
   Check(n.LiveCount==2 && n.IsBroken(0),"Second actual projectile destroys exposed crystal");
   Check(!n.targets[0].gameObject.activeSelf && !n.visuals.supply[0].enabled && n.visuals.supply[1].enabled,"Only destroyed crystal hitbox and supply disabled");
   e.DamageBoss(100,false);Check(Mathf.Abs(e.BossHealth-(max-80))<.01f,"Two crystals reduce actual damage to 80");hp=e.BossHealth;Call(e,"TickCrystalRegeneration",1f);Check(Mathf.Abs(e.BossHealth-hp-max*.003f)<.01f,"Two crystals regenerate two shares");
   n.Hit(1,10000);n.Hit(1,10000);Check(n.LiveCount==1&&Mathf.Abs(n.DamageReduction-.1f)<.001f,"One crystal confers 10 percent DR");hp=e.BossHealth;e.DamageBoss(100,false);Check(Mathf.Abs(hp-e.BossHealth-90)<.01f,"One crystal reduces actual damage to 90");hp=e.BossHealth;Call(e,"TickCrystalRegeneration",1f);Check(Mathf.Abs(e.BossHealth-hp-max*.0015f)<.01f,"One crystal regenerates one share");
   Call(e,"BeginWarning");Check(e.ActiveAttackCount>0 || e.CurrentState==BossEncounter.State.Telegraph,"Production attack windup starts before interruption");
   n.Hit(2,10000);n.Hit(2,10000);Check(n.LiveCount==0 && n.AllBrokenCount==1 && e.CurrentState==BossEncounter.State.Stagger,"Final crystal enters dedicated one-time stagger");Check(e.ActiveAttackCount==0,"All-break cancels live attacks and telegraphs");
   hp=e.BossHealth;Call(e,"TickCrystalRegeneration",10f);Check(e.BossHealth==hp,"All broken means no regeneration");e.DamageBoss(50,false);Check(Mathf.Abs(hp-e.BossHealth-50)<.01f,"All broken means no DR");
   Check(!n.Hit(2,10000)&&n.AllBrokenCount==1,"Repeated hit cannot trigger stagger twice");Call(e,"TickBoss",1f);Check(e.CurrentState==BossEncounter.State.Stagger,"Stagger persists before configured duration");Call(e,"TickBoss",1.1f);Check(e.CurrentState==BossEncounter.State.Observe,"Stagger ends after configured duration");
   e.Finish(false);hp=e.BossHealth;Call(e,"TickCrystalRegeneration",10f);Check(e.BossHealth==hp&&!n.Hit(0,10000),"Finished encounter cannot heal or damage crystals");e.Retry();State(e,BossEncounter.State.Observe);
   Check(n.LiveCount==3 && n.targets.All(t=>t.gameObject.activeSelf) && n.visuals.chains.All(t=>t.gameObject.activeSelf)&&n.AllBrokenCount==0,"Retry restores hitboxes chains crystals and one-shot flag");
   for(int i=0;i<3;i++){n.Hit(i,10000);n.Hit(i,10000);}e.DamageBoss(max*.6f,false);Check(e.Phase==2 && e.CurrentState==BossEncounter.State.Stagger,"Crossing half health during stagger preserves full stagger");Call(e,"TickBoss",2.1f);Check(e.CurrentState==BossEncounter.State.PhaseBreak,"Deferred phase transition follows stagger");
   State(e,BossEncounter.State.Observe);e.DamageBoss(max*3,false);Check(e.CurrentState==BossEncounter.State.Won && e.BossHealth==0,"Death takes priority over phase or stagger");Call(e,"TickCrystalRegeneration",10f);Check(e.BossHealth==0,"Dead boss cannot regenerate");
   e.Retry();Check(n.LiveCount==3,"Retry after victory resets network");Check(e.ReturnToLibrary() && e.PowerNetwork==null,"Return clears encounter network reference");Check(e.Begin(),"Reentry constructs fresh arena network");Check(e.PowerNetwork.LiveCount==3&&!e.PowerNetwork.AllBrokenTriggered,"Fresh arena has initial network state");
   State(e,BossEncounter.State.Observe);e.View.transform.rotation=previous;Check(e.PowerNetwork.targets.All(t=>t.GetComponent<Collider>().isTrigger),"Target volumes do not physically obstruct player movement");
  }
  static void Finish(bool ok,string failure){Directory.CreateDirectory(D);File.WriteAllText(D+"/runtime-acceptance.json",JsonUtility.ToJson(new Report{complete=ok,failure=failure,checks=checks.ToArray(),scope="Unity Play Mode production encounter; direct method-driven time stepping and damage injection, actual FirePlayerBolt + physics spherecast target collisions; not manual player acceptance."},true));Debug.Log("CRYSTAL_COMBAT_ACCEPTANCE "+ok+" "+failure);EditorApplication.Exit(ok?0:1);}
 }
}
