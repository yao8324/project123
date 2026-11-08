using System;
using System.Collections.Generic;
using System.IO;
using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace ArcaneArchive.Soulbinder {
 public sealed class SoulbinderLevel : MonoBehaviour {
  public FirstPersonController player;
  public Camera view;
  public Transform boss;
  public GameObject[] minionModels;
  public Transform[] anchorLocks;
  public GameObject[] stairBarriers;
  public Transform console;
  public Font font;
  public Material violet, gold, red, cyan;
  public AudioClip castSound, hitSound, breakSound;
  public readonly EncounterModel Model=new EncounterModel();
  public SoulbinderActor[] Actors=new SoulbinderActor[5];
  public static readonly Vector3[] SpawnPoints={new Vector3(-7.8f,.15f,-1.2f),new Vector3(-5,.15f,-3),new Vector3(7.8f,.15f,-1.2f),new Vector3(-1.7f,.15f,8),new Vector3(1.7f,.15f,8)};
  public static readonly string[] Names={"砍刀兵","剑盾兵","弓箭手","重锤兵","幽灵"};
  public float Power {get;private set;}
  public float Shield {get;private set;}=100;
  public bool Paused {get;private set;}
  public bool Started {get;private set;}
  public bool ConsoleOpen {get;private set;}
  public bool Completed {get;private set;}
  bool CampaignMode=>ArcaneArchive.Core.SoulbinderCampaignBridge.Active;
  public bool Automated;
  public string SaveOverride;
  public string Message {get;private set;}="关闭三座锚点，切断补召。击败缚魂者后前往北侧控制台。";
  public string SaveError {get;private set;}
  public SoulbinderRecord Record {get;private set;}
  public int SimultaneousSpawnFrame {get;private set;}=-1;
  public float BattleTime=>Model.Time;
  readonly List<SoulArrow> arrows=new List<SoulArrow>();
  readonly List<Transient> effects=new List<Transient>();
  readonly HashSet<int> melee=new HashSet<int>();
  AudioSource audioSource;
  int heavyOwner=-1,lastBatch;
  float nextMelee,fireAt,absorbAt,absorbUntil,hurtUntil,shieldBrokenUntil,regenAt;
  float pendingBoss,pendingPlayer,sweepAt,sweepClock,sweepProximity;
  Vector3 sweepFacing,magicAim;
  float magicClock,magicAt;
  bool magicReleased;
  LineRenderer[] magicWarnings;
  public int MagicVolleyCount {get;private set;}
  public int MagicBoltsFired {get;private set;}
  public int ActiveMagicBolts=>arrows.FindAll(a=>a.bossMagic).Count;
  public bool MagicCharging=>magicClock>0&&!magicReleased;
  bool guarding,releaseControls,focusPaused,terminalHandled,continueUnsaved;
  float elapsed,shakeUntil;
  Vector3 cameraBase;
  string savePath=>SaveOverride??Path.Combine(Application.persistentDataPath,"soulbinder-v2.json");
  class SoulArrow {public GameObject visual;public Vector3 pos,velocity;public float life,damage;public bool bossMagic;}
  class Transient {public GameObject obj;public float until;}
  void Start(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;cameraBase=view.transform.localPosition;Model.Reset();player.SetGameplayInputEnabled(false);player.SetCursorLocked(false);player.enabled=false;SoulbinderCheckpoint.Read(savePath,out var r,out var err);Record=r;SaveError=err;if(CampaignMode)Begin();}
  public bool Begin(bool skip=false){
   if(!Preflight(out string error)){Message=error;return false;}
   ClearBattle();Model.Reset(skip);Model.Begin();lastBatch=0;Power=0;Shield=100;elapsed=0;fireAt=absorbAt=absorbUntil=hurtUntil=shieldBrokenUntil=regenAt=0;
   magicClock=0;magicAt=8;magicReleased=false;MagicVolleyCount=0;MagicBoltsFired=0;magicWarnings=null;
   sweepAt=6;sweepClock=0;sweepProximity=0;heavyOwner=-1;melee.Clear();nextMelee=0;pendingBoss=pendingPlayer=0;
   Started=true;Paused=false;Completed=false;ConsoleOpen=false;terminalHandled=false;continueUnsaved=false;SaveError=null;
   player.enabled=true;player.TryReturnToSpawn(new Vector3(0,.2f,-11.5f),Quaternion.identity);player.SetGameplayInputEnabled(true);player.SetCursorLocked(true);player.WaitForReleasedControls();releaseControls=true;
   boss.gameObject.SetActive(true);boss.position=new Vector3(0,0,5.7f);boss.rotation=Quaternion.Euler(0,180,0);
   foreach(var b in stairBarriers)b.SetActive(true);foreach(var a in anchorLocks)a.gameObject.SetActive(true);
   Message="五座召唤印将同时成形 · 准备战斗";return true;
  }
  public bool Preflight(out string error){
   error=null;if(!player||!view||!boss||minionModels==null||minionModels.Length!=5||anchorLocks==null||anchorLocks.Length!=3||!console){error="关卡配置缺失：未封门，请检查引用。";return false;}
   for(int i=0;i<5;i++)if(!minionModels[i]||!NavMesh.SamplePosition(SpawnPoints[i],out var h,1,NavMesh.AllAreas)||Vector3.Distance(h.position,new Vector3(0,0,-11.5f))<6||StaticBlocked(h.position)){error="召唤落点不可用："+Names[i];return false;}
   return true;
  }
  public void ContinueSaved(){if(!SoulbinderCheckpoint.Valid(Record))return;ClearBattle();Model.Reset();Model.RestoreVictory();Started=true;Completed=false;terminalHandled=true;Paused=false;player.enabled=true;player.TryReturnToSpawn(new Vector3(0,.2f,9),Quaternion.identity);player.SetGameplayInputEnabled(true);player.SetCursorLocked(true);boss.gameObject.SetActive(false);foreach(var b in stairBarriers)b.SetActive(false);foreach(var a in anchorLocks)a.gameObject.SetActive(false);Message="战斗已结束 · 前往北侧控制台处理灵魂契约";}
  public void TogglePause(){if(!Started||Completed||ConsoleOpen||Model.Phase==EncounterPhase.Lost)return;Paused=!Paused;if(Paused)foreach(var a in Actors)if(a&&a.agent&&a.agent.isOnNavMesh)a.agent.isStopped=true;SetInput(!Paused);if(!Paused)releaseControls=true;}
  void SetInput(bool active){player.enabled=active;player.SetGameplayInputEnabled(active);player.SetCursorLocked(active);player.WaitForReleasedControls();}
  void OnApplicationFocus(bool hasFocus){if(Automated)return;if(!hasFocus&&Started&&!Paused&&!Completed&&!ConsoleOpen&&Model.Phase!=EncounterPhase.Lost){focusPaused=true;TogglePause();}else if(hasFocus&&focusPaused){focusPaused=false;Message="已暂停 · 点击继续后恢复战斗";}}
  void Update(){
   if(!player||!view||Automated)return;
   if(Started&&!Completed&&!ConsoleOpen&&player.Bindings!=null&&player.Bindings.MenuAction.WasPressedThisFrame())TogglePause();
   if(!Started||Paused||Completed||ConsoleOpen||Model.Phase==EncounterPhase.Lost)return;
   Step(Time.deltaTime,!Automated);
  }
  public void Step(float dt,bool input){
   if(dt<=0||Paused||Completed||ConsoleOpen)return;
   elapsed+=dt;
   for(int i=effects.Count-1;i>=0;i--)if(elapsed>=effects[i].until){if(effects[i].obj)Retire(effects[i].obj);effects.RemoveAt(i);}
   var previousPhase=Model.Phase;Model.Tick(dt,heavyOwner!=-1);if(previousPhase==EncounterPhase.Intro&&Model.Phase==EncounterPhase.Combat)Message="战斗开始 · 关闭锚点阻止补召，攻击缚魂者结束战斗";
   SyncActors();
   if(Model.Phase==EncounterPhase.Won||Model.Phase==EncounterPhase.Lost)HandleTerminal();
   if(Model.Phase==EncounterPhase.Intro){var p=player.transform.position;p.x=Mathf.Clamp(p.x,-2,2);p.z=Mathf.Clamp(p.z,-13,-10);if((p-player.transform.position).sqrMagnitude>.001f)player.TryReturnToSpawn(p,player.transform.rotation);Message="五类军团同时降临 · 成形前双方无敌";return;}
   if(Model.Phase==EncounterPhase.Won){if(input)Interact();return;}
   if(Model.Phase!=EncounterPhase.Combat)return;
   if(input)ReadCombatInput();
   if(guarding){Shield=Mathf.Max(0,Shield-18*dt);regenAt=elapsed+2.5f;if(Shield<=0)BreakShield();}else if(elapsed>=regenAt)Shield=Mathf.Min(100,Shield+12*dt);
   TickBoss(dt);
   for(int i=0;i<5;i++)if(Actors[i])Actors[i].Step(dt);
   TickArrows(dt);
   Model.Damage(pendingBoss,pendingPlayer);pendingBoss=pendingPlayer=0;
   if(Model.Phase==EncounterPhase.Won||Model.Phase==EncounterPhase.Lost)HandleTerminal();
   view.transform.localPosition=cameraBase+(elapsed<shakeUntil?UnityEngine.Random.insideUnitSphere*.025f:Vector3.zero);
  }
  void ReadCombatInput(){
   var b=player.Bindings;if(b==null||b.SuppressInputThisFrame||!player.CursorIsLocked)return;
   if(releaseControls){guarding=false;if(!b.AttackAction.IsPressed()&&!b.AbsorbAction.IsPressed())releaseControls=false;return;}
   guarding=b.AbsorbAction.IsPressed()&&elapsed>=shieldBrokenUntil;
   if(b.AbsorbAction.WasPressedThisFrame())TryAbsorb();
   if(b.AttackAction.IsPressed()&&!guarding&&elapsed>=fireAt){fireAt=elapsed+.32f;Fire();}
   Interact();
  }
  public bool TryAbsorb(){if(Model.Phase!=EncounterPhase.Combat||elapsed<absorbAt||Shield<14||elapsed<shieldBrokenUntil)return false;absorbAt=elapsed+2.5f;absorbUntil=elapsed+.65f;Shield-=14;regenAt=elapsed+2.5f;Ring(player.transform.position+Vector3.up*.1f,1.7f,cyan,.65f);return true;}
  void Interact(){if(Model.Phase==EncounterPhase.Won&&Vector3.Distance(player.transform.position,console.position)<3.5f&&player.Bindings.InteractAction.WasPressedThisFrame()){ConsoleOpen=true;SetInput(false);}}
  public void Fire(){
   if(Model.Phase!=EncounterPhase.Combat)return;
   bool empowered=Power>=100;if(empowered)Power=0;
   float amount=empowered?80:20;Vector3 start=view.transform.position+view.transform.forward*.5f;
   Vector3 end=start+view.transform.forward*45;
   if(Physics.Raycast(view.transform.position,view.transform.forward,out var hit,45,~0,QueryTriggerInteraction.Collide)){
    end=hit.point;var target=hit.collider.GetComponent<SoulbinderTarget>();if(target)HitTarget(target.kind,amount,empowered,player.transform.position);
   }
   Beam(start,end,empowered?gold:cyan,empowered?.08f:.025f,.1f);Play(hitSound,.18f);
  }
  public void HitTarget(int kind,float damage,bool empowered,Vector3 attacker){
   if(Model.Phase!=EncounterPhase.Combat)return;
   if(kind==-1){pendingBoss+=damage;Ring(boss.position+.1f*Vector3.up,.65f,empowered?gold:violet,.18f);}
   else if(kind>=10){int a=kind-10;bool before=Model.Anchors[a];Model.DamageAnchor(a,damage);if(before&&!Model.Anchors[a]){Message="锚点关闭 · 停止该来源补召 · 恢复10生命";Play(breakSound,.6f);Ring(anchorLocks[a].position,1.5f,gold,1);anchorLocks[a].gameObject.SetActive(false);SyncActors();}}
   else if(kind>=0&&kind<5&&Actors[kind]&&Model.Slots[kind]==SoulSlot.Alive)Actors[kind].Hit(damage,empowered,attacker);
  }
  void SyncActors(){
   Vector3[] introPositions=null;
   if(Model.Phase==EncounterPhase.Intro&&lastBatch!=Model.BatchSerial){
    introPositions=new Vector3[5];bool valid=true;for(int i=0;i<5;i++)valid&=LegalSpawn(i,out introPositions[i]);
    for(int i=0;i<5;i++)for(int j=0;j<i;j++)if(Vector3.Distance(introPositions[i],introPositions[j])<1.3f)valid=false;
    if(!valid){ClearBattle();Model.Reset();Started=false;SetInput(false);foreach(var b in stairBarriers)b.SetActive(false);Message="开场落点被阻挡：五类召唤整批撤销，请重新进入。";return;}
   }
   if(lastBatch!=Model.BatchSerial){lastBatch=Model.BatchSerial;SimultaneousSpawnFrame=Time.frameCount;Play(castSound,.55f);}
   for(int i=0;i<5;i++){
    var slot=Model.Slots[i];
    if((slot==SoulSlot.Forming||slot==SoulSlot.Alive)&&!Actors[i]){
     Vector3 pos;bool legal=introPositions!=null;if(legal)pos=introPositions[i];else legal=LegalSpawn(i,out pos);if(!legal){Model.Remove(i);continue;}
     var go=new GameObject(Names[i]);go.transform.position=pos;var a=go.AddComponent<SoulbinderActor>();Actors[i]=a;a.Initialize(this,i,minionModels[i]);Ring(pos+Vector3.up*.04f,.9f,violet,1.5f);
    }
    if(slot==SoulSlot.Empty&&Actors[i]){Release(Actors[i].Index);Retire(Actors[i].gameObject);Actors[i]=null;}
   }
  }
  bool StaticBlocked(Vector3 p){foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.7f,.4f,~0,QueryTriggerInteraction.Ignore))if(!c.transform.IsChildOf(player.transform))return true;return false;}
  bool LegalSpawn(int i,out Vector3 p){
   p=SpawnPoints[i];for(int n=0;n<12;n++){
    var v=SpawnPoints[i]+new Vector3(Mathf.Sin(n*2.4f),0,Mathf.Cos(n*2.4f))*(n==0?0:.6f+n*.18f);
    if(!NavMesh.SamplePosition(v,out var h,.8f,NavMesh.AllAreas))continue;
    if(StaticBlocked(h.position))continue;
    if(Vector3.Distance(h.position,player.transform.position)<(Model.Phase==EncounterPhase.Intro?6:2.5f))continue;
    bool overlap=false;for(int j=0;j<5;j++)if(j!=i&&Actors[j]&&Vector3.Distance(h.position,Actors[j].transform.position)<1.3f)overlap=true;
    if(!overlap){p=h.position;return true;}
   }return false;
  }
  public bool Relocate(SoulbinderActor actor){if(!Model.Anchors[EncounterModel.Source[actor.Index]]||!LegalSpawn(actor.Index,out var p)){Model.Remove(actor.Index);return false;}actor.agent.Warp(p);Ring(p,.9f,violet,1.2f);return true;}
  public bool Acquire(int id,bool heavy){
   if(Model.Phase!=EncounterPhase.Combat)return false;
   if(heavy){if(Model.HeavyLocked||heavyOwner!=-1||Model.Time<6.3f)return false;heavyOwner=id;return true;}
   int max=Model.Cast==Invocation.Casting?1:2;
   if(elapsed<nextMelee||melee.Count>=max)return false;melee.Add(id);nextMelee=elapsed+.6f;return true;
  }
  public void Release(int id){melee.Remove(id);if(heavyOwner==id)heavyOwner=-1;}
  public void Hurt(float amount,Vector3 origin,bool unblockable=false){
   if(Model.Phase!=EncounterPhase.Combat||elapsed<hurtUntil)return;
   Vector3 dir=origin-view.transform.position;dir.y=0;
   if(guarding&&!unblockable&&Vector3.Dot(view.transform.forward.normalized,dir.normalized)>=.5f&&Shield>0){Shield=Mathf.Max(0,Shield-amount*2.8f);regenAt=elapsed+2.5f;Ring(player.transform.position,.65f,cyan,.15f);if(Shield<=0)BreakShield();hurtUntil=elapsed+.15f;return;}
   pendingPlayer+=amount;hurtUntil=elapsed+.7f;shakeUntil=elapsed+.18f;Play(hitSound,.3f);
  }
  void BreakShield(){guarding=false;Shield=0;shieldBrokenUntil=elapsed+1.8f;regenAt=elapsed+2.5f;Message="防御崩解 · 暂时无法格挡";}
  void TickBoss(float dt){
   if(Model.Cast==Invocation.Casting){Message=Model.Channel?"群体召唤引导 · 2.4秒内造成180实际伤害可打断":"群体召唤 · 正在预约缺失军团";boss.localScale=Vector3.one*(1+.012f*Mathf.Sin(elapsed*8));if(UnityEngine.Random.value<dt*3)Ring(boss.position+.06f*Vector3.up,1.8f,violet,.7f);return;}
   boss.localScale=Vector3.one;
   if(Model.Cast==Invocation.Stagger){Message="召唤已打断 · 趁僵直攻击";return;}
   Vector3 to=player.transform.position-boss.position;to.y=0;
   if(magicClock>0){
    magicClock+=dt;
    if(!magicReleased){
     if(magicClock<.9f)magicAim=player.transform.position+Vector3.up*1.1f+Vector3.ClampMagnitude(player.ActualVelocity,6)*.25f;
     UpdateMagicWarnings();if(to.sqrMagnitude>.01f)boss.forward=to.normalized;
     Message="缚魂者 · 扇形魂弹蓄力 · 侧移、格挡或吞印";
     if(magicClock>=1.25f){magicReleased=true;MagicVolleyCount++;var origin=boss.position+Vector3.up*2;var direction=(magicAim-origin).normalized;
      for(int i=-1;i<=1;i++)ShootArrow(origin,Quaternion.AngleAxis(i*10,Vector3.up)*direction,true);
      Play(castSound,.5f);Message="三发魂弹 · 命中伤害14 · 可吞印";
     }
    }
    if(magicClock>=2.15f&&!arrows.Exists(a=>a.bossMagic)){magicClock=0;magicAt=elapsed+7;Release(6);}return;
   }
   if(sweepClock>0){sweepClock+=dt;if(sweepClock<.8f){sweepFacing=to.normalized;boss.forward=sweepFacing;}
    if(sweepClock>=1.2f&&sweepClock-dt<1.2f){Ring(boss.position+.05f*Vector3.up,3.2f,red,.25f);if(to.magnitude<=3.2f&&Vector3.Dot(sweepFacing,to.normalized)>=.5f)Hurt(16,boss.position);}
    if(sweepClock>=3){sweepClock=0;sweepAt=elapsed+6;Release(5);}return;
   }
   if(Model.Cast!=Invocation.Idle)return;
   sweepProximity=to.magnitude<=3?sweepProximity+dt:0;
   if(sweepProximity>=.8f&&elapsed>=sweepAt&&Acquire(5,true)){sweepClock=.001f;sweepFacing=to.normalized;Sector(boss.position,3.2f,to.normalized,120,red,1.2f);Message="近身横扫 · 后退或绕后";return;}
   if(to.magnitude>3&&to.magnitude<23&&elapsed>=magicAt&&BossSight()&&Acquire(6,true)){
    magicClock=.001f;magicReleased=false;magicAim=player.transform.position+Vector3.up*1.1f;magicWarnings=new LineRenderer[3];
    for(int i=0;i<3;i++){var go=new GameObject("Boss_Magic_Telegraph");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=gold;line.positionCount=2;line.startWidth=line.endWidth=.035f;magicWarnings[i]=line;effects.Add(new Transient{obj=go,until=elapsed+1.25f});}
    UpdateMagicWarnings();Ring(boss.position+Vector3.up*2,.7f,violet,1.25f);Play(castSound,.3f);return;
   }
   if(to.sqrMagnitude>.01f)boss.rotation=Quaternion.RotateTowards(boss.rotation,Quaternion.LookRotation(to),dt*35);
   // Stay within central north lane; sampled navigation prevents crossing props.
   var desired=new Vector3(Mathf.Sin(elapsed*.18f)*2,0,3.5f+Mathf.Cos(elapsed*.13f)*1.5f);
   var step=Vector3.MoveTowards(boss.position,desired,dt*.35f);
   if(NavMesh.SamplePosition(step,out var nav,.5f,NavMesh.AllAreas))boss.position=nav.position;
  }
  bool BossSight(){var from=boss.position+Vector3.up*2;var delta=player.transform.position+Vector3.up*1.1f-from;return !Physics.Raycast(from,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore)||hit.collider.transform.IsChildOf(player.transform);}
  void UpdateMagicWarnings(){if(magicWarnings==null)return;var origin=boss.position+Vector3.up*2;var direction=(magicAim-origin).normalized;for(int i=0;i<3;i++)if(magicWarnings[i]){magicWarnings[i].SetPosition(0,origin);magicWarnings[i].SetPosition(1,origin+Quaternion.AngleAxis((i-1)*10,Vector3.up)*direction*18);}}
  public void ShootArrow(Vector3 origin,Vector3 direction,bool bossMagic=false){
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
   go.name=bossMagic?"Boss_可吞印魂弹":"可吞印魂箭";go.transform.position=origin;go.transform.localScale=Vector3.one*(bossMagic?.38f:.22f);go.GetComponent<Renderer>().sharedMaterial=violet;
   if(bossMagic){MagicBoltsFired++;var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=violet;trail.time=.25f;trail.startWidth=.2f;trail.endWidth=.02f;}
   arrows.Add(new SoulArrow{visual=go,pos=origin,velocity=direction.normalized*(bossMagic?11:8),life=bossMagic?4:6,damage=bossMagic?14:10,bossMagic=bossMagic});
  }
  void TickArrows(float dt){for(int i=arrows.Count-1;i>=0;i--){var a=arrows[i];Vector3 delta=a.velocity*dt;Vector3 closest=a.pos+Vector3.Project(view.transform.position-a.pos,delta);float along=Vector3.Dot(closest-a.pos,delta)/Mathf.Max(.0001f,delta.sqrMagnitude);closest=a.pos+delta*Mathf.Clamp01(along);
    bool remove=false;Vector3 to=closest-view.transform.position;
    if(elapsed<=absorbUntil&&to.sqrMagnitude<=4&&Vector3.Dot(view.transform.forward,to.normalized)>.7f){Power=Mathf.Min(100,Power+25);Message="吞印成功 +25 · 满100后下一发强化秘文弹";Ring(closest,.4f,cyan,.3f);remove=true;}
    else if(Vector3.Distance(closest,player.transform.position+Vector3.up*1.1f)<.65f){Hurt(a.damage,a.pos);remove=true;}
    else if(Physics.Raycast(a.pos,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore)&&!hit.collider.GetComponent<SoulbinderActor>()&&!hit.collider.GetComponent<SoulbinderTarget>())remove=true;
    a.pos+=delta;a.life-=dt;a.visual.transform.position=a.pos;if(remove||a.life<=0){Retire(a.visual);arrows.RemoveAt(i);}
   }}
  void HandleTerminal(){if(terminalHandled)return;terminalHandled=true;CancelMagic();bool won=Model.Phase==EncounterPhase.Won;
   foreach(var a in Actors)if(a){Ring(a.transform.position,1,violet,1.2f);Retire(a.gameObject);}Actors=new SoulbinderActor[5];foreach(var a in arrows)Retire(a.visual);arrows.Clear();melee.Clear();heavyOwner=-1;
   if(won){boss.gameObject.SetActive(false);foreach(var b in stairBarriers)b.SetActive(false);foreach(var a in anchorLocks)a.gameObject.SetActive(false);Record=new SoulbinderRecord{bossDefeated=true};Save();Message="缚魂者已败 · 灵魂停止敌对 · 前往北侧控制台 [交互]";}
   else{SetInput(false);Message="你被军团击败 · 重试会重置锚点、军团和生命";}
  }
  public bool Save(){bool ok=SoulbinderCheckpoint.Write(savePath,Record,out string e);SaveError=e;return ok;}
  public void Choose(string decision){if(Model.Phase!=EncounterPhase.Won||!(decision=="Contain"||decision=="Contract"))return;Record=new SoulbinderRecord{bossDefeated=true,decision=decision,assistTokens=decision=="Contract"?1:0};Save();Message=decision=="Contract"?"契约已建立 · 保留一次未来援助资格":"灵魂已安全收容";}
  public void ExitDemo(){if(Record==null||Record.decision=="Pending")return;if(SaveError!=null&&!continueUnsaved)return;ConsoleOpen=false;Completed=true;SetInput(false);if(CampaignMode)ArcaneArchive.Core.SoulbinderCampaignBridge.Return(true);}
  public void AllowUnsaved(){continueUnsaved=true;}
  public void OpenConsole(){if(Model.Phase==EncounterPhase.Won){ConsoleOpen=true;SetInput(false);}}
  public void ReturnToHall(){ConsoleOpen=false;SetInput(true);releaseControls=true;}
  public void BackToMenu(){if(CampaignMode){ArcaneArchive.Core.SoulbinderCampaignBridge.Return(false);return;}ClearBattle();Message="关闭三座锚点，切断补召。击败缚魂者后前往北侧控制台。";Started=false;Completed=false;ConsoleOpen=false;Paused=false;SetInput(false);}
  void Retire(GameObject obj){if(!obj)return;obj.SetActive(false);Destroy(obj);}
  void CancelMagic(){magicClock=0;magicReleased=false;if(magicWarnings!=null)foreach(var line in magicWarnings)if(line)Retire(line.gameObject);magicWarnings=null;}
  void ClearBattle(){CancelMagic();foreach(var a in Actors)if(a){a.gameObject.SetActive(false);Retire(a.gameObject);}Actors=new SoulbinderActor[5];foreach(var a in arrows)if(a.visual)Retire(a.visual);arrows.Clear();foreach(var e in effects)if(e.obj)Retire(e.obj);effects.Clear();}
  void OnDestroy(){ClearBattle();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  void Play(AudioClip clip,float volume){if(clip&&audioSource)audioSource.PlayOneShot(clip,volume);}
  public void Beam(Vector3 start,Vector3 end,Material mat,float width,float life){var go=new GameObject("秘文轨迹");var lr=go.AddComponent<LineRenderer>();lr.sharedMaterial=mat;lr.positionCount=2;lr.SetPosition(0,start);lr.SetPosition(1,end);lr.startWidth=lr.endWidth=width;effects.Add(new Transient{obj=go,until=elapsed+life});}
  public void Ring(Vector3 center,float radius,Material mat,float life){Sector(center,radius,Vector3.forward,360,mat,life);}
  public void Sector(Vector3 center,float radius,Vector3 forward,float angle,Material mat,float life){
   var go=new GameObject("攻击预警");var lr=go.AddComponent<LineRenderer>();lr.sharedMaterial=mat;lr.startWidth=lr.endWidth=.045f;int n=48;lr.positionCount=n+3;
   for(int i=0;i<=n;i++)lr.SetPosition(i,center+Quaternion.AngleAxis(-angle/2+angle*i/n,Vector3.up)*forward*radius);
   lr.SetPosition(n+1,angle>=359?lr.GetPosition(0):center);lr.SetPosition(n+2,lr.GetPosition(0));effects.Add(new Transient{obj=go,until=elapsed+life});
  }
  GUIStyle label,heading,button,small;
  void Styles(){if(label!=null)return;label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=true};label.normal.textColor=new Color(.88f,.87f,.94f);heading=new GUIStyle(label){fontSize=32};small=new GUIStyle(label){fontSize=15};button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,wordWrap=true};}
  void Bar(Rect r,float ratio,Color c){GUI.color=new Color(.07f,.06f,.11f,.95f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;GUI.DrawTexture(new Rect(r.x,r.y,r.width*Mathf.Clamp01(ratio),r.height),Texture2D.whiteTexture);GUI.color=Color.white;}
  void Panel(Rect r){GUI.color=new Color(.035f,.025f,.065f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
  bool Button(float x,float y,float w,string text)=>GUI.Button(new Rect(x,y,w,44),text,button);
  void OnGUI(){if(!view)return;Styles();float sx=Screen.width/1440f,sy=Screen.height/900f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));
   if(!Started){Panel(new Rect(410,195,620,510));GUI.Label(new Rect(455,230,540,60),"缚魂者 · 五印同召 V3",heading);GUI.Label(new Rect(455,310,530,95),Message,label);
    if(Button(455,430,530,"进入大厅 · 开始完整关卡"))Begin();if(CampaignMode){if(Button(455,490,530,"返回章节书页"))BackToMenu();}else if(Record!=null&&Button(455,490,530,"继续已保存的战后进度"))ContinueSaved();GUI.Label(new Rect(455,565,530,100),"WASD 移动 / Shift 冲刺 / 左键秘文弹\n右键按下吞印、按住格挡 / E 交互 / Esc 暂停",small);return;}
   if(Completed){Panel(new Rect(380,220,680,440));GUI.Label(new Rect(430,260,580,60),"关卡完成",heading);GUI.Label(new Rect(430,340,580,110),(Record.decision=="Contract"?"你与灵魂建立契约。未来援助资格 ×1。":"你解除了敌对束缚，灵魂得到安全收容。")+"\n"+(SaveError==null?"进度已保存。":"本次选择未保存。"),label);if(Button(430,500,280,"重新挑战"))Begin(true);if(Button(730,500,280,CampaignMode?"返回章节书页":"返回主菜单"))BackToMenu();return;}
   if(ConsoleOpen){Panel(new Rect(330,150,780,610));GUI.Label(new Rect(375,185,690,55),"灵魂控制台",heading);GUI.Label(new Rect(375,255,690,115),"战斗结束，灵魂不再敌对。\n选择安全收容，或保留一份可解除的契约。\n当前："+(Record?.decision??"Pending"),label);
    if(Button(375,390,325,"安全收容 / 解除契约"))Choose("Contain");if(Button(730,390,325,"建立契约 · 援助资格 ×1"))Choose("Contract");
    if(SaveError!=null){GUI.Label(new Rect(375,455,690,55),"未保存："+SaveError,small);if(Button(375,520,325,"重试保存"))Save();if(Button(730,520,325,"确认不保存，继续离场"))AllowUnsaved();}
    if(Button(375,635,325,"返回大厅探索"))ReturnToHall();GUI.enabled=Record!=null&&Record.decision!="Pending"&&(SaveError==null||continueUnsaved);if(Button(730,635,325,CampaignMode?"完成第一章 · 继续故事":"完成关卡并离场"))ExitDemo();GUI.enabled=true;return;}
   if(Paused||Model.Phase==EncounterPhase.Lost){Panel(new Rect(470,250,500,390));GUI.Label(new Rect(510,285,420,65),Paused?"战斗暂停":"挑战失败",heading);if(Paused&&Button(510,380,420,"继续"))TogglePause();if(Button(510,440,420,"重新挑战 · 跳过演出"))Begin(true);if(Button(510,500,420,CampaignMode?"返回章节书页":"返回主菜单"))BackToMenu();return;}
   GUI.Label(new Rect(420,24,600,35),Model.Phase==EncounterPhase.Won?"军团已平息":"缚魂者 · SOULBINDER",label);Bar(new Rect(350,65,740,14),Model.BossHealth/1800,new Color(.6f,.23f,.78f));
   if(Model.Cast==Invocation.Casting){GUI.Label(new Rect(450,88,650,32),Model.Channel?"引导中 · 伤害 ×1.25 · 180伤害打断":"群体召唤",small);Bar(new Rect(450,122,540,6),Model.CastTime/EncounterModel.SummonCastDuration,new Color(.86f,.58f,1));}
   if(Model.Channel){GUI.Label(new Rect(450,134,650,28),"打断进度 "+(int)Model.InterruptProgress+" / 180",small);Bar(new Rect(450,164,540,6),Model.InterruptProgress/EncounterModel.InterruptThreshold,new Color(1,.7f,.2f));}
   if(MagicCharging)GUI.Label(new Rect(480,96,600,38),"扇形魂弹 · 侧移 / 格挡 / 吞印",label);
   Panel(new Rect(26,690,390,180));GUI.Label(new Rect(44,706,350,32),"生命 "+Mathf.CeilToInt(Model.PlayerHealth)+" / 100",label);Bar(new Rect(44,744,350,10),Model.PlayerHealth/100,new Color(.7f,.18f,.25f));GUI.Label(new Rect(44,765,350,28),"护盾 "+(int)Shield+"     秘文能量 "+(int)Power,small);Bar(new Rect(44,802,165,8),Shield/100,new Color(.22f,.65f,.85f));Bar(new Rect(224,802,170,8),Power/100,new Color(.9f,.68f,.26f));GUI.Label(new Rect(44,825,350,28),Power>=100?"强化秘文弹已就绪":guarding?"正在格挡":"吞印：右键按下",small);
   for(int a=0;a<3;a++){GUI.Label(new Rect(35,32+a*30,280,28),new[]{"西印 · 砍刀 / 剑盾","东印 · 弓箭","北印 · 重锤 / 幽灵"}[a]+(Model.Anchors[a]?"  "+(int)Model.AnchorHealth[a]:"  已关闭"),small);}
   GUI.Label(new Rect(1050,35,350,130),"军团 "+Model.Occupied+" / 5\n左键：攻击\n右键：吞印 / 格挡\nE：交互　Esc：暂停",small);
   GUI.Label(new Rect(450,810,920,60),Message,label);GUI.Label(new Rect(711,434,30,35),"+",label);
   if(Model.Phase==EncounterPhase.Won&&Vector3.Distance(player.transform.position,console.position)<3.5f)GUI.Label(new Rect(490,650,650,40),"按 "+player.Bindings.Display("Interact")+" 打开灵魂控制台",label);
   if(Physics.Raycast(view.transform.position,view.transform.forward,out var aim,40,~0,QueryTriggerInteraction.Collide)){var t=aim.collider.GetComponent<SoulbinderTarget>();if(t&&t.kind>=0&&t.kind<5&&Actors[t.kind])GUI.Label(new Rect(590,480,500,40),Names[t.kind]+"  "+(int)Actors[t.kind].Health,small);}
  }
 }

}
