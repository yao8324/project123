# Knight Sanctuary Implementation Plan
> Execute inline with superpowers:executing-plans. User has explicitly chosen the concept and requested Unity construction; no additional design permission is needed.
**Goal:** Deliver a detailed playable Unity sanctuary matching Concept.png.
**Architecture:** Additive art prefab and cloned knight config/test entry over immutable source combat. Procedural editor authoring creates editable meshes/materials/props; runtime presentation owns only lighting and visual state.
**Tech Stack:** Unity 6000.3.25f1, URP 17.3, C#, original authored PBR textures.
**Spec:** Design.md
## Global Constraints
- Radius 14 m, ground Y=0, all original collisions and boss/spawn contract; solid prop colliders beyond the clear ring.
- Original gameplay scripts, published arena and knight config remain unchanged.
- No new storyline decisions, no remote integration.
## Review Focus
- Runtime clone at x=160: all lighting and effect anchors use local coordinates.
- Clear, level ring for blink, jump and melee.
- Readability in normal gameplay and both phases, not only photo cameras.
- Original library lighting restored after return.
- Generated artifacts remain valid after editor restart and source regeneration.
### Task 1: Baseline and validation harness
- [x] Independent complete 1.0.8 checkout with LFS; compile original baseline.
- [x] Record source hashes and original collision signature; add acceptance requiring new arena/entry (fails until generated).
### Task 2: Sanctuary assets
- [x] Revision 2 upper architecture and updated visuals; Assets/Scripts/Visuals/Editor/KnightSanctuaryGeometry.cs provides reusable mesh primitives.
- [x] KnightSanctuaryBuilder.cs generates architectural modules, PBR materials, props and lights under Assets/Art/Bosses/Knight/Sanctuary20261107.
- [x] Independent prefab/config and Assets/Scenes/BossTests/Knight_Sanctuary_Test.unity.
- [x] Edit-mode asset validation, actual rendered review and refinement.
### Task 3: Runtime presentation and acceptance
- [x] KnightSanctuaryLighting.cs supports clone-relative ambient and phase change, restores state.
- [x] KnightSanctuaryAcceptance.cs plays actual scene, checks collision/routes, attacks, lifecycle; captures actual camera.
- [x] Inspect Revision 2 screenshots and repair aesthetic/technical faults.
### Task 4: Delivery
- [x] Optional standalone Mac preview omitted: user requested editable Unity construction; target-machine build/playtesting remains separate.
- [x] Revision 2 final review, resolve findings and update delivery evidence.
