using UnityEngine;

/// <summary>
/// vehicle-feel.md F10 collision side: contact speed, E12 (one impact per physics step), E13 (light
/// destructibles) and the event-obstacle exemption. HP thresholds and landing severity stay in
/// <see cref="VehicleModel.ImpactHpLoss"/> / <see cref="VehicleModel.LandingSeverity"/>.
/// </summary>
public static class ImpactSeverity
{
    /// <summary>F10 HP for an impact of <paramref name="kind"/>; event obstacles are exempt (event-system.md ramming).</summary>
    public static int HpLoss(float speed, float light, float severe, ImpactKind kind) =>
        kind == ImpactKind.EventObstacle ? 0 : VehicleModel.ImpactHpLoss(speed, light, severe);

    /// <summary>F10 collision speed: relative velocity along the contact normal, s = |v_rel · n|.</summary>
    public static float CollisionSpeed(Vector3 relativeVelocity, Vector3 normal) =>
        Mathf.Abs(Vector3.Dot(relativeVelocity, normal.normalized));

    /// <summary>E13: severity scale min(1, m_other / (0.5 · M)); static geometry counts as full mass.</summary>
    public static float MassScale(float otherMass, float truckMass, bool isStatic) =>
        isStatic ? 1f : Mathf.Clamp01(otherMass / Mathf.Max(1e-3f, 0.5f * truckMass));

    /// <summary>E12: collects one physics step's impacts and keeps only the most severe.</summary>
    public sealed class StepMax
    {
        public bool Has { get; private set; }
        public float Speed { get; private set; }
        public ImpactKind Kind { get; private set; }
        public Vector3 Point { get; private set; }

        public void Offer(float speed, ImpactKind kind, Vector3 point)
        {
            if (Has && speed <= Speed) return;
            Has = true;
            Speed = speed;
            Kind = kind;
            Point = point;
        }

        public void Clear()
        {
            Has = false;
            Speed = 0f;
        }
    }
}
