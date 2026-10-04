/// <summary>
/// The Storm Director's own random stream (storm-director.md Rule 10): PCG32 seeded from the run seed and a
/// stream id, so plans are identical on desktop and WebGL builds of one version and never disturbed by
/// gameplay randomness. Never use UnityEngine.Random, System.Random or Mathf.PerlinNoise in director code.
/// </summary>
public sealed class DirectorRng
{
    private const ulong Multiplier = 6364136223846793005UL;
    private ulong _state;
    private readonly ulong _increment;

    /// <summary>Creates the stream for <paramref name="seed"/> and sub-stream <paramref name="stream"/>.</summary>
    public DirectorRng(ulong seed, ulong stream)
    {
        _increment = (SplitMix(stream ^ 0x9E3779B97F4A7C15UL) << 1) | 1UL;
        _state = 0UL;
        NextUInt();
        _state += SplitMix(seed);
        NextUInt();
    }

    /// <summary>Next 32 random bits.</summary>
    public uint NextUInt()
    {
        ulong old = _state;
        _state = unchecked(old * Multiplier + _increment);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    /// <summary>Uniform float in [0, 1) with 24 bits of precision.</summary>
    public float NextFloat01() => (NextUInt() >> 8) * (1f / 16777216f);

    /// <summary>Uniform float in [min, max).</summary>
    public float Range(float min, float max) => min + (max - min) * NextFloat01();

    /// <summary>Uniform integer in [min, max] (both inclusive), without modulo bias.</summary>
    public int RangeInclusive(int min, int max)
    {
        if (max <= min) return min;
        uint span = (uint)(max - min + 1);
        uint limit = uint.MaxValue - (uint.MaxValue % span);
        uint x;
        do x = NextUInt(); while (x >= limit);
        return min + (int)(x % span);
    }

    /// <summary>Index drawn from <paramref name="weights"/> (non-negative; need not sum to 1).</summary>
    public int Pick(float[] weights)
    {
        float total = 0f;
        foreach (float w in weights) total += w;
        float u = NextFloat01() * total;
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (u < cumulative) return i;
        }
        for (int i = weights.Length - 1; i >= 0; i--)
            if (weights[i] > 0f) return i;
        return 0;
    }

    private static ulong SplitMix(ulong x)
    {
        x = unchecked(x + 0x9E3779B97F4A7C15UL);
        x = unchecked((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL);
        x = unchecked((x ^ (x >> 27)) * 0x94D049BB133111EBUL);
        return x ^ (x >> 31);
    }
}
