# Story 006: KTVR livery reward

> **Epic**: Run Goals v1
> **Status**: Done (2026-10-05) — a newly earned livery is equipped automatically (Claude's call; title toggle in run-screens 005)
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 2 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R06 (Rule 6; Formulas: Reward)
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

Proves the loop: 5 of 10 career goals grant `livery.ktvr`. The paint material is Codex's lane; this story owns the rule, the unlock and the saved choice.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] A profile with 4 recorded career goals that completes a 5th gets `livery.ktvr` at that run's checkpoint; an already-owned livery is never added twice
- [ ] The chosen livery is saved in `LastLoadout.livery` and is still applied after a full reload
- [ ] Codex request posted for the KTVR paint material on the Blender pickup, read via the owned/chosen livery

---

## Implementation Notes

- Reward check runs inside the run-complete transaction (story 005), after the record update.
- `RewardThreshold` is data (default 5).
- Title toggle UI is the Run Screens epic; here only the API to read/set the choice.

---

## Out of Scope

- Run Screens epic: title paint toggle, results unlock banner

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration test `unity/StormChaserLoop3D/Assets/Tests/PlayMode/LiveryRewardTests.cs` — must exist and pass

**Status**: [x] `Tests/Goals/RunRewardsTests.cs` (5) + `Tests/PlayMode/LiveryRewardTests.cs` (2) passing

---

## Dependencies

- Depends on: Story 005
- Unlocks: Run Screens title and results stories
