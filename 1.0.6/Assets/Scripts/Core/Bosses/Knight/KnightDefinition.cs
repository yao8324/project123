using System;
using UnityEngine;
using ArcaneArchive.Visuals;

namespace ArcaneArchive.Core
{
    public enum KnightMove { OathCombo, SealThrust, SealBolts, GateCleave, Bastion, ShieldBash, OrbitSweep, Heavenfall }

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
        public KnightMoveSpec[] moves;

        public KnightMoveSpec Get(KnightMove move)
        {
            foreach(var spec in moves)if(spec.move==move)return spec;
            throw new InvalidOperationException("Knight move is missing: "+move);
        }
    }
}
