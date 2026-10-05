using UnityEngine;

/// <summary>
/// Pure camera math for the chase camera and Storm Cam (vehicle-feel.md Core Rule 8, F14).
/// Angles are world yaw in degrees (Unity convention: positive turns right, viewed from above).
/// </summary>
public static class ChaseCameraMath
{
    /// <summary>Cinemachine's damping convention: a damp time is the time to cover 99 % of the gap.</summary>
    private const float NegligibleResidualLog = 4.60517f; // -ln(0.01)

    /// <summary>
    /// Yaw the camera rests behind: the direction of travel when moving forward faster than
    /// <paramref name="minSpeed"/>, otherwise the truck's facing. Moving backward never flips the camera.
    /// Keeps <paramref name="previousYaw"/> when the facing is near vertical (tossed, upended).
    /// </summary>
    public static float RestYaw(Vector3 velocity, Vector3 facing, float previousYaw, float minSpeed)
    {
        Vector3 flatFacing = Flat(facing);
        Vector3 flatVelocity = Flat(velocity);
        bool movingForward = flatVelocity.magnitude >= minSpeed
                             && (flatFacing.sqrMagnitude < 1e-6f || Vector3.Dot(flatVelocity, flatFacing) > 0f);
        if (movingForward) return Yaw(flatVelocity);
        if (flatFacing.magnitude < 0.3f) return previousYaw;
        return Yaw(flatFacing);
    }

    /// <summary>
    /// F14 Storm Cam offset framing: θ_off = clamp(Lag · v_perp / v_top + Sway · sin(0.7 t), ±MaxOffset).
    /// Positive v_perp is the truck moving right across the camera→funnel line; the camera lags right.
    /// </summary>
    public static float StormCamOffset(float vPerp, float vTop, float time, float lag, float sway, float maxOffset)
    {
        float lagTerm = vTop > 0f ? lag * (vPerp / vTop) : 0f;
        return Mathf.Clamp(lagTerm + sway * Mathf.Sin(0.7f * time), -maxOffset, maxOffset);
    }

    /// <summary>Signed speed across the camera→funnel line (positive = moving to the right of the line).</summary>
    public static float PerpendicularSpeed(Vector3 velocity, Vector3 cameraToFunnel)
    {
        Vector3 line = Flat(cameraToFunnel);
        if (line.sqrMagnitude < 1e-6f) return 0f;
        Vector3 right = Vector3.Cross(Vector3.up, line.normalized);
        return Vector3.Dot(Flat(velocity), right);
    }

    /// <summary>
    /// Storm Cam target switch rule (E11): a new target replaces the current one only when it is closer
    /// than <paramref name="switchRatio"/> × the current distance (0.75 = at least 25 % closer).
    /// </summary>
    public static bool ShouldSwitchTarget(float currentDistance, float candidateDistance, float switchRatio)
    {
        return candidateDistance < currentDistance * switchRatio;
    }

    /// <summary>Damps <paramref name="current"/> toward <paramref name="target"/> along the shortest arc.</summary>
    public static float DampAngle(float current, float target, float dampTime, float deltaTime)
    {
        if (dampTime <= 0f || deltaTime < 0f) return target;
        float delta = Mathf.DeltaAngle(current, target);
        float k = 1f - Mathf.Exp(-NegligibleResidualLog * deltaTime / dampTime);
        return current + delta * k;
    }

    /// <summary>Damps a scalar the same way as <see cref="DampAngle"/>.</summary>
    public static float Damp(float current, float target, float dampTime, float deltaTime)
    {
        if (dampTime <= 0f || deltaTime < 0f) return target;
        float k = 1f - Mathf.Exp(-NegligibleResidualLog * deltaTime / dampTime);
        return current + (target - current) * k;
    }

    /// <summary>Unsigned angle in degrees between two directions after flattening both onto the ground.</summary>
    public static float FlatAngle(Vector3 forward, Vector3 toTarget)
    {
        Vector3 f = Flat(forward);
        Vector3 t = Flat(toTarget);
        if (f.sqrMagnitude < 1e-6f || t.sqrMagnitude < 1e-6f) return 180f;
        return Vector3.Angle(f, t);
    }

    /// <summary>World yaw of a direction (0 = +Z).</summary>
    /// <summary>
    /// S9-02a drift framing: leans the chase yaw from the direction of travel toward the truck's facing, by up to
    /// <paramref name="blend"/>, scaled by the slip between them (full lean at <paramref name="fullAtDeg"/>). Grip
    /// driving (no slip) is unchanged; a drift shows where the nose points as well as where the truck is going.
    /// </summary>
    public static float SlideYaw(float travelYaw, float facingYaw, float blend, float fullAtDeg)
    {
        float slip = Mathf.DeltaAngle(travelYaw, facingYaw);
        float weight = Mathf.Clamp01(blend) * Mathf.Clamp01(Mathf.Abs(slip) / Mathf.Max(1f, fullAtDeg));
        return travelYaw + slip * weight;
    }

    public static float Yaw(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

    /// <summary>The direction projected onto the ground plane (not normalized).</summary>
    public static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
