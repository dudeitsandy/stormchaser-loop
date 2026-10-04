# Story 005: Accomplishments record and first save slice

> **Epic**: Run Goals v1
> **Status**: Blocked — BLOCKED: no ADR for WebGL save storage. Run `/architecture-decision` (browser save storage: IndexedDB sync, `Application.persistentDataPath`, flush timing) and set this story Ready once it is Accepted
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 4 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R05, RG-R09 (Rules 5, 9; Edge Cases: write failure, read-only, old save)
*(No `tr-registry.yaml` yet; IDs are local to `production/epics/run-goals-v1/EPIC.md`.)*

**ADR Governing Implementation**: **Missing**: WebGL save storage ADR (proposed ADR-0006, not yet written). `save-profile.md` lists browser storage sync as an open ADR question
**ADR Decision Summary**: Not yet decided
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: HIGH (no ADR; WebGL storage)
**Engine Notes**: Unity 6.x is post-cutoff (`docs/engine-reference/unity/VERSION.md`); verify any engine API against
that directory before use. Pure C# where possible (EditMode-testable).

**Control Manifest Rules (this layer)**: N/A — manifest not yet created. Project rules apply: no
`FindObjectOfType` in Update, new Input System only, gameplay values data-driven, `DirectorRng` never
`UnityEngine.Random` for seeded draws.

The first code that writes a save file. It implements only the slice Run Goals needs, but by `save-profile.md`'s rules so it never has to be redone: versioned file, unknown IDs kept, the run-complete write as one transaction.

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals, scoped to this story:*

- [ ] Record round trip: save → load → identical; a goal or unlock ID this build doesn't know survives load and re-save; a pre-goals save loads with an empty record
- [ ] Run complete writes score, accomplishments and unlocks together or not at all; quitting mid-run writes nothing
- [ ] A failed write (simulated) keeps the progress in memory, retries, and raises the "Couldn't save — progress kept, will retry" state; a ReadOnly profile records nothing and exposes its reason for Results

---

## Implementation Notes

- New `Scripts/Save/ProfileStore.cs` (+ a storage interface so tests inject a fake: no file I/O in unit tests).
- Fields this slice owns: `Accomplishments` (per mode: id → first seed, date, version, count), `Unlocks`, `LastLoadout.livery`. Leave room for the rest of `save-profile.md` (slots, dollars, album) without building it.
- Follow `save-profile.md` Core Rules for temp-write / swap / `.bak` and its strings verbatim.

---

## Out of Scope

- Profiles menu, slots, Storm Dollars balance, album (post-M1)
- Story 006: the livery reward rule

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*


---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration test `unity/StormChaserLoop3D/Assets/Tests/Save/AccomplishmentsTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004; WebGL save-storage ADR Accepted
- Unlocks: Story 006, Run Screens settings and results stories
