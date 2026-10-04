# Sprint 9 — Driving Feel + Outside Playtest (2026-10-20 → 2026-11-02)

> **Status:** Planned (2026-10-04). Sprint 9's original M1 work (run goals v1, run screens) moved into the
> re-scoped Sprint 8, so this sprint takes the driving pass that needs outside-player feedback first.
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

## Gates (Andy)
| Gate | When | Pass = | Result |
|------|------|--------|--------|
| **G6 — Feel sign-off** | After S9-01/02 | Andy + 2 outside players: `vehicle-feel.md` feel criteria pass on the tuned build | |

## Tasks

### Must Have — Claude
| ID | Task | Type | Est. | Status |
|----|------|------|------|--------|
| S9-01 | Triage round 1 feedback into a tuning list: each complaint → the `vehicle-feel.md` knob(s) it maps to, with a proposed value. Andy picks | Analysis | 1.5 h | Blocked on PT-1 only: starts as soon as findings land in the repo (due 2026-10-11), not on 10-20 |
| S9-02 | Feel pass: apply the chosen values (steer, grip, air/jump, landing), record them in `vehicle-feel.md` with the playtest that motivated each; PlayMode feel tests updated | Config/Data | 4 h | Blocked on S9-01 |
| S9-02b | Camera default (Andy, 2026-10-04: "lower so it captures more of the sky, like Rocket League"): default pitch from 26.6° toward ≈ 10–12° so storms stay on screen, tuned with round 1 feedback; it becomes the SKY/CLASSIC preset split shipped with Settings (run-screens RS-2) | Config/Data | 1 h | With S9-02 |
| S9-03 | 0.8.x release with the tuned feel; tester brief v2 (same questions plus "compared to last time") | Release | 1.5 h | Blocked on S9-02 |

### Must Have — Andy
| ID | Task | Est. |
|----|------|------|
| PT-2 | Outside playtest round 2 on the tuned build, 2+ of the round 1 players | — |
| G6 | Feel sign-off | 1 h |

### Should Have
| ID | Owner | Task | Est. |
|----|-------|------|------|
| S9-04 | Claude | WebGL frame-time pre-check for M1's budget criterion (no frame > 50 ms over a 3-minute run with live storms, debris and presentation), so Sprint 10 has no surprises | 2 h |
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
