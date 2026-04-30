# Sprint 3 — 2026-05-05 to 2026-05-18

## Sprint Goal
The first complete game loop: a tornado auto-spawns, the player drives toward it,
presses a button to photograph it (aim + distance scored), accumulates score across
multiple photos, and the session ends at 90 seconds with a total. No PiP camera yet —
aim is calculated from truck facing and distance.

## Capacity
- Sprint length: 2 weeks
- Hours/week: 8–10
- Total hours: 16–20
- Buffer (20%): 3–4 hours
- **Available: ~13–16 hours**

---

## Tasks

### Architecture (Doomsday Foundation)

Low-effort decisions made now that prevent a rewrite in Season 2. See `design/vision/vision-1.0.md`.

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S3-A1 | DisasterEntity base class — abstract MonoBehaviour with ThreatClass (I–V enum), BehaviorPattern enum, and MoveSpeed; refactor TornadoController to inherit it; add ThreatClass + BehaviorPattern fields to TornadoData SO | 1.5h | S2-03, S2-04 | TornadoController compiles as DisasterEntity subclass; TornadoData has ThreatClass + BehaviorPattern fields; no gameplay change |

**Architecture total: ~1.5 hours**

### Must Have (Critical Path)

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S3-01 | DisasterSpawner.cs — spawns disaster entities (typed by DisasterData SO list) at random map edge every N seconds; stops on session end | 2h | S3-A1, S2-05 | Tornado spawns without manual placement; spawn interval + disaster roster configurable via ScriptableObject; stops spawning when session ends |
| S3-02 | PhotoTrigger.cs — press L2/Space to photograph; calculates AimScore (dot product) + DistanceScore (bell curve, optimal 20 units) | 2h | S2-01, S3-A1, S2-06 | Button press calls ScoringSystem.CalculatePhotoScore; score logs to Console; only scores nearest active disaster entity |
| S3-03 | ScoreAccumulator.cs — tracks running total across all photos; logs final score when OnSessionEnd fires | 1h | S2-05, S3-02 | Final score printed to Console at session end; individual photo scores additive |
| S3-04 | Basic terrain — replace flat Plane with grass-colored material + road stripe running through the field | 1.5h | — | Scene has visible grass area and road; truck drives on it; no procedural generation yet |

**Must Have total: ~6.5 hours**

### Should Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S3-05 | HUD stub — UI Toolkit canvas: time remaining (countdown) + current score (running total) | 2h | S3-03, S2-05 | Two text elements visible in Play mode; update in real time; no styling required |
| S3-06 | Tornado prefab — cone mesh (not cylinder) scaled by TornadoData.ConeScale at runtime | 1h | S2-04 | Tornado renders as cone; EF5 visibly larger than EF0 |

**Should Have total: ~3 hours**

### Nice to Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S3-07 | AimIndicator — simple screen-space arrow or dot pointing toward nearest tornado | 1h | S3-02 | Player can locate tornado without hunting; visible in HUD |
| S3-08 | Session result — freeze time + display final score overlay when session ends | 1h | S3-03, S3-05 | "SESSION OVER — Score: XXXX" visible at end; dismiss with any key |

**Nice to Have total: ~2 hours**

---

## Carryover from Previous Sprint
None — Sprint 2 completed 7/7 tasks.

---

## Risks This Sprint

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| UI Toolkit learning curve for HUD (S3-05) | Medium | Low | Use UXML + USS for layout; start with Label elements only — no complex bindings yet |
| Aim score feels wrong without PiP (R-photo) | Medium | Medium | Acceptable for Sprint 3 — dot product gives reasonable feel; PiP replaces this in Sprint 4 |
| Tornado spawning off-screen / hard to find (R-spawn) | Medium | Low | Spawn within visible range (±30 units on Z axis); add S3-07 aim indicator if needed |

---

## Dependencies on External Factors
- None — all systems exist from Sprint 1–2

---

## Deferred Architecture Tasks

These are not Sprint 3 scope but must land before the systems they extend get expanded.
Tracked here so they don't get lost. See `design/vision/vision-1.0.md` for full context.

| Target Sprint | Task | Trigger |
|--------------|------|---------|
| Sprint 4–5 | ScoringSystem → 4-axis (Documentation, Intervention, Style, Objectives) + style multiplier | When intervention scoring or stunt detection is added |
| Sprint 5–6 | VehicleData → VehicleLoadout with module slots (Engine, Armor, Payload, Wheels, Electronics) | Before first vehicle customization feature |
| Sprint 5–6 | SessionTimer → RunManager with objective tracking + per-run modifier state | Before dynamic objectives or modifiers are implemented |
| Sprint 6–7 | Vehicle Feel pass — momentum physics, drift, boost, stunt detection | Ships with Style scoring axis; Rocket League energy goal |

---

## Definition of Done for Sprint 3
- [x] S3-A1 complete — DisasterEntity base class; TornadoController inherits it
- [ ] S3-01 through S3-04 complete (Must Have)
- [ ] Press Space/L2 near a tornado → score appears in Console
- [ ] 90-second session ends with total score logged
- [ ] Scene has road + grass terrain (no procedural generation)
- [ ] No hardcoded values — spawn interval in ScriptableObject
- [ ] Sprint 4 scope drafted
