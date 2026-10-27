using UnityEngine;

namespace ArcaneArchive.Visuals
{
    public sealed class BossArena : MonoBehaviour
    {
        public Transform spawn, boss;
        public WardenVisuals model;
        public GameObject pageProjectile, ringEffect, boltEffect, impactEffect;
        public LaserBeamVisuals laserEffect;
        public GroundFractureVisuals fractureEffect;
        public RushTelegraphVisuals rushEffect;
        public WardenVolleyVisuals volleyEffect;
        public WardenRushWake rushWakeEffect;
        public WardenPageImpact pageImpactEffect;
        public Transform effectRoot;
        public float radius = 14;

        public GameObject Effect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var go = Instantiate(prefab, position, rotation, effectRoot);
            go.SetActive(true);
            return go;
        }
    }
}
