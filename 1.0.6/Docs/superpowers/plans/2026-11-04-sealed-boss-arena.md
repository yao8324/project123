# Sealed Boss Arena Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Build the approved unobstructed Gothic boss arena as a playable Unity environment.
**Architecture:** Generate editable modular meshes and PBR materials into Assets/Art/SealedDuelArena20261104; clone the combat prefab preserving gameplay references, replace environment only, create a direct-combat showcase and link the normal game Boss entrance.
**Tech Stack:** Unity 6000.3.25f1, URP, C# Editor builders and existing real runtime acceptance drivers, deterministic procedural textures.
**Spec:** docs/superpowers/specs/2026-11-04-sealed-boss-arena-design.md

## Global Constraints
- Work from art 0253ec7 on codex/sealed-boss-arena-20261104. Preserve user root changes and all unrelated archives.
- All new files are inside this Unity project; maintain matching .meta GUIDs.
- Keep Boss mechanics and radius 14. Centre radius 12m remains clear and flat. No maintenance-machine concept.
- No remote push/merge or manager acceptance claims.

## Review Focus
- Environment colliders must not intersect the player capsule or sweep paths in the combat disk.
- Cloned prefab must preserve every effect reference, model rig and spawn.
- Return-to-library and retry must work after repeated loads; new lights must disappear with arena destruction.
- Scene and asset generation must be repeatable without losing GUIDs or unsaved work.
- Building must show actual geometry/material improvements, not substitute concept-image planes or dark lighting.

### Task 1: Arena contract checks
**Files:** Create Assets/Scripts/Visuals/Editor/SealedDuelArenaAcceptance.cs.
**Interfaces:** RunStatic() reads target Prefab/scene and throws with concrete failure; emits Docs/SealedDuelArena/StaticChecks.json.
- [x] Write checks for target existence and preserved Boss references; central capsule grid, safe spawn, enclosed boundary.
- [x] Run against absent target and record the expected RED.

### Task 2: Environment art and connection
**Files:** Create Assets/Scripts/Visuals/Editor/SealedDuelArenaGeometry.cs, SealedDuelArenaBuilder.cs; Tools/SealedDuelArena/generate_textures.py; generated Assets/Art/SealedDuelArena20261104/{Meshes,Materials,Textures,Prefabs}; Assets/Scenes/SealedDuelArena_Showcase.unity. Modify only arena reference in Assets/Scenes/MilanLibrary_Magic_V6.unity.
**Interfaces:** Builder.Build() creates the environment, preserves root BossArena references, builds two scene entry points; Geometry provides mesh primitives and batching.
- [x] Build stone, basalt and bronze materials with non-stretched UVs and normal/roughness variation.
- [x] Build floor, arches, columns, gallery, engraved gate, vault breach, peripheral dressing and lighting; no central obstruction.
- [x] Save cloned arena and scene, preserve player/Boss/effect linkage and old archive.
- [x] Run static checks GREEN and inspect actual Unity screenshots; refine until the approved target is recognizable.

### Task 3: Runtime and handoff
**Files:** Add runtime validation/capture to SealedDuelArenaAcceptance.cs; create Docs/SealedDuelArena/{Evidence,README.md,verification.json}.
- [x] Actual Play Mode: entry, floor traversal, boss attacks/telegraphs, pause, retry, victory/return. Reuse WardenFlowAcceptance / WardenRitualAcceptance as appropriate, record failures by name.
- [x] Capture actual entrance, gallery, breach, gate and battle views at normal gameplay exposure; compare with concept.
- [x] Build native Mac playable, launch it and confirm interactive combat; include clear start instructions.
- [x] Final independent review of changed source, scene/Prefab linkage and verification evidence; fix important defects and leave complete local delivery.
