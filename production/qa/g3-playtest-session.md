# G3 + G4 Playtest Session — Sprint 7 close, Sprint 8 gates

> **Build:** 0.7.4 on itch (Storm Director live, 3-minute runs; updated 2026-10-04) (https://ghostweavelabs.itch.io/doomsday). html5 for outside players, windows
> build for Andy's live checks.
> **Length:** Andy ≈ 60–90 min. Outside players ≈ 15 min each, can be remote and async.
> **Closes:** Sprint 7 (`production/sprints/sprint-07-build.md`) and Sprint 8's G4 storm-tension gate
> (0.7.4 already has the Storm Director). Results feed the Sprint 8 re-scope and the S8-C1 durability tuning.
> If S8-C1 ships before the session, run G5 too (Part A2).
> **Date run:** ____________

The session has three parts. Part A is the G3 feel gate from `vehicle-feel.md` Acceptance Criteria, and
Part A2 is the G4 storm-tension gate (plus G5 durability if S8-C1 is in the build).
Part B is Andy's live acceptance of Codex's shipped presentation work. Part C covers the measurements
and decisions. Claude does the technical prep before the session so that Andy's time goes only to things
that need a human.

---

## Before the session — Claude prep (no Andy time)

- [x] Confirm the live itch build and check html5 for 0 console errors. *(0.7.3 checked 2026-10-04,
      `evidence/g3-prep-itch-073-title.png`; superseded by 0.7.4, verified at release with 0 WebGL console
      errors, `evidence/v074_*.png`)*
- [x] **S7-06 physics measurement (G1 condition).** *(2026-10-04: **PASS with a stress caveat.** Max step 1.20 ms,
      avg 0.25 ms, 0 steps over 4 ms across ~180 s of scripted boosted driving with 25 vehicle impacts; but debris
      peaked at only 3 awake bodies. `evidence/s7-06-physics.md`. Andy decides on the caveat in C6.)*
- [ ] **X7-06 CPU capture: not done.** It needs the Unity Profiler attached to a running development build,
      which the headless setup here can't do. Codex hit the same wall. Andy decides in C3.
- [ ] **Andy:** send the outside-player message below to the 2 players (they can play before the session).

---

## Part A — G3 feel gate (Andy + 2 outside players, 5 runs each)

**Rules for observing:** don't explain powerslide, drift photos or the Storm Cam. Controls are on the title
screen, and that's all players get. Note exact quotes, especially "that's cheap", "floaty", "loose", "heavy".

### Message to outside players
> Hey, can you play 5 runs of my storm-chaser prototype (browser, 3 minutes each)? Link: https://ghostweavelabs.itch.io/doomsday.
> Controller recommended, keyboard fine. Afterward tell me: (1) did the truck feel heavy? (2) did it feel
> responsive? (3) anything that felt cheap or unfair? (4) one thing you'd change. Screenshots or a quick video
> of anything weird would help.

### Per-player log

| Check (GDD criterion) | Andy | Player 2 | Player 3 |
|---|---|---|---|
| Powerslid **on purpose** within 2 runs, unprompted | | | |
| "Did the truck feel **heavy**?" (yes/no + quote) | | | |
| "Did it feel **responsive**?" (yes/no + quote) | | | |
| At least 1 **drift or airborne photo** by run 5 | | | |
| EF5 toss read as **exciting/funny, not unfair** (watch for "that's cheap") | | | |
| Best score over 5 runs | | | |
| Got stuck or wedged? (run #, where) | | | |
| One thing they'd change | | | |

**G3 passes when** every row's criterion holds for all three players. A single miss isn't an automatic
fail. Record it, and Andy calls it in C1.

### Andy's own feel notes (the open "a little loose" finding from 0.7.1)
- [ ] Straight-line stability at top speed: loose / right / stiff
- [ ] Turn-in at mid speed: loose / right / stiff
- [ ] Brake to stop feels Rocket League-quick (≈ 1.1 s from full speed)
- [ ] Jump arc no longer floaty
- [ ] Nose-first landings never leave the truck wedged on two wheels

---

## Part A2 — G4 storm tension (Andy, 5 seeded runs, ≈ 20 min; fold into Part A's runs)

From `sprint-08-storm-director.md` G4. Note each run's seed and regime from the results screen.

| Run | Seed / regime | Storms visible before they mattered? | "Oh no" escape? | EF5 seen? Rare and imposing? |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |

**G4 passes when** storms read before they matter on most runs, at least one run has a real "oh no" escape,
and EF5 feels rare and imposing rather than common or pale. Codex already flagged the EF5 wedge as reading
pale; note whether that's true.

**G5 durability (only if S8-C1 is in the build):**
- [ ] Light bumps cost nothing
- [ ] A toss or a barn hit at speed clearly hurts
- [ ] A full 3-minute run is survivable with care

---

## Part B — Codex live acceptance (Andy, windows build, ≈ 20 min)

Tick each item that fires correctly. Any item that fails becomes one line in AGENTS.md Requests for Codex.

**X7-01 / X7-08 — viewfinder = roof cab cam**
- [ ] The viewfinder shows what the shot will score, in chase, orbit and Storm Cam

**X7-02 — vehicle VFX**
- [ ] Tire smoke while sliding (handbrake drift)
- [ ] Landing dust on a hard landing
- [ ] Sparks on a wall or prop impact
- [ ] Boost flame and streaks start and stop with boost
- [ ] "DRIFT n.ns", "AIR n.ns" and "NEAR MISS" pops appear mid-screen and stay clear of the HUD

**X7-03 — vehicle audio (listening check)**
- [ ] Engine pitch follows speed and load
- [ ] Skid sound follows slip
- [ ] Suspension thump on landing
- [ ] Boost ignition whoosh, roar while held, fade on release
- [ ] Metal crunch gets louder with impact severity

**X7-04 — tornado reads as one funnel**
- [ ] One continuous funnel mass, no stacked ribbons, in both the main view and the viewfinder
- [ ] The funnel descends from the cloud base while forming and retracts upward on rope-out

- [ ] Tall cloud-to-ground funnels sway without detaching from the cloud (0.7.4)

**X7-07 — knock-loose props**
- [ ] Bales, mailboxes, signs and crates fly when hit; the truck keeps most of its speed
- [ ] Trees, barns, silos and poles block solidly

**X7-09 — storm telegraph presentation (Sprint 8)**
- [ ] Forecast indicator and world cues point the right way; a jog visibly bows before the storm turns
- [ ] EF5 reads dark and rumbles; the failed-touchdown cue reads as a storm that didn't drop

**X7-10 — broadcast alert tones (Sprint 8)**
- [ ] Three-tone warning on EF3+; a longer emergency tone for EF5, then radio static

**X8-01 — outdoor warning sirens (Sprint 8)**
- [ ] Sirens wail from a direction and get louder near a pole; 25 s per warning, continuous for an EF5

**X8-02 — run-wide storm sky (Sprint 8)**
- [ ] The whole sky darkens as storms build (near-black for an EF5) and eases back slowly after rope-out

---

## Part C — Decisions (Andy, ≈ 10 min)

- [x] **C1 — G3 verdict:** PASS / PASS with tuning notes for Sprint 8 / FAIL (one more feel pass first)
      → **PASS with tuning notes** (Andy, 2026-10-04 on 0.7.5: "will need to tune driving at some point a little
      more"). The driving pass is Sprint 9 S9-01/02, fed by outside playtest round 1.
- [x] **C2 — Hay bale conflict:** resolved 2026-10-03 (Claude, Codex acked): (a) a 250 kg bale registers a
      light impact with no HP cost; `LooseSceneryPlayTests` covers it. Andy can still overrule here.
- [x] **C3 — X7-06 budget:** *resolved 2026-10-04: M1's criterion reworded to whole-frame WebGL budgets
      measured headlessly; the per-component CPU capture runs only if that fails.* Original question: accept the GPU/browser evidence (0.68 ms mean / 1.58 ms p95 under strong wind)
      as enough for now and move the Unity CPU capture to Sprint 10's WebGL budget criterion, or block on it now
- [x] **C4 — Durability input for S8-C1:** after these runs, how many hits should a careful run survive?
      → settled by S8-C1 (Pickup 6 HP, light bumps free) passing G5 on 0.7.5. S8-C2 deferred past M1.
- [ ] **C6 — S7-06 physics caveat:** accept the PASS (≤ 1.2 ms with vehicle + tornado + props; G1 already covered
      60-piece debris at ≤ 1.5 ms), or do one deliberate prop-cluster smash with `?physProbe=1` on the local dev
      build and read the `[PHYS-RESULT]` line from the browser console
- [x] **C7 — G4 verdict** (and G5, if run): PASS / tuning notes / FAIL → **G4 PASS, G5 PASS** (Andy, 2026-10-04, 0.7.5)
- [x] **C5 — Sprint 8 re-scope** (`sprint-08-storm-director.md`): the epic is done, so pull in run goals v1,
      run screens and the S8-C2 decision from Sprint 9? yes / adjust → **yes** (2026-10-04): run goals + run
      screens in Sprint 8; S8-C2 deferred past M1 (Andy); driving pass in Sprint 9 (`sprint-09-feel-and-playtest.md`)

---

## After the session — Claude

- [ ] Log findings into `sprint-07-build.md` as "Playtest Notes — G3", in the same triage-table format as earlier passes
- [ ] Record tuned defaults or new findings in `vehicle-feel.md`'s Playtest Tuning Log, and tick the Feel ACs that passed
- [ ] File failed Part B items as Codex requests in AGENTS.md
- [ ] Tick the Sprint 7 DoD rows, mark Sprint 7 closed; ~~log G4 (and G5) in Sprint 8 and re-scope it per C5~~ (done 2026-10-04)
- [ ] Commit the evidence to `production/qa/evidence/` (Andy's F9 captures from Part B, plus the physics and CPU results)
