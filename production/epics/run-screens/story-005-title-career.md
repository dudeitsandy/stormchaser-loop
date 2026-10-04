# Story 005: Title: career strip, career page and paint toggle

> **Epic**: Run Screens
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R06 (Layout: Title, Career page; States: Title variants)
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

Shows what the player is chasing before a run.

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] Title shows "CAREER n/10", reward progress and the next reward (first launch: "CAREER 0/10 · 5 GOALS UNLOCK KTVR PAINT"); hidden while the profile is still loading; C / Y opens the Career page listing all 10 goals done or not
- [ ] Once `livery.ktvr` is owned, L / X toggles STOCK / KTVR paint on the title truck and the choice persists
- [ ] O / Select opens Settings; C, Y, L, X, O, Select and Esc never start a run

---

## Implementation Notes

- Reads the accomplishments record and unlocks (run-goals-v1 stories 005 and 006).

---

## Out of Scope

- Profiles menu, album (post-M1)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: Retained screenshots of Title (first launch and with progress), the Career page and the paint toggle in `production/qa/evidence/`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002; run-goals-v1 stories 005 and 006
- Unlocks: Story 006
