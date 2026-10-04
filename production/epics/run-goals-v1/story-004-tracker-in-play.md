# Story 004: Goal tracker in a live run

> **Epic**: Run Goals v1
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R04, RG-R07, RG-R09 (Rules 4, 7; Edge Cases: wreck, quit)
*(No `tr-registry.yaml` yet; IDs are local to `production/epics/run-goals-v1/EPIC.md`.)*

**ADR Governing Implementation**: ADR-0005: Raycast vehicle architecture
**ADR Decision Summary**: Custom raycast vehicle on a Rigidbody; the driving model raises style moments (drift at slide end with its duration, airtime on landing with seconds counted above `MinAirtimeHeight`, near miss) and toss through `GameEvents`.
**ADR Version**: 2026-10-01

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: HIGH (ADR-0005 Knowledge Risk)
**Engine Notes**: Unity 6.x is post-cutoff (`docs/engine-reference/unity/VERSION.md`); verify any engine API against
that directory before use. Pure C# where possible (EditMode-testable).

**Control Manifest Rules (this layer)**: N/A — manifest not yet created. Project rules apply: no
`FindObjectOfType` in Update, new Input System only, gameplay values data-driven, `DirectorRng` never
`UnityEngine.Random` for seeded draws.

Wire the pure tracker into the run: a thin component subscribes to the existing events, feeds the tracker, adds bonuses to the score and carries the run's goals in `RunSummary` for results.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] A scripted run that drifts 3 s beside a live storm and photographs the anchor PERFECT completes `storm_drift` and `front_page`, raises one `GoalCompleted` each, and `RunSummary` lists both with bonuses
- [ ] Goal bonuses are added to the run score (and so to Storm Dollars 1:1); score-tier goals are judged on the final score including them
- [ ] A wrecked run keeps goals done before the wreck and does not meet `toss_survivor`; a run quit to title forfeits its goals (nothing passed on for recording)

---

## Implementation Notes

- New `Scripts/Goals/GoalRunner.cs` MonoBehaviour, auto-installed like `StormDirector`; reads `DisasterSpawner.Director` for the plan, `LiveCells` for anchor and phase; nearest storm distance from `DisasterEntity.Active`.
- Extend `RunSummary` with the run's goal completions (optional ctor param, like `StormRunInfo`).
- Run end reason (timer / wreck / quit) comes from `RunManager`; quit needs the forfeit path from the run-screens epic (pause story), so test quit via a direct call until then.

---

## Out of Scope

- Story 005: writing the record
- Story 007: HUD display
- Run Screens epic: results goals column

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration test `unity/StormChaserLoop3D/Assets/Tests/PlayMode/GoalTrackerPlayTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003
- Unlocks: Story 005, Story 007, Run Screens results story
