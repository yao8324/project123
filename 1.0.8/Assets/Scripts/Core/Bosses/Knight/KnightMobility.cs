using UnityEngine;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        Vector3 knightDestination;
        bool knightBlinked;
        int knightBarrageRound;
        public int KnightBarrageRound=>knightBarrageRound+1;
        public Vector3 KnightMobilityDestination=>knightDestination;
        bool KnightBlinkMove=>knightMove==KnightMove.BlinkChase || knightMove==KnightMove.BlinkBarrage;

        void ResetKnightMobility()
        {knightBlinked=false;knightBarrageRound=0;PlanKnightDestination();}

        bool ContinueKnightBarrage()
        {
            if(knightMove!=KnightMove.BlinkBarrage || knightBarrageRound>=2)return false;
            // Keep already released projectiles alive; each round gets its own warning and blink.
            knightBarrageRound++;knightImpact=0;knightBlinked=false;
            knightOrigin=arena.boss.position;knightTarget=transform.position;
            knightForward=arena.boss.forward;PlanKnightDestination();
            SetState(State.Telegraph);UpdateKnightWarning();
            PlaySound(definition.warningSound,.3f);
            return true;
        }

        void PlanKnightDestination()
        {
            Vector3 toward=knightTarget-knightOrigin;toward.y=0;
            Vector3 forward=toward.sqrMagnitude>.001f?toward.normalized:knightForward;
            Vector3 desired=knightOrigin;
            if(knightMove==KnightMove.LeapCleave || knightMove==KnightMove.BlinkChase)
            {
                // Stop in front of the locked target, never silently appear behind the camera.
                float standoff=knightMove==KnightMove.LeapCleave?1.9f:3.1f;
                float maximum=knightMove==KnightMove.LeapCleave?knightDefinition.pursuitLeapTravel:knightDefinition.blinkTravel;
                desired+=forward*Mathf.Clamp(toward.magnitude-standoff,0,maximum);
            }
            if(knightMove==KnightMove.BlinkBarrage)
            {
                Vector3 side=Vector3.Cross(Vector3.up,forward)*((knightPattern+knightBarrageRound)%2==0?1:-1);
                Vector3 first=SafeKnightDestination(knightOrigin+side*knightDefinition.sideBlinkTravel-forward*1.2f);
                Vector3 other=SafeKnightDestination(knightOrigin-side*knightDefinition.sideBlinkTravel-forward*1.2f);
                // Use the free side when a pillar blocks the preferred orbit direction.
                desired=(first-knightOrigin).sqrMagnitude>=1f?first:other;
            }
            knightDestination=SafeKnightDestination(desired);
        }

        Vector3 SafeKnightDestination(Vector3 desired)
        {
            Vector3 from=arena.boss.position;from.y=arena.transform.position.y;
            var capsule=arena.boss.GetComponent<CapsuleCollider>();
            float radius=capsule?capsule.radius*Mathf.Max(arena.boss.lossyScale.x,arena.boss.lossyScale.z):.55f;
            Vector3 offset=desired-arena.transform.position;offset.y=0;
            desired=arena.transform.position+Vector3.ClampMagnitude(offset,Mathf.Max(0,arena.radius-radius-.2f));
            Vector3 delta=desired-from;float distance=delta.magnitude;
            if(distance<.001f)return from;
            Vector3 direction=delta/distance;
            Vector3 center=capsule?arena.boss.TransformPoint(capsule.center):from+Vector3.up*1.8f;
            center.y-=arena.boss.position.y-from.y;
            float half=capsule?Mathf.Max(0,capsule.height*arena.boss.lossyScale.y*.5f-radius):1.1f;
            foreach(var hit in Physics.CapsuleCastAll(center-Vector3.up*half,center+Vector3.up*half,radius,direction,distance+.06f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.transform.IsChildOf(arena.boss))continue;
                distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-.06f));
            }
            return from+direction*distance;
        }

        void TickKnightMobilityWarning()
        {
            if(knightMove!=KnightMove.LeapCleave && !KnightBlinkMove)return;
            if(stateTime<knightSpec.warning-knightSpec.lockTime)PlanKnightDestination();
            if(KnightBlinkMove && !knightBlinked && stateTime>=.65f)
            {
                Vector3 from=arena.boss.position;
                knightDestination=SafeKnightDestination(knightDestination);
                Vector3 delta=knightDestination-from;delta.y=0;
                if(delta.sqrMagnitude>.0001f)SweepBoss(delta.normalized,delta.magnitude,false);
                knightDestination=arena.boss.position;knightOrigin=knightDestination;
                Vector3 aim=knightTarget-knightDestination;aim.y=0;
                if(aim.sqrMagnitude>.001f){knightForward=aim.normalized;arena.boss.rotation=Quaternion.LookRotation(knightForward);}
                knightBlinked=true;
                if(knightVisual && knightVisual.refinedEffects)knightVisual.refinedEffects.Blink(this,from,knightDestination);
            }
        }

        void TickKnightMobilityAttack()
        {
            if(knightMove!=KnightMove.LeapCleave)return;
            float t=Mathf.Clamp01(stateTime/.72f);
            Vector3 ground=arena.boss.position;ground.y=arena.transform.position.y;arena.boss.position=ground;
            Vector3 wanted=Vector3.Lerp(knightOrigin,knightDestination,Mathf.SmoothStep(0,1,t));
            Vector3 delta=wanted-ground;delta.y=0;
            if(delta.sqrMagnitude>.0001f)SweepBoss(delta.normalized,delta.magnitude,false);
            Vector3 actual=arena.boss.position;
            actual.y=arena.transform.position.y+4*t*(1-t)*knightDefinition.pursuitLeapHeight;
            arena.boss.position=actual;
            // A late moving obstacle can shorten the landing, so the footprint follows the safe endpoint.
            if(t>=1)knightDestination=new Vector3(actual.x,arena.transform.position.y,actual.z);
            UpdateKnightWarning();
        }

        bool UpdateKnightMobilityWarning()
        {
            if(knightMove!=KnightMove.LeapCleave && !KnightBlinkMove)return false;
            Vector3 center=knightDestination+Vector3.up*.07f;
            float radius=knightMove==KnightMove.LeapCleave?knightSpec.reach:.8f;
            var ring=new Vector3[49];
            for(int i=0;i<ring.Length;i++){float a=i*Mathf.PI*2/48;ring[i]=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
            Material material=knightMove==KnightMove.BlinkBarrage?knightDefinition.absorbable:knightDefinition.blockable;
            SetKnightWarningLine(0,"Locked mobility destination",material,ring,.065f,false);
            if(knightMove==KnightMove.LeapCleave)return true;
            Vector3 aim=knightTarget-knightDestination;aim.y=0;
            Vector3 forward=knightBlinked?knightForward:aim.sqrMagnitude>.001f?aim.normalized:knightForward;
            if(knightMove==KnightMove.BlinkChase)
            {
                var arc=new Vector3[43];arc[0]=arc[42]=center;
                for(int i=0;i<=40;i++)arc[i+1]=center+Quaternion.AngleAxis(Mathf.Lerp(-knightSpec.arc*.5f,knightSpec.arc*.5f,i/40f),Vector3.up)*forward*knightSpec.reach;
                SetKnightWarningLine(1,"Reappearance slash",material,arc,.045f,false);
            }
            else
            {
                // The rays preview flight corridors, not immediate ground damage.
                for(int i=0;i<5;i++)
                {
                    Vector3 direction=Quaternion.AngleAxis((i-2)*12,Vector3.up)*forward;
                    SetKnightWarningLine(i+1,"Seal fan corridor",material,new[]{center+direction,center+direction*knightSpec.reach},.022f,false);
                }
            }
            return true;
        }

        void SpawnKnightBarrage(int wave)
        {
            Vector3 start=arena.boss.position+Vector3.up*1.35f;
            for(int i=0;i<5;i++)
            {
                float angle=(i-2)*12+(wave==1?6:0);
                Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*knightForward;
                var go=arena.Effect(arena.pageProjectile,start+direction*.7f,Quaternion.LookRotation(direction));
                if(knightVisual && knightVisual.refinedEffects)
                    go.AddComponent<ArcaneArchive.Visuals.KnightBoltVisual>().Initialize(this,knightVisual.refinedEffects,knightDefinition.absorbable);
                float speed=knightDefinition.projectileSpeed*.85f;
                shots.Add(new Shot{go=go,velocity=direction*speed,damage=knightSpec.damage,enemy=true,life=knightSpec.reach/Mathf.Max(1,speed),
                    homing=knightDefinition.streamlinedKit,knightBarrage=knightDefinition.streamlinedKit,knightFanOffset=(i-2)*.8f+(wave==1?.4f:0)});
            }
            PlaySound(definition.shotSound,.22f);
        }

        void TickKnightOrbitPursuit(float dt)
        {
            // Reacquire between swings, then commit before each impact; never turn through a hit.
            if(!(stateTime<.08f || stateTime>.35f && stateTime<.88f))return;
            Vector3 toward=Vector3.ProjectOnPlane(transform.position-arena.boss.position,Vector3.up);
            if(toward.sqrMagnitude<.01f)return;
            knightForward=Vector3.RotateTowards(knightForward,toward.normalized,knightDefinition.orbitTurnSpeed*Mathf.Deg2Rad*dt,0);
            arena.boss.rotation=Quaternion.LookRotation(knightForward);
            Vector3 side=Vector3.Cross(Vector3.up,knightForward)*(knightPattern%2==0?1:-1);
            Vector3 direction=toward.magnitude>2.7f?(knightForward+side*.18f).normalized:side;
            float distance=Mathf.Min(knightDefinition.orbitPursuitSpeed*dt,Mathf.Max(0,5.8f-knightTravel));
            knightTravel+=SweepBoss(direction,distance,false);
        }

        void SteerKnightBarrage(Shot shot,float dt)
        {
            shot.age+=dt;
            if(!shot.homing || !knightDefinition)return;
            if(shot.age>1.15f || shot.homingUsed>=knightDefinition.barrageHomingBudget){shot.homing=false;return;}
            if(shot.age<.12f)return;
            Vector3 playerTarget=transform.position+Vector3.up*1.2f;
            Vector3 direct=playerTarget-shot.go.transform.position;
            if(direct.magnitude<1.7f || Vector3.Dot(shot.velocity.normalized,direct.normalized)<=0){shot.homing=false;return;}
            if(!HasClearShot(shot.go.transform.position,playerTarget))return;
            // Distinct offset targets preserve a fan instead of merging all five shots into one.
            Vector3 side=Vector3.Cross(Vector3.up,direct.normalized).normalized;
            Vector3 toward=playerTarget+side*shot.knightFanOffset-shot.go.transform.position;
            float turn=Mathf.Min(knightDefinition.barrageHomingSpeed*dt,knightDefinition.barrageHomingBudget-shot.homingUsed);
            Vector3 old=shot.velocity.normalized;
            Vector3 next=Vector3.RotateTowards(old,toward.normalized,turn*Mathf.Deg2Rad,0).normalized;
            shot.homingUsed+=Vector3.Angle(old,next);
            shot.velocity=next*shot.velocity.magnitude;shot.go.transform.rotation=Quaternion.LookRotation(next);
            MaximumEnemyHomingTurn=Mathf.Max(MaximumEnemyHomingTurn,shot.homingUsed);
        }
    }
}
