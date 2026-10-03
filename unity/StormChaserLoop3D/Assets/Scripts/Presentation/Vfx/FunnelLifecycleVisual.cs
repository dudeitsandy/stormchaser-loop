using System;

/// <summary>Sky-anchored funnel dimensions normalized to mature height and width.</summary>
public readonly struct FunnelLifecycleVisual
{
    /// <summary>Condensation length measured downward from the cloud base.</summary>
    public float Length { get; }
    /// <summary>Width relative to the mature funnel.</summary>
    public float Width { get; }
    /// <summary>Rope tilt in degrees; the upper endpoint stays anchored.</summary>
    public float Tilt { get; }
    /// <summary>Ground particles are allowed only during damaging mature contact.</summary>
    public bool GroundContact { get; }

    private FunnelLifecycleVisual(float length, float width, float tilt, bool groundContact)
    { Length = length; Width = width; Tilt = tilt; GroundContact = groundContact; }

    /// <summary>Maps authoritative phase/intensity without changing gameplay lifecycle or damage.</summary>
    public static FunnelLifecycleVisual Evaluate(TornadoLifecycle.Phase phase, float intensity, bool touchedDown, bool damaging)
    {
        float i = float.IsNaN(intensity) ? 0f : Math.Max(0f, Math.Min(1f, intensity));
        switch (phase)
        {
            case TornadoLifecycle.Phase.Forming:
                return new FunnelLifecycleVisual(0.6f * i, 0.25f + 0.45f * i, 0f, false);
            case TornadoLifecycle.Phase.Mature:
                return new FunnelLifecycleVisual(1f, 1f, 0f, damaging);
            case TornadoLifecycle.Phase.Dissipating:
                if (!touchedDown) return new FunnelLifecycleVisual(0.6f * i, 0.25f + 0.45f * i, 0f, false);
                // Thin first, then pull the rope into its cloud; no ground skirt during retraction.
                float thinning = Math.Max(0f, (i - 0.65f) / 0.35f);
                return new FunnelLifecycleVisual(Math.Min(1f, i / 0.65f), 0.25f + 0.75f * thinning,
                    30f * (1f - i), false);
            default:
                return new FunnelLifecycleVisual(0f, 0f, 0f, false);
        }
    }
}
