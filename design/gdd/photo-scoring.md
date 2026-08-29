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
Dot product of the truck's forward vector and the direction to the tornado.
- 1.0 = tornado dead ahead
- 0.0 = tornado 90° to the side
- Negative values treated as 0.0 (behind player = no score)

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
- `DisasterEntity.cs` (S3-A1) — provides nearest entity query
- `TornadoData.cs` — EFStrength value per tornado type
- `event-system.md` — `DaredevilMultiplier` and `StuntAirMultiplier` chain onto
  `PhotoScore` from this doc (see that doc's Formulas section)
- `economy-progression.md` — Camera Lens unlocks override `OptimalDistance`/`DistanceSpread`
  and introduce `CameraAimMultiplier` (see "Lens-Modified Scoring" below)

---

## Tuning Knobs

| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `AimWeight` | 0.6 | 0.4–0.8 | How much framing matters vs. distance |
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
