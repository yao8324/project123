using System;
using UnityEngine;
using ArcaneArchive.Visuals;

namespace ArcaneArchive.Core
{
    public enum KnightMove { OathCombo, SealThrust, SealBolts, GateCleave, Bastion, ShieldBash, OrbitSweep, Heavenfall, LeapCleave, BlinkChase, BlinkBarrage }

    [Serializable]
    public sealed class KnightMoveSpec
    {
        public KnightMove move;
        public string title, hint;
        public float warning, lockTime, duration, recovery, damage, reach, width, arc;
        public float[] impacts;
    }

    [CreateAssetMenu(menuName="Arcane Archive/Knight Definition")]
    public sealed class KnightDefinition : ScriptableObject
    {
        public BossDefinition combat;
        public BossArena arena;
        public Material blockable, unblockable, absorbable;
        public float phaseThreshold=.55f, storyThreshold=.20f;
        public float observeTime=.95f, phaseTwoObserve=.7f, walkSpeed=2.5f;
        public float guardDamageMultiplier=.2f, guardArc=120, thrustTravel=4;
        public float projectileSpeed=14, projectileDamage=12, projectileInterval=.32f;
        [Header("Loshide motion reference adaptation")]
        public bool danceStyle;
        public float retreatDistance=2.2f, orbitTravel=2.4f, leapHeight=1.15f;
        [Header("1.0.8 mobility")]
        public float pursuitLeapHeight=2.6f, pursuitLeapTravel=9f, blinkTravel=9f, sideBlinkTravel=5.5f;
        public KnightMoveSpec[] moves;
        public bool streamlinedKit;
        public float orbitPursuitSpeed=6.5f, orbitTurnSpeed=140f;
        public float barrageAimSpeed=110f, barrageHomingSpeed=120f, barrageHomingBudget=55f;
        public bool IsMoveEnabled(KnightMove move)
        {
            return !streamlinedKit || move==KnightMove.OathCombo || move==KnightMove.OrbitSweep ||
                move==KnightMove.Heavenfall || move==KnightMove.LeapCleave ||
                move==KnightMove.BlinkChase || move==KnightMove.BlinkBarrage;
        }

        // Explicit asset migration: old trial configurations remain playable without new moves.
        public void AddMobilityMoves()
        {
            streamlinedKit=true;
            var list=new System.Collections.Generic.List<KnightMoveSpec>(moves ?? new KnightMoveSpec[0]);
            AddMove(list,new KnightMoveSpec{move=KnightMove.LeapCleave,title="踏光·跳劈",hint="落点光环锁定后侧闪；落地重劈可举盾",warning=.95f,lockTime=.4f,duration=1.08f,recovery=1.2f,damage=28,reach=3.8f,arc=360,impacts=new[]{.72f}});
            AddMove(list,new KnightMoveSpec{move=KnightMove.BlinkChase,title="追誓·瞬斩",hint="光环预告瞬移落点；显形后侧闪或弹反",warning=1.2f,lockTime=.65f,duration=.66f,recovery=1.05f,damage=24,reach=4.8f,arc=140,impacts=new[]{.25f}});
            AddMove(list,new KnightMoveSpec{move=KnightMove.BlinkBarrage,title="折光·封印幕",hint="侧向闪现后一轮扇形封印弹；横移穿隙或吞印",warning=1.35f,lockTime=.8f,duration=.55f,recovery=1.25f,damage=8,reach=18,arc=56,impacts=new[]{.12f}});
            moves=list.ToArray();
            // Migrate existing production assets as well as freshly built configurations.
            var barrage=Get(KnightMove.BlinkBarrage);
            barrage.impacts=new[]{.12f};barrage.duration=.55f;
            barrage.hint="侧向闪现后一轮扇形封印弹；横移穿隙或吞印";
        }
        static void AddMove(System.Collections.Generic.List<KnightMoveSpec> list,KnightMoveSpec spec)
        {if(!list.Exists(item=>item.move==spec.move))list.Add(spec);}

        public KnightMoveSpec Get(KnightMove move)
        {
            foreach(var spec in moves)if(spec.move==move)return spec;
            throw new InvalidOperationException("Knight move is missing: "+move);
        }
    }
}
