# Story 004: New scale in play: wind drag, lift and toss

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 2–3 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-003`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: ADR-0005: Raycast vehicle architecture
**ADR Decision Summary**: Pure VehicleModel computes forces from wind/lift inputs; PlayerVehicle applies them to one Rigidbody.
**ADR Version**: 2026-10-01

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Wires the F3 scale into `TornadoController` so the real truck feels it: EF3 drags, EF4 and EF5 toss inside their cores, and a Forming cell never hurts. This is the story that turns the long-failing EF3 wind PlayMode test green.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-11: Mature EF3, truck parked 15 m away, no input: > 0.5 m horizontal displacement in 1 s and `Tossed` not raised (the existing `RunLoopSmokeTests.TornadoWind_ParkedNearEF3_IsDraggedNotTossed` passes)
- [ ] AC-12: Mature EF5, truck parked 5 m away: `Tossed` within 1 s
- [ ] AC-13: EF4 tossed within 1 s at 5.0 m (lift 0.75) and not tossed after 2 s at 8.0 m (lift 0.55); EF5 not tossed at 17 m (lift 0.61)
- [ ] AC-19: truck inside an EF5 core while it is Forming: no damage taken and not tossed

---

## Implementation Notes

- `TornadoController.GetWindAt`, `GetLiftFractionAt` and `DamageRadius` read the F3 table and intensity from story 003; vehicle F11/F12 unchanged (ADR-0005: forces computed in VehicleModel, applied by PlayerVehicle).
- Test seam: `HoldMature` exists; add a way to hold Forming at a chosen I for AC-19.
- Re-run the full PlayMode suite: wind changes touch the smoke, scenery and camera tests.

**Performance**: no impact expected — swaps the wind/lift formula at existing call sites; full PlayMode suite re-run covers regressions

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: the director spawning cells (these tests spawn tornadoes directly)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration: automated PlayMode test (or a documented playtest) — `unity/StormChaserLoop3D/Assets/Tests/PlayMode/StormScalePlayTests.cs` + the existing EF3 smoke test

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 DONE
- Unlocks: Story 006
