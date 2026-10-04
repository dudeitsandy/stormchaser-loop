# Story 006: Director runtime driver and StormCell lifecycle events

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 5 h (L)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-005, TR-storm-director-002, TR-storm-director-009`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: ADR-0004: Destructible tiled world
**ADR Decision Summary**: Seeded world and bounds; compact mode runs on today's ±85 m arena (streaming ring is Epic-only).
**ADR Version**: 2026-10-01

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

The MonoBehaviour that runs the plan in compact mode: it replaces `DisasterSpawner`'s timer, spawns cells at their scheduled times, drives each `TornadoController` from the track model and intensity, evicts per the plan, and raises the agreed `StormCell*` events (already declared in `GameEvents`). It holds no references to downstream systems.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-20: a full life raises Forming → Peak → RopeOut → Ended once each; evicted while Forming: Forming → RopeOut → Ended; a Mature cell forced out of the world raises Ended only; ending the run with 2 live cells raises exactly one Ended each; a cap-dropped cell raises nothing; payloads carry CellId, EF and Role
- [ ] AC-17: seed 777 run idle and while driving a scripted loop: every cell's position sampled every 0.5 s matches within 0.01 m
- [ ] AC-22: pausing for 10 s leaves every cell's age and position unchanged (game time)
- [ ] AC-28: per-frame director cost with live cells ≤ 0.10 ms desktop, and 0 B GC allocation per frame in steady state

---

## Implementation Notes

- `StormDirector` driver states Idle → Planning → Running → Complete (GDD States and Transitions); the Run Manager starts and ends it; the director never ends a run.
- Read-only query surface for HUD and presentation (cells with EF, role, phase, I, position); no per-frame events.
- Arena bounds come from today's compact arena (ADR-0004: compact mode, no streaming ring). Spawner roster weights are retired.

**Performance**: AC-28: ≤ 0.10 ms per frame desktop with live cells, 0 B GC steady state

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 007: forecast
- Story 008: siren caption and `e` query
- Story 009: results screen

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration: automated PlayMode test (or a documented playtest) — `unity/StormChaserLoop3D/Assets/Tests/PlayMode/StormDirectorPlayTests.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002, 003, 004, 005 DONE
- Unlocks: Story 007, 008, 009
