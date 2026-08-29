# Vehicle Damage

**Status:** Design — not yet implemented
**Target Sprint:** TBD (needed before Sprint 5-6 `event-system.md` Ram & Unblock ships)
**Owner:** Game Designer
**Implements Pillar:** Core Pillar 4 — Living Damage Economy

## Overview

Vehicle Damage tracks a small, integer hit-point pool for the player's vehicle. Ramming
failures, disaster-caused collisions, and specific hazard effects (Ice Vortex hull damage,
EMP shockwaves, wind suction) each consume HP in discrete amounts. Damage is automatic and
reactive — the player never manages it directly, only makes the driving decisions that cause
or avoid it. As HP drops, the vehicle progressively degrades both visually and mechanically
(reduced turning radius, glitched viewfinder, momentum-only movement) before reaching zero,
which destroys the vehicle and ends the run early. This system exists to give weight to every
risk decision in the game — without it, ramming a roadblock, diving close to an EF5, or
ignoring a Cataclysm Heat modifier would carry no real cost, and the "Living Damage Economy"
pillar would be fiction with no mechanical teeth.

## Player Fantasy

"My truck is held together with duct tape and I don't care — I saved twelve people and got
the shot." Damage is a badge of survival, not a failure state to avoid. Per Pillar 4:
"damage is expressive, not just punishing" — a battle-scarred vehicle that made it to the
end of a Heat 5 run should feel like a trophy, not a mistake. This is closer to Mad Max's
wear-as-character aesthetic than a racing game's clean-car purity. The tension comes from
choosing when to trade HP for score, not from dreading damage itself.

## Detailed Design

### Core Rules

1. **Max HP is derived from vehicle Armor rating** (`vision-1.0.md` archetype table): HP
   equals star count (★=1 through ★★★★★=5). ✗ Armor (Motorcycle) floors to 1 HP, same as ★.
   "Varies" (Prototype) is set per individual prototype unlock, not fixed here.
2. **Damage sources are either instant (per-event) or continuous (per-second while in a
   hazard)**:
   - Instant, 1 HP: standard collision, generic disaster contact, ramming failure (fixed by
     `event-system.md`, not redefined here)
   - Instant, 2 HP: "severe" named interactions — currently only the Tornado+Hailstorm Ice
     Vortex ("severe vehicle hull damage" per the interaction matrix)
   - Continuous, 1 HP/sec while inside the hazard: Fire Tornado exposure (Tornado+Wildfire
     merge — "vehicle takes heat damage" reads as ongoing exposure, not a single hit)
3. **EMP effects are a separate status effect, not HP damage.** "Systems temporarily
   offline / momentum-only until reboot" (EMP Mech interaction) does not consume HP — it's
   a timed debuff. The EMP Deflector Shield payload can absorb/block it.
4. **Cataclysm Heat 4 ("Brittle Chassis") doubles all instant and continuous HP damage.**
   Status effects (EMP) are unaffected.
5. **Damage is monotonic within a run** — HP only decreases (never regenerates) except via
   explicit repair sources (Repair Kit, Survivor perk).

### States and Transitions

Two pre-destruction stages, not three — "broken windshield" and "disabled engine" from the
vision doc combine into a single **Critical** stage, since a 3-HP vehicle (the default
Pickup Truck) only has two non-zero, non-full HP values to work with. Thresholds scale by
percentage of Max HP so this generalizes across archetypes:

| State | Entry Condition | Exit Condition | Behavior |
|-------|-----------------|-----------------|----------|
| Healthy | HP > floor(MaxHP × ⅔) | HP drops to Damaged threshold | No penalties |
| Damaged | HP ≤ floor(MaxHP × ⅔) and > floor(MaxHP × ⅓) | HP drops to Critical threshold, or repaired above threshold | Turning radius reduced (bent frame) |
| Critical | HP ≤ floor(MaxHP × ⅓) and > 0 | HP reaches 0, or repaired above threshold | Turning radius reduced + viewfinder glitch (broken windshield) + momentum-only movement (disabled engine) — all three combine here |
| Destroyed | HP = 0 | Run ends (or Survivor perk consumes itself and restores to full Healthy) | Session ends early, 80% score banked |

Worked examples: MaxHP=3 (Pickup Truck) → Healthy@3, Damaged@2, Critical@1, Destroyed@0 —
matches the "3 hits" default already in `event-system.md`. MaxHP=1 (Motorcycle/✗ Armor) →
both thresholds evaluate to 0, so it's Healthy@1, Destroyed@0 — no visible stages ever show,
fitting its "hyper-fragile" identity. MaxHP=5 (APC/Monster Truck) → Healthy@5-4, Damaged@3-2,
Critical@1, Destroyed@0 — tankier vehicles absorb more hits before showing any wear.

### Interactions with Other Systems

| System | Direction | Interface |
|--------|-----------|-----------|
| `PlayerVehicle.cs` | This depends on it | Receives collision/ramming/hazard-zone events; must add a health field (doesn't exist today) |
| `PlayerVehicle.cs` (movement) | This feeds it | Current stage modifies turning radius multiplier; Critical stage forces momentum-only movement |
| `event-system.md` | Depended on by | Ramming Impact Threshold's "1 HP damage" and `VehicleMaxHP` knob are already fixed by that doc — this GDD implements them, doesn't redefine them |
| `economy-progression.md` | Depended on by | Repair Kit (+1 HP), Survivor perk (full restore + destruction-prevention, once per run), Heavy Bumper (zeroes ramming self-damage), Anchor Harpoon/Clamps (immune to wind-suction damage specifically), EMP Deflector Shield (absorbs one EMP/shockwave status effect) |
| `session-modes.md` | Depended on by | Destroyed state triggers the existing "vehicle destroyed" early-end + 80% score bank behavior |
| PiP Camera (S4-01, not yet built) | Provisional / deferred | Critical stage's viewfinder glitch is a visual effect on the PiP camera feed — cannot be implemented until S4-01 exists; not a score penalty, purely cosmetic feedback |

## Formulas

### Damage Stage Thresholds
```
DamagedThreshold = floor(MaxHP × 2/3)
CriticalThreshold = floor(MaxHP × 1/3)
```
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| MaxHP | int | 1–5 | Vehicle archetype's Armor rating | Vehicle's max hit points |

**Expected output range**: DamagedThreshold 0–3, CriticalThreshold 0–1. **Edge case**: at
MaxHP=1, both thresholds are 0 — Damaged/Critical are unreachable (matches Motorcycle's
one-hit-destruction identity, per States and Transitions).

### Damage Applied (instant sources)
```
DamageApplied = BaseDamage × (Heat Rank 4 active ? 2 : 1)
```
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| BaseDamage | int | 1–2 | Core Rules (1 standard, 2 for "severe" interactions) | Raw HP cost before Heat scaling |

**Expected output range**: 1–4 HP per instant event. **Example**: standard collision
(BaseDamage=1) with Heat rank 4 active → 2 HP. Ice Vortex hit (BaseDamage=2) with Heat rank
4 active → 4 HP. **Edge case**: Heat ranks stack (`economy-progression.md`), so this checks
whether rank 4 is among the active stacked ranks, not that exactly rank 4 alone is selected.

### Continuous Exposure Damage (Fire Tornado only, currently)
Implemented as a tick, not fractional continuous damage: apply 1 HP (2 with Heat rank 4)
every full second spent inside the hazard zone.

**Example**: 2.5 seconds inside a Fire Tornado, no Heat modifiers: ticks fire at t=1.0s and
t=2.0s → 2 HP total; the remaining 0.5s doesn't trigger a third tick.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|-------------------|-----------|
| Damage would take HP below 0 | Clamp at 0 (Destroyed) | No negative HP; overkill doesn't matter once destroyed |
| Two damage sources hit in the same frame (e.g. ramming failure + Fire Tornado tick) | Both apply independently and stack additively | Simplest, most predictable rule — no special-casing simultaneous hits |
| Repair Kit used at full HP | No effect, pickup is still consumed | Matches other pickups in `economy-progression.md` that don't special-case "already at cap" |
| A hit that would destroy the vehicle occurs while Survivor perk is unused this run | Perk consumes itself, vehicle restores to full Healthy instead of being destroyed, run continues | Perk description says "one free instant field repair," which only makes sense as a destruction-prevention, not a mid-HP heal |
| EMP status effect (momentum-only) active at the same time as Critical stage (which also forces momentum-only) | No stacking issue — momentum-only is a boolean flag, not additive; whichever source is active, movement is momentum-only until both clear | Avoids needing a "double momentum-only" state that doesn't exist |
| Heavy Bumper perk equipped, vehicle rams while already in Critical stage | Ramming self-damage is still fully zeroed | The perk negates that damage source outright; current HP stage doesn't reduce its effect |
| Anchor Harpoon deployed (wind-suction immunity) while also taking direct hull/heat damage from the same hazard | Wind-suction damage is blocked; hull/heat damage from direct contact still applies | Immunity is source-specific (wind suction only), not a general damage shield |
| Vehicle destroyed mid-event-window (e.g. mid Ram & Unblock) | Event expires immediately per `event-system.md`'s existing edge case — no partial credit | Already defined by that doc; this system just triggers the destruction that causes it |
| Prototype archetype's "Varies" Max HP | Each individual Prototype unlock must define its own Max HP (1–5) at content-authoring time | Not a runtime edge case — a content requirement for whoever designs each Prototype unlock |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|----------------------|
| `PlayerVehicle.cs` | This depends on it | Needs collision/ramming/hazard-zone-entry events; needs a new health field added (doesn't exist today) |
| `VehicleData.cs` (SO) | This depends on it | Needs a Max HP value added per archetype, derived from Armor rating |
| `event-system.md` | Bidirectional | This doc implements `event-system.md`'s Ramming Impact Threshold (1 HP) and `VehicleMaxHP` knob rather than redefining them; `event-system.md` already lists vehicle damage as a dependency (added during design review) |
| `economy-progression.md` | Bidirectional | Repair Kit, Survivor perk, Heavy Bumper, Anchor Harpoon/Clamps, and EMP Deflector Shield all hook into this system — that doc's Dependencies section now points back here |
| `session-modes.md` | Depended on by | Destroyed state triggers the existing early-end + 80% score bank behavior |
| PiP Camera (S4-01, not yet built) | Soft / provisional | Critical stage's viewfinder glitch is cosmetic feedback on that system once it exists — not a hard blocker for the rest of this system |
| HUD / UI | Depended on by (future) | Needs to display current HP — see UI Requirements below |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|---------------|------------|---------------------|---------------------|
| `StandardCollisionDamage` | 1 HP | 1–2 | Runs end sooner from routine collisions | Damage feels trivial, undercuts risk/reward |
| `SevereCollisionDamage` (Ice Vortex) | 2 HP | 2–3 | "Severe" interactions become genuinely scary | Loses its intended weight vs. standard collisions |
| `ContinuousExposureDamagePerSecond` (Fire Tornado) | 1 HP/sec | 1–2 | Lingering in fire becomes punishing fast | Encourages reckless dwelling in fire zones |
| `HeatRank4Multiplier` (Brittle Chassis) | 2.0× | 1.5–3.0 | Heat 4+ becomes a much harder difficulty spike | Heat 4 loses its "brittle" identity vs. other ranks |
| `DamagedThresholdFraction` | ⅔ | 0.5–0.8 | Vehicles show wear sooner (more time spent Damaged) | Vehicles feel invincible longer before any visible damage |
| `CriticalThresholdFraction` | ⅓ | 0.2–0.4 | More time spent in the high-penalty Critical stage | Critical stage becomes a brief, easy-to-miss window |
| Max HP per archetype | 1–5 (see `VehicleData.cs`) | 1–5 | *(owned by vehicle archetype data, not this system — listed for cross-reference)* | |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|-----------------|-----------------|----------|
| Entering Damaged stage | Frame deforms slightly, scratch/dent decals appear | Metal crunch/dent sound | Must Have |
| Entering Critical stage | Cracked/glitched overlay on PiP viewfinder, engine smoke particles | Sputtering engine loop, glass-crack sting | Must Have |
| Instant damage hit (any stage) | Brief screen shake / impact flash | Impact sound matching hazard type | Should Have |
| Continuous exposure tick (Fire Tornado) | Heat haze overlay, accumulating scorch decal | Crackling fire loop while inside zone | Should Have |
| Destroyed | Vehicle breakdown/explosion animation, brief freeze beat | Explosion/dead-engine sound, silence beat before end screen | Must Have |
| Repair Kit used | Spark/repair particle effect, damage decals partially clear | Mechanical repair chime | Nice to Have |

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|--------------|-------------------|---------------------|-----------|
| Current HP / Max HP | HUD, persistent (near score) | On change (event-driven) | Always visible during a run |
| Damage stage transition | Brief on-screen text/icon flash ("DAMAGED" / "CRITICAL") | On stage transition only | Momentary, not persistent |
| Active status effects (EMP momentum-only) | Icon near HP display | On change | Only while active |
| Survivor perk availability | Small badge/icon | Cleared after single use | Only if perk is equipped |

## Acceptance Criteria

- [ ] Vehicle at full HP shows no penalty of any kind
- [ ] Vehicle enters Damaged stage exactly at `floor(MaxHP × ⅔)` HP; turning radius is measurably reduced
- [ ] Vehicle enters Critical stage exactly at `floor(MaxHP × ⅓)` HP; movement becomes momentum-only
- [ ] Vehicle is destroyed at exactly 0 HP; session ends early and 80% of score is banked (per `session-modes.md`)
- [ ] A 1 Max HP vehicle (Motorcycle) goes directly from Healthy to Destroyed on its first hit, never visibly entering Damaged or Critical
- [ ] Repair Kit restores exactly 1 HP, capped at Max HP (no overheal)
- [ ] Survivor perk prevents exactly one destruction per run, restores to full HP, and has no effect on any subsequent destruction that run
- [ ] Cataclysm Heat rank 4 doubles both instant and continuous (per-tick) damage amounts
- [ ] Heavy Bumper zeroes ramming self-damage regardless of current damage stage
- [ ] Anchor Harpoon / Anchor Clamps block wind-suction-sourced damage specifically, without blocking other simultaneous damage from the same hazard
- [ ] Performance: damage evaluation is event-driven (collision/tick callbacks), not polled every frame

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|------------|
| Does the Damaged/Critical merge (2 stages instead of 3) hold up once playtested, or does damage progression feel too abrupt? | Game Designer | Post-implementation playtest | Open |
| Should NPCs and structures share this same HP/stage model (per the "Living Damage Economy" pillar), or get their own simpler system? | Game Designer | Before Season 2 scoping | Open |
| Exact Max HP for each individual Prototype archetype unlock | Economy Designer | Before Tree 1 content is finalized | Open |
| Should Critical-stage viewfinder glitch reduce actual photo score, or stay purely cosmetic as currently specced? | Game Designer | Before S4-01 PiP camera implementation | Open — currently specced as cosmetic only |
