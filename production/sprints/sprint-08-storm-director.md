# Sprint 8 — Storm Director + Run Goals (2026-10-06 → 2026-10-19)

> **Status:** Storm Director half done ahead of schedule (2026-10-04): stories 001–009 and S8-C1 shipped in
> 0.7.4/0.7.5, G4 and G5 passed. **Re-scoped 2026-10-04 (C5 = yes):** the open two weeks take Sprint 9's M1
> work: run goals v1 (design, then build) and run screens. S8-C2 is **deferred past M1** (Andy, 2026-10-04:
> the interim 6 HP passed G5). The driving pass moves to Sprint 9 so outside-player feedback can feed it.
> **Milestone:** M1 — Vertical Slice 0.9 (`production/milestones/milestone-1-vertical-slice.md`)

## Sprint Goal
Replace random tornado spawning with the Storm Director in compact mode, so every run has a readable
weather plan, storms you can see coming, and EF3+ storms that are actually dangerous. Turn collision HP
back on with S8-C1 durability tuning. *(Done.)* **Added by the re-scope:** every run carries a short
Tony Hawk-style goal list that scores alongside photos, and the run screens (title, pause/quit, wrecked,
results) are specified and complete.

## Capacity
- 2 weeks, Andy 8–10 h/week; Claude + Codex implement.
- Claude: ~26.5 h of epic stories + 2 h tuning. Codex: X7-09 telegraph presentation on `StormCell*` events.
- Design freeze in effect (M1): no new GDDs. The re-scope uses the freeze's two M1 exceptions: the run-goals
  section of `event-system.md` and the run-screens UX spec.
- Re-scoped work: ~5 h of Andy design sessions (run goals, run screens) + ~21–23 h Claude build: RG-2a in-run
  goals ~10 h → Save & Profile **M1 stem** + WebGL sync check ~5–7 h → RG-2b persistence (record, livery, career
  page, title toggle) ~4 h, plus RS-2. Fits the free capacity left by the early Storm Director finish.

## Gates (Andy)
| Gate | When | Pass = | Result |
|------|------|--------|--------|
| **G4 — Storm tension playtest** | After story 006 + 008 | Andy plays 5 seeded runs: storms visible before they matter, at least one "oh no" escape, EF5 feels rare and imposing | **Passed** 2026-10-04 (0.7.5) |
| **G5 — Durability** | After S8-C1 | Light bumps cost nothing; a tossed truck or a barn hit at speed hurts; a full run is survivable with care | **Passed** 2026-10-04 (0.7.5) |

## Tasks

### Must Have — Claude (epic `storm-director-compact`)
| Story | Task | Type | Est. | Status |
|-------|------|------|------|--------|
| 003 | Storm scale table and lifecycle intensity (F3) | Logic | 2.5 h | **Done** (8aef7fc) |
| 004 | New scale in play: wind drag, lift and toss (fixes EF3 wind test) | Integration | 2.5 h | **Done** (8aef7fc) |
| 001 | Seeded plan RNG, regime draw and anchor EF (F1) | Logic | 3 h | **Done** |
| 002 | Compact cell schedule and cap resolution (F2) | Logic | 4 h | **Done** |
| 005 | Track motion, jogs and fixed substeps (F5) | Logic | 3 h | **Done** |
| 006 | Director runtime driver and `StormCell*` lifecycle events | Integration | 5 h | **Done** |
| 008 | Telegraph hooks: siren caption and environmental intensity | Integration | 2 h | **Done** (KTVR News crawl) |

### Should Have
| ID | Owner | Task | Est. |
|----|-------|------|------|
| 007 | Claude | Forecast model + HUD forecast panel (F4) | 3 h — **Done** |
| 009 | Claude | Results: seed, build version, regime, "The big one got away" | 1.5 h — **Done** |
| S8-C1 | Claude | Durability tuning within the current model: severity threshold so light bumps cost 0 HP, Pickup Max HP raised, F10 HP cost back on; values in `vehicle-damage.md` | 2 h — **Done** (Pickup 6 HP, Light 15.5 / Severe 23.25 m/s, HP on; G5 next) |
| X7-09 | Codex | Storm telegraph presentation on `StormCell*` events (world cues, storm audio, failed touchdown) | Codex lane |

### Re-scope (2026-10-04): pulled in from Sprint 9
| ID | Owner | Task | Type | Est. | Status |
|----|-------|------|------|------|--------|
| RG-1 | Andy + Claude | Run goals v1 design: new section in `event-system.md` (how many goals per run, seeded or not, goal types from style moves + storms, HUD and results display, scoring next to photos), then `/design-review` | Design | 3 h | **Done**: RG section in `event-system.md` (056170f), review NEEDS REVISION, fixed same day |
| RG-2 | Claude | Run goals v1 build: stories via `/create-stories` once RG-1 is approved (goal model, tracking from existing events, HUD list, results tally) | Logic + UI | ~10 h | **Ready**: epic `run-goals-v1`, 8 stories (22de7d8); story 005 = Save & Profile stem |
| RS-1 | Andy + Claude | Run screens UX spec (`/ux-design`): title, pause/quit, wrecked, results; documents what's built, fills gaps, places run goals on results | Design | 2 h | **Done**: `design/ux/run-screens.md` (59c9a76, review fixes 5bdce65) |
| RS-2 | Claude | Run screens build: gaps from RS-1 (pause/quit, wrecked state, results goal tally) | UI | ~6 h | **Ready**: epic `run-screens` (91568a0); stories not yet created |
| PT-1 | Andy | Outside playtest round 1 on 0.7.5 with the tester brief (Google Doc), 3+ players; findings logged **in the repo** (this file) | Playtest | — | Out with players; **due 2026-10-11** |

### Carried over from Sprint 7 (closed 2026-10-04)
| ID | Owner | Task | Est. | Status |
|----|-------|------|------|--------|
| CA-1 | Andy | Codex live acceptance pass, `production/qa/g3-playtest-session.md` Part B: X7-01/08, X7-02, X7-03, X7-04, X7-07, X7-09, X7-10, plus X8-01 sirens and X8-02 storm sky (add them to Part B). Fold into the next play session, ≈ 25 min | 25 min | **Partial** (0.7.6, 2026-10-04): X8-01 sirens, X7-10 tones (pre-polish OK), X7-04 funnel pass; sky, jog lean, X7-02/03/07 still to check |
| CA-2 | Andy | Checklist C6: accept the S7-06 physics PASS caveat, or one `?physProbe=1` prop-cluster smash | 5 min | Open |

### Not this sprint
S8-C2 per-truck HP redesign → **deferred past M1** (Andy, 2026-10-04). Driving tuning pass → Sprint 9.
Roads, scatter, Tier B fracture, Story/Career, new worlds → after M1.
Bystander "VIEWER VIDEO" cellphone clip on the main storm's touchdown (Andy, 2026-10-04) → after M1, with the TV News bounties.

## Order of Work
1. Finish 003 → 004 (closes the EF3 wind test, PlayMode suite green).
2. 001 → 002 → 005 (pure model, all EditMode).
3. 006 → 008 (driver + events; Codex X7-09 follows), then G4.
4. S8-C1 + G5 alongside; 007 and 009 if capacity holds.
5. *(Re-scope)* RG-1 → RG-2; RS-1 once RG-1 settles the results layout → RS-2. PT-1 runs in parallel.

## Risks
| Risk | Mitigation |
|------|------------|
| Tuning (regime weights, jog rate, forecast error) eats the sprint (R09) | Ship GDD placeholder values; tune only after G4 |
| Claude and Codex both touching tornado code (R11) | Director code is Claude's; presentation reads `StormCell*` events and the query surface only |
| Run-goals design grows into a progression system (re-scope) | Persistence is in M1 **only as the stem** (Andy, 2026-10-04): **M1 stem** (Andy, 2026-10-04): one profile file (accomplishments record, unlocks, `LastLoadout.livery`, best score, imported once from the PlayerPrefs `BestScoreStore`) and one device file (Settings), each written tmp + rename, `SchemaVersion` 0 (**disposable**: the full build may reset them; patch notes say so). Stable string IDs for goals and unlocks still apply. Gated by a ~1 h WebGL IndexedDB sync check; if it fails, M1 persistence is session-only. Nothing else from `save-profile.md` is in M1. No garage, Storm Dollars balance, slots or album. Anything bigger goes to `production/backlog.md` |

## Playtest findings (Andy, 2026-10-04, 0.7.5)
- G4 storm tension and G5 durability both pass. Next: hand 0.7.5 to outside players for fresh eyes.
- **Driving needs another tuning pass** ("a little more", not blocking). Earlier notes still open: jump a
  touch floaty, steering a little loose (G3). Candidate for Sprint 9: a focused `vehicle-feel.md` pass on
  grip/steer/air values, with outside-player feedback as input.

## Definition of Done
- [x] Stories 001–006 and 008 closed via `/story-done`; EditMode + PlayMode green, EF3 wind test included
      *(all 9 stories Done; 0.7.4: EditMode 288/288, PlayMode 47/47)*
- [x] Shipping build spawns storms from the Director, not `DisasterSpawner`'s timer *(0.7.4)*
- [x] G4 run and findings logged: **passed** (Andy, 2026-10-04, on 0.7.5); external players next
- [x] S8-C1 shipped, G5 passed (Andy, 2026-10-04, on 0.7.5)
- [ ] *(Re-scope)* Run goals v1 designed, reviewed and built; a run shows its goals and the results tally them
- [ ] *(Re-scope)* Run screens spec approved; pause/quit and wrecked state built; screenshot of each screen
- [ ] Release 0.8.0 to itch (html5 + windows) with run goals, WebGL 0 console errors
