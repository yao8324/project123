using System;
using System.Collections.Generic;
using ArcaneArchive.Player;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        KnightDefinition knightDefinition;
        BossDefinition beforeKnightDefinition;
        BossArena beforeKnightArena;
        KnightVisuals knightVisual;
        KnightMove knightMove;
        KnightMoveSpec knightSpec;
        Vector3 knightOrigin,knightForward,knightTarget;
        float knightTravel;
        readonly float[] knightUsedAt=new float[11];
        int knightImpact,knightPattern,knightBlockedHits,knightLast=-1,knightRepeat;
        bool knightStoryEnabled,knightStorySeen;
        GameObject knightWarning;
        readonly List<LineRenderer> knightLines=new List<LineRenderer>();
        public bool KnightMode=>knightDefinition;
        public bool KnightStoryPending { get; private set; }
        public KnightMove KnightCurrentMove=>knightMove;
        public Vector3 KnightLockedTarget=>knightTarget;
        public Vector3 KnightCommittedForward=>knightForward;
        public KnightMoveSpec KnightCurrentSpec=>knightSpec;
        public Vector3 KnightHeavenfallPoint(int index)
        {
            return knightThunderTargets[Mathf.Clamp(index,0,2)];
        }
        public int KnightImpactCount { get; private set; }
        public int KnightBlockedCount { get; private set; }
        public event Action<string> KnightStoryRequested;
        public bool KnightGuarding=>KnightMode && knightMove==KnightMove.Bastion &&
            (CurrentState==State.Telegraph || CurrentState==State.Attack);
        public string PhaseLabel=>KnightMode?(Phase==2?" / 断流誓约":" / 守门誓约"):(Phase==2?" / 封印破裂":"");
        public string ResultTitle=>KnightMode?(CurrentState==State.Won?"圣骑士试炼完成":"突破未成"):(CurrentState==State.Won?"封印已破":"再次被收容");
        public string ResultDescription=>KnightMode?"独立招式原型 · 本次结果不提交主线结局。\n观察锁向，用弹反、盾击、吞印和侧闪寻找反击。":
            CurrentState==State.Won?"典狱长倒下了。\n收容物编号零，正在离馆。":"你体内的力量尚未醒来。\n观察预警，吞噬禁页，再次挑战。";

        public bool BeginKnight(KnightDefinition config,bool storyGate=false)
        {
            if(IsActive || !config || !config.combat || !config.arena || config.moves==null || config.moves.Length<6)return false;
            beforeKnightDefinition=definition;beforeKnightArena=arenaPrefab;
            knightDefinition=config;definition=config.combat;arenaPrefab=config.arena;knightStoryEnabled=storyGate;
            if(Begin())return true;
            RestoreKnightSession();return false;
        }
        void RestoreKnightSession()
        {
            if(!KnightMode)return;
            definition=beforeKnightDefinition;arenaPrefab=beforeKnightArena;
            knightDefinition=null;knightVisual=null;knightSpec=null;KnightStoryPending=false;
        }
        void ResetKnight()
        {
            knightVisual=arena.boss.GetComponent<KnightVisuals>();
            knightPattern=knightImpact=knightBlockedHits=knightRepeat=0;knightLast=-1;
            for(int i=0;i<knightUsedAt.Length;i++)knightUsedAt[i]=-100;
            KnightImpactCount=KnightBlockedCount=0;KnightStoryPending=knightStorySeen=false;
            knightMove=KnightMove.OathCombo;knightSpec=knightDefinition.Get(knightMove);
            Feedback("源石不能离位。外层封闭前，退回门后。",3);
        }
        void TickKnight(float dt)
        {
            stateTime+=dt;
            TickKnightShockwave(dt);
            if(IsFinished)return;
            TickKnightThunderTargets();
            if(knightDefinition.danceStyle && knightMove==KnightMove.Heavenfall && (CurrentState==State.Telegraph || CurrentState==State.Attack))
            {
                float lift=CurrentState==State.Telegraph?Mathf.SmoothStep(0,1,Mathf.Clamp01((stateTime-.45f)/.8f)):
                    1-Mathf.SmoothStep(0,1,Mathf.Clamp01((stateTime-1.6f)/.3f));
                Vector3 position=arena.boss.position;position.y=arena.transform.position.y+lift*knightDefinition.leapHeight;arena.boss.position=position;
            }
            switch(CurrentState)
            {
                case State.Intro:if(stateTime>=2.2f)SetState(State.Observe);break;
                case State.PhaseBreak:if(stateTime>=2.2f)SetState(State.Observe);break;
                case State.Stagger:if(stateTime>=definition.staggerDuration)SetState(State.Recovery);break;
                case State.Observe:
                    Vector3 toward=transform.position-arena.boss.position;toward.y=0;
                    FacePoint(transform.position,dt);
                    float desired=knightDefinition.danceStyle?4.1f:3.2f;
                    if(toward.magnitude>desired)SweepBoss(toward.normalized,Mathf.Min(toward.magnitude-desired,knightDefinition.walkSpeed*dt),false);
                    else if(knightDefinition.danceStyle && toward.magnitude>0.1f)
                    {
                        Vector3 step=Vector3.Cross(Vector3.up,toward.normalized)*(knightPattern%2==0?1:-1);
                        if(toward.magnitude<2.6f)step-=toward.normalized;
                        SweepBoss(step.normalized,knightDefinition.walkSpeed*.45f*dt,false);
                    }
                    if(!knightShockwave && stateTime>=(Phase==2?knightDefinition.phaseTwoObserve:knightDefinition.observeTime))BeginKnightMove(ChooseKnightMove());
                    break;
                case State.Telegraph:
                    if(knightDefinition.danceStyle && knightMove==KnightMove.SealBolts)
                    {
                        float budget=knightDefinition.retreatDistance*Mathf.SmoothStep(0,1,Mathf.Clamp01(stateTime/.42f));
                        knightTravel+=SweepBoss(-knightForward,Mathf.Max(0,budget-knightTravel),false);
                    }
                    if(stateTime<knightSpec.warning-knightSpec.lockTime)
                    {
                        FacePoint(transform.position,dt);knightForward=arena.boss.forward;knightTarget=transform.position;
                    }
                    TickKnightMobilityWarning();
                    UpdateKnightWarning();
                    if(knightMove==KnightMove.Bastion && knightBlockedHits>=2){BeginKnightMove(KnightMove.ShieldBash);break;}
                    if(stateTime>=knightSpec.warning){knightImpact=0;SetState(State.Attack);}
                    break;
                case State.Attack:
                    TickKnightMobilityAttack();
                    if(knightMove==KnightMove.Bastion)
                    {
                        if(knightBlockedHits>=2){BeginKnightMove(KnightMove.ShieldBash);break;}
                    }
                    if(knightMove==KnightMove.SealThrust)
                    {
                        float wanted=knightDefinition.thrustTravel*Mathf.SmoothStep(0,1,Mathf.Clamp01((stateTime-.10f)/.34f));
                        knightTravel+=SweepBoss(knightForward,Mathf.Max(0,wanted-knightTravel),false);
                    }
                    if(knightMove==KnightMove.OrbitSweep)
                    {
                        if(knightDefinition.streamlinedKit)TickKnightOrbitPursuit(dt);
                        else
                        {
                        float wanted=knightDefinition.orbitTravel*Mathf.SmoothStep(0,1,Mathf.Clamp01(stateTime/1.1f));
                        knightTravel+=SweepBoss(Vector3.Cross(Vector3.up,knightForward),Mathf.Max(0,wanted-knightTravel),false);
                        }
                        UpdateKnightWarning();
                    }
                    if(knightMove==KnightMove.BlinkBarrage && knightDefinition.streamlinedKit)
                    {
                        knightTarget=transform.position;
                        Vector3 aim=Vector3.ProjectOnPlane(knightTarget-arena.boss.position,Vector3.up).normalized;
                        if(knightImpact<knightSpec.impacts.Length && aim.sqrMagnitude>.01f)
                        {
                            knightForward=Vector3.RotateTowards(knightForward,aim,knightDefinition.barrageAimSpeed*Mathf.Deg2Rad*dt,0);
                            arena.boss.rotation=Quaternion.LookRotation(knightForward);
                        }
                        UpdateKnightWarning();
                    }
                    ResolveKnightImpacts();
                    if(IsFinished || CurrentState!=State.Attack)break;
                    float duration=knightSpec.duration+(knightMove==KnightMove.OathCombo && Phase==2?.78f:0);
                    if(stateTime>=duration){ClearKnightTelegraph(false);SetState(State.Recovery);}
                    break;
                case State.Recovery:
                    if(knightDefinition.danceStyle && stateTime>knightSpec.recovery*.65f)FacePoint(transform.position,dt*.45f);
                    if(stateTime>=knightSpec.recovery)SetState(State.Observe);break;
            }
        }
        KnightMove ChooseKnightMove()
        {
            if(knightDefinition.danceStyle)
            {
                float combatDistance=HorizontalDistance(arena.boss.position,transform.position);
                float[] weights={combatDistance<6?4f:.15f,combatDistance>6?5f:1f,combatDistance>5?3f:1f,2f,combatDistance<4?1.1f:0,0,combatDistance<7?3.5f:.4f,Phase==2?2.8f:0,combatDistance>4?3.6f:.6f,combatDistance>5?3.2f:.3f,combatDistance<10?2.7f:1.2f};
                float sum=0;
                for(int i=0;i<weights.Length;i++)
                {
                    bool available=knightDefinition.IsMoveEnabled((KnightMove)i) && Array.Exists(knightDefinition.moves,spec=>(int)spec.move==i);
                    if(!available || i==knightLast || (i>=9 && knightLast>=9) || combatTime-knightUsedAt[i]<(i==7?12f:i>=8?9f:i==4?10f:5f))weights[i]=0;
                    sum+=weights[i];
                }
                int chosen=knightLast==(int)KnightMove.SealThrust?(int)KnightMove.SealBolts:(int)KnightMove.SealThrust;
                if(knightDefinition.streamlinedKit)chosen=knightLast==(int)KnightMove.OathCombo?(int)KnightMove.OrbitSweep:(int)KnightMove.OathCombo;
                float pick=UnityEngine.Random.value*sum;
                if(sum>0)for(int i=0;i<weights.Length;i++){pick-=weights[i];if(pick<=0 && weights[i]>0){chosen=i;break;}}
                knightLast=chosen;knightPattern++;return (KnightMove)chosen;
            }
            // Context changes weights; no input-reading or reactive cancellation of committed attacks.
            KnightMove[] order=knightDefinition.danceStyle?
                new[]{KnightMove.OathCombo,KnightMove.OrbitSweep,KnightMove.SealBolts,KnightMove.SealThrust,KnightMove.Bastion,KnightMove.GateCleave,Phase==2?KnightMove.Heavenfall:KnightMove.OrbitSweep}:
                new[]{KnightMove.OathCombo,KnightMove.SealBolts,KnightMove.SealThrust,KnightMove.Bastion,KnightMove.GateCleave};
            KnightMove selected=order[knightPattern++%order.Length];
            float distance=HorizontalDistance(arena.boss.position,transform.position);
            if(distance>8 && selected==KnightMove.OathCombo)selected=KnightMove.SealThrust;
            if(distance>6 && selected==KnightMove.Bastion)selected=KnightMove.SealBolts;
            if((int)selected==knightLast && knightRepeat>=2)selected=KnightMove.GateCleave;
            knightRepeat=(int)selected==knightLast?knightRepeat+1:1;knightLast=(int)selected;
            return selected;
        }
        public void BeginKnightMove(KnightMove move)
        {
            if(!KnightMode || !CanFight || CurrentState==State.PhaseBreak)return;
            ClearAttacks();knightMove=move;knightSpec=knightDefinition.Get(move);
            knightUsedAt[(int)move]=combatTime;
            knightImpact=knightBlockedHits=0;knightTravel=0;
            knightOrigin=arena.boss.position;knightForward=arena.boss.forward;knightTarget=transform.position;
            ResetKnightMobility();
            ResetKnightThunderTargets();
            attack=5;SetState(State.Telegraph);
            knightWarning=new GameObject("Knight telegraph: "+move);knightWarning.transform.SetParent(arena.effectRoot,false);
            string hint=move==KnightMove.Heavenfall?"三道追身雷 · 逐次锁定，落雷前突进离圈":
                move==KnightMove.LeapCleave?"跳劈可挡 · 落地后起跳越过金雷波":knightSpec.hint;
            UpdateKnightWarning();Feedback(hint,knightSpec.warning+1);PlaySound(definition.warningSound,.3f);
        }
        void ResolveKnightImpacts()
        {
            int count=knightSpec.impacts.Length+(knightMove==KnightMove.OathCombo && Phase==2?1:0);
            while(knightImpact<count)
            {
                float time=knightImpact<knightSpec.impacts.Length?knightSpec.impacts[knightImpact]:1.72f;
                if(stateTime<time)break;
                int index=knightImpact++;KnightImpactCount++;
                if(knightVisual)knightVisual.Release(this,index);
                if(knightMove==KnightMove.LeapCleave)StartKnightShockwave();
                if(knightMove==KnightMove.SealBolts){SpawnKnightBolt(index);continue;}
                if(knightMove==KnightMove.BlinkBarrage){SpawnKnightBarrage(index);continue;}
                bool hit=KnightCovers(transform.position,View.transform.position,index);
                if(!hit && CheckingDodge)CheckDodge(KnightCovers(ActiveDodge.StartPosition,ActiveDodge.StartEye,index),false);
                if(hit)
                {
                    bool blockable=knightMove!=KnightMove.GateCleave && knightMove!=KnightMove.Heavenfall;
                    bool parryable=knightMove==KnightMove.OathCombo || knightMove==KnightMove.SealThrust || knightMove==KnightMove.OrbitSweep || knightMove==KnightMove.BlinkChase;
                    float damage=knightSpec.damage*(knightMove==KnightMove.OathCombo?index==0?1:index==1?1.25f:1.4f:1);
                    combat.ReceiveHit(damage,arena.boss.position+Vector3.up*1.5f,blockable,-1,parryable);
                }
                // Parry may synchronously cancel the combo; never resolve later hits afterward.
                if(IsFinished || CurrentState!=State.Attack)return;
                PlaySound(definition.shotSound,.25f);
                if(knightWarning)foreach(var line in knightLines)if(line)line.startWidth=line.endWidth=.11f;
            }
        }
        bool KnightCovers(Vector3 feet,Vector3 eye,int index=0)
        {
            Vector3 from=knightMove==KnightMove.SealThrust?knightOrigin:arena.boss.position;
            Vector3 delta=feet-from;delta.y=0;
            if(Mathf.Abs(feet.y-arena.boss.position.y)>2 || !HasClearShot(arena.boss.position+Vector3.up*1.3f,eye))return false;
            if(knightMove==KnightMove.LeapCleave)return delta.magnitude<=knightSpec.reach;
            if(knightMove==KnightMove.Heavenfall)
                return HorizontalDistance(feet,KnightHeavenfallPoint(index))<=knightSpec.reach;
            if(knightMove==KnightMove.GateCleave)
            {
                // Ground seals can be jumped; the 0.55m height is shown in the design.
                if(feet.y>arena.transform.position.y+.55f)return false;
                foreach(float angle in Phase==2?new[]{-35f,0,35}:new[]{-25f,25})
                    if(InKnightStrip(delta,Quaternion.AngleAxis(angle,Vector3.up)*knightForward,knightSpec.reach,knightSpec.width))return true;
                return false;
            }
            if(knightMove==KnightMove.SealThrust)
                return InKnightStrip(delta,knightForward,knightTravel+Mathf.Max(.1f,knightSpec.reach-knightDefinition.thrustTravel),knightSpec.width);
            return delta.magnitude<=knightSpec.reach && Vector3.Angle(knightForward,delta)<=knightSpec.arc*.5f;
        }
        static bool InKnightStrip(Vector3 delta,Vector3 forward,float reach,float width)
        {
            float along=Vector3.Dot(delta,forward);
            return along>=0 && along<=reach && Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,forward)))<=width*.5f;
        }
        void SpawnKnightBolt(int index)
        {
            Vector3 start=arena.boss.position+Vector3.up*2.9f+arena.boss.right*(index-1)*.55f;
            Vector3 aim=knightTarget+Vector3.up*1.2f;
            Vector3 direction=(aim-start).normalized;
            var go=arena.Effect(arena.pageProjectile,start,Quaternion.LookRotation(direction));
            shots.Add(new Shot{go=go,velocity=direction*knightDefinition.projectileSpeed,damage=knightDefinition.projectileDamage,enemy=true,life=4,homing=false});
            PlaySound(definition.shotSound,.2f);
        }
        bool KnightFrontBlocked=>KnightGuarding && Vector3.Angle(arena.boss.forward,Vector3.ProjectOnPlane(transform.position-arena.boss.position,Vector3.up))<=knightDefinition.guardArc*.5f;
        void DamageKnight(float damage,bool core)
        {
            if(float.IsNaN(damage) || float.IsInfinity(damage))return;
            if(KnightFrontBlocked){damage*=knightDefinition.guardDamageMultiplier;knightBlockedHits++;KnightBlockedCount++;Feedback("正面盾防 · 绕侧或用盾击打断",.65f);}
            if(core && CurrentState==State.Recovery)damage*=1.5f;
            float next=Mathf.Max(0,BossHealth-damage);
            // Story beat has priority over phase/death, including a single huge hit.
            if(knightStoryEnabled && !knightStorySeen && next<=definition.health*knightDefinition.storyThreshold)
            {
                BossHealth=definition.health*knightDefinition.storyThreshold;
                knightStorySeen=KnightStoryPending=true;ClearAttacks();combat.SuppressUntilReleased();
                if(ActiveDodge)ActiveDodge.Cancel(false);
                player.SetGameplayInputEnabled(false);player.SetCursorLocked(false);
                KnightStoryRequested?.Invoke("knight.final_decision");return;
            }
            BossHealth=next;
            if(BossHealth<=0){Finish(true);return;}
            if(Phase==1 && BossHealth<=definition.health*knightDefinition.phaseThreshold)
            {ClearAttacks();Phase=2;SetState(State.PhaseBreak);Feedback("断流誓约 · 我守的门后，也有不能失去的人。",2.2f);}
        }
        public void ResumeKnightTrial()
        {
            if(!KnightStoryPending)return;KnightStoryPending=false;
            // Debug continuation consumes the pending gate once; it does not commit a story choice.
            if(Phase==1){Phase=2;SetState(State.PhaseBreak);}else SetState(State.Recovery);
            menu.Resume();player.SetGameplayInputEnabled(true);player.SetCursorLocked(true);
            player.WaitForReleasedControls();combat.SuppressUntilReleased();
        }
        void UpdateKnightWarning()
        {
            if(!knightWarning)return;
            if(UpdateKnightMobilityWarning())return;
            int lineIndex=0;
            Material mat=knightMove==KnightMove.GateCleave?knightDefinition.unblockable:knightMove==KnightMove.SealBolts?knightDefinition.absorbable:knightDefinition.blockable;
            Vector3 origin=(knightMove==KnightMove.OrbitSweep || knightMove==KnightMove.SealBolts?arena.boss.position:knightOrigin)+Vector3.up*.055f;
            if(knightMove==KnightMove.Heavenfall)
            {
                for(int n=0;n<3;n++)
                {
                    Vector3 center=KnightHeavenfallPoint(n)+Vector3.up*.06f;var points=new Vector3[49];
                    for(int i=0;i<49;i++){float angle=i*Mathf.PI*2/48;points[i]=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*knightSpec.reach;}
                    SetKnightWarningLine(lineIndex++,"Heavenfall locked footprint "+n,knightDefinition.unblockable,points,.06f,false);
                    knightLines[n].enabled=n==knightImpact;
                }
                UpdateKnightFallingThunder(lineIndex);
            }
            else if(knightMove==KnightMove.GateCleave || knightMove==KnightMove.SealThrust)
            {
                foreach(float angle in knightMove==KnightMove.SealThrust?new[]{0f}:Phase==2?new[]{-35f,0,35}:new[]{-25f,25})
                {
                    Vector3 forward=Quaternion.AngleAxis(angle,Vector3.up)*knightForward;
                    Vector3 side=Vector3.Cross(Vector3.up,forward)*knightSpec.width*.5f;
                    SetKnightWarningLine(lineIndex++,"Strip: actual damage bounds",mat,new[]{origin-side,origin+side,origin+forward*knightSpec.reach+side,origin+forward*knightSpec.reach-side},.045f,true);
                }
            }
            else
            {
                float radius=knightMove==KnightMove.SealBolts?1.2f:knightSpec.reach;
                float arc=knightMove==KnightMove.SealBolts?360:knightSpec.arc;
                var points=new List<Vector3>{origin};
                for(int i=0;i<=40;i++)points.Add(origin+Quaternion.AngleAxis(Mathf.Lerp(-arc*.5f,arc*.5f,i/40f),Vector3.up)*knightForward*radius);
                points.Add(origin);SetKnightWarningLine(lineIndex++,"Arc: actual damage bounds",mat,points.ToArray(),.045f,false);
            }
        }
        void SetKnightWarningLine(int index,string name,Material material,Vector3[] points,float width,bool loop)
        {
            if(index>=knightLines.Count)knightLines.Add(KnightVisuals.Line(name,knightWarning.transform,material,points,width,loop));
            else
            {
                var line=knightLines[index];line.sharedMaterial=material;line.positionCount=points.Length;line.SetPositions(points);
                line.loop=loop;line.startWidth=line.endWidth=width;
            }
        }
        void ClearKnightTelegraph(bool cancelEffects=true)
        {
            if(KnightMode && arena && (knightMove==KnightMove.Heavenfall || knightMove==KnightMove.LeapCleave))
            {var point=arena.boss.position;point.y=arena.transform.position.y;arena.boss.position=point;}
            if(knightWarning)Destroy(knightWarning);knightWarning=null;knightLines.Clear();
            if(cancelEffects && knightVisual)knightVisual.CancelEffects();
        }
    }
}
