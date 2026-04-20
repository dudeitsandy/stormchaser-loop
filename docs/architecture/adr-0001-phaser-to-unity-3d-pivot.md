# ADR-0001: Phaser 2D to Unity 3D Platform Pivot

## Status
Accepted

## Date
2026-03-31

## Context

### Problem Statement
Stormchaser Loop launched as a web game on itch.io (v0.3.0) built with Phaser 3 +
TypeScript + Vite. While the core loop is proven and fun, the web-first 2D format
limits the visual fidelity, depth of atmosphere, and platform reach needed to compete
as a production game on Steam. A competitor (Funnel Runners, Steam App 3712080)
already occupies the survival weather game space, making differentiation through
presentation and depth critical. The 2D approach cannot deliver the cinematic,
immersive feel the game requires.

### Constraints
- Solo developer (Ghostweave Games)
- Existing Phaser codebase (~4,600 LOC TypeScript) is a reference, not a starting point
- Must support Steam Early Access pipeline (Steamworks SDK, achievements, cloud saves)
- Must target 60 FPS on PC, 30 FPS on Steam Deck
- Web version (itch.io) to remain live for awareness until Steam Early Access launch,
  then sunset

### Requirements
- Must support true 3D environments (realized 3D world space, not pseudo-3D)
- Must deliver the visual target: low-poly 3D with pixel post-process filter,
  CRT scanlines, bloom on headlights, film grain, teal-orange color grade
- Must support the photography risk mechanic (see Decision section)
- Must support gamepad input (PS5 DualSense primary, Xbox compatible)
- Must support a post-FX pipeline with: bloom, chromatic aberration, vignette,
  scanlines, film grain, color grading
- Must be capable of a Steam PC release (Windows primary, Steam Deck secondary)

## Decision

Migrate Stormchaser Loop from Phaser 3 (TypeScript, web) to **Unity 6.3 LTS**
with the **Universal Render Pipeline (URP)**.

The itch.io Phaser version is preserved as-is for player awareness and will be
sunset at Steam Early Access launch.

### Platform Target
- **Primary**: Steam (PC, Windows)
- **Secondary**: Steam Deck (Linux/Proton, 30 FPS target)
- **Sunset**: itch.io web version (kept live until EA, then archived)

### Rendering Architecture
True 3D world built on Unity URP:
- Low-poly 3D environment (road, fields, sky, buildings) rendered in real 3D space
- Pixel post-process filter applied as a full-screen URP Renderer Feature to achieve
  the pixel-art aesthetic without sacrificing 3D geometry and camera freedom
- Full-screen post-FX stack via URP Render Graph: bloom (Kawase/Dual filter),
  chromatic aberration, vignette, film grain, CRT scanlines, color grading (teal-orange LUT)
- Tornado: multiple alpha-blended cone meshes with noise-scrolled textures + GPU
  particle debris system

### Camera & Photography System Architecture

The photography mechanic is redesigned as a **dual-view risk system**:

```
┌─────────────────────────────────────────────────────┐
│                  MAIN VIEW (1st person)              │
│                                                      │
│   [Road / World / Driving perspective]               │
│                                                      │
│                           ┌──────────────┐           │
│                           │  PiP CAMERA  │           │
│                           │  [Aim view]  │           │
│                           │  Activates   │           │
│                           │  on L1 hold  │           │
│                           └──────────────┘           │
└─────────────────────────────────────────────────────┘
```

- **Main view**: 1st-person driving camera (always active)
- **PiP window**: Secondary camera rendered to a RenderTexture, displayed as a
  screen-space UI overlay. Activates when the player holds the camera button.
- **Aim control**:
  - Gamepad: Hold L1 → right stick to pan PiP camera, touchpad tap to shoot
  - Keyboard: Hold camera key → mouse or WASD to aim, Space to shoot
- **Risk mechanic**: Player must continue driving in the main view while managing
  aim in the PiP — splitting attention is the core tension. Getting closer to the
  tornado = higher score but requires both driving and aiming simultaneously.
- **Photo capture**: Triggered from PiP view. Quality scored on aim duration,
  tornado distance, tornado EF rating (ported from Phaser scoring formula).

### What Ports vs. What Rebuilds

| System | Decision | Notes |
|--------|----------|-------|
| Scoring formula | **Port** | `(AimScore × 0.6 + DistanceScore × 0.4) × EFStrength × 100` |
| Photo quality thresholds | **Port** | Poor/Decent/Good/Excellent/Perfect bands |
| Tornado EF scale + movement | **Port** | Behavior logic; rendering rebuilt |
| Terrain biome definitions | **Port** | Speed modifiers, biome types |
| Session structure (90s, combo decay) | **Port** | Config-driven values |
| All Phaser rendering code | **Rebuild** | Replaced by Unity URP + 3D meshes |
| Physics | **Rebuild** | Arcade physics → Rigidbody / CharacterController |
| UI (HUD, minimap, weather alerts) | **Rebuild** | Rebuilt in UI Toolkit |
| Scene management | **Rebuild** | Phaser Scenes → Unity SceneManager |
| Procedural terrain | **Rebuild** | Tile sprites → 3D mesh generation |

## Alternatives Considered

### Alternative 1: Stay Web-First (Phaser 3 Upgrade)
- **Description**: Upgrade Phaser to pseudo-3D (OutRun-style road renderer),
  add full-screen post-FX pipeline via custom WebGL shaders, billboard tornado.
- **Pros**: No migration cost; stays under 5MB; easy itch.io deploy; TypeScript skills
  transfer directly; ~85% of the visual target achievable.
- **Cons**: Cannot achieve true 3D camera freedom; pseudo-3D limits level design;
  no viable path to Steam (WebGL bundles are 20MB+ with wrappers); harder to add
  controller support; limited post-FX compositing compared to URP Render Graph.
- **Rejection Reason**: Steam is the target platform. True 3D is required for the
  cinematic camera system and volumetric tornado. The ceiling of the web approach
  is too low for a production game.

### Alternative 2: PlayCanvas (Web-Native 3D)
- **Description**: Move to PlayCanvas (JavaScript/TypeScript, real-time 3D, web-native).
- **Pros**: Web-native; small builds (2-5MB); real 3D; TypeScript skills transfer;
  excellent itch.io fit.
- **Cons**: No viable Steam pipeline; limited asset store; less mature post-FX;
  no path to console. Optimizes for the platform we're leaving, not the one we're
  targeting.
- **Rejection Reason**: Same fundamental problem as Alternative 1 — web-first
  engine with no credible Steam production path.

### Alternative 3: Godot 4
- **Description**: Open-source 3D engine with GDScript/C#, good 2D-3D blend.
- **Pros**: Free/MIT; no licensing concerns; good 3D; open source.
- **Cons**: Web export with heavy post-FX is still experimental in 4.6; smaller
  ecosystem than Unity for Steam tools (Steamworks integration, achievement systems);
  GDScript requires full language learning (unlike C# which is closer to TypeScript).
- **Rejection Reason**: Unity has a more mature Steam production pipeline, better
  post-FX tooling for the visual target, and a larger verified plugin ecosystem.

## Consequences

### Positive
- Full 3D camera freedom enables cinematic follow cam, shake, spring-damped sway
- True volumetric tornado (cone meshes + GPU particles) vs. billboard sprite
- URP Render Graph delivers the full post-FX stack natively
- Steam pipeline: Steamworks SDK, achievements, cloud saves, Steam Deck verification
- C# is structurally close to TypeScript — logic porting is straightforward
- Long-term path to console (PS5, Xbox) via Unity platform licensing

### Negative
- Full engine migration — no incremental path, greenfield Unity project
- C# and Unity API learning curve for a TypeScript developer
- Unity 6.3 URP Render Graph is post-training-cutoff — agents must reference
  `docs/engine-reference/unity/` before suggesting rendering code
- Larger build sizes than web (~500MB installed vs. ~5MB web)
- itch.io web audience lost at sunset (mitigated by keeping Phaser version live
  until EA launch)

### Risks
- **URP Render Graph complexity**: Custom post-FX requires Render Graph API (not
  legacy Compatibility Mode, which is removed in 6.3). Mitigation: reference
  `docs/engine-reference/unity/modules/rendering.md` before implementing.
- **Photography PiP system**: RenderTexture + dual-camera setup has performance
  cost. Mitigation: PiP camera renders at lower resolution; only active when
  player holds camera button.
- **Scope expansion**: 3D adds art production cost (modelling, texturing).
  Mitigation: low-poly aesthetic reduces poly counts and texture resolution
  requirements; procedural generation reduces hand-authored content.
- **Steam Deck performance**: 3D + post-FX stack may not hit 30 FPS on Deck.
  Mitigation: scalable quality settings; post-FX toggles in options menu.

## Performance Implications
- **CPU**: Higher than Phaser (3D scene graph, physics, C# GC). Mitigated by
  low-poly geometry and short session length (90s).
- **Memory**: ~200-500MB estimated for full build vs. ~30MB for web.
- **GPU**: Post-FX stack (bloom, grain, scanlines, CA) adds fillrate cost.
  Target: all effects enabled at 60fps on GTX 1060 equivalent.
- **Load Time**: Unity builds have longer initial load than web. Target: <10s
  cold start on SSD.

## Migration Plan

1. **Phase 1 — Unity Project Setup**: Create Unity 6.3 LTS project, configure URP,
   verify post-FX pipeline (bloom, scanlines, grain, CA, color grading).
2. **Phase 2 — Core Systems Port**: Player vehicle (Rigidbody), tornado entity
   (EF logic + particle system), scoring formula, session timer.
3. **Phase 3 — 3D Environment**: Low-poly road + terrain mesh, procedural biome
   placement, sky gradient.
4. **Phase 4 — Photography System**: PiP camera (RenderTexture), aim mechanic,
   photo quality scoring, Polaroid results overlay.
5. **Phase 5 — UI Rebuild**: HUD, minimap, weather alerts, results screen
   (UI Toolkit).
6. **Phase 6 — Vertical Slice**: Playable 90-second session matching v0.3.0
   feature parity with new visual target.
7. **Phase 7 — Steam Integration**: Steamworks SDK, achievements, cloud saves,
   Steam Deck verification.
8. **Phase 8 — Early Access**: Launch.

## Validation Criteria
- Playable 90-second session runs at 60 FPS on GTX 1060 equivalent PC
- Playable at 30 FPS on Steam Deck (verified via Steam Deck compatibility checker)
- PiP photography mechanic reproduces scoring parity with Phaser v0.3.0
  (same score ranges for same skill level)
- All post-FX active simultaneously without dropping below 60 FPS on target hardware
- Steamworks achievements fire correctly in Steam client

## Related Decisions
- See `production/project-stage-report.md` for full project gap analysis
- See `docs/GAME_DESIGN_DOCUMENT.md` for original game design (web-scoped; to be
  revised for Steam/3D in subsequent ADR or `/reverse-document` pass)
- Future ADR: Photography system architecture (PiP camera, RenderTexture pipeline)
- Future ADR: Procedural terrain system (3D mesh vs. tile-based approach)
