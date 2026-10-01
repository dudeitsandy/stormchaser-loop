using UnityEngine;

/// <summary>ADR-0004 §4 surface types (feed vehicle-feel.md F3 SurfaceGrip and surface drag).</summary>
public enum SurfaceType { Asphalt, DirtRoad, Grass, Gravel, Mud, Debris }

/// <summary>Grip multiplier and rolling drag (m/s²) for a surface.</summary>
public readonly struct SurfaceProperties
{
    public readonly float Grip;
    public readonly float Drag;

    public SurfaceProperties(float grip, float drag)
    {
        Grip = grip;
        Drag = drag;
    }
}

/// <summary>ADR-0004 §4 table. Untagged colliders are Grass.</summary>
public static class SurfaceTable
{
    public const SurfaceType Default = SurfaceType.Grass;

    public static SurfaceProperties Get(SurfaceType type)
    {
        switch (type)
        {
            case SurfaceType.Asphalt: return new SurfaceProperties(1.00f, 0.0f);
            case SurfaceType.DirtRoad: return new SurfaceProperties(0.85f, 0.3f);
            case SurfaceType.Gravel: return new SurfaceProperties(0.70f, 0.8f);
            case SurfaceType.Mud: return new SurfaceProperties(0.45f, 2.5f);
            case SurfaceType.Debris: return new SurfaceProperties(0.60f, 1.0f);
            default: return new SurfaceProperties(0.75f, 0.6f); // Grass
        }
    }
}

/// <summary>Marks a world collider's surface type (ADR-0004). Read by the vehicle's wheel queries.</summary>
public sealed class SurfaceTag : MonoBehaviour
{
    public SurfaceType Type = SurfaceTable.Default;
}
