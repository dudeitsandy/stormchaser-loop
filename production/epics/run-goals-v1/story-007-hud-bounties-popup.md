# Story 007: HUD bounty list and career-goal pop-up

> **Epic**: Run Goals v1
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R08 (Rule 8; `design/ux/run-screens.md` Layout, HUD)
*(No `tr-registry.yaml` yet; IDs are local to `production/epics/run-goals-v1/EPIC.md`.)*

**ADR Governing Implementation**: N/A — pure model / UI over existing `GameEvents`; no architectural pattern beyond the GDD's event contract
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: Unity 6.x is post-cutoff (`docs/engine-reference/unity/VERSION.md`); verify any engine API against
that directory before use. Pure C# where possible (EditMode-testable).

**Control Manifest Rules (this layer)**: N/A — manifest not yet created. Project rules apply: no
`FindObjectOfType` in Update, new Input System only, gameplay values data-driven, `DirectorRng` never
`UnityEngine.Random` for seeded draws.

The in-run goal UI from the run-screens spec: a KTVR WANTS list under BOOST in the left column and a 2 s career pop-up in the upper centre, above Codex's style pops.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] The HUD shows the run's bounties under BOOST: open rows dim, a done row shows DONE/✓ and its bonus, a failed timed row is struck through with MISS; hidden on the legacy spawner
- [ ] A career goal completion shows "GOAL! <NAME> +<bonus>" (NEW on first ever) for 2 s at upper centre without overlapping style pops or the forecast panel
- [ ] Every state reads without colour (text markers); glyphs fall back to text if missing in the WebGL font

---

## Implementation Notes

- `HudController` subscribes to `GoalCompleted` / `BountyFailed`; reads the drawn bounty list once at run start.
- Verify ✓ ✕ ▪ in the HUD font on WebGL first (★ is known missing).

---

## Out of Scope

- Run Screens epic: results goals column, title career page

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: UI
**Required evidence**: Retained screenshots in `production/qa/evidence/` (HUD with 3 bounties: one done, one failed; career pop-up)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004
- Unlocks: None
