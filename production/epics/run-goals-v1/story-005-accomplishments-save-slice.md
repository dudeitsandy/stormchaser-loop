# Story 005: Accomplishments record and the Save & Profile M1 stem

> **Epic**: Run Goals v1
> **Status**: Ready — the WebGL IndexedDB sync check (AC 1) gates the rest
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 4 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/event-system.md` (part "# Run Goals (v1)")
**Requirement**: RG-R05, RG-R09 (Rules 5, 9; Edge Cases: write failure, read-only, old save)
*(No `tr-registry.yaml` yet; IDs are local to `production/epics/run-goals-v1/EPIC.md`.)*

**ADR Governing Implementation**: N/A — M1 stem by decision (Andy, 2026-10-04); the full Save & Profile ADR comes after M1
**ADR Decision Summary**: **M1 stem** (Andy, 2026-10-04): one profile file (accomplishments record, unlocks, `LastLoadout.livery`, best score, imported once from the PlayerPrefs `BestScoreStore`) and one device file (Settings), each written tmp + rename, `SchemaVersion` 0 (**disposable**: the full build may reset them; patch notes say so). Stable string IDs for goals and unlocks still apply. Gated by a ~1 h WebGL IndexedDB sync check; if it fails, M1 persistence is session-only. Nothing else from `save-profile.md` is in M1.
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: HIGH (no ADR; WebGL storage)
**Engine Notes**: Unity 6.x is post-cutoff (`docs/engine-reference/unity/VERSION.md`); verify any engine API against
that directory before use. Pure C# where possible (EditMode-testable).

**Control Manifest Rules (this layer)**: N/A — manifest not yet created. Project rules apply: no
`FindObjectOfType` in Update, new Input System only, gameplay values data-driven, `DirectorRng` never
`UnityEngine.Random` for seeded draws.

The first code that writes a save file, built as the **M1 stem**, not `save-profile.md`: a disposable proof of concept that gives run goals and Settings real persistence in the vertical slice. The device file for Settings is part of this stem (used by the Run Screens settings story).

---

## Acceptance Criteria

*From GDD `design/gdd/event-system.md` Run Goals Rule 9 and the M1 stem decision (Andy, 2026-10-04):*

- [ ] **WebGL sync check first (~1 h):** in a WebGL build, a file written under `Application.persistentDataPath`
      survives a page reload (with an explicit IndexedDB FS sync after each write if needed). Result recorded in
      `production/qa/evidence/rg-webgl-save-check.md`. If it fails, persistence is session-only for M1 and the
      remaining ACs apply to the in-memory store
- [ ] Profile file round trip: accomplishments record (per mode: id → first seed, date, version, count), unlocks,
      `LastLoadout.livery` and best score save → load identical; `SchemaVersion` 0; written tmp + rename; the
      PlayerPrefs best score is imported once
- [ ] Run complete writes score, record and unlocks in one write; quitting mid-run writes nothing; a failed write
      leaves the run playable and keeps progress in memory for the next run complete
- [ ] Device file for Settings saves and loads the Settings record (API used by the Run Screens settings story)

---

## Implementation Notes

- New `Scripts/Save/ProfileStore.cs` (+ a storage interface so tests inject a fake: no file I/O in unit tests).
- Fields this slice owns: `Accomplishments` (per mode: id → first seed, date, version, count), `Unlocks`, `LastLoadout.livery`, best score; plus the device file for Settings. Do not build anything else from `save-profile.md`.
- Stem only: tmp + rename, no `.bak`, checksums, slots, migrations or ReadOnly handling. Do not mistake this for `save-profile.md`.

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
**Required evidence**: `production/qa/evidence/rg-webgl-save-check.md` + integration test `unity/StormChaserLoop3D/Assets/Tests/Save/AccomplishmentsTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004
- Unlocks: Story 006, Run Screens settings and results stories
