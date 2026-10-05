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
  **Reading PT-1 by build** (testers start on 0.8.5; `playtest-round-1.md` records each tester's build): Q5 "boring
  stretch" answers from ≤ 0.8.3 are the *before* baseline for the pacing fill; 0.8.4+ answers are already the *after*
  side. Drift and feathering answers from ≤ 0.8.4 describe the old drift (pass 1 or none); only 0.8.5+ reflects pass 2.
  Most testers will be 0.8.5, so the pacing before/after comes mainly from round 2 vs Andy's own 0.8.3 runs.
- **Crash weight, low-end camera shake in** (Andy: "noticeable but not disruptive"; no tester has started):
  jolts on HP-costing impacts, hard landings and tosses, chase view only; values in `vehicle-feel.md` → Crash weight.
- **For planning:** run pacing (dead moments; measured, `production/backlog.md` → Run pacing) and crash weight
  (tactile feedback on impacts, flips and tosses; backlog → Crash weight). Both also go to PT-1 testers.

## Sprint 10 carry-ins (decided 2026-10-05)
- **Smooth the title → run start** (111 ms frame in S9-04: demo storm cleared, run plan built, screens swapped in one
  frame): pre-build the run's storm plan / first spawns and UI during the title, spread over frames. Andy: transitions
  sit outside M1's "3-min run", so this is polish, not a gate blocker.

## Polish notes (Andy 2026-10-05 via stormchaser-38; low priority, behind S9-02a)
- **Sirens, one spatial source** (Codex, X9-03): instead of four poles (X8-01), one central siren (farmstead or town
  crossroads), fully 3D with a clear distance rolloff, so you hear where it is and that it's off in the distance. First
  check whether today's setup sounds "everywhere" (spatialBlend, overlapping poles).
- **Siren tail + radio return** (Codex siren release + Claude RadioLite duck release, X9-04): the siren tails off longer
  instead of stopping, and the radio comes back with a 1–2 s ease after the duck instead of snapping. The radio ramp
  starts as the siren tail fades, so they never overlap loudly.

## Boot card (Andy 2026-10-05 via stormchaser-96; ~1 h, passes the freeze rule)
- [x] Ghostweave Labs badge between Unity's splash and the title: fade in 0.4 s with a VHS-tracking glitch (2 shallow
  dips, ≤ 3/s) and a quiet synthesized tape-static sting, hold 1.8 s, fade 0.4 s into the title + attract mode. Any
  press skips (armed once visible, so a click to focus the browser canvas during Unity's splash doesn't skip it unseen;
  the skip press never starts a run). Once per app session. Holds until `SplashScreen.isFinished` (otherwise it played
  out hidden behind Unity's splash). Logo `Resources/UI/GhostweaveLogo.png` (full-res master, sprite import capped at
  1024, no mipmaps; 919×1024 isn't a multiple of 4 so it stays uncompressed, unloaded after the card): WebGL download
  +283 KB. Tests: `BootCardTests` (2), `BootCardGlitchTests`. Evidence `bootcard-{960x600,1920x1080,1080-into-title}.png`,
  WebGL 0 console errors.

## Carried over from Sprint 8 (closed 2026-10-05)
| ID | Owner | Task | Est. |
|----|-------|------|------|
| RG-008 | Andy | run-goals-v1 story 008: complete every career goal once in a real compact run, seeds in `production/qa/smoke-run-goals.md` (before goals go to outside players). Tip: seed 554 has an EF5 at ≈ 80 s (EF4-at-peak photo, a toss for BIG AIR / TOSS SURVIVOR); C on the title shows what's left | 30 min |
| RS-001 | Andy | run-screens story 001: on Windows, Quit to Desktop, Esc-Esc on the title, Alt-tab auto-pause | 5 min |
| CA-1 | Andy | Remaining Codex live checks: X7-02 vehicle VFX, X7-03 vehicle audio, X7-07 knock-loose props (`production/qa/g3-playtest-session.md` Part B) | 15 min |
| CA-2 | Andy | Accept the S7-06 physics caveat, or one `?physProbe=1` prop-cluster smash | 5 min |
| PT-1 | Andy | Outside playtest round 1 findings into `production/qa/playtest-round-1.md` (due 2026-10-11); unblocks S9-01 | — |

## Andy's queue (~1 h, one sitting)
- [ ] RG-008 career goals smoke run → **6 / 10 done (2026-10-05, 0.8.4)**; during the goal pass on 0.8.6 Andy got stuck
  upside down after a storm → fixed (88bbf0c: no phantom wheels when flipped, 3 s failsafe, jump rights the truck);
  Andy: "back-to-back near misses seems complicated" → see the bounty note in the backlog; left: SICK SCORE, TOSS SURVIVOR, BIG AIR, FRONT PAGE (seed 554) (`production/qa/smoke-run-goals.md` doesn't exist yet: I'll write the seed list before you start)
- [x] RS-001 Windows quit / Alt-tab check: Esc-Esc, pause → Quit to Desktop and Alt-tab auto-pause all ✅ (2026-10-05) → **M1 "run screens" ticked**
- [ ] CA-1 Codex live checks X7-02 / X7-03 / X7-07, 15 min (Andy asking Codex to do them, 2026-10-05)
- [x] CA-2 physics smash: **Andy's Windows smash (0.8.6, `-physProbe=1`, 2026-10-05): max step 3.46 ms at 68 s, 0 over the
  4 ms budget, avg 0.29 ms, 12 impacts, peak 6 awake bodies** (heaviest load measured; scripted runs peak 3–4). Ended at
  ≈ 105 s (partial). Caveats: native Windows physics is faster than WebGL, and 6 bodies is a small pile-up (up to ≈ 40
  knock-loose props possible). To close fully: one browser smash with `?physProbe=1`, or accept with this as evidence.
  **Closed 2026-10-05: Andy accepts the caveat on the Windows result** (his browser run's console wasn't kept)
  Note: Andy's browser smash "looked rough"; that was the probe itself (`?physProbe=1` steps physics manually, which
  turns off rigidbody interpolation). The same 0.8.7 build without the flag looked fine. Never judge feel in a probe run
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
| | | **S9-04 result (2026-10-05, 0.8.4, seed 554, 20 s title + 190 s run, Chrome headless on the Radeon 890M iGPU, `production/qa/perf/frametime-0.8.4-seed554-2026-10-05.md`):** p95 **17.1 ms** (budget 33.3: pass, big margin). Worst frame **50.1 ms** (budget 50: one frame over by 0.1 ms; 0.8.3 had 2 over, worst 66.6). Physics max **4.50 ms**, 1 of 9001 steps (budget 4: one spike; 0.8.3 3.10). Frames > 33.3 ms: 128 (1.1 %, vs 66 on 0.8.3; seed 554 now also has the two pacing storms, and driving differs run to run). 0 console errors. Title/attract p95 17.1, max 33.9. Presentation share small (Codex X9-01: viewfinder 0.22 ms mean GPU, everything else < 0.05 ms). Spikes vs the seed's plan: physics spike at 74 s ≈ EF5 + satellite touchdown, 50.1 ms at 122.3 s ≈ EF5 rope-out, 49.9 ms at 93.8 s matches no storm event. **Read: no surprises for the M1 gate, two single-frame outliers to chase.** Next (Sprint 10 gate work): 3 repeat captures to see if the outliers repeat, then a Unity profiler pass on a development build at EF5 touchdown / rope-out | |
| | | **S9-04 repeats (2026-10-05, 0.8.5, seed 554 × 3, `production/qa/perf/0.8.5-r{1,2,3}/`):** p95 17.1 ms in all three (pass). Physics max 1.6 / 1.2 / 1.8 ms (pass; 0.8.4's 4.5 ms spike did not repeat). Worst frames: r1 one 333.8 ms hitch at 176 s = the scripted drive got wrecked and the scene reloaded (a load, not gameplay; its physics probe ended early); r2 four frames at 49.8–50.0 ms; r3 five at 49.7–50.3 ms (3 over 50 by ≤ 0.3 ms). Those ≈ 50 ms frames are exactly 3 vsync intervals (3 × 16.7), so frame time is quantized: some frames take > 33 ms of work and land on the 50 ms line, and whether they read 49.9 or 50.3 is jitter. Frames > 33.3 ms rose run to run across versions: 66 (0.8.3) → 128 (0.8.4) → 177 / 214 / 144 (0.8.5), ≈ 1.3–2 % of frames; the pacing fill keeps more storms alive. 0 console errors throughout. **Read: average and physics budgets hold comfortably; the "no frame > 50 ms" line is marginal: 3–5 heavy frames per run sit right on it.** Next: find what the heavy frames are (GC, spawns, audio start, storm transitions) before tuning anything | |
| | | **S9-04 slow-frame log (2026-10-05, 0.8.5 + `FrameSpikeLog`, `?seed=554&physProbe=1&spikeLog=1`, 25 s title + 190 s drive):** 152 frames over 30 ms out of 12,289 (1.2 %). **Not GC** (gc ran in 8 frames all run, none of them a slow one; heap steady at 3 MB). **Not storm events, impacts or audio** (149 of 152 slow frames had no game event in that or the previous frame; funnel count 0–3 and 4–7 playing sources made no difference: slow frames came just as often with 0 funnels after 195 s and on the title). **Not physics** (max step 1.6 ms). Almost all are single 33–34 ms frames: one missed vsync, spread evenly at ≈ 0.7 per second, which reads as the baseline frame cost sitting close to 16.7 ms on the 890M iGPU at 1280×800, not hitches. **Real outliers: 4.** Load (2.3 s, before the title). **Title → run transition 111 ms** (the frame with `ended #0 EF4, runstarted`: demo storm cleared, run started). 50 ms at 58.8 s (no event, 2 funnels). Two 50–51 ms frames at ≈ 212 s, just after the physics probe released the timer and the run ended (results screen). **Read:** the "no frame > 50 ms" line is crossed at the **two screen transitions**, not in play. Options: (a) count transitions as outside the 3-min run (the M1 wording is "over a 3-min run"), (b) spread the run-start work (pre-warm the run's first storm and UI during the title), (c) more margin overall by lowering WebGL render cost (resolution scale, shadows) if Sprint 10 wants it. Decide with Andy | |
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
