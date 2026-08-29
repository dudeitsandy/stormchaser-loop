# Procedural Event System & In-Run Objectives

**Status:** Design — not yet implemented  
**Target Sprint:** Sprint 5–6  
**Owner:** Game Designer  

---

## Overview

As disasters move across the map, the event system procedurally generates time-critical event windows, stunt opportunities, and route interventions in their path. Events are situational — the player chooses to engage or ignore each one. Engaging is always riskier and always more rewarding. Events create the chaotic "Twister / Action Movie" moments that turn runs into unforgettable stories.

---

## Player Fantasy

*"I had half a second to decide: go through the barn at 80 MPH, launch off the billboard ramp over the funnel, or slam into the fallen semi to clear the road for the convoy. I did all three."*

---

## Detailed Rules

### Event Generation
- Events spawn dynamically in the disaster's projected path (2–4 seconds ahead of current position) or at static POIs.
- Event density scales with Threat Class / EF rating: EF0 = rare, EF5 = frequent.
- Maximum 2 active events simultaneously.
- Events expire if not engaged within their time window (8–12 seconds).

---

## Event Taxonomy

### 1. Stunt Events

#### Structure Gap (Barn / Warehouse Punch)
- A structure spawns between player and tornado with a driveable corridor.
- Gap width: 1.5× vehicle width (tight but passable at speed).
- **Reward:** High-speed shortcut + "Daredevil" score multiplier (1.5×) on next photo.
- **Risk:** Collision damage if misaligned; falling debris.
- **Visual:** Light beam / dust shaft illuminates the entrance.

#### Twister Jump Ramp
- A collapsed billboard, dirt mound, or buckled highway bridge creates a ramp aligned with the disaster path.
- **Reward:** Massive airtime, +2.0× "Airborne Snapshot" multiplier if photographed mid-flight.
- **Risk:** Landing in rough terrain or directly inside the vortex damage zone.

#### Drift Framing Zone
**Implementation Trigger — do not build before this is met:** requires the Vehicle Feel pass
(momentum physics + drift) planned for Sprint 6-7 (`vision-1.0.md` Pillar 1). This event's
own doc targets Sprint 5-6, one sprint *before* drift exists — treat this specific stunt as
deferred to whenever the Vehicle Feel pass actually lands, even if the rest of this doc ships
on schedule.
- A tight 90° asphalt or gravel bend directly perpendicular to the disaster.
- **Reward:** "Drift Style" multiplier based on continuous slide duration with tornado in frame.
- **Risk:** Spin-out or loss of momentum into obstacles.

---

### 2. Intervention & Route Clearing Events

#### Ram & Unblock (Roadblock Clearance)
- A fallen tree, boulder, or stalled semi-truck blocks a primary highway artery.
- Player rams the blockage at speed (speed ≥ 50 MPH or heavy armor chassis).
- **Reward:** Instant +200 Intervention score + clears the high-speed escape lane for NPC traffic.
- **Risk:** High chassis impact damage if attempted at low speed with a light vehicle.

#### NPC Rescue & Escort
- A civilian vehicle is stalled or pinned in the direct path of the cataclysm.
- Player intercepts by driving within 15 units or honking to guide them.
- **Reward:** Flat bonus score + "Saved" badge on run summary (+5s time extension with Adrenaline perk).
- **Risk:** Intercept path takes player directly into the vortex suction/debris field.

#### Sensor Deployment ("The Dorothy Pod")
- A green holographic target beacon spawns 3 seconds ahead of the projected vortex trajectory.
- Player must drive through the beacon at speed and trigger the sensor launcher payload.
- **Reward:** +300 Intervention score + live telemetry on tornado path for the rest of the run.
- **Risk:** Point-blank exposure to front-line wind turbulence.

#### Bridge / Bottleneck Trap
- Narrow passage with tornado visible directly on the other side.
- **Reward:** Guaranteed sub-10-unit photo distance (maximum distance score).
- **Risk:** If disaster shifts path, player is trapped at point-blank range.

---

### 3. TV News Network Bounties ("Breaking News Demands")

At run start, the News Network assigns 1–3 procedural photo bounties:
- **"Airborne Subject"**: Photograph an airborne car, cow, or roof section in the funnel (+250 pts).
- **"Point-Blank EF5"**: Photograph an EF5 within 12 units (+500 pts).
- **"Dual Cataclysm"**: Capture two interacting disaster centers in one frame (+1000 pts).
- **"Hero of the Day"**: Complete 3 NPC rescues in a single run (+400 pts).

---

## Formulas

### Event Spawn Probability (per 10 seconds)
`SpawnChance = BaseChance + (ThreatClass × 0.08)`
- EF0 / Class I: 10% / 10s
- EF3 / Class III: 34% / 10s
- EF5 / Class V: 50% / 10s

### Ramming Impact Threshold
`ClearSuccess = (VehicleMass × VehicleSpeed) >= ObstacleMassThreshold`
- If successful: Obstacle fractures into debris; player retains 70% momentum.
- If failed: Vehicle takes 1 HP damage; comes to an immediate dead stop.

### Daredevil Multiplier Stacking
`FinalPhotoScore = BasePhotoScore × DaredevilMultiplier × StuntAirMultiplier`
- Stacks with EF strength: EF5 base (400) × Daredevil (1.5) × Jump (2.0) = **1,200 pts**.

---

## Edge Cases

- **A 3rd event would spawn while 2 are already active**: the new event does not spawn and is
  not queued — it's simply skipped. The next spawn check (per `BaseEventSpawnChance`) may
  produce a new event once a slot frees up.
- **Player doesn't have the required payload equipped** (e.g. Sensor Deployment without the
  Dorothy Sensor Launcher from `economy-progression.md` Tree 4): the event does not spawn at
  all — payload-gated event types are excluded from the spawn pool until unlocked and equipped.
- **Event's disaster despawns or merges mid-window**: the event expires immediately, as if its
  time window ran out — no partial credit.
- **Two Ram & Unblock events target the same obstacle**: cannot occur — obstacles are
  consumed (destroyed) by the first successful clear, removing them as an event target.

## Dependencies

- `DisasterEntity.cs` / `TornadoController.cs` — path projection for event placement.
- `VehicleData.cs` / `PlayerVehicle.cs` — collision damage, mass, and payload triggers.
- `SessionTimer.cs` — event window timing and expiration.
- `ScoringSystem.cs` — multiplier chaining and Intervention score tallying (`photo-scoring.md`
  lists this doc's `DaredevilMultiplier`/`StuntAirMultiplier` in its own Dependencies).
- `AudioSystem` — event stings, siren warnings, impact crunches.
- `economy-progression.md` — payload-gated events (Sensor Deployment) require Tree 4 unlocks.
- `vehicle-damage.md` — the Ramming Impact Threshold formula and `VehicleMaxHP` tuning knob
  below are implemented by that doc's HP/stage model, not redefined here. `PlayerVehicle.cs`
  still needs the health field that doc specifies before Ram & Unblock or stunt-collision
  mechanics can be built.

---

## Tuning Knobs

| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `BaseEventSpawnChance` | 0.10 | 0.05–0.25 | Frequency of procedural events |
| `EFSpawnModifier` | 0.08 | 0.04–0.15 | Escalation rate per threat tier |
| `EventExpiryWindow` | 10s | 6–15s | Time player has to engage event |
| `DaredevilMultiplier` | 1.5× | 1.2–2.0× | Score reward for structure gaps |
| `JumpAirMultiplier` | 2.0× | 1.5–3.0× | Score reward for mid-air photos |
| `VehicleMaxHP` | 3 hits | 1–5 hits | Survival buffer before run destruction |

---

## Acceptance Criteria

- [ ] Events spawn in projected disaster trajectory within correct time window
- [ ] Stunt ramps grant airborne photo multiplier when snap occurs mid-air
- [ ] Barn corridors detect clean passage vs. collision
- [ ] Ramming obstacles clears roads and awards Intervention score
- [ ] TV News Bounties display on HUD and complete dynamically on trigger
- [ ] All events remain strictly opt-in (player can ignore and focus purely on driving/photos)
