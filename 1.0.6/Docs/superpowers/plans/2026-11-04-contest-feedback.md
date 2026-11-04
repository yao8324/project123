# Contest Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Make aiming and interaction outcomes clear and replace uniform beeps/low drone with contextual audio.

**Architecture:** A pure interaction policy supplies action labels and eligibility to both HUD and activation. A custom UI Graphic draws the reticle; a dedicated audio director owns recorded clips, independent buses and ambience crossfades. The existing story state remains authoritative.

**Tech Stack:** Unity 6000.3.25f1, C#, Unity UI, Input System, bundled WAV/OGG.

**Spec:** `../specs/2026-11-04-contest-feedback-design.md`

## Global Constraints

- Only the user-approved reticle, prompts and audio change; preserve current story and ending.
- All project artifacts remain inside `1.0.6/`; preserve GUID/meta files and unrelated work.
- Compile and inspect Unity errors after script edits. Validate the actual contest scene.
- The publishing action is authorized. Push `codex/contest-feedback-20261104`; do not merge main.

## Review Focus

- Ready prompt versus actual rejected activation, including completed nodes, disabled props and a wall between player and target.
- Reader close, Escape and rebind produce correct cue/label and no stacked clicks.
- Pausing mid-crossfade or mid-power and returning to menu leaves no stale environment/power loop.
- Checkpoint restoration and completed-game Continue select the correct ambience.
- Missing clips, repeated blocked presses and low/zero/corrupt volume settings do not crash or create misleading sounds.

## Task 1 Interaction feedback

Files: add `Assets/Scripts/Interaction/ContestInteractionFeedback.cs`, `Assets/Scripts/UI/ContestReticle.cs`; modify `ContestStoryGame.cs` and `ContestHud.cs`; add `Assets/Scripts/Visuals/Editor/ContestFeedbackAcceptance.cs`.

Interfaces: `ContestInteractionFeedback.Evaluate(ContestStoryState, string, bool)` returns availability, action text and reason; `ContestStoryGame.GetInteractionFeedback(string)` exposes policy; `ContestReticle.Present(bool,bool)` updates geometry state.

- [x] Run existing acceptance for baseline.
- [x] Add a runtime contract check, observe missing reticle/policy fail, then implement geometry and shared policy.
- [x] Exercise ready, blocked, done, range and obstruction against actual scene and inspect screenshots; compile without C# errors.
- [x] Commit the cohesive interaction change.

## Task 2 Contextual sound

Files: add `Assets/Scripts/Core/ContestAudioDirector.cs` and `Assets/Contest20261107/Resources/ContestAudio/`; modify story/HUD integration; store sources and license notes under `Docs/Feedback20261104/`.

Interfaces: audio director owns cue playback, story-context selection, power loop and volume buses. HUD calls semantic reader/UI feedback; story plays cues at successful actions.

- [x] Add runtime checks for distinct paper cues, ambience context and pause/volume lifecycle; observe failure before implementation.
- [x] Acquire and document recorded audio sources, prepare compact loops, implement crossfade and classified cues.
- [x] Add master/effects/environment sliders and persisted clamped values; verify all contexts and pause/reader/power behavior.
- [x] Run full contest suite, commit audio change and evidence.

## Task 3 Review and delivery

- [x] Run final targeted and complete runtime checks, inspect screenshots and verify clip signals are non-silent.
- [ ] Fresh whole-branch review; address important findings and rerun their checks.
- [ ] Commit selected code, assets, meta files and concise delivery notes. Verify clean diff and push only the feature branch.
- [ ] Read remote head to confirm the uploaded commit and report the branch URL and verification limits.
