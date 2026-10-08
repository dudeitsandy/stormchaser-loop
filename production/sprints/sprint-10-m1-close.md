# Sprint 10 — M1 Close: 0.9 + Outside Playtest (2026-10-20 → 2026-11-02)

> **Status:** **APPROVED** (Andy, 2026-10-08; drafted by Claude 2026-10-05). Follows Sprint 9 directly (re-dated 10-06 → 10-19), so
> M1 can close ≈ 2 weeks ahead of its 2026-11-16 target. Scope is M1's remaining criteria only; design freeze applies.
> **Milestone:** M1 — Vertical Slice 0.9 (`production/milestones/milestone-1-vertical-slice.md`)

## Sprint Goal
Ship **0.9** on itch with the round 1 and 2 tuning in, signed off by outside players, and close M1.

## Where M1 stands going in (2026-10-05)
| Criterion | Status | Closes with |
|---|---|---|
| Storm Director compact mode | ✅ | — |
| Durability decided | ✅ | — |
| Run screens | ✅ (2026-10-05) | — |
| Run goals v1 | built; RG-008 6/10 | Andy: TOSS SURVIVOR, BIG AIR, FRONT PAGE, SICK SCORE (seed 554), or retune one with Andy |
| WebGL budget holds | passes in play (S9-04; transitions outside the run, Andy) | S10-03: 3 captures on the 0.9 candidate |
| Feel tuned from G3 | 2 driving passes in (S9-02a); waits on PT-1 → S9-02b → PT-2 → G6 | G6 sign-off (Sprint 9 if PT-2 lands in time, else S10-01) |
| Outside playtest | PT-1 running on 0.8.5+ | 0.9 on itch with ≥ 3 outside players, findings logged (S10-05) |

## Tasks

### Must Have — Claude
| ID | Task | Est. |
|----|------|------|
| S10-01 | Finish what Sprint 9 leaves: S9-02b tuning from PT-1 / PT-2 if not done; record every value in `vehicle-feel.md` | 0–3 h |
| S10-02 | PT-1 non-driving findings (storms, pacing, damage, goals) as tuning only: pacing fill knobs (`CompactSettings`), the near-miss bounty difficulty (backlog), any goal that proved impossible | 2 h |
| S10-03 | M1 performance gate on the 0.9 candidate: 3 × `tools/perf/webgl-frametime.mjs` (seed 554, `--flag spikeLog=1` once CU-02 lands); budget per the M1 wording (transitions outside the run). Never judge feel from a `physProbe` run | 1 h |
| S10-04 | 0.9 release: version, release log, itch html5 + windows, tester brief v3 ("0.9 or later") | 1 h |
| S10-06 | M1 gate check (`/gate-check`), milestone retro, Sprint 10 close | 1 h |

### Must Have — Andy
| ID | Task |
|----|------|
| RG-008 | The last 4 career goals (seed 554), or a retune decision for one that won't fall |
| G6 | Feel sign-off with 2 outside players (if not closed in Sprint 9) |
| S10-05 | 0.9 outside playtest: ≥ 3 outside players, findings in `production/qa/playtest-round-3.md` (or round 2 if it slides) |
| CA-1 | Codex live checks X7-02 / X7-03 / X7-07 (Andy asked Codex, 2026-10-05) |

### Should Have
| ID | Owner | Task |
|----|-------|------|
| S10-07 | Claude | Near-miss bounty "1 / 2" counter while the window is open (if PT-1 says it's unclear) |
| S10-08 | Codex | X9-02 / X9-03 / X9-04 live mix checks on the 0.9 candidate |
| S10-09 | Cursor | CU-02 perf tool flags + slow-frame capture (if not done in Sprint 9) |

### Not this sprint
Anything in M1's out-of-scope list; non-storm run content (jumps/ramps, rescues, tasks: backlog "Run pacing", post-M1);
controller rumble / hit-stop (backlog "Crash weight"); the self-driving attract mode and run replays (backlog).

## Risks
| Risk | Mitigation |
|------|------------|
| Fewer than 3 outside players by 11-02 | Andy recruits early; 0.9 can ship and the playtest criterion closes when the third report lands |
| PT feedback asks for a new system | Park it in `production/backlog.md` (freeze rule); M1 closes on tuning |
| SICK SCORE proves out of reach | Retune its threshold with Andy (RG-008 AC 2), re-verify |
