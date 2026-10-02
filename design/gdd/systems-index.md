# Systems Index: Doomsday

> **Status**: Draft
> **Created**: 2026-10-01
> **Last Updated**: 2026-10-01
> **Source Concept**: design/gdd/game-concept.md (derived from design/vision/vision-1.0.md)

---

## Overview

Doomsday is a disaster-chasing roguelite: drive a physics vehicle into escalating
disasters, photograph them, rescue people, pull off stunts, and cash the run out into
meta unlocks. Mechanically that means four clusters. A **driving core** (Vehicle Feel,
Camera & Aim, Wind, Surfaces) carries Pillar 1, Kinetic Chaos, and is the part already
built. A **disaster layer** (Disaster Entity Framework, Tornado, Wildfire, Hailstorm,
Disaster Alchemy) carries Pillar 2, and only Tornado exists today. A **scoring layer**
(Photo Documentation, Stunt & Style, Civilians, Objectives, Four-Axis Scoring, Vehicle
Damage) carries Pillars 3 and 4; Documentation is built, the other three axes are not.
A **run and meta layer** (Run Manager, Loadout, Storm Dollars, Modifiers, Cataclysm
Heat) carries Pillar 5. The world those clusters run in is the ADR-0004 streamed tile
world, which gets its own category because it is neither gameplay nor presentation.

This index was backfilled in Sprint 7 (S7-11): six GDDs and a shipped 0.6.0 build
predate it, so several systems are **Implemented** without a GDD. Those get GDDs by
reverse-documentation (`/reverse-document`), not fresh design.

Scope is **Season 1 (Storm Season, Heartland biome)** only. Season 2–4 disasters,
biomes, and vehicles enter the index when their season is planned.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Input & Controls (inferred) | Core | MVP | Implemented | — | — |
| 2 | Vehicle Feel | Core | MVP | Implemented | design/gdd/vehicle-feel.md | Input & Controls, Surfaces & Terrain Modifiers |
| 3 | Camera & Aim (Storm Cam) | Core | MVP | In Design | design/gdd/vehicle-feel.md (Camera section) | Vehicle Feel |
| 4 | Run Manager & Session Modes | Core | MVP | In Design | design/gdd/session-modes.md | Game Events Bus, Save & Profile |
| 5 | Game Events Bus (inferred) | Core | MVP | Implemented | AGENTS.md (GameEvents contract) | — |
| 6 | Tiled World Streaming | World | MVP | Implemented | docs/architecture/adr-0004-destructible-tiled-world.md | — |
| 7 | Surfaces & Terrain Modifiers (inferred) | World | MVP | Designed | design/gdd/surfaces-terrain-modifiers.md | — |
| 8 | Destructibles & Debris | World | Vertical Slice | Not Started | docs/architecture/adr-0004-destructible-tiled-world.md | Tiled World Streaming, Surfaces & Terrain Modifiers |
| 9 | Roads & POI Chunks (Heartland) | World | Vertical Slice | Not Started | — | Tiled World Streaming, Surfaces & Terrain Modifiers |
| 10 | Wind Field | Gameplay | MVP | Implemented | design/gdd/vehicle-feel.md (F11/F12) | Disaster Entity Framework, Vehicle Feel |
| 11 | Disaster Entity Framework | Gameplay | MVP | Implemented | design/gdd/disaster-entity-framework.md | Run Manager & Session Modes (SessionTimer); Tiled World Streaming (intended, not yet used in code) |
| 12 | Tornado (EF0–EF5) | Gameplay | MVP | Implemented | — | Disaster Entity Framework |
| 13 | Disaster Alchemy (Merge / Modify) | Gameplay | Alpha | Not Started | — | Tornado, Wildfire, Hailstorm |
| 14 | Wildfire | Gameplay | Vertical Slice | Not Started | — | Disaster Entity Framework, Wind Field, Surfaces & Terrain Modifiers |
| 15 | Hailstorm | Gameplay | Alpha | Not Started | — | Disaster Entity Framework, Wind Field, Surfaces & Terrain Modifiers |
| 16 | Photo Documentation | Gameplay | MVP | Implemented | design/gdd/photo-scoring.md | Camera & Aim, Disaster Entity Framework |
| 17 | Four-Axis Scoring & Style Multiplier | Gameplay | MVP | In Design | design/gdd/photo-scoring.md (Documentation axis only) | Photo Documentation, Stunt & Style Detection, Game Events Bus |
| 18 | Stunt & Style Detection | Gameplay | Vertical Slice | Not Started | — | Vehicle Feel, Game Events Bus |
| 19 | Dynamic Objectives & Events | Gameplay | Vertical Slice | In Design | design/gdd/event-system.md | Run Manager & Session Modes, Disaster Entity Framework, Civilians, Destructibles & Debris |
| 20 | Civilians & Rescue (inferred) | Gameplay | Vertical Slice | Not Started | — | Tiled World Streaming, Disaster Entity Framework |
| 21 | Vehicle Damage | Gameplay | Vertical Slice | In Design | design/gdd/vehicle-damage.md | Vehicle Feel, Game Events Bus |
| 22 | Payload & Utility Modules | Gameplay | Alpha | Not Started | — | Vehicle Loadout & Archetypes, Destructibles & Debris, Civilians |
| 23 | Storm Dollars & HQ Garage | Economy | Vertical Slice | In Design | design/gdd/economy-progression.md | Four-Axis Scoring, Save & Profile, Vehicle Loadout & Archetypes |
| 24 | Vehicle Loadout & Archetypes | Progression | Vertical Slice | Not Started | — | Vehicle Feel, Vehicle Damage |
| 25 | Per-Run Modifiers | Progression | Alpha | Not Started | — | Run Manager & Session Modes, Four-Axis Scoring, Vehicle Loadout & Archetypes |
| 26 | Cataclysm Heat | Progression | Alpha | In Design | design/gdd/economy-progression.md | Storm Dollars & HQ Garage, Per-Run Modifiers |
| 27 | Save & Profile (inferred) | Persistence | MVP | In Review | design/gdd/save-profile.md | — (platform storage only) |
| 28 | Settings (inferred) | Persistence | Vertical Slice | Not Started | — | Save & Profile, Input & Controls |
| 29 | HUD (inferred) | UI | MVP | Implemented | — | Four-Axis Scoring, Run Manager & Session Modes, Vehicle Damage |
| 30 | Viewfinder / PiP & Camcorder Lens | UI | MVP | Implemented | docs/architecture/adr-0003-art-direction-stylized-world-retro-lens.md | Camera & Aim, Photo Documentation |
| 31 | Run Screens (Title, Results, Newspaper Cover) | UI | MVP | Implemented | design/gdd/economy-progression.md (Newspaper Cover) | Four-Axis Scoring, Run Manager & Session Modes, Photo Documentation |
| 32 | Garage & Pre-Run Screen (inferred) | UI | Vertical Slice | Not Started | — | Storm Dollars & HQ Garage, Vehicle Loadout & Archetypes, Per-Run Modifiers, Cataclysm Heat |
| 33 | Off-Screen Disaster Indicators (inferred) | UI | MVP | Implemented | — | Disaster Entity Framework |
| 34 | Procedural Audio (inferred) | Audio | Vertical Slice | Not Started | — | Vehicle Feel, Game Events Bus, Disaster Entity Framework |
| 35 | Online Leaderboard | Meta | Alpha | Not Started | — | Four-Axis Scoring, Save & Profile, Run Manager & Session Modes |
| 36 | Onboarding / First Run (inferred) | Meta | Alpha | Not Started | — | HUD, Run Manager & Session Modes, Dynamic Objectives & Events |
| 37 | Accessibility (inferred) | Meta | Full Vision | Not Started | — | Settings, Input & Controls, HUD |
| 38 | Storm Director | Gameplay | MVP | In Review | design/gdd/storm-director.md | Disaster Entity Framework, Tiled World Streaming, Run Manager & Session Modes |

**Status notes.** "Implemented" means code exists and ships in 0.6.0, not that it is
feature-complete against its GDD. Run Manager (#4) has a working `RunManager` /
`SessionTimer` but `session-modes.md` is not implemented in full. Photo Documentation
(#16) covers `ScoringSystem` + `PhotoTrigger`; the camera-forward aim rework is S7-05.
Procedural Audio (#34) has a Codex-owned `ProceduralAudio` stub; X7-03 is the real pass.
Save & Profile (#27) is only `BestScoreStore` today. Destructibles (#8) is S7-08/S7-10;
Roads (#9) is S7-09.

**Open decisions — #19 Dynamic Objectives.** Two objective types from vision-1.0 are
not in `event-system.md`: **Protect** (a high-value structure that must survive the
run; Season 1 candidate, needs a spec) and **Push** (deal threshold damage to shove a
Kaiju/mech off-map; Season 3+, needs the Kaiju roster). Decide in the #19 review.

---

## Categories

| Category | Description | Systems here |
|----------|-------------|--------------|
| **Core** | Foundation systems everything depends on | Input, Vehicle Feel, Camera & Aim, Run Manager, Events Bus |
| **World** *(custom)* | The streamed tile world and what's in it (ADR-0004) | Tile Streaming, Surfaces, Destructibles, Roads & POI |
| **Gameplay** | The systems that make the game fun | Disasters, Wind, Photo, Scoring, Stunts, Objectives, Civilians, Damage, Payload |
| **Progression** | How the player grows across runs | Loadout & Archetypes, Per-Run Modifiers, Cataclysm Heat |
| **Economy** | Resource creation and consumption | Storm Dollars & HQ Garage |
| **Persistence** | Save state and continuity | Save & Profile, Settings |
| **UI** | Player-facing information displays | HUD, Viewfinder, Run Screens, Garage, Indicators |
| **Audio** | Sound systems | Procedural Audio |
| **Meta** | Systems outside the core loop | Leaderboard, Onboarding, Accessibility |

Narrative is omitted: the game has no story layer beyond the Newspaper Cover, which
lives with Run Screens.

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | The core loop: drive, chase, photograph, score, run ends. Without these you can't test "is this fun?" | Already playable (0.x on itch) | Design FIRST — mostly reverse-document what exists |
| **Vertical Slice** | One complete Heartland run with all four scoring axes, damage, a second disaster, and the meta loop | Sprint 8–9 | Design SECOND |
| **Alpha** | Season 1 content-complete for Early Access: full disaster set + alchemy, modifiers, heat, leaderboard | Milestone 3 — Early Access | Design THIRD |
| **Full Vision** | Polish and nice-to-haves | Post-EA | Design as needed |

Tier counts: **MVP 17 · Vertical Slice 12 · Alpha 7 · Full Vision 1.**

Rationale worth keeping:
- **Camera & Aim is MVP** even though the loop already runs: the shutter must score what
  the player is looking at, not what the truck points at, or Pillar 3's Documentation
  axis rewards the wrong skill.
- **Wildfire is Vertical Slice, not Alpha** (decided 2026-10-01): a second disaster in the
  slice is what lets Disaster Alchemy (Pillar 2) be proven before Alpha, instead of
  arriving with Hailstorm all at once.
- **Civilians and Vehicle Damage are Vertical Slice**: Pillar 4 ("a battered car that saved
  40 NPCs beats a pristine one") has nothing to measure until both exist.
- **Leaderboard is Alpha** because it is a Milestone 3 success criterion.
- **Accessibility is Full Vision** only because remapping lives in Settings (VS); text
  size and motion/shake options follow.

---

## Dependency Map

### Foundation Layer (no dependencies)

1. Input & Controls — every verb starts here.
2. Game Events Bus — the decoupling contract between Claude's and Codex's lanes; most systems publish or subscribe.
3. Tiled World Streaming — the world everything else is placed into.
4. Surfaces & Terrain Modifiers — the grip/speed data Vehicle Feel and the world both read.
5. Save & Profile — persistence the run and meta layers write to.

### Core Layer (depends on foundation)

1. Vehicle Feel — depends on: Input, Surfaces
2. Disaster Entity Framework — depends on: Events Bus, World Streaming
3. Run Manager & Session Modes — depends on: Events Bus, Save & Profile
4. Destructibles & Debris — depends on: World Streaming, Surfaces
5. Roads & POI Chunks — depends on: World Streaming, Surfaces
6. Settings — depends on: Save & Profile, Input

### Feature Layer (depends on core)

1. Camera & Aim — depends on: Vehicle Feel
2. Tornado — depends on: Disaster Framework
3. Wind Field — depends on: Disaster Framework, Vehicle Feel
4. Wildfire, Hailstorm — depend on: Disaster Framework, Wind Field
5. Disaster Alchemy — depends on: Tornado, Wildfire, Hailstorm
6. Vehicle Damage — depends on: Vehicle Feel, Events Bus
7. Photo Documentation — depends on: Camera & Aim, Disaster Framework
8. Stunt & Style Detection — depends on: Vehicle Feel, Events Bus
9. Four-Axis Scoring — depends on: Photo, Stunt & Style, Events Bus
10. Civilians & Rescue — depends on: World Streaming, Disaster Framework
11. Dynamic Objectives — depends on: Run Manager, Disaster Framework, Civilians, Destructibles
12. Vehicle Loadout & Archetypes — depends on: Vehicle Feel, Vehicle Damage
13. Payload & Utility Modules — depends on: Loadout, Destructibles, Civilians
14. Per-Run Modifiers — depends on: Run Manager, Scoring, Loadout
15. Storm Dollars & HQ Garage — depends on: Scoring, Save & Profile, Loadout
16. Cataclysm Heat — depends on: Storm Dollars, Per-Run Modifiers

Intervention points (rescues, cleared roadblocks, sensor pods) reach Scoring as events
from Civilians and Objectives, so Scoring does not depend on either.

### Presentation Layer (depends on features)

1. HUD — depends on: Scoring, Run Manager, Vehicle Damage
2. Viewfinder / PiP & Camcorder Lens — depends on: Camera & Aim, Photo
3. Run Screens — depends on: Scoring, Run Manager, Photo
4. Garage & Pre-Run Screen — depends on: Storm Dollars, Loadout, Modifiers, Heat
5. Off-Screen Disaster Indicators — depends on: Disaster Framework
6. Procedural Audio — depends on: Vehicle Feel, Events Bus, Disaster Framework

### Polish Layer (depends on everything)

1. Online Leaderboard — depends on: Scoring, Save & Profile, Run Manager
2. Onboarding / First Run — depends on: HUD, Run Manager, Objectives
3. Accessibility — depends on: Settings, Input, HUD

**Bottlenecks** (5+ dependents; changes ripple widely): Game Events Bus, Disaster Entity
Framework, Vehicle Feel, Four-Axis Scoring, Run Manager & Session Modes.

**Leaf nodes** (nothing depends on them; can be designed late): Disaster Alchemy, Payload,
Garage UI, Indicators, Audio, Viewfinder, Run Screens, Leaderboard, Onboarding,
Accessibility.

> TD-SYSTEM-BOUNDARY skipped — Lean mode. PR-SCOPE skipped — Lean mode. CD-SYSTEMS skipped — Lean mode.

---

## Recommended Design Order

Reverse-documented systems (code exists, no GDD) are marked *(reverse-doc)*; run
`/reverse-document` for those rather than `/design-system`. Systems that already have a
GDD in review are listed so the order is complete; their work is `/design-review`.

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Surfaces & Terrain Modifiers | MVP | Foundation | systems-designer | S |
| 2 | Save & Profile | MVP | Foundation | game-designer, lead-programmer | S |
| 3 | Disaster Entity Framework *(reviewed 2026-10-01: NEEDS REVISION, revisions applied, In Review; re-run /design-review)* | MVP | Core | game-designer, systems-designer | M |
| 4 | Run Manager & Session Modes *(review session-modes.md)* | MVP | Core | game-designer | S |
| 5 | Tornado *(reverse-doc)* | MVP | Feature | systems-designer | S |
| 6 | Four-Axis Scoring & Style Multiplier *(extend photo-scoring.md)* | MVP | Feature | systems-designer, economy-designer | M |
| 7 | HUD *(reverse-doc)* | MVP | Presentation | ux-designer | S |
| 8 | Destructibles & Debris | Vertical Slice | Core | systems-designer, technical-artist | M |
| 9 | Roads & POI Chunks (Heartland) | Vertical Slice | Core | level-designer | M |
| 10 | Settings | Vertical Slice | Core | ux-designer | S |
| 11 | Vehicle Damage *(review vehicle-damage.md)* | Vertical Slice | Feature | systems-designer | S |
| 12 | Stunt & Style Detection | Vertical Slice | Feature | game-designer, systems-designer | M |
| 13 | Civilians & Rescue | Vertical Slice | Feature | game-designer, ai-programmer | M |
| 14 | Dynamic Objectives & Events *(review event-system.md; Protect/Push)* | Vertical Slice | Feature | game-designer | S |
| 15 | Wildfire | Vertical Slice | Feature | systems-designer | M |
| 16 | Vehicle Loadout & Archetypes | Vertical Slice | Feature | systems-designer, economy-designer | M |
| 17 | Storm Dollars & HQ Garage *(review economy-progression.md)* | Vertical Slice | Feature | economy-designer | S |
| 18 | Garage & Pre-Run Screen | Vertical Slice | Presentation | ux-designer | S |
| 19 | Procedural Audio | Vertical Slice | Presentation | audio-director, sound-designer | S |
| 20 | Hailstorm | Alpha | Feature | systems-designer | S |
| 21 | Disaster Alchemy (Merge / Modify) | Alpha | Feature | game-designer, systems-designer | L |
| 22 | Payload & Utility Modules | Alpha | Feature | game-designer | M |
| 23 | Per-Run Modifiers | Alpha | Feature | game-designer, economy-designer | M |
| 24 | Cataclysm Heat *(review economy-progression.md)* | Alpha | Feature | economy-designer | S |
| 25 | Online Leaderboard | Alpha | Polish | game-designer, technical-director | S |
| 26 | Onboarding / First Run | Alpha | Polish | ux-designer | S |
| 27 | Accessibility | Full Vision | Polish | accessibility-specialist | S |

Not listed (designed and implemented, no further design work queued): Input & Controls,
Vehicle Feel, Camera & Aim (in `vehicle-feel.md`; build is S7-05), Game Events Bus,
Tiled World Streaming, Wind Field, Photo Documentation, Viewfinder, Run Screens,
Off-Screen Indicators.

---

## Circular Dependencies

None remain. Three near-cycles were broken by direction:

- **Per-Run Modifiers ↔ Scoring** — Scoring exposes multiplier hooks; Modifiers register into them. Scoring never references Modifiers.
- **Cataclysm Heat ↔ Storm Dollars** — same pattern: Heat plugs into the payout calculation.
- **Wind Field ↔ Vehicle Feel** — wind reaches the vehicle only as an external force input (`vehicle-feel.md` F11/F12), so Vehicle Feel has no dependency on Wind.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Disaster Alchemy | Design / Scope | Emergent merge/modify interactions multiply with every disaster added; easy to become scripted or unreadable | Prove one pairing (Tornado + Wildfire → Fire Tornado) in the Vertical Slice before Hailstorm; merged entities as their own `DisasterData` (vision-1.0) |
| Tiled World Streaming | Technical | WebGL frame-time headroom; G1 passed with a condition (physics ≤ 4 ms under real debris + vehicle) | S7-06 re-measures; 1 km fallback with the same tile code (ADR-0004 Alt 5) |
| Destructibles & Debris | Technical | Debris counts blow the WebGL physics budget | ADR-0004 tiers + debris budget; Tier A first (S7-08), Tier B fracture after |
| Vehicle Feel | Design | G3 feel playtest not yet run; everything in Pillar 1 rides on it | G3 at sprint end (Andy + 2 outside players); tuning continues into Sprint 8 |
| Civilians & Rescue | Technical / Scope | NPC agents in a physics-heavy, destructible, streamed world | Prototype a single pinned-group Rescue before generalizing; NPCs as tile-scoped, pooled |
| Online Leaderboard | Technical | Backend undecided (Supabase vs Steam leaderboards); anti-cheat for submitted scores | Decide via ADR before Alpha; Steam leaderboards are the lower-ops default |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 37 |
| Design docs started | 16 systems, covered by 7 GDDs + 2 ADRs + the AGENTS.md events contract |
| Design docs reviewed | 1 (vehicle-feel.md) |
| Design docs approved | 1 (vehicle-feel.md) |
| MVP systems designed | 11/17 |
| Vertical Slice systems designed | 4/12 (3 unreviewed GDDs + ADR-0004 for Destructibles) |

---

## Next Steps

- [x] Review and approve this systems enumeration (2026-10-01)
- [ ] Reverse-document MVP systems that shipped without a GDD (Disaster Framework, Tornado, HUD)
- [x] Surfaces & Terrain Modifiers designed 2026-10-01 (pending `/design-review`)
- [ ] Design remaining MVP systems (next: `/design-system save-profile`)
- [ ] Run `/design-review` on the five unreviewed GDDs (session-modes, photo-scoring, event-system, vehicle-damage, economy-progression)
- [ ] Run `/gate-check technical-setup` when MVP systems are designed
