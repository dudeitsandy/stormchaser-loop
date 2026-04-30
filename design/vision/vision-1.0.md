# Vision 1.0 — Doomsday

**Status:** Approved concept  
**Date:** 2026-04-21  
**Author:** Andy Styx / Ghostweave Games

---

## Elevator Pitch

A chaotic disaster roguelite where you drive a customized vehicle into world-ending events,
scoring points for documenting disasters, saving civilians, pulling off stunts, and influencing
massive inter-entity encounters — while your car takes real damage and the world actively falls
apart around you.

---

## Name

**Doomsday**

Working project name is `StormChaserLoop3D` (Unity project folder). Brand name is Doomsday.

**Trailer audio reference:** Nero — "Doomsday" (Welcome Reality, 2011). The drop timing
maps directly to a kaiju-tornado-mech chaos montage. Use as hype reel target reference.

---

## Core Pillars

### 1. Kinetic Chaos
Movement has Rocket League energy: momentum-based, physics-forward, trick-capable.
Drifting through debris, ramping off wreckage, threading between a kaiju's legs — motion
is expressive and scorable. This is a draw, not a gimmick.

**Implementation note:** The Season 1 build uses arcade direct-velocity as a placeholder.
A Vehicle Feel pass (physics overhaul + stunt detection) is planned for late Season 1
Sprint 6-7, shipping alongside the style scoring system. These two systems are tightly coupled
and should land together.

### 2. Disaster Stacking
Multiple disaster types coexist in a single run. They interact: a kaiju walking through
a tornado gets pulled sideways; a mech's EMP disrupts your vehicle systems; flooding makes
traction unpredictable. Disaster combinations are emergent content, not scripted sequences.

### 3. Dual-Axis Scoring
Every action has a **Documentation score** (observe/photograph/record the event) and an
**Intervention score** (damage dealt, NPCs saved, collateral prevented, objects pushed).
Style multiplies both. Neither axis is mandatory — high-risk documenting a peak-phase kaiju
encounter is a valid strategy.

### 4. Living Damage Economy
Everything takes damage: your vehicle, NPCs, structures. Part of the skill ceiling is knowing
when to absorb hits for style points vs. protecting assets. A battered vehicle run that
saved 40 NPCs beats a pristine car with zero intervention. Damage is expressive, not just
punishing.

### 5. Roguelite Identity
Each run is shaped by your meta-unlocked loadout and per-run modifiers (positive and
negative). No two runs feel identical. The meta layer rewards mastery and unlocks access
to escalating chaos.

---

## The Run Structure

```
Pre-run:   Select vehicle + loadout → accept/reroll modifiers → drop in
During:    Dynamic objectives spawn → disasters escalate and interact → style windows open
End:       Tally: documentation + intervention + style + modifier bonuses → Storm Dollars
Meta:      Spend Storm Dollars on unlocks → next run escalates
```

Runs target 3–7 minutes. Short enough to iterate, long enough for a dramatic arc.

---

## Disaster System

All disasters implement a **DisasterEntity** interface:

- **Threat Class** (I–V) — scales size, damage output, and score value; analogous to EF scale
- **Behavior Pattern** — move / charge / stationary / patrol / swarm
- **Phase Arc** — building → peak → dissipating (or: spawning → rampaging → staggered)
- **Interaction Tags** — defines behavior when contacting other disaster entities

### Disaster Roster by Season

**Season 1 — Storm Season** *(current build)*
- Tornado (EF0–EF5) — core mechanic, already implemented
- Wildfire — spreading, wind-influenced spread direction
- Hailstorm — area denial, visibility reduction

**Season 2 — Ground Zero**
- Earthquake — shockwave pulses + terrain deformation
- Tsunami — wall of water, elevation matters
- Sinkhole — sudden terrain collapse, vehicle trap risk

**Season 3 — Kaiju Rising**
- Kaiju — boss-scale entity, destructible phases, push-off-map objective
- Rogue Mech — EMP attacks, mechanical weak points, military escort context
- Swarm (insect/nanobots) — density-based, engulfs and damages

**Season 4 — First Contact** *(TBD)*
- Interdimensional Rift — spawns randomized entities, reality distortion field
- Alien Invasion — coordinated attack patterns, abduction mechanics
- ???

### Disaster Interaction Examples

| Season | Pairing | Effect | Model |
|--------|---------|--------|-------|
| S1 | Tornado + Wildfire | Tornado absorbs fire → Fire Tornado; merged entity, +1 ThreatClass | Merge |
| S1 | Tornado + Hailstorm | Tornado picks up hail → ice debris radius; player damage risk increases | Modify |
| S1 | Two Wildfires (adjacent) | Merge into larger wildfire with expanded radius and higher ThreatClass | Merge |
| S1 | Wildfire + Hailstorm | Fire partially suppressed; steam cloud reduces visibility in overlap zone | Modify |
| S1 | Two Tornadoes (close) | Merge into larger entity or repel based on rotation direction | Merge |
| S2 | Tornado + Kaiju | Kaiju movement destabilized; debris becomes projectiles | Modify |
| S2 | EMP Mech + Player Vehicle | Systems temporarily offline; momentum-only movement | Modify |
| S2 | Tsunami + Wildfire | Fire suppressed on contact; steam cloud reduces visibility | Modify |
| S3 | Kaiju + Earthquake | Kaiju stumbles during shockwave; brief stagger window | Modify |

### Interaction Models

Two distinct models govern how disasters interact:

**Merge** — two entities collide and produce a third. Both source entities despawn;
a new DisasterEntity of higher ThreatClass spawns at the collision point. The fire
tornado is the canonical Season 1 example: Tornado + Wildfire → Fire Tornado,
ThreatClass upgraded by +1, photographable as a single entity.

**Modify** — two entities in proximity alter each other's behavior but both persist.
Each gains a behavior modifier while in range. The kaiju destabilized by a tornado
is the canonical Season 3 example.

**Open architectural question (resolve before Sprint 4–5):**
Is a Fire Tornado a new DisasterEntity subclass with its own DisasterData SO, or a
TornadoController in a modified state? This decision affects three things:
- How it's photographed (which ThreatClass does PhotoTrigger read?)
- What asset it references (new SO or runtime mutation of existing one?)
- How it despawns (does it revert to source entities or simply die?)

Recommendation: treat merged entities as new subclasses with their own DisasterData.
Cleaner serialization, cleaner scoring, and the fire tornado *is* genuinely a
different thing — not a tornado that happens to be on fire.

---

## Vehicle System

Vehicles are modular. Each slot is independently upgradeable and swappable at the meta layer.

```
CHASSIS      — base stats: weight, handling, damage threshold
ENGINE       — top speed, acceleration curve, fuel type (gasoline, electric, nuclear)
ARMOR        — damage absorption, visual degradation model
PAYLOAD      — offensive/utility: blades, grapple, rockets, fireworks, water cannon
WHEELS       — traction, terrain bonus, trick capability
ELECTRONICS  — camera quality, scanner, shield, EMP, boost
```

### Vehicle Archetypes (meta-unlocks)

| Archetype | Speed | Armor | Trick | Role |
|-----------|-------|-------|-------|------|
| Sports Car | ★★★★★ | ★ | ★★★★★ | Style runs, documentation |
| Armored SUV | ★★★ | ★★★★ | ★★ | NPC rescue, intervention |
| Monster Truck | ★★ | ★★★★★ | ★★★ | Terrain immunity, impact |
| Motorcycle | ★★★★★ | ✗ | ★★★★★ | Extreme expression, fragile |
| APC | ★★ | ★★★★★ | ★ | Squad carrier, mobile fortress |
| Prototype | Varies | Varies | Varies | Unique per-unlock ability |

Vehicle damage is **visual and mechanical**: bent frame reduces turning radius, broken
windshield reduces documentation score, disabled engine forces momentum-only movement.

---

## Scoring Philosophy

**Four axes:**

1. **Documentation** — observe/record disasters at risk-appropriate distances
2. **Intervention** — NPCs saved, structures protected, damage dealt to hostile entities
3. **Style** — stunt execution, trick combos, dramatic near-misses, combo chains
4. **Objectives** — dynamic per-run goals (see below)

Style is a **multiplier layer**, not additive. All four axes firing simultaneously during
a single moment = peak run expression (e.g., blade-charging a mech's leg while drifting
through tornado debris as NPCs sprint to evac behind you).

Negative scoring: civilian casualties, friendly fire on military assets, abandoning
active timed objectives.

---

## Dynamic Objectives

Objectives spawn during runs with time windows. They stack. You will always have more
options than time — triage is part of the skill ceiling.

| Type | Description |
|------|-------------|
| **Rescue** | NPC group pinned by disaster; extract before threat arrives |
| **Intercept** | Disaster on collision course with population center; redirect or delay |
| **Stunt Window** | A specific trick is worth 5× for 20 seconds |
| **Document First** | First to photograph entity at peak Threat Class gets score bonus |
| **Push** | Kaiju/mech near map edge; deal threshold damage to push it off |
| **Chain** | Rapid-fire objective sequence; tests triage speed |
| **Protect** | Specific structure has high point value if it survives the run |
| **Minimize Damage** | Total structure damage below threshold at run end |

---

## Per-Run Modifiers

Offered 2-of-3 positive at run start. One mandatory negative drawn randomly.

**Positive examples:**
- *Overclock* — vehicle top speed +40%; overheats after 90s continuous use
- *Disaster Magnet* — disasters trend toward your position (risk up, score density up)
- *Golden Hour* — all documentation scores doubled for first 2 minutes
- *Adrenaline Loop* — each NPC saved adds 5s to run timer
- *Blade Sharpened* — blade payload deals 2× damage this run
- *Second Wind* — vehicle fully repairs once at 10% health threshold

**Negative examples:**
- *Bald Tires* — no traction bonus on any surface
- *Cracked Lens* — documentation score capped at 70%
- *Hostile AI* — the mech targets you specifically
- *Aftershock* — earthquake pulses every 30s regardless of active disasters
- *NPC Surge* — 3× normal civilian population; rescue objectives scale up

---

## Season Structure

Seasons add disaster tiers, vehicle archetypes, biomes, and objectives. Core systems
are built once and extend cleanly. No season requires a rewrite.

| Season | Theme | New Disasters | New Biome | New Vehicles |
|--------|-------|--------------|-----------|-------------|
| 1 | Storm Season | Tornado, Wildfire, Hailstorm | Great Plains | Sports Car, SUV |
| 2 | Ground Zero | Earthquake, Tsunami, Sinkhole | Coastal City | Monster Truck, APC |
| 3 | Kaiju Rising | Kaiju, Rogue Mech, Swarm | Industrial Port | Motorcycle, Prototype |
| 4 | First Contact | Alien Invasion, Dimensional Rift | ??? | ??? |

---

## Systems Architecture — Build Now vs. Later

The following Season 1 Sprint 3 decisions enable the full vision at zero extra cost:

| Current System | Sprint 3 Scope | Vision Extension |
|---------------|---------------|-----------------|
| `TornadoController` | Tornado only | → `DisasterEntity` abstract base class |
| `TornadoData` SO | EF scale + move speed | → `DisasterData` with Threat Class, Behavior, Interaction Tags |
| `TornadoSpawner` | Single type | → `DisasterSpawner` with typed roster |
| `ScoringSystem` (static) | Photo aim + distance | → 4-axis system + style multiplier |
| `VehicleData` SO | Flat stat block | → `VehicleLoadout` with module slots |
| `SessionTimer` | Simple countdown | → `RunManager` with objective tracking + modifier state |

These are naming and structure decisions only. They cost nothing extra in Sprint 3 but
prevent a rewrite in Season 2.

---

## Open Questions (not blocking Season 1)

- Does each season require a new scene/biome, or does one scene support layered theming?
- Push-off-map objective: fixed map bounds or dynamic edge detection?
- Multiplayer? (Rocket League comparison implies it — probably not Season 1)
- Procedural map generation vs. hand-crafted arenas?
- Is "documentation" always a camera, or does Season 3 introduce a scanner/sensor variant?
