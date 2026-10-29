using System.Collections.Generic;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        WardenVolleyVisuals volleyCrown;
        WardenRushWake rushWake;
        readonly List<WardenPageImpact> pageImpacts = new List<WardenPageImpact>();
        public int VolleyCount => Phase == 2 ? definition.volleyPhaseTwoCount : definition.volleyCount;
        public int VolleyReleasedCount { get; private set; }
        public int SuspendedPageCount => volleyCrown ? volleyCrown.ReadyCount : 0;
        public float LastRushDuration { get; private set; }
        public WardenVolleyVisuals VolleyCrown => volleyCrown;
        public WardenRushWake RushWake => rushWake;

        void BeginVolleyCrown()
        {
            if (volleyCrown) Destroy(volleyCrown.gameObject);
            VolleyReleasedCount = 0;
            volleyCrown = Instantiate(arena.volleyEffect, arena.effectRoot);
            volleyCrown.Configure(arena.pageProjectile, VolleyCount, definition.volleyHaloRadius);
            TickRitualEffects();
        }

        void BeginRushWake()
        {
            if (rushWake) Destroy(rushWake.gameObject);
            LastRushDuration = 0;
            rushWake = Instantiate(arena.rushWakeEffect, arena.effectRoot);
            rushWake.Configure(definition.rushWakeDuration);
            rushWake.SetPath(rushOrigin, rushDirection, 0, combatTime);
        }

        void TickRitualEffects()
        {
            if (volleyCrown)
            {
                if (attack == 0 && (CurrentState == State.Telegraph || CurrentState == State.Attack))
                {
                    Vector3 anchor = arena.model.mask.position + Vector3.up * 1.15f;
                    volleyCrown.Present(anchor, arena.boss.rotation, lockTarget, combatTime,
                        stateTime, CurrentState == State.Attack, definition.volleyWarning);
                }
                else { Destroy(volleyCrown.gameObject); volleyCrown = null; }
            }
            if (rushWake && !rushWake.Present(combatTime))
            { Destroy(rushWake.gameObject); rushWake = null; }
            for (int i = pageImpacts.Count - 1; i >= 0; i--)
                if (!pageImpacts[i] || !pageImpacts[i].Present(combatTime))
                {
                    if (pageImpacts[i]) Destroy(pageImpacts[i].gameObject);
                    pageImpacts.RemoveAt(i);
                }
        }

        void PageImpact(Shot shot, Vector3 position, bool absorbed)
        {
            if (!shot.enemy || !arena.pageImpactEffect)
            { FlashEffect(arena.impactEffect, position, Vector3.one * .45f, .25f); return; }
            var impact = Instantiate(arena.pageImpactEffect, position, Quaternion.LookRotation(-shot.velocity.normalized), arena.effectRoot);
            impact.Configure(combatTime, absorbed, View.transform.position + View.transform.forward * .45f);
            pageImpacts.Add(impact);
        }

        void ClearRitualEffects()
        {
            if (volleyCrown) Destroy(volleyCrown.gameObject);
            if (rushWake) Destroy(rushWake.gameObject);
            foreach (var impact in pageImpacts) if (impact) Destroy(impact.gameObject);
            volleyCrown = null; rushWake = null; pageImpacts.Clear();
            VolleyReleasedCount = 0; LastRushDuration = 0;
        }
    }
}
