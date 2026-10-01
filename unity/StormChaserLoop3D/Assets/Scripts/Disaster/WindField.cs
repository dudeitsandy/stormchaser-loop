using UnityEngine;

/// <summary>Pure wind math for vortex-style disasters. Output is a velocity (units/sec) on the XZ plane.</summary>
public static class WindField
{
    /// <summary>
    /// Wind at <paramref name="offsetFromCenter"/> for a vortex of the given radius.
    /// Falloff is quadratic: (1 - d/R)². Inflow pulls toward the center; swirl is counter-clockwise from above.
    /// </summary>
    public static Vector3 Vortex(Vector3 offsetFromCenter, float radius, float inflowSpeed, float swirlSpeed)
    {
        offsetFromCenter.y = 0f;
        float d = offsetFromCenter.magnitude;
        if (radius <= 0f || d >= radius || d < 0.001f) return Vector3.zero;

        float f = 1f - d / radius;
        f *= f;
        Vector3 outward = offsetFromCenter / d;
        Vector3 tangent = Vector3.Cross(Vector3.up, outward);
        return (-outward * inflowSpeed + tangent * swirlSpeed) * f;
    }
}
