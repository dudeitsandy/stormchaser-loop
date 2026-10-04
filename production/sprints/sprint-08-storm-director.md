# Sprint 8 — Storm Director (2026-10-06 → 2026-10-19)

> **Status:** Planned (draft 2026-10-04). Starts when Sprint 7 closes at G3.
> **Milestone:** M1 — Vertical Slice 0.9 (`production/milestones/milestone-1-vertical-slice.md`)

## Sprint Goal
Replace random tornado spawning with the Storm Director in compact mode, so every run has a readable
weather plan, storms you can see coming, and EF3+ storms that are actually dangerous. Turn collision HP
back on with S8-C1 durability tuning.

## Capacity
- 2 weeks, Andy 8–10 h/week; Claude + Codex implement.
- Claude: ~26.5 h of epic stories + 2 h tuning. Codex: X7-09 telegraph presentation on `StormCell*` events.
- Design freeze in effect (M1): no new GDDs. The only design work allowed this sprint is answering the
  epic's open tuning questions from playtest data.

## Gates (Andy)
| Gate | When | Pass = |
|------|------|--------|
| **G4 — Storm tension playtest** | After story 006 + 008 | Andy plays 5 seeded runs: storms visible before they matter, at least one "oh no" escape, EF5 feels rare and imposing |
| **G5 — Durability** | After S8-C1 | Light bumps cost nothing; a tossed truck or a barn hit at speed hurts; a full run is survivable with care |

## Tasks

### Must Have — Claude (epic `storm-director-compact`)
| Story | Task | Type | Est. | Status |
|-------|------|------|------|--------|
| 003 | Storm scale table and lifecycle intensity (F3) | Logic | 2.5 h | **In Progress** (started in Sprint 7) |
| 004 | New scale in play: wind drag, lift and toss (fixes EF3 wind test) | Integration | 2.5 h | Ready |
| 001 | Seeded plan RNG, regime draw and anchor EF (F1) | Logic | 3 h | Ready |
| 002 | Compact cell schedule and cap resolution (F2) | Logic | 4 h | Ready |
| 005 | Track motion, jogs and fixed substeps (F5) | Logic | 3 h | Ready |
| 006 | Director runtime driver and `StormCell*` lifecycle events | Integration | 5 h | Ready |
| 008 | Telegraph hooks: siren caption and environmental intensity | Integration | 2 h | Ready |

### Should Have
| ID | Owner | Task | Est. |
|----|-------|------|------|
| 007 | Claude | Forecast model + HUD forecast panel (F4) | 3 h |
| 009 | Claude | Results: seed, build version, regime, "The big one got away" | 1.5 h |
| S8-C1 | Claude | Durability tuning within the current model: severity threshold so light bumps cost 0 HP, Pickup Max HP raised, F10 HP cost back on; values in `vehicle-damage.md` | 2 h |
| X7-09 | Codex | Storm telegraph presentation on `StormCell*` events (world cues, storm audio, failed touchdown) | Codex lane |

### Not this sprint
S8-C2 per-truck HP redesign → Sprint 9 (design task). Run goals and run screens → Sprint 9.
Roads, scatter, Tier B fracture, Story/Career, new worlds → after M1.

## Order of Work
1. Finish 003 → 004 (closes the EF3 wind test, PlayMode suite green).
2. 001 → 002 → 005 (pure model, all EditMode).
3. 006 → 008 (driver + events; Codex X7-09 follows), then G4.
4. S8-C1 + G5 alongside; 007 and 009 if capacity holds.

## Risks
| Risk | Mitigation |
|------|------------|
| Tuning (regime weights, jog rate, forecast error) eats the sprint (R09) | Ship GDD placeholder values; tune only after G4 |
| Claude and Codex both touching tornado code (R11) | Director code is Claude's; presentation reads `StormCell*` events and the query surface only |

## Definition of Done
- [ ] Stories 001–006 and 008 closed via `/story-done`; EditMode + PlayMode green, EF3 wind test included
- [ ] Shipping build spawns storms from the Director, not `DisasterSpawner`'s timer
- [ ] G4 run and findings logged
- [ ] S8-C1 shipped, G5 passed
- [ ] Release 0.8.0 to itch (html5 + windows), WebGL 0 console errors
