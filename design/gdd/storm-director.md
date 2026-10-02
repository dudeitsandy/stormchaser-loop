# Storm Director

> **Status**: In Design
> **Author**: Andy Styx + Claude
> **Last Updated**: 2026-10-01
> **Last Verified**: 2026-10-01
> **Implements Pillar**: 2 — Disaster Stacking & "Disaster Alchemy" (game-concept.md); serves Pillar 1 (Kinetic Chaos) via tension, and the anti-pillar "NOT scripted spectacle" (set pieces come from rules)

## Summary

The Storm Director decides which tornadoes a run gets, where they form, when they peak and how violent they
are. It builds a seeded Weather Plan from regimes like Lone Giant, Outbreak and Chaos, around one anchor
storm and smaller satellites, and it owns the EF scale that makes an EF5 imposing and rare. It turns random
spawning into a run with a shape: tension you can see building on the horizon, a window to get there, and a
peak worth the risk.

> **Quick reference** — Layer: `Core` · Priority: `MVP` (compact mode now; Epic after the 2 km world) · Key deps: Disaster Entity Framework / Tornado, Run Manager & Session Modes, Tiled World Streaming, Vehicle Feel (wind and lift)

## Overview

The Storm Director is the rule engine that decides which disasters exist in a run, where they form,
when they peak, and how big and violent they are. It replaces the random timer in `DisasterSpawner`
with a seeded plan, so a run has a shape: a quiet read of the sky, a storm building on the horizon, a
window to reach it, a peak worth risking everything for, and a decline. It also owns **storm scale**,
the mapping from an EF rating to a funnel's size, wind strength, lifespan and lift, so an EF5 reads as
a different kind of event from an EF1 and not 1.6× the same one.

The director has two postures over the same storm model. In **Epic** play it paces a whole run on the
large streamed world: storms are forecast and telegraphed from a distance, mature on their own
schedule, and die whether or not the player arrived, so reaching them in time is part of the
challenge. In **Arcade** play it takes a fixed scenario (storm count, EF range, wind multiplier, film
limit) and runs it exactly, with no pacing of its own. Both are deterministic from a seed, so a
scenario or a run can be shared and replayed.

Other systems never spawn storms themselves: the spawner, objectives, scoring and HUD ask the
director what is happening and where, and the director raises events through `GameEvents`. Players
never interact with it directly. They feel it as dread when a storm builds on the horizon, and as the
choice to drive at it.

## Detailed Design

### Core Rules

1. **Weather Plan.** At run start the director builds a *Weather Plan* from the run seed: one **regime**,
   then a list of **storm cells**, each with a spawn time, spawn point, EF rating, size, lifetime and
   track. The plan is plain data, generated once. Nothing in it is hand-authored (anti-pillar: set
   pieces come from rules).
2. **Regimes** set the shape of a run. Drawn by weight (F1), shifted by Cataclysm Heat:
   - *Quiet Day*: a few weak cells, lots of driving, a breather.
   - *Lone Giant*: one anchor storm that dominates the run, high EF, with small satellites.
   - *Sequence*: cells one after another, usually rising.
   - *Outbreak*: several cells at once in the same area.
   - *Chaos*: random counts and timing with **no guaranteed anchor**. Some Chaos runs are unwinnable or
     luck-driven; this is intended.
3. **Anchor and satellites.** Every regime except Chaos has one **anchor**, the cell with the run's peak,
   which is the only cell that can be EF4–EF5. **Satellites** are capped at EF3 (Andy, 2026-10-01) and
   give early points and risk. This is what keeps EF5 rare.
4. **Cell lifecycle.** Forming (visible and growing; **damage and lift are zero**, wind ramps with
   intensity), Mature (the peak window), Roping Out (decline), Done. Bigger storms last longer. In Epic
   the anchor is always reachable: its Form + Mature time ≥ its distance from the plan origin ÷
   (0.7 × v_top). Arcade scenarios use short forming times.
5. **Tracks.** A cell moves along its own heading and speed with wander. **No homing:** the track ignores
   the player. Late in a cell's life it makes erratic jogs, so a storm is dangerous to *loiter near*, not
   to flee. A cell that leaves the world ends.
6. **Storm scale.** EF rating maps to funnel size, wind strength, lift, lifetime and track speed through
   one monotonic, strongly non-linear table (F3). EF3+ must be dangerous at close range; EF5 must be
   visibly huge and rare. The pre-director scale was too flat: peak wind was about 16 m/s at EF0 and
   26 m/s at EF5.
7. **Telegraphs (all four):**
   - a **far-field cell** is rendered at ≥ 800 m in Epic (across the whole arena in compact mode) while
     Forming or Mature;
   - a **forecast** on the HUD lists each cell's bearing, estimated EF, distance and "peak in N s". The
     EF estimate can be off by ±1 and gets more accurate as the player gets close (F4), which is
     deliberate uncertainty;
   - a **siren / radio cue** on forming EF3+ and on the anchor's peak;
   - **environmental cues** (sky darkening, gust amplitude, debris) rise monotonically with the nearest
     cell's wind at the player, W(d_player), and are at maximum when W ≥ 20 m/s.
8. **Concurrency and budget.** Active cells are capped (Epic and Arcade each have a cap). Only cells
   inside the streaming ring (ADR-0004) run full wind and damage; farther cells run as simple tracks plus
   the far-field visual.
9. **Authority.** In **Epic** the director draws the regime and builds the plan. In **Arcade** a scenario
   supplies the plan parameters directly (counts, EF range, wind multiplier, timing) and no regime is
   drawn. Both use the same cell and scale model.
10. **Determinism.** All director randomness comes from one run seed with its own RNG stream, separate
    from gameplay RNG. The seed shows on the results screen so a run can be replayed.
11. **Events out.** The director raises `GameEvents` for a cell forming, reaching peak, roping out and
    ending (names agreed with AGENTS.md before implementation). It holds no references to downstream
    systems; it only raises `GameEvents`.
12. **Compact mode (provisional).** Until the 2 km world is integrated (S7-06+), the director runs in
    compact mode: the same rules with distances scaled to today's arena, so tension tuning can ship
    before the chase map does.

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

### States and Transitions

| State | Meaning | Enter when | Exit to |
|---|---|---|---|
| Idle | No plan | Title screen | Planning on run start |
| Planning | Building the Weather Plan | Run starts | Running when the plan is ready (same frame) |
| Running | Executing the plan | Plan ready | Complete at run end |
| Complete | Run over; plan kept for the results screen | Timer ends, wreck, or all cells Done | Idle on retry or title |

Per cell: **Scheduled → Forming → Mature → Roping Out → Done.** A cell can skip to Done if it leaves the
world.

### Interactions with Other Systems

| System | Reads from director | Gives to director | Notes |
|---|---|---|---|
| Disaster Entity Framework / Tornado | cell spawn, EF, scale | — | Replaces the `DisasterSpawner` timer |
| Wind Field / Vehicle Feel | scale table (wind, lift) | — | Wind and lift values live here; vehicle-feel keeps the physics |
| Run Manager & Session Modes | regime, seed, scenario override | run start / end | Arcade override comes from `session-modes.md` |
| Dynamic Objectives & Events | cell tracks, ETA | — | Events spawn on the projected path |
| HUD / Off-Screen Indicators | forecast list | — | Forecast and arrows |
| Tiled World Streaming | ring position | — | Far-field vs full simulation |
| Save & Profile | — | seed and regime for the Results record | Optional |

**Provisional assumptions:** the Epic world size (2 km) and the Arcade settings list are not yet designed;
`session-modes.md` needs a revision to define them. The `GameEvents` names need a handoff in AGENTS.md
before implementation.

## Formulas

> `systems-designer` consulted 2026-10-01 (lean mode, high-risk section). Values are hand-calculated from
> code and `vehicle-feel.md`, not yet simulated. Regime weights, Heat slopes, forecast error and the jog
> rate are **placeholders pending playtest**. Locked inputs: Pickup top speed `v_top` = 21.5 m/s; EF
> strength 1.0 / 1.5 / 2.0 / 2.5 / 3.0 / 4.0 (EF0–EF5, `TornadoData_EF*.asset`); vehicle-feel F11 wind
> force saturates at 1.2 g (≈ 12.6 m/s relative wind for the Pickup); F12 lift = 0.85 × LiftScale × t²,
> t = 1 − d / (0.5 R), LiftScale = (EFStrength − 2) × 1.35; toss at lift ≥ 0.7.

**F1. Regime draw and anchor EF**

The regime draw formula is defined as:

`w_r(H) = w0_r · exp(s_r · H)`, `P_r = w_r / Σ w`. The anchor's EF is then drawn from that regime's
anchor table, whose EF5 share is `p5(r, H) = p5_0(r) · (1 + 0.4 · H)`; the added EF5 mass is taken from
the table's lowest tier.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| H | int | 0–5 | Save & Profile (Cataclysm Heat) | Heat level for the run |
| w0_r | float | 0–1 | data file | Base weight: Quiet .20, Lone Giant .25, Sequence .30, Outbreak .15, Chaos .10 |
| s_r | float | −1 to 1 | data file | Heat slope: Quiet −0.55, Lone +0.05, Sequence +0.10, Outbreak +0.30, Chaos +0.30 |
| p5_0(r) | float | 0–0.3 | data file | Base EF5 share: Lone .10, Sequence .03, Outbreak .03, Quiet 0, Chaos .01 per cell (Chaos also scales by (1 + 0.4H)) |
| Anchor table | table | — | data file | Quiet EF0 .4 / EF1 .4 / EF2 .2; Lone EF3 .55 / EF4 .35 / EF5 .10; Sequence EF2 .30 / EF3 .40 / EF4 .27 / EF5 .03; Outbreak EF2 .30 / EF3 .45 / EF4 .22 / EF5 .03; Chaos: no anchor, each cell draws independently (EF4 ≈ .06), at most one EF5 per run |

**Output Range:** probabilities sum to 1. Heat 0 → Quiet .200, Lone .250, Sequence .300, Outbreak .150,
Chaos .100. Heat 5 → Quiet .007, Lone .165, Sequence .254, Outbreak .345, Chaos .230. P(any EF5 in a run)
≈ 4.2 % at Heat 0, ≈ 13 % at Heat 5.
**Example:** Heat 3 → weights Q .038, L .291, S .405, O .369, C .246 (sum 1.349) → P = Q .028, L .215,
S .300, O .274, C .182. Draw u = 0.47 → Sequence (cumulative .543). Sequence p5 = .03 × 2.2 = .066, so
EF2 drops to .264; second draw 0.90 → EF4 (cumulative .264 / .664 / .934).

**F2. Cell schedule**

The cell schedule formula is defined as:

`T = 180 + 45 · clamp(N_cells, 3, 6)`; anchor peak `t_peak = U(0.40, 0.65) · T`; anchor spawn
`t_a = t_peak − Form(EF_a)`, clamped so the anchor is Mature by `T − 30`. Satellites by regime:

- Quiet / Lone Giant: S = 1–3, EF ≤ 2 (Lone: ≤ min(2, EF_a − 2)), spawn at `U(0.05T, 0.75T)`.
- Sequence: S = 2–4, spawn `t_k = t_a − (S − k + 1) · Δ`, `Δ = U(35, 55)` s, EF rising to min(EF_a − 1, 3);
  drop any with t < 5 s.
- Outbreak: S = 3–5, EF ≤ min(EF_a − 1, 3), spawn `t_a + U(−15, 15)` within 120 m of the anchor.
- Chaos: N = U{3..8}, spawn at `U(5, 0.8T)`, no anchor.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| N_cells | int | 3–8 | calculated | Anchor + satellites (Chaos: all cells) |
| T | float | 315–450 s | calculated | Epic session length (`session-modes.md` Chase formula) |
| t_peak | float | 0.40T–0.65T | calculated | When the anchor reaches Mature |
| Form(EF) | float | 20–45 s | data file (F3) | Forming time of the cell's EF |
| SpawnDist (Epic) | float | anchor 350–700 m, satellites 200–600 m | data file | From plan origin P0 (player start); first satellite ≥ 40° from the anchor bearing; ≥ 100 m inside the world edge |
| SpawnDist (compact) | float | anchor 70–110 m | data file | Compact mode (pre-2 km world) |
| CapRing / CapTotal | int | Epic 3 / 5 (Chaos 6); Arcade 3; compact 2 | data file | Concurrency caps |
| CapDelay | float | 10 s steps, 30 s max | data file | Over-cap spawns wait; past 30 s the lowest-EF waiting cell is dropped. **Resolved against CapTotal at plan time**, so the plan never depends on the player. CapRing only decides which cells run full simulation, never when they spawn |

**Output Range:** T = 315–450 s; anchor spawn ≥ 15 s; never more cells alive than the cap.
**Example:** Sequence, S = 3 → N = 4, T = 360 s. Anchor EF4 (Form 40 s), t_peak = 0.5T = 180 s, spawn
140 s. Δ = 45 s → satellites spawn at 95, 50 and 5 s as EF3, EF2, EF1.

**F3. Storm scale**

The storm scale formula is defined as:

`W(d) = P_EF · I · (1 − d / (R_EF · I))²` for d < R_EF · I, split into inflow 0.537 · W and swirl
0.843 · W (the existing 7 : 11 ratio). Every other column is a per-EF lookup.

| EF | Damage radius D | Wind radius R | Peak wind P | P / v_top | W at 15 m | Form / Mature / Rope-out | Track speed |
|----|----|----|----|----|----|----|----|
| EF0 | 1.5 m | 12 m | 8 m/s | 0.37 | 0 | 20 / 30 / 10 s | 3 m/s |
| EF1 | 2.2 m | 18 m | 12 m/s | 0.56 | 0.3 m/s | 25 / 40 / 12 s | 4 m/s |
| EF2 | 3.2 m | 26 m | 17 m/s | 0.79 | 3.0 m/s | 30 / 50 / 15 s | 5 m/s |
| EF3 | 5 m | 34 m | 26 m/s | 1.21 | 8.1 m/s | 35 / 60 / 20 s | 6.5 m/s |
| EF4 | 6.5 m | 52 m | 40 m/s | 1.86 | 20.3 m/s | 40 / 75 / 25 s | 8 m/s |
| EF5 | 12 m | 70 m | 62 m/s | 2.88 | 38.3 m/s | 45 / 90 / 30 s | 8 m/s |

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| d | float | ≥ 0 m | calculated | Horizontal distance from the funnel axis |
| I | float | 0–1 | calculated | Lifecycle intensity (ramps in Forming, 1 in Mature, ramps out in Roping Out) |
| P_EF, R_EF, D_EF | float | table | data file (`TornadoData` per EF) | Peak wind, wind radius, damage radius |
| Form / Mature / Rope | float | table | data file | Lifecycle seconds. Compact mode: Form × 0.25 (≥ 4 s), Mature and Rope × 0.5 |
| v_track | float | table | data file | Base track speed (F5) |
| ConeScale | float | — | data file | Visual only from now on: funnel width factor ≈ D / 4.3 |

**Output Range:** EF3 and above out-blow the truck's top speed inside the core, so the core can't be
out-driven; track speeds stay ≤ 0.4 v_top, so fleeing is always possible. Toss radius: EF3 never
(peak lift 0.574 < 0.7), **EF4 within ≈ 5.7 m** (inside its 6.5 m core; Andy 2026-10-01: keep), **EF5
within ≈ 15.7 m**. **Compact mode allows EF5 at full size** (Andy 2026-10-01): an EF5 covers most of the
≈ 180 m arena, a deliberate survival moment.
**Example (locked checks):** EF3 Mature, truck parked at 15 m → W = 26 × (1 − 15/34)² = 8.1 m/s → wind
accel ≈ 7.6 m/s² → ≈ 3.8 m moved in 1 s (> 0.5 ✓); lift at 15 m ≈ 0.008 → not Tossed ✓. EF5 Mature,
5 m → lift = 0.85 × 2.7 × (1 − 5/35)² = 1.69 ≥ 0.7 → Tossed on the first Mature frame ✓.
**Implementation note:** replaces `TornadoController`'s sqrt EF wind scale, which cannot exceed ≈ 2×
EF0 (this table needs 7.75×). Remove `_windScaleBase`, `_windScalePerSqrtEF`, the radius-per-ConeScale
fields and `_baseDamageRadius`; keep `_windInflow` / `_windSwirl` as direction ratios only.

**F4. Forecast estimate**

The forecast formula is defined as:

`σ(d) = 0.7 · g`, `g = clamp01((d − 150) / 650)`;
`EF_shown = clamp(round(EF + clamp(b_i · σ(d), −1, +1)), 0, 5)`;
`ETA_shown = max(0, t_true · (1 + clamp(b_t, −1.5, 1.5) · (0.05 + 0.25 · g)))`, rounded to 5 s.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| d | float | ≥ 0 m | calculated | Player-to-cell distance |
| b_i, b_t | float | N(0, 1) | seeded, fixed per cell | Bias terms; fixed so the readout never flickers |
| 150 m / 800 m | float | constant | data file | Exact inside 150 m; maximum error at ≥ 800 m |
| t_true | float | ≥ 0 s | calculated | True time until the cell's next phase (peak or rope-out) |

**Hysteresis:** the shown EF changes only after d has moved 10 m past the boundary that flipped it, so the
readout never flickers.
**Output Range:** |EF error| ≤ 1 always; 0 inside 150 m; P(wrong EF) ≈ 47 % at ≥ 800 m. ETA error
±5 % close, up to ±45 % far.
**Example:** EF4 at 500 m → g = 0.538, σ = 0.377, b = −1.4 → error −0.53 → HUD shows **EF3** (corrects to
EF4 near 150 m). True ETA 60 s, b_t = +1.2 → factor 1.22 → "peak in ~75 s".

**F5. Track motion**

The track motion formula is defined as:

`dθ/dt = n(t) · ω_EF`, `n = 2 · Perlin(seed_i, 0.25 t) − 1`; initial heading θ0 within ±60° of the
bearing to world centre; `v = v_track · lerp(0.4, 1, I)`. Late-life jogs are pre-rolled at plan time as
a Poisson process with rate `λ(u) = λ0 · clamp01((u − 0.6) / 0.4)`, u = age / lifetime. Each jog turns
`±U(40°, 110°)` eased over 1.5 s with a 3 s speed burst (× 1.5), telegraphed by a 1 s funnel tilt.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| ω_EF | float | 24 / 20 / 16 / 12 / 9 / 7 °/s (EF0–5) | data file | Wander turn rate; big storms are ponderous |
| v_track | float | 3–8 m/s | data file (F3) | Base speed |
| λ0 | float | 1/12 per s | data file | Jog rate at end of life (placeholder) |
| v_cap | float | 0.6 · v_top = 12.9 m/s | constant | Jog speed cap |
| Player position | — | — | — | **Never read** (no homing, Core Rule 5) |

**Output Range:** heading unbounded; speed 0.4 v_track to 12.9 m/s during jogs. Epic: a cell that leaves
the world ends. Compact keeps today's edge steer-back.
**Example:** EF4, ω = 9°/s, noise 0.2 → turns 1.8°/s. At u = 0.75, λ = 0.083 × 0.375 = 0.031/s, about one
jog per 32 s; a +75° jog at 8–12 m/s for 3 s moves the funnel ≈ 30 m off its expected path.

## Edge Cases

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

**Plan and spawning**
- **If a spawn point falls in a blocked or invalid spot** (off the map, inside a POI): reroll the point
  up to 8 times from the same seeded stream, then place it at the nearest valid point along the same
  bearing. The plan stays deterministic.
- **If the player drives far from the plan origin**: cells still spawn where planned. The forecast always
  shows bearing and distance, so a storm behind you is a choice, not a bug (live distance clamp: Open
  Questions).
- **If a planned spawn would exceed the concurrency cap**: resolved at plan time against CapTotal: delay it
  in 10 s steps, and drop the lowest-EF waiting cell after 30 s (F2). **The anchor is never dropped.** If the anchor is the one waiting, the
  lowest-EF live satellite is told to rope out early instead.
- **If the run ends before the anchor reaches Mature** (a wreck, or the timer in Arcade): the anchor never
  peaks. The results screen shows "The big one got away" with its EF. That's tension, not an error.
- **If a Chaos run rolls only EF0–1 cells**: allowed. It's a quiet chaos day. Chaos guarantees nothing.
- **If a Chaos run rolls two EF5s**: impossible, since F1 caps Chaos at one EF5 per run.

**Storms meeting**
- **If two cells' wind fields overlap**: their wind vectors add (the existing `TotalWindAt`), and lift uses
  the strongest single cell (the existing `MaxLiftFractionAt`). There is no merge here; merging is Disaster
  Alchemy (#13).
- **If two funnels' damage radii overlap the player in the same frame**: one damage tick, from the
  strongest cell. The existing i-frames still apply.

**Player and storm**
- **If the player is inside the core when a cell spawns or forms**: damage and lift are zero while
  Forming (Core Rule 4); only wind ramps. The player gets the Forming window to react.
- **If an EF5 forms in compact mode with the player anywhere in the arena**: allowed (Andy, 2026-10-01).
  The arena has no safe zone from wind, but the tossing core is only ≈ 16 m across (F3), so survival comes
  down to distance and luck.
- **If a storm's late-life jog points at the player**: allowed. Jogs are pre-rolled at plan time without
  the player's position (F5), so it's chance, not homing. The 1 s funnel-tilt warning always plays first.
- **If the truck is tossed while Critical**: normal toss rules apply; limp mode doesn't change lift
  (`vehicle-damage.md`).

**Forecast**
- **If the forecast EF is wrong and the player commits based on it**: intended. The error is at most ±1 EF
  and shrinks inside 150 m (F4), and the true EF is always revealed when the player is close or takes a
  photo.
- **If more cells are alive than the forecast UI can show**: list the nearest 3 by distance plus the
  anchor; the rest are summarized as "+N more".

**Arcade and seeds**
- **If an Arcade scenario's settings are contradictory** (an EF range wider than its storm count allows,
  more storms than the cap): clamp to the caps and to valid ranges, and show the clamped settings before
  the run starts. Never fail silently.
- **If the same seed is replayed in a different build**: identical plans are guaranteed only within the
  same build version. The version is stored next to the seed, and a mismatch shows "Different version:
  storms may differ."

**Timing**
- **If the game is paused or the tab is hidden**: director time is game time, so it pauses too. No cell
  advances while paused.
- **If the frame rate drops badly**: cell motion integrates on game time with a capped step, so a storm
  never teleports past the player.

## Dependencies

**Depends on (upstream):**

| Dependency | Type | Interface |
|------------|------|-----------|
| Disaster Entity Framework / Tornado (`disaster-entity-framework.md`; `TornadoController`, `TornadoData`, `TornadoLifecycle`) | Hard | Director spawns cells and hands each its EF row (F3) and track (F5); `TornadoData` gains per-EF scale fields. That GDD already defers spawn choice, placement and timing to this doc; its EF table (wind radius 20–38 m, move speed 4–16 m/s) is the **current** behavior that F3/F5 replace. Back-link: pending, owned by the session editing that doc |
| Run Manager & Session Modes (`session-modes.md`) | Hard | Run start and end, mode (Epic / Arcade / compact), session length T; Arcade scenario supplies plan parameters |
| Tiled World Streaming (ADR-0004) | Hard for Epic, soft for compact | World bounds; streaming-ring position (full simulation vs far-field) |
| Cataclysm Heat (`economy-progression.md`) | Soft | Heat 0–5 for F1; defaults to 0 |

**Depended on by (downstream):**

| Dependent | Type | Needs from this system | Back-link |
|-----------|------|------------------------|-----------|
| Wind Field / Vehicle Feel (`vehicle-feel.md` F11/F12) | Hard | Per-EF peak wind and radius; lift reads EF strength and radius | Added 2026-10-01; F12 sanity note updated |
| Dynamic Objectives & Events (`event-system.md`) | Hard | Cell tracks and ETA for path projection; EF for event density | Added 2026-10-01 |
| Run Manager & Session Modes (`session-modes.md`) | Hard | Weather Plan replaces the Storm Front EF Escalation curve; cell count feeds T | Added 2026-10-01; escalation curve marked superseded |
| Photo Documentation & Scoring (`photo-scoring.md`) | Soft | True EF revealed by a photo (forecast reveal); EF strength in score unchanged | No change needed |
| HUD / Off-Screen Indicators | Soft | Forecast list and bearings | No GDD yet |
| Procedural Audio | Soft | Siren, radio and wind-intensity cues | No GDD yet |
| Save & Profile (`save-profile.md`) | Soft | Seed and regime, optionally stored with run results | Pending (save-profile is under review) |

**Code impact:** replaces `DisasterSpawner`'s timer and roster weights, and `TornadoController`'s sqrt
wind scale, player pull (`_playerPull` → 0) and radius-per-ConeScale fields.

## Visual/Audio Requirements

> `art-director` consulted 2026-10-01 (lean mode; storm telegraphs are read to play). Presentation code
> is Codex's lane (`Scripts/Presentation/**`); `audio-director` owns ProceduralAudio specifics.

**Far-field cell (≥ 800 m in Epic)**
- A sky-layer object, not a world object: a slowly rotating low-poly wall-cloud base, 2–3 painted anvil
  cards, and one tapered funnel silhouette card, in the same card language as the near tornado.
- ADR-0004 fog ends at ≈ 250 m and would erase it, so it needs a **fog-exempt** material drawn after the
  distant silhouette mesh and tinted toward the horizon colour by distance.
- **EF reads without text:** apparent width scales with D, and value darkens with EF (EF0–1 grey-blue,
  lighter than the sky; EF4–5 near-black with a green-teal cast). Rotation slows as EF rises. Size is
  always honest, which rewards reading the sky over trusting the ±1 EF forecast.
- **While Forming:** the base lowers and widens with I and the funnel card grows down, never reaching
  the ground.

**EF up close**
- Funnel width scales with D, not R. **EF5 is a wedge** (width ≥ height) with 2–3 thin sub-vortex cards
  orbiting inside it, the cheapest blind-clip separator from EF4 (silhouette beats size).
- **Debris ring:** radius ≈ 1.2 × D, height ≈ 1.5 × D; EF5 lifts large chunks (planks, roof panels) and
  lower EFs small ones, using sprite size classes rather than more particles.
- **Ground dust skirt:** out to ≈ 0.5 R (≈ 35 m for EF5), a low rolling ring showing where the wind
  starts.
- **WebGL budget (AC-29):** one atlas and one material for funnel, sub-vortices and debris (instanced);
  dust skirt is one ring mesh with a scrolling texture, not particles; ≈ 1,200 debris and ≈ 500 gust
  particles; no per-particle lights, no soft particles, overdraw ≤ 4 layers on the wedge.

**Lifecycle**
- **Forming:** base rotates and lowers, condensation funnel reaches ≈ 60 % height, no ground contact, no
  dust (damage is zero, so nothing touches the ground).
- **Mature:** full ground contact, debris ring and dust skirt on, peak darkness.
- **Roping Out:** width falls to ≈ 0.25, the funnel bends into an S-curve or tilts up to 30°, debris drops
  off, the base lightens. It reads as a rope, not a shrinking cone.
- **Jog telegraph (1 s):** the funnel leans ≈ 15° toward the new heading, the base shifts off-centre, and
  a dust puff kicks up on the leading side. The lean always points where the storm will go.

**Environmental cues**
- One normalised value `e = clamp01(W(d_player) / 20 m/s)` drives every cue so they rise together: sky
  gradient and sun intensity drop by up to 60 % with a green-teal shift; gust streak density and length;
  grass and tree sway; pooled ambient debris (leaves, paper).
- World-grade only: no grain or CRT on the world as storms rise.

**Audio (mood; synthesis by `audio-director`)**
- **EF3+ forming:** distant and institutional, a radio voice through static, synced to the base starting
  to lower, not to the HUD.
- **Anchor peak:** a sharper alert on the frame the funnel touches down.
- **Wind roar:** tracks the same `e` as the sky so sound and picture agree; EF5 adds a low continuous
  rumble.
- **Jog:** a 1 s rising sound with the lean, landing on the speed burst.

**ADR-0003 principles that apply most:** cards carry the energy (tornado, debris, dust and far-field are
all 2D cards in 3D); the world is a cartoon (silhouette and value contrast tell EF apart, not effects or
text); your footage is from 1996 (the camcorder treatment stays in the viewfinder and photos, and its LUT
must not crush the near-black EF5 wedge, the money shot).

**New assets / shaders:** fog-exempt far-field sky material or pass; tornado card atlas with a wedge
variant and sub-vortex cards; debris sprite atlas in 3 size classes; dust-skirt ring mesh with a
scrolling shader; anvil and wall-cloud card set; a global storm-intensity `e` shader parameter.

## Game Feel

The director should feel like weather, not a game master. The sky warns you before anything can hurt you,
so the dread arrives early and on fair terms: you always see the anchor forming before it can matter
(AC-30). The tension loop is **notice → commit → arrive → risk → escape**:

- **Notice:** a far-field cell forms.
- **Commit:** you drive across the map, with the forecast's uncertainty making you guess.
- **Arrive:** you reach the storm during its Mature window.
- **Risk:** you decide how close to go; an EF4/5 core tosses you.
- **Escape:** storms are slower than the truck, so fleeing always works if you start in time (AC-32).

Every loss should read as a choice the player made: too slow to commit, too close, too greedy. The only
exceptions are a late-life jog, which is always telegraphed, and Chaos runs, which are allowed to be
cruel. An EF5 should change how the player drives: they slow down, circle, and line up the shot from a
respectful distance. Arcade drops the notice and commit steps (short forming, nearby spawns) and keeps
arrive, risk and escape.

## UI Requirements

- **Forecast panel (HUD):** one row per cell, for the nearest 3 plus the anchor, then "+N more". Each row
  has a bearing arrow, estimated EF (marked "~EF3" while uncertain, plain "EF3" inside 150 m), distance,
  and "peak in ~N s" or "roping out". The anchor's row is highlighted. Shares the off-screen indicator's
  bearing logic.
- **Radio caption:** a one-line caption when the siren or radio cue plays ("Tornado warning: EF3+ forming,
  bearing NW"), so the audio is also readable on screen (accessibility).
- **Results screen:** the seed and build version, the regime name ("Outbreak"), and "The big one got away
  (EF5)" when the anchor never peaked.
- **Arcade setup:** shows each setting after clamping before the run starts, with clamped values marked.
- UI Toolkit, gamepad-navigable, readable on Steam Deck.

## Cross-References

| Referenced doc | What this GDD relies on | Used in |
|----------------|-------------------------|---------|
| `vehicle-feel.md` | F11 wind force, F12 lift / toss threshold 0.7, `LiftCoefficient` 1.35, Pickup top speed 21.5 m/s, the EF3 15 m acceptance criterion | F3; AC-11–13 |
| `disaster-entity-framework.md` | Disaster and tornado model, `ThreatMultiplier` values, current spawner (being replaced) | Core Rules 1, 6; F3 |
| `session-modes.md` | Session length T, Sprint / Chase (becoming Arcade / Epic), superseded storm-front curve | F2; Rule 9 |
| `event-system.md` | Events spawn on the projected path; spawn chance by EF | Interactions |
| `economy-progression.md` | Cataclysm Heat 0–5 | F1 |
| `photo-scoring.md` | EF strength in photo score; a photo reveals true EF | Rule 7; F4 |
| `vehicle-damage.md` | Limp mode doesn't change lift | Edge Cases |
| ADR-0004 | 2 km world, streaming ring, fog distance | Rules 8, 12; F2 |
| ADR-0003 | Stylized world and retro-lens art direction | Visual/Audio |

## Acceptance Criteria

> `qa-lead` consulted 2026-10-01 (lean mode). Plan generation is a pure C# model (seed, Heat, mode,
> scenario → WeatherPlan), so most criteria are EditMode. Test classes: `StormDirectorTests` (EditMode)
> and `StormDirectorPlayTests` (PlayMode). PlayMode criteria need a test seam that forces a cell to
> Mature, since real Form times are 35–45 s. Tags: **[Unit]** · **[PlayMode]** · **[WebGL]** ·
> **[Playtest]**. Performance budgets and playtest thresholds are proposals approved by Andy 2026-10-01.

**Plan, regimes, determinism (Rules 1, 2, 9, 10)**
- **AC-1 [Unit]** GIVEN seed 12345, Heat 2, Epic, WHEN the plan is generated twice, THEN the serialized
  plans are byte-identical, including when 1,000 `UnityEngine.Random` calls happen between the two
  generations (own RNG stream).
- **AC-2 [Unit] (F1)** GIVEN 10,000 seeds at Heat 0, WHEN regimes are drawn, THEN each share is within
  ±1.5 pp of .20 / .25 / .30 / .15 / .10; at Heat 5, within ±1.5 pp of .007 / .165 / .254 / .345 / .230.
- **AC-3 [Unit] (Rule 9)** GIVEN an Arcade scenario, WHEN the plan is generated, THEN no regime is drawn
  and cell count, EF range and wind multiplier match the scenario exactly.
- **AC-4 [PlayMode] (Rule 10)** GIVEN a finished run, WHEN Results shows, THEN the seed and build version
  are displayed; replaying the seed on another version shows "Different version: storms may differ."
  Screenshot retained in `production/qa/evidence/`.

**Anchor and EF5 (Rule 3, F1)**
- **AC-5 [Unit]** GIVEN 10,000 seeds, THEN P(any EF5) = 4.2 % ±0.8 pp at Heat 0 and 13 % ±1.0 pp at Heat 5.
- **AC-6 [Unit]** GIVEN 10,000 non-Chaos plans, THEN exactly one cell is the anchor, no satellite is EF4+,
  no satellite exceeds the anchor's EF, and Quiet plans contain no EF5.
- **AC-7 [Unit]** GIVEN 10,000 Chaos plans at Heat 5, THEN none has more than one EF5 and none has an anchor.

**Schedule (Rule 8, F2)**
- **AC-8 [Unit]** For all plans: T ∈ [315, 450] s; the anchor's Mature start ∈ [0.40T, 0.65T] and ≤ T − 30;
  anchor spawn ≥ 15 s; no Sequence satellite spawns before 5 s; in Epic, anchor Form + Mature ≥ distance
  from P0 ÷ (0.7 × 21.5 m/s).
- **AC-9 [Unit]** GIVEN an Outbreak plan with S = 5 and CapTotal 5, WHEN the schedule is resolved, THEN live
  cells never exceed the cap, over-cap spawns wait in 10 s steps, and the lowest-EF waiting cell drops
  after 30 s. Across 10,000 plans the anchor is never dropped; when the anchor waits, a satellite ropes
  out early instead.

**Scale (Rule 6, F3)**
- **AC-10 [Unit]** The table is strictly monotonic in D, R and P; P / 21.5 > 1 for EF3+; track speed
  ≤ 8.6 m/s for every EF; W(15 m) = 8.1 ±0.05 m/s for EF3 and 38.3 ±0.1 m/s for EF5.
- **AC-11 [PlayMode]** GIVEN a Mature EF3 and the truck parked 15 m away with no input, WHEN 1 s passes,
  THEN horizontal displacement > 0.5 m and `Tossed` is not raised.
- **AC-12 [PlayMode]** GIVEN a Mature EF5 and the truck parked 5 m away, THEN `Tossed` fires within 1 s.
- **AC-13 [PlayMode]** EF4: Tossed within 1 s at 5.0 m (lift 0.75); not Tossed after 2 s at 8.0 m
  (lift 0.55). EF5: not Tossed at 17 m (lift 0.61).

**Forecast (Rule 7, F4)**
- **AC-14 [Unit]** For every EF, any bias in [−3, 3] and any d ≤ 150 m, the shown EF equals the true EF;
  for any d, |shown − true| ≤ 1.
- **AC-15 [Unit]** For a fixed cell and d, 1,000 evaluations return the same value; a d oscillating ±5 m
  across a flip boundary never changes the shown EF (hysteresis). GIVEN 10,000 EF2 cells at 800 m, THEN
  47.5 % ±1.5 pp are shown wrong.
- **AC-16 [Unit]** ETA ≥ 0 and a multiple of 5 s; inside 150 m its error ≤ 5 % of t_true + 2.5 s; never
  more than 45 % of t_true + 2.5 s.

**Tracks and lifecycle (Rules 4, 5, F5)**
- **AC-17 [PlayMode]** GIVEN seed 777 run twice, once idle and once driving a scripted loop, THEN every
  cell's position sampled every 0.5 s matches within 0.01 m. Companion [Unit]: the track model API takes
  no player or transform parameter.
- **AC-18 [Unit]** Jogs only start at u ≥ 0.6; each jog's tilt telegraph begins 1.0 s ±1 frame before the
  turn; jog speed ≤ 12.9 m/s.
- **AC-19 [PlayMode]** GIVEN the player inside an EF5's core while it is Forming, THEN no damage is taken
  and the truck is not Tossed.

**Events, compact mode, pause (Rules 7, 11, 12)**
- **AC-20 [PlayMode]** Each cell raises Forming → Peak → RopeOut → Ended once each, in order; a cell that
  leaves the world raises Ended. The director assembly holds no references to downstream systems.
- **AC-21 [Unit]** Compact mode: anchors spawn 70–110 m from P0; Form = max(4 s, 0.25 × table); Mature and
  Rope-out = 0.5 × table; cap 2.
- **AC-22 [PlayMode]** Pausing for 10 s leaves every cell's age and position unchanged. [WebGL]: same with
  the browser tab hidden for 10 s.
- **AC-23 [PlayMode]** At a forced 5 FPS, no cell moves more than v × max-step in one frame.
- **AC-24 [PlayMode]** With 6 cells live, the forecast shows the nearest 3 plus the anchor and "+2 more".
  Screenshot retained.
- **AC-25 [PlayMode]** GIVEN a run ends before the anchor is Mature, THEN Results shows "The big one got
  away" with its EF.
- **AC-26 [WebGL]** GIVEN an anchor Forming 800 m away in Epic, THEN its far-field cell is visible on
  screen; environmental cues are at maximum when W(d_player) ≥ 20 m/s. Screenshot retained.

**Performance**
- **AC-27 [Unit / WebGL]** Plan generation ≤ 2 ms desktop, ≤ 8 ms WebGL (median of 100 runs).
- **AC-28 [PlayMode]** Director per-frame cost with 6 live cells ≤ 0.10 ms desktop, ≤ 0.30 ms WebGL;
  0 B GC allocation per frame in steady state.
- **AC-29 [WebGL]** With a Mature EF5 filling the compact arena, the reference machine holds a p95 frame
  time ≤ 33.3 ms over 60 s, ≤ 2,000 live particles, and ≤ 40 extra draw calls over an empty sky.

**Subjective goals**
- **AC-30 [Playtest]** Five testers play fixed Lone Giant and Sequence seeds: ≥ 4 of 5 turn toward the
  anchor while it is still Forming (heading logged), and ≥ 4 of 5 answer yes to "Did you see the big storm
  building before it peaked?"
- **AC-31 [Playtest]** In a blind set of 10 paired EF4 / EF5 clips, testers pick the EF5 in ≥ 80 % of pairs,
  and ≥ 4 of 5 rate the EF5 "imposing" at 4+ on a 5-point scale.
- **AC-32 [Playtest]** Fleeing a Mature EF4 at full throttle from 30 m, the tester escapes in ≥ 9 of 10
  trials.

## Open Questions

| Question | Owner | Resolve by |
|----------|-------|-----------|
| Live distance clamp: should spawns follow a player who drives far from the plan origin? | game-designer | After the first Epic playtest |
| Regime weights, Heat slopes, jog rate and forecast error are placeholders; simulate 10k plans and playtest | systems-designer | Before the director story is accepted |
| WebGL EF5 budget (AC-29): can a 12 m core plus debris fit? | technical-artist / Codex (X7-06) | Before EF5 ships |
| The Arcade settings list (which knobs, ranges, scenario-code format) | `session-modes.md` revision | Next modes pass |
| `GameEvents` names for cell Forming / Peak / RopeOut / Ended | Claude + Codex via AGENTS.md | Before implementation |
| Back-links from `disaster-entity-framework.md` and `save-profile.md` (seed in run record) | The session editing those docs | When their reviews close |
| Compact-mode EF5 at full size: fair, or just brutal? | Andy, playtest | First 0.7 playtest |
| Fog-exempt far-field rendering under URP Render Graph (main technical unknown) | technical-artist / Codex | Before the Epic world integrates |
| A compact-mode EF5 wedge can hide the truck: camera pull-back or translucency near the core? | art-director / Camera (S7-05) | First 0.7 playtest |
| EF5 wedge overdraw on WebGL; profile EF5 Mature up close first | technical-artist / Codex (X7-06) | Before EF5 ships |
| Readability at maximum darkening (`e` = 1) with a near-black EF5: check outline contrast | art-director | First EF5 capture |
