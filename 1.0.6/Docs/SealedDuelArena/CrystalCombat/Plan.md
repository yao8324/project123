# Crystal combat implementation

User goal: finish attackable chain/crystal, 10% additive DR per live crystal, continuous regen, one all-broken stagger; match supplied concept image; no new audio selection.

- Add BossPowerNetwork + CrystalCombatTarget to runtime arena. Defaults chain 60HP, crystal 80HP, regen .15% maxHP/sec per crystal, 2s stagger, inspector-tunable. Ignore invalid damage, closed combat, broken targets; no overflow; retry resets state.
- Route real spherecast projectile impacts and crystal-priority aim assist, leaving enemy attacks passing through trigger hit volumes. Add HUD state and contextual target health.
- Integrate regen into combat tick after pause/finished checks, damage modifier after core bonus, all-broken cancels attacks and enters dedicated Stagger state. No regression of phase after regen. Phase crossing during stagger deferred until stagger completes.
- Runtime acceptance: real production Begin + firing, projectile hits chain first then crystal, all DR stages, capped regen, pause/finish no regen, all-break cancels attacks exactly once, timer, phase crossing, retry/return/reentry. Compile and native app build, run screenshot evidence.
- Continue environment fidelity: clear center, dark rich materials, starry broken vault, three violet supply streams, dimensional chains, detailed sockets. Compare runtime captures against concept; document gaps rather than mark complete prematurely.
