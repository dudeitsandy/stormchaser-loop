# Smoke: Run Goals achievability (run-goals-v1 story 008, RG-008)

> **Purpose:** each of the 10 career goals done once in a real compact run before goals go to outside players.
> **Owner:** Andy (play) · sheet by Claude 2026-10-05 · **Build:** 0.8.3 or later
> **Pass:** every row has a seed, build and date. A goal you can't get in 3 tries on its seed → note why in the row;
> it gets retuned or swapped with Andy and re-verified (story 008 AC 2).

## How to play a seed
- **Windows (recommended):** `builds\windows\Doomsday.exe -seed=554` (or a shortcut with that argument).
- **Browser (local build):** `index.html?seed=554` (itch's embed doesn't pass the URL through).
- On the title, **C** (or Y) shows the career page: what's done and what's left. Progress saves between runs, so one
  goal per run is fine.

## Seeds
Every seed's main storm (the anchor) touches down at ≈ 72–75 s and stays at peak until ≈ 110 s, then ropes out.
Found with `AttractSeedProbe.ListGoalSmokeSeeds` (Explicit probe), positions at run start.

| Seed | Main storm | Where | Other storms | Good for |
|------|-----------|-------|--------------|----------|
| **554** | **EF5** at ≈ 80 s | — | — | EF4 at peak, toss (BIG AIR + TOSS SURVIVOR), point-blank |
| **210** | EF4, peak 74–112 s | 62 m out | 3 cells, 2 are EF3+ | front page, storm drift, near misses |
| **667** | EF4, peak 73–111 s | 62 m out | 4 cells | near misses (most cells), storm drift |
| 69 | EF4, peak 72–110 s | 73 m out | lone giant | a calm run to set up the front-page shot |

## Goals

| # | Goal (career page) | What counts (thresholds from `GoalTuning`) | Try on | Seed | Build | Date | Done |
|---|---|---|---|---|---|---|---|
| 1 | ROOKIE SCORE | Run score ≥ 750 | any | | | | |
| 2 | PRO SCORE | Run score ≥ 1,500 | 554 | | | | |
| 3 | SICK SCORE | Run score ≥ 3,000 (expect this to be the hard one: note your best if it doesn't fall) | 554 | | | | |
| 4 | EF4 AT PEAK | Photo of an EF4+ while it's at peak (mature, not forming or roping) | 554 / 210, 75–110 s | | | | |
| 5 | POINT BLANK | Photo of any tornado from under 20 m | 554 | | | | |
| 6 | TOSS SURVIVOR | Get tossed by a tornado, then **finish the run on the timer** (not wrecked, not quit) | 554, EF5 | | | | |
| 7 | STORM DRIFT | One drift of 3 s or longer, ending within 60 m of a tornado | 210 / 667 | | | | |
| 8 | BIG AIR | 1.0 s of airtime. **Right now only a toss gets there** (jumps peak ≈ 1.55 m, under the 1.8 m airtime floor: S9-02a fixes this). Do it with the toss in #6 | 554 | | | | |
| 9 | NEAR MISSES | 3 near misses in one run | 667 | | | | |
| 10 | FRONT PAGE | A **Perfect** shot of the main storm (the anchor): centred, close, at peak | 210 / 69, 75–110 s | | | | |

## Notes
- A toss costs truck HP. If the EF5 wrecks you, TOSS SURVIVOR doesn't count (the run must end on the timer): get tossed,
  then drive clear and survive to 0:00.
- Score goals stack with the rest: a big storm run on 554 with a few good photos is the best bet for PRO and SICK.
- Write anything that felt impossible or unfair in the row; that's the point of this check.

## Result
- [ ] All 10 done → tick M1 "run goals" in `production/milestones/milestone-1-vertical-slice.md`, close story 008.
