using UnityEngine;

namespace ArcaneArchive.Core
{
    [CreateAssetMenu(menuName = "Arcane Archive/Boss Definition")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string displayName = "缄默典狱长 · 维纶";
        [Min(1)] public float health = 1000;
        [Min(1)] public float playerHealth = 100;
        public float boltDamage = 20, empoweredDamage = 80, shotInterval = .32f;
        public float projectileSpeed = 22, volleyDamage = 10, waveDamage = 22, sealDamage = 25;
        [Header("Crown of forbidden pages")]
        [Min(3)] public int volleyCount = 6, volleyPhaseTwoCount = 8;
        [Min(.02f)] public float volleyInterval = .05f;
        [Min(.1f)] public float volleyReleaseDelay = .20f;
        [Min(.5f)] public float volleyHaloRadius = 1.35f;
        public float absorbWindow = .65f, absorbCooldown = 2.5f, hurtProtection = .7f;
        [Header("Warden presentation")]
        [Range(.5f,1.2f)] public float bossScale=.82f;
        [Min(0)] public float volleyRetreatDistance=4.2f;
        public Material spellRibbon,spellParticle;
        public Material waveSurface;
        public float volleyWarning = 1.25f, waveWarning = 1.4f, sealWarning = 1.5f;
        public float waveSpeed = 4, sealRadius = 1.6f;
        [Header("Charged laser")]
        [Min(.8f)] public float laserChargeTime = 2.2f;
        [Min(.2f)] public float laserLockTime = .65f;
        [Min(.2f)] public float laserDuration = 1.3f;
        [Min(.1f)] public float laserTickInterval = .22f;
        [Min(1)] public float laserDamage = 14;
        [Min(.05f)] public float laserRadius = .30f;
        [Min(1)] public float laserRange = 32;
        [Tooltip("Maximum beam turning speed while firing, in degrees per second.")]
        [Range(0,45)] public float laserTrackingSpeed = 10;
        [Min(0)] public float laserTrackingDelay = .18f;
        [Header("Movement")]
        [Min(.5f)] public float moveSpeed = 2.4f;
        [Min(1)] public float repositionTime = 2.8f;
        public float preferredMinDistance = 6.5f, preferredMaxDistance = 9.5f;
        [Header("Perception and intent")]
        public float perceptionInterval = .7f, visionAngle = 210, bodyTurnSpeed = 65;
        [Header("Fracture and rush")]
        public float fractureWarning = 1.6f, fractureDamage = 18;
        [Header("Pursuing fracture sequence")]
        [Range(.5f,1.5f)] public float fractureLeadTime = .8f;
        [Range(.12f,.4f)] public float fractureInterval = .2f;
        [Min(.8f)] public float fractureFadeTime = .9f;
        [Min(.3f)] public float fractureWidth = 1.8f;
        [Min(.3f)] public float fracturePhaseTwoWidth = 2.2f;
        [Min(1)] public float fractureTravelSpeed = 10.5f;
        [Range(0,180)] public float fractureTurnSpeed = 135;
        [Min(1)] public float fractureDuration = 4.2f;
        [Header("Combat flow")]
        public float standardRecovery = .95f, heavyRecovery = 1.15f;
        public float rushWarning = 1.35f, rushLockTime = .5f, rushSpeed = 75;
        [Min(.1f)] public float rushWakeDuration = .85f;
        public float rushDistance = 11, rushDamage = 32, rushRecovery = 2.4f;
        [Header("Projectile homing")]
        public float enemyHomingSpeed = 95, playerHomingSpeed = 16;
        public float enemyHomingBudget = 85, playerHomingBudget = 9;
        [Min(0)] public float enemyHomingDelay=.04f;
        [Min(.1f)] public float enemyHomingDuration=1.8f;
        [Min(.1f)] public float enemyHomingCommitDistance=1.7f;
        public float playerAcquireAngle = 15, projectileHomingDelay = .12f;
        [Header("Guard")]
        [Min(1)] public float guardCapacity = 100;
        public float guardHoldDrain = 18, guardRegen = 12, guardRegenDelay = 2.5f;
        public float guardDamageCost = 2.8f, guardBreakTime = 1.8f, absorbGuardCost = 14;
        [Header("Blade-staff melee")]
        public float meleeWarning=.78f, meleeDuration=.72f, meleeImpact=.22f;
        public float meleeReach=4.8f, meleeArc=160, meleeDamage=23, meleeRecovery=.85f;
        [Min(0)] public float meleeAdvanceDistance=1.2f;
        [Min(1)] public float meleeTurnSpeed=240, meleeCooldown=4;
        public float poiseCapacity=100, staggerDuration=.9f;
        public AudioClip shotSound, warningSound, impactSound, absorbSound;
    }
}
