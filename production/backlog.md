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
