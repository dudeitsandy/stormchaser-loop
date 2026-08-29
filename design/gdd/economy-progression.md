# Economy, Meta-Progression & Cataclysm Heat

**Status:** Design — not yet implemented  
**Target Sprint:** Sprint 6–7  
**Owner:** Game Designer / Economy Designer  

---

## Overview

Doomsday uses a multi-layer progression loop:
1. **In-Run Variance**: Roadside pickups, gas station perk cards, and dynamic bounties.
2. **Meta-Progression (HQ Garage)**: Score converts 1:1 into **Storm Dollars** to permanently unlock vehicle archetypes, specialized camera lenses, radar scanners, and deployable payloads.
3. **Endgame Mastery (Cataclysm Heat)**: Customizable hazard modifiers (*Hades* style) for hardcore players pushing high-score leaderboards.
4. **The Trophy Scrapbook**: Dynamic "Front Page" retro newspaper covers generated after every run.

---

## Player Fantasy

*"I took my custom Armored Rig with an Anchor Harpoon and a Wide-Angle lens into a Heat 5 Supercell, pinned myself 8 units from an EF5, and made the front page of the Morning Tribune."*

---

## Detailed Rules

### Currency: Storm Dollars
- Final run score converts directly to Storm Dollars at 1:1.
- Earned per run, banked permanently.
- No penalty on vehicle destruction — 80% of earned score is banked ("the film survived").

---

## Meta-Progression: Storm Chaser HQ Garage

Accessed between runs. Four upgrade trees + permanent perks:

### Tree 1 — Vehicles & Chassis
Unlock names match the archetype table in `vision-1.0.md` 1:1. Costs for the three
archetypes marked NEW below are proposed, not balance-tested — flag for review.

| Unlock | Cost | Archetype / Effect |
|--------|------|--------------------|
| **Pickup Truck** (default) | Free | Balanced speed, handling, and moderate armor. |
| **Rally Truck / Buggy** (= Scout Buggy) | 500 | +30% speed, higher jump airtime, -20% debris resistance. |
| **Motorcycle** *(NEW — cost proposed)* | 700 | +60% speed, no armor, best-in-class trick multiplier, one-hit destruction. |
| **Storm Rig** ("The Tank") (= Armored SUV / Rig) | 1200 | +50% debris resistance, unblock ramming bonus, -15% speed. |
| **Monster Truck** *(NEW — cost proposed)* | 1800 | Terrain-type immunity (mud/rubble/water hazards), crushing debris bonus, high jump arc, -10% top speed. |
| **Barn Runner / Interceptor** (= Sports Car / Interceptor) | 2500 | +40% gap clearance, huge drift multipliers, high top speed. |
| **APC** *(NEW — cost proposed)* | 3500 | Multi-NPC carry capacity for Rescue objectives, best-in-class armor, lowest top speed. |
| **Hover Prototype** (= Prototype) | 5000 | Water/mud terrain immunity, experimental jump thrusters. |

### Tree 2 — Camera Lenses
| Unlock | Cost | Effect |
|--------|------|--------|
| **Standard Lens** (default) | Free | Optimal aim sweet spot: 15–25 units. |
| **Wide Angle / Fisheye** | 300 | Sweet spot: 10–35 units (easier close-up framing, -10% score ceiling). |
| **Telephoto Lens** | 800 | Sweet spot: 25–50 units (long-range safe sniping, +30% score ceiling). |
| **Prototype Sensor** | 3000 | No distance penalty — maximum score at any distance. |

### Tree 3 — Scanner & Radar Upgrades
| Unlock | Cost | Effect |
|--------|------|--------|
| **EF Detector** | 400 | Displays EF threat tier before engaging tornado. |
| **Path Predictor** | 900 | Displays projected disaster trajectory line on minimap. |
| **Event Scanner** | 1500 | Previews incoming procedural event types 5 seconds earlier. |
| **Storm Radio** | 600 | Weather alerts and TV bounties activate 10s earlier. |

### Tree 4 — Payloads & Deployable Gadgets
| Unlock | Cost | Effect |
|--------|------|--------|
| **Nitrous Boost** | 600 | 3-second +40% acceleration surge (2 charges per run). |
| **Anchor Harpoon** | 1000 | Fires spike into ground: locks vehicle in place with 100% wind suction immunity. |
| **Dorothy Sensor Launcher** | 800 | Automatically ejects weather probe pods into funnel path for +300 pts. |
| **EMP Deflector Shield** | 1400 | Absorbs 1 lightning strike or hazard shockwave per run. |

### Permanent Perks (Flat Unlocks)
- **Storm Veteran** (1000): Start each run with Daredevil multiplier pre-loaded.
- **Photojournalist** (750): Every run generates a viewable Front Page cover by default, but it
  is not saved anywhere — without this perk, it's shown once on the end screen and discarded.
  This perk auto-saves it to the permanent Newspaper Album instead.
- **Survivor** (500): Vehicle destruction grants one free instant field repair per run.

---

## Per-Run Variance: Roadside Pickups & Gas Stations

### Roadside Pickups (Field Drops)
| Pickup | Effect | Duration |
|--------|--------|----------|
| **Nitrous Refill** | +1 Boost charge | Instant |
| **Fuel Canister** | +20% speed for 15 seconds | Timed |
| **Gyro Stabilizer** | Next photo +0.2 AimScore | One photo |
| **Press Pass** | Next photo scores at +1 EF tier | One photo |
| **Repair Kit** | Restores 1 vehicle HP | Instant |

### Gas Stations (Mid-Run Pitstops)
- Drive through a gas station bay and stop for 2 seconds to draw a random **Run Perk Card**:
  - *"Golden Hour"*: +25% all photo scores for remainder of session.
  - *"Adrenaline Surge"*: Each NPC rescued adds +5 seconds to timer.
  - *"Heavy Bumper"*: Ramming obstacles deals zero chassis self-damage.

---

## Cataclysm Heat Level System (Endgame Mastery)

After completing a standard Chase run, players unlock **Cataclysm Heat** selection before launching a run. Heat ranks are cumulative — activating rank N means ranks 1 through N are all in effect simultaneously, and their Storm Dollar bonuses stack additively (e.g. Heat 3 active = ranks 1+2+3 effects all apply, for a combined **+45% Storm Dollars**):

| Heat Rank | Modifier Name | Effect |
|:---:|:---|:---|
| **1** | **F5 Maximum** | Disasters move 40% faster with wider suction pull. |
| **2** | **Dry Lightning** | Wildfires spawn continuously on storm perimeters. |
| **3** | **Blackout** | Full night mode: zero ambient light, headlights + lightning only. |
| **4** | **Brittle Chassis** | Debris and collision impacts deal 2× damage. |
| **5** | **Supercell Convergence**| Two simultaneous high-threat disasters active at all times. |

---

## "Front Page" Newspaper Album Generator

At the end of every run, the game generates a mock **retro newspaper or magazine cover**:
- **Masthead**: *"THE DAILY TRIBUNE"* / *"EXTREME WEATHER QUARTERLY"*
- **Headline**: Procedurally generated based on top event (e.g., *"DAREDEVIL DRIVER JUMPS BURNING BARN IN RECORD EF5"*).
- **Photo Feature**: The run's highest-scoring pixelated snapshot with film grain filter.
- **Footer**: Score grade (*"PERFECT SNAP - 1,450 PTS"*), date, and player vehicle name.
- **Export**: One-button export to Switch gallery / Steam screenshots / PNG.

---

## Formulas

### Storm Dollar Earning Rate
`StormDollars = FinalSessionScore × (1.0 + (HeatLevel × 0.15))`
- `FinalSessionScore`: the run's total score from `ScoreAccumulator.cs`, ~10 to several
  thousand depending on run length and lens/style multipliers (see `photo-scoring.md`).
- `HeatLevel`: 0–5, the highest active Cataclysm Heat rank (ranks stack, so this is the
  count of active ranks, not a single toggle — see Cataclysm Heat section above).
- On vehicle destruction: `StormDollars = (FinalSessionScore × 0.8) × (1.0 + (HeatLevel × 0.15))`
- **Example:** FinalSessionScore = 2000, Heat 3 active (ranks 1+2+3 = +45%):
  `2000 × 1.45 = 2900 Storm Dollars`. Same run ending in destruction: `2000 × 0.8 × 1.45 = 2320`.

### Modified Aim Score
`ModifiedAimScore = Mathf.Clamp01(BaseAimScore × CameraAimMultiplier)`
- `BaseAimScore`: 0.0–1.0, from `photo-scoring.md`'s existing AimScore calculation — unchanged.
- `CameraAimMultiplier`: 1.0 with no lens equipped (Standard Lens); lens-specific value TBD
  per lens once integrated — **this is a new hook point that does not exist in
  `PhotoTrigger.cs`/`ScoringSystem.cs` yet.** See `photo-scoring.md`'s "Lens-Modified Scoring"
  section for how this and the per-lens `OptimalDistance`/`DistanceSpread` overrides combine.
- **Example:** BaseAimScore = 0.9, Telephoto Lens equipped (CameraAimMultiplier 1.0, since
  Telephoto changes distance sweet spot, not aim): `Clamp01(0.9 × 1.0) = 0.9` (unchanged from
  base — Telephoto's effect is entirely on DistanceScore, not AimScore).

---

## Edge Cases

- **Insufficient Storm Dollars for a garage purchase**: purchase button is disabled/grayed out;
  no partial purchases or debt.
- **Cataclysm Heat rank stacking**: ranks are cumulative, not exclusive — activating Heat 3
  means ranks 1, 2, and 3 are all in effect (see Cataclysm Heat section). A player cannot
  activate rank 3 without ranks 1–2 also being active.
- **Mid-run purchases**: garage upgrades only apply between runs — a run in progress always
  uses the loadout selected at Pre-run, even if the player could afford an upgrade mid-run.
- **Photojournalist perk not owned**: Front Page cover still generates and displays on the
  end screen, but is not saved to the Newspaper Album — it's lost once the player continues.
- **Vehicle destroyed with zero score**: 80% of zero is zero — the "film survived" discount
  has no effect on already-zero runs; no negative Storm Dollars are possible.

---

## Dependencies

- `ScoreAccumulator.cs` (S3-03) — provides `FinalSessionScore` for the Storm Dollar formula.
- `PhotoTrigger.cs` / `ScoringSystem.cs` (S3-02, S2-06) — `CameraAimMultiplier` and per-lens
  `OptimalDistance`/`DistanceSpread` overrides are a **new integration point that doesn't exist
  yet**; see `photo-scoring.md`'s "Lens-Modified Scoring" section (cross-referenced there).
- `event-system.md` — the Dorothy Sensor Launcher payload (Tree 4) must be equipped before
  the Sensor Deployment event can trigger; see that doc's Edge Cases.
- `vehicle-damage.md` — Repair Kit, Survivor perk, Heavy Bumper, Anchor Harpoon/Clamps, and
  EMP Deflector Shield all hook into that doc's HP/stage model; see its Dependencies section.
- **New system required**: a save/profile persistence layer. Nothing in the current codebase
  saves data across sessions — Storm Dollars, garage unlocks, and the Newspaper Album all
  require this to exist before Sprint 6-7 implementation can start. Not yet scoped anywhere.
- `vision-1.0.md`'s Vehicle Archetypes table — Tree 1 unlock names must stay in sync with it
  (see that table's note).

---

## Tuning Knobs

| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `StormDollarConversionRate` | 1.0 | 0.5–2.0 | Overall progression pacing |
| `DestructionPenalty` | 0.8 (80% kept) | 0.5–1.0 | How punishing vehicle loss feels |
| `HeatBonusPerRank` | 0.15 (+15%) | 0.05–0.25 | Reward for playing at higher difficulty |
| Garage unlock costs | 500–5000 (see Tree 1–4 tables) | — | Meta-progression pacing; tune per-unlock |
| Gas Station pitstop duration | 2s | 1–4s | Risk/reward of stopping mid-run |

---

## Acceptance Criteria

- [ ] Storm Dollars accumulate reliably across runs and save to profile
- [ ] Garage upgrades alter vehicle weight, speed, and lens sweet spots
- [ ] Deployable payloads (Nitrous, Anchor Harpoon) trigger on assigned hotkeys
- [ ] Active Cataclysm Heat ranks apply intended gameplay penalties and bonus multipliers
- [ ] End-of-session screen renders the dynamic "Front Page" newspaper graphic with correct photo and stats
