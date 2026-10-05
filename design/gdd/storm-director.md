# Storm Director

> **Status**: Approved (2026-10-03, fifth /design-review)
> **Author**: Andy Styx + Claude
> **Last Updated**: 2026-10-03
> **Last Verified**: 2026-10-03
> **Implements Pillar**: 2 — Disaster Stacking & "Disaster Alchemy" (game-concept.md); serves Pillar 1 (Kinetic Chaos) via tension, and the anti-pillar "NOT scripted spectacle" (set pieces come from rules)

## Summary

The Storm Director decides which tornadoes a run gets, where they form, when they peak and how violent they
are. It builds a seeded Weather Plan from regimes like Lone Giant, Outbreak and Chaos, around one anchor
storm and smaller satellites, and it owns the EF scale that makes an EF5 imposing and rare. It turns random
spawning into a run with a shape: tension you can see building on the horizon, a window to get there, and a
peak worth the risk.

> **Quick reference** — Layer: `Core` · Priority: `MVP` (compact mode now; Epic after the 2 km world) · Key deps: Disaster Entity Framework / Tornado, Run Manager & Session Modes, Tiled World Streaming · Key dependent: Vehicle Feel (reads wind and lift)

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
   give early points and risk. This is what keeps EF5 rare. **Heat 5 exception (Supercell
   Convergence):** Sequence and Outbreak plans add one **co-anchor**, fixed at EF4, so two high-threat
   cells share the run (Andy, 2026-10-02). In those plans the anchor is floored at EF4 (F1), so the
   anchor still holds the run's peak and the co-anchor never outranks it. The co-anchor follows the
   anchor's never-dropped rule (F2). Lone Giant, Quiet and Chaos are unchanged.
4. **Cell lifecycle.** Forming (visible and growing; **damage and lift are zero**, wind ramps with
   intensity), Mature (the peak window), Roping Out (decline), Done. Bigger storms last longer.
   Intensity I ramps linearly 0 → 1 over Forming, holds at 1 through Mature, and falls linearly 1 → 0
   over Roping Out. **In Roping Out, wind, lift and damage radius all scale with I** (Andy, 2026-10-02):
   a fresh rope-out is still dangerous and its end is harmless (F3). In Epic the anchor is reachable
   *from the plan origin* even if its track runs straight away: its spawn distance from P0 is at most
   d_reach = (Form + Mature) × (0.7 × v_top − k_H × v_track) (F2). The 30 % throttle margin covers
   steering and late-life jogs. A player who drives away from P0 can still miss it; that is a choice
   (Edge Cases). Arcade scenarios use short forming times.
5. **Tracks.** A cell moves along its own heading and speed with wander. **No homing:** the track ignores
   the player. Late in a cell's life it makes erratic jogs, so a storm is dangerous to *loiter near*, not
   to flee. A cell that leaves the world ends.
6. **Storm scale.** EF rating maps to funnel size, wind strength, lift, lifetime and track speed through
   one monotonic, strongly non-linear table (F3). EF3+ must be dangerous at close range; EF5 must be
   visibly huge and rare. The pre-director scale was too flat: peak wind was about 16 m/s at EF0 and
   26 m/s at EF5.
7. **Telegraphs (all four):**
   - a **far-field cell** stays visible out to at least 800 m in Epic (across the whole arena in compact mode) while
     Forming or Mature;
   - a **forecast** on the HUD lists each cell's bearing, estimated EF, distance and "peak in N s". The
     EF estimate can be off by ±1 and gets more accurate as the player gets close (F4), which is
     deliberate uncertainty;
   - a **siren / radio cue** on forming EF3+ and on the anchor's peak. The siren keys on the **true** EF
     (like a warning system detecting rotation), but its caption never states an EF ("severe cell
     forming"), so it says a cell is dangerous, not how dangerous, and the forecast's ±1 still matters
     (Andy, 2026-10-03). **Exception, EF5 (Andy, 2026-10-04):** a forming true EF5 escalates the warning to
     **"TORNADO EMERGENCY"** (the real-world term for a violent tornado), with a harsher alert tone; still no
     EF number. It is presented as a **KTVR News crawl** (lower-third ticker) with a broadcast attention tone;
   - **environmental cues** (sky darkening, gust amplitude, debris) rise monotonically with the wind
     strength at the player, W_player = Σ_i |W_i(player)|, the **sum of each cell's wind magnitude**,
     and are at maximum when W_player ≥ 20 m/s. Magnitudes add without cancelling, so a weak cell nearby
     never masks a strong one farther out. Physics still uses the vector sum (`TotalWindAt`, Edge Cases).
8. **Concurrency and budget.** Active cells are capped (Epic and Arcade each have a cap). Only cells
   inside the streaming ring (ADR-0004) run full wind and damage; farther cells skip wind and damage and
   show the far-field visual. **Track motion (F5) is identical in both tiers**, so the ring, which follows
   the player, never changes where a storm is (Rules 5, 10; AC-17).
9. **Authority.** In **Epic** the director draws the regime and builds the plan. In **Arcade** a scenario
   supplies the plan parameters directly (counts, EF range, wind multiplier, timing) and no regime is
   drawn. Both use the same cell and scale model.
10. **Determinism.** All director randomness comes from one run seed with its own RNG stream, separate
    from gameplay RNG. The stream and the track noise are the director's own implementations (a seeded
    integer PRNG and hash-based noise), never `UnityEngine.Random`, `System.Random` or
    `Mathf.PerlinNoise`, so a plan is the same on desktop and WebGL builds of one version. The seed shows
    on the results screen so a run can be replayed.
11. **Events out.** The director raises four `GameEvents`, agreed with Codex in AGENTS.md (2026-10-02):
    `StormCellForming`, `StormCellPeak` (Mature start; for the anchor, the touchdown alert),
    `StormCellRopeOut` and `StormCellEnded`, all `Action<StormCellInfo>` with
    `StormCellInfo { CellId, EF, Role (Anchor / CoAnchor / Satellite), Position }`, where Position is the
    cell's position at that transition. Per-frame values (I, wind at the player, forecast rows) are a
    read-only query surface, not events. **Lifecycle rule:**
    - a cell that never spawns (dropped by the cap, or scheduled after the run ends) raises nothing;
    - every spawned cell raises `StormCellEnded` exactly once;
    - normal life: Forming → Peak → RopeOut → Ended;
    - evicted while Forming (failed touchdown): Forming → RopeOut → Ended, no Peak;
    - cut short by leaving the world or by run end: `StormCellEnded` immediately, with no fabricated Peak
      or RopeOut.

    The director holds no references to downstream systems; it only raises `GameEvents`.
12. **Compact mode (provisional).** Until the 2 km world (ADR-0004) replaces today's arena, the director runs in
    compact mode, a **mini-Epic** on today's arena (Andy, 2026-10-02): it draws a regime (F1, Heat
    applies) and builds a plan exactly as Epic does, but with a fixed T = 180 s, compact spawn
    distances, compact lifecycle times, smaller satellite counts and a cap of 2 (F2, F3). Arcade scenarios played on the compact
    arena keep their own timer and are not affected. This lets tension tuning ship before the chase map
    does.

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

### States and Transitions

| State | Meaning | Enter when | Exit to |
|---|---|---|---|
| Idle | No plan | Title screen | Planning on run start |
| Planning | Building the Weather Plan | Run starts | Running when the plan is ready (same frame) |
| Running | Executing the plan | Plan ready | Complete at run end |
| Complete | Run over; plan kept for the results screen | Run Manager signals run end (timer, wreck, quit) | Idle on retry or title |

The director never ends a run. If every cell is Done before run end, it stays Running with an empty sky
and raises nothing further; whether an exhausted plan ends the run is the Run Manager's call (Open
Questions).

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
`session-modes.md` needs a revision to define them. The `GameEvents` contract is agreed with Codex (Rule 11).

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
the table's lowest tier. **Heat 5 anchor floor (Rule 3):** for Sequence and Outbreak at H = 5, after the
EF5 adjustment, all EF2 and EF3 mass moves onto EF4, giving EF4 .91 / EF5 .09 for both. EF5 odds are
unchanged.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| H | int | 0–5 | Save & Profile (Cataclysm Heat) | Heat level for the run |
| w0_r | float | 0–1 | data file | Base weight: Quiet .20, Lone Giant .25, Sequence .30, Outbreak .15, Chaos .10 |
| s_r | float | −1 to 1 | data file | Heat slope: Quiet −0.55, Lone +0.05, Sequence +0.10, Outbreak +0.30, Chaos +0.30 |
| p5_0(r) | float | 0–0.3 | data file | Base EF5 share: Lone .10, Sequence .03, Outbreak .03, Quiet 0, Chaos .01 per cell (Chaos also scales by (1 + 0.4H)) |
| Anchor table | table | — | data file | Quiet EF0 .4 / EF1 .4 / EF2 .2; Lone EF3 .55 / EF4 .35 / EF5 .10; Sequence EF2 .30 / EF3 .40 / EF4 .27 / EF5 .03; Outbreak EF2 .30 / EF3 .45 / EF4 .22 / EF5 .03 |
| Chaos cell table | table | — | data file | No anchor; each cell draws independently from EF0 .20 / EF1 .25 / EF2 .27 / EF3 .21 / EF4 .06 / EF5 .01 (Andy, 2026-10-02). EF5 share × (1 + 0.4H), added mass taken from EF0. **At most one EF5 per run:** cells draw in spawn order, and any EF5 drawn after the first becomes EF4 |

**Output Range:** probabilities sum to 1. Heat 0 → Quiet .200, Lone .250, Sequence .300, Outbreak .150,
Chaos .100. Heat 5 → Quiet .007, Lone .165, Sequence .254, Outbreak .345, Chaos .230. P(any EF5 in a run,
Epic, drawn EF before cap resolution) = **4.39 %** at Heat 0 and **13.85 %** at Heat 5, computed exactly:
Σ P_r · p5(r, H), with Chaos contributing P_C · mean over N = 3..8 of 1 − (1 − p5_C)^N. The Heat 5
co-anchor is fixed at EF4 and the anchor floor only moves EF2/EF3 mass, so neither changes these numbers.
"EF5 is rare" holds at low Heat; at Heat 5 a Lone Giant anchor is EF5 30 % of the time, by design.
**Example:** Heat 3 → weights Q .038, L .291, S .405, O .369, C .246 (sum 1.349) → P = Q .028, L .215,
S .300, O .274, C .182. Draw u = 0.47 → Sequence (cumulative .543). Sequence p5 = .03 × 2.2 = .066, so
EF2 drops to .264; second draw 0.90 → EF4 (cumulative .264 / .664 / .934).

**F2. Cell schedule**

The cell schedule formula is defined as:

`T = 180 + 45 · clamp(N_cells, 3, 6)`; anchor peak `t_peak = U(0.40, 0.65) · T`; anchor spawn
`t_a = t_peak − Form(EF_a)`, clamped so the anchor is Mature by `T − 30`. Satellites by regime:

- Quiet: S = 1–3, EF ≤ min(2, EF_a), spawn at `U(0.05T, 0.75T)`. Lone Giant: same, with EF ≤ min(2, EF_a − 2).
- Sequence: S = 2–4, spawn `t_k = t_a − (S − k + 1) · Δ`, `Δ = U(35, 55)` s,
  `EF_k = max(0, EF_top − (S' − k))` with `EF_top = min(EF_a − 1, 3)` and S' the satellites placed (S, or
  S − 1 at Heat 5), so the last satellite is EF_top and each earlier one is one lower; drop any with t < 5 s.
- Outbreak: S = 3–5, EF ≤ min(EF_a − 1, 3), spawn `t_a + U(−15, 15)` within 120 m of the anchor.
- Chaos: N = U{3..8}, spawn at `U(5, 0.8T)`, no anchor.
- **Draws:** every count range (S, N) is a uniform integer draw. Quiet, Lone Giant and Outbreak satellites
  draw EF uniformly from 0..cap, where cap is the regime's limit above. Sequence EF is deterministic (above).
- **Heat 5 co-anchor** (Sequence, Outbreak; Rule 3): one EF4 cell. Sequence: it **replaces satellite S**
  in the last sequence slot, `t_a − Δ`, so the plan keeps S − 1 satellites and N_cells is unchanged.
  Outbreak: it is an extra cell that spawns at `t_a + U(−15, 15)` within 120 m of the anchor and counts
  toward N_cells. It counts toward the caps in both.
- **Compact mode (Rule 12):** T = 180 s fixed (no N_cells term). Δ = U(17, 28) s; Sequence S = 1–3;
  Outbreak S = 1–2; Chaos N = U{3..5}; anchors spawn 60–85 m and satellites 40–85 m from P0, inside the
  arena's ±85 m spawn square (compact Outbreak satellites satisfy both this and "within 120 m of the
  anchor", rerolling per Edge Cases); CapTotal = 2. At Heat 5 the anchor
  and co-anchor fill the cap, so any satellite still live when they spawn ropes out early. Every other rule
  is the same.
- **Compact pacing fill (Andy, 2026-10-05: "real dead moments with nothing to do").** After the regime's own
  schedule, and with draws taken after all of it (so a seed's regime cells and anchor never change):
  - **Opener:** if no cell is scheduled to spawn by 18 s, add one satellite at U(8, 14) s, EF U{0..2}.
  - **Closer:** if no cell is planned to be alive at 160 s, add one satellite EF U{1..3}, timed to be on the
    ground at 160 s (spawn U(160 − Form − Mature, 160 − Form)).
  - Both are capped at the regime's own satellite ceiling (never above what the regime allows), go through
    the cap pass like any satellite, and are flagged `Pacing` in the plan.
  - Measured over 1000 seeds (`AttractSeedProbe.PacingQuietTime`), median: no storm alive 98 → 49 s of 180,
    longest gap 61 → 27 s, wait for the first storm 49 → 11 s, empty tail 37 → 3 s; anchor touchdown unchanged.
  - Knobs: `CompactSettings` `PacingFill`, `OpenerBy`, `OpenerWindow`, `OpenerEf`, `CloserAliveAt`, `CloserEf`.
  - Pacing cells count for bounty eligibility (event-system.md RG "Bounty draw" step 1): an EF3 closer can make
    the EF3+ bounties eligible, so some seeds draw different bounties than before 2026-10-05. Weather on a seed's
    regime cells is unchanged; bounties use their own stream.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| N_cells | int | 2–8 | calculated | Anchor + co-anchor + satellites (Chaos: all cells), as drawn, **before** any t < 5 s or cap drops; clamped to 3–6 inside T. T is fixed once computed |
| T | float | 315–450 s (compact 180 s) | calculated | Epic session length (`session-modes.md` Chase formula); compact mode fixes it at 180 s |
| t_peak | float | 0.40T–0.65T | calculated | When the anchor reaches Mature |
| Form(EF) | float | 20–45 s | data file (F3) | Forming time of the cell's EF |
| SpawnDist (Epic) | float | anchor 350 m to min(700 m, d_reach), satellites 200–600 m | data file | From plan origin P0 (player start); first satellite ≥ 40° from the anchor bearing; ≥ 100 m inside the world edge. d_reach = (Form + Mature) × (15.05 − k_H × v_track) for the anchor's EF (Rule 4): Heat 0 EF0–5 = 603 / 718 / 804 / 812 / 811 / 952 m; Heat 1+ = 565 / 653 / 704 / 658 / 581 / 682 m. Never below 350 m, so the range is never empty |
| SpawnDist (compact) | float | anchor 60–85 m, satellites 40–85 m | data file | Compact mode (pre-2 km world). Spawn square ±85 m (`DisasterSpawner`); tornado edge steer-back at ±90 m. With P0 near the arena centre every bearing is valid, so rerolls don't pull spawns toward the corners |
| CapRing / CapTotal | int | Epic 3 / 5 (Chaos 6); Arcade 3; compact 2 | data file | Concurrency caps |
| CapDelay | float | 10 s steps, 30 s max | data file | Over-cap spawns wait in 10 s steps; a cell that has waited 30 s and still has no slot is dropped (when several reach 30 s on the same step, the lowest-EF one goes first). **Resolved against CapTotal at plan time**, using each cell's Form + Mature + Rope lifetime (a cell that leaves the world early only frees its slot early, which is safe), so the plan never depends on the player. CapRing only decides which cells run full simulation, never when they spawn. **Anchors never wait:** when an anchor or co-anchor would exceed the cap, the lowest-EF live satellite ropes out early instead, so the `T − 30` Mature clamp always holds. An early rope-out starts from the cell's current I (F3). *A cell holds a cap slot from spawn until its rope-out starts (scheduled or early); a roping-out cell no longer counts, so "live cells never exceed the cap" (AC-9, AC-21) is measured on slot holders (clarified 2026-10-04, story 002).* |

**Output Range:** T = 315–450 s (compact 180 s); anchor spawn ≥ 15 s (compact: anchor Mature start
72–117 s); never more cells alive than the cap. The `T − 30` clamp never binds today (0.65T ≤ T − 30 for
every T ≥ 86 s, and anchors never wait); it is a safety net for tuning changes.
**Example:** Sequence, S = 3 → N = 4, T = 360 s. Anchor EF4 (Form 40 s), t_peak = 0.5T = 180 s, spawn
140 s. Δ = 45 s → satellites spawn at 95, 50 and 5 s as EF3, EF2, EF1.

**F3. Storm scale**

The storm scale formula is defined as:

`W(d) = P_EF · I · (1 − d / (R_EF · I))²` for d < R_EF · I, else 0, split into inflow 0.537 · W and swirl
0.843 · W (the existing 7 : 11 ratio). **Guard:** W = 0 when I < 0.01 (no division by R · I ≈ 0). Every
other column is a per-EF lookup.

**Intensity and phase gating.** Forming: I = age / Form; lift and damage are 0. Mature: I = 1. Roping
Out: I = 1 − t_rope / Rope; lift = I · F12(d) with F12 evaluated at radius R · I (so lift and wind
shrink together and there is no lift outside the wind field), and the damage radius is D · I. Damage per
tick is unchanged (owned by `disaster-entity-framework.md`). An EF5 rope-out stops tossing at the axis
once I < 0.305 (0.7 / 2.295), and an EF4 once I < 0.61 (0.7 / 1.1475).
**Early rope-out** (cap eviction, F2): the cell enters Roping Out from its current intensity I0, with
I = I0 · (1 − t_rope / (I0 · Rope)), so there is no jump. A cell evicted while Forming skips Mature,
never raises Peak and **fails to touch down**: its lift and damage radius stay 0 for the whole decline
(only wind scales with I), so a cell that was harmless while Forming never becomes harmful by eviction.

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
| I | float | 0–1 | calculated | Lifecycle intensity: linear 0 → 1 over Form, 1 in Mature, linear 1 → 0 over Rope |
| P_EF, R_EF, D_EF | float | table | data file (`TornadoData` per EF) | Peak wind, wind radius, damage radius |
| Form / Mature / Rope | float | table | data file | Lifecycle seconds. Compact mode: Form × 0.25 (≥ 4 s), Mature and Rope × 0.5 |
| v_track | float | table | data file | Base track speed (F5) |
| ConeScale | float | — | data file | Visual only from now on: funnel width factor ≈ D / 4.3 |

**Output Range:** EF3+ wind exceeds the truck's top speed only near the axis: within ≈ 3 m (EF3), ≈ 14 m
(EF4) and ≈ 29 m (EF5). Vehicle-feel F11 saturates wind force at 1.2 g, so the fleeing guarantee rests on
track speed, not wind speed: tracks stay ≤ 0.4 v_top (≤ 0.5 v_top at Heat 1+, F5), so fleeing is always
possible from outside the core (AC-32). Toss radius: EF3 never
(peak lift 0.574 < 0.7), **EF4 within ≈ 5.7 m** (inside its 6.5 m core; Andy 2026-10-01: keep), **EF5
within ≈ 15.7 m**. **Compact mode allows EF5 at full size** (Andy 2026-10-01): an EF5 covers most of the
≈ 180 m arena, a deliberate survival moment.
**Example (locked checks):** EF3 Mature, truck parked at 15 m → W = 26 × (1 − 15/34)² = 8.1 m/s → wind
accel ≈ 7.6 m/s² → ≈ 3.8 m in 1 s **with no tire grip**, an upper bound. With grip the pre-director scale
(≈ 4.45 m/s at 15 m) moves the truck 0.47 m, so 8.1 m/s should clear 0.5 m with margin; AC-11 verifies
it in PlayMode. Lift at 15 m ≈ 0.008 → not Tossed ✓. EF5 Mature,
5 m → lift = 0.85 × 2.7 × (1 − 5/35)² = 1.69 ≥ 0.7 → Tossed on the first Mature frame ✓.
**Implementation note:** replaces `TornadoController`'s sqrt EF wind scale, which cannot exceed ≈ 2×
EF0 (this table needs 7.75×). Remove `_windScaleBase`, `_windScalePerSqrtEF`, the radius-per-ConeScale
fields and `_baseDamageRadius`; keep `_windInflow` / `_windSwirl` as direction ratios only.

**F4. Forecast estimate**

The forecast formula is defined as:

`σ(d) = 0.7 · g`, `g = clamp01((d − 150) / 650)`;
`EF_shown = clamp(round(EF + clamp(b_i · σ(d), −1, +1)), 0, 5)`;
`ETA_shown = max(0, t_true · (1 + clamp(b_t, −1.5, 1.5) · (0.033 + 0.267 · g)))`, rounded to 5 s.
b_i is clamped to [−3, 3] at draw time.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| d | float | ≥ 0 m | calculated | Player-to-cell distance |
| b_i, b_t | float | N(0, 1); b_i clamped ±3, b_t ±1.5 | seeded, fixed per cell | Bias terms; fixed so the readout never flickers |
| 150 m / 800 m | float | constant | data file | Exact inside 150 m; maximum error at ≥ 800 m |
| t_true | float | ≥ 0 s | calculated | True time until the cell's next phase (peak or rope-out) |

**Hysteresis:** the shown EF changes only after d has moved 10 m past the boundary that flipped it, so the
readout never flickers.
**Output Range:** |EF error| ≤ 1 always; 0 inside 150 m; P(wrong EF) ≈ 47 % at ≥ 800 m. ETA error
±5 % close, up to ±45 % far.
**Example:** EF4 at 500 m → g = 0.538, σ = 0.377, b = −1.4 → error −0.53 → HUD shows **EF3** (corrects to
EF4 near 150 m). True ETA 60 s, b_t = +1.2 → factor 1 + 1.2 × 0.177 = 1.21 → 72.7 s → "peak in ~75 s".

**F5. Track motion**

The track motion formula is defined as:

`dθ/dt = n(t) · ω_EF`, `n = 2 · Perlin(seed_i, 0.25 t) − 1`; initial heading θ0 within ±60° of the
bearing to world centre; `v = v_track · k_H · lerp(0.4, 1, I)`, where k_H = 1.25 at Heat 1+ (rank 1, "F5
Maximum") and 1 otherwise. Late-life jogs are pre-rolled at plan time as a Poisson process with rate
`λ(u) = λ0 · clamp01((u − 0.6) / 0.4)`, u = age / lifetime, where lifetime is the cell's **plan-time**
lifetime after cap resolution (an early rope-out shortens Rope to I0 · Rope; leaving the world early
does not change it, since that isn't known at plan time). Each jog turns `±U(40°, 110°)` eased over
1.5 s with a 3 s speed burst (× 1.5), telegraphed by a 1 s funnel tilt. **Refractory:** no jog starts
within 4.5 s of the previous jog's start (tilt + burst + 0.5 s); pre-rolled events inside that window
are discarded.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| ω_EF | float | 24 / 20 / 16 / 12 / 9 / 7 °/s (EF0–5) | data file | Wander turn rate; big storms are ponderous |
| v_track | float | 3–8 m/s | data file (F3) | Base speed |
| λ0 | float | 1/12 per s | data file | Jog rate at end of life (placeholder) |
| k_H | float | 1 or 1.25 | Cataclysm Heat rank 1 | Track speed multiplier (`economy-progression.md`); replaces "40 % faster with wider suction pull" |
| v_cap | float | 0.7 · v_top = 15.05 m/s | constant | Jog speed cap; an EF4/5 jog at Heat 1+ reaches 8 × 1.25 × 1.5 = 15 m/s |
| Player position | — | — | — | **Never read** (no homing, Core Rule 5) |

**Output Range:** heading unbounded; speed 0.4 v_track to 15.05 m/s during jogs (12 m/s at Heat 0), always
below v_top. Epic: a cell that leaves
the world ends. Compact keeps today's edge steer-back at ±90 m, evaluated inside the same 0.05 s
substeps (Edge Cases, Timing) so it stays frame-rate independent.
**Example:** EF4, ω = 9°/s, noise 0.2 → turns 1.8°/s. At u = 0.75, λ = 0.083 × 0.375 = 0.031/s, about one
jog per 32 s; a +75° jog at 8–12 m/s for 3 s moves the funnel ≈ 30 m off its expected path.

## Edge Cases

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

**Plan and spawning**
- **If a spawn point falls in a blocked or invalid spot** (off the map, inside a POI): reroll the point
  up to 8 times from the same seeded stream, then place it at the nearest valid point along the same
  bearing. The plan stays deterministic. In compact mode the only invalid spot is outside the ±85 m spawn
  square (scattered barns and silos are not POIs; a funnel may form over them).
- **If the player drives far from the plan origin**: cells still spawn where planned. The forecast always
  shows bearing and distance, so a storm behind you is a choice, not a bug (live distance clamp: Open
  Questions).
- **If a planned spawn would exceed the concurrency cap**: resolved at plan time against CapTotal: delay it
  in 10 s steps, and drop the lowest-EF waiting cell after 30 s (F2). **Anchors never wait and are never
  dropped:** if the anchor or Heat 5 co-anchor would exceed the cap, the lowest-EF live satellite ropes
  out early instead.
- **If the run ends before the anchor reaches Mature** (a wreck, or the timer in Arcade): the anchor never
  peaks. The results screen shows "The big one got away" with its EF. That's tension, not an error.
- **If a Chaos run rolls only EF0–1 cells**: allowed. It's a quiet chaos day. Chaos guarantees nothing.
- **If a Chaos run rolls two EF5s**: the second and any later EF5 become EF4 (F1, spawn order), so a Chaos
  run never has more than one EF5.
- **If a Heat 5 run draws Lone Giant, Quiet or Chaos**: no co-anchor. Supercell Convergence only shapes
  Sequence and Outbreak (Rule 3); F1 already pushes Heat 5 toward those regimes.

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
  The arena has no safe zone from wind, but the tossing core is only ≈ 31 m across (15.7 m radius, F3), so survival comes
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
  anchor and co-anchor if not already among them; the rest are summarized as "+N more".

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
- **If the frame rate drops badly**: cell motion integrates in fixed 0.05 s substeps of game time (an
  accumulator), so a cell's position depends only on its age, never on frame rate. A slow frame runs more
  substeps; nothing is dropped, so a replayed seed puts every storm in the same place on any machine
  (Rule 10).

## Dependencies

**Depends on (upstream):**

| Dependency | Type | Interface |
|------------|------|-----------|
| Disaster Entity Framework / Tornado (`disaster-entity-framework.md`; `TornadoController`, `TornadoData`, `TornadoLifecycle`) | Hard | Director spawns cells and hands each its EF row (F3) and track (F5); `TornadoData` gains per-EF scale fields. That GDD already defers spawn choice, placement and timing to this doc; its EF table (wind radius 20–38 m, move speed 4–16 m/s) is the **current** behavior that F3/F5 replace. Back-link: done 2026-10-01 (its Storm Director dependency row, plus supersession notes on its Tornado F5/F6, EF table and Tuning Knobs) |
| Run Manager & Session Modes (`session-modes.md`) | Hard | Run start and end, mode (Epic / Arcade / compact), session length T; Arcade scenario supplies plan parameters |
| Tiled World Streaming (ADR-0004) | Hard for Epic, soft for compact | World bounds; streaming-ring position (full simulation vs far-field) |
| Cataclysm Heat (`economy-progression.md`) | Soft | Heat 0–5 for F1; rank 1 → track speed k_H (F5); rank 5 → co-anchor (Rule 3). Defaults to 0. Back-link: ranks 1 and 5 rewritten in director terms 2026-10-02 |

**Depended on by (downstream):**

| Dependent | Type | Needs from this system | Back-link |
|-----------|------|------------------------|-----------|
| Wind Field / Vehicle Feel (`vehicle-feel.md` F11/F12) | Hard | Per-EF peak wind and radius; lift reads EF strength and radius | Added 2026-10-01; F12 sanity note updated |
| Dynamic Objectives & Events (`event-system.md`) | Hard | Cell tracks and ETA for path projection; EF for event density | Added 2026-10-01 |
| Run Goals v1 (`event-system.md` Run Goals) | Hard | Weather Plan (anchor, non-dropped cells, EFs) for bounty eligibility; `DirectorRng` **stream 3 reserved for bounties**; `StormCellForming` and phase for timed bounties and `ef4_peak` | Added 2026-10-04 |
| Run Manager & Session Modes (`session-modes.md`) | Hard | Weather Plan replaces the Storm Front EF Escalation curve; cell count feeds T | Added 2026-10-01; escalation curve marked superseded |
| Photo Documentation & Scoring (`photo-scoring.md`) | Soft | True EF revealed by a photo (forecast reveal); EF strength in score unchanged | Added 2026-10-02 |
| HUD / Off-Screen Indicators | Soft | Forecast list and bearings | No GDD yet |
| Procedural Audio | Soft | Siren, radio and wind-intensity cues | No GDD yet |
| Save & Profile (`save-profile.md`) | Soft | Seed, build version and regime, optionally stored with run results | Added 2026-10-02 |
| Story / Career mode (`story-career-mode-concept.md`, concept) | Soft | Anchor, phases and regime odds for storm-relative goals; storms are dealt, never authored | Concept already cites the director; no GDD yet |

**Code impact:** replaces `DisasterSpawner`'s timer and roster weights, and `TornadoController`'s sqrt
wind scale, player pull (`_playerPull` → 0) and radius-per-ConeScale fields.

## Tuning Knobs

All values live in a director data asset (Epic and compact profiles) or in `TornadoData` per EF. Safe
ranges are where the formulas' guarantees still hold. Leaving one breaks the guarantee named in the right
column.

| Knob | Default | Safe range | Affects / guarantee at risk |
|------|---------|-----------|-----------------------------|
| Regime base weights w0_r | .20 / .25 / .30 / .15 / .10 | each 0.05–0.5 | Run variety; AC-2 targets must be recomputed if changed |
| Heat slopes s_r | −0.55 / +.05 / +.10 / +.30 / +.30 | −1 to 1 | How Heat reshapes runs; Quiet must stay negative so Heat 5 isn't a breather |
| p5_0 (Lone / Seq / Outbreak / Chaos per cell) | .10 / .03 / .03 / .01 | Lone ≤ .15, others ≤ .05 | EF5 rarity (AC-5); the Heat 5 added mass 2 × p5_0 must not exceed the lowest tier that funds it |
| Peak wind P_EF | 8 / 12 / 17 / 26 / 40 / 62 m/s | strictly increasing; EF3+ > 21.5 | Danger at range; AC-10/11 |
| Wind radius R_EF | 12 / 18 / 26 / 34 / 52 / 70 m | strictly increasing; EF5 ≤ 0.45 × compact arena | Reach of the wind; toss radius via F12 |
| Damage radius D_EF | 1.5 / 2.2 / 3.2 / 5 / 6.5 / 12 m | strictly increasing; ≤ 0.25 R | Core size and visual width |
| Form / Mature / Rope (s) | F3 table | Form ≥ 20 s Epic; (Form + Mature) × (15.05 − 1.25 × v_track) ≥ 350 m | Reachability (Rule 4): d_reach must not fall below the 350 m anchor minimum; and the notice step |
| Track speed v_track | 3–8 m/s | ≤ 0.4 v_top (8.6 m/s) | Fleeing guarantee (AC-32) |
| k_H (Heat rank 1) | 1.25 | 1.0–1.25 | Keeps jog speed ≤ v_cap (8 × 1.25 × 1.5 = 15.0 m/s) |
| λ0 (jog rate) | 1/12 s | 1/20–1/8 s | Late-life loiter danger |
| Jog turn / burst | ±40–110°, × 1.5 for 3 s | burst ≤ × 1.5 | Jog stays below v_cap |
| σ max (forecast EF error) | 0.7 | 0.4–0.9 | P(wrong EF) far away: 47.5 % at 0.7, ≈ 58 % at 0.9; AC-15 must be recomputed if changed |
| Forecast exact / max-error range | 150 / 800 m | exact 100–200 m; max-error ≥ exact + 400 m | Where the forecast becomes trustworthy |
| Caps (CapRing / CapTotal) | Epic 3 / 5 (Chaos 6); Arcade CapTotal 3; compact CapTotal 2 (no ring: all cells run full simulation) | CapTotal ≥ 2 (anchor + co-anchor) | Perf (AC-28/29) vs. Outbreak density |
| Compact T | 180 s | 150–240 s | Compact run length; anchor peak window 0.40–0.65 T |
| Compact multipliers | Form × 0.25 (≥ 4 s), Mature / Rope × 0.5 | Form ≥ 4 s | Compact pacing |
| Environmental-cue full scale | 20 m/s | 15–30 m/s | When the sky reaches maximum dread |
| `StorminessFull` | 6 | 4–10 | How much storm it takes to fully darken the run-wide sky (6 = one EF5 at peak) |
| Storminess rise / fall time constants | 5 s / 20 s | 2–10 s / 10–40 s | How fast the sky closes in and how long it stays dark after a rope-out |
| Rain pool size / curtain cards per cell | 300 / 2 | 100–400 / 1–3 | Rain density vs WebGL fill cost |
| `SirenCycleSeconds` | 25 s | 15–45 s | How long the outdoor sirens wail per warning (EF5 emergency: continuous) |

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
- **Height is independent of EF** (Andy, 2026-10-04): every funnel spans from a cloud base to the ground. The
  cloud base varies per cell (deterministic from the cell, Rule 10) within a band above all play, ≥ 25 m
  (compact ≈ 25–45 m). Weak storms are tall slender ropes; strong ones broaden. No gameplay reads height.
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
- **Failed touchdown (evicted while Forming):** the condensation funnel retracts back into a lightening
  base; no ground contact, no debris, no rope.
- **Jog telegraph (1 s):** the funnel leans ≈ 15° toward the new heading, the base shifts off-centre, and
  a dust puff kicks up on the leading side. The lean always points where the storm will go.

**Environmental cues**
- One normalised value `e = clamp01(W_player / 20 m/s)` (summed per-cell wind magnitude at the player, Rule 7) drives every cue so they rise together: sky
  gradient and sun intensity drop by up to 60 % with a green-teal shift; gust streak density and length;
  grass and tree sway; pooled ambient debris (leaves, paper).
- World-grade only: no grain or CRT on the world as storms rise.

**Run-wide storm sky (Andy, 2026-10-04: "jarring having sunny skies when there's a big tornado")**
- `e` is local: it only rises near a storm, so an EF5 at 200 m stood under a sunny sky. A second,
  run-wide value **storminess** `s` drives the sky as a whole, independent of where the player is:
  `s_target = clamp01( Σ over live cells of (EF_i + 1) · I_i / StorminessFull )`, `StorminessFull` = 6
  (one EF5 at full intensity → 1; an EF2 at peak → 0.5; an EF0 forming at I = 0.3 → 0.05).
  `I_i` is the cell's F3 lifecycle intensity. `s` eases toward `s_target`: rising with a 5 s time
  constant, falling with 20 s, so the sky never snaps back to sunny the moment a cell ropes out.
- `s` drives an **overcast cloud deck** (coverage and thickness, the deck the funnels' cloud bases hang
  from), **sun intensity** and **ambient light**. Sky gradient and sun use `max(e, s)` so the near-storm
  cue still deepens up close. At `s` = 1 the sky is near-black green-teal overhead with light only at the
  horizon; at `s` = 0 it is the baseline sky.
- Deterministic from the plan (Rule 10): `s` reads director state only, never player input.

**Rain (Andy, 2026-10-04: "there isn't much rain in these storms")**
- Presentation only; never blocks a photo or changes scoring in M1. WebGL-safe card language, no VFX Graph
  (needs compute, unavailable on WebGL).
- **Rain curtains:** 1–2 tall scrolling translucent cards hanging under each live cell's cloud base, density
  following the cell's intensity `I`; slanted with the cell's motion. Readable from distance, like a real
  chaser reading the sky.
- **Near-camera rain:** a fixed pool (≤ 300) of streak cards around the camera, density from run-wide
  storminess `s` (none at `s` = 0), slanted by local wind.
- **Lens drops:** droplets on the viewfinder / camcorder only (ADR-0003: retro treatment stays in the lens),
  rising with `s`.
- Budget: ≈ 1–2 draw calls and a few hundred quads; measured against the M1 whole-frame budget.

**Outdoor warning sirens (Andy, 2026-10-04)**
- Real outdoor civil-defense sirens on poles placed around the map: the rising-and-falling wail, positional
  audio, so the player hears them from a direction and louder near a pole.
- Start with the TORNADO WARNING crawl (forming true EF ≥ 3) and run for `SirenCycleSeconds` (≈ 3 min of
  real sirens compressed to 25 s). A forming true EF5 (TORNADO EMERGENCY) runs them continuously until that
  cell ropes out. Never states an EF (Rule 7). The radio/static cue and broadcast tones (X7-10) stay.

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
  and "peak in ~N s" or "roping out". The anchor and the Heat 5 co-anchor always get a row and are
  highlighted. Shares the off-screen indicator's
  bearing logic.
- **News crawl (was radio caption; Andy, 2026-10-04):** a KTVR News lower-third ticker when the siren or
  radio cue plays ("TORNADO WARNING  ·  SEVERE CELL FORMING  ·  BEARING NW"; a forming EF5 escalates to a
  red "TORNADO EMERGENCY"; never an EF number, Rule 7), so the audio is also readable on screen
  (accessibility). Post-M1 idea, not in scope: a 3 s vertical "VIEWER VIDEO" bystander clip on the anchor's
  touchdown (pairs with event-system.md TV News bounties).
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
- **AC-5 [Unit]** GIVEN 10,000 Epic seeds, THEN P(any EF5, drawn before cap resolution) = 4.39 % ±0.6 pp
  at Heat 0 and 13.85 % ±1.1 pp at Heat 5 (≈ 3 standard errors of the exact F1 values).
- **AC-6 [Unit]** GIVEN 10,000 non-Chaos plans, THEN exactly one cell is the anchor, no satellite is EF4+,
  no satellite exceeds the anchor's EF (including Quiet plans with an EF0 or EF1 anchor), and Quiet plans
  contain no EF5. At Heat 5, Sequence and Outbreak plans also contain exactly one EF4 co-anchor and an
  anchor of EF4 or EF5 (never below the co-anchor), and other regimes contain none. Heat 5 Sequence plans
  have S − 1 satellites plus the co-anchor in slot `t_a − Δ`.
- **AC-6b [Unit] (F2 draws)** Every Sequence plan's satellites satisfy `EF_k = max(0, EF_top − (S' − k))`
  (e.g. S = 3, EF_a = 4 → EF1, EF2, EF3). GIVEN 10,000 Outbreak satellites with an EF4 anchor (cap 3), THEN
  each of EF0–EF3 is 25 % ±1.5 pp; GIVEN 10,000 Outbreak plans, THEN each S ∈ 3..5 is 33.3 % ±1.5 pp.
- **AC-7 [Unit]** GIVEN 10,000 Chaos plans at Heat 5, THEN none has more than one EF5 and none has an anchor;
  GIVEN a forced draw sequence of EF5, EF5, THEN the second cell is EF4.

**Schedule (Rule 8, F2)**
- **AC-8 [Unit]** For all Epic plans (compact: AC-21): T ∈ [315, 450] s; the anchor's Mature start ∈ [0.40T, 0.65T] and ≤ T − 30;
  anchor spawn ≥ 15 s; no Sequence satellite spawns before 5 s; in Epic, the anchor's distance from P0 ≤
  (Form + Mature) × (0.7 × 21.5 − k_H × v_track) for its EF and the run's Heat.
- **AC-9 [Unit]** GIVEN an Outbreak plan with S = 5 and CapTotal 5, WHEN the schedule is resolved, THEN live
  cells never exceed the cap, over-cap spawns wait in 10 s steps, and the lowest-EF waiting cell drops
  after 30 s. Across 10,000 plans (Heat 0 and Heat 5) the anchor and co-anchor never wait and are never
  dropped; when one would exceed the cap, the lowest-EF live satellite ropes out early instead, and the
  anchor's Mature start still satisfies AC-8.

**Scale (Rule 6, F3)**
- **AC-10 [Unit]** The table is strictly monotonic in D, R and P; P / 21.5 > 1 for EF3+; base track speed
  ≤ 8.6 m/s for every EF (≤ 10.75 m/s with k_H at Heat 1+); W(15 m) = 8.1 ±0.05 m/s for EF3 and
  38.3 ±0.1 m/s for EF5; W = 0 for I < 0.01 with no NaN or infinity.
- **AC-10b [Unit] (Rule 4, F3)** For an EF5 cell: in Forming, lift and damage radius are 0 at every d; at
  Rope-out I = 0.5, lift at the axis = 0.5 × 2.295 ±0.01, the damage radius = 6.0 m ±0.05, and lift is 0 at
  every d ≥ 0.5 × R × I = 17.5 m; at I = 0.25 the axis lift is < 0.7 (no toss). A cell evicted at Forming
  I0 = 0.4 enters Rope-out at I = 0.4 (no jump), reaches 0 after 0.4 × Rope, and has lift and damage
  radius 0 at every d throughout.
- **AC-11 [PlayMode]** GIVEN a Mature EF3 and the truck parked 15 m away with no input, WHEN 1 s passes,
  THEN horizontal displacement > 0.5 m and `Tossed` is not raised.
- **AC-12 [PlayMode]** GIVEN a Mature EF5 and the truck parked 5 m away, THEN `Tossed` fires within 1 s.
- **AC-13 [PlayMode]** EF4: Tossed within 1 s at 5.0 m (lift 0.75); not Tossed after 2 s at 8.0 m
  (lift 0.55). EF5: the live lift at 17 m is 0.61 (< 0.7). *Clarified 2026-10-04 (story 004): a truck
  parked at 17 m does not stay there; the EF5 inflow drags it into the 15.7 m toss zone within ≈ 2 s, which
  is intended, so the 17 m case checks the lift field, not a parked truck.*

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
- **AC-18 [Unit]** Jogs only start at u ≥ 0.6; each jog's tilt telegraph begins 1.0 s ±0.05 s (one substep)
  before the turn; no two jog starts are < 4.5 s apart; jog speed ≤ 12 m/s at Heat 0 and ≤ 15.05 m/s at Heat 1+.
- **AC-19 [PlayMode]** GIVEN the player inside an EF5's core while it is Forming, THEN no damage is taken
  and the truck is not Tossed.

**Events, compact mode, pause (Rules 7, 11, 12)**
- **AC-20 [PlayMode] (Rule 11)** A cell that lives its full life raises `StormCellForming` →
  `StormCellPeak` → `StormCellRopeOut` → `StormCellEnded`, once each, in order. A cell evicted while
  Forming raises Forming → RopeOut → Ended (no Peak). A Mature cell forced out of the world raises Ended
  only, with no Peak or RopeOut after its exit. Ending the run with 2 cells live raises exactly one Ended
  for each and nothing else. A cell dropped by the cap raises nothing. Every payload carries the cell's
  CellId, EF and Role. The director assembly holds no references to downstream systems.
- **AC-21 [Unit]** Compact mode: a regime is drawn; T = 180 s; anchor Mature start ∈ [72, 117] s; anchors
  spawn 60–85 m and satellites 40–85 m from P0; Form = max(4 s, 0.25 × table); Mature and Rope-out =
  0.5 × table; Sequence S ∈ 1..3, Outbreak S ∈ 1..2, Chaos N ∈ 3..5; live cells never exceed 2.
- **AC-22 [PlayMode]** Pausing for 10 s leaves every cell's age and position unchanged. [WebGL]: same with
  the browser tab hidden for 10 s.
- **AC-23 [PlayMode]** GIVEN seed 777 run once at a forced 5 FPS and once at 60 FPS, THEN every cell's
  position at ages 10, 30 and 60 s matches within 0.01 m (fixed 0.05 s substeps).
- **AC-24 [PlayMode]** (a) A Chaos run with 6 cells live shows the nearest 3 and "+3 more". (b) A
  non-Chaos Epic run with 5 cells live and the anchor 4th nearest shows the nearest 3, the highlighted
  anchor, and "+1 more". (c) A Heat 5 Outbreak run with 5 cells live and the anchor and co-anchor 4th
  and 5th nearest shows 5 rows, both highlighted, and no "+N more". Screenshot of each retained.
- **AC-25 [PlayMode]** GIVEN a run ends before the anchor is Mature, THEN Results shows "The big one got
  away" with its EF.
- **AC-26 [WebGL]** GIVEN an anchor Forming 800 m away in Epic, THEN its far-field cell is visible on
  screen; environmental cues are at maximum when W_player ≥ 20 m/s. Companion [Unit]: with an EF0 5 m
  away and an EF5 Mature 30 m away, `e` = 1 **at every relative bearing of the two cells** (magnitudes
  2.72 + 20.24 m/s; a vector sum could drop to 17.5). Companion [Unit]: the siren caption string for a
  true EF5 contains no EF number. Screenshot retained.

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
| ~~`GameEvents` names for cell Forming / Peak / RopeOut / Ended~~ Resolved 2026-10-02: Codex ack in AGENTS.md; contract and lifecycle exceptions in Rule 11 | — | — |
| Should an exhausted plan (every cell Done before T) end an Epic run early, or leave an empty sky? Quiet runs can go quiet ~80–150 s before T | `session-modes.md` revision | First Epic playtest |
| ~~Back-links from `disaster-entity-framework.md` and `save-profile.md`~~ Resolved: DEF in 3c8254d, save-profile 2026-10-02 | — | — |
| ~~Heat ranks 1 and 5 contradicted Rule 3 and no-homing~~ Resolved 2026-10-02: rank 1 → k_H 1.25, rank 5 → EF4 co-anchor | — | — |
| Does Heat 3 "Blackout" (night) break the far-field read? A near-black EF5 against a black sky | art-director | Before Heat ships |
| Compact-mode EF5 at full size: fair, or just brutal? | Andy, playtest | First director build playtest |
| Story / Career: does a region set its own regime weights or Heat, or use Epic's (Rule 9 has only Epic and Arcade authority)? | game-designer | Story mode GDD |
| Fog-exempt far-field rendering under URP Render Graph (main technical unknown) | technical-artist / Codex | Before the Epic world integrates |
| A compact-mode EF5 wedge can hide the truck: camera pull-back or translucency near the core? | art-director / Camera (S7-05) | First director build playtest |
| EF5 wedge overdraw on WebGL; profile EF5 Mature up close first | technical-artist / Codex (X7-06) | Before EF5 ships |
| Readability at maximum darkening (`e` = 1) with a near-black EF5: check outline contrast | art-director | First EF5 capture |
