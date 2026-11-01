using ArcaneArchive.Visuals;
using UnityEngine;
namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        WardenWaveVisuals waveCharge;
        public WardenWaveVisuals WaveCharge=>waveCharge;
        WardenWaveVisuals CreateWaveVisual(bool charging)
        {
            var go=new GameObject(charging?"Warden inward wave charge":"Warden engraved shockwave");go.transform.SetParent(arena.effectRoot,false);
            var visual=go.AddComponent<WardenWaveVisuals>();
            visual.Initialize(definition.waveSurface?definition.waveSurface:definition.spellRibbon,definition.spellRibbon,definition.spellParticle,charging);
            return visual;
        }
        void BeginWaveCharge()
        {
            ClearWaveCharge();waveCharge=CreateWaveVisual(true);TickWaveCharge();
        }
        void TickWaveCharge()
        {
            if(!waveCharge)return;
            if(attack!=1 || (CurrentState!=State.Telegraph && CurrentState!=State.Attack)){ClearWaveCharge();return;}
            // The first collapse ends at the actual first release, including its 0.15s follow-through.
            float until=CurrentState==State.Telegraph?definition.waveWarning-stateTime+.15f:(emitted==0?.15f-stateTime:1.40f-stateTime);
            bool awaiting=CurrentState==State.Telegraph || emitted<(Phase==1?1:2);
            float progress=Mathf.Clamp01(1-until/.70f);
            Vector3 origin=arena.boss.position;origin.y=arena.transform.position.y+.045f;
            waveCharge.PresentCharge(origin,progress,awaiting&&until<=.70f&&until>=0,combatTime);
        }
        void ClearWaveCharge(){if(waveCharge)Destroy(waveCharge.gameObject);waveCharge=null;}
    }
}
