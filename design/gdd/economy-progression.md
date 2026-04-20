# Economy & Progression

**Status:** Design — not yet implemented
**Target Sprint:** Sprint 6–7
**Owner:** Game Designer / Economy Designer

---

## Overview

Stormchaser Loop uses a two-layer progression system: meta-progression between runs
(permanent unlocks via Storm Dollars) and per-run variance (temporary pickups and
modifiers). Score = currency. The loop is: photograph → earn → upgrade → photograph better.

---

## Player Fantasy

"My telephoto lens got me a perfect EF5 shot I couldn't have taken last week.
I earned that shot."

---

## Detailed Rules

### Currency: Storm Dollars
- Photo score converts directly to Storm Dollars at 1:1
- Earned per run, banked permanently
- No run penalty on death — partial score (80%) is banked regardless

### Meta-Progression: Storm Chaser HQ
Accessed between runs. Three upgrade trees:

#### Tree 1 — Vehicles
| Unlock | Cost | Effect |
|--------|------|--------|
| Pickup Truck (default) | Free | Balanced speed/handling |
| Rally Truck | 500 | +30% speed, -20% debris resistance |
| Storm Rig | 1200 | +50% debris resistance, -15% speed |
| Barn Runner | 2500 | +40% gap clearance, unique gap animations |

#### Tree 2 — Cameras
| Unlock | Cost | Effect |
|--------|------|--------|
| Standard Lens (default) | Free | Aim sweet spot: 15–25 units |
| Wide Angle | 300 | Aim sweet spot: 10–35 units (easier framing, -10% score ceiling) |
| Telephoto | 800 | Aim sweet spot: 25–50 units (hard to frame, +30% score ceiling) |
| Prototype Lens | 3000 | No sweet spot penalty — full score at any distance |

#### Tree 3 — Scanner Upgrades
| Unlock | Cost | Effect |
|--------|------|--------|
| EF Detector | 400 | See EF rating before engaging tornado |
| Path Predictor | 900 | See projected tornado path on minimap |
| Event Scanner | 1500 | Preview incoming event type 5s earlier |
| Storm Radio | 600 | Weather alert activates 10s earlier |

### Permanent Perks (flat unlocks, no tree)
- **Storm Veteran** (1000): Start each run with Daredevil multiplier pre-loaded
- **Photojournalist** (750): Each run's best photo is saved to Photo Album
- **Survivor** (500): Vehicle destroyed → one free repair per run

---

### Per-Run Variance: Roadside Pickups
Spawned on terrain during Chase and Sprint runs:

| Pickup | Effect | Duration |
|--------|--------|----------|
| Camera Film | +1 photo charge (if limited shots implemented) | Instant |
| Fuel Canister | +20% speed for 15 seconds | Timed |
| Tripod | Next photo +0.2 AimScore | One photo |
| Press Pass | Next photo scores at +1 EF tier | One photo |

### Per-Run Variance: Gas Stations
- Appear on terrain, driveable location
- Stop inside for 3 seconds → receive one random perk card from the pool
- Perk cards are run-only (reset each session)
- Examples: "Golden Hour" (+25% all scores), "Adrenaline" (+15% vehicle speed), "Scoop" (next photo auto-aims)

---

## Formulas

### Storm Dollar Earning Rate
`StormDollars = FinalSessionScore × 1.0`
- On death: `StormDollars = FinalSessionScore × 0.8`

### Camera Aim Score Modifier
`ModifiedAimScore = BaseAimScore × CameraMultiplier`
- Applied before ScoringSystem.CalculatePhotoScore

### Upgrade Cost Curve
Costs follow roughly 2.5× multiplier per tier to prevent early saturation.

---

## Edge Cases

- Player maxes all upgrades: add prestige system (cosmetic resets, no gameplay change)
- Run ends before reaching gas station: no perk card earned (working as intended)
- Two "Press Pass" pickups in same run: second overrides first (don't stack)

---

## Dependencies

- ScoringSystem.cs — camera modifiers feed in as multipliers
- SessionTimer.cs — run end triggers Storm Dollar banking
- ScoreAccumulator.cs — final score feeds currency
- TornadoController.cs + terrain — pickup spawn positions
- UI: Storm Chaser HQ screen (Phase 5)
- Photo Album (Phase 6)

---

## Tuning Knobs

- `StormDollarRate`: default 1.0
- `DeathScoreRetention`: default 0.8
- All upgrade costs in ScriptableObject table
- Perk card pool in ScriptableObject list (easy to add/remove cards)

---

## Acceptance Criteria

- [ ] Storm Dollars accumulate correctly across multiple runs
- [ ] Camera upgrades measurably affect scoring output
- [ ] Vehicle upgrades produce distinct driving feel differences
- [ ] Gas station perk cards are random and non-stackable
- [ ] No upgrade makes the game trivially easy at max level
- [ ] Prestige option exists once all upgrades purchased
