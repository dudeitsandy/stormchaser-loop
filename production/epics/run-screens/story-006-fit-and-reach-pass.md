# Story 006: Screen-size and input reachability pass

> **Epic**: Run Screens
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: 2 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R07 (Acceptance Criteria; Input Method Completeness Checklist)
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

Final pass across every run screen at the real target sizes and inputs.

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] Every run screen fits with no overlap or clipped text at 960×600 (itch embed), 1280×800 and 1920×1080
- [ ] Keyboard only, gamepad only and mouse each reach every menu item; every goal state readable without colour
- [ ] Retained screenshots of Title, Career page, HUD with bounties, Pause, quit confirm, Settings, Wrecked and Results

---

## Implementation Notes

- Verify ✓ ✕ ▪ ◂ ▸ glyphs in the WebGL font; fall back to text where missing.

---

## Out of Scope

- Steam Deck hardware check (post-M1)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: Retained screenshots in `production/qa/evidence/run-screens-*.png`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 001–005
- Unlocks: Run Screens epic done
