using System;

/// <summary>Pure bounded density mappings for cosmetic rain, independent of photo scoring.</summary>
public static class RainLevels
{
    private static float Clamp(float value) => float.IsNaN(value) ? 0f : Math.Max(0f, Math.Min(1f, value));
    /// <summary>Active camera streaks in a hard-capped 300-quad pool; dry skies emit none.</summary>
    public static int StreakCount(float storminess, int capacity) =>
        (int)Math.Floor(Clamp(storminess) * Math.Max(0, Math.Min(300, capacity)));
    /// <summary>Cell intensity controls the opacity of its translucent rain curtain.</summary>
    public static float CurtainOpacity(float intensity) => Clamp(intensity) * 0.24f;
    /// <summary>Viewfinder-only water stays subtle enough to retain subject visibility.</summary>
    public static float LensOpacity(float storminess) => Clamp(storminess) * 0.38f;
}
