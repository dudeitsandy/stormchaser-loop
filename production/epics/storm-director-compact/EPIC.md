# Epic: Storm Director (compact mode)

> **Layer**: Core
> **GDD**: design/gdd/storm-director.md (Approved 2026-10-03)
> **Architecture Module**: `Scripts/Director` (new) — pure C# plan / scale / forecast / track model plus a thin
> MonoBehaviour driver; replaces `DisasterSpawner`'s timer and rewires `TornadoController` / `TornadoData`
> (`Scripts/Tornado`). HUD forecast in `Scripts/UI`; presentation stays in Codex's lane via `StormCell*` events.
> No `architecture.md` exists yet; module mapped from the current code layout.
> **Status**: In Progress — scheduled in Sprint 8 (`production/sprints/sprint-08-storm-director.md`)
> **Stories**: 9 stories (see table)

## Overview

This epic replaces random tornado spawning with the Storm Director in **compact mode**, the mini-Epic that
runs on today's ±85 m arena until the 2 km streamed world exists (GDD Rule 12). At run start a seeded
Weather Plan draws a regime (Quiet, Lone Giant, Sequence, Outbreak, Chaos), shaped by Cataclysm Heat,
around one anchor storm and smaller satellites, and schedules every cell in a fixed 180 s run. It owns the
new strongly non-linear EF storm scale (F3), so EF3+ storms are genuinely dangerous up close and an EF5 is
rare and imposing, which also resolves the long-standing failing EF3 wind PlayMode test. Cells follow a
lifecycle (Forming with zero damage, Mature peak, Roping Out) on deterministic, never-homing tracks with
telegraphed late-life jogs, and announce themselves through the agreed `StormCell*` events, a ±1-EF
forecast panel, a siren caption and an environmental-intensity value that presentation reads. Everything
is reproducible from the run seed, shown on the results screen. The rationale is Andy's 2026-10-01
direction: tension, danger and risk-taking with storms you can see coming. The main risk is tuning (regime
weights, jog rate, forecast error are placeholders pending playtest), not architecture.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Art direction, stylized world + retro lens | Telegraph visuals are toon cards; camcorder treatment only in viewfinder/photos | LOW |
| ADR-0004: Destructible tiled world | World bounds and streaming ring; compact uses today's arena, ring is Epic-only (soft here) | LOW (compact) |

No dedicated director ADR. Determinism (own seeded integer PRNG and hash noise, fixed 0.05 s substeps) and
the event contract are specified in the GDD (Rules 10, 11; Edge Cases, Timing) and agreed with Codex in
AGENTS.md. At `workflow: standard` only critical Foundation-layer ADRs are expected, so this is informational.

## GDD Requirements

No `tr-registry.yaml` exists yet; requirement IDs are local to this epic and cite the GDD rule / formula / AC.

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-storm-director-001 | Seeded Weather Plan: regime draw with Heat (F1), anchor/satellite/co-anchor composition (Rule 3), compact schedule T = 180 s, counts, cap 2, cap resolution at plan time (F2, Rule 12); AC-1–3, 5–7, 21 | GDD only (Rules 1–3, 9, 12) |
| TR-storm-director-002 | Determinism: own RNG stream and hash noise, never UnityEngine.Random / System.Random / Mathf.PerlinNoise; fixed 0.05 s substeps; seed + build version on Results (Rule 10); AC-1, 4, 17, 22, 23 | GDD only |
| TR-storm-director-003 | Storm scale F3: per-EF D/R/P table, W(d) with I, inflow/swirl split, I < 0.01 guard, phase gating (Forming: no lift/damage; Rope-out scales with I; early rope-out from I0; failed touchdown); remove sqrt wind scale and player pull; AC-10, 10b, 11–13, 19 | GDD only |
| TR-storm-director-004 | Track motion F5: hash-noise wander, k_H, plan-time Poisson jogs with 1 s tilt telegraph and 4.5 s refractory, no player input, compact edge steer-back inside substeps; AC-17, 18 | GDD only |
| TR-storm-director-005 | Lifecycle events (Rule 11): `StormCellForming/Peak/RopeOut/Ended` with `StormCellInfo`, exact lifecycle rules incl. eviction, world exit and run end; no downstream references; AC-20 | GDD + AGENTS.md contract |
| TR-storm-director-006 | Forecast F4: σ(d), bias clamps, hysteresis, ETA rounding; HUD panel (nearest 3 + anchor/co-anchor, "~EF", "+N more"); AC-14–16 | GDD only |
| TR-storm-director-007 | Telegraph hooks (Rule 7): siren on true EF3+ with EF-free caption; environmental `e` from summed per-cell wind magnitude; read-only query surface for presentation; AC-26 Unit companions | ADR-0003 (visual language) |
| TR-storm-director-008 | Results: seed, build version, regime name, "The big one got away (EFn)"; AC-4, 25 | GDD only |
| TR-storm-director-009 | Per-frame cost ≤ 0.10 ms desktop / 0.30 ms WebGL with live cells, 0 B GC steady state; AC-28 | GDD only |

**Deferred to a later Epic-mode epic** (needs the 2 km world, ADR-0004): streaming-ring tiers (Rule 8),
d_reach and Epic spawn distances, 800 m far-field (fog-exempt Render Graph pass), AC-8, 9, 24, 26 (WebGL
far-field), 27, 29.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [Seeded plan RNG, regime draw and anchor EF (F1)](story-001-plan-rng-regime-draw.md) | Logic | Ready | N/A |
| 002 | [Compact cell schedule and cap resolution (F2, Rule 12)](story-002-compact-schedule-caps.md) | Logic | Ready | N/A |
| 003 | [Storm scale table and lifecycle intensity (F3)](story-003-storm-scale-intensity.md) | Logic | Done | N/A |
| 004 | [New scale in play: wind drag, lift and toss](story-004-scale-in-play-wind-lift-toss.md) | Integration | Done | ADR-0005 |
| 005 | [Track motion, jogs and fixed substeps (F5)](story-005-track-motion-jogs-substeps.md) | Logic | Ready | N/A |
| 006 | [Director runtime driver and StormCell lifecycle events](story-006-runtime-driver-lifecycle-events.md) | Integration | Ready | ADR-0004 |
| 007 | [Forecast model and HUD forecast panel (F4)](story-007-forecast-model-hud.md) | UI | Ready | N/A |
| 008 | [Telegraph hooks: siren caption and environmental-intensity query](story-008-telegraph-hooks-siren-e.md) | Integration | Ready | ADR-0003 |
| 009 | [Results: seed, build version, regime, "The big one got away"](story-009-results-seed-big-one.md) | UI | Ready | N/A |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All in-scope acceptance criteria from `design/gdd/storm-director.md` are verified (compact list above)
- All Logic and Integration stories have passing test files in `unity/StormChaserLoop3D/Assets/Tests/`
  (EditMode `StormDirectorTests`, PlayMode `StormDirectorPlayTests`), and the EF3 wind PlayMode test passes
- All Visual/Feel and UI stories have retained screenshots in `production/qa/evidence/` — each screen
  touched for UI (forecast panel, results), plus a lead sign-off for Visual/Feel

## Next Step

Stories exist. Work them in Sprint 8 order: 003 → 004 → 001 → 002 → 005 → 006 → 008, then 007, 009.
