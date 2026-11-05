using ArcaneArchive.Player;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        float nextMeleeAt, poise, poiseLastHit;
        bool meleeResolved;
        float meleeAdvanceTravel;
        LineRenderer meleeWarning;
        public int MeleeHitCount { get; private set; }
        public int StaggerCount { get; private set; }
        public float Poise => poise;
        bool MeleeInRange => arena && HorizontalDistance(transform.position,arena.boss.position)<definition.meleeReach+definition.meleeAdvanceDistance;
        void ResetMelee() { nextMeleeAt=poise=poiseLastHit=0;MeleeHitCount=StaggerCount=0;meleeResolved=false; }
        void BeginMeleeWarning()
        {
            meleeResolved=false;meleeAdvanceTravel=0;
            var equipment=GetComponent<PlayerMelee>();
            if(!equipment || !equipment.loadout)return;
            meleeWarning=MeleeEffects.CreateLine("Blade-staff danger arc",arena.effectRoot,equipment.loadout.violetEffect,.026f,33);
        }
        void AdvanceMeleeWarning()
        {
            if(stateTime>=definition.meleeWarning-AimLockDuration)return;
            Vector3 delta=transform.position-arena.boss.position;delta.y=0;
            float budget=definition.meleeAdvanceDistance*Mathf.SmoothStep(0,1,Mathf.Clamp01(stateTime/.36f));
            float requested=Mathf.Min(Mathf.Max(0,budget-meleeAdvanceTravel),Mathf.Max(0,delta.magnitude-1.7f));
            if(requested>.001f)meleeAdvanceTravel+=SweepBoss(delta.normalized,requested,false);
        }
        void TickMeleeStrike()
        {
            if(!meleeResolved && stateTime>=definition.meleeImpact)
            {
                meleeResolved=true;
                bool hitPlayer=MeleeCovers(transform.position,View.transform.position);
                if(!hitPlayer && CheckingDodge)CheckDodge(MeleeCovers(ActiveDodge.StartPosition,ActiveDodge.StartEye),false);
                if(hitPlayer)
                {
                    MeleeHitCount++;
                    combat.ReceiveHit(definition.meleeDamage,arena.boss.position+Vector3.up*1.4f,true,-1,true);
                }
            }
            if(CurrentState==State.Attack && stateTime>=definition.meleeDuration){ClearMeleeVisual();SetState(State.Recovery);}
        }
        void PresentMeleeVisual(float dt)
        {
            if(poise>0 && combatTime-poiseLastHit>2.5f)poise=Mathf.Max(0,poise-18*dt);
            if(!meleeWarning)return;
            if(attack!=5 || (CurrentState!=State.Telegraph && CurrentState!=State.Attack)){ClearMeleeVisual();return;}
            float radius=definition.meleeReach;
            for(int i=0;i<33;i++)
            {
                float angle=Mathf.Lerp(-definition.meleeArc*.5f,definition.meleeArc*.5f,i/32f);
                meleeWarning.SetPosition(i,arena.boss.position+Vector3.up*.04f+Quaternion.AngleAxis(angle,Vector3.up)*arena.boss.forward*radius);
            }
            meleeWarning.startWidth=meleeWarning.endWidth=CurrentState==State.Attack?.065f:.026f;
        }
        void ClearMeleeVisual() { if(meleeWarning)Destroy(meleeWarning.gameObject);meleeWarning=null; }
        public bool TryMeleeHit(float reach,float arc,float damage,float poiseDamage,bool interrupt)
        {
            if(!CanFight || CurrentState==State.PhaseBreak || !arena)return false;
            var body=arena.boss.GetComponent<CapsuleCollider>();
            Vector3 origin=View.transform.position,point=body?body.ClosestPoint(origin):arena.boss.position+Vector3.up*1.5f;
            Vector3 to=point-origin;
            if(to.magnitude>reach || Mathf.Abs(to.y)>1.65f || Vector3.Angle(View.transform.forward,to)>arc*.5f || !HasClearShot(origin,point))return false;
            var melee=GetComponent<PlayerMelee>();
            if(melee && melee.loadout)MeleeEffects.Burst(arena.effectRoot,point,View.transform.forward,melee.loadout.goldEffect,this,false);
            DamageBoss(damage,true);PlaySound(definition.impactSound,.28f);
            if(IsFinished || CurrentState==State.PhaseBreak)return true;
            poise+=poiseDamage;poiseLastHit=combatTime;
            if(interrupt || poise>=definition.poiseCapacity)StaggerBoss(false);
            else if(!KnightMode)arena.model.animatedRig.ReactToHit();
            return true;
        }
        public void StaggerBoss(bool parry)
        {
            if(!CanFight || CurrentState==State.PhaseBreak || CurrentState==State.Stagger)return;
            poise=0;ClearAttacks();SetState(State.Stagger);StaggerCount++;BossIntent="受击失衡";
            Feedback(parry?"完美弹反 · 封印失衡":"封印失衡 · 近身追击",definition.staggerDuration);
        }
    }
}
