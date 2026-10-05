# Sprint 9 — Driving Feel + Outside Playtest (2026-10-06 → 2026-10-19)

> **Status:** **ACTIVE from 2026-10-06** (Andy, via stormchaser-38, 2026-10-05: start now after 0.8.3; re-dated from
> 10-20 → 11-02 to close the gap left by Sprint 8's early close). S9-02 is split: **S9-02a** starts now from Andy's own
> notes; **S9-02b** waits for PT-1. Sprint 9's original M1 work (run goals v1, run screens) moved into Sprint 8.
> **Milestone:** M1 — Vertical Slice 0.9 (`production/milestones/milestone-1-vertical-slice.md`)

## Sprint Goal
The truck feels right to people who aren't me: a focused `vehicle-feel.md` tuning pass driven by outside
playtest round 1, checked by a second round on the tuned build. Closes M1's "feel tuned from G3" criterion.

## Capacity
- 2 weeks, Andy 8–10 h/week; Claude implements. Codex: presentation follow-ups only if the feel pass
  changes what its VFX/audio read (drift, landing, boost thresholds).
- Design freeze in effect (M1): tuning values change in `vehicle-feel.md`; no new mechanics.

## Inputs
- Outside playtest round 1 (Sprint 8 PT-1, tester brief in Google Docs): driving questions 6–9, damage 10–11.
- Andy's open notes: steering a little loose, jump a touch floaty (G3, 2026-10-03); "will need to tune
  driving at some point a little more" (G4, 2026-10-04).

## Known input from Sprint 8
- **Andy, 2026-10-04 (0.7.8):** rain "looks and feels good". Driving: **drift / e-brake** needs more of a driver feel;
  **camera turn on drift and turn** needs work (how the chase camera swings and lags through slides and corners);
  **lower the camera** for a better sky perspective, especially with tornadoes forming and rain (S9-02b).
- Jumps peak ≈ 1.55 m, below `MinAirtimeHeight` 1.8 m, so the Airtime style moment and its boost refill never fire
  from a jump on the compact map, only from tosses (`production/qa/evidence/rg-airtime-evidence.md`). Consider in
  S9-02 alongside the "floaty jump" note.

## Andy's 0.8.4 feel check (2026-10-05)
- Drift and counter-steer "feel pretty good". The camera slide lean was "a little too jarring": eased (0.6 s in/out)
  and softened (35 % → 20 %).
- **Run pacing, quick fix in (Andy: "fit in an early and late storm"):** Storm Director compact pacing fill, an
  opener storm by ≈ 11 s and a closer on the ground at 160 s (`storm-director.md` Rule 12 → Compact pacing fill).
  Median empty time 98 → 49 s of 180. The bigger pacing plan (non-storm activity) stays in the backlog.
  **PT-1 testers are on 0.8.3 (no fill):** their Q5 "boring stretch" answers are the *before* baseline; ask the
  same question in round 2 on the fill build.
- **Crash weight, low-end camera shake in** (Andy: "noticeable but not disruptive"; no tester has started):
  jolts on HP-costing impacts, hard landings and tosses, chase view only; values in `vehicle-feel.md` → Crash weight.
- **For planning:** run pacing (dead moments; measured, `production/backlog.md` → Run pacing) and crash weight
  (tactile feedback on impacts, flips and tosses; backlog → Crash weight). Both also go to PT-1 testers.

## Polish notes (Andy 2026-10-05 via stormchaser-38; low priority, behind S9-02a)
- **Sirens, one spatial source** (Codex, X9-03): instead of four poles (X8-01), one central siren (farmstead or town
  crossroads), fully 3D with a clear distance rolloff, so you hear where it is and that it's off in the distance. First
  check whether today's setup sounds "everywhere" (spatialBlend, overlapping poles).
- **Siren tail + radio return** (Codex siren release + Claude RadioLite duck release, X9-04): the siren tails off longer
  instead of stopping, and the radio comes back with a 1–2 s ease after the duck instead of snapping. The radio ramp
  starts as the siren tail fades, so they never overlap loudly.

## Carried over from Sprint 8 (closed 2026-10-05)
| ID | Owner | Task | Est. |
|----|-------|------|------|
| RG-008 | Andy | run-goals-v1 story 008: complete every career goal once in a real compact run, seeds in `production/qa/smoke-run-goals.md` (before goals go to outside players). Tip: seed 554 has an EF5 at ≈ 80 s (EF4-at-peak photo, a toss for BIG AIR / TOSS SURVIVOR); C on the title shows what's left | 30 min |
| RS-001 | Andy | run-screens story 001: on Windows, Quit to Desktop, Esc-Esc on the title, Alt-tab auto-pause | 5 min |
| CA-1 | Andy | Remaining Codex live checks: X7-02 vehicle VFX, X7-03 vehicle audio, X7-07 knock-loose props (`production/qa/g3-playtest-session.md` Part B) | 15 min |
| CA-2 | Andy | Accept the S7-06 physics caveat, or one `?physProbe=1` prop-cluster smash | 5 min |
| PT-1 | Andy | Outside playtest round 1 findings into `production/qa/playtest-round-1.md` (due 2026-10-11); unblocks S9-01 | — |

## Andy's queue (~1 h, one sitting)
- [ ] RG-008 career goals smoke run, ~30 min (`production/qa/smoke-run-goals.md` doesn't exist yet: I'll write the seed list before you start)
- [ ] RS-001 Windows quit / Alt-tab check, 5 min → then M1 "run screens" can be ticked
- [ ] CA-1 Codex live checks X7-02 / X7-03 / X7-07, 15 min
- [ ] CA-2 physics caveat accept or one smash, 5 min
- [ ] Chase PT-1 testers (findings due 2026-10-11)

After RG-008, M1 "run goals" can be ticked. Revisit M1's 11-16 date once PT-1 is in; it may close early.

## New requests
- [x] **Title attract mode** (Andy via stormchaser-38, 2026-10-05; done 0.8.2): slow drone orbit on the title, paused
  under Settings / Career; the Storm Director runs showcase seeds 6 / 29 / 37 (data, `TitleAttract._showcaseSeeds`),
  fast-forwarded so the anchor touches down ≈ 6 s in and framed in the right margin clear of the title column; truck
  frozen; effects muted (`GameAudio.AttractMute`), title music only; HUD crawl and radio duck ignore the demo. StartRun
  eases to the chase camera and `DisasterSpawner.Begin` replaces the demo with the real seed; Codex's sky/rain reset on
  RunStarted. Evidence: `attract-{14s,20s,30s,run-blend,run-chase}.png`, PlayMode `TitleAttractTests` (2), EditMode
  attract-mute test, WebGL 0 console errors.

## Gates (Andy)
| Gate | When | Pass = | Result |
|------|------|--------|--------|
| **G6 — Feel sign-off** | After S9-01/02 | Andy + 2 outside players: `vehicle-feel.md` feel criteria pass on the tuned build | |

## Tasks

### Must Have — Claude
| ID | Task | Type | Est. | Status |
|----|------|------|------|--------|
| S9-01 | Triage round 1 feedback into a tuning list: each complaint → the `vehicle-feel.md` knob(s) it maps to, with a proposed value. Andy picks | Analysis | 1.5 h | Blocked on PT-1 only: starts as soon as findings land in the repo (due 2026-10-11), not on 10-20 |
| S9-02a | **Feel pass from Andy's notes (start now):** drift / e-brake driver feel; chase-camera swing and lag through drifts and corners; jump a touch floaty; **bug:** jumps peak ≈ 1.55 m < `MinAirtimeHeight` 1.8 m, so the Airtime moment, its boost refill and `big_air` never fire from a jump (`rg-airtime-evidence.md`): fix the threshold or the jump so a good jump counts, `big_air` achievable but not trivial. Every changed value in `vehicle-feel.md` → Playtest Tuning Log; PlayMode vehicle tests green | Config/Data | 4 h | **First pass done 2026-10-05 (0.8.4 local build), awaiting Andy's feel check.** Values + probe numbers: `vehicle-feel.md` → Tuning Log → S9-02a. Evidence `s902a-jump-4.png` (AIR 0.6 s), `s902a-drift.png` |
| S9-02b | Second tuning pass from S9-01 (round 1 findings, `production/qa/playtest-round-1.md`: stormchaser-38 fills the tally and knob table) | Config/Data | 2 h | Blocked on S9-01 |
| S9-02b | ~~Camera default~~ **Pulled into Sprint 8 (S8-08, done 2026-10-04); the swing through drifts and corners stays here.** Camera default (Andy, 2026-10-04: "lower so it captures more of the sky, like Rocket League"): default pitch from 26.6° toward ≈ 10–12° so storms stay on screen, tuned with round 1 feedback; it becomes the SKY/CLASSIC preset split shipped with Settings (run-screens RS-2) | Config/Data | 1 h | With S9-02 |
| S9-03 | 0.8.x release with the tuned feel; tester brief v2 (same questions plus "compared to last time") | Release | 1.5 h | Blocked on S9-02a/b |

### Must Have — Andy
| ID | Task | Est. |
|----|------|------|
| PT-2 | Outside playtest round 2 on the tuned build, 2+ of the round 1 players | — |
| G6 | Feel sign-off | 1 h |

### Should Have
| ID | Owner | Task | Est. |
|----|-------|------|------|
| S9-04 | Claude | **Now.** WebGL frame-time pre-check for M1's budget criterion over a 3-min run with live storms, rain, storm sky, sirens and radio (title attract counts too): no frame > 50 ms, p95 ≤ 33.3 ms, physics ≤ 4 ms (`?physProbe=1`). Codex supplies the presentation share (X9-01) | 2 h |
| CU-01 | Cursor | WebGL frame-time capture tool in `tools/perf/` (spec: AGENTS.md → Requests, Claude → Cursor 2026-10-05); feeds S9-04 and later releases | 2 h |
| S9-05 | Claude | Round 1 non-driving findings (storms, damage, bugs) triaged into fix-now vs backlog | 1 h |

### Not this sprint
New mechanics or vehicles; S8-C2 (deferred past M1); anything in M1's out-of-scope list.

## Risks
| Risk | Mitigation |
|------|------------|
| Round 1 feedback arrives late or thin | Andy's own notes are enough to start S9-02; round 2 confirms |
| Feel pass breaks style refills, near-miss or landing behaviour | Existing PlayMode vehicle tests stay green; any threshold change is checked against `vehicle-feel.md` sanity numbers |

## Definition of Done
- [ ] Tuned values recorded in `vehicle-feel.md`, tests green
- [ ] Tuned build on itch, round 2 played
- [ ] G6 passed
