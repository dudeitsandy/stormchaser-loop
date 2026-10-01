# Vehicle Feel

> **Status**: Approved (design-review 2026-10-01: NEEDS REVISION → all 15 findings revised; AimScore + ram decisions by Andy)
> **Author**: Andy Styx + Claude
> **Last Updated**: 2026-10-01
> **Implements Pillar**: 1 — Kinetic Chaos (vision-1.0.md)
> **Index**: Off-index (no systems-index.md yet; vision-1.0.md serves as concept)

## Overview

Vehicle Feel is the driving model for every player vehicle: a physics body riding on per-wheel
raycast suspension, with an arcade grip model tuned for expression over realism. The player drives
with throttle, brake/reverse, and steering, plus four verbs: a **handbrake** that breaks rear grip
into a controlled powerslide, a short **jump**, a refilling **boost**, and **air control** to pitch,
roll, and yaw while airborne. Because the truck is a real physics body, it responds to everything the
game throws at it: uneven and destructible terrain loads the suspension, ramps and wreckage launch it,
collisions deliver impacts whose force decides damage, and disaster wind pushes on the body as an
actual force rather than a velocity override. The system exists because movement is the game's first
pillar (Kinetic Chaos): chasing a storm should feel physical and expressive, and drift, airtime, and
near-misses are scorable moments in their own right. It replaces the current arcade direct-velocity
placeholder, and its stats implement the Speed / Armor / Trick ratings of every vehicle archetype.

*Jump note:* every vehicle gets a small hop; the Prototype archetype's "jump jets" (vision-1.0)
become an enhanced jump (sustained or double), preserving its uniqueness.

## Player Fantasy

*"I'm a stunt driver with a camera, and the storm is my co-star."*

On the ground, the truck has **mass**: it squats when you floor it, dives when you brake, leans into
corners, and every landing hits the suspension with a satisfying crunch. That weight is what makes the
storm frightening — when the inflow catches you, you feel a two-ton vehicle being dragged sideways,
and wrestling it back is physical. But when you **commit**, the truck answers like Rocket League:
yank the handbrake and it snaps into a controlled slide; hit a ramp and you get real airtime, with
enough air control to line up a shot mid-flight and stick the landing. The fantasy is competence under
chaos — you are never fully in control, but you are always *driving*, and your best moments (a
powerslide that swings the funnel into frame, a jump over a debris field into a PERFECT shot) feel
earned and a little ridiculous.

**Feel target:** *weighty-agile* — weight on the ground, agility on commitment (decided 2026-10-01).

**References, and what we take from each:**
- **Rocket League** — responsive commitment verbs (handbrake, jump, air control); readable physics that
  make every moment self-authored.
- **Forza Horizon / Burnout** — weight transfer and body roll as visible feedback; crunchy crashes with
  camera kick.
- **Twister (1996)** — the truck as a fragile boat in a hostile sea of wind and debris.

**Engagement type:** a system players love engaging with, not invisible infrastructure. Pillar 1:
*"Movement has Rocket League / Burnout energy: momentum-based, physics-forward, trick-capable… motion
is expressive, tactile, and scorable. This is a draw, not a gimmick."*

**Archetype spread:** the default Pickup sits at weighty-agile. Sports Car / Motorcycle push toward
floaty and twitchy (more air control, less grip stability); Monster Truck / APC push toward heavy and
unstoppable (more mass, terrain immunity, slower to commit).

## Detailed Design

### Core Rules

**1. Body & suspension**
- One `Rigidbody`. Mass and center of mass (CoM, offset low for stability) come from the archetype.
- Four wheel anchors. Each physics step, each wheel sphere-casts down (radius ~0.15 m, so it doesn't
  fall into terrain cracks) over `RestLength + WheelRadius + Travel`.
- A grounded wheel applies a **spring-damper** force along the contact normal:
  spring ∝ compression, damper ∝ compression velocity, never pulling down (clamped ≥ 0).
- Wheel visuals follow the contact point, so suspension is visible.

**2. Drive & grip** (per grounded wheel, applied at its contact point)
- **Throttle:** engine force split across drive wheels, shaped by a torque curve that falls off toward
  top speed.
- **Brake:** force opposing motion (F2b); holding brake below 1 m/s engages reverse (capped at
  `ReverseSpeed`). With no throttle or brake, rolling resistance coasts the truck down (F2b).
- **Grip (combined friction circle):** each wheel's longitudinal (drive/brake) and lateral forces share
  one budget, `√(F_long² + F_lat²) ≤ μ · load` (F3). Exceeding it is how slides, wheelspin, and
  power-oversteer happen.
- **Steering:** front-wheel angle scales down with speed ("calmer at speed").
- **Handbrake:** rear lateral grip × ~0.3, rear drive × 0.5, plus a small yaw-assist torque. On
  release, rear grip ramps back over ~0.25 s for a clean exit.
- **Arcade assists** (always on, tunable): speed-proportional downforce while grounded, anti-roll
  torque, and yaw stability that resists unintended spins unless the player is steering into the
  rotation or holding the handbrake.

**3. Air**
- **Airborne** = zero wheels grounded for > 0.1 s (grace period so bumps aren't airtime).
- **Air control** (Rocket League convention): left stick = pitch + yaw; handbrake held + stick X = roll.
  Rotation rate is capped.
- Gravity is normal — airtime comes from launches, not floatiness (weighty-agile target).
- **Auto-right:** Upended (up · worldUp < 0.3) for 1.2 s with angular speed < 1.5 rad/s → righting flip
  onto wheels. Linear speed is ignored, so wind can't prevent recovery (E5).
- **Keyboard air control:** W/S = pitch, A/D = yaw, Left Ctrl + A/D = roll.

**4. Jump**
- Requires ≥ 2 wheels grounded. Upward impulse blended between vehicle-up and world-up.
  Cooldown 0.8 s. No air jump (Prototype archetype excepted — enhanced jump jets).

**5. Boost**
- Meter 0–100. Holding boost drains it and applies forward force that fades to zero at
  `BoostMaxSpeed` (F9); works airborne (air-boost recovery). Boost is the only way the Pickup reaches
  Ram & Unblock speed (50 MPH) — a deliberate route-clearing use (decided 2026-10-01).
- **Refills from style**: per second of sliding, per second of airtime, per near-miss — plus a slow
  passive trickle. Cannot start below 5.

**6. Impacts**
- On any collision, impact speed = relative velocity along the contact normal.
- **≥ light threshold → 1 HP; ≥ severe threshold → 2 HP** (the hit sizes `vehicle-damage.md` defines).
  Below light: cosmetic only (shake, sound, dust).
- **Landings** are impacts on vertical speed, with thresholds × 1.5 (suspension absorbs). Clean
  landings never hurt.
- Existing post-hit invulnerability (1.5 s) still applies. Tornado funnel contact damage unchanged.
- **Event obstacles are exempt from F10.** Hitting a Ram & Unblock obstacle resolves only through
  `event-system.md`'s Ramming Impact Threshold (success: obstacle shatters, truck keeps 70 % momentum,
  no damage; failure: 1 HP + dead stop). Collisions report `ImpactKind` (World / Destructible /
  EventObstacle) so perks like Heavy Bumper can filter ramming.

**7. Wind as a force**
- The wind field is the **air's velocity**. Horizontal force =
  `WindDrag × WindExposure(archetype) × (windVel − bodyVel)`, clamped. Applied slightly above CoM so
  the truck **leans** away from gusts. Existing grip loss in strong wind is kept.
- **Lift:** within a fraction of an **EF3+** tornado's wind radius, wind adds upward force. Below the
  toss threshold this only unloads the suspension — wheels go light and grip drops (via F3's `load`).
- **Toss:** when lift first crosses `TossThreshold × weight`, apply a one-shot **toss impulse** (F12b:
  upward + along the swirl) — the lift force alone (< 1 g) cannot leave the ground. The truck becomes
  **Tossed** (airborne, air control × 0.5). Re-toss is blocked until the truck has landed.

**8. Camera & aim** *(changes `photo-scoring.md` AimScore — update on approval)*
- Right stick / mouse **orbits** the chase camera; on release it recenters behind the direction of
  travel, or behind the truck's facing when speed < 3 m/s (no flipping when reversing/stopped).
- **Storm Cam** (toggle): camera tracks the nearest disaster within the shutter's 60 m range (Rocket
  League ball-cam analogue); the truck drives freely while the funnel stays in view. It frames with a
  **drifting offset** (F14, up to ±8°), so the player still nudges the shot to center for PERFECT.
  Storm Cam keeps the storm in view; it doesn't take the photo for you (decided 2026-10-01).
- **Viewfinder and photo AimScore use the camera's forward**, not the truck's.

**9. Damage-stage hooks** (implements `vehicle-damage.md`)
- **Damaged:** max steer angle × 0.75. **Critical:** momentum-only — no throttle, boost, or jump;
  steering and handbrake still work.

**10. Input map** *(changes current bindings)*

| Verb | Gamepad | Keyboard / Mouse |
|------|---------|------------------|
| Throttle / Brake-Reverse | RT / LT | W / S |
| Steer | Left stick | A / D |
| Handbrake | X | Left Ctrl |
| Jump | A | Space |
| Boost | B | Left Shift |
| **Shutter** | **RB** (was L2) | **Left mouse** (was Space) |
| Camera orbit | Right stick | Mouse |
| Storm Cam | Y | Middle mouse / Tab |

Shutter moves because LT becomes brake and Space becomes jump; on mouse, aim with the mouse and click
to shoot.

### States and Transitions

| State | Meaning | Enter when | Exit when |
|-------|---------|------------|-----------|
| **Grounded** | Normal driving | ≥ 2 wheels grounded | Handbrake / slip / leaving ground |
| **Sliding** | Powerslide / drift | Grounded, speed > 3 m/s, and (handbrake held or slip angle > 20°) | (Slip < 10° and handbrake released) or speed < 2 m/s (+0.25 s grip recovery) |
| **Airborne** | Player-launched air | 0 wheels grounded > 0.1 s | Any wheel grounds → landing impact check → Grounded |
| **Tossed** | Wind-launched air | Wind lift > toss threshold | As Airborne; air control × 0.5 while Tossed |
| **Upended** | On roof / side | Up · worldUp < 0.3 and angular speed < 1.5 rad/s | Auto-right after 1.2 s |
| **Disabled** | Critical damage stage | `vehicle-damage.md` Critical | HP restored (perk) or Wrecked |
| **Wrecked** | 0 HP | `vehicle-damage.md` Destroyed | Run ends (existing flow) |

Sliding / Airborne / Tossed / Upended are mutually exclusive; **Disabled** overlays any of them.
**Exactly one wheel grounded** (tipping, two-wheel stunts): hysteresis — keep the previous state.

### Interactions with Other Systems

| System | Flows in | Flows out |
|--------|----------|-----------|
| `photo-scoring.md` / PhotoTrigger | — | Camera-forward aim direction; state flags (Sliding, Airborne, Tossed) for future style multipliers |
| `event-system.md` | — | Slide duration (Drift Framing Zone); Airborne flag + airtime (Twister Jump Ramp ×2.0) |
| `vehicle-damage.md` | Current damage stage (steer × 0.75 Damaged; momentum-only Critical) | Impact events with severity (1 or 2 HP) |
| Wind field (`DisasterEntity`) | Wind velocity at body position; **new lift query** per disaster | — |
| Terrain (ADR-0004) | Surface type per wheel contact → grip/drag multipliers (2D's mud 0.4× / highway 1.5× return as surface properties) | Impact force against destructibles (decides breakage) |
| `GameEvents` | — | **New:** `StyleEvent` (drift / airtime / near-miss + amount), `Landed` (impact speed), `Impact` (severity), `Tossed` |
| HUD (Claude lane) | — | Boost meter value |
| Presentation (Codex lane) | — | `CurrentWind`, state flags, and the new events → wind/landing/drift VFX, tire smoke, sparks |
| Style scoring (future system) | — | Consumes `StyleEvent`. This system **emits** style moments; it does not score them. |

## Formulas

Units: m, s, kg; g = 9.81. Values are starting defaults exposed as Tuning Knobs; the *shape* of each
formula is the design. Pickup (Speed 3 / Armor 3 / Trick 2) used for examples.

**F1. Suspension spring-damper** (per wheel)
`F_susp = max(0, k·x + c·ẋ)` — `x` compression ∈ [0, Travel], `ẋ` compression velocity.
`k = (M·g/4) / (SagFraction · Travel)` (rest sag = `SagFraction` = 0.35 of travel);
`c = 2·ζ·√(k·M/4)`, damping ratio `ζ` = 0.45 (slightly bouncy so landings read).
Pickup: M = 2100, Travel = 0.35 → k ≈ 42,000 N/m, c ≈ 4,300 N·s/m.

**F2. Drive force**
`F_drive = M · A_engine · throttle · (1 − (v / v_top)^2.5)`, split across drive wheels.
`A_engine = 1.28 · v_top / AccelTime` → ≈ 90 % of top speed in `AccelTime`
(∫₀^0.9 du / (1 − u^2.5) ≈ 1.28). Drive force is part of each wheel's friction-circle budget (F3), so
launches can spin the wheels.

**F2b. Brake, coast, reverse**
Brake `F_brake = M · BrakeDecel` (14 m/s²) opposing velocity, within the friction budget.
Coast `F_roll = M · CoastDecel` (2.5 m/s²) with no throttle or brake.
Reverse below 1 m/s with brake held: drive force reversed, speed capped at `ReverseSpeed` = 0.35 · v_top.

**F3. Grip — combined friction circle** (per wheel)
Desired lateral force `F_lat* = −v_lat · GripStiffness · load`, also clamped to the force that would
cancel the wheel's lateral velocity in one step (`(M/4) · |v_lat| / Δt`) to prevent explicit-force
jitter at 50 Hz.
Budget `B = μ · load · SurfaceGrip · HB · WindGripMul`. If `√(F_long² + F_lat*²) > B`, scale both down
proportionally — that shortfall is wheelspin / power-oversteer.
- `GripStiffness` in s/m: lateral force saturates at `v_lat = μ / GripStiffness` (default 1.5 s/m →
  saturates at ~0.7 m/s of side-slip: firm, but slides are reachable).
- `load` = this wheel's `F_susp` — unloaded wheels lose grip, so bumps and jumps break traction.
- `HB` = `HandbrakeGrip` (≈ 0.3) on rear wheels while handbrake held, else 1.
- `WindGripMul = 1 / (1 + |w| / WindGripLossAt)` (carried over from the 0.4 implementation).
- `SurfaceGrip` from terrain: ADR-0004 SurfaceTag table (Asphalt 1.0 … Mud 0.45); untagged = Grass 0.75.

**F4. Steering angle**
`δ = δ_max · steer · lerp(1, HighSpeedSteerFactor, v / v_top) · DamageSteerMul`
`δ_max` = 32°, `HighSpeedSteerFactor` = 0.45, `DamageSteerMul` = 0.75 when Damaged else 1.

**F5. Slip angle** (drives Sliding state)
`β = atan2(|v_lat|, |v_long|)`; enter Sliding at β > 20°, exit at β < 10°.

**F6. Downforce** (grounded only)
`F_down = M · g · DownforceCoeff · (v / v_top)²`; DownforceCoeff = 0.25 → +0.25 g at top speed.

**F7. Air control**
Angular acceleration `α = AirAccel · TrickScale · input`, applied as acceleration (independent of mass
and inertia, so long/heavy archetypes rotate comparably); AirAccel = 20 rad/s²; angular speed capped at
`AirMaxRate` ≈ 4.5 rad/s (~260°/s).

**F8. Jump**
Impulse `J = M · v_jump`; `v_jump` = 5.5 m/s → apex `v²/2g` ≈ **1.5 m** (clears fences/debris, no float).

**F9. Boost**
`F_boost = M · A_boost · (1 − (v / BoostMaxSpeed)²)`, A_boost = 9 m/s², `BoostMaxSpeed = 1.35 · v_top`
(Pickup 29 m/s ≈ 65 MPH, clears the 50 MPH ram requirement); drain 33/s (full meter ≈ 3 s).
Refill: +18/s Sliding, +14/s Airborne, +25 per near-miss (all × `StyleRefillScale`), +4/s passive.

**F10. Impact severity**
Collision `s = |v_rel · n|`; landing `s = |v_y| / 1.5`.
`HP_loss = 2 if s ≥ Severe; 1 if s ≥ Light; else 0`. Pickup: Light 12.5 m/s, Severe 22.5 m/s.
Sanity: an ~8 m drop lands at 12.5 m/s → /1.5 = 8.3 → no damage (clean landings never hurt);
head-on at ≥ 12.5 m/s (~45 km/h) → 1 HP; full speed ≥ 22.5 m/s → 2 HP.

**F11. Wind force**
`F_wind = M · WindResponse · Exposure · (w − v_h)`, capped at 1.2 · M · g, applied 0.4 m above CoM.
`WindResponse` = 1.1 /s → horizontal velocity relaxes toward air velocity with time constant
≈ 0.9 s / Exposure. Replaces the 0.4 direct velocity offset; heavier archetypes resist naturally.

**F12. Wind lift** (EF3+ only)
`L = M · g · Exposure · LiftScale · Intensity · (1 − d / (0.5 · R))²` for `d < 0.5 · R`
(R = tornado wind radius, d = horizontal distance to funnel).
`LiftScale = max(0, (EFStrength − 2.0) · 0.8)` → EF3 0.4, EF4 0.8, EF5 1.6.
**Tossed when `L ≥ 0.7 · M · g`.** Pickup sanity (Exposure 0.85): EF3 peaks at 0.34 g (wheels light,
grip loss, never tossed); EF4 peaks at 0.68 g (never quite tossed — wheels nearly off the ground);
**EF5 (R = 38 m → lift zone 19 m) tosses within ≈ 5.4 m — just outside its 4.3 m damage radius**:
flung right before you'd be hit.

**F12b. Toss impulse** (once on crossing the toss threshold; blocked until landed)
`Δv = up · TossUpSpeed · Exposure + tangent · 0.5 · |w_swirl|`, TossUpSpeed = 8 m/s.
Pickup rises at ≈ 6.8 m/s → apex ≈ 2.4 m, spinning with the funnel. Landing from that (≈ 6.8 m/s
vertical) is far under the landing damage threshold: the toss itself is free; what you land on is not. Higher-Exposure archetypes (Motorcycle 1.3) get tossed by EF4s too.

**F14. Storm Cam offset framing**
Camera aim point = funnel position rotated about the camera by
`θ_off = clamp(StormCamLag · (v_perp / v_top) + StormCamSway · sin(0.7 t), ±StormCamMaxOffset)`
where `v_perp` = truck velocity perpendicular to the camera→funnel line (crossing the funnel fast pushes
it off-center). StormCamLag = 10°, StormCamSway = 3°, StormCamMaxOffset = 8°.
Paired with `photo-scoring.md`'s framing-curve AimScore (`clamp01(1 − θ / 15°)`): the full 8° offset
leaves AimScore at 0.47, so an uncorrected Storm Cam shot lands GOOD, and centering it earns PERFECT.

**F13. Archetype star mapping** (stars 1–5; Armor ✗ = 0)

| Stat | Drives | Formula | Pickup (3/3/2) |
|------|--------|---------|----------------|
| Speed `s` | Top speed, accel | `v_top = 14 + 2.5s` m/s; `AccelTime = 4.2 − 0.4s` s | 21.5 m/s (77 km/h); 3.0 s |
| Armor `a` | Mass, wind, crash thresholds | `M = 1200 + 300a`; `Exposure = 1.3 − 0.15a`; `Light = 8 + 1.5a`; `Severe = 1.8 · Light` | 2100 kg; 0.85; 12.5 / 22.5 m/s |
| Trick `t` | Air, slide, style refill | `TrickScale = 0.6 + 0.2t`; `HandbrakeGrip = 0.45 − 0.04t`; `StyleRefillScale = 0.7 + 0.15t` | 1.0; 0.37; 1.0 |

Extremes: **Motorcycle (5/✗/5)** — 26.5 m/s, 1200 kg, exposure 1.3, Light 8 m/s: fast, wind-blown,
fragile (fits its 1 HP). **Monster Truck (2/5/3)** — 2700 kg, exposure 0.55, Light 15.5 m/s: shrugs
off wind and crashes.

## Edge Cases

| # | Situation | Resolution |
|---|-----------|------------|
| E1 | **Donut farming** — handbrake spins in place to refill boost | Slide refill only counts at speed > 6 m/s |
| E2 | **Jump farming** — jump spam on flat ground ≈ continuous airtime → infinite boost | Airtime refill/style only counts while **> 1.8 m above ground** (above the 1.5 m jump apex). Ramps, wreckage, tosses count; bunny-hops don't |
| E3 | **Near-miss farming** — orbiting just outside a damage radius | Near-miss cooldown 3 s per tornado; requires speed > 8 m/s |
| E4 | Toss → hard landing → damage | Intended risk; normal landing rules; post-hit invulnerability prevents stacking |
| E5 | Upended in wind — linear speed never drops below 2 m/s, auto-right never fires | Auto-right condition: upended 1.2 s **and** angular speed < 1.5 rad/s (linear speed ignored) |
| E6 | Tunneling at boost/toss speeds | Continuous-dynamic collision detection on the vehicle body |
| E7 | Low framerate (Steam Deck 30 fps, WebGL hitches) | Fixed 50 Hz physics, independent of render rate; max catch-up 0.1 s per frame |
| E8 | Flung out of the world | Soft boundary push-back + hard clamp; below y = −10, respawn at last grounded position (no HP cost) |
| E9 | Wall-driving via suspension | Wheel grounded only if contact normal is < 60° from up. No Rocket League wall-riding |
| E10 | Critical damage stage while airborne | Air control allowed (rotation, not propulsion); throttle/boost/jump stay disabled |
| E11 | Storm Cam with no disaster in range | Falls back to free orbit while staying toggled; re-locks when one enters range; switching targets requires the new one to be ≥ 25 % closer (no flicker) |
| E12 | Multiple impacts in one physics step | Apply only the single highest severity |
| E13 | Hitting light destructibles (fences, signs) | Severity scaled by `min(1, m_other / (0.5 · M))`; static geometry = full mass. Plowing through fences never hurts; silos do. *(Masses per ADR-0004 §5)* |
| E14 | Jump input while Upended or Disabled | Ignored (needs ≥ 2 grounded wheels and not Critical) |
| E15 | Photo with camera looking backward | Allowed — aim is camera-forward and the viewfinder shows what will be scored; shooting over your shoulder while fleeing is valid |

## Dependencies

**This system depends on:**

| Dependency | Type | Interface |
|------------|------|-----------|
| Wind field (`DisasterEntity.TotalWindAt`, `WindField`) | Hard | Wind velocity at a position; **new** per-disaster lift query (F12) |
| `vehicle-damage.md` / `VehicleHealth` | Hard | Current damage stage → steer multiplier, momentum-only mode |
| Vehicle archetype data (`VehicleData`, `vision-1.0.md` stars) | Hard | Speed/Armor/Trick stars → F13 parameters (`VehicleData` gains star fields) |
| Input (`StormChaserControls`) | Hard | New actions: Handbrake, Jump, Boost, CameraOrbit, StormCam; Shutter rebound (RB / left mouse) |
| Camera rig (Cinemachine `FollowCam`) | Hard | Orbit offset + Storm Cam target; existing `DisasterProximityFov` (FOV + wind shake) carries over |
| Terrain (ADR-0004, Proposed 2026-10-01) | Soft | Surface type per contact → `SurfaceGrip`/drag; destructible piece mass (E13). Works on flat ground without it |

**Depended on by:**

| Dependent | Type | Needs | Back-link status |
|-----------|------|-------|------------------|
| `photo-scoring.md` / `PhotoTrigger` | Hard | Camera-forward aim direction | AimScore redefined camera-forward (2026-10-01) |
| `vehicle-damage.md` | Hard | Impact events with severity (F10) | Added (2026-10-01) |
| `event-system.md` | Soft | Slide duration; Airborne flag + airtime | Added — Drift Framing Zone now satisfiable (2026-10-01) |
| `economy-progression.md` Tree 1 | Soft | Archetype stars → physics (F13) | Added (2026-10-01) |
| HUD (Claude lane) | Soft | Boost meter value | — |
| Presentation (Codex lane): viewfinder, VFX, audio | Soft | Camera orientation for PiP; state flags + new events for tire smoke, sparks, landing dust, engine/skid audio | Codex request posted in AGENTS.md: PiP follows camera, not truck |
| Style scoring (future system) | Soft | `StyleEvent` stream | — |

**Code impact (Sprint 7):** `PlayerVehicle.cs` is rewritten; `RunLoopSmokeTests` must be updated
(they teleport the truck and rely on 0.4 knockback behavior).

## Tuning Knobs

All live on `VehicleData` (per archetype) or a shared vehicle-feel config. Defaults = Pickup.

| Knob | Default | Safe range | Too high → | Too low → |
|------|---------|-----------|------------|-----------|
| **Suspension** | | | | |
| `SagFraction` | 0.35 | 0.2–0.5 | Wallowy, bottoms out | Stiff, jittery |
| `DampingRatio` (ζ) | 0.45 | 0.3–0.8 | Dead landings | Pogo-sticking |
| `Travel` | 0.35 m | 0.2–0.6 | Monster-truck float | Harsh, no visible suspension |
| **Drive & grip** | | | | |
| `GripStiffness` | 1.5 s/m | 0.5–3 | On rails, no slides | Ice skating |
| `μ` (grip cap) | 1.1 | 0.8–1.6 | Can't drift without handbrake | Slides every turn |
| `δ_max` | 32° | 24–40° | Twitchy at low speed | Boat-like turning |
| `HighSpeedSteerFactor` | 0.45 | 0.3–0.8 | Unstable at top speed | Can't corner fast |
| `DownforceCoeff` | 0.25 (× g at top speed) | 0–0.6 | Glued down, ramps lose air | Floaty, rolls easily |
| `BrakeDecel` / `CoastDecel` | 14 / 2.5 m/s² | 10–20 / 1–5 | Stoppies, nose-dives / truck stops dead | Can't stop / coasts forever |
| `AntiRoll` / `YawStability` | 0.6 / 0.5 | 0–1 | Feels assisted/scripted | Spin-outs, rollovers |
| **Handbrake** | | | | |
| `HandbrakeGrip` (base, Trick-scaled per F13) | 0.45 | 0.2–0.6 | Barely slides | Uncontrollable spin |
| `GripRecoveryTime` | 0.25 s | 0.1–0.6 | Sluggish exits | Twitchy exits |
| **Air & jump** | | | | |
| `AirAccel` | 20 rad/s² | 10–35 | Spins like a top | Can't correct landings |
| `AirMaxRate` | 4.5 rad/s | 3–7 | Uncontrolled flips | Sluggish air |
| `v_jump` | 5.5 m/s | 4–7.5 | Floaty cartoon hops | Useless hop |
| `JumpCooldown` | 0.8 s | 0.5–1.5 | Unresponsive | Bunny-hop spam |
| **Boost** | | | | |
| `A_boost` | 9 m/s² | 5–14 | Boost dominates driving | Unnoticeable |
| `BoostMaxSpeed` | 1.35 × v_top | 1.15–1.6 | Tunneling risk, world crossed in seconds | Can't reach ram speed |
| `BoostDrain` / `PassiveRegen` | 33 / 4 per s | 20–50 / 0–10 | Boost scarce / always full | Endless / style-only |
| Style refills (slide / air / near-miss) | 18/s / 14/s / 25 | ±50 % | Boost always full | Style doesn't feed speed |
| **Impacts** | | | | |
| `Light` / `Severe` (F13 coefficients) | `8 + 1.5a` / ×1.8 | ±30 % | Invincible bumper car | Every bump costs HP |
| `LandingThresholdMul` | 1.5 | 1.2–2.5 | Landings never hurt | Jumps punishing |
| **Wind** | | | | |
| `WindResponse` | 1.1 /s | 0.5–2 | Tumbleweed | Wind cosmetic again |
| `WindForceCap` | 1.2 g | 0.6–2 | EF0s ragdoll you | EF5 ≈ EF0 |
| `LiftScale` coefficient | 0.8 | 0.4–1.2 | EF3 tosses | EF5 can't toss |
| `TossThreshold` | 0.7 g | 0.5–1.0 | Toss rare / only inside damage radius | Constant tossing near EF4+ |
| `TossUpSpeed` | 8 m/s | 5–12 | Moon launches, landing damage | Toss barely leaves ground |
| **Camera** | | | | |
| `OrbitRecenterTime` | 0.6 s | 0.2–1.5 | Camera lags | Fights player orbit |
| `StormCamSwitchRatio` | 0.75 | 0.6–0.9 | Sticky lock on far target | Target flicker |
| `StormCamMaxOffset` / `Lag` / `Sway` | 8° / 10° / 3° | 4–12° / 5–15° / 0–5° | Storm Cam useless for framing | Storm Cam aims for you |
| **Exploit gates (E1–E3)** | | | | |
| `MinSlideRefillSpeed` | 6 m/s | 4–10 | Boost hard to earn | Donut farming |
| `MinAirtimeHeight` | 1.8 m | 1.6–3 | Only big ramps count | Bunny-hop farming |
| `NearMissCooldown` | 3 s | 2–6 | Near-misses rare | Orbit farming |

**Ownership:** F13's star coefficients are knobs owned by F13. Change an archetype's feel through its
stars, not per-vehicle overrides, so archetypes stay comparable (and `economy-progression.md` Tree 1
deltas stay expressible as stars).

**Interacting knobs:**
- `GripStiffness`/`μ` vs `HandbrakeGrip` — a high grip cap makes the handbrake the only way to slide
  (intended for the Pickup).
- `WindResponse` × `Exposure` multiply — tune `WindResponse` once; vary only `Exposure` per archetype.
- `LiftScale` vs `TossThreshold` set the toss distance — keep EF5's just outside its damage radius.

## Visual/Audio Requirements

ADR-0003 style; most of this is the Presentation lane (Codex), driven by vehicle state flags and events.

| Moment | Visual | Audio | Priority |
|--------|--------|-------|----------|
| Driving | Visible suspension travel, body roll/squat, wheel spin matching speed | Engine pitch tracks speed; load raises rev | Must |
| Sliding | Hand-drawn tire-smoke cards from rear wheels; skid decals | Tire squeal scaled by slip angle | Must |
| Landing | Dust-burst cards scaled by impact speed; camera dip | Suspension thump; heavy crunch above 70 % of Light | Must |
| Boost | Exhaust flame/streak cards; FOV +4° while active | Whoosh on start, sustained roar | Must |
| Impact (1/2 HP) | Camera kick, spark cards, debris puff; 2 HP adds a brief chroma flash *in the viewfinder only* (retro lens) | Metal crunch sized to severity | Must |
| Tossed | Debris cards orbiting truck, wind streaks wrapping around it | Wind roar peaks, muffles mid-air | Should |
| Near-miss | Brief speed-line burst toward the funnel | Doppler whoosh | Should |
| Wheels light (lift) | Suspension visibly extends | Rising wind pitch | Should |

## UI Requirements

HUD = Claude lane; style pops = Codex lane.
- **Boost meter** under truck HP: amber fill; flashes when a style refill lands; greys out when Critical
  damage disables boost.
- **Storm Cam indicator**: small lock icon + target EF rating near the IN THE WIND meter; "NO TARGET"
  while toggled with nothing in range.
- **Controls copy**: title-screen controls panel updates to the new bindings (shutter RB / left mouse);
  replaces 0.4's "SHOOT SPACE / L2".
- **Style pops** ("DRIFT 2.4s", "AIR 1.8s", "NEAR MISS") from Codex, driven by `StyleEvent`.

## Acceptance Criteria

**Unit (EditMode, pure math)**
- [ ] F1: Pickup at rest on flat ground settles at 35 % ± 3 % of travel.
- [ ] F10: impact speeds just below/above Light and Severe return 0 / 1 / 2 HP; a 12.5 m/s vertical
      landing returns 0 (×1.5 rule).
- [ ] F12: Pickup (Exposure 0.85) at Intensity 1 — EF3 never reaches the toss threshold at any
      distance; EF5 tosses at 5.0 m and not at 6.0 m.
- [ ] F13: Pickup stars (3/3/2) → exactly 21.5 m/s, 2100 kg, exposure 0.85, 12.5 / 22.5 m/s;
      Motorcycle and Monster Truck match the doc's extremes.
- [ ] E2/E3: jump-only airtime (1.5 m apex) adds zero boost; a second near-miss on the same tornado
      within 3 s adds zero.

**PlayMode (real scene, scripted input)**
- [ ] Full throttle from rest reaches ≥ 90 % of `v_top` in `AccelTime` ± 0.3 s.
- [ ] Handbrake at 15 m/s + full steer enters Sliding within 0.3 s; release returns to Grounded within
      1 s with no spin-out.
- [ ] Driving off a 2 m ledge at speed: `Airborne` → `Landed` events fire; HP unchanged.
- [ ] Static wall at 14 m/s → exactly 1 HP; at 24 m/s → exactly 2 HP; light fence → 0 HP.
- [ ] Parked (no input) 15 m from an EF3: horizontal displacement > 0.5 m within 1 s; never Tossed.
- [ ] Parked 5 m from a mature EF5: `Tossed` fires within 1 s and the truck actually leaves the ground
      (all wheels ungrounded within 0.3 s of the event).
- [ ] Ramming a Ram & Unblock obstacle at ≥ 50 MPH (boosting) clears it with 0 HP lost; F10 is not
      applied to event obstacles.
- [ ] Holding boost on flat ground never exceeds `BoostMaxSpeed` + 0.5 m/s.
- [ ] Full-throttle launch from rest shows wheelspin (driven-wheel longitudinal force capped by the
      friction circle) for ≥ 0.2 s.
- [ ] Upside-down on flat ground: auto-rights within 1.5 s, including with wind acting on it.
- [ ] Storm Cam on, tornado in range: camera-to-tornado angle stays < 5° while the truck drives a full
      circle around it.
- [ ] Camera-forward aim: truck facing 90° away, camera centered on funnel at 20 m → PERFECT tier.
- [ ] Damage hooks: Damaged → max steer = 0.75 × normal; Critical → throttle/boost/jump produce no
      forward force.
- [ ] Regression: updated `RunLoopSmokeTests` pass with the new vehicle.

**Performance**
- [ ] Vehicle physics (4 casts + forces + wind) ≤ 0.3 ms per physics step on dev PC; ≤ 0.8 ms in WebGL.
- [ ] No vehicle-code frame spikes > 2 ms during a toss → crash sequence (Profiler).

**Feel (playtest gate — Andy + 2 outside players, 5 runs each)**
- [ ] All three powerslide intentionally within 2 runs, unprompted.
- [ ] Each answers yes to both "did the truck feel heavy?" and "did it feel responsive?" (weighty-agile).
- [ ] At least one drift or airborne photo per player by run 5.
- [ ] EF5 toss reads as exciting/funny, not unfair (watch for "that's cheap").

## Open Questions

| Question | Owner | Resolve by |
|----------|-------|------------|
| ~~Storm Cam trivializes aim?~~ Resolved 2026-10-01: offset framing (F14) — contingent on a framing-sensitive AimScore in `photo-scoring.md` | Andy + Claude | Resolved |
| ~~Replace AimScore dot product with a framing curve?~~ Resolved 2026-10-01: `clamp01(1 − θ / 15°)` written to `photo-scoring.md` | Andy | Resolved |
| ADR-0005: raycast-vehicle architecture (required by coding standards before implementation) | Claude | Before Sprint 7 code |
| Keyboard comfort: Left Shift boost next to Left Ctrl handbrake, or move handbrake/jump? | Andy | Playtest |
| Motorcycle: two wheels break the 4-wheel model — suspension and fall-over rules? | Claude | Before the Motorcycle unlock is built |
| Monster Truck "terrain immunity": surface-grip exemption, or much larger wheel radius/travel? | Claude | With ADR-0004 |
| Should Tossed grant a style bonus? Landing a toss is the best stunt in the game | Andy | With the style-scoring GDD |
| WebGL physics budget: is 50 Hz affordable alongside destructible terrain? | Claude | ADR-0004 performance section |
