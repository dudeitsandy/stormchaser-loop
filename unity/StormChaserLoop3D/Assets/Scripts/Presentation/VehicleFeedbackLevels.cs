using UnityEngine;

/// <summary>Shared bounded feedback levels so vehicle sound and cards agree about severity.</summary>
public static class VehicleFeedbackLevels
{
    /// <summary>Skid strength; only sliding with ground contact and useful motion can squeal or smoke.</summary>
    public static float Skid(VehicleState state, int groundedWheels, float speed, float slipAngle)
    {
        if (state != VehicleState.Sliding || groundedWheels < 2) return 0f;
        return Mathf.Clamp01((Mathf.Abs(slipAngle) - 5f) / 30f) * Mathf.Clamp01(Mathf.Abs(speed) / 6f);
    }

    /// <summary>Landing strength above the small-touchdown threshold; accepts either velocity sign.</summary>
    public static float Landing(float verticalSpeed, float threshold)
    {
        float speed = Mathf.Abs(verticalSpeed);
        return speed < Mathf.Max(0f, threshold) ? 0f : Mathf.Clamp01(speed / 12f);
    }

    /// <summary>Impact severity from reported speed and HP loss; ignores slow contact chatter.</summary>
    public static float Impact(float speed, int hpLoss)
    {
        if (speed < 2f) return 0f;
        return Mathf.Clamp01(speed / 20f + Mathf.Max(0, hpLoss) * 0.1f);
    }
}
