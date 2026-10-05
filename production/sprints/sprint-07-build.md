# Sprint 7 — Build the Feel (2026-10-01 → closes at G3, no later than 2026-10-15)

## Status: CLOSED 2026-10-04
Every Claude Must-Have is done, both suites green (EditMode 288/288, PlayMode 47/47 in 0.7.4), S7-06 physics
PASS. **G3 passed with tuning notes** (Andy alone, 0.7.5, checklist C1). The outside-player half of the feel
criteria moves to Sprint 9's **G6**, which must include 2 outside players. **Carried to Sprint 8:** the Codex
live acceptance pass (CA-1, checklist Part B) and decision C6 (CA-2). X7-02/03/04/06/07 stay "implemented,
acceptance pending" until CA-1 runs. X7-06 is reframed in M1 as whole-frame budgets (2026-10-04).

| ID | Status | Evidence / what's left |
|----|--------|------------------------|
| S7-00 | Done | 0.5.0 live (fd0be93) |
| S7-01 | Done | G1 PASS w/ condition, 2 km kept (5c364fb) |
| S7-02 | Done | ADR-0005 Accepted (dacca25) |
| S7-03 | Done | e2f590f, fd64700, 92b3996 |
| S7-04 | Done | b13cc65 |
| S7-05 | Done | 07e0f14 |
| S7-06 | Done | Impacts raise events (8213fea). G1 condition PASS: physics step max 1.20 ms vs 4 ms in WebGL (aba4810, `evidence/s7-06-physics.md`); light-debris caveat is Andy's call (C6). HP cost returns with S8-C1 |
| S7-07 | Done | G2 PASS (e268cdc) |
| X7-01 | Done | Superseded by X7-08 cab cam, shipped 0.7.3 (73a1cd6) |
| X7-02 / X7-03 | Partial | Shipped 0.7.0; live trigger/listening check by Andy pending |
| X7-04 | Partial | Single funnel, sky-first lifecycle, then tall cloud-to-ground funnels in 0.7.4 (02ffda7, 885940b, c8cc901); Andy's live check pending |
| X7-07 | Partial | Knock-loose props shipped 0.7.1–0.7.3; Andy's live check pending |
| X7-06 | Partial | GPU subchecks pass; Unity per-component CPU + max-pool budget pending |
| S7-08 | Partial | Knock-loose delivered via Codex X7-07; `SurfaceTable` and Blender props → Sprint 9 |
| X7-05, S7-09, S7-10 | Not started | → Sprint 9 (2 km world work) |
| S7-11 | Done | systems-index backfill (1068872) |
| S7-12 | Cut | Andy 2026-10-03 |
| G3 | **Passed with tuning notes** | Andy, 2026-10-04 on 0.7.5 (C1); outside-player criteria → Sprint 9 G6 |

**Unplanned but legitimate** (playtest-driven): solid scenery, brake assist, landing-wedge fix, X7-07 props,
X7-08 cab cam, DJ title. **Unplanned and moved to Sprint 8:** the Storm Director compact epic (GDD approved
2026-10-03; story 003 already in progress).

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
| **G2 — Truck asset** ✅ PASS (2026-10-03, Andy: "good enough for now"; final art pass later) | After S7-07 | Blender-MCP truck reads as a storm-chaser truck in ArtTest at gameplay distance; otherwise commission it |
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
| S7-12 | Claude | **CI** (first run failed: GameCI test-runner v4.3.2 has no Personal-license path — Unity removed .ulf for free seats; GameCI CLI v0.1.70 `personal` mode is the candidate, needs a CI-only Unity account w/o 2FA): GitHub Actions + GameCI 6000.6.0f1 — tests on push, WebGL/Windows builds + butler release on demand. Added 2026-10-01 after Windows Smart App Control blocked local WebGL builds (`WebGLPlayerBuildProgram.Data.dll`, unsigned, policy {0283ac0f…}) | **Cut** (Andy 2026-10-03): a CI-only Unity account isn't worth it. Workflow stays in repo, manual-only and dormant; tests and builds run locally |

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
- [x] 0.5 Art Preview live on itch (html5 + windows), WebGL verified
- [x] G1 decided (2 km or 1 km) and recorded in ADR-0004
- [x] ADR-0005 accepted
- [x] New vehicle replaces `PlayerVehicle` 0.4 in the shipping scene; all GDD unit/PlayMode criteria
      for S7-03–S7-06 pass; EditMode + PlayMode suites green — *0.7.4: EditMode 288/288, PlayMode 47/47
      (EF3 wind test fixed by Storm Director 003/004); S7-06 physics PASS (aba4810)*
- [x] Truck model in game (G2 passed or commission started)
- [ ] Codex X7-01–X7-04 done — *X7-01 done (via X7-08); X7-02/03/04 implemented, live check carried to
      Sprint 8 CA-1*
- [x] G3 feel playtest run; findings logged for Sprint 8 *(Andy alone; outside players → Sprint 9 G6)*

## Release Log
- 2026-10-05 — **0.8.2 built** (html5 + windows): **title attract mode** (drone flyover + showcase storm on the title,
  title music only, run start unchanged). Evidence: EditMode 389/389, PlayMode 70/70, WebGL 0 console errors
  (`attract-*.png`).
- 2026-10-05 — **0.8.1 pushed** (html5 + windows): **rotating radio jingles** (four new KTVR jingles + the original,
  no back-to-back), **fit pass** (title tightened, career page in two columns, HUD hidden behind title / WRECKED /
  results; every screen checked at 960×600, 1280×800, 1920×1080). Evidence: EditMode 388/388, PlayMode 68/68, WebGL 0
  console errors (`rs006-*.png`). Sprint 8 closed.
- 2026-10-05 — **0.8.0 pushed** (html5 + windows): **goals save** (Save & Profile M1 stem; WebGL persistence verified),
  **KTVR paint** earned at 5 career goals (title L / X toggle), **radio lite** (title loop, station jingle, five songs,
  ducking under warnings), **Settings** (camera SKY / CLASSIC / HIGH, sensitivity, invert Y, brightness, master /
  effects / music, fullscreen, resolution), **results with goals**, **WRECKED** slow-mo slam, **title career page**
  (C / Y). WebGL gzip + decompression fallback: 37 MB (was 71). Evidence: EditMode 383/383, PlayMode 68/68, WebGL 0
  console errors (`run-screens-{title,career,settings-pause,hud,results}.png`).
- 2026-10-04 — **0.7.9 pushed** (html5 + windows): **Run Goals in play** (run-goals-v1 001–004, 007): 10 career goals
  + 3 seeded KTVR bounties per run, bonuses into the score, **KTVR WANTS** HUD list and **GOAL!** pop-up; **lower chase
  camera** (12°, more sky); Codex **rain** (X8-03), **UI and goal sounds** (X8-06), **KTVR livery** (X8-05, preview
  `?livery=ktvr` until it is earned). Evidence: EditMode 355/355, PlayMode 64/64, WebGL 0 console errors
  (`rg-007-hud-bounties.png`, `x804_*`, `x83_rain_*`, `s9-02b_camera_*`). Goals are not saved yet (story 005, the M1
  stem).
- 2026-10-04 — **0.7.7 pushed** (html5 + windows): **pause menu and quitting** (run-screens story 001): P / Esc /
  Start pause (time, timer, audio frozen), Resume / Settings (placeholder) / Quit Run / Quit to Desktop (Windows),
  confirm defaults to KEEP PLAYING, quit forfeits the run, auto-pause on focus loss or controller disconnect,
  Windows title Esc-Esc quits. Evidence: EditMode 315/315, PlayMode 59/59, WebGL 0 console errors
  (`run-screens-*.png`).
- 2026-10-04 — **0.7.6 pushed** (html5 + windows): Codex **outdoor warning sirens** (X8-01) and **run-wide storm
  sky** (X8-02: overcast deck, near-black overhead for an EF5, eases back after rope-out). Evidence: EditMode
  309/309, PlayMode 55/55, WebGL 0 console errors (`v076_sky_*.png`). Andy live check: sirens good, tones OK
  pre-polish, funnel good. Pause / quit not in this build (run-screens RS-2, next).
- 2026-10-04 — **0.7.5 pushed** (html5 + windows): **forecast panel** (story 007: nearest storms with bearing,
  `~EF` estimate past 150 m, ON GROUND / PEAK ETAs, anchor in amber; off-screen indicator agrees), **results
  screen** shows WEATHER regime, seed + version replay line, "THE BIG ONE GOT AWAY" (story 009), **durability
  S8-C1**: Pickup 6 HP, light bumps free (Light 15.5 m/s), collision HP back on (2 HP only boosted/thrown).
  Director fixes from Codex review (long-frame lifecycle, restart cleanup, callback state). Codex: broadcast
  alert tones, darker EF4–5 funnels, jog lean. Evidence: EditMode 298/298, PlayMode 55/55, WebGL 0 console
  errors (`forecast-panel*.png`, `results-seed.png`, `s8c1_hp6.png`, `s8c1_after_drive.png`).
- 2026-10-04 — **0.7.4 pushed** (html5 + windows): **Storm Director live** (Sprint 8 stories 001–006, 008):
  seeded compact weather plans (replay with `?seed=N`), F3 storm scale (EF3 shoves, EF4/5 toss in their
  cores, EF5 inflow drags you in), storm tracks with late-life jogs, `StormCell*` events, KTVR News crawl with
  TORNADO EMERGENCY for EF5, runs now 3:00 (compact T = 180 s, GDD Rule 12). Also: wheel visuals follow the
  suspension (no sinking), Blender hero pickup default, Codex tall cloud-to-ground funnels + slender sway,
  PiP near clip 1.5 m. Evidence: EditMode 288/288, PlayMode 47/47, WebGL 0 console errors
  (`production/qa/evidence/v074_*.png`). Known: EF5 wedge reads pale, not near-black (Codex art note);
  forecast HUD (007) and results seed (009) not in yet.
- 2026-10-03 — **0.7.3 pushed** (html5 + windows): X7-08 viewfinder renders the roof cab cam (Codex), zoomed to
  the ±15° scoring cone; X7-07 props accepted (`LooseSceneryPlayTests` 6/6). Evidence: EditMode 182/182,
  PlayMode 33/34 (known EF3 wind failure), WebGL 0 console errors (`production/qa/evidence/v073_*.png`).
- 2026-10-03 — **0.7.2 pushed** (html5 + windows): S7-05 camera (orbit/recenter, Storm Cam F14, camera-forward
  aim via roof `CamcorderMount`), landing-wedge fix (lifted wheel casts) + heavier jump, KTVR DJ title with
  Andy's key-art banner; Codex: knock-loose bales/mailboxes/signs/crates, sky-first tornado lifecycle, PiP on
  the main camera. Evidence: EditMode 180/180, PlayMode 27/28 (known EF3 wind failure), WebGL 0 console
  errors (`production/qa/evidence/v072_*.png`). Known: viewfinder still mirrors the main camera until Codex
  renders it from `CamcorderMount` (AGENTS.md request).
- 2026-10-03 — **0.7.1 pushed** (html5 + windows): solid scenery (Codex: trunks, barns, silos, poles block;
  45 kg fences knock loose as E13 destructibles) and arcade brake assist (full-speed stop ≈ 1.1 s). Evidence:
  PlayMode 11/12 (new ScenerySolidityTests 3/3: trunk/barn block with impact and no HP loss, canopy not solid,
  fence flies while truck keeps ~16 m/s; EscapeTurn 150° in 1.10 s); known EF3 wind failure unchanged. WebGL
  verified on real GPU, 0 errors (`production/qa/evidence/v071_*.png`).
- 2026-10-03 — **0.7.0 pushed** (html5 + windows): S7-04 verbs (jump, boost + style refills, air control,
  RT/LT/A/B/X/RB map), S7-06 impacts (VehicleImpact raised, HP cost off pending S8-C2), Codex boost
  flame/roar, sparks/crunch, DRIFT/AIR/NEAR MISS pops, HUD boost meter. Evidence: EditMode 138/138, PlayMode
  7/8 (EF3 wind test fails deterministically, 0.47 m vs 0.5 m, pending Storm Director F3), WebGL verified in
  Chrome on real GPU, 0 errors (`production/qa/evidence/v070_*.png`).
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

## Playtest Notes — 0.7.0 (Andy + son, 2026-10-03)
"Looks and feels pretty good"; son: "the driving is good." Findings and where they land:

| Finding | Triage | Where |
|---|---|---|
| Truck drives through trees, barns, silos | **Defect**: `EnvironmentScatter` strips prop colliders. Solid trunks/barns/silos/poles; fences as light destructibles (E13) | 0.7.1 — Codex (Environment lane), requested in AGENTS.md |
| Braking should feel like Rocket League: stops too slowly | Tuning: raise `BrakeDecel` (14 m/s² now) within the GDD safe range | 0.7.1 — Claude |
| Boost should crash through big props | Already designed: Ram & Unblock ≥ 50 MPH; Tier A knock-loose (S7-08), Tier B barn/silo fracture (S7-10) | S7-08 / S7-10 → Sprint 8 |
| Score pops only for photos | Design gap: style moves refill boost but don't score. THPS-style run goals in `event-system.md` | Sprint 8 design |
| More worlds | 2 km streamed world (ADR-0004) + Arcade scenarios; biomes undesigned | 0.8+ |
| Professional title screen, game over, quit | Basic results screen + Esc quit (Windows only) exist; needs `/ux-design` title, pause/quit menu, game-over | Sprint 8 polish |
| Story mode with quests (Fortnite × THPS career: levels with goal lists — rescues, pets, tricks, shots) | New mode alongside Arcade / Epic; builds on run goals + Storm Director | `/brainstorm` → `session-modes.md` revision |

## Playtest Notes — 0.7.1 (Andy, 2026-10-03)
| Finding | Triage | Where |
|---|---|---|
| Truck sometimes lands at an angle "in the ground" and you have to jump out | **Defect.** Wheel sphere casts started at the axle; with the body resting on a corner, the low wheels' spheres began inside the ground and Unity casts skip those, so two springs went dead and the truck sat at ≈ 15° on two wheels. Fix: casts start 0.4 m higher (nearest non-self hit), frictionless body shell, pitch levelling beyond 30°, stuck recovery also when tilted > 35° (1 s). Regression: `VehicleVerbsPlayTests.NoseFirstLanding_*` (12 cases; 6 failed before the fix) | Fixed, Claude |
| Jump a little floaty | Tuning: air gravity × 1.5 rising / × 2 falling, jump 6.65 m/s, same 1.5 m apex | Fixed, Claude (vehicle-feel F8) |
| Driving still a little loose | Minor; revisit with G3 feel playtest | G3 |
| Tornadoes sprout from and shrink into the ground; real ones come down from the sky | Presentation: funnel descends from a cloud base while Forming, retracts up on rope-out (matches `storm-director.md` Lifecycle) | Codex, AGENTS.md request 2026-10-03 |
| Title tagline and rules text "so AI"; wants Burnout / Crazy Taxi energy | Rewritten: KTVR STORM RADIO DJ shouting rotating lines, "PRESS ANYTHING. GO GO GO." | Done, Claude |

## Final Art Pass Backlog (hero pickup, deferred to the polish milestone)
G2 passed on a placeholder-grade model on purpose: the script-built truck carries gameplay now, and the
final pass upgrades it in place (`tools/blender/build_pickup.py`) or swaps in a commissioned model
(`TruckVisualSelector` keeps any visual behind one switch). Logged so none of it is lost:
- Wheels spin with speed and the fronts steer (they're already separate meshes pivoted at the hub).
- Suspension travel on the visual body (bob, squat on throttle, dive on brake).
- Decal sheet: tornado-warning diamond, chase-team livery, numbers. Our own designs, no real logos.
- Damage stages (`vehicle-damage.md`): dents, cracked glass, lost light bar / mast as HP drops.
- Beacon and light bar animate (emissive pulse); headlights at night (Heat 3 Blackout).
- Anemometer cups spin with `CurrentWind`; vane points into the wind.
- Higher-fidelity silhouette pass vs the Fennec / Tacoma references (more panel breaks, a camper-cap
  variant), still within the WebGL budget.
- Per-archetype vehicles (SUV / Storm Rig, interceptor, mesonet van) from the same script pipeline.
- Reference photos stay local (gitignored): the repo is public.

