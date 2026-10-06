# Doomsday Roadmap: M1 to Early Access

> **Status:** Decided by Andy, 2026-10-05, from a leadership review (creative director, technical director,
> producer). The milestone files for M2 and later get written **after the M1 verdict**: don't start M2 design
> until outside players have answered "is this fun and tense?"
> **Pitch:** Crazy Taxi and Tony Hawk meet disaster movie, *with a camcorder*.

## Decisions (2026-10-05)

1. **Targets:** a Steam page live by about **April 2027**, a demo in **Steam Next Fest, June 2027** (confirm the
   edition's dates and registration deadline on Steamworks), then **Early Access** after Next Fest (about fall 2027).
2. **Scope:** **no cuts.** The full Season 1 list stays. Instead, a **descoping ladder** (below) is agreed in
   advance, so a slip becomes a one-line decision, not a debate.
3. **Season 1 hazards:** Tornado, Wildfire, **Lightning**. Lightning replaces Hailstorm (Andy: hail isn't epic). Hail
   survives as the "hail core" combination (`vision-1.0.md`).
4. **Mode lineup:** the four modes below, with Career split into goal lists now and a story layer later.
5. **Platform:** WebGL hosts the M1 slice; desktop (Windows/Steam, then Steam Deck) is primary after M1 (R13).

## Milestones

| # | Milestone | Goal | Key content and systems | Exit | Size |
|---|---|---|---|---|---|
| M1 | Vertical Slice 0.9 | Answer "fun and tense?" | Built; feel tuning, G6, 0.9 on itch | ≥ 3 outside players say yes; gate check | closes ~Nov 2–16, 2026 |
| M2 | Desktop + Heartland World | The real game's foundation | Platform ADRs 0006–0009 (below), desktop build, 2 km Doomsday Chase world, roads, scatter, Heartland kit with **verticality** (below), Save & Profile v1, first Steam Deck test | A 5–7 min chase on 2 km at 60 fps desktop / 30 fps Deck, playtested | ~4 sprints |
| M3 | Steam Page + Demo | Start the wishlist clock | Storm visuals pass, capsule art and screenshots (via `?clean=1`), teaser, Steamworks basics, demo build (compact Arcade + one Chase map) | Page public; demo accepted for Next Fest | ~2–3 sprints |
| M4 | Season 1 Systems (Alpha) | Prove pillars 2–5 | Wildfire + Firenado, Lightning, Civilians & Rescue, meta loop (Storm Dollars, Garage, trucks), Storm of the Day | Every scoring axis fires in one run; outside playtest | ~5 sprints |
| M5 | EA Content-Complete | Ship-ready | Modifiers, Heat ranks, onboarding, Steam leaderboards and achievements, cloud saves, Deck verified, bug bash | No S1 bugs; Steamworks checklist | ~3 sprints |
| M6 | Early Access | Live and listening | Launch trailer, EA roadmap, day-1 hotfix lane | Stable first 2 h; feedback loop running | ~1 sprint |

Post-EA: Career story layer and characters, hail core and the other combinations, Suburbs biome, Season 2
(earthquake with phase-changing arenas), scenario share codes, replays and photo mode.

**The real constraint is Andy's gate time, not build speed** (R10). Agents build sprints in days; every system still
needs Andy's design calls, gates and playtests. Budget one batched gate session per sprint, and recruit a standing
pool of 8–10 playtesters once.

## Mode lineup

| Mode | Fantasy | Owns | When |
|---|---|---|---|
| **Doomsday Chase** (was Epic) | "I read the sky, drove 2 km, and was there when it peaked." Roguelite Heat; the top rank is **Last Broadcast** (storms never stop until the truck or the station dies) | Epic, tense | EA core (M2/M4) |
| **Arcade** (absorbs the 3-minute Sprint) | "I set the craziness dial." Share codes, mutators, B-movie days (Cownado, Sharknado, UFO night) | Stupid, fun | EA |
| **Storm of the Day** | One seed for everyone, one leaderboard, rival-chaser ghosts | Edgy (rivalry), tense | EA (plan determinism makes it nearly free) |
| **Career** | THPS goal lists county by county | Fun | **Goal lists in EA** (already built); story layer and characters post-EA |

Cut or merged: live co-op (only through an ADR), survival (becomes Last Broadcast), sandbox (becomes photo mode on
replays), evacuation heist (folded into Career and Chase events).

**Tone gaps to close:** *Epic* is the weakest today, and comes from the 2 km world, an EF5 that reads as a different
event (storm visuals pass) and Firenado. *Edgy* is missing and lives in the fiction: KTVR is a ratings-hungry station
that pays more for the shot than for the rescue (film the family or save them?). Guardrail: storm comedy, never
mocking real victims or towns.

**Signature moment (marketing):** the rain curtain parts on a wedge, the radio cuts to the emergency tone, the
sirens wind up. Launch off a ramp, cliff or rooftop over the road it's about to cross, airborne in the viewfinder,
take the shot, then hard cut to that frame as the newspaper cover.

**Protect:** (1) the best shot and the most dangerous spot are the same place: no safe zoom, no scripted photo spots;
(2) storms are dealt, never scripted, which is why Career, the daily seed and Arcade codes all replay.

## World: size, population and verticality (open, resolve in M2 design)

Andy (2026-10-05): the map's size and how it's populated need resolving so the world has **verticality as well as
horizontal movement**: big jumps off a cliff or building to get a prime shot. Today ADR-0004 plans "gentle rolls in
Heartland" on a 2 km heightfield, which is flat by design. Questions for the M2 world pass:
- **Size and density:** is 2 km × 2 km right for a 5–7 min chase, and how dense are POIs, roads and props so there's
  always something to use (ramps, cover, rescues) without long empty drives? (The run-pacing data says empty time
  kills tension.)
- **Vertical vantage points in the Heartland kit:** grain elevators, water towers, overpasses and ramps, bluffs or a
  river valley, rooftops in a town crossroads, billboards: places to climb to or launch from for the airborne shot.
- **Terrain shape:** does the heightfield need real relief (bluffs, river cuts, levees) beyond gentle rolls, and
  does that stay inside the streaming and physics budgets?
- **Vehicle side:** big drops need landing rules (survivable heights, damage), air control for framing mid-air, and
  photo scoring that rewards airborne shots (the signature moment).
- **Architecture:** ADR-0007 (world context) must cover runtime topology changes from the start, for phase-changing
  arenas (backlog) and collapsible bridges and overpasses.

## Platform foundation (technical director): critical ADRs at the start of M2

The slice is well built but not yet a platform: the director, the event contract, the lane split and the vehicle
are platform-grade; disasters, world and modes still assume one tornado, one 170 m arena and one mode.

1. **ADR-0006 Hazard abstraction:** disasters as capabilities (wind, lift, damage, burn, visibility, strike),
   lifecycle phases and a generic intensity tier. Proof: Wildfire touches nothing in `Tornado/`.
2. **ADR-0007 World context:** one bounds and spawn service for the compact arena and the 2 km tile streamer (replacing
   the ±85 m constants), biomes as tile data, runtime topology changes.
3. **ADR-0008 Mode and session framework:** a mode definition (rules, timer, goal catalogue, map, scoring) passed into
   a split-up `RunManager`. It gives Chase, Arcade, Career and Storm of the Day almost for free.
4. **ADR-0009 Determinism contract:** seeded streams for all gameplay RNG, a fixed disaster tick, input recording.
   Same seed means the same weather; physics replays are not promised.
5. **Save & Profile v1** (migrations, slots, Steam Cloud) and a desktop/Steam build pipeline (CI is manual-only today:
   `.github/workflows/unity-ci.yml` runs on `workflow_dispatch`; its header comment is stale).

## Descoping ladder (agreed in advance; use only if a milestone slips)

Cut in this order, one rung at a time:
1. iPad port, replays and photo mode, full attract AI, KTVR characters and weather person, new biomes
2. Scenario builder; Supabase (use Steam leaderboards)
3. Heat from 5 ranks to 3
4. Trucks from 4 to 2; modular payloads become stat blocks
5. Disaster Alchemy down to one pairing (Firenado)
6. Career story layer (goal lists stay)
7. The WebGL demo

Never cut: the storm visuals pass (it sells the page), Wildfire and Firenado (the trailer shot), simple civilians
(the Intervention axis), the core loop.
