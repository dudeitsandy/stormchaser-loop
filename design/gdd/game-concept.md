# Game Concept: Doomsday

*Created: 2026-10-01 (backfilled from `design/vision/vision-1.0.md`, S7-11)*
*Status: Draft*

> **Source of truth.** `design/vision/vision-1.0.md` is the approved concept. This file
> restates it in the template the framework skills read, and links back rather than
> duplicating detail. Where the two disagree, vision-1.0 wins until this file is
> reviewed. Sections marked **(inferred)** are not in vision-1.0 and need Andy's
> confirmation.

---

## Elevator Pitch

> It's a disaster-chasing roguelite where you drive a customized vehicle into
> world-ending events to photograph them, rescue people, and pull off stunts, while
> your car takes real damage and the world falls apart around you.
>
> *"Twister meets Crazy Taxi & Pokémon Snap in an escalating roguelite apocalypse."*

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Arcade driving roguelite; disaster photography |
| **Platform** | PC (Steam / Steam Deck) for Early Access; itch.io html5 + Windows builds for playtests. Switch is a post-launch maybe, not designed against (ADR-0001) |
| **Target Audience** | See Target Player Profile |
| **Player Count** | Single-player; asynchronous online leaderboard |
| **Session Length** | 90-second Sprint runs; 5–7.5 minute Chase runs (`session-modes.md`) |
| **Monetization** | Premium, Steam Early Access (Milestone 3) |
| **Estimated Scope** | Large (multi-season roadmap; Season 1 is the EA scope) |
| **Comparable Titles** | Crazy Taxi, Pokémon Snap, Burnout Paradise; *Twister* (film) for tone |

---

## Core Fantasy

You are the storm chaser who drives *into* the thing everyone else is fleeing, gets the
shot nobody else could, and drags a few people out on the way, in a truck that is held
together by luck and duct tape by the end of the run. Competence under chaos: the world
is ending loudly and you are the calmest, most reckless person in it.

---

## Unique Hook

It's like Pokémon Snap, **and also** the subjects are disasters that combine with each
other, and you're photographing them from a physics-driven truck that is getting thrown
around by the same storm. The best shot and the most dangerous position are usually the
same place, and disaster combinations ("Disaster Alchemy" — a wildfire drawn into a
tornado becomes a fire tornado) generate new photo subjects and new hazards without
scripting.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS) — (inferred)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** | 1 | Physics driving with Burnout/Rocket League energy; wind pull, lift and toss; cel-shaded world with a 90s camcorder lens (ADR-0003) |
| **Challenge** | 2 | Risk/reward framing: closer and more dangerous shots score more; Cataclysm Heat for mastery |
| **Expression** | 3 | Four scoring axes, no mandatory one; style multiplier; vehicle loadouts and archetypes |
| **Discovery** | 4 | Emergent disaster combinations; procedural event windows and modifiers |
| **Fantasy** | 5 | Storm chaser / disaster documentarian identity; Newspaper Cover of your best shot |
| **Narrative** | 6 | Player-made stories only (the run that saved 40 NPCs in a wrecked truck) |
| **Fellowship** | N/A | Leaderboard only |
| **Submission** | N/A | Short, intense runs by design |

### Key Dynamics (Emergent player behaviors)

- Players will drive closer to disasters than is safe because the shot is better there.
- Players will chain stunts into photo moments (jump the funnel, snap mid-air) to stack the style multiplier.
- Players will triage stacked objectives in real time: rescue now, or chase the peak-phase photo.
- Players will steer disasters into each other to create alchemy combinations on purpose.
- Players will accept damage strategically ("absorb the hit for the style points").

### Core Mechanics (Systems we build)

1. Physics vehicle with drift, jump, boost, air control; wind as a force (`vehicle-feel.md`)
2. Disaster entities with threat class, phase arc, and interaction tags; merge/modify alchemy
3. Camera-forward photo documentation scored on framing, distance, and phase (`photo-scoring.md`)
4. Four-axis scoring (Documentation, Intervention, Style, Objectives) with style as a multiplier
5. Roguelite run structure: modifiers, Cataclysm Heat, Storm Dollars into HQ Garage unlocks (`economy-progression.md`)

---

## Player Motivation Profile — (inferred)

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | No mandatory scoring axis; documentarian, rescuer, and stunt-driver are all valid runs | Core |
| **Competence** | Driving skill ceiling plus readable risk/reward on every shot | Core |
| **Relatedness** | Leaderboard and shareable Newspaper Covers | Minimal |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** — How: Storm Dollar unlocks, Cataclysm Heat ranks, best-score chasing
- [x] **Explorers** — How: discovering disaster combinations and how modifiers interact
- [ ] **Socializers** — not a target
- [x] **Killers/Competitors** — How: online leaderboard

### Flow State Design

- **Onboarding curve**: first run is a 90-second Sprint against a single tornado; drive and shutter only.
- **Difficulty scaling**: threat class and phase arc within a run; Cataclysm Heat across runs.
- **Feedback clarity**: per-photo score pops, run results breakdown by axis, Newspaper Cover.
- **Recovery from failure**: runs are 90 seconds to 7.5 minutes; restart is immediate.

---

## Core Loop

### Moment-to-Moment (30 seconds)
Read the disaster, pick a line, drive it at speed through wind and debris, and frame the
shot at the right moment: closer and more dangerous scores more. Drift, jump, and boost
are both movement and style.

### Short-Term (5-15 minutes)
One run: objectives spawn in time windows and stack, disasters escalate through their
phases and interact, and the run ends in a tally of documentation, intervention, style,
and objective scores (vision-1.0 Run Structure).

### Session-Level (30-120 minutes)
Several runs. Each payout buys garage unlocks; each unlock changes the next run's
loadout and options. The session ends at a natural stopping point after a payout.

### Long-Term Progression
HQ Garage unlocks (vehicle archetypes, modules), Cataclysm Heat ranks for mastery, and a
season roadmap that adds disasters, biomes, and vehicles (vision-1.0 Season Structure).

### Retention Hooks
- **Curiosity**: untried disaster combinations; locked archetypes
- **Investment**: Storm Dollar progress and garage unlocks
- **Social**: leaderboard placement; shareable Newspaper Covers
- **Mastery**: higher Heat levels; best-score runs

---

## Game Pillars

Verbatim from vision-1.0 (definitions there); design tests added here.

### Pillar 1: Kinetic Chaos
Movement is momentum-based, physics-forward, and trick-capable: expressive, tactile, and scorable.

*Design test*: Between a safer, more controllable vehicle and one that slides and gets thrown around, choose the expressive one and give the player tools (handbrake, air control, auto-right) to master it.

### Pillar 2: Disaster Stacking & "Disaster Alchemy"
Multiple disasters coexist and interact systemically; combinations are emergent content, not scripted sequences.

*Design test*: Between a hand-authored disaster set piece and a rule that produces it from two interacting entities, choose the rule.

### Pillar 3: Dual-Axis Scoring
Every action scores on Documentation and Intervention; style multiplies both; neither is mandatory.

*Design test*: If a mechanic only rewards one playstyle, it must not be required to finish a run well.

### Pillar 4: Living Damage Economy
Everything takes damage, and knowing when to absorb hits is part of the skill ceiling.

*Design test*: Between damage as a fail state and damage as a cost the player chooses to pay, choose the cost.

### Pillar 5: Roguelite Identity & Cataclysm Heat
Each run is shaped by meta loadout, procedural event windows, and per-run modifiers; mastery is rewarded with escalating Heat.

*Design test*: If two runs with the same loadout would play out the same way, add variance (modifiers, event windows), not more content.

### Anti-Pillars (What This Game Is NOT)

- **NOT a simulation**: physics serves arcade feel; meteorological accuracy loses to readability.
- **NOT scripted spectacle**: disaster set pieces come from rules (Pillar 2), not authored sequences.
- **NOT a retro-filter game**: the 90s look lives in the camera, not the world (ADR-0003).
- **NOT designed for Switch**: no Season 1 system assumes Switch hardware (vision-1.0, ADR-0001).

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| Crazy Taxi | Short timed runs, arcade driving, score chasing | Disasters are the course, and they move | Validates 90-second runs as a full experience |
| Pokémon Snap | Photography as the core verb; scoring the shot | Shooting from a physics vehicle under threat | Validates photo-as-gameplay |
| Burnout / Rocket League | Momentum, boost, crashes as spectacle | Damage carries over and has a cost | Movement as the draw, not a gimmick |
| Hades-style roguelites | Meta progression that changes the next run | Modifiers + Heat on a driving game | Validates "one more run" structure |

**Non-game inspirations**: *Twister* (1996); real storm-chaser footage; 90s camcorder
video; Nero — "Doomsday" as trailer audio reference.

---

## Target Player Profile — (inferred)

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18–40 |
| **Gaming experience** | Mid-core |
| **Time availability** | Short sessions; 10–30 minutes on weeknights, longer on weekends |
| **Platform preference** | PC and Steam Deck |
| **Current games they play** | Roguelites, arcade racers, physics sandboxes |
| **What they're looking for** | Short, replayable runs with spectacle and a real skill ceiling |
| **What would turn them away** | Floaty or unreadable driving; disasters that feel scripted; long grind before the fun |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity 6.6, URP with Render Graph (ADR-0001; upgraded from 6.3 LTS 2026-10-01) |
| **Key Technical Challenges** | Raycast vehicle feel (ADR-0005); streamed 2 km tile world with destruction on WebGL (ADR-0004); emergent disaster interactions |
| **Art Style** | 3D stylized: cel shading, outlines, 2D VFX cards in 3D; retro lens on viewfinder/photos (ADR-0003) |
| **Art Pipeline Complexity** | Medium: low-poly props and vehicles via Blender MCP, commission fallback (G2) |
| **Audio Needs** | Moderate: procedural vehicle/disaster/impact audio (X7-03) |
| **Networking** | None beyond an online leaderboard (backend TBD) |
| **Content Volume** | Season 1: 1 biome (Heartland), 3 disasters, ~4 vehicle archetypes |
| **Procedural Systems** | Seeded tile world, road graph, scatter; procedural event windows and modifiers |

---

## Risks and Open Questions

### Design Risks
- Vehicle feel may not hit the Pillar 1 bar (G3 playtest pending).
- Disaster Alchemy may read as noise rather than as emergent content.
- Four axes may dilute focus if the scoring breakdown isn't readable.

### Technical Risks
- WebGL frame budget under streaming + destruction + vehicle physics (ADR-0004 G1 condition).
- NPC civilians in a physics-heavy, destructible world.

### Market Risks
- Niche premise; the trailer and store page must sell the spectacle in seconds.

### Scope Risks
- Four-season roadmap; Season 1 alone needs 3 disasters, alchemy, meta, and leaderboard for EA.

### Open Questions
- Protect and Push objectives: spec or drop (systems-index #19).
- Leaderboard backend: Supabase or Steam leaderboards (needs an ADR).
- Push-off-map bounds: fixed or dynamic edge detection (vision-1.0, Season 3).

---

## MVP Definition

**Core hypothesis**: Driving a physics truck into a tornado to get the best photo is fun
in 90-second runs and makes players want another run.

**Required for MVP** (built; shipped as 0.x on itch):
1. Physics vehicle with drift, jump, boost (Vehicle Feel)
2. Tornado with EF scale, phase arc, and wind force
3. Camera-forward photo documentation and scoring
4. Timed run with results screen

**Explicitly NOT in MVP**: second disaster type, Disaster Alchemy, civilians, meta
progression, modifiers, Heat, leaderboard.

### Scope Tiers

See `design/gdd/systems-index.md` for the per-system breakdown.

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | Heartland tiles, Tornado | Drive, photograph, score | Done (0.x) |
| **Vertical Slice** | Heartland with roads + destructibles; Tornado + Wildfire | All four axes, damage, civilians, objectives, garage | Sprint 8–9 |
| **Alpha** | Season 1 complete | Hailstorm, alchemy, modifiers, Heat, leaderboard, onboarding | Milestone 3 (Early Access) |
| **Full Vision** | Seasons 2–4 | Accessibility and post-EA content | Post-EA |

---

## Next Steps

- [ ] Andy confirms the **(inferred)** sections (MDA, motivation profile, target player)
- [ ] `/design-review design/gdd/game-concept.md`
- [ ] Work the design order in `design/gdd/systems-index.md`
