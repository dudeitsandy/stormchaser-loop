# Vision 1.0 — Doomsday

**Status:** Approved concept  
**Date:** 2026-04-21 (Updated 2026-08-28)  
**Author:** Andy Styx / Ghostweave Games  
**Target Platforms:** PC (Steam / Steam Deck) for Early Access. Nintendo Switch is a
possible post-launch port, contingent on Steam EA performance — not a Season 1 commitment.
See `ADR-0001` for the authoritative platform decision.  

---

## Elevator Pitch

A chaotic disaster roguelite where you drive a customized vehicle into world-ending events, scoring points for documenting disasters, saving civilians, pulling off stunts, and influencing massive inter-entity encounters — while your car takes real damage and the world actively falls apart around you.

*"Twister meets Crazy Taxi & Pokémon Snap in an escalating roguelite apocalypse."*

---

## Name & Brand

**Doomsday**

Working project name is `StormChaserLoop3D` (Unity project folder). Brand name is **Doomsday**.

**Trailer audio reference:** Nero — "Doomsday" (Welcome Reality, 2011). The drop timing maps directly to a high-speed vehicle powersliding across an earthquake fault line into an EF5 fire tornado.

---

## Core Pillars

### 1. Kinetic Chaos
Movement has Rocket League / Burnout energy: momentum-based, physics-forward, trick-capable. Drifting through debris, ramping off wreckage, threading through collapsing structures, and skimming past disaster funnels — motion is expressive, tactile, and scorable. This is a draw, not a gimmick.

**Implementation note:** The Season 1 build uses arcade direct-velocity as a placeholder. A Vehicle Feel pass (physics overhaul + stunt detection) is planned for late Season 1 Sprint 6-7, shipping alongside the style scoring system. These two systems are tightly coupled and should land together.

### 2. Disaster Stacking & "Disaster Alchemy"
Multiple disaster types coexist in a single run. They interact systemically: a wildfire drawn into an EF5 becomes a roaring Fire Tornado; an earthquake cracks roads into natural stunt ramps; flash floods destroy tire traction; an EMP disrupts vehicle systems. Disaster combinations are emergent content, not scripted sequences.

### 3. Dual-Axis Scoring
Every action has a **Documentation score** (observe/photograph/record the event) and an **Intervention score** (clearing roadblocks, saving NPCs, deploying sensor pods, collateral prevented, objects pushed). Style and stunt execution multiply both. Neither axis is mandatory — high-risk documenting a peak-phase disaster encounter is a completely valid playstyle.

### 4. Living Damage Economy
Everything takes damage: your vehicle, NPCs, structures. Part of the skill ceiling is knowing when to absorb hits for style points vs. protecting assets. A battered vehicle run that saved 40 NPCs beats a pristine car with zero intervention. Damage is expressive, not just punishing.

### 5. Roguelite Identity & Cataclysm Heat
Each run is shaped by your meta-unlocked loadout, procedural event windows, and per-run modifiers (positive and negative). No two runs feel identical. The meta layer rewards mastery, unlocks garage gear, and challenges players with escalating **Cataclysm Heat Levels**.

---

## Platform & Feel Target: PC (Steam)

- **Visual Style**: High-contrast, low-poly 3D with a retro arcade post-processing stack (pixel dithering, CRT scanlines, bloom, teal-orange grade).
- **Performance Budget**: Locked 60 FPS on PC, 30 FPS on Steam Deck (per `ADR-0001` and `technical-preferences.md`).
- **Bite-Sized Sessions**: 90-second Sprint and 5–7.5 minute Chase modes optimized for instant pick-up-and-play — this design goal holds regardless of platform.

### Post-Launch Consideration: Nintendo Switch
If Steam EA performs well, a Switch port is worth revisiting. Not designed against yet —
no Season 1 system should assume Switch hardware (HD Rumble, gyro aiming, docked/handheld
perf splits). If this becomes real, it needs its own ADR before any Switch-specific work
starts, since it requires a separate Unity platform module and Nintendo dev-kit access
that don't exist today.

---

## Map Architecture: The Modular Biome Hybrid

Maps use **handcrafted point-of-interest (POI) chunks stitched together with procedural road networks and hazard grids**.

### 4 Signature Biomes

```
┌────────────────────────────────────────────────────────────────────────┐
│ 1. THE HEARTLAND (Tornado Alley)                                       │
│    • Flat agricultural drag strips, billboard jump ramps               │
│    • Barns with driveable collapse corridors, toppling grain silos    │
├────────────────────────────────────────────────────────────────────────┤
│ 2. CANYON FAULTLINE (Seismic Badlands)                                 │
│    • Multi-elevation plateaus, sandstone natural ramps                 │
│    • Dynamic earthquake chasms requiring boost jumps, blinding dust    │
├────────────────────────────────────────────────────────────────────────┤
│ 3. COASTAL GATEWAY (The Floodplains)                                   │
│    • Raised highway viaducts, drawbridges, coastal fog                │
│    • Washed-out lowlands, floating debris hazards                      │
├────────────────────────────────────────────────────────────────────────┤
│ 4. METRO SUBURBS (Evacuation Zone)                                     │
│    • Tightly packed streets, powerline arcs, collapsing overpasses    │
│    • Underground stormwater tunnels as high-risk shortcuts             │
└────────────────────────────────────────────────────────────────────────┘
```

---

## The Run Structure

```
Pre-run:   Select vehicle + loadout → accept/reroll modifiers → set Cataclysm Heat → drop in
During:    Dynamic objectives spawn → disasters escalate and interact → stunts & photo windows open
End:       Tally: documentation + intervention + style + modifier bonuses → Storm Dollars + Newspaper Cover
Meta:      Spend Storm Dollars in HQ Garage on unlocks → next run escalates
```

Sprint sessions run a fixed 90 seconds; Chase sessions run 5–7.5 minutes depending on
storm front size (see `session-modes.md` for the exact formula). Short enough to
iterate, long enough for a dramatic arc.

---

## Disaster System

All disasters implement a **DisasterEntity** interface:

- **Threat Class** (I–V) — scales size, damage output, and score value; analogous to EF scale
- **Behavior Pattern** — move / charge / stationary / patrol / swarm / wander
- **Phase Arc** — building → peak → dissipating (or: spawning → rampaging → staggered)
- **Interaction Tags** — defines behavior when contacting other disaster entities

### Disaster Roster by Season

**Season 1 — Storm Season** *(current build)*
- Tornado (EF0–EF5) — core mechanic, already implemented
- Wildfire — spreading, wind-influenced spread direction
- Hailstorm — area denial, visibility reduction

**Season 2 — Ground Zero**
- Earthquake — shockwave pulses + terrain deformation / fissure ramps
- Tsunami — wall of water, elevation matters
- Sinkhole — sudden terrain collapse, vehicle trap risk

**Season 3 — Kaiju Rising**
- Kaiju — boss-scale entity, destructible phases, push-off-map objective
- Rogue Mech — EMP attacks, mechanical weak points, military escort context
- Swarm (insect/nanobots) — density-based, engulfs and damages

**Season 4 — First Contact** *(TBD)*
- Interdimensional Rift — spawns randomized entities, reality distortion field
- Alien Invasion — coordinated attack patterns, abduction mechanics

### Disaster Interaction Matrix & Alchemy

| Season | Pairing | Effect | Model | Gameplay Impact |
|--------|---------|--------|-------|-----------------|
| S1 | Tornado + Wildfire | Tornado absorbs fire → Fire Tornado | Merge | ThreatClass +1. Vehicle takes heat damage; flying debris is ignited. |
| S1 | Tornado + Hailstorm | Tornado picks up hail → Ice Vortex | Modify | Severe vehicle hull damage; PiP viewfinder glitched with heavy static. |
| S1 | Two Wildfires (adjacent) | Merge into larger wildfire front | Merge | Expanded radius and higher ThreatClass. |
| S1 | Wildfire + Hailstorm | Fire partially suppressed; Steam Cloud | Modify | Instant dense steam reduces visibility; requires radar scanner. |
| S1 | Two Tornadoes (close) | Merge into super-vortex or repel | Merge/Mod | Massive suction pull or violent crosswind buffeting. |
| S2 | Tornado + Earthquake | Fissure Slingshot | Modify | Fault line launches terrain into stunt ramps; vortex path abruptly shifts. |
| S2 | Earthquake + Flood | Dam Collapse | Event | Massive tidal surge across low ground; forces scramble to high roads. |
| S2 | Tornado + Kaiju | Kaiju movement destabilized | Modify | Debris becomes high-velocity projectiles against the Kaiju. |
| S2 | EMP Mech + Player Vehicle | Systems temporarily offline | Modify | Momentum-only drift movement until reboot. |
| S2 | Tsunami + Wildfire | Fire suppressed on contact | Modify | Massive steam explosion; instant area denial. |
| S3 | Kaiju + Earthquake | Kaiju stumbles during shockwave | Modify | Brief stagger window opening high-value photo/attack angle. |

### Interaction Models

Two distinct models govern how disasters interact:

**Merge** — two entities collide and produce a third. Both source entities despawn; a new DisasterEntity of higher ThreatClass spawns at the collision point (e.g. Tornado + Wildfire → Fire Tornado).

**Modify** — two entities in proximity alter each other's behavior but both persist. Each gains a behavior modifier while in range.

**Architecture Note:** Merged entities are treated as new subclasses with their own `DisasterData`. Cleaner serialization, cleaner scoring, and specialized visual assets.

---

## Vehicle System

Vehicles are modular. Each slot is independently upgradeable and swappable at the meta layer:

```
CHASSIS      — base stats: weight, handling, damage threshold
ENGINE       — top speed, acceleration curve, fuel/power type (gasoline, electric, nuclear)
ARMOR        — damage absorption, visual degradation model
PAYLOAD      — offensive/utility: blades, grapple, rockets, fireworks, water cannon, sensor launcher
WHEELS       — traction, terrain bonus, trick capability, drift control
ELECTRONICS  — camera quality, radar scanner, shield, EMP deflector, nitrous boost, anchor harpoon
```

### Vehicle Archetypes (Meta-Unlocks)

Flavor name here; mechanical unlock (cost, exact stat deltas) lives in
`economy-progression.md` Tree 1 — this table is the fiction/role layer, that doc is
the numbers layer. Names below match 1:1 with that tree's unlock names.

| Archetype | Economy Tree 1 Unlock | Speed | Armor | Trick | Role |
|-----------|------------------------|-------|-------|-------|------|
| **Pickup Truck** (default) | Pickup Truck | ★★★ | ★★★ | ★★ | Starting vehicle, balanced |
| **Sports Car / Interceptor** | Barn Runner / Interceptor | ★★★★★ | ★ | ★★★★★ | Style runs, drift scoring, documentation |
| **Armored SUV / Rig** | Storm Rig ("The Tank") | ★★★ | ★★★★ | ★★ | NPC rescue, obstacle ramming, suction resistance |
| **Monster Truck** | *(not yet in economy tree — needs adding)* | ★★ | ★★★★★ | ★★★ | Terrain immunity, crushing debris, high jumps |
| **Motorcycle** | *(not yet in economy tree — needs adding)* | ★★★★★ | ✗ | ★★★★★ | Extreme expression, hyper-fragile |
| **APC** | *(not yet in economy tree — needs adding)* | ★★ | ★★★★★ | ★ | Squad carrier, mobile fortress, multi-rescue |
| **Scout Buggy** | Rally Truck / Buggy | ★★★★ | ★★ | ★★★★ | High clearance, stunt ramps, agile framing |
| **Prototype** | Hover Prototype | Varies | Varies | Varies | Unique per-unlock abilities (e.g. hover/jump jets) |

Vehicle damage is **visual and mechanical**: bent frame reduces turning radius, broken windshield glitched documentation viewfinder, disabled engine forces momentum-only movement.

---

## Scoring Philosophy

**Four axes:**
1. **Documentation** — observe/record disasters at risk-appropriate distances and optimal framing
2. **Intervention** — NPCs saved, roadblocks cleared, sensor pods deployed, structures protected, damage dealt
3. **Style** — stunt execution (Twister jumps, barn punches), trick combos, drift framing, dramatic near-misses
4. **Objectives** — dynamic per-run bounties and mission windows

Style is a **multiplier layer**, not additive. All four axes firing simultaneously during a single moment = peak run expression (e.g., jumping a burning bridge gap over an EF5 while taking a point-blank snapshot as a rescued civilian truck escapes below).

---

## Dynamic Objectives & Stunts

Objectives spawn during runs with time windows. They stack, forcing real-time triage.

This table matches `event-system.md`'s taxonomy exactly for Season 1 (Sprint 5-6). Two
older ideas (Protect, Push) are kept below but flagged — they weren't carried into
`event-system.md`'s spec and need a decision: write them up for a later season, or drop them.

| Type | Description |
|------|-------------|
| **Rescue** | NPC group pinned by disaster; intercept/escort before threat arrives |
| **Ram & Unblock** | Smash fallen trees or wrecked obstacles to clear civilian escape routes |
| **Sensor Deploy ("Dorothy")**| Drive ahead of projected disaster path and deploy scientific sensor pods |
| **Bridge / Bottleneck Trap** | Narrow passage forces a guaranteed close-range photo, but a path shift traps the player at point-blank range |
| **Stunt: Twister Jump** | Launch off a ramp or collapsed structure *over* a moving disaster |
| **Stunt: Barn Punch** | Blast through front and back doors of a barn at 70+ MPH without hitting walls |
| **Stunt: Drift Framing** | Hold a 90° power-slide while keeping the camera locked dead-ahead on the vortex (blocked on the Sprint 6-7 Vehicle Feel pass — see `event-system.md`) |
| **Dynamic Bounties** | TV News Network demands (e.g., *"Airborne Tractor"*, *"Dual Cataclysm Shot"*) |
| **Document First** | First to photograph entity at peak Threat Class gets major score bonus |
| **Protect** *(not in event-system.md yet)* | Specific structure has high point value if it survives the run — Season 1 candidate, needs spec |
| **Push** *(not in event-system.md yet)* | Kaiju/mech near map edge; deal threshold damage to push it off — Season 3+ only, requires Kaiju roster |

---

## Per-Run Modifiers & Cataclysm Heat

Offered 2-of-3 positive at run start. One mandatory negative drawn randomly.

**Positive examples:**
- *Overclock* — vehicle top speed +40%; overheats after 90s continuous use
- *Disaster Magnet* — disasters trend toward your position (risk up, score density up)
- *Golden Hour* — all documentation scores doubled for first 2 minutes
- *Adrenaline Loop* — each NPC saved adds 5s to run timer
- *Blade Sharpened* — blade payload deals 2× damage this run
- *Anchor Clamps* — harpoon anchors vehicle in place, granting 100% wind suction immunity

**Negative examples:**
- *Bald Tires* — no traction bonus on any surface
- *Cracked Lens* — documentation score capped at 70%
- *Hostile AI* — the mech targets you specifically
- *Aftershock* — earthquake pulses every 30s regardless of active disasters
- *NPC Surge* — 3× normal civilian population; rescue objectives scale up

### Cataclysm Heat Levels (Endgame Mastery)
Players stack optional crisis conditions for higher Storm Dollar multipliers. Full 5-rank
table, stacking rules, and formulas live in `economy-progression.md` — don't duplicate here.

---

## "Front Page" Newspaper Cover

At the end of every run, the game generates a mock retro newspaper cover from the run's
best photo. Full spec (masthead, headline generation, export flow) lives in
`economy-progression.md` — don't duplicate it here.

---

## Season Structure

| Season | Theme | New Disasters | New Biome | New Vehicles |
|--------|-------|--------------|-----------|-------------|
| 1 | Storm Season | Tornado, Wildfire, Hailstorm | The Heartland | Sports Car, SUV, Buggy |
| 2 | Ground Zero | Earthquake, Tsunami, Sinkhole | Canyon Fault & Coast | Monster Truck, APC |
| 3 | Kaiju Rising | Kaiju, Rogue Mech, Swarm | Metro Suburbs & Port | Motorcycle, Prototype |
| 4 | First Contact | Alien Invasion, Dimensional Rift | Deep Crater | Hovercraft / Experimental |

---

## Systems Architecture — Build Now vs. Later

| Current System | Sprint 3–4 Scope | Vision Extension |
|---------------|------------------|------------------|
| `TornadoController` | Tornado only | → `DisasterEntity` abstract base class |
| `TornadoData` SO | EF scale + move speed | → `DisasterData` with Threat Class, Behavior, Interaction Tags |
| `TornadoSpawner` | Single type | → `DisasterSpawner` with typed roster |
| `ScoringSystem` (static) | Photo aim + distance | → 4-axis system + style multiplier |
| `VehicleData` SO | Flat stat block | → `VehicleLoadout` with module slots |
| `SessionTimer` | Simple countdown | → `RunManager` with objective tracking + modifier state |

---

## Open Questions (Not Blocking Season 1)

- Push-off-map objective: fixed map bounds or dynamic edge detection?
- Multiplayer / Leaderboard Ghost runs? (Asynchronous time trial / ghost cars)
- Gyro Aiming sensitivity curves for handheld vs. Pro Controller on Switch.
