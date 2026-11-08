# Soulbinder V2 complete encounter implementation
Spec: ../V2/缚魂者_召唤核心战_V2.md
Approved for direct execution by user, 2026-11-08.

1. Add independent deterministic encounter model and batch acceptance harness. Test intro simultaneous slots, atomic refill, interruption, anchor cancellation, terminal precedence and retry. New code Assets/Scripts/Core/Soulbinder; no changes to existing Warden combat.
2. Export R10 hall and six accepted character revisions through Blender background. Merge environment meshes by material and spatial group; preserve anchor/console interaction positions. Export runtime geometry and material manifests; retain editable originals. Validate bounds/triangle counts.
3. Add Soulbinder runtime controller, actor AI, shared pressure budget, input using LibraryInputBindings/FirstPersonController, projectile absorption, guard, HUD, pause/retry, atomic checkpoint and postcombat console.
4. Add editor builder to create standalone Assets/Scenes/SoulbinderComplete.unity and Mac build. Use accepted models/materials, collision, navigation and lighting. Preserve existing build scenes.
5. Compile with Unity 6000.3.25f1, run deterministic and runtime acceptance routes, capture actual rendered scene, produce playable package and instructions. Final independent review and targeted fixes.

Interfaces: EncounterModel owns slots/states/timers; Runtime mirrors slots and delivers damage, UI observes it. Runtime save separate from ContestCheckpointStore. Exported FBX coordinates converted once at export. Scene builder assigns refs explicitly.
Ruling: Work inside existing feature checkout, adding isolated namespace/assets; previous uncommitted art and combat changes must remain intact. Do not auto-publish. Existing art push email verification failure is unrelated.
