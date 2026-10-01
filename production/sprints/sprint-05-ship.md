# Sprint 5 — Ship It (2026-10-01 → 2026-10-14)

## Goal
Put the Unity 3D build in front of players on itch.io as **Doomsday: Storm Season (prototype 0.4)**.
One mode (90-second Sprint), one disaster (tornado, EF0–EF5), one vehicle. Everything in
vision-1.0 beyond that is post-ship.

## Why this cut
Six months after the pivot the Unity build is ~600 lines and a session that ends with a
`Debug.Log`. The vision doc describes four seasons of content; none of it matters until a
stranger can download a build, chase a tornado for 90 seconds, and want to do it again.
Steam is not "asap" (store review + coming-soon period), so the target is itch.io, where the
2D v0.3.0 already has a page.

## Ship gate (all must be true)
- [ ] Title → 90s run → Results → Retry loop works with keyboard and gamepad
- [ ] Every shutter press gives feedback (flash + score popup + tier, or a "no subject" miss)
- [ ] PiP viewfinder shows what the shot will frame
- [ ] Tornadoes wander, grow, dissipate, and hurt you; 3 hits wrecks the truck and ends the run
- [ ] Off-screen tornado indicator
- [ ] World has props (not a green plane) and boundaries
- [ ] Audio: shutter, wind by proximity, engine, damage hit
- [ ] Best score persists between sessions
- [ ] Windows build + WebGL build produced by a script; pushed to itch.io
- [ ] 60 FPS on dev machine with post-FX on

## Lanes
Two agents share this working tree. File ownership is the conflict-avoidance mechanism.

| Lane | Owner | Task | Files |
|------|-------|------|-------|
| C1 | Claude | Run flow: title / running / results / retry, time freeze, best score | `Scripts/Core/*`, `Scripts/Session/*` |
| C2 | Claude | Vehicle damage: 3 HP, i-frames, knockback, wreck ends run | `Scripts/Vehicle/*` |
| C3 | Claude | Tornado behavior: wander, lifecycle, EF roster, spawn around player | `Scripts/Tornado/*`, `Scripts/Disaster/*` |
| C4 | Claude | HUD (HP, time, score), scene wiring, world bounds | `Scripts/UI/HudController.cs`, `Scenes/*` |
| C5 | Claude | Build script (Win + WebGL), itch.io push | `Assets/Editor/*`, `tools/build/*` |
| X1 | Codex | Photo feedback: flash, "+XXX" popup, PERFECT / GOOD SHOT / GLANCING | `Scripts/Presentation/*` |
| X2 | Codex | PiP viewfinder (ADR-0002): runtime camera + RenderTexture, HUD bottom-right | `Scripts/Presentation/*` |
| X3 | Codex | Off-screen tornado indicator | `Scripts/Presentation/*` |
| X4 | Codex | Procedural audio: shutter, wind, engine, hit, run-end sting | `Scripts/Presentation/Audio/*` |
| X5 | Codex | Environment props + scatter (farmhouses, silos, fences, poles, trees) | `Scripts/Environment/*` |

Integration contract: `Assets/Scripts/Core/GameEvents.cs`. See `AGENTS.md` for rules.

## Explicitly cut from this ship
Wildfire/hail, disaster alchemy, intervention scoring, Chase mode, garage/meta economy,
Cataclysm Heat, stunts/style, minimap, TV weather alert, Steam integration.
Those are the Early Access roadmap, not the prototype.

## Decisions
- 2026-10-01: Project is on Unity **6000.6.0f1** (only installed editor) with Cinemachine 6.6.0.
  The editor bump was already sitting uncommitted; it compiles clean. Treat 6.6 as the pinned
  version going forward (update `VERSION.md` + technical-preferences when committing).
- 2026-10-01: Shot tier uses framing quality (≥0.85 PERFECT, ≥0.5 GOOD), not raw score.
  S4-07's absolute thresholds made PERFECT impossible below EF3.
- 2026-10-01: Tests were never running (no test asmdef). Added `StormChaser.asmdef` +
  `StormChaser.Tests.asmdef`; moved the generated input wrapper into `Scripts/Input/`.

## Playtest Notes — 2026-10-01 (Andy)
- Film (24) is plenty for a 90s run; gameplay is hectic enough that shot count is a good **mode
  knob**: e.g. a "4 shots only" mode, or film that varies per mode / dynamically. → session-modes.md
- Wind risk/reward isn't legible yet. Polish later: wind physics feel + secondary effects
  (debris from destruction, dust, flying props) so the player *sees* the danger and the bonus.
- Frame rate fine with PiP + props + audio.

## Post-Ship Backlog (from playtests)
| Item | Lane | Notes |
|------|------|-------|
| Film as a per-mode tuning knob (incl. "4 shots" mode) | Claude | `PhotoTrigger._filmPerRun` already data-driven; needs mode config |
| ~~Wind readability: live "IN THE WIND ×1.4" HUD meter~~ | Claude | **Done pre-ship 2026-10-01** |
| Debris / dust VFX scaled by wind field | Codex | the "secondary effects" ask |
| Wind physics polish (gusts, truck lift/tilt) | Claude | after Vehicle Feel pass |
| **Vehicle Feel pass — Rocket League target** | Claude | Replace arcade direct-velocity with real rigidbody vehicle: weight transfer on accel/brake, powerslide/handbrake turns, suspension over uneven terrain, air control, tangible crash impacts (camera kick, debris, damage tied to impact force), wind as a real force on the body. Vision-1.0 Pillar 1 (Kinetic Chaos), planned Sprint 6-7 alongside stunt/style scoring. Depends on terrain becoming a large-scale, uneven, destructible space — design both together, not before. Requested 2026-10-01. |
| Large-scale destructible terrain | Claude + Codex | Prerequisite for the Vehicle Feel pass to matter (bumps, ramps, debris to drive over/through). Needs its own ADR. |
