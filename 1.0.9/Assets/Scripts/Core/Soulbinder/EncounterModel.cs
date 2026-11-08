using System;
namespace ArcaneArchive.Soulbinder
{
 public enum EncounterPhase { Ready, Intro, Combat, Won, Lost }
 public enum SoulSlot { Empty, Reserved, Forming, Alive }
 public enum Invocation { Idle, Casting, Recovery, Stagger }
 // Authoritative state. No Unity clock, objects, randoms or I/O: pause means no Tick.
 public sealed class EncounterModel
 {
  public const float AnchorMaxHealth=240, InterruptThreshold=180, ChannelEnd=3.2f, SummonCastDuration=4f;
  public float InterruptProgress=>channelDamage;
  public readonly SoulSlot[] Slots=new SoulSlot[5];
  public readonly bool[] Anchors={true,true,true};
  public readonly float[] AnchorHealth={AnchorMaxHealth,AnchorMaxHealth,AnchorMaxHealth};
  public static readonly int[] Source={0,0,1,2,2};
  public EncounterPhase Phase {get;private set;}
  public Invocation Cast {get;private set;}
  public float Time {get;private set;}
  public float BossHealth {get;private set;}=1800;
  public float PlayerHealth {get;private set;}=100;
  public float CastTime {get;private set;}
  public float NextSummon {get;private set;}
  public int BatchSerial {get;private set;}
  public int Occupied {get {int n=0;foreach(var s in Slots)if(s!=SoulSlot.Empty)n++;return n;}}
  public bool HeavyLocked => Phase==EncounterPhase.Intro || Cast!=Invocation.Idle;
  public bool Channel => Cast==Invocation.Casting && CastTime>=.8f && CastTime<ChannelEnd;
  float introCommit, introEnd, channelDamage, formingEnd;
  bool introDone;
  public void Reset(bool skip=false)
  {
   Time=0;BossHealth=1800;PlayerHealth=100;Phase=EncounterPhase.Ready;Cast=Invocation.Idle;
   CastTime=0;NextSummon=0;channelDamage=0;formingEnd=0;introDone=false;BatchSerial=0;
   for(int i=0;i<5;i++)Slots[i]=SoulSlot.Empty;
   for(int i=0;i<3;i++){Anchors[i]=true;AnchorHealth[i]=AnchorMaxHealth;}
   introCommit=skip?0:2;introEnd=skip?1.5f:3.5f;
  }
  public bool Begin(bool valid=true){if(!valid||Phase!=EncounterPhase.Ready)return false;Phase=EncounterPhase.Intro;return true;}
  public void Tick(float dt,bool externalHeavy=false)
  {
   if(dt<=0||Phase==EncounterPhase.Ready||Phase==EncounterPhase.Won||Phase==EncounterPhase.Lost)return;
   Time+=dt;
   if(Phase==EncounterPhase.Intro){
    if(!introDone&&Time>=introCommit){for(int i=0;i<5;i++)Slots[i]=SoulSlot.Forming;introDone=true;BatchSerial++;}
    if(Time>=introEnd){for(int i=0;i<5;i++)Slots[i]=SoulSlot.Alive;Phase=EncounterPhase.Combat;NextSummon=Time+20;}
    return;
   }
   if(formingEnd>0&&Time>=formingEnd){for(int i=0;i<5;i++)if(Slots[i]==SoulSlot.Forming)Slots[i]=SoulSlot.Alive;formingEnd=0;}
   if(Cast!=Invocation.Idle){
    CastTime+=dt;
    if(Cast==Invocation.Casting&&CastTime>=SummonCastDuration){
     for(int i=0;i<5;i++)if(Slots[i]==SoulSlot.Reserved)Slots[i]=SoulSlot.Forming;
     BatchSerial++;formingEnd=Time+1.5f;Cast=Invocation.Recovery;CastTime=0;
    }else if(Cast==Invocation.Recovery&&CastTime>=1.6f){Cast=Invocation.Idle;NextSummon=Time+20;CastTime=0;}
    else if(Cast==Invocation.Stagger&&CastTime>=2.5f){Cast=Invocation.Idle;NextSummon=Time+10;CastTime=0;}
   }else if(Time>=NextSummon&&!externalHeavy){
    bool any=false;
    for(int i=0;i<5;i++)if(Anchors[Source[i]]&&Slots[i]==SoulSlot.Empty){Slots[i]=SoulSlot.Reserved;any=true;}
    if(any){Cast=Invocation.Casting;CastTime=0;channelDamage=0;}else NextSummon=Time+3;
   }
  }
  public void Remove(int i){if(i>=0&&i<5)Slots[i]=SoulSlot.Empty;}
  public void DamageAnchor(int i,float amount)
  {
   if(Phase!=EncounterPhase.Combat||i<0||i>=3||!Anchors[i]||amount<=0)return;
   AnchorHealth[i]=Math.Max(0,AnchorHealth[i]-amount);
   if(AnchorHealth[i]>0)return;
   Anchors[i]=false;PlayerHealth=Math.Min(100,PlayerHealth+10);
   for(int n=0;n<5;n++)if(Source[n]==i&&(Slots[n]==SoulSlot.Reserved||Slots[n]==SoulSlot.Forming))Slots[n]=SoulSlot.Empty;
   if(Cast==Invocation.Casting){bool pending=false;foreach(var s in Slots)pending|=s==SoulSlot.Reserved;
    if(!pending){Cast=Invocation.Recovery;CastTime=0;}}
  }
  // Resolve both damage sources before terminal decision: mutual knockout is a loss.
  public void Damage(float boss,float player)
  {
   if(Phase!=EncounterPhase.Combat)return;
   float dealt=Math.Max(0,boss)*(Channel?1.25f:1);
   BossHealth=Math.Max(0,BossHealth-dealt);PlayerHealth=Math.Max(0,PlayerHealth-Math.Max(0,player));
   if(PlayerHealth<=0){End(EncounterPhase.Lost);return;}
   if(BossHealth<=0){End(EncounterPhase.Won);return;}
   if(Channel){channelDamage+=dealt;if(channelDamage>=InterruptThreshold){
    for(int i=0;i<5;i++)if(Slots[i]==SoulSlot.Reserved||Slots[i]==SoulSlot.Forming)Slots[i]=SoulSlot.Empty;
    Cast=Invocation.Stagger;CastTime=0;formingEnd=0;
   }}
  }
  public void RestoreVictory(){End(EncounterPhase.Won);BossHealth=0;}
  void End(EncounterPhase p){Phase=p;Cast=Invocation.Idle;for(int i=0;i<5;i++)Slots[i]=SoulSlot.Empty;}
 }
}
