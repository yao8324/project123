using System;
using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Quiet heraldic light, driven solely by the encounter clock.</summary>
    public sealed class KnightOathPresentation : MonoBehaviour
    {
        [Serializable] public struct SealSlot { public Renderer renderer; public int materialIndex; }
        public SealSlot[] seals = Array.Empty<SealSlot>();
        public float PhaseWeight { get; private set; }
        float previousClock = -1;
        MaterialPropertyBlock block;
        public void Present(BossEncounter encounter)
        {
            float clock = encounter.BattleTime;
            if (previousClock >= 0 && clock == previousClock) return;
            if (clock < previousClock) PhaseWeight = 0;
            float dt = previousClock < 0 ? 0 : Mathf.Max(0, clock - previousClock);
            previousClock = clock;
            PhaseWeight = Mathf.MoveTowards(PhaseWeight, encounter.Phase == 2 ? 1 : 0, dt * .8f);
            if (block == null) block = new MaterialPropertyBlock();
            float breath = 1 + .045f * Mathf.Sin(clock * .85f);
            Color emission = Color.Lerp(new Color(.22f, .13f, .035f), new Color(.66f, .36f, .08f), PhaseWeight) * breath;
            foreach (var slot in seals) if (slot.renderer)
            {
                slot.renderer.GetPropertyBlock(block, slot.materialIndex);
                block.SetColor("_EmissionColor", emission);
                slot.renderer.SetPropertyBlock(block, slot.materialIndex);
            }
        }
    }
}
