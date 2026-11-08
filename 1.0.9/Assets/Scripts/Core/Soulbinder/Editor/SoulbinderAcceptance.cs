#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ArcaneArchive.Soulbinder.Editor {
public static class SoulbinderAcceptance {
 static int assertions;
 static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Advance(EncounterModel m,float sec){for(int i=0;i<(int)Math.Ceiling(sec/.01f);i++)m.Tick(.01f);}
 static EncounterModel Live(){var m=new EncounterModel();m.Reset();Check(m.Begin(),"Begin");Advance(m,3.52f);return m;}
 public static void Tests(){
  assertions=0;var m=new EncounterModel();m.Reset();Check(!m.Begin(false)&&m.Phase==EncounterPhase.Ready,"Invalid preflight leaves gate open");m.Begin();Advance(m,1.99f);Check(m.Occupied==0,"No staggered intro spawn");m.Tick(.02f);Check(m.Occupied==5&&m.BatchSerial==1,"Five same-frame spawns");foreach(var s in m.Slots)Check(s==SoulSlot.Forming,"Same forming state");m.Damage(2000,200);Check(m.BossHealth==1800&&m.PlayerHealth==100,"Intro invulnerable");Advance(m,1.51f);Check(m.Phase==EncounterPhase.Combat,"Combat unlocked");
  m.Remove(0);m.Remove(2);Advance(m,20.1f);Check(m.Cast==Invocation.Casting&&m.Slots[0]==SoulSlot.Reserved&&m.Slots[2]==SoulSlot.Reserved&&m.Occupied==5,"Atomic missing reservation");m.Remove(1);Check(m.Slots[1]==SoulSlot.Empty,"Midcast death deferred");Advance(m,.9f);m.Damage(80,0);Check(m.Cast==Invocation.Casting,"100 damage no longer interrupts");m.Damage(64,0);Check(m.Cast==Invocation.Stagger&&m.Slots[0]==SoulSlot.Empty&&m.Slots[2]==SoulSlot.Empty,"180 actual channel damage interrupts entire batch");Advance(m,2.51f);Check(m.NextSummon-m.Time>9.9f&&m.NextSummon-m.Time<=10,"Interrupted cooldown only ten");
  m=Live();m.Remove(0);m.Remove(2);Advance(m,20.1f);m.DamageAnchor(0,240);Check(m.Slots[0]==SoulSlot.Empty&&m.Slots[1]==SoulSlot.Alive&&m.Slots[2]==SoulSlot.Reserved,"Source closure selective, alive survives");Advance(m,4.7f);Check(m.Slots[2]==SoulSlot.Forming&&m.BatchSerial==2,"Remaining batch commits once");m.DamageAnchor(1,240);Check(m.Slots[2]==SoulSlot.Empty,"Close cancels forming");Advance(m,2);m.Remove(1);Advance(m,24);Check(m.Slots[0]==SoulSlot.Empty&&m.Slots[1]==SoulSlot.Empty&&m.Slots[2]==SoulSlot.Empty,"Closed sources never refill");
  m=Live();for(int i=0;i<3;i++)m.DamageAnchor(i,240);for(int i=0;i<5;i++)m.Remove(i);Advance(m,90);Check(m.Occupied==0&&m.Cast==Invocation.Idle,"All closed no fake casts");m.Damage(1800,100);Check(m.Phase==EncounterPhase.Lost,"Mutual knockout loses");
  m=Live();m.Damage(1800,0);Check(m.Phase==EncounterPhase.Won&&m.Occupied==0,"Victory peaceful cleanup");m.Damage(0,100);Check(m.Phase==EncounterPhase.Won,"Terminal idempotent");
  for(int k=0;k<10;k++){m.Reset(true);m.Begin();Advance(m,1.52f);Check(m.Occupied==5&&m.PlayerHealth==100&&m.BossHealth==1800&&m.Anchors[0]&&m.BatchSerial==1,"Ten clean retries "+k);}
  m=Live();Advance(m,25);Check(m.BatchSerial==1&&m.Cast==Invocation.Idle,"Full party skips cast");m.Remove(4);for(int i=0;i<400&&m.Cast==Invocation.Idle;i++)m.Tick(.01f);Advance(m,4.61f);Check(m.Occupied==5&&m.Slots[4]==SoulSlot.Forming,"Refill holds cap through forming");Advance(m,1.6f);Check(m.Slots[4]==SoulSlot.Alive,"Forming becomes alive");
  m=Live();m.DamageAnchor(0,160);Check(m.Anchors[0]&&m.AnchorHealth[0]==80,"Anchor requires 240 damage");m.DamageAnchor(0,80);Check(!m.Anchors[0],"Anchor closes at 240");
  m=Live();m.Remove(0);Advance(m,20.1f);Advance(m,3.15f);Check(!m.Channel&&m.Cast==Invocation.Casting,"Channel ends after 2.4 seconds");m.Damage(180,0);Check(m.Cast==Invocation.Casting,"Late damage cannot interrupt release");Advance(m,.8f);Check(m.Slots[0]==SoulSlot.Forming,"Four second summon still commits");
  Directory.CreateDirectory("Docs/SoulbinderCombat20261108/Implementation");File.WriteAllText("Docs/SoulbinderCombat20261108/Implementation/model-tests.txt",assertions+" assertions passed\n");Debug.Log("SOULBINDER_MODEL_PASS "+assertions);
 }
 public static void Run(){try{Tests();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
#endif
