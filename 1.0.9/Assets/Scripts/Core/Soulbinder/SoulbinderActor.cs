using UnityEngine;
using UnityEngine.AI;
namespace ArcaneArchive.Soulbinder {
 public sealed class SoulbinderActor:MonoBehaviour {
  public int Index {get;private set;}
  public float Health {get;private set;}
  public NavMeshAgent agent;
  SoulbinderLevel level;Transform visual;Vector3 originalScale;float cooldown,windup,recovery,dash,relocating,stuck;Vector3 lockedDirection,lastPosition;
  bool attacking,heavy,shieldBroken,dashHit;float liveSince;
  static readonly float[] Hp={60,100,60,140,60}, Windup={.9f,1.1f,1.3f,1.8f,1.4f}, Recover={1.1f,1.2f,2.7f,2.2f,2};
  public void Initialize(SoulbinderLevel owner,int index,GameObject model){
   level=owner;Index=index;Health=Hp[index];heavy=index>=3;var facing=owner.player.transform.position-transform.position;facing.y=0;if(facing.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(facing);
   var go=Instantiate(model,transform);go.name=SoulbinderLevel.Names[index]+"_Visual";visual=go.transform;originalScale=visual.localScale;
   agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=index==3?.6f:.42f;agent.height=2.2f;agent.speed=index==3?1.4f:2;agent.acceleration=8;agent.angularSpeed=180;agent.stoppingDistance=index==2?8:index==3?1.8f:1.4f;
   var capsule=gameObject.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*1.1f;capsule.height=2.2f;capsule.radius=index==3?.65f:.5f;capsule.isTrigger=true;
   gameObject.AddComponent<SoulbinderTarget>().kind=index;lastPosition=transform.position;liveSince=owner.BattleTime;
  }
  public void Hit(float damage,bool empowered,Vector3 source){
   if(level.Model.Slots[Index]!=SoulSlot.Alive)return;
   if(Index==1&&!shieldBroken){if(empowered){shieldBroken=true;CancelAttack();recovery=2;level.Ring(transform.position,1,level.gold,.5f);}else {var to=source-transform.position;to.y=0;if(Vector3.Dot(transform.forward,to.normalized)>.5f)damage*=.4f;}}
   Health-=damage;level.Ring(transform.position+Vector3.up*.06f,.55f,empowered?level.gold:level.cyan,.15f);
   if(Health<=0){CancelAttack();level.Model.Remove(Index);gameObject.SetActive(false);}
  }
  void CancelAttack(){attacking=false;dash=0;level.Release(Index);if(agent&&agent.isOnNavMesh){agent.isStopped=true;agent.updatePosition=true;agent.updateRotation=true;}}
  public void Step(float dt){
   if(!gameObject.activeSelf||!agent||!agent.isOnNavMesh)return;
   bool alive=level.Model.Slots[Index]==SoulSlot.Alive;
   if(!alive){agent.isStopped=true;visual.localScale=originalScale*(.82f+.18f*Mathf.Clamp01((level.BattleTime-liveSince)/1.5f));return;}
   visual.localScale=originalScale;cooldown-=dt;recovery-=dt;
   if(relocating>0){relocating-=dt;agent.isStopped=true;return;}
   if(dash>0){agent.isStopped=true;Vector3 step=lockedDirection*4*dt;
    if(!CanDashStep(step)){dash=0;FinishAttack();return;}
    agent.Move(step);dash-=dt;if(!dashHit&&Vector3.Distance(transform.position,level.player.transform.position)<1.15f){level.Hurt(12,transform.position-lockedDirection);dashHit=true;}
    if(dash<=0)FinishAttack();return;}
   var to=level.player.transform.position-transform.position;to.y=0;float distance=to.magnitude;
   if(attacking){agent.isStopped=true;windup-=dt;
    if(windup>.4f&&to.sqrMagnitude>.01f){lockedDirection=to.normalized;transform.rotation=Quaternion.LookRotation(lockedDirection);}
    visual.localRotation=Quaternion.Euler(-Mathf.Sin(Mathf.Clamp01(1-windup/Windup[Index])*Mathf.PI)*18,0,0);
    if(windup<=0){visual.localRotation=Quaternion.identity;
     if(Index==2){level.ShootArrow(transform.position+Vector3.up*1.45f,(level.player.transform.position+Vector3.up*1.1f)-(transform.position+Vector3.up*1.45f));FinishAttack();}
     else if(Index==4){dash=1.5f;dashHit=false;}
     else {float range=Index==3?2.2f:2.1f;if(distance<=range&&(Index==3||Vector3.Dot(lockedDirection,to.normalized)>.35f))level.Hurt(Index==3?20:10,transform.position,Index==3);FinishAttack();}
    }return;
   }
   visual.localRotation=Quaternion.Euler(0,0,Mathf.Sin(level.BattleTime*6+Index)*Mathf.Min(agent.velocity.magnitude*1.3f,3));
   if(recovery>0){agent.isStopped=true;return;}
   float threshold=Index==2?12:Index==4?6:Index==3?2.3f:2;
   float earliest=Index==0?4.5f:Index==1?5.7f:Index==2?5.1f:6.3f;
   bool canShoot=Index!=2||level.Model.Cast!=Invocation.Casting;
   if(cooldown<=0&&distance<=threshold&&level.BattleTime>=earliest&&canShoot&&Sight()){
    bool permit=Index==2||level.Acquire(Index,heavy);
    if(permit){attacking=true;windup=Windup[Index];lockedDirection=to.normalized;agent.isStopped=true;
     if(Index==3)level.Ring(transform.position+Vector3.up*.04f,2,level.red,1.8f);
     else if(Index==4)level.Beam(transform.position+Vector3.up*.08f,transform.position+lockedDirection*6+Vector3.up*.08f,level.red,.08f,1.4f);
     else if(Index==2)level.Beam(transform.position+Vector3.up*1.4f,level.player.transform.position+Vector3.up,level.violet,.02f,1.3f);
     else level.Sector(transform.position+Vector3.up*.04f,2,lockedDirection,90,level.gold,Windup[Index]);return;}
   }
   agent.isStopped=false;
   if(Index==2&&distance<5){var away=transform.position-to.normalized*3;if(NavMesh.SamplePosition(away,out var h,2,NavMesh.AllAreas))agent.SetDestination(h.position);}else agent.SetDestination(level.player.transform.position);
   if(distance>threshold&&Vector3.Distance(transform.position,lastPosition)<.004f)stuck+=dt;else stuck=0;
   lastPosition=transform.position;
   if(stuck>=5){stuck=0;CancelAttack();if(level.Relocate(this))relocating=1.2f;}
  }
  public bool CanDashStep(Vector3 step){return !NavMesh.Raycast(transform.position,transform.position+step,out _,NavMesh.AllAreas)&&!Physics.CapsuleCast(transform.position+Vector3.up*.65f,transform.position+Vector3.up*1.7f,.4f,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore);}
  bool Sight(){var from=transform.position+Vector3.up*1.4f;var delta=level.player.transform.position+Vector3.up-from;return !Physics.Raycast(from,delta.normalized,out var h,delta.magnitude,~0,QueryTriggerInteraction.Ignore)||h.collider.GetComponent<ArcaneArchive.Player.FirstPersonController>();}
  void FinishAttack(){attacking=false;dash=0;recovery=Recover[Index];cooldown=Index==2?2.7f:recovery+.5f;level.Release(Index);}
  void OnDestroy(){if(level)level.Release(Index);}
 }
}
