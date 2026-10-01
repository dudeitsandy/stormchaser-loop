using System;
using System.Collections.Generic;

/// <summary>Weighted pick whose weights slide from early-run to late-run values.</summary>
public static class SpawnTable
{
    /// <summary>
    /// Picks an index. Weight_i = lerp(early_i, late_i, progress). Returns -1 if all weights are zero.
    /// </summary>
    /// <param name="progress">0 = run start, 1 = run end.</param>
    /// <param name="roll01">Uniform random in [0,1).</param>
    public static int Pick(IReadOnlyList<float> earlyWeights, IReadOnlyList<float> lateWeights, float progress, float roll01)
    {
        if (earlyWeights.Count != lateWeights.Count)
            throw new ArgumentException("Weight lists must be the same length.");

        float p = progress < 0f ? 0f : progress > 1f ? 1f : progress;
        float total = 0f;
        for (int i = 0; i < earlyWeights.Count; i++)
            total += Weight(earlyWeights[i], lateWeights[i], p);
        if (total <= 0f) return -1;

        float threshold = roll01 * total;
        float cumulative = 0f;
        for (int i = 0; i < earlyWeights.Count; i++)
        {
            cumulative += Weight(earlyWeights[i], lateWeights[i], p);
            if (threshold < cumulative) return i;
        }
        return earlyWeights.Count - 1;
    }

    private static float Weight(float early, float late, float p)
    {
        float w = early + (late - early) * p;
        return w > 0f ? w : 0f;
    }
}
