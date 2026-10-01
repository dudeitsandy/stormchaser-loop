using UnityEngine;

// Shared vehicle types (ADR-0005). Value types only: VehicleModel must stay free of UnityEngine.Object.

/// <summary>Player intent for one physics step.</summary>
public struct VehicleInputFrame
{
    /// <summary>0..1 forward throttle.</summary>
    public float Throttle;
    /// <summary>0..1 brake; engages reverse below 1 m/s.</summary>
    public float Brake;
    /// <summary>-1..1 steering.</summary>
    public float Steer;
    /// <summary>Air control: x = yaw, y = pitch (roll when Handbrake held — vehicle-feel.md Core Rule 3).</summary>
    public Vector2 Air;
    public bool Handbrake;
    public bool JumpPressed;
    public bool Boost;
}

/// <summary>Source of player intent; real input, scripted tests, and autopilots implement it.</summary>
public interface IVehicleInput
{
    VehicleInputFrame Read();
}

/// <summary>vehicle-feel.md States and Transitions (Disabled is an overlay flag, not a state).</summary>
public enum VehicleState { Grounded, Sliding, Airborne, Tossed, Upended }

public enum ImpactKind { World, Destructible, EventObstacle }

public enum StyleKind { Drift, Airtime, NearMiss }

/// <summary>Damage stage from vehicle-damage.md, as seen by the driving model.</summary>
public enum DamageStage { Healthy, Damaged, Critical }

/// <summary>Result of one wheel's ground query, in world space.</summary>
public struct WheelContact
{
    public bool Grounded;
    /// <summary>Distance from the wheel anchor down to the ground (m).</summary>
    public float GroundDistance;
    public Vector3 Point;
    public Vector3 Normal;
    /// <summary>World velocity of the body at the contact point.</summary>
    public Vector3 PointVelocity;
    public float SurfaceGrip;
    public float SurfaceDrag;
}

/// <summary>A collision as reported to VehicleHealth and presentation.</summary>
public readonly struct ImpactInfo
{
    public readonly float Speed;
    public readonly int HpLoss;
    public readonly ImpactKind Kind;
    public readonly Vector3 Point;

    public ImpactInfo(float speed, int hpLoss, ImpactKind kind, Vector3 point)
    {
        Speed = speed;
        HpLoss = hpLoss;
        Kind = kind;
        Point = point;
    }
}

/// <summary>Per-archetype layout of wheels and body (vehicle-local space, +Z forward, origin = body origin).</summary>
[System.Serializable]
public struct WheelLayout
{
    public float HalfTrack;
    public float FrontAxleZ;
    public float RearAxleZ;
    /// <summary>Anchor height of the suspension mounts in local space.</summary>
    public float AnchorY;
    /// <summary>Sphere-cast radius for wheel queries (vehicle-feel.md Core Rule 1).</summary>
    public float CastRadius;
    public float WheelRadius;
    /// <summary>Low center of mass, local space.</summary>
    public Vector3 CenterOfMass;
    /// <summary>Body box used for the collider and the explicit inertia tensor.</summary>
    public Vector3 BodySize;
    public Vector3 BodyCenter;

    /// <summary>Pickup layout: rides with its origin ≈ 0.5 m above ground, matching the 0.4 visuals.</summary>
    public static WheelLayout Pickup => new WheelLayout
    {
        HalfTrack = 0.52f,
        FrontAxleZ = 0.62f,
        RearAxleZ = -0.6f,
        AnchorY = 0f,
        CastRadius = 0.15f,
        WheelRadius = 0.22f,
        CenterOfMass = new Vector3(0f, -0.15f, 0.05f),
        BodySize = new Vector3(1.0f, 0.5f, 1.85f),
        BodyCenter = new Vector3(0f, 0.2f, 0f),
    };
}
