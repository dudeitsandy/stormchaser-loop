# Story 003: Goal evaluation, bonuses and goal events

> **Epic**: Run Goals v1
> **Status**: Done (2026-10-04)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R04, RG-R07 (Rules 4, 7; Formulas: Score tiers, Run bonus, Goal thresholds)
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

The rules engine: given a run's inputs (photos, style moments, tosses, cell phases, run end), decide which goals completed, what they pay, and raise the events. Pure C# driven by plain inputs so it is fully EditMode-testable.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] Threshold boundaries: drift 2.99 s fails / 3.0 s passes `storm_drift` (storm within 60 m at drift end); counted airtime just under / at `BigAirSeconds`; photo at 19.9 m passes `point_blank`, 20.0 m fails; `double_near_miss` 9.9 s passes, 10.1 s fails
- [ ] Score tiers: final score 1,499 meets Rookie only, 1,500 Rookie and Pro; score goals pay no bonus
- [ ] A goal met twice in one run pays once; one photo meeting `front_page`, `ef4_peak`, `point_blank` and a bounty completes and pays all four
- [ ] `GoalCompleted` fires once per goal per run with `Kind`, `Bonus` and `FirstEver`; a timed bounty's window closing raises `BountyFailed` once

---

## Implementation Notes

- New `Scripts/Goals/GoalTracker.cs` (pure): methods like `OnPhoto(PhotoResult-like data)`, `OnStyle(kind, amount, nearestStormDistance)`, `OnTossed()`, `OnCellForming/Phase(...)`, `Tick(dt)`, `OnRunEnd(...)`.
- Add `GoalCompletion` struct and `GameEvents.GoalCompleted` / `BountyFailed` (with `ResetStatics`).
- `FirstEver` needs the accomplishments record; take it as an injected `Func<string,bool> hasCompleted` so this story does not depend on story 005.

---

## Out of Scope

- Story 004: subscribing to live `GameEvents`, score integration
- Story 005: persistence

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Unit test `unity/StormChaserLoop3D/Assets/Tests/Goals/GoalTrackerTests.cs` — must exist and pass

**Status**: [x] `Tests/Goals/GoalTrackerTests.cs` (22 cases) passing

---

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 004, Story 007
