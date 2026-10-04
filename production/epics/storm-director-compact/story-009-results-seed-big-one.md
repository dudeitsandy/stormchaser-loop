# Story 009: Results: seed, build version, regime, "The big one got away"

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: UI
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-002, TR-storm-director-008`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — UI Toolkit results screen
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Makes runs shareable and the anchor's miss legible: the results screen shows the run seed and build version, the regime name, and "The big one got away (EFn)" when the anchor never reached Mature.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-4: after a run, Results shows the seed and build version; replaying a seed stored with another version shows "Different version: storms may differ."; screenshot retained
- [ ] AC-25: a run that ends before the anchor is Mature shows "The big one got away" with its EF

---

## Implementation Notes

- `RunScreens.ShowResults` gains a seed / version / regime line; `RunSummary` carries them from the director. A seed replay entry point (e.g. a URL or command-line `seed=`) is enough for AC-4 in compact.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Save & Profile storage of seeds (separate system)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: UI: retained screenshot of each screen touched in `production/qa/evidence/` (+ unit tests for the model) — PlayMode check + screenshot `production/qa/evidence/results-seed.png`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 DONE
- Unlocks: None
