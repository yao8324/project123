using System.Linq;
using UnityEngine;
using ArcaneArchive.Visuals;
namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        Vector3 lastKnownPlayer,moveGoal,rushOrigin,rushDirection;
        float nextPerception,lastSeenAt,rushLength,ramTravel;
        bool ramHit,moveBlocked;
        RushTelegraphVisuals rushWarningVisual;
        Vector3 riftHead,riftDirection;
        float riftStartedAt,riftCommitUntil;
        bool riftBlocked;
        public Vector3 RiftHead=>riftHead;
        public Vector3 RiftDirection=>riftDirection;
        public float FracturePathLength { get; private set; }
        public float FractureTotalTurn { get; private set; }
        public bool FractureBlocked=>riftBlocked;
        public string BossIntent { get; private set; }
        public Vector3 LastKnownPlayerPosition=>lastKnownPlayer;
        public Vector3 RushDirection=>rushDirection;
        public float RushTravel=>ramTravel;
        public int RushHitCount { get; private set; }
        public float MaximumEnemyHomingTurn { get; private set; }
        public float MaximumPlayerHomingTurn { get; private set; }
        void ResetIntent()
        {
            lastKnownPlayer=transform.position;nextPerception=lastSeenAt=0;moveGoal=arena.boss.position;
            ramTravel=0;ramHit=false;RushHitCount=0;MaximumEnemyHomingTurn=MaximumPlayerHomingTurn=0;BossIntent="审视";
        }
        void UpdatePerception(float dt)
        {
            if(CurrentState!=State.Observe && !(CurrentState==State.Telegraph && stateTime<WarningDuration-AimLockDuration))return;
            if(combatTime<nextPerception)return;nextPerception=combatTime+definition.perceptionInterval;
            Vector3 offset=transform.position-arena.boss.position;offset.y=0;
            if(offset.magnitude>35 || Vector3.Angle(arena.boss.forward,offset)>definition.visionAngle*.5f)return;
            if(!HasClearShot(arena.boss.position+Vector3.up*3.5f,View.transform.position))return;
            lastKnownPlayer=transform.position;lastSeenAt=combatTime;
        }
        void ChooseMoveIntent()
        {
            Vector3 toward=lastKnownPlayer-arena.boss.position;toward.y=0;float distance=toward.magnitude;
            toward=distance>.01f?toward/distance:arena.boss.forward;Vector3 side=Vector3.Cross(Vector3.up,toward)*orbitDirection,step;
            if(distance>definition.preferredMaxDistance){step=(toward+.4f*side).normalized*5.5f;BossIntent="接近记忆位置";}
            else if(distance<=definition.meleeReach+definition.meleeAdvanceDistance && combatTime>=nextMeleeAt)
            {step=toward*Mathf.Max(0,distance-(definition.meleeReach-.7f))+side*.35f;BossIntent="近身压迫";}
            else if(distance<4.4f){step=side*1.1f;BossIntent="近身对峙";}
            else if(distance<definition.preferredMinDistance){step=(-toward+.45f*side).normalized*4;BossIntent="拉开距离";}
            else{step=side*4.6f;BossIntent="侧向换位";}
            if(combatTime-lastSeenAt>3){step=side*3;BossIntent="搜索";}
            Vector3 local=arena.transform.InverseTransformPoint(arena.boss.position+step);local.y=0;
            moveGoal=arena.transform.TransformPoint(Vector3.ClampMagnitude(local,arena.radius-2.4f));moveBlocked=false;
        }
        void FacePoint(Vector3 target,float dt)
        {
            Vector3 direction=target-arena.boss.position;direction.y=0;if(direction.sqrMagnitude<.01f)return;
            arena.boss.rotation=Quaternion.RotateTowards(arena.boss.rotation,Quaternion.LookRotation(direction),(!KnightMode && CurrentState==State.Telegraph && attack==5?definition.meleeTurnSpeed:definition.bodyTurnSpeed)*dt);
        }
        void MoveTowardIntent(float dt,float factor)
        {
            // Commit to a waypoint instead of chasing the live player transform every frame.
            if(stateTime>definition.repositionTime-.3f){FacePoint(lastKnownPlayer,dt);return;}
            Vector3 delta=moveGoal-arena.boss.position;delta.y=0;if(delta.magnitude<.15f || moveBlocked)return;
            FacePoint(lastKnownPlayer,dt);
            float ease=Mathf.SmoothStep(.15f,1,Mathf.Clamp01(stateTime/.23f))*Mathf.SmoothStep(.2f,1,Mathf.Clamp01((definition.repositionTime-.22f-stateTime)/.25f));
            float length=Mathf.Min(delta.magnitude,definition.moveSpeed*(Phase==2?1.2f:1)*factor*dt*ease);
            if(SweepBoss(delta.normalized,length,false)<.001f){moveBlocked=true;orbitDirection*=-1;BossIntent="受阻等待";}
        }
        int SelectAttack()
        {
            if(MeleeInRange && combatTime>=nextMeleeAt){nextMeleeAt=combatTime+definition.meleeCooldown;BossIntent="刃杖近斩";return 5;}
            int[] order={0,3,2,4,1};int choice=order[pattern++%order.Length];float distance=HorizontalDistance(arena.boss.position,lastKnownPlayer);
            if(choice==4 && (distance<4 || distance>14 || combatTime-lastSeenAt>2.5f))choice=distance<4?1:0;
            if(combatTime-lastSeenAt>5)choice=1;
            BossIntent=choice==4?"锁定冲撞路线":"准备施法";return choice;
        }
        float SweepBoss(Vector3 direction,float requested,bool rush)
        {
            float travel=requested;
            var capsule=arena.boss.GetComponent<CapsuleCollider>();
            float radius=capsule?capsule.radius*Mathf.Max(arena.boss.lossyScale.x,arena.boss.lossyScale.z):.55f;
            Vector3 center=capsule?arena.boss.TransformPoint(capsule.center):arena.boss.position+Vector3.up*1.8f;
            float half=capsule?Mathf.Max(0,capsule.height*arena.boss.lossyScale.y*.5f-radius):1.1f;
            var hits=Physics.CapsuleCastAll(center-Vector3.up*half,center+Vector3.up*half,radius,direction,travel+.06f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(hit.collider.transform.IsChildOf(arena.boss))continue;travel=Mathf.Min(travel,Mathf.Max(0,hit.distance-.05f));
                if(rush && !ramHit && hit.collider.transform.IsChildOf(transform)){ramHit=true;RushHitCount++;combat.ReceiveHit(definition.rushDamage,arena.boss.position,true);}break;
            }
            Vector3 offset=arena.boss.position-arena.transform.position;offset.y=0;
            float limit=arena.radius-radius-.15f,dot=Vector3.Dot(offset,direction);
            float safe=-dot+Mathf.Sqrt(Mathf.Max(0,dot*dot+limit*limit-offset.sqrMagnitude));
            travel=Mathf.Min(travel,Mathf.Max(0,safe));Vector3 next=arena.boss.position+direction*travel;
            if(rush && travel>0)CheckRushDodge(center,center+direction*travel,radius,half);
            arena.boss.position=next;BossDistanceTravelled+=travel;return travel;
        }
        void AimRush()
        {
            rushOrigin=arena.boss.position;rushOrigin.y=arena.transform.position.y+.045f;var delta=lockTarget-rushOrigin;delta.y=0;
            rushDirection=delta.sqrMagnitude>.01f?delta.normalized:arena.boss.forward;rushLength=Mathf.Min(definition.rushDistance,delta.magnitude+.8f);ramTravel=0;ramHit=false;
        }
        void TickRush(float dt)
        {
            if(rushWarningVisual)rushWarningVisual.Present(rushOrigin,rushOrigin+rushDirection*rushLength,1);
            if (!rushWake) BeginRushWake();
            float duration = Mathf.Clamp(rushLength / Mathf.Max(1, definition.rushSpeed), .12f, .18f);
            float requested = Mathf.Max(0, rushLength * Mathf.Clamp01(stateTime / duration) - ramTravel);
            float moved=SweepBoss(rushDirection,requested,true);ramTravel+=moved;
            if(IsFinished)return;
            if(rushWake)rushWake.SetPath(rushOrigin,rushDirection,ramTravel,combatTime);
            if(ramHit || moved+.002f<requested || ramTravel>=rushLength-.02f || stateTime>=duration)
            {
                LastRushDuration=stateTime;
                if(rushWake)rushWake.Finish(combatTime);
                if(rushWarningVisual)Destroy(rushWarningVisual.gameObject);rushWarningVisual=null;
                SetState(State.Recovery);BossIntent="冲撞收招";Feedback("冲势已尽 · 反击窗口",definition.rushRecovery);
            }
        }
        void StartFractureLine()
        {
            fractureSequenceStarted=true;riftStartedAt=nextFractureSpawn=combatTime;riftCommitUntil=0;riftBlocked=false;
            FracturePathLength=FractureTotalTurn=0;
            riftDirection=transform.position-arena.boss.position;riftDirection.y=0;
            riftDirection=riftDirection.sqrMagnitude>.01f?riftDirection.normalized:arena.boss.forward;
            riftHead=arena.boss.position+riftDirection*.9f;riftHead.y=arena.transform.position.y+.026f;
        }
        void ExtendFractureLine()
        {
            float interval=Mathf.Max(.12f,definition.fractureInterval);
            Vector3 target=transform.position-riftHead;target.y=0;
            // Commit near the player so a last-second side step can evade the advancing line.
            if(target.magnitude<2.5f && combatTime>=riftCommitUntil)riftCommitUntil=combatTime+definition.fractureLeadTime;
            if(target.magnitude>=2.5f && combatTime>=riftCommitUntil)
            {
                var old=riftDirection;
                riftDirection=Vector3.RotateTowards(riftDirection,target.normalized,definition.fractureTurnSpeed*Mathf.Deg2Rad*interval,0).normalized;
                FractureTotalTurn+=Vector3.Angle(old,riftDirection);
            }
            float length=definition.fractureTravelSpeed*interval;
            float width=Phase==2?definition.fracturePhaseTwoWidth:definition.fractureWidth;
            // Sweep the full widened strip: a centre ray could let its edges cross walls.
            foreach(var hit in Physics.BoxCastAll(riftHead+Vector3.up*.18f,new Vector3(width*.5f,.10f,.02f),riftDirection,Quaternion.LookRotation(riftDirection),length+.06f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
            {
                if(hit.collider.transform.IsChildOf(arena.boss) || hit.collider.transform.IsChildOf(transform))continue;
                length=Mathf.Max(0,hit.distance-.06f);riftBlocked=true;break;
            }
            if(length<.12f){riftBlocked=true;return;}
            Vector3 end=riftHead+riftDirection*length;
            if(HorizontalDistance(end,arena.transform.position)>arena.radius-width*.5f){riftBlocked=true;return;}
            var f=Instantiate(arena.fractureEffect,arena.effectRoot);
            f.transform.SetPositionAndRotation(riftHead,Quaternion.LookRotation(riftDirection));
            f.transform.localScale=new Vector3(width,1,length);
            f.Configure(FractureSpawnCount+pattern*7,definition.fractureFadeTime);
            zones.Add(new Zone{go=f.gameObject,fracture=f,position=riftHead,born=combatTime});
            riftHead=end;FracturePathLength+=length;FractureSpawnCount++;
        }
        void TickFractures()
        {
            if(!fractureSequenceStarted)return;
            bool extending=!riftBlocked && combatTime-riftStartedAt<definition.fractureDuration+(Phase==2?1.2f:0);
            if(attack==2 && (CurrentState==State.Telegraph || CurrentState==State.Attack) && extending && combatTime>=nextFractureSpawn)
            {
                ExtendFractureLine();nextFractureSpawn+=Mathf.Max(.12f,definition.fractureInterval);
            }
            for(int i=zones.Count-1;i>=0;i--)
            {
                var zone=zones[i];float age=combatTime-zone.born,eruption=age-definition.fractureLeadTime;
                zone.fracture.Present(Mathf.Clamp01(age/Mathf.Max(.5f,definition.fractureLeadTime)),eruption);
                if(!zone.fired && eruption>=0)
                {
                    zone.fired=true;if(SelectedAudio)SelectedAudio.Play(SelectedAudio.groundFracture,.24f);float before=combat.Health;
                    bool hitPlayer=zone.fracture.Contains(transform.position,.24f) && transform.position.y<arena.transform.position.y+.65f;
                    if(!hitPlayer && CheckingDodge)CheckDodge(zone.fracture.Contains(ActiveDodge.StartPosition,.24f) &&
                        ActiveDodge.StartPosition.y<arena.transform.position.y+.65f,false);
                    if(hitPlayer)combat.TakeDamage(definition.fractureDamage);
                    if(combat.Health<before)FractureHitCount++;
                    if(IsFinished)return;
                }
                if(eruption>=definition.fractureFadeTime){Destroy(zone.go);zones.RemoveAt(i);}
            }
            if(CurrentState==State.Attack && attack==2 && (!extending || riftBlocked) && zones.Count==0)
            {fractureSequenceStarted=false;SetState(State.Recovery);}
        }
        bool HasClearShot(Vector3 from,Vector3 to)
        {
            var delta=to-from;if(delta.sqrMagnitude<.001f)return true;
            foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(transform) && !hit.collider.transform.IsChildOf(arena.boss))return false;
            return true;
        }
        void SteerProjectile(Shot shot,float dt)
        {
            shot.age+=dt;if(shot.crystalTarget && !shot.crystalTarget.Available)shot.homing=false;if(!shot.homing)return;float limit=shot.enemy?definition.enemyHomingBudget:definition.playerHomingBudget;
            if(shot.age>(shot.enemy?definition.enemyHomingDuration:.85f) || shot.homingUsed>=limit){shot.homing=false;return;}
            if(shot.age<(shot.enemy?definition.enemyHomingDelay:definition.projectileHomingDelay))return;
            Vector3 target=shot.crystalTarget?shot.crystalTarget.transform.position:shot.enemy?transform.position+Vector3.up*1.2f:arena.boss.TransformPoint(new Vector3(0,2.55f,.88f));
            Vector3 toward=target-shot.go.transform.position;
            if(toward.magnitude<(shot.enemy?definition.enemyHomingCommitDistance:.65f) || Vector3.Dot(shot.velocity.normalized,toward.normalized)<(shot.enemy?.05f:.4f)){shot.homing=false;return;}
            if(!HasClearShot(shot.go.transform.position,target))return;
            float speed=shot.velocity.magnitude,turn=Mathf.Min((shot.enemy?definition.enemyHomingSpeed:definition.playerHomingSpeed)*dt,limit-shot.homingUsed);
            var old=shot.velocity.normalized;var direction=Vector3.RotateTowards(old,toward.normalized,turn*Mathf.Deg2Rad,0).normalized;
            shot.homingUsed+=Mathf.Atan2(Vector3.Cross(old,direction).magnitude,Vector3.Dot(old,direction))*Mathf.Rad2Deg;
            shot.velocity=direction*speed;shot.go.transform.rotation=Quaternion.LookRotation(direction);
            if(shot.enemy)MaximumEnemyHomingTurn=Mathf.Max(MaximumEnemyHomingTurn,shot.homingUsed);else MaximumPlayerHomingTurn=Mathf.Max(MaximumPlayerHomingTurn,shot.homingUsed);
        }
        void PresentBoss(float dt){if(KnightMode){if(poise>0 && combatTime-poiseLastHit>2.5f)poise=Mathf.Max(0,poise-18*dt);if(knightVisual)knightVisual.Present(this);return;}arena.model.animatedRig.SetMeleeRange(arena.boss.position,definition.meleeReach);arena.model.Present(dt,Phase,attack,CurrentState,stateTime,WarningDuration,RecoveryDuration);PresentMeleeVisual(dt);}
    }
}
