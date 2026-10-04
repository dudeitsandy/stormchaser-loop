# G3 Playtest Session — Sprint 7 close

> **Build:** 0.7.3 on itch (https://ghostweavelabs.itch.io/doomsday). html5 for outside players, windows
> build for Andy's live checks.
> **Length:** Andy ≈ 60–90 min. Outside players ≈ 15 min each, can be remote and async.
> **Closes:** Sprint 7 (`production/sprints/sprint-07-build.md`). Results feed Sprint 8 and the
> S8-C1 durability tuning.
> **Date run:** ____________

The session has three parts. Part A is the G3 feel gate from `vehicle-feel.md` Acceptance Criteria.
Part B is Andy's live acceptance of Codex's shipped presentation work. Part C covers the measurements
and decisions. Claude does the technical prep before the session so that Andy's time goes only to things
that need a human.

---

## Before the session — Claude prep (no Andy time)

- [x] Confirm 0.7.3 is still the live build on itch, and check html5 in Chrome for 0 console errors. *(2026-10-04: title shows PROTOTYPE 0.7.3, 0 errors, 2 known FSR-shader-stripped warnings; `evidence/g3-prep-itch-073-title.png`)*
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
> Hey, can you play 5 runs of my storm-chaser prototype (browser, 90 seconds each)? Link: https://ghostweavelabs.itch.io/doomsday.
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

**X7-07 — knock-loose props**
- [ ] Bales, mailboxes, signs and crates fly when hit; the truck keeps most of its speed
- [ ] Trees, barns, silos and poles block solidly

---

## Part C — Decisions (Andy, ≈ 10 min)

- [ ] **C1 — G3 verdict:** PASS / PASS with tuning notes for Sprint 8 / FAIL (one more feel pass first)
- [x] **C2 — Hay bale conflict:** resolved 2026-10-03 (Claude, Codex acked): (a) a 250 kg bale registers a
      light impact with no HP cost; `LooseSceneryPlayTests` covers it. Andy can still overrule here.
- [ ] **C3 — X7-06 budget:** accept the GPU/browser evidence (0.68 ms mean / 1.58 ms p95 under strong wind)
      as enough for now and move the Unity CPU capture to Sprint 10's WebGL budget criterion, or block on it now
- [ ] **C4 — Durability input for S8-C1:** after these runs, how many hits should a careful run survive?
      ____  How about a reckless one? ____
- [ ] **C6 — S7-06 physics caveat:** accept the PASS (≤ 1.2 ms with vehicle + tornado + props; G1 already covered
      60-piece debris at ≤ 1.5 ms), or do one deliberate prop-cluster smash with `?physProbe=1` on the local dev
      build and read the `[PHYS-RESULT]` line from the browser console
- [ ] **C5 — Start Sprint 8** (`sprint-08-storm-director.md`): yes / adjust

---

## After the session — Claude

- [ ] Log findings into `sprint-07-build.md` as "Playtest Notes — G3", in the same triage-table format as earlier passes
- [ ] Record tuned defaults or new findings in `vehicle-feel.md`'s Playtest Tuning Log, and tick the Feel ACs that passed
- [ ] File failed Part B items as Codex requests in AGENTS.md
- [ ] Tick the Sprint 7 DoD rows, mark Sprint 7 closed, set Sprint 8 to Active
- [ ] Commit the evidence to `production/qa/evidence/` (Andy's F9 captures from Part B, plus the physics and CPU results)
