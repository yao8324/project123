# Contest interaction and audio feedback

User-approved scope: persistent center reticle, truthful interaction availability, action-specific sounds and story/location ambience. Existing single ending and story progression remain unchanged.

## Design

- Render a font-independent center dot and fine outlined ring throughout free exploration and power guidance. Use a bright ring for an available action, a muted amber ring for a targeted but unavailable action, and a small neutral dot/ring when looking elsewhere. Hide it with gameplay HUD in readers, menus and cinematics.
- Use the same target ray, range, occlusion and story eligibility for both prompts and activation. Explain completed prerequisites, already-completed actions and out-of-order maintenance nodes; never signal success or play object sounds for rejected actions.
- Replace generic interaction beeps with recorded paper, cloth, hardware and door sounds, plus restrained magical feedback for actual power events. UI clicks stay separate; a reader action plays one paper sound instead of stacked click and object sounds.
- Ambience distinguishes calm archive/menu, breach/awakening tension, unstable and restored maintenance, returned-archive investigation, identity/rescue tension and relieved ending. Crossfade with unscaled time while gameplay is running; pause freezes environmental audio and crossfades. Keep UI audible during pause. Checkpoint restore chooses the right context immediately.
- Separate persisted master, effects and environment volume controls. Clamp corrupt values and retain old master setting compatibility. No new dependency or external runtime service.
- Bundle selected audio and author/license/source manifest with stable meta files. Existing combat sound proposals and all unrelated local changes are excluded.

## Verification

Run existing complete contest acceptance in Unity 6000.3.25f1, targeted runtime checks for geometry/occlusion/range/status and recorded audio assets, then capture normal, ready and blocked reticle states and inspect them. Verify pause, reader/escape, rebind, checkpoint and ending still work. Push only this change on a new codex branch after review; leave main and member branches untouched.
