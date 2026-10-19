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
        public float projectileSpeed = 8, volleyDamage = 10, waveDamage = 22, sealDamage = 25;
        public float absorbWindow = .65f, absorbCooldown = 2.5f, hurtProtection = .7f;
        public float volleyWarning = 1.1f, waveWarning = 1.4f, sealWarning = 1.5f;
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
        public float fractureWarning = 2.0f, fractureRadius = 1.9f, fractureDamage = 25;
        public float rushWarning = 1.65f, rushLockTime = .6f, rushSpeed = 11.5f;
        public float rushDistance = 11, rushDamage = 32, rushRecovery = 2.4f;
        [Header("Subtle projectile homing")]
        public float enemyHomingSpeed = 10, playerHomingSpeed = 16;
        public float enemyHomingBudget = 12, playerHomingBudget = 9;
        public float playerAcquireAngle = 15, projectileHomingDelay = .12f;
        [Header("Guard")]
        [Min(1)] public float guardCapacity = 100;
        public float guardHoldDrain = 18, guardRegen = 26, guardRegenDelay = 1.5f;
        public float guardDamageCost = 2.8f, guardBreakTime = 1.8f, absorbGuardCost = 14;
        public AudioClip shotSound, warningSound, impactSound, absorbSound;
    }
}
