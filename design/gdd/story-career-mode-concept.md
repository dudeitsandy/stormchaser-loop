# Story / Career Mode — Concept

> **Status**: Concept (brainstorm 2026-10-03, Andy + Claude). Not a GDD yet. Next: `session-modes.md`
> revision, then `/design-system story-career-mode`.
> **Origin**: 0.7.0 playtest (Andy and his son): "a story mode like how Fortnite has quests, which fits our
> Tony Hawk saving people, pets, doing tasks and tricks for goals to pass a level."
> **Implements pillars**: 3 Dual-Axis Scoring (style scores everywhere), 1 Kinetic Chaos, 2 Disaster
> Alchemy (storms stay rule-driven); respects the anti-pillar "NOT scripted spectacle".

## Pitch

A season of chase missions across one county. Each region is an 8–12 minute mission with a 10-goal list,
handed to you as contracts by the people who need you: save Farmer Hale's cattle, get KTVR its EF4
footage, beat Dusty's drift record. Clear 5 goals to open the next region, all 10 for gold. The storm is
dealt fresh by the Storm Director every attempt, so the goal you missed last time might fall your way
next time.

*Tony Hawk career levels, told through Fortnite-style contracts, in a county that remembers you saved it.*

## Mode in the lineup

| Mode | Shape | Player |
|------|-------|--------|
| **Arcade** | Short, configurable, shareable scenarios | Competitors, quick sessions |
| **Epic** | 2 km chase, forecast and intercept, roguelite Heat | Mastery, long-form tension |
| **Story / Career** | Authored regions with goal lists and characters | Completionists, the game's spine |

All three share the Storm Director, the vehicle, scoring and events. Story mode adds authored layouts,
goal lists, characters and the county map.

## Design decisions (Andy, 2026-10-03)

| Decision | Choice |
|----------|--------|
| Primary audience | Completionists (THPS "10/10 gold" players); families as secondary |
| Mission length | 8–12 minutes, played in one go |
| Rewards | All of: new regions, trucks and upgrades, story beats, cosmetics / album |
| Structure | Combination: Career levels (spine) + contract characters (story) + county map (heart) |
| County memory | **Cosmetic only**: gold regions rebuild, rescued people and pets appear at HQ and in the album; no gameplay carry-over |
| Storm goals | **Relative to the storm dealt** ("shoot the anchor at peak"), never a fixed EF |
| Style scoring | **Yes, in every mode**: drift / air / near-miss score and chain into a multiplier on photos and rescues |

## Mode rules

1. **The storm is the level's moving part.** Maps and goal lists are authored; storms never are. The
   Storm Director deals them. *Test:* a goal that only works with a scripted storm is rewritten as a
   relative goal.
2. **Every goal is a person.** Goals arrive as contracts from characters. *Test:* if we can't say who
   wants it and why, cut the goal.
3. **Always winnable, never identical.** Any attempt can complete any non-"lucky" goal; no two attempts
   play the same. *Test:* between guaranteeing a goal's setup and keeping variance, keep variance and
   widen the goal.
4. **The county remembers how you did, but never makes the next level harder.** Memory is celebratory, never a penalty.
   *Test:* if carry-over would make the next level harder, it's out.

**Anti-rules — Story mode will NOT have:**
- cutscenes or scripted disaster set pieces (story beats are radio lines and contract text);
- level-locked trucks or required upgrades (any truck can gold any level; Pillar 3);
- a timerless open world (the mission and the storm lifecycle are the clock);
- escort-only or fetch-only goals (every goal touches the storm, style or the shot).

## Core loop

**30 seconds:** drive the storm's edge: drift, jump and near-miss for style, line up shots, grab rescues.
A live score ticker shows style building a chain multiplier; banking a photo or rescue on a hot chain
multiplies it. (Fixes the playtest note "I didn't see scoring pop up, only for photos" in every mode.)

**5 minutes (the mission):** the forecast says where the storm is building; the radio relays contracts.
Route between contracts as the storm matures. Rescue first, or gamble on the peak shot? Relative goals
make every dealt storm a fair puzzle.

**Session (30–60 minutes):** a few attempts at a region, each crossing off different goals because the
storm differed. "One more try" bites at 4/5 (next region) and 9/10 (gold).

**Progression (weeks):** the county map fills in; gold regions rebuild; rescued families and pets gather
at HQ; the album fills; contract chains advance the characters (Dusty's rivalry, KTVR's big break);
region medals unlock trucks and upgrades, including durability enhancements from the HP redesign (S8-C2).

**Motivation:** Autonomy: route and contract order, which goals to chase. Competence: the list
3 → 5 → 10/10 and longer style chains. Relatedness: named people and pets you saved, a rival who reacts.

## Goal kinds (draft)

Built from existing and planned systems; each is phrased relative to the storm dealt.

| Kind | Example contract | Builds on |
|------|------------------|-----------|
| Documentation | KTVR: "Shoot the anchor at its peak, PERFECT tier" | Storm Director anchor and phases, photo scoring |
| Documentation | KTVR: "Two cells in one frame" (Outbreak/Sequence attempts) | Storm Director, photo scoring |
| Intervention | Farmer Hale: "Get the herd to the barn before the storm ropes out" | event-system NPC Rescue & Escort |
| Intervention | Sheriff: "Clear the bridge roadblock" | event-system Ram & Unblock (≥ 50 MPH boost) |
| Style | Dusty: "Out-drift me: 4 s drift inside the wind" | vehicle-feel style events, Drift Framing Zone |
| Style | "SICK: 5,000 style in one run" | style scoring (new) |
| Secret | A hidden pet in each region ("find Biscuit") | authored layout |
| Lucky (max 1 per region) | "The big one: EF4+ at point-blank" | Storm Director regime odds |

Goal difficulty must scale with the regime dealt (a Quiet attempt vs a Chaos attempt); this is the main
fairness question for the GDD.

## Player types

- **Primary:** Achievers / completionists.
- **Secondary:** Explorers (county map, secrets); families (pets, named characters).
- **Not for:** speedrunners and pure competitors; Arcade's scenarios and leaderboards serve them.
- **Market proof:** Tony Hawk's Pro Skater 1+2 (2020) goal lists, Fortnite quests; career modes in
  Hot Wheels Unleashed and Wreckfest.

## Scope

**MVP (proves the loop):** one region on today's map with an 8-goal list; style scoring + chain ticker in
all modes; HUD goal list; contract text as radio lines; results screen with goals crossed off.

**Tiers:**
- **v1 (Early Access):** 3 regions × 10 goals, 4–5 characters, county map with rebuild states, level select.
- **Full vision:** 6–8 regions, contract chains with a season finale ("the big one"), HQ with rescued
  pets, album pages per region.
- **If time runs out:** MVP region + 2 more; skip town-rebuild art.

**Dependencies (build order):**
1. Storm Director implementation (relative goals need its anchor and phases).
2. Run goals in `event-system.md`.
3. Style scoring (in `photo-scoring.md` or a new scoring GDD).
4. `session-modes.md` revision (Arcade / Epic / Story).
5. 2 km world (ADR-0004) for full-size regions; the MVP can use today's map.

## Risks

1. **Goal readability mid-chase**: the player must know what's left without stopping; the HUD goal list
   and radio need real UX design (`/ux-design`).
2. **Relative-goal fairness**: goal difficulty has to scale with the regime the director deals.
3. **Content cost**: each region needs an authored layout, contracts and lines.
4. **Pillar drift**: authored regions must not slide into scripted spectacle; mode rule 1 is the guard.

*Lean review: CD-PILLARS, AD-CONCEPT-VISUAL, TD-FEASIBILITY and PR-SCOPE gates were skipped.*
