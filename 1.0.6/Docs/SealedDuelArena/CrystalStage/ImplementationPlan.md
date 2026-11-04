# Crystal scene and sound selection implementation plan

Goal: deliver a selectable complete audition catalog and first built crystal arena scene while the manager chooses sounds.
Architecture: reuse the approved arena prefab and add three perimeter assemblies. Visual-only state APIs prepare intact, exposed and broken looks; gameplay damage/regen integration is a later stage. Keep audio selection entirely outside Unity.
Tech: existing Unity 6000.3 URP, local HTML/JS, WAV assets.
Spec: ../NextIteration/锁链护晶_音效与特效清单.md and current user instruction.
Ruling: user explicitly asks to start the scene while choosing sounds; build visual stage now without choosing sounds or claiming completed combat mechanics. Use existing isolated art checkout; no remote publication.

- [x] Download new crisp metal and stone fracture candidates; retain source/license provenance and previous choices.
- [x] Reduce each ambient loop another 8 dB; keep prior versions available in archive.
- [x] Build complete audition catalog including V2, V3, new V4, and original samples. Stable IDs, checkboxes, persistent localStorage, selected-only view, notes and JSON export/import. Default nothing selected. Verify selection survives reload and import/export.
- [x] Add CrystalArenaVisuals.cs: animate chain roots, rising rune glyphs and three thin supply lines; explicit SetChainBroken / SetCrystalBroken / ResetVisuals APIs. No AudioSource or damage logic.
- [x] Add CrystalArenaBuilder.cs: beveled multi-facet amethyst crystal meshes, interlocked elliptical iron links, metal socket, etched concentric bases, bronze claws, purple local light; perimeter radius >=14.7m, center clear. Save prefab changes and screenshots.
- [x] Run Unity compile and builder, state assertion tests, central-path physics checks and image inspection. Record actual results and limits; do not claim runtime boss gameplay integration.
- [x] Fresh review for catalog persistence and scene safety, fix important issues.

Results: 63 audio entries probed; loop gain measured -7.994 and -8.000 dB. Browser check/reload/filter/export trigger verified; import only code reviewed. Unity compile, state assertions and 72 capsule diameter checks passed. Review fixes: preview served within HTTP root; standalone capture guards dirty scenes and restores saved scene setup (empty unnamed setup restores empty scene). No remote publish, gameplay integration or native app rebuild.
