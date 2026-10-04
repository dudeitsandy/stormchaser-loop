# Story 004: Results: two columns, goals and save messages

> **Epic**: Run Screens
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: 4 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R05 (Layout: Results, States: Results variants, Interaction Map: input lock)
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

Adds the goals tally to the existing results (score, best, photos, weather, seed).

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] Two columns (score/stats/weather left, goals right; one column below 1000 px wide); goals list each completion with its short name and bonus, NEW on first-ever, the career count, "NO GOALS THIS RUN" when none
- [ ] The unlock banner shows when the livery is earned; a failed save write shows the toast "COULDN'T SAVE — PROGRESS KEPT, WILL RETRY"; session-only persistence shows the storage line
- [ ] Results ignore input for the first 1.0 s; then any button retries and Esc / B goes to Title

---

## Implementation Notes

- Reads the run's goals from `RunSummary` (run-goals-v1 story 004) and unlocks from story 006.

---

## Out of Scope

- Read-only reason lines (post-M1)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: Retained screenshots in `production/qa/evidence/` (results with goals + NEW + unlock; no goals; 960×600 one column)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003; run-goals-v1 stories 004 and 006
- Unlocks: Story 006
