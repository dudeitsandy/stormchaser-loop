# Story 002: Goal catalogue and seeded bounty draw

> **Epic**: Run Goals v1
> **Status**: Done (2026-10-04)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R01, RG-R02, RG-R03 (Rules 1–3, Formulas: Bounty draw)
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

The data side of goals: every career goal and bounty as a catalogue entry with a stable ID, and the per-run draw of 3 bounties from the eligible pool. Pure C#, no MonoBehaviour.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] The catalogue holds the 10 compact/heartland career goals and the 7 v1 bounties with IDs `career.compact.heartland.<goal>` and `bounty.compact.<name>`, types, HUD text, results short names (≤ 16 chars) and bonuses as in the GDD
- [ ] Same seed and build → the same 3 bounties in the same order; for seeds 1–1,000 every director plan draws exactly 3 and never a bounty the plan cannot satisfy (no `point_blank_ef5` without a non-dropped EF5 cell)
- [ ] The draw uses `DirectorRng` stream 3 only: a plan serialized with and without a bounty draw is identical
- [ ] No bounty's HUD text contains an EF number or any digit

---

## Implementation Notes

- New `Scripts/Goals/GoalCatalogue.cs` (data) and `BountyDraw.cs` (pure). Eligibility reads `WeatherPlan.Cells` (non-dropped), `Role`, `Ef`.
- Draw: Fisher–Yates over the eligible list in catalogue order with `new DirectorRng(seed, 3)`, take the first `BountiesPerRun`.
- Thresholds and bonuses are data (tuning knobs), not literals in the tracker.

---

## Out of Scope

- Story 003: evaluating goals
- Story 004: wiring into a run

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Unit test `unity/StormChaserLoop3D/Assets/Tests/Goals/GoalCatalogueTests.cs` — must exist and pass

**Status**: [x] `Tests/Goals/GoalCatalogueTests.cs` (8 cases) passing

---

## Dependencies

- Depends on: Story 001 (career list final)
- Unlocks: Story 003
