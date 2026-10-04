# Story 003: Storm scale table and lifecycle intensity (F3)

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 2–3 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-003`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — per-EF data and pure formulas
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Replaces the flat sqrt EF wind scale with the GDD's strongly non-linear per-EF table (damage radius, wind radius, peak wind, lifetimes, track speed) and the intensity rules for Forming, Mature and Roping Out, including early rope-out and failed touchdown.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-10: the table is strictly monotonic in D, R, P; P/21.5 > 1 for EF3+; base track speed ≤ 8.6 m/s (≤ 10.75 with k_H); W(15 m) = 8.1 ±0.05 m/s for EF3 and 38.3 ±0.1 for EF5; W = 0 for I < 0.01 with no NaN or infinity
- [ ] AC-10b: EF5 Forming has zero lift and damage radius at every d; Rope-out I = 0.5 gives axis lift 0.5 × 2.295 ±0.01, damage radius 6.0 ±0.05 m and zero lift at d ≥ 17.5 m; I = 0.25 gives axis lift < 0.7; a cell evicted at Forming I0 = 0.4 enters Rope-out at 0.4, reaches 0 after 0.4 × Rope, and keeps zero lift and damage throughout
- [ ] Data migration: `TornadoData_EF0–5` assets carry the F3 values (D, R, P, Form/Mature/Rope, v_track, ω); `TornadoController` no longer has `_windScaleBase`, `_windScalePerSqrtEF` or `_baseDamageRadius`, and `_playerPull` is 0 (asserted in a unit test over the six assets)

---

## Implementation Notes

- `StormScale` pure functions: `W(d) = P · I · (1 − d/(R · I))²` for d < R · I; inflow 0.537 W, swirl 0.843 W; guard I < 0.01.
- Phase gating: Forming I = age/Form, no lift or damage; Rope-out lift = I · F12(d at radius R · I), damage radius D · I; early rope-out `I = I0 · (1 − t/(I0 · Rope))`; failed touchdown keeps lift and damage at 0.
- Add per-EF fields to `TornadoData` (D, R, P, Form/Mature/Rope, v_track, ω) and update the six `TornadoData_EF*.asset` files from the F3 table. Remove `_windScaleBase`, `_windScalePerSqrtEF`, radius-per-ConeScale and `_baseDamageRadius`; `_playerPull` → 0; keep inflow/swirl as direction ratios. ConeScale becomes visual-only (≈ D/4.3).

**Performance**: no impact expected — W(d) is a few multiplies per disaster per query, at the same call sites as today

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: proving the forces in play
- Story 005: track motion

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Logic: automated test must exist and pass — `unity/StormChaserLoop3D/Assets/Tests/Director/StormScaleTests.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: Story 004, 006
