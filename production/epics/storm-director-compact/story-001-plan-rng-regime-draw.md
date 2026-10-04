# Story 001: Seeded plan RNG, regime draw and anchor EF (F1)

> **Epic**: Storm Director (compact mode)
> **Status**: Done (2026-10-04)
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-001, TR-storm-director-002`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — pure C# plan model; determinism is specified by GDD Rule 10
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

First slice of the director: a deterministic, seeded integer PRNG owned by the director and the F1 regime + anchor-EF draw, shaped by Cataclysm Heat. Everything later builds on this plan model, so it must be pure C# with no Unity randomness.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-1: seed 12345, Heat 2 plan generated twice is byte-identical, including with 1,000 `UnityEngine.Random` calls in between (own RNG stream)
- [ ] AC-2: 10,000 seeds, regime shares within ±1.5 pp of .20/.25/.30/.15/.10 at Heat 0 and .007/.165/.254/.345/.230 at Heat 5
- [ ] AC-5: P(any EF5, before cap resolution) = 4.39 % ±0.6 pp at Heat 0 and 13.85 % ±1.1 pp at Heat 5 (anchor tables incl. the Heat 5 EF4 floor .91/.09; Chaos per-cell table)
- [ ] AC-7: 10,000 Chaos plans at Heat 5 have no anchor and never more than one EF5; a forced EF5, EF5 draw makes the second cell EF4

---

## Implementation Notes

- New `Scripts/Director/` (StormChaser asmdef), plain C#: `DirectorRng` (seeded integer PRNG, e.g. xorshift/PCG; never `UnityEngine.Random`, `System.Random` or `Mathf.PerlinNoise`), `RegimeDraw`; values in a `StormDirectorProfile` ScriptableObject (weights, slopes, tables — data-driven).
- F1: `w_r(H) = w0_r · exp(s_r · H)`; EF5 share `p5_0(r) · (1 + 0.4H)` taken from the lowest tier; Heat 5 Sequence/Outbreak floor moves EF2/EF3 mass onto EF4.
- Serialize the plan deterministically (fixed field order, invariant culture) so AC-1 can compare bytes.

**Performance**: no runtime impact — plan generation runs once at run start (AC-27 budget ≤ 2 ms desktop applies when Epic lands)

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: cell schedule, satellites, caps
- Story 005: track noise (same seed, separate sub-stream)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Logic: automated test must exist and pass — `unity/StormChaserLoop3D/Assets/Tests/Director/RegimeDrawTests.cs`

**Status**: [x] Created and passing (`Tests/Director/RegimeDrawTests.cs`, 19 cases)

---

## Dependencies

- Depends on: None
- Unlocks: Story 002, 005
