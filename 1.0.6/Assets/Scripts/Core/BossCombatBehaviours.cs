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
            else if(distance<definition.preferredMinDistance){step=(-toward+.45f*side).normalized*4;BossIntent="拉开距离";}
            else{step=side*4.6f;BossIntent="侧向换位";}
            if(combatTime-lastSeenAt>3){step=side*3;BossIntent="搜索";}
            Vector3 local=arena.transform.InverseTransformPoint(arena.boss.position+step);local.y=0;
            moveGoal=arena.transform.TransformPoint(Vector3.ClampMagnitude(local,arena.radius-2.4f));moveBlocked=false;
        }
        void FacePoint(Vector3 target,float dt)
        {
            Vector3 direction=target-arena.boss.position;direction.y=0;if(direction.sqrMagnitude<.01f)return;
            arena.boss.rotation=Quaternion.RotateTowards(arena.boss.rotation,Quaternion.LookRotation(direction),definition.bodyTurnSpeed*dt);
        }
        void MoveTowardIntent(float dt,float factor)
        {
            // Commit to a waypoint instead of chasing the live player transform every frame.
            if(stateTime<.3f)return;
            if(stateTime>definition.repositionTime-.65f){FacePoint(lastKnownPlayer,dt);return;}
            Vector3 delta=moveGoal-arena.boss.position;delta.y=0;if(delta.magnitude<.15f || moveBlocked)return;
            FacePoint(moveGoal,dt);float length=Mathf.Min(delta.magnitude,definition.moveSpeed*(Phase==2?1.2f:1)*factor*dt);
            if(SweepBoss(delta.normalized,length,false)<.001f){moveBlocked=true;orbitDirection*=-1;BossIntent="受阻等待";}
        }
        int SelectAttack()
        {
            int[] order={0,3,2,4,1};int choice=order[pattern++%order.Length];float distance=HorizontalDistance(arena.boss.position,lastKnownPlayer);
            if(choice==4 && (distance<4 || distance>14 || combatTime-lastSeenAt>2.5f))choice=distance<4?1:0;
            if(combatTime-lastSeenAt>5)choice=1;
            BossIntent=choice==4?"锁定冲撞路线":"准备施法";return choice;
        }
        float SweepBoss(Vector3 direction,float requested,bool rush)
        {
            float travel=requested;
            var hits=Physics.CapsuleCastAll(arena.boss.position+Vector3.up*1.05f,arena.boss.position+Vector3.up*3.35f,.70f,direction,travel+.06f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(hit.collider.transform.IsChildOf(arena.boss))continue;travel=Mathf.Min(travel,Mathf.Max(0,hit.distance-.05f));
                if(rush && !ramHit && hit.collider.transform.IsChildOf(transform)){ramHit=true;RushHitCount++;combat.ReceiveHit(definition.rushDamage,arena.boss.position,true);}break;
            }
            Vector3 next=arena.boss.position+direction*travel;var local=arena.transform.InverseTransformPoint(next);local.y=0;
            if(local.magnitude>arena.radius-2.1f)return 0;
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
            float speed=definition.rushSpeed*Mathf.SmoothStep(.25f,1,Mathf.Clamp01(stateTime/.18f));
            float requested=Mathf.Min(speed*dt,Mathf.Max(0,rushLength-ramTravel)),moved=SweepBoss(rushDirection,requested,true);ramTravel+=moved;
            if(IsFinished)return;
            if(ramHit || moved+.002f<requested || ramTravel>=rushLength-.02f || stateTime>1.6f)
            {if(rushWarningVisual)Destroy(rushWarningVisual.gameObject);rushWarningVisual=null;SetState(State.Recovery);BossIntent="冲撞收招";Feedback("冲势已尽 · 反击窗口",definition.rushRecovery);}
        }
        void TickFractures()
        {
            for(int i=0;i<zones.Count;i++)
            {
                var zone=zones[i];float elapsed=stateTime-i*.22f;zone.fracture.Present(1,elapsed);
                if(!zone.fired && elapsed>=.08f)
                {zone.fired=true;if(zone.fracture.Contains(transform.position,.24f) && transform.position.y<arena.transform.position.y+.65f)combat.TakeDamage(definition.fractureDamage);if(IsFinished)return;}
            }
            if(stateTime>2.05f){foreach(var z in zones)Destroy(z.go);zones.Clear();SetState(State.Recovery);}
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
            shot.age+=dt;if(!shot.homing)return;float limit=shot.enemy?definition.enemyHomingBudget:definition.playerHomingBudget;
            if(shot.age>(shot.enemy?1.15f:.85f) || shot.homingUsed>=limit){shot.homing=false;return;}
            if(shot.age<definition.projectileHomingDelay)return;
            Vector3 target=shot.enemy?transform.position+Vector3.up*1.2f:arena.boss.TransformPoint(new Vector3(0,2.55f,.88f));
            Vector3 toward=target-shot.go.transform.position;
            if(toward.magnitude<(shot.enemy?1.7f:.65f) || Vector3.Dot(shot.velocity.normalized,toward.normalized)<.4f){shot.homing=false;return;}
            if(!HasClearShot(shot.go.transform.position,target))return;
            float speed=shot.velocity.magnitude,turn=Mathf.Min((shot.enemy?definition.enemyHomingSpeed:definition.playerHomingSpeed)*dt,limit-shot.homingUsed);
            var old=shot.velocity.normalized;var direction=Vector3.RotateTowards(old,toward.normalized,turn*Mathf.Deg2Rad,0).normalized;
            shot.homingUsed+=Mathf.Atan2(Vector3.Cross(old,direction).magnitude,Vector3.Dot(old,direction))*Mathf.Rad2Deg;
            shot.velocity=direction*speed;shot.go.transform.rotation=Quaternion.LookRotation(direction);
            if(shot.enemy)MaximumEnemyHomingTurn=Mathf.Max(MaximumEnemyHomingTurn,shot.homingUsed);else MaximumPlayerHomingTurn=Mathf.Max(MaximumPlayerHomingTurn,shot.homingUsed);
        }
        void PresentBoss(float dt){arena.model.Present(dt,Phase,attack,CurrentState,stateTime,WarningDuration);}
    }
}
