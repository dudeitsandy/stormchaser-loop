# Story 002: Compact cell schedule and cap resolution (F2, Rule 12)

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 4 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-001`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — pure C# plan model
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Turns a drawn regime into a compact-mode Weather Plan: T = 180 s, the anchor peak window, satellites per regime, the Heat 5 co-anchor, and plan-time resolution against CapTotal = 2. Spawn points use the compact distances inside the ±85 m square.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-6: 10,000 non-Chaos plans: exactly one anchor; no satellite EF4+ or above the anchor; Quiet has no EF5; Heat 5 Sequence/Outbreak add exactly one EF4 co-anchor with an anchor ≥ EF4; Heat 5 Sequence keeps S − 1 satellites with the co-anchor at `t_a − Δ`
- [ ] AC-6b: Sequence `EF_k = max(0, EF_top − (S′ − k))`; Outbreak satellite EF uniform over 0..cap (25 % ±1.5 pp each with an EF4 anchor); counts are uniform integer draws
- [ ] AC-21: compact: a regime is drawn; T = 180 s; anchor Mature start ∈ [72, 117] s; anchors 60–85 m and satellites 40–85 m from P0; Form = max(4 s, 0.25 × table), Mature/Rope × 0.5; Sequence S 1..3, Outbreak S 1..2, Chaos N 3..5; live cells never exceed 2
- [ ] Cap resolution (AC-9 rules at cap 2): over-cap cells wait in 10 s steps and the lowest-EF waiter drops after 30 s; anchor and co-anchor never wait or drop — the lowest-EF live satellite ropes out early instead, from its current I

---

## Implementation Notes

- F2 compact: T fixed at 180 s; `t_peak = U(0.40, 0.65) · T`; `t_a = t_peak − Form(EF_a)`; Δ = U(17, 28) s; drop any Sequence slot with t < 5 s.
- Spawn points: bearing + distance from P0 (truck start, the arena origin). In compact the only invalid spot is outside ±85 m; reroll up to 8 times, then the nearest valid point on the same bearing (Edge Cases). Compact Outbreak satellites satisfy both 40–85 m from P0 and within 120 m of the anchor.
- Cap resolution is a plan-time simulation using each cell's Form + Mature + Rope lifetime; it never reads the player.

**Performance**: no runtime impact — plan-time only; cap resolution simulates a few cells once per run

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: lifecycle intensity math
- Story 006: runtime spawning

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Logic: automated test must exist and pass — `unity/StormChaserLoop3D/Assets/Tests/Director/ScheduleTests.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE
- Unlocks: Story 006
