/// <summary>Forming → Mature → Dissipating arc for a tornado. Pure math, no Unity dependencies.</summary>
public class TornadoLifecycle
{
    public enum Phase { Forming, Mature, Dissipating, Done }

    public float FormSeconds { get; }
    public float MatureSeconds { get; }
    public float DissipateSeconds { get; }
    public float TotalSeconds => FormSeconds + MatureSeconds + DissipateSeconds;

    public TornadoLifecycle(float formSeconds, float matureSeconds, float dissipateSeconds)
    {
        FormSeconds = formSeconds > 0f ? formSeconds : 0.01f;
        MatureSeconds = matureSeconds > 0f ? matureSeconds : 0f;
        DissipateSeconds = dissipateSeconds > 0f ? dissipateSeconds : 0.01f;
    }

    public Phase GetPhase(float age)
    {
        if (age < FormSeconds) return Phase.Forming;
        if (age < FormSeconds + MatureSeconds) return Phase.Mature;
        if (age < TotalSeconds) return Phase.Dissipating;
        return Phase.Done;
    }

    /// <summary>0 at birth, ramps linearly to 1 while forming, holds at 1, ramps to 0 while dissipating.</summary>
    public float GetIntensity(float age)
    {
        switch (GetPhase(age))
        {
            case Phase.Forming: return age < 0f ? 0f : age / FormSeconds;
            case Phase.Mature: return 1f;
            case Phase.Dissipating: return 1f - (age - FormSeconds - MatureSeconds) / DissipateSeconds;
            default: return 0f;
        }
    }
}
