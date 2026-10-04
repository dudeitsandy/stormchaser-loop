# Story 005: Track motion, jogs and fixed substeps (F5)

> **Epic**: Storm Director (compact mode)
> **Status**: Done (2026-10-04)
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-004, TR-storm-director-002`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — pure C# track model
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Deterministic storm tracks that never read the player: hash-noise wander, the Heat speed multiplier, plan-time Poisson jogs with a 1 s telegraph and refractory, integrated in fixed 0.05 s substeps so a seed replays identically at any frame rate. Compact keeps the ±90 m edge steer-back inside the substeps.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-17 (Unit companion): the track model API takes no player or transform parameter
- [ ] AC-18: jogs only start at u ≥ 0.6; each tilt telegraph starts 1.0 s ±0.05 s before its turn; no two jog starts < 4.5 s apart; jog speed ≤ 12 m/s at Heat 0 and ≤ 15.05 m/s at Heat 1+
- [ ] AC-23: seed 777 at a forced 5 FPS and at 60 FPS: every cell's position at ages 10, 30 and 60 s matches within 0.01 m (fixed 0.05 s substeps with an accumulator)

---

## Implementation Notes

- F5: `dθ/dt = n(t) · ω_EF`, `n = 2 · noise(seed_i, 0.25 t) − 1` (director hash noise, not Mathf.PerlinNoise); θ0 within ±60° of the bearing to world centre; `v = v_track · k_H · lerp(0.4, 1, I)`.
- Jogs pre-rolled at plan time: rate `λ0 · clamp01((u − 0.6)/0.4)` over the plan-time lifetime; turn ±U(40°, 110°) eased over 1.5 s, × 1.5 burst for 3 s, 1 s tilt telegraph; refractory 4.5 s.
- Compact steer-back at ±90 m is evaluated inside each substep (frame-rate independent).

**Performance**: ≤ 0.02 ms per cell per frame; substeps are 20 Hz pure math, 0 B GC

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: driving TornadoController from the track

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Logic: automated test must exist and pass — `unity/StormChaserLoop3D/Assets/Tests/Director/TrackModelTests.cs` + a PlayMode FPS case

**Status**: [x] Created and passing (`Tests/Director/TrackModelTests.cs`, 11 cases; AC-23 is covered in EditMode by stepping at 5 and 60 FPS frame sizes)

---

## Dependencies

- Depends on: Story 001 DONE
- Unlocks: Story 006
