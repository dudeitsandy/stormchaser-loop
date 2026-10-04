# Story 008: Achievability check before release

> **Epic**: Run Goals v1
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: 1 h + play (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R10 (Acceptance Criteria: achievability)
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

Before Run Goals ships to outside players, each career goal must have been done at least once in a real compact run, so no goal is impossible and the reward is reachable.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] Every career goal has been completed at least once in a real compact run, each with its seed and build recorded
- [ ] Any goal that could not be completed is retuned or swapped (with Andy) and re-verified

---

## Implementation Notes

- Use seeded replays (`?seed=N`) to make storm goals repeatable; record in the smoke doc.

---

## Out of Scope

- Balance tuning beyond achievability (Sprint 9 / outside playtest)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: Smoke check `production/qa/smoke-run-goals.md` (goal, seed, build, date)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 004–007
- Unlocks: Run Goals in the 0.8.0 release
