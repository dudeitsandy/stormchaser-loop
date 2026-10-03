# Sprint 7 — Build the Feel (2026-10-02 → 2026-10-15)

## Sprint Goal
Turn Sprint 6's approved designs into a playable vertical slice: the **Rocket League-target truck**
(`design/gdd/vehicle-feel.md`) driving on the **first streamed terrain** (ADR-0004), in the **approved
art direction** (ADR-0003), with the first real modeled assets. Ship an art-preview build to itch at
the start so players see the new look while the feel work happens.

## Capacity
- 2 weeks, Andy 8–10 h/week for review/playtest/decisions; Claude + Codex implement.
- Andy-time is the constraint, not code: every gate below needs Andy for ≤ 30 min.

## Gates (Andy)
| Gate | When | Pass = |
|------|------|--------|
| **G1 — WebGL streaming spike** ✅ PASS w/ condition (2026-10-01; 2 km kept) | After S7-01 | ADR-0004 Validation #1: no frame > 50 ms, no GC spike > 5 ms over a 3-min boosted drive across tiles, physics step ≤ 4 ms. **Fail → 1 km fallback** (same tile code) |
| **G2 — Truck asset** | After S7-07 | Blender-MCP truck reads as a storm-chaser truck in ArtTest at gameplay distance; otherwise commission it |
| **G3 — Feel playtest** | End of sprint | `vehicle-feel.md` Feel acceptance criteria (Andy + 2 outside players, 5 runs each) |

## Tasks

### Must Have — Claude
| ID | Task | Est. | Depends | Acceptance |
|----|------|------|---------|------------|
| S7-00 | **0.5 Art Preview release**: promote the ADR-0003 look to the shipping scene (ArtTest becomes the build scene), title → PROTOTYPE 0.5, verify WebGL in headless Chromium (0 GL errors, outlines + toon render), push html5 + windows | 1 h | — | Live on itch as 0.5; WebGL renders correctly |
| S7-01 | **WebGL streaming spike**: `WorldPlan` (seed, heightfield noise) + `TileStreamer` (5×5 ring, time-sliced pooled builds) + distant silhouette on a 2 km map; placeholder props; perf capture in WebGL | 4 h | — | G1 measured and reported |
| S7-02 | **ADR-0005 Raycast vehicle architecture** (required by coding standards before S7-03) | 0.5 h | — | ADR accepted |
| S7-03 | **Vehicle core**: rigidbody + 4-wheel raycast suspension (F1), combined friction-circle grip (F3), drive/brake/coast/reverse (F2, F2b), steering (F4), downforce/assists (F6), states (Grounded/Sliding/Airborne/Upended + hysteresis), archetype star mapping (F13). Unit tests F1/F10/F12/F13; PlayMode accel/slide/ledge tests | 5 h | S7-02 | GDD unit + PlayMode criteria for these rules pass |
| S7-04 | **Verbs + input**: handbrake, jump (F8), boost + refill + exploit gates (F9, E1–E3), air control (F7), auto-right (E5); input remap (shutter → RB / left mouse) + title controls copy | 3 h | S7-03 | GDD verb criteria pass; title shows new bindings |
| S7-05 | **Camera & aim**: orbit + recenter, Storm Cam with offset framing (F14), camera-forward aim, AimScore framing curve (`photo-scoring.md`); update scoring tests | 2.5 h | S7-03 | Storm Cam + camera-aim PlayMode criteria pass |
| S7-06 | **Forces & impacts** (+ **G1 condition**: physics step ≤ 4 ms under real debris + vehicle in WebGL; warm-up/cap if not): wind as force (F11), lift + toss impulse (F12/F12b), impact severity + `ImpactKind` (F10, E12, E13), damage-stage hooks; new GameEvents (`StyleEvent`, `Landed`, `Impact`, `Tossed`); update `RunLoopSmokeTests` | 2.5 h | S7-03 | Wind/toss/impact PlayMode criteria pass; smoke test green |
| S7-07 | **Blender MCP + hero truck**: install add-on + MCP server, scripted low-poly truck matching F13 Pickup proportions (wheel anchors as named empties), FBX/glTF export → prefab with toon materials | 3 h | — | G2 |

### Must Have — Codex
| ID | Task | Est. | Depends | Acceptance |
|----|------|------|---------|------------|
| X7-01 | **PiP follows the gameplay camera** (not the truck), keeping CamcorderLens | 1 h | S7-05 | Viewfinder shows exactly what will be scored |
| X7-02 | **Vehicle VFX** from new events/state flags: tire smoke cards (Sliding), landing dust (Landed), sparks (Impact), boost flame/streaks, style pops ("DRIFT 2.4s", "AIR 1.8s", "NEAR MISS") | 3 h | S7-06 | Each fires on its trigger in ArtTest |
| X7-03 | **Vehicle audio** (procedural): engine pitch by speed/load, skid by slip, suspension thump/crunch, boost roar, metal crunch by severity | 2 h | S7-06 | Listening check by Andy |
| X7-04 | **Tornado bands → single funnel mass** (Sprint 6 gate note) | 1 h | — | Funnel reads as one mass in captures |
| X7-06 | **Presentation perf budget (WebGL)**: ms/frame for PiP, CamcorderLens, card VFX, audio; cuts if > 3 ms total — feeds G1 | 1 h | — | Numbers posted in AGENTS.md |

### Should Have
| ID | Owner | Task | Est. |
|----|-------|------|------|
| S7-08 | Claude | `Destructible` Tier A (knock-loose) + `SurfaceTag`/`SurfaceTable` + first Blender props (fence, pole, hay bale) | 2 h |
| X7-05 | Codex | `EnvironmentScatter` → per-tile scatter from `WorldPlan` spawning Tier A destructibles (ADR-0004 step 4) | 2 h |
| S7-09 | Claude | Roads from the global road graph on tiles (Asphalt/DirtRoad surfaces) | 2 h |

### Added mid-sprint
| ID | Owner | Task | Status |
|----|-------|------|--------|
| S7-12 | Claude | **CI** (first run failed: GameCI test-runner v4.3.2 has no Personal-license path — Unity removed .ulf for free seats; GameCI CLI v0.1.70 `personal` mode is the candidate, needs a CI-only Unity account w/o 2FA): GitHub Actions + GameCI 6000.6.0f1 — tests on push, WebGL/Windows builds + butler release on demand. Added 2026-10-01 after Windows Smart App Control blocked local WebGL builds (`WebGLPlayerBuildProgram.Data.dll`, unsigned, policy {0283ac0f…}) | Workflow written; needs secrets + first run |

### Nice to Have (cut first)
| ID | Owner | Task | Est. |
|----|-------|------|------|
| S7-10 | Claude | Tier B fracture: silo + barn intact/fractured pair (Blender Cell Fracture) + debris pool/budget | 3 h |
| S7-11 | Claude | `/map-systems` backfill: `systems-index.md` + `game-concept.md` from vision-1.0 | 1 h |

## Order of Work
1. **S7-00** (ship 0.5 art preview) — Day 1.
2. **S7-01 spike** and **S7-07 Blender setup** in parallel — the two highest-uncertainty items, early.
3. **S7-02 → S7-03 → S7-04/05/06** vehicle chain; Codex X7-01–03 follow S7-05/06 interfaces.
4. Should-haves as the chain completes; G3 feel playtest on the integrated build.

## Interfaces Codex needs early (Claude posts to AGENTS.md as they land)
- After S7-03: `VehicleState` flags (`IsGrounded/IsSliding/IsAirborne/IsTossed/IsUpended`),
  `SlipAngle`, `WheelContact[]` (position, grounded, surface).
- After S7-06: `GameEvents.StyleEvent/Landed/Impact/Tossed` signatures.
- After S7-05: `CameraRig.AimForward`, `StormCamTarget`.

## Risks
| Risk | Prob. | Impact | Mitigation |
|------|-------|--------|------------|
| 2 km streaming fails on WebGL (G1) | Medium | High | 1 km fallback with same code (ADR-0004 Alt 5) |
| Raycast vehicle tuning eats the sprint | High | High | Implement exactly the GDD formulas first; tune only after tests pass; feel tuning continues into Sprint 8 |
| Blender-MCP truck below bar (G2) | Medium | Medium | Commission the truck; props stay MCP |
| WebGL regressions from toon/outline renderer (S7-00) | Medium | Medium | Headless-Chromium console check before every push |
| Andy-time bottleneck at gates | Medium | Medium | Gates are ≤ 30 min, batched; captures via F9 |

## Definition of Done
- [ ] 0.5 Art Preview live on itch (html5 + windows), WebGL verified
- [ ] G1 decided (2 km or 1 km) and recorded in ADR-0004
- [ ] ADR-0005 accepted
- [ ] New vehicle replaces `PlayerVehicle` 0.4 in the shipping scene; all GDD unit/PlayMode criteria
      for S7-03–S7-06 pass; EditMode + PlayMode suites green
- [ ] Truck model in game (G2 passed or commission started)
- [ ] Codex X7-01–X7-04 done
- [ ] G3 feel playtest run; findings logged for Sprint 8

## Release Log
- 2026-10-01 — **0.6.0 pushed** (html5 + windows): new raycast vehicle (S7-03 + feel passes 1–2), tornado
  lift/toss, wind steer, heading-only camera, handbrake slide on title. Evidence: EditMode 96/96 pass;
  WebGL headless run 0 console errors, title 0.6.0, drive + handbrake slide verified
  (`production/qa/evidence/v060_*.png`). PlayMode run-loop tests not run before release — follow-up.
  Incident: script recompile during Play mode (two agents editing) produced ~65 NRE/frame; fix = Editor
  Preference "Recompile After Finished Playing".
- 2026-10-01 — **0.5.0 Windows** pushed (ArtTest look).
- 2026-10-01 — **0.5.0 html5** pushed after Andy unblocked local WebGL builds (Smart App Control). Verified in headless Chromium: 0 errors; title 0.5.0, toon/outlines, card tornado, camcorder PiP all render. **S7-00 done.**
- 2026-10-01 — Repo hygiene: 21 `(2)` duplicate files inside `.git` (Mar 31 copy-over artifact) moved to
  `../stormchaser-git-dupes-backup/`; the bad ref `refs/heads/main (2)` was breaking fetch. Rebased 25
  local commits onto origin (1 remote session-log commit) and pushed.

## Playtest Notes — S7-03 vehicle core (Andy, 2026-10-01)
"A little too loose but very close." Tornado pick-up/flip "very jarring." Truck got stuck in the ground
once or twice. Wants slightly less extreme suspension and camera turns.

Fixes (commit after e2f590f): grip μ 1.1→1.25, stiffness 1.5→2.0, yaw stability 0.5→0.7; sag 0.35→0.28,
damping 0.45→0.6; follow camera LockToTarget→LockToTargetWithWorldUp (camera was rolling/pitching with
the truck and flipping with it) + yaw damping 1.6; knockback spread over 0.15 s at 0.7× with grip 0.35×
during the window, roll stabilizer beyond 25°, wind lever 0.4→0.2 m; 2 m GroundSlab under the
zero-thickness Plane ground + stuck-recovery hop (2 s trying, <2 wheels, not moving).
GDD follow-up: record tuned defaults + F11 directional-wind correction in vehicle-feel.md after the next feel check.

**Pass 2 (Andy, 2026-10-01):** pass 1 "too stiff"; likes not getting stuck; "the more powerful tornadoes
should throw and the smaller should have a pull effect to steering."
Fixes: grip 1.15 / stiffness 1.7 / yaw 0.6; sag 0.32 / damping 0.52 (midpoints). Pulled S7-06 lift/toss
forward: lift coefficient 0.8→1.35 (EF3 lifts only, EF4 tosses at ≈ its 3.5 m damage edge, EF5 from ≈ 8.5 m);
toss impulse scales with EF (≈ 3 m / 4 m apex), no added spin; lift acts only while grounded (≤ 0.9 g);
toss latch requires leaving the ground (1 s pinned timeout). New wind steer: wind biases steering toward its
direction, ≤ 0.5 steer, so weak tornadoes tug the wheel. GDD vehicle-feel F12 coefficient to be updated.

## Playtest Notes — Codex presentation pass (Andy, 2026-10-02, via AGENTS.md)
"This looks and feels good." The implemented presentation pass is accepted. Not included: boost
visuals and audio (waiting on `PlayerVehicle.BoostActive` / `EngineLoad`), gameplay events not yet
raised, and the X7-06 performance gate.

Gameplay feedback: "the HP system ... is too fragile"; wants "a full HP system per truck." Not a
Sprint 7 change. Two Sprint 8 candidates, kept separate:

| ID | Candidate | Scope | Notes |
|----|-----------|-------|-------|
| S8-C1 | **Durability tuning** within the current model | S (config + GDD values) | e.g. Pickup Max HP above 3, or a `VehicleImpact` severity threshold so light bumps cost 0 HP |
| S8-C2 | **Per-truck HP redesign** (Andy 2026-10-03: do the full review, not just tuning) | M–L (`vehicle-damage.md` revision + `/design-review`, code) | Direction: a full health bar (100 %) or a numeric HP value per vehicle, with roguelite durability enhancements earned during a run. Damage scaled by impact severity. Capacities and rules undecided. Until then F10 collision HP cost ships **off** (impacts are detected and raised for VFX/audio only) |

Tracked as an Open Question in `design/gdd/vehicle-damage.md`.
