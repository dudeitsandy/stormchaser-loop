# Story 001: Measure counted airtime and set the big-air threshold

> **Epic**: Run Goals v1
> **Status**: Done (2026-10-04) — `BigAirSeconds` = 1.0 s; big_air earned by a toss for now (Andy)
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 1.5 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R10 (Acceptance Criteria: achievability, `big_air` measured first)
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

The review found `big_air` may be unreachable: the driving model only counts air above 1.8 m, a flat jump peaks at 1.5 m, and a toss is estimated at about 0.4 s above 1.8 m, under the 0.5 s `MinStyleSeconds`. Measure before building the goal, because the result decides story 003's threshold or swaps the goal.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] Counted airtime (the Airtime style amount) is measured on the compact map for: a standing jump, a boosted jump off the steepest terrain feature, and an EF4 and an EF5 toss; each with seed, method and value
- [ ] If any route reaches `MinStyleSeconds`, `BigAirSeconds` is set from the measurements (achievable but not trivial) and recorded in `event-system.md` Tuning Knobs with the evidence link
- [ ] If no route reaches it, Andy picks a replacement Style goal and the career table in `event-system.md` is updated before story 002 starts

---

## Implementation Notes

- Use a PlayMode probe that subscribes to `GameEvents.StyleEvent` and logs `StyleKind.Airtime` amounts and the max `GroundClearance`; a toss can be forced with the existing StormScale PlayMode fixtures (EF4/EF5 core).
- Also log `_airTimer` (total air) for comparison: it tells whether redefining the goal on total air would be a better fit, which is Andy's call.
- Side finding for Sprint 9: if no normal play reaches the Airtime style moment, the boost refill from air (RefillAir) never fires either; note it in the evidence doc for the driving pass.

---

## Out of Scope

- Story 003: implements the `big_air` check against the chosen threshold

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `production/qa/evidence/rg-airtime-evidence.md` (measurements, seeds, decision) — playtest/measurement doc

**Status**: [x] `production/qa/evidence/rg-airtime-evidence.md` + probe `Tests/PlayMode/AirtimeProbeTests.cs`

---

## Dependencies

- Depends on: None
- Unlocks: Story 002 (career list final), Story 003 (threshold)
