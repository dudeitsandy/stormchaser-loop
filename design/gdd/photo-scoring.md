# Photo Scoring System

**Status:** Partially implemented (ScoringSystem.cs) — PhotoTrigger pending S3-02
**Target Sprint:** Sprint 3 (S3-02)
**Owner:** Game Designer

---

## Overview

The player photographs tornadoes by pressing a button. Score is calculated from
three inputs: how well the tornado is framed (AimScore), how close to optimal
distance the player is (DistanceScore), and the tornado's EF strength multiplier.
Higher-risk shots (closer, better-framed, stronger tornado) always yield higher scores.

---

## Player Fantasy

"I had one second to line it up perfectly. I floored it to get inside 20 units,
held the wheel straight, and pressed the button at the exact moment the EF5 filled
the frame."

---

## Detailed Rules

- Player presses L2 (gamepad) or Space (keyboard) to photograph
- Only the nearest active disaster entity is scored
- One photo per button press — no hold/burst
- Photos can be taken at any time during the session

### AimScore
Dot product of the **camera's** forward vector (flattened to the ground plane) and the direction to
the tornado. *Changed 2026-10-01 by `vehicle-feel.md` Core Rule 8: aim follows the chase camera /
Storm Cam, not the truck, so drifting and airborne shots can be framed. Until the Vehicle Feel pass
ships, the 0.4 implementation still uses truck-forward.*
**Framing curve** (decided 2026-10-01, ships with the Vehicle Feel pass):
`AimScore = clamp01(1 − θ / AimHalfAngle)`, where θ = angle between camera forward (flattened) and the
direction to the tornado; `AimHalfAngle` = 15°.
- 1.0 = tornado dead center
- 0.5 = 7.5° off center
- 0.0 = 15° or more off center
Why: with aim following the camera, the old dot product scored ≥ 0.87 for anything on screen (and
PERFECT only needed ~41° accuracy), so aim stopped being a skill and Storm Cam's offset framing
(`vehicle-feel.md` F14) had no effect. *0.4 still uses `max(0, dot(truckForward, dirToTornado))`
until the Vehicle Feel pass replaces it.*

### DistanceScore
Bell curve centered on optimal distance (20 units).
- Peak (1.0) at exactly 20 units
- Falls to 0.0 at ≤5 units (too close — framing lost) and ≥50 units (too far)

---

## Formulas

### Photo Score
`PhotoScore = (AimScore × 0.6 + DistanceScore × 0.4) × EFStrength × 100`

**Variables:**
- `AimScore`: 0.0–1.0
- `DistanceScore`: 0.0–1.0
- `EFStrength`: 1.0 (EF0) to 4.0 (EF5), linear

**Example calculations:**
- Perfect EF5 shot (aim 1.0, distance 1.0, EF5): `(0.6 + 0.4) × 4.0 × 100 = 400`
- Solid EF3 shot (aim 0.8, distance 0.7, EF3): `(0.48 + 0.28) × 2.5 × 100 = 190`
- Glancing EF0 shot (aim 0.3, distance 0.2, EF0): `(0.18 + 0.08) × 1.0 × 100 = 26`

**Score range:** ~10 (worst possible) to 400 (perfect EF5) — Season 1 baseline, before any
lens ceiling modifiers land (see "Lens-Modified Scoring" below)

### DistanceScore Bell Curve
`DistanceScore = Mathf.Exp(-Mathf.Pow(distance - OptimalDistance, 2) / (2 × Spread²))`
- `OptimalDistance`: 20 units
- `Spread`: 10 units (controls width of bell curve)

---

## Edge Cases

- No active tornado when button pressed: no score, no feedback (silent no-op)
- Player inside tornado (distance < 5): DistanceScore = 0.0; AimScore still valid
- Tornado despawning mid-photograph: score calculated against last known position
- Multiple tornadoes active: only nearest is scored; others ignored

---

## Dependencies

- `ScoringSystem.cs` (S2-06) — `CalculatePhotoScore()` already implemented
- `PhotoTrigger.cs` (S3-02) — computes AimScore + DistanceScore, calls ScoringSystem
- `ScoreAccumulator.cs` (S3-03) — receives score from PhotoTrigger and accumulates
- `DisasterEntity.cs` (S3-A1) — provides the live `Active` registry; the nearest-entity search itself is `PhotoTrigger.FindNearest` (see `disaster-entity-framework.md`)
- `TornadoData.cs` — EFStrength value per tornado type
- `event-system.md` — `DaredevilMultiplier` and `StuntAirMultiplier` chain onto
  `PhotoScore` from this doc (see that doc's Formulas section)
- `economy-progression.md` — Camera Lens unlocks override `OptimalDistance`/`DistanceSpread`
  and introduce `CameraAimMultiplier` (see "Lens-Modified Scoring" below)
- `storm-director.md` — owns each cell's EF; a photo of a cell reveals its true EF in the HUD forecast
  (F4). The EFStrength used in scoring is unchanged. Added 2026-10-02.

---

## Tuning Knobs

| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `AimWeight` | 0.6 | 0.4–0.8 | How much framing matters vs. distance |
| `AimHalfAngle` | 15° | 10–25° | Angle at which AimScore reaches 0; lower = PERFECT demands precise centering (added 2026-10-01) |
| `DistanceWeight` | 0.4 | 0.2–0.6 | How much distance matters vs. framing |
| `OptimalDistance` | 20 units | 10–35 | Ideal photo range |
| `DistanceSpread` | 10 units | 5–20 | Forgiveness of distance scoring |
| `ScoreScale` | 100 | 50–200 | Overall score magnitude |

---

## Acceptance Criteria

- [ ] Pressing Space/L2 near a tornado logs a score to Console
- [ ] Perfect aim + optimal distance + EF5 yields exactly 400 points
- [ ] Score decreases measurably as player moves away from optimal distance
- [ ] Score decreases measurably as player turns away from tornado
- [ ] No score event fires when no tornado is active
- [ ] EF5 shot always outscores identical-quality EF0 shot

---

## Film, Repeat Shots, and Wind Bonus (Sprint 5, 2026-10-01)
**Status:** Implemented in `PhotoTrigger.cs`, `RepeatPenalty.cs`, `ScoringSystem.WindMultiplier`.

**Why:** with a 0.35s cooldown and full value per shot, parking at 20 units from an EF5 and
mashing the shutter scored ~100,000 in a 90s run. Every shot needs to be a decision.

### Rules
- **Film:** 24 frames per run. Every shutter press uses a frame, including misses (no subject in
  range). At 0 film the shutter does nothing except raise `OutOfFilm`.
- **Repeat decay:** each subject accumulates "heat" +1 per shot; heat drains at 1 per
  `RepeatRecoverySeconds`. Multiplier = `RepeatDecay ^ heat`. Different tornadoes are independent.
- **In The Wind:** shooting from inside a disaster's wind field multiplies the shot, scaling with
  wind speed at the camera. This is the risk/reward link to the vortex wind field.
- **Tier** (PERFECT / GOOD / GLANCING) still comes from framing quality alone.

### Formulas
`FinalPhotoScore = PhotoScore × RepeatMultiplier × WindMultiplier`
- `RepeatMultiplier = RepeatDecay ^ max(0, heat − Δt / RepeatRecoverySeconds)`
- `WindMultiplier = 1 + WindBonusMax × clamp01(WindSpeed / WindBonusFullAt)`
- Event-system multipliers (`DaredevilMultiplier`, `StuntAirMultiplier`) chain on top when events ship.
  "In The Wind" is deliberately a different name from event-system.md's Daredevil (structure gap).

**Examples:** mashing one EF5 24 times at 0.35s intervals totals ≈2.2 full shots instead of 24.
A PERFECT EF5 shot (400) taken in full wind is 800; a PERFECT EF0 in full wind is 200.

### Tuning Knobs
| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `FilmPerRun` | 24 | 12–36 | Shots per run; lower = each shot weightier |
| `RepeatDecay` | 0.5 | 0.3–0.8 | Penalty per rapid repeat of same subject |
| `RepeatRecoverySeconds` | 4s | 2–8s | How fast a subject becomes "fresh" again |
| `WindBonusFullAt` | 15 u/s | 8–25 | Wind speed for full bonus |
| `WindBonusMax` | 1.0 | 0.5–2.0 | Max extra multiplier (1.0 = up to 2×) |

### Acceptance Criteria
- [x] Film counter starts at 24 each run; every press decrements; 0 film = no score (unit + PlayMode tests)
- [x] Immediate repeat of the same tornado scores < 75% of the first (PlayMode smoke test)
- [x] 24 mashed shots of one subject total < 3 full shots (`RepeatPenaltyTests`)
- [x] Wind multiplier is 1.0 outside wind, 2.0 at/above `WindBonusFullAt` (`WindMultiplierTests`)

---

## Multi-Entity Composition (Future Extension — Season 2+)

The single-entity scoring model is correct for Season 1. When multiple DisasterEntities
are active simultaneously (Season 2+), photographing more than one entity in a single
frame upgrades to composition scoring.

### Design Intent
A kaiju caught in a tornado is worth more than either entity alone — the risk is higher,
the moment is rarer, and the image is more dramatic. Composition scoring rewards the
player for positioning that captures an interaction moment, not just proximity to a
single threat.

### Extension Points
- `PhotoTrigger` (S3-02): build to score nearest entity only. Future: scan all
  DisasterEntities within camera frustum and pass the full list to ScoringSystem.
- `ScoringSystem.CalculatePhotoScore`: single `efStrength` becomes `combinedThreatLevel`
  derived from all entities in frame. Style multiplier slots in as a final factor.
- `DisasterEntity`: will need an `IsInteracting` flag or event when two entities make
  contact — triggers the interaction bonus.

### Intended Formula (not yet implemented)
`PhotoScore = (AimScore × 0.6 + DistanceScore × 0.4) × CombinedThreat × StyleMultiplier × 100`

- `CombinedThreat`: sum of ThreatClass values for all entities in frame
  (EF5 tornado + Class V kaiju = higher ceiling than either alone)
- `StyleMultiplier`: 1.0 baseline; increases with stunt state, near-misses, active
  entity interactions. This is the Style axis from the 4-axis scoring vision.

### Implementation Trigger
Build when: (a) a second disaster type ships, AND (b) the Style scoring axis lands
(planned Sprint 6–7). Do not build before both conditions are met.

---

## Lens-Modified Scoring (Future Extension — Sprint 6-7)

`economy-progression.md`'s Camera Lens tree (Tree 2) changes what counts as a good shot.
This doc's Season 1 formula treats `OptimalDistance` (20 units) and `DistanceSpread` (10
units) as fixed constants — they become per-equipped-lens values instead:

| Lens | OptimalDistance | DistanceSpread | Score Ceiling |
|------|-----------------|-----------------|---------------|
| Standard (default) | 20 | 10 | ×1.0 (baseline 400 max) |
| Wide Angle / Fisheye | ~22 (center of 10–35 range) | ~13 (wider bell) | ×0.9 |
| Telephoto | ~37 (center of 25–50 range) | ~13 | ×1.3 |
| Prototype Sensor | n/a — no distance penalty | n/a | DistanceScore fixed at 1.0 |

### Design Intent
A closer lens rewards aggressive positioning; a longer lens trades score ceiling for safety.
`CameraAimMultiplier` (from `economy-progression.md`'s Modified Aim Score formula) is
separate from this table — it defaults to 1.0 for all current lenses since none of them
change aim difficulty, only distance framing. A future lens could introduce a non-1.0 value.

### Extension Points
- `DistanceScore` formula's `OptimalDistance`/`Spread` become read from the equipped lens's
  data (a new `LensData` ScriptableObject, not yet created) instead of hardcoded constants.
- Final formula becomes:
  `PhotoScore = (AimScore × 0.6 + DistanceScore × 0.4) × EFStrength × 100 × LensScoreCeiling`
- Prototype Sensor is a special case: `DistanceScore` is forced to 1.0 regardless of actual
  distance, rather than using a bell curve with an enormous spread.

### Implementation Trigger
Build when the Economy system (Sprint 6-7) ships Tree 2. Do not build before then — Season 1
ships with Standard Lens only, no lens selection UI needed yet.
