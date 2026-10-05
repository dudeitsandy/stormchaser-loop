# Story 001: Pause menu and quitting

> **Epic**: Run Screens
> **Status**: Done (2026-10-05) — Andy's Windows check on 0.8.5: Title Esc-Esc quits ✅, pause → Quit to Desktop ✅, Alt-tab auto-pause ✅
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R01, RS-R02 (Entry & Exit Points, States, Interaction Map, Events Fired)
*(No `tr-registry.yaml` yet; IDs are local to `production/epics/run-screens/EPIC.md`.)*

**ADR Governing Implementation**: N/A — UI and run-state flow over existing `RunManager`; no architectural pattern beyond the UX spec
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: MEDIUM (UI Toolkit runtime + new Input System are post-cutoff;
check `docs/engine-reference/unity/` before using an API)
**Engine Notes**: HUD and run screens are UI Toolkit built in code (`RunScreens`, `HudController`). Input via the
new Input System (`StormChaserControls`; the generated wrapper regenerates only on asset import — two-phase
batchmode if new actions are added). Time scale is owned by `RunManager`.

**Control Manifest Rules (this layer)**: N/A — manifest not yet created. Project rules: no `FindObjectOfType` in
Update, new Input System only.

Andy (2026-10-04): "i need a better way to quit the windows build and a pause menu with resume/quit/settings". Pause, forfeit and quit are `RunManager` state; the pause panel only requests them.

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] P, gamepad Start, or Esc (where the browser passes it) opens Pause within 50 ms: world and timer freeze; Resume continues with the same time left (±0.05 s). Losing window/tab focus, leaving fullscreen or a controller disconnect auto-pauses; nothing auto-resumes
- [ ] Pause menu: RESUME (default focus), SETTINGS (placeholder "COMING SOON" until story 002), QUIT RUN, and QUIT TO DESKTOP on Windows only; navigable by W/S, arrows, d-pad, stick, mouse; Enter / A / click picks; Esc / B backs
- [ ] QUIT RUN / QUIT TO DESKTOP open a confirm "QUIT RUN? NOTHING FROM THIS RUN IS BANKED." with KEEP PLAYING as default focus; confirming Quit Run returns to Title and raises `RunForfeited` (no run end, nothing banked); Quit to Desktop exits the app
- [ ] Windows Title: Esc shows "QUIT GAME? ESC AGAIN"; a second Esc exits. WebGL shows neither desktop quit

---

## Implementation Notes

- `RunManager`: new `Paused` state (or flag) owning `Time.timeScale` and `AudioListener.pause`; `GameEvents.PauseChanged(bool, reason)` and `RunForfeited` (+ `ResetStatics`). Pause input ignored in Title/Ending/Results and during the Wrecked beat (story 003).
- Focus loss: `OnApplicationFocus(false)` / `OnApplicationPause(true)`; controller disconnect: `InputSystem.onDeviceChange` Removed for the active gamepad.
- Add a Pause action (P, Esc, Start) to `StormChaserControls` (two-phase import) or read the devices directly; menu UI in `RunScreens` (UI Toolkit, unscaled time).
- `Application.Quit()` behind `Application.platform != WebGLPlayer`.

---

## Out of Scope

- Story 002: the Settings panel itself
- Story 003: Wrecked beat

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: PlayMode test `unity/StormChaserLoop3D/Assets/Tests/PlayMode/PauseTests.cs` (freeze, resume time, forfeit, auto-pause) + retained screenshots of Pause and the quit confirm in `production/qa/evidence/`

**Status**: [x] `Tests/Gameplay/PauseMenuTests.cs` (6) + `Tests/PlayMode/PauseTests.cs` (4) passing; WebGL screenshots `production/qa/evidence/run-screens-{pause,pause-frozen,quit-confirm,resumed}.png`

---

## Dependencies

- Depends on: None
- Unlocks: Story 002
