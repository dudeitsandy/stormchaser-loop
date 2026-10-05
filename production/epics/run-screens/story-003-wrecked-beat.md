# Story 003: Wrecked slow-mo beat

> **Epic**: Run Screens
> **Status**: Done (2026-10-05, 0.8.0) — WebGL screenshots `production/qa/evidence/run-screens-*.png` (WRECKED slam not captured headless)
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: 1.5 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/ux/run-screens.md` (UX spec)
**Requirement**: RS-R04 (Transitions & Animations)
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

Failure as a moment instead of a cut: the wreck plays out briefly before Results.

---

## Acceptance Criteria

*From `design/ux/run-screens.md`, scoped to this story:*

- [ ] On HP reaching 0, time runs at 0.3× for 1.5 ± 0.1 s of real time, the HUD fades out over 0.3 s, and "WRECKED" slams in red (scale 1.4 → 1.0 in 0.2 s), then Results with the WRECKED header
- [ ] Pause input is ignored during the beat; time scale is restored to 1 for Results and Retry

---

## Implementation Notes

- Use `RunManager`'s existing Ending state; drive the beat with unscaled time.

---

## Out of Scope

- Results layout (story 004)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: Retained screenshot of the WRECKED slam in `production/qa/evidence/` + sign-off in `production/qa/evidence/run-screens-wrecked-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001
- Unlocks: Story 004
