# Project Stage Analysis — Stormchaser Loop

**Date:** 2026-03-31
**Stage:** Production (2D) → Pre-Production (3D Unity rebuild)
**Developer:** Ghostweave Games

---

## Completeness Overview

| Area | Status | Detail |
|------|--------|--------|
| **Code (2D)** | ✅ Complete | 24 TS files, ~4,600 LOC — full game loop, all core mechanics |
| **Design Docs** | ⚠️ Partial | GDD exists (`docs/GAME_DESIGN_DOCUMENT.md`) but scoped to web; needs Steam/3D repositioning |
| **Engine Config** | ❌ Missing | `technical-preferences.md` unconfigured — Unity URP decision made 2026-03-31 |
| **Architecture Docs** | ❌ Missing | No ADRs; 2D→3D pivot needs to be recorded |
| **Tests** | ❌ Missing | No test suite |
| **Production Planning** | ❌ Missing | No sprints, milestones, or roadmap |

---

## What Exists

### Phaser 2D Game (Live on itch.io)
- **URL:** https://ghostweavelabs.itch.io/stormchaser-loop
- **Version:** v0.3.0

**Implemented systems:**
- Vehicle physics with terrain speed modifiers (highway 1.5×, mud 0.4×, etc.)
- Dynamic EF0-EF5 tornado lifecycle with particle debris
- Photography system: hold-to-aim mechanic, quality ratings (Poor→Perfect), pixelated photo generation
- Procedural terrain: simplex noise, multiple biomes, 2400×1350 world
- TV weather alerts (picture-in-picture), minimap, NPC system
- Local leaderboard, zzfx sound, complete scene flow

**Documentation:**
- Full GDD (`docs/GAME_DESIGN_DOCUMENT.md`)
- Terrain guide, tornado lifecycle doc, changelog, enhancement notes

---

## Strategic Context

### Core Loop
Chase tornado → photograph it → results screen → replay (90-second sessions)

### Core Pillars
1. Mastery through repetition (Hotline Miami / Super Meat Boy model)
2. Risk/Reward decisions (get close = danger + big points)
3. Photo collection (Pokémon Snap satisfaction)
4. Atmospheric immersion (TV news drama, procedural world)

### Competitor
**Funnel Runners** (https://store.steampowered.com/app/3712080/Funnel_Runners/) — survival-focused weather game on Steam. Stormchaser Loop differentiates via: photography mechanic, roguelike session structure, cinematic/atmospheric presentation.

### Platform Decision (2026-03-31)
Moving from web-first (Phaser 3) to **Unity URP** for a production Steam release. The 2D itch.io version remains live as the web presence.

---

## What Ports vs. What Rebuilds (Unity)

### Port (logic worth preserving)
- Scoring formula: `(AimScore × 0.6 + DistanceScore × 0.4) × TornadoStrength × 100`
- Photo quality thresholds (Poor/Decent/Good/Excellent/Perfect)
- Tornado EF scale mechanics and movement behavior
- Terrain biome definitions and speed modifiers
- Session structure (90s, combo decay, fuel pickup timing)
- Config/RemoteConfig pattern

### Rebuild (Phaser-specific, throw away)
- All rendering code (Phaser sprites, graphics, tilemaps)
- Scene management (replaced by Unity SceneManager)
- Arcade physics (replaced by Unity physics)
- HUD (rebuilt in Unity UI Toolkit or UGUI)
- Procedural terrain renderer (rebuilt as 3D mesh/tile system)

---

## Gaps to Address

1. **Engine not configured** — run `/setup-engine` to lock in Unity URP
2. **GDD needs Steam/3D repositioning** — current doc is web-scoped; run `/reverse-document` to reframe
3. **No ADRs** — record the 2D→3D decision and key architecture choices
4. **No production plan** — no milestones or sprints
5. **No tests** — scoring formulas and terrain logic are unit-testable and worth protecting during port

---

## Recommended Next Steps

| # | Action | Tool |
|---|--------|------|
| 1 | Configure Unity URP as engine | `/setup-engine` |
| 2 | Reframe GDD for Steam/3D | `/reverse-document` |
| 3 | Record 2D→3D pivot decision | `/architecture-decision` |
| 4 | Plan first 3D milestone | `/sprint-plan` |
| 5 | Design 3D-specific systems (volumetric tornado, 3D camera/photography) | `/design-system` |
