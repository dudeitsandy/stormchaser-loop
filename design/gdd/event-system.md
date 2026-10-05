# Procedural Event System & In-Run Objectives

**Status:** Design — not yet implemented  
**Target Sprint:** Sprint 5–6  
**Owner:** Game Designer  

---

## Overview

As disasters move across the map, the event system procedurally generates time-critical event windows, stunt opportunities, and route interventions in their path. Events are situational — the player chooses to engage or ignore each one. Engaging is always riskier and always more rewarding. Events create the chaotic "Twister / Action Movie" moments that turn runs into unforgettable stories.

---

## Player Fantasy

*"I had half a second to decide: go through the barn at 80 MPH, launch off the billboard ramp over the funnel, or slam into the fallen semi to clear the road for the convoy. I did all three."*

---

## Detailed Rules

### Event Generation
- Events spawn dynamically in the disaster's projected path (2–4 seconds ahead of current position) or at static POIs.
- Event density scales with Threat Class / EF rating: EF0 = rare, EF5 = frequent.
- Maximum 2 active events simultaneously.
- Events expire if not engaged within their time window (8–12 seconds).

---

## Event Taxonomy

### 1. Stunt Events

#### Structure Gap (Barn / Warehouse Punch)
- A structure spawns between player and tornado with a driveable corridor.
- Gap width: 1.5× vehicle width (tight but passable at speed).
- **Reward:** High-speed shortcut + "Daredevil" score multiplier (1.5×) on next photo.
- **Risk:** Collision damage if misaligned; falling debris.
- **Visual:** Light beam / dust shaft illuminates the entrance.

#### Twister Jump Ramp
- A collapsed billboard, dirt mound, or buckled highway bridge creates a ramp aligned with the disaster path.
- **Reward:** Massive airtime, +2.0× "Airborne Snapshot" multiplier if photographed mid-flight.
- **Risk:** Landing in rough terrain or directly inside the vortex damage zone.

#### Drift Framing Zone
**Implementation Trigger — do not build before this is met:** requires the Vehicle Feel pass
(momentum physics + drift) planned for Sprint 6-7 (`vision-1.0.md` Pillar 1). This event's
own doc targets Sprint 5-6, one sprint *before* drift exists — treat this specific stunt as
deferred to whenever the Vehicle Feel pass actually lands, even if the rest of this doc ships
on schedule.
- A tight 90° asphalt or gravel bend directly perpendicular to the disaster.
- **Reward:** "Drift Style" multiplier based on continuous slide duration with tornado in frame.
- **Risk:** Spin-out or loss of momentum into obstacles.

---

### 2. Intervention & Route Clearing Events

#### Ram & Unblock (Roadblock Clearance)
- A fallen tree, boulder, or stalled semi-truck blocks a primary highway artery.
- Player rams the blockage at speed (speed ≥ 50 MPH or heavy armor chassis).
- **Reward:** Instant +200 Intervention score + clears the high-speed escape lane for NPC traffic.
- **Risk:** High chassis impact damage if attempted at low speed with a light vehicle.

#### NPC Rescue & Escort
- A civilian vehicle is stalled or pinned in the direct path of the cataclysm.
- Player intercepts by driving within 15 units or honking to guide them.
- **Reward:** Flat bonus score + "Saved" badge on run summary (+5s time extension with Adrenaline perk).
- **Risk:** Intercept path takes player directly into the vortex suction/debris field.

#### Sensor Deployment ("The Dorothy Pod")
- A green holographic target beacon spawns 3 seconds ahead of the projected vortex trajectory.
- Player must drive through the beacon at speed and trigger the sensor launcher payload.
- **Reward:** +300 Intervention score + live telemetry on tornado path for the rest of the run.
- **Risk:** Point-blank exposure to front-line wind turbulence.

#### Bridge / Bottleneck Trap
- Narrow passage with tornado visible directly on the other side.
- **Reward:** Guaranteed sub-10-unit photo distance (maximum distance score).
- **Risk:** If disaster shifts path, player is trapped at point-blank range.

---

### 3. TV News Network Bounties ("Breaking News Demands")

*(2026-10-04: these become the **KTVR bounties** of Run Goals v1, below. Only Point-Blank EF5 is in the v1
pool; the other three need systems that don't exist yet (debris-in-frame detection, a second disaster type,
rescues). The v1 pool, draw and display are defined there.)*

At run start, the News Network assigns 1–3 procedural photo bounties:
- **"Airborne Subject"**: Photograph an airborne car, cow, or roof section in the funnel (+250 pts).
- **"Point-Blank EF5"**: Photograph an EF5 within 12 m (+500 pts).
- **"Dual Cataclysm"**: Capture two interacting disaster centers in one frame (+1000 pts).
- **"Hero of the Day"**: Complete 3 NPC rescues in a single run (+400 pts).

---

## Formulas

### Event Spawn Probability (per 10 seconds)
`SpawnChance = BaseChance + (ThreatClass × 0.08)`
- EF0 / Class I: 10% / 10s
- EF3 / Class III: 34% / 10s
- EF5 / Class V: 50% / 10s

### Ramming Impact Threshold
`ClearSuccess = (VehicleMass × VehicleSpeed) >= ObstacleMassThreshold`
- If successful: Obstacle fractures into debris; player retains 70% momentum.
- If failed: Vehicle takes 1 HP damage; comes to an immediate dead stop.
- Ram obstacles are exempt from `vehicle-feel.md` F10 impact damage: this rule is the only damage source
  for them (no 2 HP severe-crash stacking on a successful ram). Default Pickup needs boost to reach
  50 MPH (vehicle-feel F9). Added 2026-10-01.

### Daredevil Multiplier Stacking
`FinalPhotoScore = BasePhotoScore × DaredevilMultiplier × StuntAirMultiplier`
- Stacks with EF strength: EF5 base (400) × Daredevil (1.5) × Jump (2.0) = **1,200 pts**.

---

## Edge Cases

- **A 3rd event would spawn while 2 are already active**: the new event does not spawn and is
  not queued — it's simply skipped. The next spawn check (per `BaseEventSpawnChance`) may
  produce a new event once a slot frees up.
- **Player doesn't have the required payload equipped** (e.g. Sensor Deployment without the
  Dorothy Sensor Launcher from `economy-progression.md` Tree 4): the event does not spawn at
  all — payload-gated event types are excluded from the spawn pool until unlocked and equipped.
- **Event's disaster despawns or merges mid-window**: the event expires immediately, as if its
  time window ran out — no partial credit.
- **Two Ram & Unblock events target the same obstacle**: cannot occur — obstacles are
  consumed (destroyed) by the first successful clear, removing them as an event target.

## Dependencies

- `DisasterEntity.cs` / `TornadoController.cs` — path projection for event placement.
- `storm-director.md` — owns storm cells, their tracks and ETA (path projection source) and each cell's EF
  (drives `SpawnChance`). Added 2026-10-01.
- `VehicleData.cs` / `PlayerVehicle.cs` — collision damage, mass, and payload triggers.
- `vehicle-feel.md` — supplies Sliding state + slide duration (Drift Framing Zone — this satisfies its
  "requires the Vehicle Feel pass" trigger) and Airborne flag + airtime (Twister Jump Ramp). Added 2026-10-01.
- `SessionTimer.cs` — event window timing and expiration.
- `ScoringSystem.cs` — multiplier chaining and Intervention score tallying (`photo-scoring.md`
  lists this doc's `DaredevilMultiplier`/`StuntAirMultiplier` in its own Dependencies).
- `AudioSystem` — event stings, siren warnings, impact crunches.
- `economy-progression.md` — payload-gated events (Sensor Deployment) require Tree 4 unlocks.
- `vehicle-damage.md` — the Ramming Impact Threshold formula and `VehicleMaxHP` tuning knob
  below are implemented by that doc's HP/stage model, not redefined here. `PlayerVehicle.cs`
  still needs the health field that doc specifies before Ram & Unblock or stunt-collision
  mechanics can be built.

---

## Tuning Knobs

| Knob | Default | Safe Range | Affects |
|------|---------|-----------|---------|
| `BaseEventSpawnChance` | 0.10 | 0.05–0.25 | Frequency of procedural events |
| `EFSpawnModifier` | 0.08 | 0.04–0.15 | Escalation rate per threat tier |
| `EventExpiryWindow` | 10s | 6–15s | Time player has to engage event |
| `DaredevilMultiplier` | 1.5× | 1.2–2.0× | Score reward for structure gaps |
| `JumpAirMultiplier` | 2.0× | 1.5–3.0× | Score reward for mid-air photos |
| `VehicleMaxHP` | 3 hits | 1–5 hits | Survival buffer before run destruction |

---

## Acceptance Criteria

- [ ] Events spawn in projected disaster trajectory within correct time window
- [ ] Stunt ramps grant airborne photo multiplier when snap occurs mid-air
- [ ] Barn corridors detect clean passage vs. collision
- [ ] Ramming obstacles clears roads and awards Intervention score
- [ ] TV News Bounties display on HUD and complete dynamically on trigger
- [ ] All events remain strictly opt-in (player can ignore and focus purely on driving/photos)

---
---

# Run Goals (v1)

**Status:** Revised after `/design-review` (2026-10-04, NEEDS REVISION → 3 blockers fixed), awaiting re-review. M1 design-freeze exception (`milestone-1-vertical-slice.md`).
**Sprint:** 8 (re-scoped), tasks RG-1 (design) and RG-2 (build).

## RG Overview

Every run carries two kinds of goals. **Career goals** are a fixed list of 10 per mode and map that the
player chips away at across many runs (Tony Hawk's Pro Skater). **KTVR bounties** are 3 goals drawn from the
run's seed, often timed (Crazy Taxi), issued by the same KTVR News that runs the storm crawl. Inside a run,
every completion pays a score bonus, which feeds Storm Dollars 1:1 like all score
(`economy-progression.md`). Across runs, every completion is written to a per-mode **accomplishments
record**, and career milestones grant **unlocks**, the roguelite pull: goals get you things. v1 proves that
loop end to end with one real unlock in the vertical slice.

## RG Player Fantasy

*"Seven of ten. I still need 'photograph an EF4 at its peak', and one more goal unlocks the KTVR paint job.
This run's seed has a Lone Giant, so this is the one."* Career goals give every run a reason beyond the
score: a checklist you finish over many sessions. Bounties add the in-run "go, go, go": KTVR wants a shot of
the next EF3 within 40 seconds, and the clock is running.

## RG Detailed Rules

1. **Goal catalogue.** Every goal has a stable string ID, a type, a mode and a map, e.g.
   `career.compact.heartland.ef4_peak` (career) or `bounty.compact.rope_out` (bounty: `bounty.<mode>.<name>`,
   not tied to a map). v1 types: **Score**, **Style**, **Storm**. Reserved for later, added
   without redesign: **Destruction**, **Rescue**, **NPC Challenge**, **Collectible** (Tony Hawk-style letters,
   which need world-placed pickups). An ID never changes meaning once shipped (`save-profile.md` keeps
   unknown IDs).
2. **Career list v1** (mode `compact`, map `heartland`). Fixed, not seeded:

   | # | ID suffix | Goal | Type |
   |---|-----------|------|------|
   | 1 | `score_rookie` | Score at least the Rookie threshold in one run | Score |
   | 2 | `score_pro` | Score at least the Pro threshold in one run | Score |
   | 3 | `score_sick` | Score at least the Sick threshold in one run | Score |
   | 4 | `ef4_peak` | Photograph an EF4 or stronger while it is at peak (Mature) | Storm |
   | 5 | `point_blank` | Photograph a tornado from closer than the point-blank distance | Storm |
   | 6 | `toss_survivor` | Get tossed by a tornado and still finish the run (timer, not wreck) | Storm |
   | 7 | `storm_drift` | One continuous drift of the drift duration with a storm within the drift range | Style |
   | 8 | `big_air` | One airtime style moment (`vehicle-feel.md` E2: only air above `MinAirtimeHeight` counts) of at least the big-air time | Style |
   | 9 | `near_misses` | The near-miss count in one run | Style |
   | 10 | `front_page` | A PERFECT-framed photo of the run's main storm (the anchor) | Storm |

   Thresholds are tuning knobs (Formulas / Tuning Knobs).
3. **KTVR bounties.** At run start, **3** bounties are drawn from the v1 pool with the run's seed on its own
   sub-stream, so a replay (`?seed=N`) gets the same bounties. Bounty text never states an EF number (it
   follows the crawl's Rule 7). v1 pool, built only on events that already exist:

   | ID (`bounty.compact.`…) | HUD text | Complete when | Eligible when the plan has… | Bonus |
   |---|---|---|---|---|
   | `point_blank_ef5` | "POINT-BLANK ON A MONSTER" | Photo of an EF5 subject closer than `PointBlankEf5Distance` | a non-dropped EF5 cell | 500 |
   | `warning_cell` *(timed)* | "SHOOT THE NEXT WARNED STORM" | Photo of **the** cell named by the first TORNADO WARNING crawl after run start (first `StormCellForming` with true EF ≥ 3), within `TimedBountyWindow` of that crawl | a non-dropped EF3+ cell | 300 |
   | `before_touchdown` | "CATCH THE MAIN STORM FORMING" | Photo of the anchor while its phase is Forming | an anchor (not Chaos) | 250 |
   | `rope_out` | "GET THE ROPE-OUT" | Photo of any tornado whose phase is RopingOut | any cell | 200 |
   | `close_call` | "RIDE IT OUT UP CLOSE" | `CloseCallSeconds` in total within `CloseCallRange` of an EF3+ tornado that is on the ground (Mature or RopingOut) | a non-dropped EF3+ cell | 300 |
   | `double_near_miss` | "BACK-TO-BACK NEAR MISSES" | Two near-miss style moments within `NearMissPairWindow` | always | 250 |
   | `drift_by` | "DRIFT PAST A TWISTER" | A drift style moment that ends with a tornado within `DriftByRange` | any cell | 250 |

   Deferred until their systems exist: Airborne Subject (needs debris-in-frame detection), Dual Cataclysm
   (needs a second disaster type), Hero of the Day (needs rescues). A timed bounty fails quietly if its window
   passes (shown struck through); no bounty ever costs score.
4. **In-run scoring.** A completion pays its bonus in **every** run it happens in, once per run per goal.
5. **Accomplishments record.** Per mode, keyed by goal ID: first completion (seed, date, build version) and
   a running count. Bounty IDs are recorded the same way (count only matters for stats). Only the first
   completion of a career goal counts toward rewards.
6. **Rewards (v1).** Completing **5 of the 10** career goals grants the unlock `livery.ktvr` (KTVR News paint
   job on the pickup: a material swap). It enters the `save-profile.md` unlock set like any garage unlock;
   `economy-progression.md` unlocks gain a **source** (Storm Dollars, goal, or both). Until a garage exists,
   an owned livery is switched on with a toggle on the title screen; the choice is saved in `save-profile.md`
   `LastLoadout` (`livery`), so it stays on between sessions.
7. **Events.** The goal tracker raises `GameEvents.GoalCompleted(GoalCompletion)` once per goal per run
   (`Id`, `Kind` Career / Bounty, `Bonus`, `FirstEver`) and `GameEvents.BountyFailed(id)` when a timed window
   closes. HUD, results and presentation (the goal-complete sting) read these only; the tracker holds no
   references to them.
8. **Display.** HUD: the run's 3 bounties as a short list, ticked live; a pop-up when a career goal
   completes ("GOAL: BIG AIR"). Results: a goals block listing this run's completions with **NEW** on
   first-ever completions, and the unlock when one is earned. Title: a career page with the 10 goals, done
   or not, and progress to the reward.
9. **Persistence.** The accomplishments record and unlocks are written at the run-complete checkpoint
   (timer or wreck). Quitting mid-run forfeits the run's goals like everything else from it
   (`save-profile.md`: "nothing from it is banked"). This
   is the first slice of `save-profile.md` to be built and follows its rules: versioned file, unknown IDs
   kept, the run-complete write is one transaction (score, record and unlock together, never one without the
   other).

## RG Formulas

### Score-tier goals
`ScoreGoalMet(tier) = FinalRunScore ≥ ScoreTier[tier]`, evaluated once at run end.

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `FinalRunScore` | float | 0–∞ (Andy's best on 0.7.4: 1,249) | Run score **including** style/storm goal and bounty bonuses |
| `ScoreTier` | float[3] | Rookie 750 · Pro 1,500 · Sick 3,000 | Tier thresholds |

Score-tier goals pay **no** bonus themselves, so a score goal can never count its own bonus.
**Example:** photos 1,100 + `big_air` 150 + `near_misses` 150 + one +250 bounty = 1,650 → Rookie and Pro met,
Sick not.

### Run bonus
`RunGoalBonus = Σ over goals completed this run of Bonus(goal)`, each goal at most once per run.

| Goal kind | `Bonus` | Rationale |
|-----------|---------|-----------|
| Career, Style or Storm | `CareerBonus` = 150 | Five goals in one run (+750) ≈ one PERFECT EF5 in wind (800): goals add to photography, never replace it |
| Career, Score | 0 | See above |
| Bounty | Per the v1 pool table (Rule 3): 200–500 | Point-Blank EF5 keeps its section 3 value; timed and up-close bounties (300) pay for time pressure and risk |

Expected range per run: 0 to about 2,100 (all seven style/storm goals plus the three richest bounties). That is
an upper bound; a typical run is expected to land 1–3 goals and 1–2 bounties (+350 to +1,000).
**Example:** `rope_out` 200 + `drift_by` 250 + `big_air` 150 = +600.

### Bounty draw
1. **Eligible pool** `E` = the v1 bounties (Rule 3 table) whose "eligible when" column the run's Weather
   Plan satisfies, judged on **non-dropped** cells. `rope_out`, `double_near_miss` and `drift_by` are eligible
   in every plan with at least one cell, so every director run draws the full 3. **Pacing cells count**
   (storm-director.md Rule 12, compact pacing fill, 2026-10-05): they are real storms the player can shoot and
   ride out, so an EF3 closer makes `warning_cell` and `close_call` eligible on a seed with no regime EF3+. Since
   that change, such seeds can draw different bounties than before it (pacing cells are never EF5, and every plan
   already had a cell, so only the EF3+ bounties move).
2. `n = min(BountiesPerRun, |E|)` bounties are drawn **without replacement** with `DirectorRng(runSeed,
   stream 3)`: a Fisher–Yates shuffle of `E` in catalogue order, first `n` taken. Stream 3 is reserved for
   bounties (director streams: 1 plan, 2 placement, 100+id tracks, 1000+id forecast), so adding or changing
   bounties never changes the weather.
3. Same seed + same build → same bounties (replay with `?seed=N`).

### Goal thresholds

| Goal | Condition | Default | Rationale |
|------|-----------|---------|-----------|
| `point_blank` | Photo with subject distance `< PointBlankDistance` | 20 m | Inside the 60 m shutter range, outside the < 5 m "inside the tornado" zone where DistanceScore = 0 |
| `storm_drift` | A drift style moment with duration ≥ `DriftSeconds` and a tornado within `DriftStormRange` **when the drift ends** (the event fires once, at slide end, with its duration) | 3 s, 60 m | 60 m = Storm Cam / shutter range |
| `big_air` | An airtime style moment with counted seconds ≥ `BigAirSeconds` (counted = time above `MinAirtimeHeight` 1.8 m, `vehicle-feel.md` E2) | **1.0 s** | Measured (`production/qa/evidence/rg-airtime-evidence.md`): jumps 0.00 s (peak 1.55 m), EF4 toss 1.12 s, EF5 toss 1.36 s. Earned by a toss for now (Andy, 2026-10-04); revisit when ramps / bigger jumps exist |
| `near_misses` | `NearMissGoal` near-miss style events in one run | 3 | `NearMissCooldown` 3 s keeps them spread out |
| `ef4_peak` | Photo of a subject with EF ≥ 4 whose phase is Mature | EF 4 | Peak is the money shot (`storm-director.md` F3) |
| `front_page` | Photo with tier PERFECT whose subject is the run's anchor | — | Anchor = the run's main storm |
| `toss_survivor` | ≥ 1 toss this run and the run ends on the timer | — | Not on a wreck or a quit |
| `warning_cell` | Photo of the named cell within `TimedBountyWindow` of its warning crawl | 40 s | About one forecast ETA step |
| `point_blank_ef5` | Photo of an EF5 subject closer than `PointBlankEf5Distance` | 12 m | Section 3 value, now in metres |
| `close_call` | Total time within `CloseCallRange` of an EF3+ tornado on the ground | 5 s, 30 m | Inside an EF3's shove range, outside its damage radius for a careful driver |
| `double_near_miss` | Two near misses within `NearMissPairWindow` | 10 s | `NearMissCooldown` 3 s makes 2 within 10 s possible but deliberate |
| `drift_by` | Drift style moment ending with a tornado within `DriftByRange` | 40 m | Closer than `storm_drift`'s 60 m, no duration floor |

### Reward
`livery.ktvr` is granted when `|{career goals with a recorded first completion}| ≥ RewardThreshold`
(default 5 of 10), checked at every run-complete checkpoint.
**Example:** a profile with 4 first completions finishes a run that adds `big_air` → 5 → unlock granted in
that checkpoint, usable next run.

## RG Edge Cases

| Situation | What happens |
|-----------|--------------|
| Run ends in a **wreck** | Goals completed before the wreck pay their bonus and are recorded. `toss_survivor` is not met (it needs a timer end). Score tiers are judged on the wrecked run's final score |
| Player **quits to title** mid-run | The run is forfeited: its goals are **not** recorded and nothing from it is banked, matching `save-profile.md` and the pause menu's "Nothing from this run is banked." (Andy, 2026-10-04) |
| One photo satisfies **several goals** (e.g. `front_page` + `ef4_peak` + `point_blank` + a bounty) | All complete; bonuses stack |
| A goal is met **twice in one run** | Pays once; the record's count rises by 1 for that run |
| **Replay** (`?seed=N`), including a different build version | Goals count normally. Single-player; replaying a seed to hunt a goal is intended |
| The bounty's storm **never forms or fails to touch down** | Dropped cells are already excluded from eligibility. A later failure (early rope-out) just leaves the bounty incomplete at run end; no penalty |
| A timed bounty's window **expires** | `BountyFailed` raised; shown struck through; no penalty. It does not re-arm on a later warning |
| `warning_cell`'s named cell **ends before it can be photographed** (early rope-out inside the window) | It can still be photographed while roping out; once it has Ended the bounty fails |
| No TORNADO WARNING fires all run (its EF3+ cell was evicted before forming) | `warning_cell` stays open and fails at run end; no penalty |
| **Save write fails** (disk or storage full, write error) | The run plays normally; the record and unlock stay in memory and are retried (`save-profile.md`: "Couldn't save — progress kept, will retry"). Results still show NEW tags and the unlock |
| **Profile is read-only** (newer version, migration failed, open in another window) or browser storage blocked | Goals still pay in-run bonuses but nothing is recorded or unlocked; Results show `save-profile.md`'s reason-specific "Progress not saved (...)" line instead of NEW tags |
| **Livery earned** this run | Granted at the run-complete checkpoint; usable from the next run. Results show the unlock |
| **Old save** without an accomplishments record | Loads with an empty record (migration default). Goal or unlock IDs this build doesn't know are kept (`save-profile.md` Core Rules) |
| A **reward threshold is lowered** in a later build, or goals are added | Re-checked at every checkpoint, so qualifying profiles get the unlock at their next run end |
| Legacy spawner (`?spawner=legacy`) | No Weather Plan: bounties are not drawn; career goals that need an anchor (`front_page`) cannot complete; other goals work |

## RG Dependencies

| System | Direction | Interface |
|--------|-----------|-----------|
| `photo-scoring.md` / `PhotoTrigger` | Upstream (hard) | `PhotoTaken` (`PhotoResult`: tier, subject, subject position) for `point_blank`, `ef4_peak`, `front_page` and photo bounties |
| `vehicle-feel.md` | Upstream (hard) | `StyleEvent` (slide duration, airtime, near miss) and `Tossed` |
| `storm-director.md` | Upstream (hard) | Weather Plan (anchor, non-dropped cells, EFs) for bounty eligibility; `DirectorRng` stream 3 for the draw; `StormCellForming` / phase for the timed bounty and `ef4_peak` |
| Run Manager (`session-modes.md`, `RunManager`) | Upstream (hard) | Run start, run end reason (timer / wreck / quit), `RunSummary` |
| `save-profile.md` | Downstream (hard) | New `Accomplishments` record per mode; run-complete checkpoint writes record + unlocks + score as one transaction |
| `economy-progression.md` | Both (soft) | Unlock IDs gain a **source** (Storm Dollars / goal / both); `livery.ktvr` is the first goal-sourced unlock. Goal bonuses are score, so they convert to Storm Dollars 1:1 |
| HUD + Run Screens (Claude lane) | Downstream (hard) | Bounty list, career-goal pop-up, results goals block with NEW, title career page, livery toggle |
| Presentation (Codex lane) | Downstream (soft) | Goal-complete sting; `livery.ktvr` material on the pickup |

## RG Tuning Knobs

| Knob | Default | Safe range | Affects |
|------|---------|-----------|---------|
| `ScoreTier` (Rookie / Pro / Sick) | 750 / 1,500 / 3,000 | ±50 % each, ascending | How far into the career a new player gets on score alone |
| `CareerBonus` | 150 | 50–300 | Goal value vs photos; above ~300, goal hunting outscores photography |
| Bounty bonuses (Rule 3 table) | 200–500 | 100–800 each | Willingness to divert for each bounty |
| `BountiesPerRun` | 3 | 1–4 | In-run pressure; 4+ crowds the HUD |
| `TimedBountyWindow` | 40 s | 25–60 s | Timed bounty difficulty |
| `PointBlankDistance` | 20 m | 12–30 m | Risk demanded by `point_blank` |
| `DriftSeconds` / `DriftStormRange` | 3 s / 60 m | 2–5 s / 30–80 m | `storm_drift` difficulty |
| `BigAirSeconds` | 1.0 s (measured) | `MinStyleSeconds` – 1.3 s counted (above 1.3 s only an EF5 toss qualifies) | `big_air` difficulty; jumps can't earn it until ramps exist |
| `PointBlankEf5Distance` | 12 m | 8–20 m | `point_blank_ef5` risk |
| `CloseCallSeconds` / `CloseCallRange` | 5 s / 30 m | 3–10 s / 20–45 m | `close_call` risk |
| `NearMissPairWindow` | 10 s | 6–15 s | `double_near_miss` difficulty |
| `DriftByRange` | 40 m | 25–60 m | `drift_by` difficulty |
| `NearMissGoal` | 3 | 2–6 | `near_misses` difficulty |
| `RewardThreshold` | 5 | 3–8 | How soon the first unlock lands; 5 of 10 targets the first few sessions |

## RG Acceptance Criteria

**Unit (EditMode)**
- [ ] Same seed and build → the same bounties in the same order; seeds 1–1,000 never draw a bounty the plan
      cannot satisfy (no `point_blank_ef5` without an EF5 cell), and every one of them draws exactly 3
- [ ] No bounty's HUD text contains an EF number or any digit
- [ ] Bounty draw uses stream 3 only: a plan built with and without bounties serializes identically
- [ ] Score tiers: final score 1,499 meets Rookie only; 1,500 meets Rookie and Pro; score goals add no bonus
- [ ] `RunGoalBonus`: a goal met twice in a run pays once; one photo meeting four goals pays all four
- [ ] Thresholds at the boundary: drift 2.99 s fails, 3.0 s passes; counted airtime just under / at
      `BigAirSeconds`; photo at 19.9 m passes `point_blank`, 20.0 m fails; `double_near_miss` at 9.9 s passes,
      10.1 s fails
- [ ] Reward: 4 recorded career goals + 1 new → `livery.ktvr` granted at that checkpoint; already owned → no
      duplicate
- [ ] Record round trip: save → load → identical; an unknown goal ID survives load and re-save

**Integration (PlayMode)**
- [ ] A scripted run that drifts 3 s beside a live storm and photographs the anchor PERFECT completes
      `storm_drift` and `front_page`, raises one goal event each, and the results summary lists both
- [ ] A wrecked run records goals done before the wreck and does not meet `toss_survivor`; a run quit to title
      records nothing
- [ ] `GoalCompleted` fires once per goal per run with the right `Kind`, `Bonus` and `FirstEver`; a timed window
      closing raises `BountyFailed` once
- [ ] **Achievability (before release):** every career goal has been completed at least once in a real compact
      run (seed recorded). Measured first, before `big_air` is built: counted airtime from a jump, a boosted
      jump off the steepest terrain, and a toss on the compact map. If no route reaches `MinStyleSeconds`,
      `big_air` is swapped for another style goal before RG-2 ships
- [ ] A failed save write (simulated) leaves the run playable, shows the retry toast and keeps the NEW tags; a
      ReadOnly profile records nothing and Results show its reason line

**UI (retained screenshots in `production/qa/evidence/`)**
- [ ] HUD with 3 bounties, one ticked and one failed; career-goal pop-up
- [ ] Results goals block with a NEW tag and the livery unlock line
- [ ] Title career page with progress to the reward; livery toggle on, truck in KTVR paint, still on after a
      reload (`LastLoadout`)

**Playtest**
- [ ] Outside players can say what a bounty asks without explanation, and at least one chases a career
      goal on purpose within their first 5 runs
