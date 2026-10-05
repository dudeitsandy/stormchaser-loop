# Story 002: Settings panel v1

> **Epic**: Run Screens
> **Status**: In Review (2026-10-05) — code + tests done; screenshots with the 0.8.0 build; EFFECTS waits on Codex X8-08
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: 4 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R03 (Layout: Settings, Interaction Map, Data Requirements)
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

One Settings panel opened from Pause and from Title (O / Select), saved through the Save & Profile M1 stem's device file.

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] Rows: CAMERA (SKY / CLASSIC / HIGH), SENSITIVITY, INVERT Y, BRIGHTNESS (−50 % … +50 %, default 0), MASTER / EFFECTS / MUSIC volume, FULLSCREEN, and RESOLUTION on Windows only; each value applies live
- [ ] Values are saved when the panel closes and are still applied after a full reload (session-only if the stem's WebGL check failed)
- [ ] Fully usable by keyboard only, gamepad only and mouse; Esc / B backs out to Pause or Title

---

## Implementation Notes

- Camera presets map to `ChaseCameraRig` default pitch/distance; the SKY default value comes from Sprint 9 S9-02b, use a provisional ≈ 12° until then.
- Device file API from `run-goals-v1` story 005.
- **Brightness** (Andy 2026-10-04, accessibility Basic): a post-exposure offset on the global URP Volume (Color
  Adjustments), applied to the main view **and** the viewfinder (the player needs both brighter); never applied to
  photo scoring or stored covers. Check the viewfinder's camcorder lens (Codex lane) still reads at both extremes.

---

## Out of Scope

- Reduced-motion setting (open question in the spec)
- Key remapping

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: Retained screenshots of Settings from Pause and Title in `production/qa/evidence/` + a PlayMode test for save-and-reload of values

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001; run-goals-v1 story 005 (device file)
- Unlocks: Story 006
