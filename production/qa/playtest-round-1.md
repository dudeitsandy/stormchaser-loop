# Playtest Round 1 — Findings Intake (PT-1)

> **Build:** 0.7.5 (testers may be on a later itch build; record which one)
> **Brief:** Google Doc "Doomsday — Playtest Brief (0.7.5)", 15 questions in four sections
> **Due:** findings in this file by **2026-10-11** (Sprint 8 PT-1)
> **Feeds:** Sprint 9 S9-01 driving triage → S9-02 feel pass; S9-05 non-driving triage; G6 feel sign-off

## How to use this file

1. **Andy:** for each tester, paste their answers into a tester block below **verbatim**. Don't summarize; the
   exact words ("floaty", "twitchy", "cheap") are the data. A message instead of the form is fine: paste it
   under "Free-form" and leave the question rows blank.
2. **Public repo:** use tester codes (P1, P2, …) only. No names, emails or handles. Keep the code-to-person
   mapping outside the repo.
3. **Claude** fills the tally and the triage tables (S9-01, S9-05) once two or more testers are in. Andy picks
   values in the "Andy pick" column.

---

## Tester blocks

Copy this block once per tester.

### P1

| Field | Answer |
|---|---|
| Build played | |
| Browser or Windows | |
| Keyboard or controller | (not in the brief; ask if they didn't say) |
| Prior genre experience | (optional: racing / Rocket League / storm games) |

**Runs**

| Run | Seed | Score | Wrecked? | Notes |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |

**Answers** (brief numbering: each section restarts at 1; the Q column is the running number Sprint 9 uses)

| Q | Section / brief # | Question (short) | Answer (verbatim) |
|---|---|---|---|
| 1 | Storms 1 | Saw storms coming before they were a threat? Unfair surprises? | |
| 2 | Storms 2 | An "oh no" escape? What happened? | |
| 3 | Storms 3 | EF4/EF5 bigger and scarier? Did TORNADO EMERGENCY land? | |
| 4 | Storms 4 | Forecast panel: clear, ignored or confusing? | |
| 5 | Storms 5 | Any boring stretch? | |
| 6 | Driving 1 | Steering: too loose, too twitchy, about right? | |
| 7 | Driving 2 | Jump and landings: floaty, heavy, good? Stuck or bad landing? | |
| 8 | Driving 3 | Braking, drifting, boost: anything off? | |
| 9 | Driving 4 | One word for the handling | |
| 10 | Damage 1 | Damage fair? Unexpected cost, or a big hit with no damage? | |
| 11 | Damage 2 | Wrecked? Felt like your fault? | |
| 12 | Overall 1 | Best moment | |
| 13 | Overall 2 | Most frustrating moment | |
| 14 | Overall 3 | Play another run now? Why? | |
| 15 | Overall 4 | Bugs, ideas, things that looked wrong | |

**Free-form** (paste messages, voice-note transcripts, screenshots described)

>

---

## Tally (Claude, once 2+ testers are in)

| Q | Signal | P1 | P2 | P3 | Count | Read |
|---|---|---|---|---|---|---|
| 6 | Steering | loose / twitchy / right | | | | |
| 7 | Jump & landing | floaty / heavy / good; stuck? | | | | |
| 8 | Brake / drift / boost | issue named | | | | |
| 9 | Handling word | | | | | |
| 1 | Storms readable early | yes / no | | | | |
| 2 | "Oh no" moment | yes / no | | | | |
| 3 | EF5 lands | yes / no / didn't see one | | | | |
| 10–11 | Damage fair | yes / no | | | | |
| 14 | Would replay | yes / no | | | | |

**G6 inputs** (`vehicle-feel.md` feel criteria): powerslid on purpose by run 2? said "heavy"? said
"responsive"? drift or air photo by run 5? EF5 toss "cheap"? The brief doesn't ask these directly. Read them
from the answers and run notes, and mark *inferred* where that's what it is.

---

## S9-01 — Driving triage → `vehicle-feel.md` knobs (Claude proposes, Andy picks)

Current values are `VehicleFeelValues.Defaults` (`Scripts/Vehicle/Model/VehicleFeelConfig.cs`) as of 0.7.8.
Change a knob only when the complaint repeats across testers or matches Andy's own notes (sprint-09 "Known input from
Sprint 8": "steering a little loose", "jump a touch floaty", plus his 0.7.8 driving and camera notes).

| Complaint (typical words) | Knob(s) | Current | Direction | Proposed | Testers | Andy pick |
|---|---|---|---|---|---|---|
| Steering loose / slidey / boaty | `GripMu`, `GripStiffness`, `YawStability` | 1.15, 1.7, 0.6 | up | | | |
| Steering twitchy / darty | `MaxSteerDeg`, `HighSpeedSteerFactor`, `YawStability` | 32°, 0.45, 0.6 | down / down / up | | | |
| Jump floaty | `AirGravityMul`, `FallGravityMul`, `JumpSpeed` | 1.5, 2.0, 6.65 m/s | up / up / hold apex | | | |
| Jump heavy / weak | `JumpSpeed`, `AirGravityMul` | 6.65 m/s, 1.5 | up / down | | | |
| Landings harsh or bouncy | `DampingRatio`, `SagFraction` | 0.52, 0.32 | tune | | | |
| Air control too much / too little | `AirAccel`, `AirMaxRate` | 20, 4.5 | tune | | | |
| Brakes weak / too grabby | `BrakeDecel`, `BrakeAssistDecel` | 14, 8 m/s² | tune | | | |
| Drift hard to start / hard to hold | `HandbrakeGripBase`, `SlideEnterDeg`, `SlideExitDeg` | 0.45, 20°, 10° | tune | | | |
| Boost runs out too fast / never matters | `BoostDrain`, `BoostPassiveRegen`, refills | 33, 4, slide 18 / air 14 / near-miss 25 | tune | | | |
| Wind yanks the wheel | `WindSteer`, `WindSteerMax` | 0.35, 0.5 | down | | | |
| Camera too high / storms off screen | chase camera pitch (S8-08: 12°) | 12° | tune | | | |

Guardrails: changing a threshold (slide angle, airtime height, near-miss) moves style refills and run goals
(`big_air` uses `MinAirtimeHeight` 1.8 m). Check `vehicle-feel.md` sanity numbers and keep the PlayMode vehicle
tests green (Sprint 9 risks).

---

## S9-05 — Non-driving findings (Claude triages, Andy confirms)

| # | Finding | Testers | Seed / time | Area | Fix now (M1) or backlog | Where |
|---|---|---|---|---|---|---|
| | | | | Storms / Damage / Camera / UI / Bug | | sprint file, AGENTS.md (Codex) or `production/backlog.md` |

Freeze rule applies: fix now only if it's ≤ ~2 h, adds no new system and touches no save data
(`milestone-1-vertical-slice.md` → Design freeze). Everything else goes to the backlog.

**Damage reference** (for Q10–11): Pickup 6 HP; Light impact from 15.5 m/s, Severe from 23.25 m/s
(S8-C1, `vehicle-damage.md`). S8-C2 per-truck HP redesign is deferred past M1, so damage complaints tune these
values and don't reopen the redesign.
