# Sprint 4 — 2026-08-28 to 2026-09-10

## Sprint Goal
Photography becomes a real mechanic: the player sees what they're framing through a PiP
viewfinder, gets immediate feedback on every shot, and the camera creates tension as they chase.

## Resumption Note
Originally drafted 2026-06-17 targeting a 2026-06-17 → 2026-06-30 window. Work started that
same day but was never committed, and the project went untouched from 2026-07-03 (an editor
crash left autosave dumps in `Assets/_Recovery/`) until resuming 2026-08-28 — roughly an 8-week
gap with no other changes to the plan. Dates below are reset to the resumption date; scope is
unchanged except for progress already banked (see Carryover).

## Capacity
- Sprint length: 2 weeks
- Hours/week: 8–10
- Total hours: 16–20
- Buffer (20%): 3–4 hours
- **Available: ~13–16 hours** (S4-03 groundwork already banked — see Carryover; realistic remaining load is closer to ~7–8 hours)

---

## Tasks

### Architecture (Decision Only — No Code)

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S4-A1 | ADR: Merged Disaster Entity — decide new subclass vs. runtime mutation; write to `docs/architecture/` | 0.5h | vision-1.0.md | ADR exists with decision, rationale, and consequences; clears path for Season 1 disaster interactions in Sprint 5+ |

**Architecture total: ~0.5 hours**

### Must Have (Critical Path)

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S4-01 | PiP camera — RenderTexture camera at truck position facing forward; always-on HUD overlay via RawImage; render at half resolution per R02 | 3h | S3-05, S3-02 | PiP visible in Play mode; tornado enters frame as player aims; FPS stays ≥55 |
| S4-02 | Photo feedback — white screen flash (0.3s fade) + floating "+XXX" score popup on every shot | 2h | S3-02, S3-05 | Every Space/L2 press near a tornado produces flash + number; silent when no tornado present |
| S4-03 | Camera system — lower pitch angle for horizon visibility; smooth follow damping; mild FOV pulse (+5°) when tornado within 40 units | 2h | S2-01 | Horizon visible at game start; follow has perceptible smoothing; FOV subtly widens on proximity |

**Must Have total: ~7 hours**

### Should Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S4-04 | Session end screen — freeze time + "SESSION OVER — Score: XXXX" overlay on session end; dismiss with any key | 1h | S3-03, S3-05 | Overlay appears at 90s; score displayed; keypress clears it |
| S4-05 | Aim indicator — screen-space UI arrow pointing at nearest active tornado when off-screen | 1h | S3-02 | Arrow visible when tornado is off-screen; disappears when tornado is dead ahead; correct direction |

**Should Have total: ~2 hours**

### Nice to Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S4-06 | EF prefab set — create EF0, EF1, EF2, EF4, EF5 prefab variants using TornadoVisual; wire all into DisasterSpawner roster | 1h | S3-06 | All six EF variants spawn correctly; EF5 visibly larger than EF0 in Play mode |
| S4-07 | Shot quality label — "PERFECT" / "GOOD SHOT" / "GLANCING" text above score popup based on tier (>300 / 150–299 / <150) | 1h | S4-02 | Label appears with every shot; text matches score tier; readable at 1080p |

**Nice to Have total: ~2 hours**

---

## Carryover from Sprint 3
None — S3-04 and S3-06 completed 2026-06-17 (committed 2026-08-28 alongside this redate).

## Carryover from the Pre-Gap Session (2026-06-17)
- **S4-03 (Camera system) is implemented**, not just estimated: `CameraController.cs` has
  SmoothDamp follow, look-ahead, and an FOV pulse at 40 units — matches the acceptance criteria.
  It has **not been Play-mode verified since being written** (8-week gap, one crash in between),
  so treat it as "needs verification," not "done." First task this sprint: confirm it still
  behaves correctly, then check off S4-03 in the Definition of Done.
- Nothing else from Sprint 4's original scope was started — S4-A1, S4-01, S4-02, S4-04, S4-05
  are all still fully open.

---

## Risks This Sprint

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| PiP RenderTexture GPU cost drops below 60 FPS (R02) | Medium | High | Render at 320×213; profile in first session; fall back to "hold to aim" activation model if FPS target missed |
| Camera smooth follow conflicts with existing camera setup | Medium | Low | Implement as standalone `CameraController.cs` rather than parenting; gives full positional control |
| S4-01 runs long, consuming Must Have budget (R03) | Medium | Medium | S4-06 and S4-07 are explicit cut candidates; drop both before cutting S4-04 or S4-05 |

---

## Dependencies on External Factors
- None — all required systems exist from Sprint 1–3

---

## Implementation Notes
- PiP camera: second Camera component with Depth set lower than main camera; culling mask
  can match main camera for now; RenderTexture assigned to RawImage in HUD UIDocument
- Camera follow: if current camera is parented to truck, extract it and write CameraController.cs
  with SmoothDamp on position; keep look-at target as truck transform
- Photo feedback flash: full-screen Image (Canvas, Screen Space Overlay) with CanvasGroup alpha
  driven by coroutine; reuse for session end overlay
- Score popup: instantiate a prefab Label at HUD anchor position; animate Y offset + alpha via
  coroutine; pool if performance warrants it

---

## Definition of Done for Sprint 4
- [ ] S4-A1 — ADR written for merged disaster entity architecture
- [ ] S4-01 — PiP visible in Play mode; no FPS regression below 55
- [ ] S4-02 — Flash + score popup appears on every photo; silent with no tornado
- [ ] S4-03 — Horizon visible at game start; smooth follow; FOV pulse on proximity (code written 2026-06-17, needs a fresh Play-mode verification pass before checking off)
- [ ] S4-04 — Session end overlay with final score; dismissable
- [ ] S4-05 — Aim indicator points to tornado when off-screen
- [ ] Sprint 5 scope drafted
