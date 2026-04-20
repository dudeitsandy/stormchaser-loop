# Sprint 2 — 2026-04-21 to 2026-05-04

## Sprint Goal
A truck drives on the verification scene, a tornado moves across the field,
a 90-second session timer runs, and the scoring formula exists in C#. The first
interactive loop is playable — no UI, no polish, just the core systems in code.

## Capacity
- Sprint length: 2 weeks
- Hours/week: 8–10
- Total hours: 16–20
- Buffer (20%): 3–4 hours reserved for C# / Unity API learning curve (R05)
- **Available: ~13–16 hours**

---

## Tasks

### Must Have (Critical Path)

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S2-01 | PlayerVehicle.cs — Rigidbody truck movement (WASD + left stick via StormChaserControls) | 2.5h | S1-09 | Truck moves forward/back, steers left/right; speed feels close to Phaser v0.3.0 feel; no clipping through plane |
| S2-02 | Follow camera — Cinemachine Virtual Camera tracking truck from behind | 1h | S2-01 | Camera follows truck at fixed offset; no manual camera code needed |
| S2-03 | TornadoController.cs — Spawn tornado at map edge, move across field, despawn at far edge | 2h | — | Tornado spawns, translates across scene at configurable speed; can be seen from truck |
| S2-04 | TornadoData ScriptableObject — EF0–EF5 stats (wind speed, cone scale, strength multiplier) | 1h | S2-03 | 6 TornadoData assets created (EF0–EF5); values data-driven, not hardcoded |
| S2-05 | SessionTimer.cs — 90-second countdown, fires OnSessionEnd event at zero | 1h | — | Timer counts down from 90s; logs "Session Over" to console on expiry |

**Must Have total: ~7.5 hours**

### Should Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S2-06 | ScoringSystem.cs — Port scoring formula: `(AimScore × 0.6 + DistanceScore × 0.4) × EFStrength × 100` | 1.5h | S2-04 | Unit test passes: EF3 tornado, perfect aim, close range → expected score output matches Phaser formula within 5% |
| S2-07 | VehicleData ScriptableObject — tuning knobs for speed, acceleration, turn radius (data-driven) | 0.5h | S2-01 | VehicleData asset exists; PlayerVehicle.cs reads from it; no magic numbers in code |

**Should Have total: ~2 hours**

### Nice to Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S2-08 | Tornado cone mesh — scale primitive cone to EF size from TornadoData | 0.5h | S2-04 | Tornado visible as correctly-scaled cone mesh in scene |
| S2-09 | GDD stub: Vehicle Physics — document tuning knobs and feel targets in `design/gdd/` | 0.5h | S2-07 | File exists with Overview, Formulas, Tuning Knobs sections filled |

**Nice to Have total: ~1 hour**

---

## Carryover from Previous Sprint
None — Sprint 1 completed 12/12 tasks.

---

## Risks This Sprint

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Rigidbody physics feel hard to tune (R05) | High | Medium | Port Phaser speed values first; use VehicleData ScriptableObject to iterate without recompiling |
| Cinemachine API unfamiliar | Low | Low | Use Cinemachine package docs; CinemachineCamera component is declarative — minimal code needed |
| Scoring formula unit test setup unfamiliar | Medium | Low | NUnit is built into Unity Test Framework; one test class, one test method |

---

## Dependencies on External Factors
- Cinemachine package (may need to install via Package Manager — check if included in URP template)

---

## Definition of Done for Sprint 2
- [ ] S2-01 through S2-05 all complete (Must Have)
- [ ] Truck controllable with PS5 controller or WASD
- [ ] Tornado visible and moving across scene
- [ ] 90-second timer confirmed in Play mode
- [ ] ScoringSystem unit test passing (if S2-06 complete)
- [ ] No hardcoded gameplay values — all in ScriptableObjects
- [ ] Sprint 3 scope drafted
