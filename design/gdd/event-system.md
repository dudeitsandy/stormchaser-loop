# Procedural Event System

**Status:** Design — not yet implemented
**Target Sprint:** Sprint 5–6
**Owner:** Game Designer

---

## Overview

As a tornado moves across the map, it procedurally generates time-critical event
windows in its path. Events are situational — the player chooses to engage or ignore
each one. Engaging is always riskier and always more rewarding. Events create the
"Twister moments" that make sessions into stories.

---

## Player Fantasy

"I had half a second to decide — go through the barn and get the shot, or play it
safe and lose the bonus. I went through the barn."

---

## Detailed Rules

### Event Generation
- Events spawn in the tornado's projected path (2–4 seconds ahead of current position)
- Event density scales with EF rating: EF0 = rare, EF5 = frequent
- Maximum 2 active events simultaneously
- Events expire if not engaged within their time window (8–12 seconds)

### Event Types

#### NPC Rescue
- A civilian vehicle spawns on a collision path with the tornado
- Player intercepts by driving within 15 units of the NPC
- NPC vehicle steers away from tornado path
- **Reward:** Flat bonus score + "Saved" badge on run summary
- **Risk:** Intercept path may take player into debris field
- **Ignore penalty:** None to score; NPC is "lost" (tracked in run summary)

#### Debris Field
- Flying objects (fence posts, lumber, vehicles) scatter across a corridor
- Player can drive through the corridor toward the tornado (risky shortcut)
- Or drive around the edge (safe, costs ~8 seconds)
- **Reward (through):** Tornado distance dramatically reduced, better photo angle
- **Risk:** Each debris object has a collision chance; hit = vehicle damage
- **Vehicle damage model:** 3 hits = destroyed (session ends, partial score)

#### Structure Gap (Barn / Building)
- A structure spawns between player and tornado with a driveable gap
- Gap width: 1.5× vehicle width (tight but passable)
- **Reward:** Shortcut to tornado + "Daredevil" score multiplier (1.5×) on next photo
- **Risk:** Miss the gap = collision damage; structure may partially collapse
- **Visual:** Gap is highlighted with a dust/light shaft — readable at speed

#### Bridge / Bottleneck
- Narrow passage with tornado visible directly on the other side
- Going through guarantees extreme close-up photo distance (high score ceiling)
- **Reward:** Guaranteed sub-10-unit photo distance
- **Risk:** If tornado shifts path, player is trapped at point-blank range
- **Tell:** Tornado wobble animation telegraphs potential path shift

---

## Formulas

### Event Spawn Probability (per 10 seconds)
`SpawnChance = BaseChance + (EFRating × 0.08)`
- EF0: 10% / 10s
- EF3: 34% / 10s
- EF5: 50% / 10s

### Debris Hit Chance
`HitChance = 0.3 - (PlayerSpeed × 0.005)`
- Faster driving = smaller collision window = lower hit chance
- Reward for committing fully to the gap

### Daredevil Multiplier Decay
- Structure Gap multiplier (1.5×) applies to next photo only
- Stacks with EF strength: EF5 + Daredevil = potentially 6× base score

---

## Edge Cases

- Two events spawn simultaneously on opposite sides: player must choose one
- Player enters debris field while NPC rescue is active: both resolve independently
- Vehicle at 1 HP enters debris field: high-tension moment, any hit ends run
- Tornado shifts path during Bridge event: trigger a "close call" camera shake + audio sting

---

## Dependencies

- TornadoController.cs — path projection needed for event spawn positioning
- VehicleData ScriptableObject — damage model (add HP field)
- SessionTimer.cs — event expiry timing
- NPC system (see npc-system.md) — NPC Rescue event type
- Audio system — event trigger sounds, near-miss stings
- Camera system — close-call shake, FOV pulse

---

## Tuning Knobs

- `BaseEventSpawnChance`: default 0.10 per 10s
- `EFSpawnModifier`: default 0.08 per EF level
- `EventExpiryWindow`: default 10s
- `DebrisHitChanceBase`: default 0.30
- `DaredevilMultiplier`: default 1.5×
- `VehicleMaxHP`: default 3 hits
- `PartialScoreOnDestruction`: 0.8 (inherited from session-modes.md)

---

## Acceptance Criteria

- [ ] Events spawn in tornado path within correct time window
- [ ] EF5 tornado generates noticeably more events than EF1
- [ ] Each event type has distinct visual read at driving speed
- [ ] Player can complete a full Chase session with zero event engagement (events are opt-in)
- [ ] Daredevil multiplier applies correctly to next photo score only
- [ ] Vehicle destruction triggers session end with partial score
