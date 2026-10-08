# Backlog — Parked Ideas

Ideas captured so they aren't lost, deliberately **not designed** while the M1 design freeze holds
(`production/milestones/milestone-1-vertical-slice.md`). Each entry says where it would land and what it
needs first. Promote an entry by planning it into a milestone; until then nothing here gets a GDD.

---

## Biomes and generated locations (Andy, 2026-10-04)

**Idea:** more maps beyond the Heartland farmland: plains/farmland, suburb, city, Japanese city and more.
Possibly Rampage-style: runs travel through random generated towns and cities across the US or the world.

**What's already planned:**
- `vision-1.0.md` ("Modular Biome Hybrid") lists four biomes, one per season: Heartland (S1), Canyon
  Faultline + Coastal Gateway (S2), Metro Suburbs + Port (S3).
- ADR-0004 already supports it: the seeded `WorldPlan` assigns a biome per 128 m tile (S1 uses Heartland
  only), plus the road graph and POI placement.
- The Story/Career concept is "a season across one county, region by region."

**New in this idea:** a dense city, a Japanese city, and generated named locations / a road-trip structure.

**Direction (not a design):**
- A biome is a **kit** (terrain rules, road style, POI set, prop palette, surface set) run through the same
  generator. Cost is mostly Blender kit art, not code.
- Generated identity is cheap: a procedural place name ("Pratt County, KS", "Kōtō Ward") plus a regional
  flavor table. Career and Epic runs become a road trip from town to town. No real-world map data.
- Each biome should change the verb mix, not just the look:
  - Plains: sightlines and speed (the best photo map).
  - Suburb: slalom, rescues, house-scale destruction.
  - City: verticality and losing sight of the storm (tension).
  - Japanese city: pairs with the Kaiju Rising season and the mecha thread.

**Suggested order:**
1. 2 km Epic world + **Suburbs** as the second biome (next milestone after M1); proves the kit pipeline
   and density/perf.
2. **City WebGL perf spike** before committing to a city biome.
3. **Japanese city** with the Kaiju season.

**Needs first:** M1 shipped; S7-09 roads and X7-05 per-tile scatter (Heartland) working on the 2 km world.

**Tester signal (PT-1, P1, 2026-10-06):** asked unprompted for a random map every run from a seed. The
generator already supports it; M1 pins the seed so run goals stay tuned. A per-run (or daily) world seed is the
cheap first step once the Epic world lands.

---

## Other parked items (pointers only)

- Story / Career mode: `design/gdd/story-career-mode-concept.md` (concept, not a GDD)
- Arcade scenario builder (share codes) and Epic 2 km chase: `design/gdd/session-modes.md` revision, after M1
- S8-C2 per-truck HP redesign: Sprint 9 design task (`sprint-07-build.md` playtest notes)
- Wildfire, Disaster Alchemy, Civilians & Rescue, meta loop: `design/gdd/systems-index.md` tiers, after M1

---

## More disasters: lightning, volcanoes, sharknado (Andy, 2026-10-04)

**Idea:** new disaster types and combos beyond tornadoes: lightning storms, volcanoes, sharknados.

**Fit with what's planned:**
- **Lightning storms** (strongest fit, likely the first new disaster): part of the same supercells, so the
  Storm Director can own them (strike timing seeded per cell, Rule 10). A hazard (near-strike damage / EMP) and
  a photo subject (catching a strike in frame). `economy-progression.md` already references lightning: EMP
  Deflector Shield absorbs strikes, Heat rank 2 "Dry Lightning", rank 3 "Blackout" (night, lit by lightning).
- **Volcanoes:** tied to a biome (Canyon Faultline, `vision-1.0.md`) more than to storms; lava, ash fall and
  ballistic debris. Pairs with the biomes entry above.
- **Sharknado:** a tone call. As a serious disaster it fights the grounded chase; as an **unlockable modifier
  day** ("SHARKNADO DAY", earned through career goals) it fits the Crazy Taxi DJ voice and gives the roguelite
  reward loop a memorable prize.

**Needs first:** the Disaster Alchemy / interaction design (out of M1 scope), a second `DisasterEntity` type,
and for lightning a strike-photo scoring rule in `photo-scoring.md`.

**Lightning in two steps (Andy, 2026-10-05):** first **visual-only lightning** in storm cells: flashes in the cloud
deck, cloud-to-ground bolts near the anchor, thunder on a distance delay. It's presentation (Codex) and would also
help the storm visuals pass below. Then **lightning as a hazard** (near strikes, EMP, damage) once a second hazard
type is designed. **Hard constraint for both:** accessibility Basic, so no full-screen flashing above 3 per second
(`design/accessibility-requirements.md`); bolts light the scene, never strobe the screen.

---

## Rain-wrapped tornadoes (Andy, 2026-10-04)

**Idea:** a tornado hidden inside its own rain curtain, one of the real dangers of storm chasing. The player can
hear it and see the curtain but not the funnel until it is close: a built-in "oh no" moment.

**Fit:** builds on X8-03 rain curtains (presentation) and the storm-tension direction. Needs a design pass in
`storm-director.md` (which cells are rain-wrapped, how the forecast and telegraphs still give fair warning) and
`photo-scoring.md` (does a shot through rain score). Post-M1.

---

## KTVR Storm Radio: the music system (Andy, 2026-10-04)

**Idea:** the game's music is a radio station playing inside the truck. It plays a playlist of tracks, the
KTVR DJ breaks in for warnings, static creeps in as storms build, and the music cuts to the alert on a
TORNADO EMERGENCY. It builds on what exists: the KTVR DJ title, the news crawl and the X7-10 broadcast tones
and static.

**In M1 instead:** only a title-screen music loop, if Andy has a track, through the existing Music volume
slider. Nothing else.

**Sample tracks:** Andy is generating them in Suno ahead of time (Google Doc "Doomsday: KTVR Storm Radio sample
music (Suno)"). Staging stays outside the public repo. Before anything ships, record the plan and date for
every track (commercial rights) and make the Steam AI-content disclosure.

**Needs first:** M1 shipped; an audio direction pass (audio-director) covering the playlist, DJ voice lines
and how the station reacts to storminess `s` and to cell events.

---

## iPad port (Andy, 2026-10-05)

**Idea:** Doomsday as an iPad app.

**Fit:** a post-Early Access port, in the same group as the Switch port in `vision-1.0.md`, and only if Steam
traction justifies it. Order: Steam desktop (primary after M1) → Steam Deck → iPad / Switch.

**What works:** M-series iPads are close to laptop-class; the low-poly toon art and URP / Render Graph run on
Metal without changes (A-series iPads need a lower quality tier). 3-minute runs suit tablet sessions. The Input
System handles touch, and the save stem works the same way.

**What's hard:** controls. Throttle, steer, free camera, shutter, boost, jump and handbrake together. Bluetooth
controllers (Xbox / PlayStation) work, so "controller recommended" is easy; touch-only needs real design work.
A virtual stick plus buttons, with Storm Cam lock-on as the basis for touch aiming, and its own driving-feel
tuning pass.

**Costs:** a Mac with Xcode to build and sign; Apple Developer account ($99/yr; 15% cut under the small-business
program); App Store review; on-device performance and UI-scale testing per update. Premium iPad games are a
tough market; Apple Arcade is a pitch option.

**Do now:** nothing, except keep controller support solid (it also serves Steam Deck).

---

## Attract mode, full version: self-driving truck (Andy, 2026-10-05)

**Idea:** a true attract mode on the title: the truck drives itself, chases the storm, drifts, takes photos,
and the KTVR crawl reacts. It builds on the M1 title attract (flyover camera plus a seeded demo storm, game
audio muted).

**Needs:** simple driving AI (steer toward a framing point near the anchor storm, keep a safe distance, use
handbrake and boost for show), a photo trigger when framing is good, and a time-out back to the flyover.
About 4–6 h. A new system, so post-M1.

---

## Run replays (Andy, 2026-10-05)

**Idea:** replay a real recorded run from its seed and the player's inputs: for the attract mode ("best run
of the day"), for sharing runs (pairs with Arcade share codes in `session-modes.md`), and for ghost cars.

**Catch:** the Storm Director is deterministic from the seed (Rule 10), but vehicle physics doesn't replay
exactly from inputs, so input replays drift. Options: record transforms per tick instead of inputs (bigger
files, exact), or periodic state snapshots with input replay between them. Decide in a design pass. New
system, post-M1.

---

## Run pacing: dead moments in the 3-minute run (Andy, 2026-10-05, for planning)

**Andy:** "there are some real dead moments with nothing to do in the current run as there's limited storms and
the main storm sometimes arrives really early." Fine for now (no hidden areas, jumps, rescues or tasks yet), but it
needs a plan.

**Measured** (`AttractSeedProbe.PacingQuietTime`, Explicit, 1000 seeds; a storm is "alive" from spawn to end;
p10 / p50 / p90 seconds of the 180 s run):

| Regime | Share | No storm alive | Longest gap | Wait for first storm | Empty tail | Main storm touchdown | Storms |
|---|---|---|---|---|---|---|---|
| All | 100 % | 63 / **98** / 127 | 35 / **61** / 89 | 15 / **49** / 89 | 14 / **37** / 61 | 75 / 93 / 113 | 2 / 3 / 4 |
| Outbreak | 17 % | 106 / **120** / 131 | 63 / 82 / 101 | 61 / **82** / 101 | 17 / 38 / 61 | 77 / 97 / 114 | 2 / 3 / 3 |
| Quiet | 20 % | 86 / 113 / 132 | 41 / 65 / 89 | 15 / 51 / 89 | 23 / 45 / 70 | 75 / 95 / 113 | 2 / 3 / 4 |
| Sequence | 29 % | 58 / 88 / 113 | 37 / 56 / 75 | 17 / 42 / 74 | 24 / 42 / 65 | 76 / 96 / 114 | 2 / 3 / 4 |
| LoneGiant | 24 % | 64 / 90 / 118 | 33 / 51 / 86 | 14 / 41 / 86 | 14 / 31 / 52 | 77 / 96 / 113 | 2 / 3 / 4 |
| Chaos | 11 % | 42 / 70 / 105 | 20 / 44 / 76 | 9 / 25 / 69 | 0 / 15 / 58 | (no anchor) | 3 / 4 / 5 |

**Read:** in a typical run, more than half the run (≈ 98 s) has no storm alive. The median wait for the first storm
is ≈ 49 s (Outbreak ≈ 82 s), and once the main storm ropes out (≈ 125 s) the last ≈ 37 s are often empty. The main storm
touches down at 75–113 s, so "arrives early" probably means it's the only thing that happens and is over with time left.

**Levers to decide in planning:** cap the opening wait (a forming cell or warned satellite by ≈ 15–20 s); keep a
storm alive in the tail (a late satellite or a second act); more or longer-lived satellites; a shorter run; or fill
quiet time with non-storm activity (jumps/ramps, rescues, KTVR tasks, hidden spots; see Arcade vs Epic modes). The
Storm Director's plan is data (`CompactSettings`), so the first three are tuning; the last is new systems (post-M1).

---

## Crash weight: impacts, flips and tosses need tactile feedback (Andy, 2026-10-05)

**Andy:** crashes, flinging and flipping (and rough terrain) should "feel a little more heavy somehow"; there isn't
much tactile feedback. Drift and counter-steer feel good. Also asked of PT-1 testers.

**Options, roughly cheapest first:** camera impulse/shake scaled by impact severity, toss and landing (Cinemachine
Impulse); a few frames of hit-stop on big impacts; controller rumble on impacts, landings, rough terrain and the
funnel's pull (Input System motor speeds; Windows only, browser gamepad haptics are unreliable); heavier landing
thump, crunch and debris audio/VFX (Codex); suspension bottom-out squash on hard landings. Camera shake plus rumble is
probably about 2 h and fits the freeze rule; decide with PT-1 feedback.

---

## Bounty: BACK-TO-BACK NEAR MISSES feels complicated (Andy, 2026-10-05, goal pass)

**Rule today:** two near misses within 10 s (`NearMissPairWindow`). A near miss is passing a funnel between its damage
radius and damage radius + 6 m, at over 8 m/s, at most once per funnel per 3 s. So it needs two clean threads of that
6 m band inside 10 s, usually around two different funnels or a turn-back past the same one.

**0.8.7:** text changed to "TWO NEAR MISSES, TEN SECONDS" (no digits: event-system RG AC) (clarity only, same rule). Difficulty waits for PT-1.

**Options (all tuning or text, no new system):** clearer text ("2 NEAR MISSES IN 10 S"); a wider band (margin 6 → 10 m)
or a longer pair window (10 → 20 s); a visible "1 / 2" counter while the window is open; or swap the bounty for a
simpler one. Decide with Andy, ideally with PT-1 answers to "did any goal feel impossible or unclear?".


---

## Storm visuals pass, before any Steam page (Andy, 2026-10-05)

**Finding:** the HUD-free capture (`?clean=1`, `production/qa/evidence/clean-title-attract.png`) shows the slice's
visual gaps, which don't matter for testing but would for a store page:
- **Funnel reads pale:** a translucent grey cone, half washed out by its own rain curtain (Codex's "EF5 wedge
  reads pale" note from 0.7.4). Also a gameplay readability point: the threat should be unmistakable.
- **Sky has no structure:** a near-black green slab, no visible cloud deck shape, wall cloud or inflow bands.
- **Lighting mismatch:** the ground stays brightly lit under a pitch-dark sky, so the scene reads composited.
  The ground needs to darken and desaturate under storminess `s` like the sky does.
- Sparse farmland (expected on the compact arena; the 2 km world and Heartland kit fix it).

**When:** top of the post-M1 art list, before Steam capsule and screenshot work. Exception: if PT-1 testers say
storms are hard to see, funnel opacity moves into M1 as a small Codex tuning task (passes the freeze rule).

**Owner:** Codex (presentation: funnel cards, sky deck, storminess grading), with an art-direction check against
ADR-0003.

---

## KTVR weather person and radar (Andy, 2026-10-05, polish, much later)

**Idea:** when a front or a dangerous storm comes in, a small picture-in-picture of the KTVR weather person
pops up with a quick radar sweep (hook echo, red and purple cells) and a one-liner, like a TV weather cut-in.
Strongest in a storm or front mode with coordinated weather (Epic 2 km chase, `session-modes.md`).

**Fit:** the forecast panel stays the gameplay readout; this is the flavor layer on top of the same Storm Director
data (cell positions, EF, phase), so it never contradicts the forecast. It reuses the crawl's rule: no exact EF
numbers in character lines. It needs a character portrait (2D, animated cut-in) and a stylized radar render
(top-down from director cells, not real data).

**When:** polish, after M1 and after the storm visuals pass.

---

## The KTVR universe: characters (Andy, 2026-10-05, long-term)

**Idea:** give the quirky world recurring characters: the KTVR weather person, the Morning Zoo crew (Big Rick, Tammy,
Weather Wally), sponsors like Dorothy's Tow & Salvage and Last Chance Storm Cellars, and in-game NPCs (townsfolk to
rescue, rival chasers). The radio, news crawl, bounties and career goals become their voice.

**Fit:** pairs with Civilians & Rescue (systems-index #20), Story/Career mode (`story-career-mode-concept.md`), the
KTVR radio system and the weather person above. Needs a narrative pass (`narrative-director`: cast, tone, how much
story the roguelite carries) before any assets. Tone guardrail: storm comedy, never mocking real disasters or real
towns.

**When:** after M1. The first cheap step is consistent names and voice in existing text (crawl lines, bounty text,
jingles), which can happen any time as copy edits.

---

## Disaster roster and combinations (Andy, 2026-10-05, long-term brainstorm)

**Frame:** "Crazy Taxi and Tony Hawk meet disaster movie." A disaster earns its place if it adds a new photo
subject, a new chase or escape pattern, new stunt terrain (THPS) and new jobs (Crazy Taxi). Season mapping
follows `vision-1.0.md`; this list only adds ideas.

**Real weather (Storm Season and later, grounded tension):** lightning (strike zones; strike photos), derecho
(a straight-line wind wall to outrun), haboob (dust wall, visibility to zero, drive by radar and sound), giant hail
(dents, hold the shot), flash flood (roads become rivers, bow-wave jumps, roof rescues), blizzard and avalanche
(whiteout; the Cool Boarders "oh no" escape), wildfire (in the vision), volcano (lava rivers, ash, flying rocks,
cooled-lava ramps).

**Combinations (Disaster Alchemy):** Firenado (tornado + wildfire, the planned first merge), Waterspout
(tornado over water; the path to Sharknado), Thundersnow (blizzard + lightning, real), Dirty thunderstorm
(volcano + lightning, real), Hail core (tornado + hail, ice shrapnel), Fire run (derecho + wildfire, the front
races), Flaming debris tornado (meteor shower + tornado).

**Disaster-movie tier (later seasons, in the vision):** earthquake, tsunami, sinkhole, kaiju, rogue mech, swarm,
alien invasion, dimensional rift. New: meteor shower (shoot the impact) and solar flare (EMP glitches the
camcorder and HUD; play half-blind).

**B-movie modifier days (unlockable rewards, not core):** Sharknado (above), **Cownado** (a Twister nod; nearly
free since knock-loose props exist: swap debris for cows), **Haunted fog** (ties to the Ghostweave "Paranormal
Field Unit" badge), **UFO night** (tractor beams lift the truck).

**Suggested order after M1:** lightning → hail and derecho (keeps Storm Season coherent) → wildfire (unlocks
Firenado) → the season plan. Cownado any time as a cheap Easter egg.

---

## Arenas that change phase mid-run (Andy, 2026-10-05; Power Stone 1/3 stage transitions)

**Idea:** the map changes mid-run on triggers or timing, like Power Stone's stage transitions, driven by the
disaster. **Earthquake:** fissures split the farmland into new ramps and gaps, an overpass collapses, roads reroute.
**Flood:** a dam breaks and the low fields become a lake, roads become rivers. **Wildfire:** cover burns away,
opening sightlines and closing routes. **Tornado:** debris fields and flattened barns persist for the rest of the run.

**Rule (protects "storms are dealt, never scripted"):** a phase change is triggered by the hazard and seeded by the
Storm Director (same seed, same collapse), never a hand-authored cinematic.

**Builds on:** ADR-0004 destructible tiled world (fracture tiers), the vision's Season 2 earthquake (fissure ramps,
collapsing overpasses), Disaster Alchemy (Earthquake + dam → Flood).

**Architecture note for post-M1:** put runtime topology changes into ADR-0007 (world context: bounds and spawn
service) from the start, so roads, spawn zones, civilians and streaming tiles can change mid-run without rework.

**When:** Season 2 with the earthquake; a small first taste (one collapsible bridge or overpass) could come with
the 2 km Heartland world.
