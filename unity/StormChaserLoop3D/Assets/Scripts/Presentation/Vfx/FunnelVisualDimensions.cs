using System;

/// <summary>Independent visual dimensions: sky clearance does not shrink with the storm's EF width.</summary>
public static class FunnelVisualDimensions
{
    /// <summary>Stable visual-only variation from a spawn position; never consumes gameplay randomness.</summary>
    public static float Variation(float x, float z, uint salt)
    {
        unchecked
        {
            uint hash = (uint)BitConverter.SingleToInt32Bits(x) ^ ((uint)BitConverter.SingleToInt32Bits(z) * 16777619u) ^ salt;
            hash ^= hash >> 16; hash *= 0x7feb352du; hash ^= hash >> 15; hash *= 0x846ca68bu; hash ^= hash >> 16;
            return (hash & 0xffffffu) / 16777215f;
        }
    }
    /// <summary>Cloud centre keeps its lowest edge above the configured clearance.</summary>
    public static float CloudHeight(float clearance, float thickness, float variationRange, float sample)
        => Math.Max(1f, clearance) + Math.Max(0f, thickness) * 0.5f + Math.Max(0f, variationRange) * Math.Max(0f, Math.Min(1f, sample));
    /// <summary>Radius follows ConeScale = damage radius / 4.3, with independent shape variation. Legacy tuning 2 is neutral.</summary>
    public static float CrownRadius(float widthScale, float widthTuning, float sample)
    {
        return Math.Max(0.1f, widthScale) * 4.3f * Math.Max(0.1f, widthTuning) * 0.5f
            * (0.9f + 0.2f * Math.Max(0f, Math.Min(1f, sample)));
    }
    /// <summary>Projected radius of an elliptical funnel; keeps depth variation bounded and honest across cameras.</summary>
    public static float ProjectedRadius(float radius, float depthRatio, float angleRadians)
    {
        double cos = Math.Cos(angleRadians), sin = Math.Sin(angleRadians);
        double depth = Math.Max(0.5f, Math.Min(1.5f, depthRatio));
        return radius * (float)Math.Sqrt(cos * cos + depth * depth * sin * sin);
    }
}
