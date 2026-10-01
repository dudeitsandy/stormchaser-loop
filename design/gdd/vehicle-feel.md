# Vehicle Feel

> **Status**: In Design
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
- **Brake:** force opposing motion; holding brake near standstill engages reverse.
- **Lateral grip:** cancels sideways velocity, capped by tire load (simplified friction circle).
  Exceeding the cap is how slides happen.
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
- **Auto-right:** upside-down or on its side and nearly stopped for 1.2 s → righting flip onto wheels.

**4. Jump**
- Requires ≥ 2 wheels grounded. Upward impulse blended between vehicle-up and world-up.
  Cooldown 0.8 s. No air jump (Prototype archetype excepted — enhanced jump jets).

**5. Boost**
- Meter 0–100. Holding boost drains it and applies forward force; works airborne (air-boost recovery).
- **Refills from style**: per second of sliding, per second of airtime, per near-miss — plus a slow
  passive trickle. Cannot start below 5.

**6. Impacts**
- On any collision, impact speed = relative velocity along the contact normal.
- **≥ light threshold → 1 HP; ≥ severe threshold → 2 HP** (the hit sizes `vehicle-damage.md` defines).
  Below light: cosmetic only (shake, sound, dust).
- **Landings** are impacts on vertical speed, with thresholds × 1.5 (suspension absorbs). Clean
  landings never hurt.
- Existing post-hit invulnerability (1.5 s) still applies. Tornado funnel contact damage unchanged.

**7. Wind as a force**
- The wind field is the **air's velocity**. Horizontal force =
  `WindDrag × WindExposure(archetype) × (windVel − bodyVel)`, clamped. Applied slightly above CoM so
  the truck **leans** away from gusts. Existing grip loss in strong wind is kept.
- **Lift:** within a fraction of an **EF3+** tornado's wind radius, wind adds upward force. Wheels go
  light first; past `TossThreshold × weight` the truck is **Tossed** (airborne, reduced air control).

**8. Camera & aim** *(changes `photo-scoring.md` AimScore — update on approval)*
- Right stick / mouse **orbits** the chase camera; on release it recenters behind the direction of
  travel.
- **Storm Cam** (toggle): camera locks onto the nearest in-range disaster (Rocket League ball-cam
  analogue); the truck drives freely while the shot stays framed. Core enabler of drift-framing.
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
| **Sliding** | Powerslide / drift | Grounded and (handbrake held or slip angle > 20°) | Slip < 10° and handbrake released (+0.25 s grip recovery) |
| **Airborne** | Player-launched air | 0 wheels grounded > 0.1 s | Any wheel grounds → landing impact check → Grounded |
| **Tossed** | Wind-launched air | Wind lift > toss threshold | As Airborne; air control × 0.5 while Tossed |
| **Upended** | On roof / side | Up · worldUp < 0.3 and speed < 2 | Auto-right after 1.2 s |
| **Disabled** | Critical damage stage | `vehicle-damage.md` Critical | HP restored (perk) or Wrecked |
| **Wrecked** | 0 HP | `vehicle-damage.md` Destroyed | Run ends (existing flow) |

Sliding / Airborne / Tossed / Upended are mutually exclusive; **Disabled** overlays any of them.

### Interactions with Other Systems

| System | Flows in | Flows out |
|--------|----------|-----------|
| `photo-scoring.md` / PhotoTrigger | — | Camera-forward aim direction; state flags (Sliding, Airborne, Tossed) for future style multipliers |
| `event-system.md` | — | Slide duration (Drift Framing Zone); Airborne flag + airtime (Twister Jump Ramp ×2.0) |
| `vehicle-damage.md` | Current damage stage (steer × 0.75 Damaged; momentum-only Critical) | Impact events with severity (1 or 2 HP) |
| Wind field (`DisasterEntity`) | Wind velocity at body position; **new lift query** per disaster | — |
| Terrain (ADR-0004, **provisional**) | Surface type per wheel contact → grip/drag multipliers (2D's mud 0.4× / highway 1.5× return as surface properties) | Impact force against destructibles (decides breakage) |
| `GameEvents` | — | **New:** `StyleEvent` (drift / airtime / near-miss + amount), `Landed` (impact speed), `Impact` (severity), `Tossed` |
| HUD (Claude lane) | — | Boost meter value |
| Presentation (Codex lane) | — | `CurrentWind`, state flags, and the new events → wind/landing/drift VFX, tire smoke, sparks |
| Style scoring (future system) | — | Consumes `StyleEvent`. This system **emits** style moments; it does not score them. |

## Formulas

[To be designed]

## Edge Cases

[To be designed]

## Dependencies

[To be designed]

## Tuning Knobs

[To be designed]

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
