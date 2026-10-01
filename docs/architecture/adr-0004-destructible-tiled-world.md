# ADR-0004: Destructible Tiled World (2 km Streamed Low-Poly Tiles)

## Status
Accepted (Andy, 2026-10-01)

## Date
2026-10-01

## Context

### Problem Statement
The 0.4 world is a 200 m flat plane with ~65 runtime-scattered primitive props. The vision
(`vision-1.0.md`) calls for a **modular biome hybrid** — handcrafted POI chunks stitched by procedural
roads — where barns collapse, silos topple, wreckage becomes ramps, and Season 2 adds earthquake
fissures and sinkholes. `vehicle-feel.md` (approved 2026-10-01) now depends on this decision for
surface grip (F3 `SurfaceGrip`), destructible piece mass (E13), uneven ground for its suspension, and
Ram & Unblock obstacles. Art (ADR-0003) and the Blender asset pipeline need to know what a "world
piece" is before production starts. This must be decided before Sprint 7 builds the vehicle, terrain,
and art together.

### Constraints
- **Platforms:** 60 FPS PC, 30 FPS Steam Deck, **WebGL** (single-threaded PhysX, tight memory, GC
  spikes visible as hitches).
- **Physics:** vehicle already budgeted at ≤ 0.3 ms per 50 Hz step (PC), ≤ 0.8 ms (WebGL).
- **Engine:** Unity 6.6 URP, Render Graph; built-in PhysX (no DOTS in this project).
- **Team:** solo dev + two coding agents, part-time. Agents are weak at authoring organic 3D art;
  hard-surface kit pieces via Blender MCP are feasible (Blender 5.2 installed).
- **Art:** ADR-0003 stylized cel-shaded world — flat-shaded low-poly reads better than realistic terrain.

### Requirements
- World of **2 km × 2 km** per run (decided by Andy 2026-10-01), streamed around the player.
- Uneven terrain (rolling fields, ditches, embankments) the suspension can work on.
- Season 1 destruction: **props + buildings**; ground static (deformation is Season 2).
- **Tornadoes destroy structures**; debris is swept by the wind field and is a hazard.
- Surface types readable per wheel contact (grip/drag for `vehicle-feel.md` F3).
- Every breakable piece has a mass (`vehicle-feel.md` E13) and the Ram & Unblock threshold model
  (`event-system.md`).
- Seeded and deterministic: same seed → same world (leaderboards, daily runs later).
- Destruction persists when a tile streams out and back in.

## Decision

**A 2048 m world made of a 16 × 16 grid of 128 m tiles, generated from a run seed and streamed around
the player. Each tile is a custom low-poly heightfield mesh plus kit pieces (roads, POIs, props).
Destruction uses tiered, pre-fractured prefabs with a global debris budget. Ground is static in
Season 1.**

### 1. World plan (run start, ~0 cost at runtime)
- From the run seed, generate a **WorldPlan** once: biome per tile (Season 1: Heartland only), the
  **road graph** across the whole map (so roads connect over tile seams), and **POI placement**
  (which tiles host which handcrafted POI chunk: farmstead, grain elevator, town crossroads, …).
- The plan is plain data; tiles are built from it on demand.

### 2. Tiles & streaming
- **Active ring:** tiles within **radius 2** of the player's tile (5 × 5 = 640 m span) are fully live:
  render, colliders, props, destructibles. Fog (ADR-0003, end ≈ 170–250 m) hides the ring edge.
- **Distant silhouette:** one low-resolution whole-map mesh (32 m grid, no colliders, no props) renders
  beyond the ring for horizon shape.
- **Build/recycle:** entering a new tile queues builds for the newly in-range tiles and recycles
  out-of-range ones to a pool. Builds are **time-sliced** (≤ 2 ms per frame, a tile completes in ≤ 6
  frames) with pooled meshes and arrays — no per-tile GC allocations (WebGL hitch risk).
- **No scene streaming / Addressables in Season 1:** tiles are code-generated; POI chunks are prefabs.
  Addressables can be introduced later if POI memory demands it.

### 3. Terrain representation
- Per tile: a **33 × 33 vertex heightfield** (4 m spacing), flat-shaded low-poly mesh, height from
  seeded noise shaped by biome rules (gentle rolls in Heartland, flattened under roads and POIs).
- **Roads** are separate kit meshes laid along the road graph, slightly above the heightfield, with
  their own colliders.
- **Collider per surface region**: heightfield (Grass), road meshes (Asphalt / DirtRoad), and flat
  patch meshes (Mud, Gravel) each carry a `SurfaceTag`.

### 4. Surface types (feeds `vehicle-feel.md` F3)

| SurfaceType | Grip | Drag (m/s² at speed) | Notes |
|-------------|------|----------------------|-------|
| Asphalt | 1.00 | 0.0 | 2D "highway 1.5×" returns as best grip + no drag |
| DirtRoad | 0.85 | 0.3 | |
| Grass | 0.75 | 0.6 | Default field |
| Gravel | 0.70 | 0.8 | Loose, slidey |
| Mud | 0.45 | 2.5 | 2D "mud 0.4×" |
| Debris | 0.60 | 1.0 | Settled destruction rubble |

Wheel raycast hit → `SurfaceTag` on the hit collider → `SurfaceProperties`. Untagged = Grass.

### 5. Destruction tiers

| Tier | Examples | Representation | Breaks when | Physics |
|------|----------|----------------|-------------|---------|
| **A — Knock-loose** | Fences, poles, signs, hay bales, mailboxes | Static collider until hit | Any impact ≥ 3 m/s or wind ≥ EF1 nearby | Converts to a single dynamic rigidbody; no fracture |
| **B — Fracture** | Barns, silos, sheds, houses, billboards | Intact prefab + pre-fractured chunk prefab (6–20 chunks, cut in Blender via Cell Fracture) | `Integrity` ≤ 0 (vehicle impact energy and/or tornado damage) | Intact disabled → chunks spawned from pool with impulse |
| **C — Static** | Ground, roads, bridges (Season 1) | Static | Never in Season 1 | — (Season 2: crater/fissure **stamps** modify the heightfield) |

- **Integrity damage:** vehicle `0.5 · m_vehicle · v_impact² / 1000` (kJ, Tier B only above 8 m/s);
  tornado `EFStrength · Intensity · 40` per second while the structure is inside the damage radius
  (×0.3 inside the wind radius). A barn (integrity 300) survives an EF0 graze but an EF3 tears it apart
  in ~3 s; a 2100 kg Pickup at 22 m/s (508 kJ) smashes it alone.
- **Mass** is authored per piece/chunk (density × volume at authoring time): fence 40 kg, pole 120 kg,
  hay bale 250 kg, silo chunk 800 kg, barn wall 1500 kg — feeds `vehicle-feel.md` E13.
- **Event obstacles** (Ram & Unblock) are Tier B destructibles flagged `IsEventObstacle` with an
  `ObstacleMassThreshold`; they break only through `event-system.md`'s ramming rule.

### 6. Debris lifecycle & budget
- Chunks/loose props come from a **global pool**. Hard cap of simultaneously **dynamic** debris:
  **PC 150 / Steam Deck 80 / WebGL 60**. When full, the oldest settled pieces are frozen first.
- **Lifecycle:** dynamic → sleeps (PhysX) → after 8 s settled, frozen to a static `Debris`-surface
  collider (or removed if tiny) → removed when its tile streams out (state persists, see §7).
- **Tornado sweep:** debris inside a funnel's wind radius receives wind force from `WindField`
  (same model as the vehicle, `vehicle-feel.md` F11/F12), max **30 swept pieces per tornado**; swept
  pieces deal impact damage to the truck through the normal F10 path (mass-scaled by E13).

### 7. World state persistence
- `WorldState` records per structure ID (tile coord + local index): Intact / Broken, plus settled
  debris snapshots (position/rotation, up to 20 per tile). Rebuilding a tile applies its state, so a
  barn destroyed earlier stays destroyed.
- **Off-ring destruction:** tornadoes keep simulating anywhere on the map. When a funnel's damage
  radius passes over an unloaded tile's structures, `WorldState` marks them Broken and seeds a debris
  snapshot — the player arrives to the aftermath.

### Architecture Diagram

```
Run seed ──▶ WorldPlan (biomes, road graph, POI placement)        [once, data]
                 │
                 ▼
 Player tile ──▶ TileStreamer ── builds/recycles (time-sliced, pooled) ──▶ Tile (128 m)
                 │                                                        ├─ Heightfield mesh + SurfaceTag(Grass)
                 │                                                        ├─ Road kit meshes + SurfaceTag(Asphalt/Dirt)
                 │                                                        ├─ Patch meshes + SurfaceTag(Mud/Gravel)
                 │                                                        ├─ POI chunk prefab(s)
                 │                                                        └─ Destructibles (Tier A/B, pooled)
                 ▼
             WorldState ◀── StructureDestroyed ── Destructible.Break()
                 ▲                                      ▲            ▲
   off-ring tornado sweep                 vehicle impact (F10)   tornado damage
                                                        │
                                   DebrisPool (budgeted) ── WindField sweep ──▶ hazards to vehicle
 Vehicle wheel raycast ──▶ SurfaceTag ──▶ SurfaceProperties ──▶ vehicle-feel F3
 DistantSilhouette (whole map, 32 m, no colliders) beyond the active ring
```

### Key Interfaces

```csharp
public enum SurfaceType { Asphalt, DirtRoad, Grass, Gravel, Mud, Debris }
public readonly struct SurfaceProperties { public readonly float Grip; public readonly float Drag; }

/// On any world collider. Vehicle reads it from RaycastHit.collider.
public sealed class SurfaceTag : MonoBehaviour { public SurfaceType Type; }
public static class SurfaceTable { public static SurfaceProperties Get(SurfaceType t); }

public enum DestructibleTier { KnockLoose, Fracture, Static }
public sealed class Destructible : MonoBehaviour
{
    public StructureId Id { get; }
    public DestructibleTier Tier { get; }
    public float Mass { get; }               // vehicle-feel E13
    public float Integrity { get; }
    public bool IsEventObstacle { get; }     // event-system Ram & Unblock
    public void ApplyImpact(float impactSpeed, float otherMass, Vector3 point, Vector3 normal);
    public void ApplyWindDamage(float damagePerSecond, float deltaTime);
    public event System.Action<Destructible> Broken;
}

public interface IWorldStreamer
{
    Vector2Int PlayerTile { get; }
    bool IsTileActive(Vector2Int tile);
    event System.Action<Vector2Int> TileActivated, TileDeactivated;
}

public sealed class WorldState
{
    public bool IsBroken(StructureId id);
    public void MarkBroken(StructureId id, DebrisSnapshot snapshot = default);
}

public static class DebrisBudget { public static bool TryAcquire(int count); public static void Release(int count); }

// GameEvents addition (Claude lane contract; Codex VFX/audio listens)
// public static event Action<StructureDestroyedInfo> StructureDestroyed; // position, tier, byPlayer, EF
```

## Alternatives Considered

### Alternative 1: Unity Terrain (heightmap + painting)
- **Description:** One or more Unity Terrain objects with splat-painted surfaces.
- **Pros:** Mature tooling, built-in LOD, painting UI, tree/detail systems.
- **Cons:** Realistic look fights ADR-0003; heavier on WebGL; awkward to generate per seed and to stamp
  deformations into at runtime; surface lookup via splat sampling per wheel.
- **Rejection Reason:** Art-direction mismatch and WebGL weight; procedural tiles give the stylized,
  seedable world directly.

### Alternative 2: Pure modular kit on flat ground
- **Description:** Everything snaps together from prefab pieces on a flat base.
- **Pros:** Simplest; easiest to author with Blender MCP.
- **Cons:** No rolling terrain — `vehicle-feel.md`'s suspension and airtime have nothing to work on.
- **Rejection Reason:** Fails Pillar 1 (Kinetic Chaos) and the Rocket League feel target.

### Alternative 3: Runtime fracture (Voronoi at break time)
- **Description:** Compute fracture geometry when a structure breaks.
- **Pros:** Unique breaks every time; no authoring of chunk prefabs.
- **Cons:** CPU spikes at the worst moment (mid-crash), GC churn, risky in single-threaded WebGL.
- **Rejection Reason:** Pre-fractured swap gives 90 % of the spectacle at a fraction of the cost.

### Alternative 4: Voxel / fully deformable terrain
- **Description:** Voxel world allowing digging, craters, collapse anywhere.
- **Pros:** Maximum emergent destruction (great for Season 2 earthquakes).
- **Cons:** Large engineering scope, poor fit for flat-shaded stylized art, heavy for WebGL/Deck.
- **Rejection Reason:** Season 2 needs are covered by heightfield **stamps** at far lower cost.

### Alternative 5: 1 km all-loaded world (no streaming)
- **Description:** 8 × 8 tiles always resident.
- **Pros:** No streaming system; simplest and safest on WebGL.
- **Cons:** Less room for Chase mode's 5–7.5 min storm front; map knowledge saturates quickly.
- **Rejection Reason:** Andy chose 2 km for scale and replayability (2026-10-01). Retained as the
  **fallback** if the WebGL streaming spike (Validation #1) fails: drop to radius-2 streaming on 1 km
  or all-loaded 1 km with the same tile code.

### Alternative 6: Scene-based streaming (additive scenes / Addressables)
- **Description:** Each tile is an authored scene loaded asynchronously.
- **Pros:** Hand-authorable tiles with full editor tooling.
- **Cons:** Async scene loading and asset bundles are the riskiest pieces on WebGL; doesn't fit a
  seeded procedural map; heavier build pipeline.
- **Rejection Reason:** Generated tiles + POI prefabs need neither; revisit for POI memory only.

## Consequences

### Positive
- Suspension, airtime, and surfaces finally matter (`vehicle-feel.md` depends on this).
- The storm visibly reshapes the map; debris creates emergent ramps and hazards (Pillar 2 and 4).
- Seeded worlds enable daily/seeded leaderboards later.
- Clear asset contract for the Blender pipeline: kit pieces + intact/fractured pairs.
- Season 2 earthquakes extend this with heightfield stamps instead of a rewrite.

### Negative
- A streaming system, world plan, persistence layer, and debris pool are real engineering (Sprint 7–8).
- Every Tier B structure needs two authored assets (intact + fractured).
- Fog/draw distance is now a gameplay constraint (ring radius 2 ≈ 320 m visible).
- Off-ring destruction is approximate (state marks + snapshots, not simulated physics).

### Risks
- **WebGL streaming hitches** (GC, mesh upload). *Mitigation:* pooled meshes/arrays, ≤ 2 ms slices,
  WebGL spike test first (Validation #1); fallback = 1 km (Alternative 5).
- **Debris physics blowing the budget** when a tornado shreds several buildings at once.
  *Mitigation:* hard caps per platform, freeze-oldest policy, swept-piece cap per tornado.
- **Tile seams** (heights/roads mismatched at borders). *Mitigation:* heights from a global noise
  function sampled in world space; roads from the global road graph, not per tile.
- **Tornado lift on debris vs. vehicle tuning** diverging. *Mitigation:* one `WindField` model for both.
- **Scope creep toward Season 2 deformation.** *Mitigation:* Tier C is explicitly static in Season 1.

## Performance Implications
- **CPU:** Physics step total (vehicle + colliders + dynamic debris) ≤ 2.5 ms PC, ≤ 4 ms Steam Deck,
  ≤ 4 ms WebGL. Tile build ≤ 2 ms per frame (time-sliced). Wind sweep on debris ≤ 0.5 ms.
- **Memory:** ~25 active tiles × (33² verts + kit) is small; the distant silhouette ≈ 64² verts. Pools
  sized to debris caps. Target ≤ 150 MB added on WebGL.
- **Load Time:** WorldPlan + initial 5 × 5 ring built during the title screen / run start (< 1.5 s).
- **Network:** N/A.
- **Rendering:** SRP Batcher with `Doomsday/ToonLit`; GPU instancing for repeated props; outline pass
  cost unchanged (screen-space).

## Migration Plan
1. **Spike (Sprint 7, first):** TileStreamer + heightfield tiles + distant silhouette on WebGL with
   placeholder props. Measure hitches and the physics step. Go/no-go on 2 km.
2. Replace `TerrainSetup.cs` (Claude lane) with `WorldPlan` + `TileStreamer`. Retire the 200 m plane.
3. Widen world bounds everywhere they're hard-coded: `PlayerVehicle._worldHalfExtent` (95 → 1024),
   `TornadoController._worldHalfExtent`, `DisasterSpawner._worldHalfExtent` (spawning becomes relative
   to the player/storm front, not the origin).
4. `EnvironmentScatter.cs` (Codex lane) becomes per-tile scatter driven by `WorldPlan` and spawns
   Tier A/B `Destructible` prefabs instead of decorative primitives (AGENTS.md request).
5. Add `SurfaceTag` to all world colliders; `vehicle-feel.md` F3 reads it.
6. Author the first destructible set in Blender (fence, pole, hay bale, silo, barn — intact + fractured).
7. ArtTestBuilder targets the streamed world once it exists.

## Validation Criteria
1. **WebGL spike:** walking the truck across tile boundaries at boost speed produces no frame > 50 ms
   and no GC spike > 5 ms over a 3-minute drive; physics step ≤ 4 ms. (Fail → 1 km fallback.)
2. Same seed produces byte-identical WorldPlan and tile heights (EditMode test).
3. No visible seams: height delta at shared tile edges = 0 (EditMode test over all 16 × 16 borders).
4. A barn destroyed, streamed out, and streamed back in is still destroyed with its debris.
5. An EF3 passing over an unloaded farmstead leaves it Broken when the player arrives.
6. Worst case (EF5 through a 3-building POI + player crash): dynamic debris never exceeds the
   platform cap; frame time stays within budget on Steam Deck.
7. Wheel on each surface reports the table's grip/drag (PlayMode test per SurfaceType).
8. Playtest: players use terrain (ditches, embankments, debris ramps) for airtime unprompted.

## Related Decisions
- ADR-0001 — Phaser → Unity 3D pivot (platforms, WebGL target)
- ADR-0003 — Art direction: stylized world, retro lens (low-poly flat-shaded tiles, fog as ring edge)
- ADR-0005 — Raycast vehicle architecture (planned; consumes `SurfaceTag`, `Destructible.Mass`)
- `design/gdd/vehicle-feel.md` — F3 SurfaceGrip, E13 mass scaling, F11/F12 wind (shared with debris)
- `design/gdd/event-system.md` — Ram & Unblock obstacles, Structure Gap, Twister Jump Ramp
- `design/gdd/session-modes.md` — Chase mode storm front across the 2 km map
- `design/vision/vision-1.0.md` — Modular Biome Hybrid, Heartland biome
