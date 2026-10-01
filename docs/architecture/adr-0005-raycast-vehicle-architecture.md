# ADR-0005: Raycast Vehicle Architecture

## Status
Accepted (Andy, 2026-10-01)

## Date
2026-10-01

## Last Verified
2026-10-01

## Decision Makers
Andy Styx (decisions: facade, input abstraction, data split, platform tiers); Claude (draft);
unity-specialist (engine validation)

## Summary
The approved `vehicle-feel.md` model (raycast suspension, friction-circle grip, verbs, wind force,
impacts) needs a structure that is unit-testable, fits the 0.3 ms PC / 0.8 ms WebGL physics budget,
and replaces the 0.4 arcade truck without breaking its consumers. Decided: a pure C# `VehicleModel`
owns every formula and the state machine; a thin `PlayerVehicle` MonoBehaviour (same name and public
facade as 0.4) gathers wheel contacts, feeds the model, and applies its forces through Rigidbody
force APIs at a fixed 50 Hz.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.6 (6000.6.0f1), built-in PhysX (3D) |
| **Domain** | Physics (+ Input) |
| **Knowledge Risk** | HIGH — Unity 6.x is post-cutoff; project reference docs cover 6.3 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/physics.md`, `breaking-changes.md` (solver iterations), `deprecated-apis.md` (direct velocity writes, RaycastAll) |
| **Post-Cutoff APIs Used** | `Rigidbody.linearVelocity` / `angularVelocity` (read-only use), `Rigidbody.linearDamping`/`angularDamping`; `Physics.SphereCast` (non-alloc single-hit form); `ForceMode.Acceleration`/`VelocityChange`. Verified compiling + running in this project: `linearVelocity`, `Physics.simulationMode`/`Physics.Simulate` |
| **Verification Required** | (1) Spring stability at 50 Hz with this project's solver settings (`DynamicsManager.asset`: 6 position / 1 velocity iterations — not the reference doc's 8) for F1 k≈42 kN/m, c≈4.3 kN·s/m; (2) `SphereCast` hit normal/`collider` on MeshCollider tiles (ADR-0004); (3) CCD (`ContinuousDynamic`) cost on WebGL; (4) per-step cost ≤ 0.8 ms in WebGL |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0004 (Accepted) — `SurfaceTag`/`SurfaceTable` surface query, `Destructible.Mass`, debris budget; ADR-0003 (Accepted) — VFX/presentation consume vehicle state, not vice versa |
| **Enables** | Camera/aim work (S7-05), wind/impact/debris work (S7-06), style-scoring system (future) |
| **Blocks** | Sprint 7 S7-03 (vehicle core), S7-04 (verbs), S7-05 (camera/aim), S7-06 (forces/impacts); Codex X7-01–X7-03 consume its events |
| **Ordering Note** | S7-03 implements Model + facade first; S7-04/05/06 extend the model without changing the facade |

## Context

### Problem Statement
0.4's `PlayerVehicle` writes `linearVelocity` directly each step: no suspension, no traction limit, no
air, wind as a velocity offset. `vehicle-feel.md` (approved) specifies ~15 formulas, a 7-state machine,
four new verbs, and a long list of unit/PlayMode acceptance criteria. Putting that math inside a
MonoBehaviour makes it untestable without the editor and the scene; Unity's built-in vehicle
(`WheelCollider`) cannot express the arcade friction circle or the drift rules. The structure has to be
decided before S7-03 code.

### Constraints
- Unity 6.6 PhysX; the project does not use DOTS.
- WebGL is single-threaded: vehicle physics ≤ 0.8 ms/step there (≤ 0.3 ms PC) — `vehicle-feel.md` Acceptance.
- Zero per-step GC allocations (ADR-0004 spike: GC was the WebGL hitch source).
- Two-agent repo: Codex's presentation lane reads `PlayerVehicle.CurrentSpeed/MaxSpeed/CurrentWind/InputEnabled`
  (AGENTS.md contract) — must keep working.
- Coding standards: gameplay values data-driven; public methods unit-testable (DI over singletons).

### Requirements
- Every `vehicle-feel.md` formula (F1–F14) and state rule is implemented in testable code.
- PlayMode tests can drive the vehicle with scripted input (GDD acceptance criteria).
- Identical gameplay physics on every platform (see Platform Tiers).
- Existing systems (RunManager, PhotoTrigger, VehicleHealth, HUD, Codex VFX/audio, SceneWiring,
  ArtTestBuilder) keep compiling and working through the migration.

## Decision

**Split the vehicle into a pure simulation model and a thin engine adapter, behind the existing
`PlayerVehicle` facade, stepping at a fixed 50 Hz with force-based Rigidbody integration.**

1. **`VehicleModel` (pure C#, no `UnityEngine.Object`)** owns all `vehicle-feel.md` math and state:
   per-wheel suspension (F1), combined friction circle (F3), drive/brake/coast/reverse (F2/F2b),
   steering (F4), slip angle (F5), downforce (F6), air control (F7), jump (F8), boost meter + refills +
   exploit gates (F9, E1–E3), impact severity (F10, E12, E13), wind force/lift/toss (F11, F12, F12b),
   archetype derivation (F13), the state machine (Grounded/Sliding/Airborne/Tossed/Upended + Disabled
   overlay, one-wheel hysteresis), and auto-right (E5). Input: a `VehicleStepInput` snapshot (body
   velocities, orientation, per-wheel contacts, wind sample, damage stage, player input, dt). Output: a
   `VehicleStepOutput` (per-wheel force + application point, body force/torque/acceleration, impulses,
   state, events to raise). Uses `UnityEngine.Vector3`/`Mathf` value types only, so it runs in EditMode
   tests with no scene.
2. **`PlayerVehicle` (MonoBehaviour adapter, same name and public facade as 0.4)**: in `FixedUpdate`
   it sphere-casts the four wheels, resolves surfaces, samples wind, builds the step input, calls the
   model, applies the output with `AddForceAtPosition` / `AddForce` / `AddTorque` (acceleration and
   velocity-change modes where the GDD specifies accelerations), and raises `GameEvents`. It never
   writes `linearVelocity` except for the documented respawn/teleport paths.
3. **Fixed 50 Hz** (`Time.fixedDeltaTime = 0.02`, `maximumDeltaTime = 0.1`, Rigidbody interpolation on)
   — `vehicle-feel.md` E7. The model receives `dt` explicitly; it never reads `Time`.
4. **Input via `IVehicleInput`** returning a `VehicleInputFrame` struct. `PlayerInputSource` wraps
   `StormChaserControls` (new Input System); tests/autopilots supply scripted sources.
5. **Data split:** `VehicleData` (per archetype) holds Speed/Armor/Trick stars + identity + wheel
   geometry; one shared `VehicleFeelConfig` ScriptableObject holds every `vehicle-feel.md` Tuning Knob
   and the F13 coefficients. `ArchetypeParams.Derive(stars, config)` (pure) produces the runtime
   parameters. Per-vehicle overrides of tuning knobs are not allowed (GDD ownership rule).
6. **Surfaces** via ADR-0004 `SurfaceTag` on the hit collider, cached in a `Dictionary<Collider,
   SurfaceProperties>` (lookup once per collider, not `GetComponent` per wheel per step). Untagged =
   Grass.
7. **Events** are added to `GameEvents` (Claude lane, additive): `StyleEvent(StyleKind, float amount)`,
   `Landed(float impactSpeed)`, `VehicleImpact(ImpactInfo)`, `Tossed()`. `VehicleHealth` consumes
   `VehicleImpact` instead of polling disaster radii for crash damage (funnel contact damage stays as is).
8. **Platform tiers (rule):** gameplay physics is **identical on every platform** — the model, its
   config, the 50 Hz step, collider radii, and hazard-relevant debris. Only **fidelity** scales by
   platform: VFX/card counts, cosmetic debris counts (ADR-0004: WebGL 60 / Deck 80 / PC 150+), render
   ring (WebGL 5×5, PC up to 7×7), fog/draw distance, shadows, PiP resolution. WebGL is the floor; PC
   gets more spectacle, never different physics. A future `PlatformProfile` asset carries the fidelity
   values; nothing in `VehicleModel` may read it.

### Architecture Diagram

```
 IVehicleInput ──VehicleInputFrame──┐
                                     ▼
 PlayerVehicle (MonoBehaviour, FixedUpdate @50 Hz)
   ├─ 4× Physics.SphereCast (non-alloc) ─▶ WheelContact[4] (+ SurfaceTag cache → SurfaceProperties)
   ├─ DisasterEntity.TotalWindAt / lift query ─▶ WindSample
   ├─ VehicleHealth.Stage ─▶ DamageStage
   │
   ├──▶ VehicleModel.Step(in VehicleStepInput) ──▶ VehicleStepOutput   (pure C#, unit-tested)
   │        uses ArchetypeParams (from VehicleData stars + VehicleFeelConfig)
   │
   ├─ Rigidbody.AddForceAtPosition / AddForce / AddTorque ◀── output forces
   └─ GameEvents.StyleEvent / Landed / VehicleImpact / Tossed ◀── output events
          │
          ├─▶ VehicleHealth (impact damage)   ├─▶ HUD (boost)   └─▶ Codex VFX/audio
 Facade (unchanged): CurrentSpeed, MaxSpeed, CurrentWind, InputEnabled, Data
 Added read-only:    State, SlipAngle, BoostMeter, Wheels (backed by a fixed array; no per-call allocation)
```

### Key Interfaces

```csharp
public interface IVehicleInput { VehicleInputFrame Read(); }

public struct VehicleInputFrame
{
    public float Throttle;       // 0..1
    public float Brake;          // 0..1 (brake / reverse)
    public float Steer;          // -1..1
    public Vector2 Air;          // pitch, yaw (air control)
    public bool Handbrake, JumpPressed, Boost;
}

public enum VehicleState { Grounded, Sliding, Airborne, Tossed, Upended }
public enum ImpactKind { World, Destructible, EventObstacle }
public enum StyleKind { Drift, Airtime, NearMiss }

public struct WheelContact { public bool Grounded; public Vector3 Point, Normal; public float Distance; public SurfaceProperties Surface; public Vector3 PointVelocity; }

public readonly struct ImpactInfo { public readonly float Speed; public readonly int HpLoss; public readonly ImpactKind Kind; public readonly Vector3 Point; }

public sealed class VehicleModel
{
    public VehicleModel(ArchetypeParams p);
    public VehicleState State { get; }
    public bool Disabled { get; }
    public float BoostMeter { get; }
    public float SlipAngleDeg { get; }
    public void Step(in VehicleStepInput input, ref VehicleStepOutput output); // allocation-free
    public static int ImpactHpLoss(float severity, float light, float severe);  // F10
    public static float LiftAcceleration(/* F12 terms */);                       // F12
}

public static class ArchetypeParams { public static ArchetypeParams Derive(Stars s, VehicleFeelConfig c); } // F13

// GameEvents additions (Claude lane, additive)
public static event Action<StyleKind, float> StyleEvent;
public static event Action<float> Landed;
public static event Action<ImpactInfo> VehicleImpact;
public static event Action Tossed;

// PlayerVehicle facade (unchanged names): CurrentSpeed, MaxSpeed, CurrentWind, InputEnabled, Data
// Added: VehicleState State; float SlipAngle; float BoostMeter; IVehicleInput InputSource { set; }
```

### Implementation Guidelines
- `VehicleModel` **must never** reference `MonoBehaviour`, `Rigidbody`, `Time`, `Physics`, or any
  `UnityEngine.Object`; value types (`Vector3`, `Quaternion`, `Mathf`) only.
- `VehicleModel.Step` and `PlayerVehicle.FixedUpdate` **must not allocate** (no LINQ, lambdas,
  closures, `new` of reference types, string formatting) — verified by a GC-alloc PlayMode test.
- Wheel queries **must** use the single-hit `Physics.SphereCast(origin, radius, dir, out hit, maxDistance,
  layerMask, QueryTriggerInteraction.Ignore)` overload; **never** `RaycastAll`/`SphereCastAll`. The mask
  **must** exclude the vehicle's own layer; the origin is the suspension mount (above the body surface).
- A cast that starts overlapping (`hit.distance <= ε`) **must** be treated as fully compressed and
  **must** reuse the last valid contact normal/point (tile seams and debris trigger this).
- Contact normals **must** be smoothed per wheel (EMA + max change per step): MeshCollider hit normals
  are raw triangle normals and jump at edges.
- **Force-mode rule:** suspension and tire forces use `AddForceAtPosition(..., ForceMode.Force)`; wind,
  downforce, and other acceleration-defined terms use `ForceMode.Acceleration` at the center of mass;
  jump/toss use `VelocityChange`; air control uses `AddTorque(..., ForceMode.Acceleration)` (F7).
- The Rigidbody **must** set `automaticCenterOfMass = false` (low `centerOfMass` from `VehicleData`) and
  an explicit `inertiaTensor` (`automaticInertiaTensor = false`), so roll/tip/auto-right don't vary by
  prefab children; `sleepThreshold = 0`; explicit `maxLinearVelocity` / `maxAngularVelocity`.
- Suspension impulse clamping **must** always be on, and the model **must** assert the 50 Hz stability
  bounds in tests: `c·dt / (M/4) < 1` and `k·dt² / (M/4) < 0.5`.
- Teleport/respawn **must** set `rb.position`/`rotation`, reset velocities, and re-sync interpolation;
  the camera (Cinemachine) follows the interpolated `transform`, never `rb.position`.
- The surface cache **must** be invalidated when ADR-0004's `TileStreamer` recycles a tile (pooled
  colliders get reused).
- Forces **must** be applied via `AddForceAtPosition`/`AddForce`/`AddTorque`. Direct
  `linearVelocity` writes are allowed **only** for respawn (E8) and test teleports.
- The Rigidbody **must** use `CollisionDetectionMode.ContinuousDynamic` (E6) and interpolation.
- Every tuning value **must** come from `VehicleFeelConfig` or derived `ArchetypeParams`; no literals
  in the model beyond physical constants.
- Gameplay code **must never** branch on platform; fidelity values come from a `PlatformProfile`
  that `VehicleModel` cannot see.
- `PlayerVehicle`'s 0.4 public members **must** keep their names and semantics until the Migration
  Plan's step 6 removes `ApplyKnockback`.

## Alternatives Considered

### Alternative 1: Monolithic MonoBehaviour
- **Description**: All formulas inline in `PlayerVehicle.FixedUpdate`, as in 0.4.
- **Pros**: Fewest files; easy to read top to bottom at first.
- **Cons**: The GDD's ~30 unit criteria would need scene-based tests; formula bugs hide behind physics noise.
- **Rejection Reason**: Violates the "public methods unit-testable" standard and the verification-first workflow.

### Alternative 2: Unity `WheelCollider`
- **Description**: Built-in realistic wheel/tire model.
- **Pros**: No custom suspension code.
- **Cons**: Hard to bend into arcade feel; jitters at speed; drift fights its tire model; can't express F3/F12b.
- **Rejection Reason**: Already rejected in `vehicle-feel.md` (Section C decision, 2026-10-01).

### Alternative 3: DOTS / Unity Physics package
- **Description**: ECS vehicle on the stateless Unity Physics package.
- **Pros**: Determinism, Burst performance.
- **Cons**: The project is MonoBehaviour-based; mixing worlds adds sync cost; WebGL/Burst support and
  team familiarity are risks; ADR-0004 terrain is PhysX colliders.
- **Rejection Reason**: Cost far exceeds benefit for a single player vehicle.

## Consequences

### Positive
- Every GDD formula gets fast EditMode tests; PlayMode tests can drive the truck with scripted input.
- Zero consumer churn: scenes, builders, VehicleHealth, HUD, and Codex's lane keep the same facade.
- Feel tuning is data-only (`VehicleFeelConfig` + stars); archetypes stay comparable.
- Platform-tier rule keeps runs fair across Steam/Deck/WebGL while letting PC look bigger.

### Negative
- Two layers to keep in sync (snapshot in, forces out); a small struct-marshalling cost per step.
- Force-based integration is harder to reason about than velocity writes; tuning takes longer.
- `VehicleHealth` changes from polling to event-driven crash damage (one migration step).

## Risks
- **Spring instability at 50 Hz** (stiff k with Unity 6 solver defaults) → explicit-force F1 with
  damping; verify Pickup sag test (GDD F1 acceptance); fall back to clamping per-step suspension impulse.
- **WebGL step cost > 0.8 ms** → profile in S7-03; sphere-cast count is fixed (4); no allocations.
- **Grip jitter** from explicit lateral forces → F3's one-step velocity-cancel clamp (already in the GDD).
- **ADR-0004 G1 condition (one-off 7.3 ms physics step)** → measured again in S7-06 with vehicle + debris.
- **Engine-knowledge gap** (Unity 6.6 post-cutoff) → items in Verification Required are explicit test tasks in S7-03.
- **Grip/suspension jitter from MeshCollider triangle normals** → per-wheel normal smoothing (guideline).
- **CCD ghost collisions / snags on tile seams** → `CookForFasterSimulation` cooking (already in ADR-0004
  spike), contact offset 0.01 (project), measure in S7-03 on streamed terrain.
- **Catch-up spiral on WebGL hitches** (`maximumDeltaTime` 0.1 = up to 5 steps) → consider 0.06 on WebGL
  via the platform profile (a time setting, not gameplay physics).
- **Sleeping vehicle ignoring small wind forces** → `sleepThreshold = 0` (guideline).
- **Default `maxAngularVelocity` (7 rad/s)** capping air control / toss spin → set explicitly from config.
- Engine validation: unity-specialist reviewed 2026-10-01 — OK with notes, no blocking issues; APIs
  confirmed in 6000.6.0f1 engine XML docs; PhysX behavioural notes are expert guidance, not verified.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| vehicle-feel.md | F1–F14 formulas, state table, E1–E15 | Implemented in pure `VehicleModel`, each unit-tested |
| vehicle-feel.md | PlayMode acceptance (accel, slide, ledge, wall, toss, Storm Cam) | `IVehicleInput` scripted sources drive the real scene |
| vehicle-feel.md | Perf ≤ 0.3 ms PC / ≤ 0.8 ms WebGL per step | 4 non-alloc casts, allocation-free model, fixed 50 Hz |
| vehicle-feel.md | Tuning knobs data-driven; stars own archetype feel | `VehicleFeelConfig` + `ArchetypeParams.Derive(stars)` |
| vehicle-damage.md | Impact severity → 1/2 HP; Damaged steer ×0.75; Critical momentum-only | `ImpactInfo` via `GameEvents.VehicleImpact`; model reads damage stage |
| event-system.md | Slide duration, airborne + airtime; ram obstacles exempt from F10 | `StyleEvent`, `VehicleState`; `ImpactKind.EventObstacle` |
| photo-scoring.md | Camera-forward aim | Out of the vehicle's scope; S7-05 provides the aim source (vehicle exposes state only) |

## Performance Implications
- **CPU**: ≤ 0.3 ms/step PC, ≤ 0.8 ms/step WebGL (4 sphere-casts + model math + force calls).
- **Memory**: < 10 KB per vehicle (model state, 4 contacts, surface cache); zero per-step allocations.
- **Load Time**: Negligible (config + derive at spawn).
- **Network**: N/A.

## Migration Plan
1. Add `VehicleModel`, `ArchetypeParams`, `VehicleFeelConfig`, input types, and EditMode tests (no scene change).
2. Add `VehicleData` star fields (keep 0.4 fields temporarily; `[FormerlySerializedAs]` where renamed).
3. Replace `PlayerVehicle` internals behind the same facade; add `PlayerInputSource`; remove direct velocity writes.
4. Add `GameEvents` vehicle events; switch `VehicleHealth` crash damage to `VehicleImpact` (keep funnel contact).
5. Update `RunLoopSmokeTests` (teleports remain; knockback assertions change to impact-based); add vehicle PlayMode tests from the GDD.
6. Remove `ApplyKnockback` once no callers remain; post the new interfaces to AGENTS.md for Codex (X7-01–03).
7. Rebuild ArtTest via `ArtTestBuilder` (no builder change expected — facade unchanged).

## Validation Criteria
- All `vehicle-feel.md` Unit and PlayMode acceptance criteria pass.
- GC-alloc PlayMode test: 0 bytes allocated by vehicle code over 600 fixed steps.
- Profiled step ≤ 0.3 ms (dev PC) and ≤ 0.8 ms (WebGL build).
- Existing EditMode/PlayMode suites stay green through each migration step.
- G3 feel playtest (Sprint 7) passes.

## Related
- ADR-0001 — Unity pivot (platforms incl. WebGL)
- ADR-0003 — Art direction (presentation consumes vehicle state)
- ADR-0004 — Destructible tiled world (SurfaceTag, Destructible mass, debris budget, G1 physics condition)
- `design/gdd/vehicle-feel.md` (primary), `vehicle-damage.md`, `event-system.md`, `photo-scoring.md`
