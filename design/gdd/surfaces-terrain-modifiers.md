# Surfaces & Terrain Modifiers

> **Status**: In Design
> **Author**: Andy Styx + Claude
> **Last Updated**: 2026-10-01
> **Last Verified**: 2026-10-01
> **Implements Pillar**: 1 — Kinetic Chaos (game-concept.md)

## Summary

Surfaces & Terrain Modifiers turns what's under each wheel into a grip multiplier and a
rolling drag: six tagged surface types, temporary condition zones that disasters lay
down (wet, hail-slick, scorched), and a per-vehicle affinity hook for loadouts and
modifiers. It makes route choice part of driving: asphalt is the safe line, dirt slides,
mud slogs, and hail can turn the safe road into a spin. Vehicle Feel reads it on every
wheel, every physics step.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None upstream; read by Vehicle Feel; zones placed by Wildfire, Hailstorm; affinity from Loadout, Per-Run Modifiers`

## Overview

Surfaces & Terrain Modifiers decides how the ground under each wheel affects the truck.
Every world collider carries a **surface type**: Asphalt, DirtRoad, Grass, Gravel, Mud or
Debris. Each type defines a **grip multiplier**, which scales the tire friction budget,
and a **rolling drag**, a constant deceleration. Ground without a tag counts as Grass. On
top of the base type, disasters can apply temporary **surface conditions**: rain-wet,
hail-slick, scorched. Each condition multiplies the base values in an area for as long as
it's active. A **vehicle affinity** hook lets Loadout and Run Modifiers scale how much a
given vehicle feels the surface (a Monster Truck shrugging off mud, Bald Tires losing
their grip). Each surface also carries **feedback data** (skid sound, dust or spray
effect, tire-mark tint), so audio and VFX react to what's under the wheels without their
own lookup table.

For the player, the ground is part of the route choice. Asphalt is fast and grippy, so
it's the safe line. Dirt and grass let the truck slide, which earns drift style. Mud costs
speed. A hail-slicked road that was safe a minute ago can now spin the truck into the
funnel. This system carries Pillar 1 under every other driving rule: Vehicle Feel's F3
grip budget and drag term read the result of this system on every wheel, every physics
step.

Implementation reference: ADR-0004 §3–4 (surface-tagged colliders, the base table, and
the lookup when a wheel hits a collider) and ADR-0005 §6 (a per-collider surface cache,
cleared when a tile is recycled).

## Detailed Design

### Core Rules

1. **Surface types.** Season 1 has a closed set of six: Asphalt, DirtRoad, Grass,
   Gravel, Mud and Debris. Every collider the vehicle can touch carries exactly one
   type, and a collider without a tag reads as **Grass**:
   - the heightfield is Grass;
   - road meshes are Asphalt or DirtRoad;
   - patch meshes are Mud or Gravel;
   - every destructible piece is Debris, whether it's still moving or has settled.

   The tag is read from the hit collider or its nearest parent. Trigger colliders and
   the vehicle's own layer are never sampled (ADR-0005). Tags don't change at runtime in
   Season 1; anything that changes one must clear the surface cache.
2. **Surface definitions.** Each type is defined as data: `BaseGrip`, `BaseDrag`, an
   `OffRoad` flag (Asphalt and DirtRoad are on-road; everything else is off-road), and a
   feedback block with `SkidSound`, `ContactVfx` and `TireMarkTint`. No surface value
   lives in code. Today's hardcoded `SurfaceTable` switch must move to data before this
   GDD counts as implemented.
3. **Per-wheel sampling.** On every physics step, each grounded wheel samples the
   surface at its own contact point. An airborne wheel has no surface sample. Because
   wheels sample independently, two wheels in mud and two on the road pull the truck
   toward the mud. That's intended: it's how a ditch grabs you.
4. **Conditions.** Season 1 has three:
   - **Wet**, for rain bands. Nothing places it yet; it's reserved for disasters to use.
   - **HailSlick**, from Hailstorm (Alpha).
   - **Scorched**, from Wildfire (VS).

   Each condition defines `GripMul` and `DragMul`, optional overrides per base type (for
   example, Wet on DirtRoad behaves closer to Mud than Wet on Asphalt does), and an
   optional feedback override (spray instead of dust).
5. **Condition zones.** A disaster system places a zone, owns it and ends it. A zone is
   a circle or a capsule (a swept swath) in world space, with a `Strength` from 0 to 1:
   - it ramps in over `RampIn`;
   - it holds while its owner keeps refreshing it;
   - it decays over `DecayTime` once the owner stops.

   Containment is checked on the ground plane (XZ; contact height is ignored). Capsules
   have round ends, and a point exactly at distance R is outside. A refresh counts for
   `RefreshHold` (0.5 s), so an owner only has to refresh at 2 Hz or faster.

   Zones aren't attached to tiles and aren't saved in `WorldState`; tiles streaming in
   and out doesn't affect them.
6. **Strongest wins.** At a contact point, every zone that contains the point produces
   an effective grip multiplier and an effective drag multiplier, each scaled by the
   zone's Strength. The lowest grip multiplier applies, and separately the highest drag
   multiplier applies. Overlapping conditions never compound. Feedback comes from the
   zone with the lowest grip.
7. **Vehicle affinity.** The final grip and drag pass through the vehicle's affinity
   parameters, which come from Loadout and Run Modifiers. Surfaces defines the slot and
   the formula but not the values. With nothing set, affinity is neutral.
8. **Output.** Each wheel produces a `SurfaceSample`: `HasSample` (false while
   airborne), `Grip`, `Drag`, `Type`, `Condition` and the feedback IDs. `Condition`
   names the winning zone's condition only once that zone's `S_eff` reaches
   `ConditionReportThreshold` (0.1); below it, it reads None, so audio and VFX don't
   flicker while a zone fades in. Grip and Drag still use the full value. Feedback comes
   from the reported condition's override if it has one, otherwise from the base
   surface. Vehicle Feel uses Grip and Drag. Presentation and audio get the rest from
   the vehicle state snapshot rather than looking up the world themselves, and show
   nothing for a wheel whose `HasSample` is false.
9. **Zone cap.** The number of active zones is limited (`MaxZones`, see Formulas). When
   a new zone would go over the cap, it is ranked against the existing zones (F5
   eviction severity, with the new zone rated at full strength). If it ranks weakest it
   isn't created; otherwise the weakest existing zone expires.

> `systems-designer` not consulted — Lean mode. Review manually before production.

### States and Transitions

Surface types have no state. Only condition zones do:

| State | Strength | Enter when | Exit to |
|---|---|---|---|
| RampIn | rising toward 1 at `1/RampIn` per second (F5) | its owner creates it, or a refresh arrives during Decaying (the climb resumes from the current Strength; no snap) | Active once Strength reaches 1; Decaying if refreshes stop first |
| Active | 1 | the ramp finished | Decaying when its owner stops refreshing it or ends it |
| Decaying | falling toward 0 at `1/DecayTime` per second (F5) | the owner has gone quiet | RampIn on a refresh; Expired at 0 |
| Expired | 0 | Strength reached 0, or evicted by the cap | removed |

### Interactions with Other Systems

| System | In (from it) | Out (to it) | Interface owner |
|---|---|---|---|
| Vehicle Feel | wheel contact collider + point | `Grip`, `Drag` for each wheel | Surfaces (the sample), Vehicle Feel (how it's used, F3/F2b) |
| Tiled World Streaming | tagged heightfield colliders; tile-recycle signal | (cleared surface cache) | World Streaming |
| Roads & POI | Asphalt/DirtRoad/Gravel/Mud tags on road and POI meshes | — | Roads & POI does the tagging |
| Destructibles | Debris tags on all pieces | — | Destructibles |
| Wildfire (VS) | Scorched zones behind the fire front | — | Wildfire places the zones |
| Hailstorm (Alpha) | HailSlick zones along the swath | — | Hailstorm places the zones |
| Loadout / Per-Run Modifiers | vehicle affinity values | — | Loadout and Modifiers own the values |
| Presentation (VFX X7-02, audio X7-03; Codex lane) | — | each wheel's Type, Condition, feedback IDs, via the vehicle state snapshot | Surfaces owns the data; Presentation consumes it |

## Formulas

Units are m, s, kg. Reference vehicle is the Pickup: `v_top` 21.5 m/s, `AccelTime`
3.0 s, `A_engine = 1.28 · v_top / AccelTime` = 9.17 m/s² (`vehicle-feel.md` F2). The
friction budget per unit of grip is `GripMu · g` = 1.15 · 9.81 = 11.28 m/s² (F3). All
four wheels are driven (`VehicleModel` splits drive force 0.25 per wheel).

**How the vehicle uses the output** (`VehicleModel`, F2b/F3): each wheel applies
`quarterMass · Drag` against its forward velocity, clamped so it can't reverse the
wheel. That force is added to the drive/brake force **before** the friction-circle
clamp `B = GripMu · load · Grip · HB · WindGripMul · GripScale`. So drag can never push
past the tire budget, and it can't stall a driven truck on its own.

**F1. Condition effective multiplier** is defined as:

`S_eff = Strength · Edge`, where `Edge = clamp01((R − d) / EdgeFeather)`; when
`EdgeFeather` = 0, `Edge` = 1 for d < R, else 0.
`GripMul_eff = 1 + S_eff · (GripMul(base) − 1)`
`DragMul_eff = 1 + S_eff · (DragMul(base) − 1)`

`GripMul(base)` and `DragMul(base)` are the condition's per-base-type override when one
exists, otherwise its default.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| Strength | float | 0–1 | calculated (F5) | Zone's current strength |
| R | float (m) | > 0 | data file (placed by the owning disaster) | Zone radius (circle) or half-width (capsule) |
| d | float (m) | ≥ 0 | calculated | Ground-plane (XZ) distance from the contact point to the zone centre (circle) or swath segment (capsule, round ends) |
| EdgeFeather | float (m) | 0–10, default 3 | data file | Soft border width; 0 = hard edge |
| GripMul(base) | float | 0.2–1.0 | data file | Condition grip multiplier; conditions never improve grip |
| DragMul(base) | float | 1.0–3.0 | data file | Condition drag multiplier; conditions never reduce drag |
| S_eff | float | 0–1 | calculated | Strength after edge falloff |

**Output Range:** `GripMul_eff` ∈ [GripMul, 1], `DragMul_eff` ∈ [1, DragMul]; both are
exactly 1 at `S_eff` = 0 and change monotonically with Strength, so a fading zone never
steps.
**Example:** Wet on DirtRoad (override 0.65 / 1.6), Strength 0.6, deep inside the zone
(Edge 1): `GripMul_eff` = 1 + 0.6·(0.65 − 1) = **0.79**; `DragMul_eff` = 1 + 0.6·(1.6 − 1) = **1.36**.

**F2. Strongest-wins combination** is defined as:

`Grip_c = min(1, min_i GripMul_eff,i)`
`Drag_c = max(1, max_i DragMul_eff,i)`

Here i runs over every zone with `Edge > 0` at the contact point. Each channel is picked
independently. `Condition` and the feedback IDs come from the zone with the lowest
`GripMul_eff`; a tie goes to the higher `DragMul_eff`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| GripMul_eff,i / DragMul_eff,i | float | see F1 | calculated | Per-zone values |
| Grip_c | float | 0.2–1.0 | calculated | Combined condition grip multiplier |
| Drag_c | float | 1.0–3.0 | calculated | Combined condition drag multiplier |

**Output Range:** with no zones the result is (1, 1). Adding a zone can only lower
`Grip_c` or leave it equal, so conditions never compound.
**Example:** the Wet zone above (0.79 / 1.36) overlaps a HailSlick zone at Strength 0.5
(default 0.55 / 1.0 gives 0.775 / 1.0). `Grip_c` = **0.775**, `Drag_c` = **1.36**, and
the feedback is hail's.

**F3. Vehicle affinity** is defined as:

`Grip_a = TireGrip · (1 − GripSens · (1 − Grip_in))`
`Drag_a = DragSens · Drag_in`

Loadout and Modifiers aggregate their sources first. `GripSens = Π GripSens_k` and
`DragSens = Π DragSens_k`, each clamped to 0–2. `TireGrip = Π TireGrip_k`, clamped to
0.6–1.2. If any source sets `NoBonus`, then `GripSens ← max(GripSens, 1)`,
`DragSens ← max(DragSens, 1)` and `TireGrip ← min(TireGrip, 1)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| Grip_in | float | 0–1 | calculated (F4) | Grip before affinity; its shortfall from Asphalt (1.0) is what sensitivity scales |
| Drag_in | float (m/s²) | ≥ 0 | calculated (F4) | Drag before affinity |
| GripSens | float | 0–2, default 1 | data file (Loadout / Modifiers) | 0 = immune, < 1 = resists, > 1 = punished |
| DragSens | float | 0–2, default 1 | data file (Loadout / Modifiers) | Same, for drag |
| TireGrip | float | 0.6–1.2, default 1 | data file (Loadout / Modifiers) | Flat grip scale on every surface, Asphalt included |
| NoBonus | bool | default false | data file (Modifiers) | Cancels every better-than-neutral affinity value |

Sensitivity scales the *shortfall* from Asphalt, so it never affects Asphalt itself. The
on-road/off-road split falls out of that without a branch, and condition shortfalls (a
wet road) scale the same way. Only `TireGrip` can change grip on Asphalt, which is why
Bald Tires uses it. Note that immunity also covers conditions: a Monster Truck mostly
ignores hail. If that's wrong, `GripSens` splits into a terrain part and a condition
part (see Open Questions).

**Output Range:** `Grip_a` ∈ (−1, 1.2] before F4's clamp; it goes below zero only at
`GripSens` > 1.8 on Mud. `Drag_a` ∈ [0, 2 · Drag_in]. Neutral values (1, 1, 1) leave
the inputs unchanged.
**Example:** on Mud (0.45 / 2.5):
- Monster Truck (GripSens 0.2, DragSens 0.2): grip 1 − 0.2·0.55 = **0.89**, drag **0.5**.
- WHEELS module (GripSens 0.85): grip **0.53**.
- Bald Tires (TireGrip 0.85, NoBonus): grip 0.85 · 0.45 = **0.38**.

**F4. Final SurfaceSample** is defined as:

`Grip_in = clamp01(BaseGrip(type) · Grip_c)`
`Drag_in = BaseDrag(type) · Drag_c`
`Grip = clamp(Grip_a, GripFloor, GripCeil)`
`Drag = min(Drag_a, MaxDrag)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| BaseGrip(type) / BaseDrag(type) | float | see table below | data file | Surface definition (ADR-0004 §4 values) |
| Grip_c / Drag_c | float | F2 | calculated | Condition result |
| Grip_a / Drag_a | float | F3 | calculated | After affinity |
| GripFloor | float | 0.2–0.4, default 0.25 | data file | Minimum surface grip |
| GripCeil | float | 1.0–1.3, default 1.2 | data file | Maximum surface grip |
| MaxDrag | float (m/s²) | 2.5–5.0, default 4.0 | data file | Drag cap; must stay below every archetype's `A_engine` |
| Grip / Drag | float | 0.25–1.2 / 0–4.0 | calculated | Output to Vehicle Feel F3 / F2b |

Base table (unchanged from ADR-0004 §4):

| Type | BaseGrip | BaseDrag (m/s²) | OffRoad |
|---|---|---|---|
| Asphalt | 1.00 | 0.0 | no |
| DirtRoad | 0.85 | 0.3 | no |
| Grass (untagged default) | 0.75 | 0.6 | yes |
| Gravel | 0.70 | 0.8 | yes |
| Debris | 0.60 | 1.0 | yes |
| Mud | 0.45 | 2.5 | yes |

- **Why the floor is 0.25.** It gives about 2.8 m/s² of total tire force, roughly an
  80 m turn radius at 15 m/s: a hard, steerable slide ("ice, not teleport"). The floor
  applies to the *surface* sample only. Vehicle Feel's handbrake (0.3), wind loss and
  knockback `GripScale` (0.35) multiply after it, so a handbrake slide on ice still works.
- **Why the drag cap is 4.0.** It limits top-speed loss to about 20% (F6).

**Output Range:** Grip ∈ [0.25, 1.2]; Drag ∈ [0, 4.0].
**Example (full chain):** Pickup with Bald Tires on DirtRoad, inside the F2 overlap
(`Grip_c` 0.775, `Drag_c` 1.36):
- `Grip_in` = 0.85 · 0.775 = 0.659 and `Drag_in` = 0.3 · 1.36 = 0.408.
- `Grip_a` = 0.85 · (1 − 1 · 0.341) = 0.560, so Grip = **0.560**.
- Drag = **0.408**.

**F5. Zone Strength over time** is defined as:

`Strength(t + dt) = clamp01(Strength(t) + dt · (refreshed ? 1 / RampIn : −1 / DecayTime))`

`Strength(0)` = 0. A zone is removed once Strength = 0 and it isn't being refreshed.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| dt | float (s) | 0.02 | constant (50 Hz physics) | Step length |
| refreshed | bool | — | calculated | The owner's last refresh was within `RefreshHold` |
| RefreshHold | float (s) | 0.1–2, default 0.5 | data file | How long one refresh counts; owners refresh at ≥ 1/RefreshHold Hz |
| RampIn | float (s) | 0.1–30 | data file (per condition) | Time from 0 to 1; floored at 0.1 |
| DecayTime | float (s) | 0.1–120 | data file (per condition) | Time from 1 to 0 |

**Output Range:** [0, 1]; ramp and decay are linear.
**Example:** HailSlick (2 s / 12 s) is refreshed for 1 s, giving Strength 0.5. Its
owner then goes quiet for 3 s: 0.5 − 3/12 = **0.25**. A new refresh resumes the climb at
0.5 per second from 0.25. It doesn't snap to 1.

**Eviction severity (Core Rule 9):** `Sev = Strength · max(1 − GripMul, (DragMul − 1) / 2)`,
using the condition's default multipliers (not per-base overrides). A zone being placed
is rated at Strength 1; existing zones use their current Strength. At the cap the lowest
`Sev` loses: if that's the new zone it isn't created, otherwise the weakest existing
zone expires. A tie goes against the oldest zone.

`ConditionReportThreshold` (data file, 0–0.5, default 0.1) is the `S_eff` a winning zone
must reach before `Condition` and its feedback are reported (Core Rule 8).

**F6. Effective top speed and launch (designer sanity check, not runtime)** is defined as:

`v_eff = v_top · (1 − Drag / A_engine)^(1 / 2.5)`, valid for `Drag < A_engine`
`LaunchAccel = min(A_engine − Drag, GripMu · g · Grip)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| v_top | float (m/s) | 21.5 (Pickup) | data file (vehicle) | Top speed on Asphalt |
| A_engine | float (m/s²) | 9.17 (Pickup) | calculated (vehicle-feel F2) | Engine acceleration coefficient |
| Drag / Grip | float | F4 | calculated | Surface output |
| v_eff | float (m/s) | 0–v_top | calculated | Full-throttle speed where drive equals drag (ignores wind, slope, downforce) |
| LaunchAccel | float (m/s²) | ≥ 0 | calculated | Standstill acceleration on flat ground, all four wheels on the surface |

**Output Range:** `v_eff` → 0 as Drag → `A_engine`. `MaxDrag` is what keeps this case
out of play.
**Example (Pickup, base table, no conditions):**

| Surface | v_eff (m/s) | Speed loss | LaunchAccel (m/s²) | Coast decel (2.5 + Drag) |
|---|---|---|---|---|
| Asphalt | 21.50 | 0.0% | 9.17 | 2.5 |
| DirtRoad | 21.22 | 1.3% | 8.87 | 2.8 |
| Grass | 20.93 | 2.7% | 8.46 | 3.1 |
| Gravel | 20.73 | 3.6% | 7.90 | 3.3 |
| Debris | 20.53 | 4.5% | 6.77 | 3.5 |
| Mud | 18.93 | 12.0% | 5.08 | 5.0 |

The four middle surfaces differ mostly in grip and coasting, not top speed; Mud is the
clear slog. At the drag cap (4.0), `v_eff` = 17.1 m/s (20.5% loss).

**Starting condition values:**

| Condition | GripMul / DragMul | Overrides | RampIn / DecayTime | Rationale |
|---|---|---|---|---|
| Wet | 0.80 / 1.15 | DirtRoad 0.65 / 1.6; Mud 0.90 / 1.2 | 6 s / 20 s | Mild on asphalt, a real hit on dirt; rain changes the route without ending the run |
| HailSlick | 0.55 / 1.0 | Mud 0.90 / 1.0 | 2 s / 12 s | Hail on Asphalt drops to 0.55, near Mud, so the safe road becomes a spin risk; mud stays a trap, not impossible |
| Scorched | 0.85 / 1.3 | Asphalt 0.90 / 1.0 | 3 s / 45 s | Ash costs a little grip and adds drag; it lingers well behind the fire front |

`MaxZones` = 32. At 4 wheels · 32 zones · 50 Hz that's 6,400 containment tests per
second, which is negligible.

> `systems-designer` consulted (Lean mode, HIGH-risk section). Two corrections were
> applied after checking the proposal against `VehicleModel`. Drag sits inside the
> friction circle, so the proposed drag traction cap was dropped and launch values were
> recomputed. The affinity flat term was renamed from `GripScale` to `TireGrip` so it
> doesn't collide with Vehicle Feel's knockback `GripScale`.

## Edge Cases

- **If a collider has no `SurfaceTag`:** it reads as Grass. **Exception:** a collider on
  a moving (non-kinematic) rigidbody with no tag reads as **Debris**, since loose
  objects are rubble whether or not anyone tagged them.
- **If a wheel's cached collider was destroyed or recycled:** the tile-recycle signal
  clears the cache (ADR-0005). A null collider reads as Grass for that step and is never
  kept in the cache.
- **If a destructible piece settles and freezes:** it stays Debris, so its cache entry
  stays valid.
- **If a wheel is airborne:** it has no sample. On landing it uses the new surface
  immediately. Tagged surface boundaries (road edge to grass) are hard steps by design;
  only condition zones have a feathered edge.
- **If a capsule zone has zero length:** it's treated as a circle.
- **If a zone's radius is less than 2 × `EdgeFeather`:** the feather is clamped to R/2
  when the zone is placed, so the zone still reaches full strength in its centre.
- **If `RampIn` or `DecayTime` is authored below 0.1 s:** it's clamped to 0.1 s at load,
  with a data-validation warning.
- **If a zone's owner despawns or merges** (say, a Wildfire is absorbed into a Fire
  Tornado): the zone counts as not refreshed and decays normally; zones never disappear
  instantly. The merged entity places its own zones.
- **If `MaxZones` is reached:** the new zone, rated at Strength 1, is ranked against the
  existing ones by `Sev`. If it's the weakest, it isn't created; otherwise the weakest
  existing zone is evicted. A tie evicts the oldest.
- **If several zones of the same condition overlap:** strongest wins as usual, so the
  result never goes deeper than a single zone at full strength.
- **If affinity sensitivity is high on Mud:** the 0.25 grip floor takes over from
  `GripSens` ≈ 1.36 (1 − 0.55 · s < 0.25), and above 1.8 the raw value goes negative;
  F4 clamps both to 0.25.
- **If `EdgeFeather` is 0:** the zone has a hard edge (Edge 1 inside, 0 outside); no
  division by zero.
- **If an owner refreshes slower than `RefreshHold`:** Strength saw-tooths between
  refreshes. This is an owner bug, caught by a data-validation warning when a zone
  re-enters RampIn more than twice a second.
- **If a wheel is on a trigger collider or the vehicle's own collider:** it's never
  sampled; the sphere cast ignores both (ADR-0005).
- **If a new archetype's `A_engine` is at or below `MaxDrag`:** the truck could stall
  in deep mud. Data validation fails the build; the fix is raising that archetype's
  acceleration or lowering `MaxDrag`.
- **If a condition targets Asphalt drag:** it has no effect, because Asphalt's base drag
  is 0 and conditions multiply it. This is a known limitation (Open Questions).
- **If the run ends or restarts:** all zones are cleared at run start.
- **If a wheel has no active condition:** its feedback IDs come from the base surface.

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

## Dependencies

**Upstream (this system depends on):** none. Surfaces is a Foundation system. A collider
without a tag defaults to Grass, so nothing has to exist first.

**Downstream (depend on this system):**

| System | Hard / Soft | Interface | GDD |
|---|---|---|---|
| Vehicle Feel | Soft (drives on untagged ground as Grass) | Reads each wheel's `SurfaceSample.Grip` and `.Drag` every step (F3 budget, F2b drag); owns the surface cache and clears it on tile recycle | `vehicle-feel.md` |
| Destructibles & Debris | Hard | Must tag every piece Debris | — (ADR-0004 §5–6) |
| Roads & POI Chunks | Hard | Must tag road and POI meshes Asphalt, DirtRoad, Gravel or Mud | — |
| Tiled World Streaming | Soft | The heightfield is Grass by default, so tagging is optional | — (ADR-0004 §3) |
| Wildfire | Soft for the fire, hard for its traction effect | Places and refreshes Scorched zones | — |
| Hailstorm | Same as Wildfire | Places and refreshes HailSlick zones | — |
| Vehicle Loadout & Archetypes | Soft | Supplies `GripSens`, `DragSens`, `TireGrip` (F3) | — |
| Per-Run Modifiers | Soft | Supplies affinity values plus `NoBonus` (Bald Tires) | — |
| Procedural Audio, Vehicle VFX (Codex lane) | Soft | Read each wheel's `Type`, `Condition` and feedback IDs from the vehicle state snapshot | — |

Provisional: every downstream system except Vehicle Feel is undesigned. The interfaces
above are the contract those GDDs must honour, or else raise a conflict against.

## Visual/Audio Requirements

The data lives here; the effects themselves are built by Presentation (VFX X7-02, audio
X7-03, Codex lane) as 2D cards in 3D space, following the stylized palette (ADR-0003).

| Surface | `SkidSound` | `ContactVfx` | `TireMarkTint` |
|---|---|---|---|
| Asphalt | rubber squeal | thin grey smoke | dark rubber |
| DirtRoad | gritty scrub | brown dust plume | tan ruts |
| Grass | soft tear | green-brown clod spray | faint green-brown |
| Gravel | stone rattle and spit | grey chip spray | grey scuffs |
| Mud | squelch / slurp | heavy, slow mud flicks | deep dark ruts |
| Debris | crunch and clatter | splinters and dust, occasional sparks | none |

| Condition | Feedback override |
|---|---|
| Wet | water spray; a hiss layered over the base sound; darker marks |
| HailSlick | ice crunch; white skid streaks |
| Scorched | ash puffs; black marks |

> `art-director` not consulted — Lean mode (the effects aren't central to this system's
> rules). Review manually before production.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is
> approved, run `/asset-spec system:surfaces-terrain-modifiers` to produce per-asset
> descriptions and generation prompts from this section.

## Game Feel

- **Readable fast:** the player can tell which surface they're on within 0.2 s of
  crossing onto it, through sound, effects and handling together.
- **Asphalt is the stable line.** On DirtRoad and Grass, a drift is reachable from a
  moderate handbrake flick.
- **Mud is a slog:** launch acceleration 5.1 vs 9.2 m/s² on Asphalt (F6), obvious within
  the first second of throttle.
- **Hail betrays the safe road:** HailSlick on Asphalt (0.55) sits near Mud grip, so a
  line that was safe a minute ago can spin the truck.
- **No smoothing on tagged surfaces.** The one-step change at a road edge is part of the
  feedback; only condition zones feather.

## UI Requirements

There's no player-facing UI in Season 1; the surface is shown through handling, sound
and effects only. A developer-only debug overlay shows each wheel's `Type`, `Grip`,
`Drag`, `Condition` and the active zones (outline + Strength). This system needs no UX
spec.

## Cross-References

| Reference | What's used |
|---|---|
| `design/gdd/vehicle-feel.md` F2, F2b, F3, E13 | Consumes Grip and Drag; F6 uses its F2 numbers; the GripFloor bounds its `SurfaceGrip` |
| `docs/architecture/adr-0004-destructible-tiled-world.md` §3–6 | Collider tagging, the base surface table, the Debris lifecycle |
| `docs/architecture/adr-0005-raycast-vehicle-architecture.md` §6 | Per-collider surface cache and clearing it on tile recycle; sphere cast ignores triggers and the vehicle layer |
| `design/gdd/economy-progression.md` (Hover Prototype) | "Water/mud terrain immunity", expressed as affinity (see Open Questions) |
| `design/vision/vision-1.0.md` (Bald Tires, WHEELS module, Monster Truck) | The affinity examples in F3 |
| `design/gdd/systems-index.md` #7, #14, #15 | Index rows for this system, Wildfire and Hailstorm |

## Acceptance Criteria

**Conventions.** Pure-math criteria assume the composition is exposed as scene-free
functions (sampling, combination, affinity, finalize, zone stepping). `VehicleModel` is
already scene-free, so it is driven directly in EditMode. Tolerances are ±0.001 where
the GDD quotes three decimals, ±1e-4 otherwise. "Pickup" means `v_top` 21.5,
`A_engine` 9.17, `GripMu` 1.15. Zones in tests come from a debug zone placer: no real
placer (Hailstorm, Wildfire) exists yet. Test types are tagged
**[EditMode]**, **[PlayMode]**, **[Data]** (data validation) and **[Visual]**. Gates
follow `coding-standards.md`: Logic and Integration are BLOCKING, Visual is BLOCKING
with retained evidence, and Data is a smoke-check item.

### Core Rules

- **CR1-a [EditMode]** GIVEN a collider with no `SurfaceTag` and no rigidbody, WHEN
  sampled, THEN Type = Grass and (Grip, Drag) = (0.75, 0.60).
- **CR1-b [EditMode]** GIVEN a collider tagged with each of the six types, WHEN each is
  sampled, THEN Type matches and (Grip, Drag) equals the base table exactly.
- **CR1-c [EditMode]** GIVEN a tag on a parent object and none on the hit collider,
  WHEN sampled, THEN the parent's type is used. GIVEN a trigger collider at the wheel
  position, THEN it is not sampled.
- **CR1-d [Data]** GIVEN the surface definitions, WHEN validated, THEN every
  `SurfaceType` member has exactly one definition (6 of 6, no extras).
- **CR1-e [Data]** GIVEN every destructible-piece prefab, WHEN scanned, THEN each carries
  Debris. Road, POI and patch prefabs carry only Asphalt, DirtRoad, Gravel or Mud.
  *Activates when the Destructibles and Roads & POI stories exist.*
- **CR2-a [Data]** GIVEN the surface definitions, WHEN validated, THEN `OffRoad` is false
  for Asphalt and DirtRoad and true for the rest; every definition has non-empty
  `SkidSound`, `ContactVfx` and `TireMarkTint`; `BaseGrip` ∈ (0, 1.2]; and `BaseDrag` ≥ 0.
- **CR2-b [EditMode]** GIVEN a test definition asset with Mud changed to 0.50 / 2.0,
  WHEN Mud is sampled with no recompile, THEN the sample is 0.50 / 2.0.
- **CR3-a [PlayMode]** GIVEN a truck with its two left wheels on Mud and two right wheels
  on Asphalt, WHEN one physics step runs, THEN the left samples are 0.45 / 2.5 and the
  right samples are 1.0 / 0.0.
- **CR3-b [EditMode]** GIVEN an airborne wheel, WHEN sampled, THEN `HasSample` is false
  and no stale value is reused. On the next grounded step the new surface applies
  immediately.
- **CR4-a [Data]** GIVEN the condition data, WHEN validated, THEN exactly Wet, HailSlick
  and Scorched exist; every `GripMul` (including overrides) ∈ [0.2, 1.0]; every
  `DragMul` ∈ [1.0, 3.0]; and the starting values match the Formulas table.
- **CR4-b [EditMode]** GIVEN a Wet zone at Strength 1 and Edge 1, WHEN sampled over
  DirtRoad, THEN `Grip_in` = 0.5525 and `Drag_in` = 0.480. Over Asphalt it is 0.80 and
  0.0, so the override applies only to its own base type. GIVEN HailSlick over Mud,
  THEN `Grip_in` = 0.405.
- **CR5-a [EditMode]** GIVEN a circle zone (R 10) and a capsule zone (half-width 10),
  WHEN points are tested on the XZ plane, THEN a point at d < R is contained and a point
  at d ≥ R is not; height differences don't change the result; and a point beyond a
  capsule end is inside if it is within R of that end (round caps).
- **CR5-b [EditMode]** GIVEN active zones, WHEN `WorldState` is serialized, THEN no zone
  data appears in it.
- **CR5-c [PlayMode]** GIVEN an Active zone over tile T, refreshed continuously, WHEN T
  streams out and back in over 10 s, THEN it is the same zone instance, its Strength
  stays 1.0 and the zone count is unchanged.
- **CR5-d [EditMode]** GIVEN a new zone, WHEN stepped through its lifecycle, THEN it goes
  RampIn → Active at 1, Active → Decaying when refreshes stop, Decaying → RampIn on a
  refresh (no snap), and Decaying → Expired at 0.
- **CR5-e [EditMode]** GIVEN an owner that refreshes every 0.4 s (`RefreshHold` 0.5),
  WHEN stepped for 5 s, THEN Strength never decreases.
- **CR6-a [EditMode]** GIVEN zones of (0.6, 1.0) and (0.9, 2.0) at one point, WHEN
  combined, THEN (`Grip_c`, `Drag_c`) = (0.6, 2.0).
- **CR6-b [EditMode]** GIVEN two HailSlick zones at Strength 1.0 and 0.5 overlapping,
  WHEN combined, THEN `Grip_c` = 0.55, never a compounded value.
- **CR6-c [EditMode]** GIVEN the F2 example, WHEN combined, THEN `Grip_c` = 0.775,
  `Drag_c` = 1.36 and Condition = HailSlick. GIVEN a grip tie, THEN Condition comes from
  the zone with the higher `DragMul_eff`.
- **CR7-a [EditMode]** GIVEN neutral affinity (1, 1, 1), WHEN applied, THEN output equals
  input for every surface and condition.
- **CR7-b [EditMode]** GIVEN any `GripSens` / `DragSens` with `TireGrip` 1, WHEN applied
  on Asphalt, THEN `Grip_a` = 1.0 exactly. GIVEN `TireGrip` 0.85 on Asphalt, THEN 0.85.
- **CR8-a [EditMode]** GIVEN a grounded wheel, WHEN sampled, THEN `HasSample`, `Grip`,
  `Drag`, `Type`, `Condition` and the feedback IDs are all populated; Condition is None
  when no zone contains the point.
- **CR8-b [EditMode]** GIVEN a winning zone at `S_eff` 0.05, WHEN sampled, THEN Condition
  is None and the feedback is the base surface's, while Grip still includes the zone's
  effect. At `S_eff` ≥ 0.1, THEN Condition and its feedback (override, or base when it
  has none) are reported.
- **CR9-a [EditMode]** GIVEN 32 active zones and a new zone whose `Sev` at Strength 1
  beats the weakest, WHEN placed, THEN the count stays 32, the weakest is gone and the
  new zone exists.
- **CR9-b [EditMode]** GIVEN a full cap and a new zone that ranks weakest, WHEN placed,
  THEN it isn't created, the existing zones are untouched and nothing throws.
- **CR9-c [EditMode]** GIVEN a full cap and a `Sev` tie, WHEN a zone must go, THEN the
  oldest is evicted.

### Formulas

- **F1-1 [EditMode]** GIVEN Wet on DirtRoad (0.65 / 1.6), Strength 0.6, Edge 1, WHEN F1
  runs, THEN `GripMul_eff` = 0.79 and `DragMul_eff` = 1.36.
- **F1-2 [EditMode]** GIVEN R 10, `EdgeFeather` 3, Strength 1, WHEN d = 8.5, THEN Edge =
  0.5. WHEN d = 7, THEN Edge = 1. WHEN d = 10, THEN the output is exactly (1, 1).
- **F1-3 [EditMode]** GIVEN Strength swept from 0 to 1 in steps of 0.01, WHEN F1 runs,
  THEN grip is non-increasing, drag is non-decreasing, no single step changes either by
  more than `0.01 · |Mul − 1| + 1e-4`, and both are exactly 1 at Strength 0.
- **F1-4 [EditMode]** GIVEN `EdgeFeather` 0, WHEN a point is inside R, THEN Edge = 1, and
  outside R, THEN 0. The result is never NaN or Infinity.
- **F2-1 [EditMode]** GIVEN no zones, WHEN combined, THEN the result is (1, 1) with
  Condition None. Over 200 seeded zone sets, adding a zone never raises `Grip_c` or
  lowers `Drag_c`.
- **F3-1 [EditMode]** GIVEN the Mud examples, WHEN F3 runs, THEN the Monster Truck gets
  0.89 / 0.50, the WHEELS module gets 0.53 and Bald Tires gets 0.38 (± 0.005).
- **F3-2 [EditMode]** GIVEN source products of 3 for sensitivity and 0.5 / 1.5 for
  `TireGrip`, WHEN aggregated, THEN sensitivity clamps to 2 and `TireGrip` to 0.6 / 1.2.
- **F3-3 [EditMode]** GIVEN `NoBonus`, WHEN aggregated, THEN `GripSens` 0.2 → 1,
  `DragSens` 0.2 → 1, `TireGrip` 1.1 → 1. Worse-than-neutral values are kept.
- **F4-1 [EditMode]** GIVEN the full-chain example, WHEN F4 runs, THEN `Grip_in` = 0.659,
  `Drag_in` = 0.408, Grip = 0.560 and Drag = 0.408.
- **F4-2 [EditMode]** GIVEN Mud with `GripSens` 1.5, WHEN F4 runs, THEN Grip = 0.25.
  GIVEN Asphalt with `TireGrip` 1.2, THEN Grip = 1.2.
- **F4-3 [EditMode]** GIVEN a raw drag of 7.5, WHEN F4 runs, THEN Drag = 4.0.
- **F5-1 [EditMode]** GIVEN HailSlick (2 s / 12 s) refreshed for 1 s at dt 0.02, WHEN
  stepped, THEN Strength = 0.5. After 3 s more without refresh (beyond `RefreshHold`),
  THEN 0.25 ± 0.05 (the `RefreshHold` tail is included in the tolerance).
- **F5-2 [EditMode]** GIVEN a zone at 0.25 that is refreshed again, WHEN 0.5 s passes,
  THEN Strength = 0.5 ± 1e-3, with no snap to 1.
- **F5-3 [EditMode]** GIVEN decay from 1, WHEN no refresh arrives, THEN the zone reaches
  0 after `RefreshHold` + 12.0 s (± 1 step) and is removed on that step.
- **F6-1 [Data]** GIVEN every archetype asset, WHEN validated, THEN `MaxDrag` is below
  that archetype's `A_engine` (Pickup: 4.0 < 9.17); otherwise the build fails.
- **F6-2 [EditMode]** GIVEN the Pickup, WHEN F6 runs for Mud, THEN `v_eff` = 18.93 ± 0.05
  and `LaunchAccel` = 5.08 ± 0.02. At drag 4.0, THEN `v_eff` = 17.1 ± 0.1.

### Edge Cases

- **EC-1 [EditMode]** GIVEN an untagged collider on a non-kinematic rigidbody, WHEN
  sampled, THEN Debris. On a kinematic body, THEN Grass. An explicit tag always wins.
- **EC-2 [EditMode]** GIVEN a Debris piece that settles and freezes, WHEN sampled, THEN
  it is still Debris and its cache entry is still valid.
- **EC-3 [EditMode]** GIVEN a cached collider that has been destroyed, WHEN sampled, THEN
  Grass for that step, no exception, and nothing written to the cache.
- **EC-4 [PlayMode]** GIVEN cache entries for tile T, WHEN the recycle signal fires, THEN
  T's entries are removed and a re-sample resolves the tag again.
- **EC-5 [EditMode]** GIVEN a zero-length capsule, WHEN sampled over a grid of points,
  THEN every result equals a circle of the same centre and R.
- **EC-6 [EditMode]** GIVEN R 5 and `EdgeFeather` 3, WHEN placed, THEN the stored
  feather = 2.5, Edge = 1 for d ≤ 2.5 and Edge = 0 at d = 5.
- **EC-7 [EditMode]** GIVEN `RampIn` authored at 0.05, WHEN loaded, THEN it is 0.1 and a
  warning is logged; a refreshed zone reaches 1 in 5 steps (± 1).
- **EC-8 [EditMode]** GIVEN a zone at 0.8 whose owner despawns, WHEN stepped, THEN it
  persists and decays smoothly, and is removed only at 0.
- **EC-9 [EditMode]** GIVEN 5 zones and a run restart, WHEN the run starts, THEN the zone
  count is 0, samples read (1, 1), and a stale owner's refresh does not recreate a zone.
- **EC-10 [EditMode]** GIVEN Asphalt with a condition `DragMul` of 3.0, WHEN F4 runs,
  THEN Drag = 0.0 (the known limitation).
- **EC-11 [PlayMode]** GIVEN a wheel crossing from a road edge to Grass, WHEN the contact
  crosses the boundary, THEN the sample changes in one step, with no interpolation.

### Cross-system

- **X-1 [PlayMode] (ADR-0004 Validation #7)** GIVEN six 20 m patches (one per surface)
  and the Pickup, WHEN each is driven, THEN all four wheels report the patch type,
  Grip/Drag match the base table, and a throttle-off roll from 15 m/s decelerates
  faster than on Asphalt by `BaseDrag` ± 0.15 m/s².
- **X-2 [PlayMode]** GIVEN 12 m/s, full throttle, straight steering, with the left wheels
  on Mud and the right wheels on Asphalt, WHEN 1.5 s passes, THEN the truck yaws toward
  the Mud side by more than 2° (calibrate on first run), mirrored for the other side. An
  all-Asphalt control run yaws less than 0.5°.
- **X-3 [EditMode]** GIVEN `VehicleModel` with flat contacts and `SurfaceGrip` /
  `SurfaceDrag` set directly, WHEN stepped, THEN:
  (a) drag 2.5 adds 2.5 ± 2% deceleration over drag 0 when coasting;
  (b) at 0.01 m/s with drag 4.0, the drag force never reverses the wheel;
  (c) each wheel's tangential force stays within the friction budget (drag included,
  ×1.01);
  (d) a standing start at drag 4.0 accelerates forward.
- **X-4 [PlayMode]** GIVEN the vehicle on a HailSlick zone at Strength 1, WHEN the state
  snapshot is read, THEN each wheel reports the tagged Type, Condition = HailSlick and
  HailSlick's feedback (or the base surface's if it has no override). On bare ground,
  Condition = None.
- **X-5 [EditMode]** GIVEN the Presentation assemblies, WHEN their references are
  inspected, THEN none reference the surface lookup or the zone store; they read only
  the vehicle snapshot.
- **X-6 [PlayMode]** GIVEN surface grip forced to the 0.25 floor, the Pickup at 15 m/s,
  full steering, WHEN it drives a steady circle, THEN the radius is 80 m ± 25%, and a
  handbrake run still yaws faster than the non-handbrake run.
- **X-7 [EditMode]** GIVEN surface grip 0.25 with handbrake (0.3) or knockback
  `GripScale` (0.35), WHEN `VehicleModel` runs, THEN the budget uses 0.25 × that
  multiplier; the floor is applied once, in Surfaces.
- **X-8 [Visual]** GIVEN the X-1 track, WHEN a drift is done on each surface, THEN
  retained screenshots in `production/qa/evidence/` show distinct dust or tire-mark
  tints for all six surfaces and a spray variant under Wet, with lead sign-off.
  Presentation owns the effects (Codex lane); this criterion accepts that the data
  reaches them.

### Data-driven

- **DD-1 [Data + EditMode]** GIVEN `Assets/Scripts`, WHEN a CI grep and a reflection
  check run, THEN there is no `SurfaceTable` switch and no surface or condition
  constants in code. Grip, drag, condition multipliers, `RampIn`, `DecayTime`,
  `RefreshHold`, `EdgeFeather`, `GripFloor`, `GripCeil`, `MaxDrag`, `MaxZones` and
  `ConditionReportThreshold` all come from data assets. Today's `SurfaceTable.Get`
  fails this by design (Core Rule 2).

### Performance

- **P-1 [PlayMode perf]** GIVEN 32 active zones (circles and capsules, all overlapping
  the contacts) and 4 grounded wheels with warm caches, WHEN 1000 physics steps run
  after 100 warm-up steps, THEN the full sample (cache lookup, containment, F1–F4)
  averages ≤ 0.05 ms per step in the Editor, and allocated bytes are unchanged (0 GC).
  Expected cost is 10–20 µs: 128 containment tests at about 50–100 ns each, plus four
  cache hits and the F1–F4 math. That leaves 3–5× headroom, about 0.3% of the PC frame.

> `qa-lead` consulted (Lean mode, HIGH-risk section). Its nine gap findings were
> resolved in Core Rules 1, 5, 8 and 9, F1, F5 and Edge Cases before this section was
> written.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Split `GripSens` into terrain and condition parts? (A Monster Truck currently ignores hail too.) | Andy | Vehicle Loadout & Archetypes GDD |
| Should affinity vary by surface? Hover Prototype is Mud-only immune; F3 has one scalar for all surfaces. | Andy | Vehicle Loadout & Archetypes GDD |
| Should conditions be able to add drag on Asphalt? Today they can't, since its base drag is 0. | Andy | Wildfire GDD |
| Does any Season 1 disaster place Wet (rain bands on the Tornado)? | Andy | Disaster Entity Framework GDD |
| Should Stunt & Style give a surface bonus (drift on dirt)? | Andy | Stunt & Style Detection GDD |
