using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What the forecast says a cell is doing.</summary>
public enum ForecastStatus { Forming, OnGround, RopingOut }

/// <summary>One forecast panel row (storm-director.md UI Requirements).</summary>
public struct ForecastRow
{
    public int CellId;
    public StormCellRole Role;
    /// <summary>8-point compass bearing from the player.</summary>
    public string Bearing;
    /// <summary>Estimated EF 0–5 (±1 far away, exact inside 150 m).</summary>
    public int ShownEf;
    /// <summary>True while the estimate can still be wrong (beyond the exact range): the HUD shows "~EF3".</summary>
    public bool Uncertain;
    /// <summary>Player-to-cell distance in metres.</summary>
    public float Distance;
    public ForecastStatus Status;
    /// <summary>Estimated seconds to the next phase (peak while Forming, rope-out while on the ground), or −1.</summary>
    public float EtaSeconds;
}

/// <summary>
/// storm-director.md F4 forecast estimate: σ(d) = 0.7 · clamp01((d − 150) / 650);
/// EF_shown = clamp(round(EF + clamp(b_i · σ, −1, 1)), 0, 5) with 10 m hysteresis; ETA_shown =
/// max(0, t · (1 + clamp(b_t, ±1.5) · (0.033 + 0.267 g))) rounded to 5 s. Biases are seeded per cell and fixed,
/// so the readout never flickers.
/// </summary>
public sealed class StormForecast
{
    public const float ExactRange = 150f;
    public const float MaxErrorRange = 800f;
    public const float SigmaMax = 0.7f;
    public const float Hysteresis = 10f;
    /// <summary>RNG sub-stream base for forecast biases (each cell adds its id).</summary>
    public const ulong ForecastStreamBase = 1000;

    private readonly ulong _seed;
    private readonly Dictionary<int, CellMemory> _memory = new Dictionary<int, CellMemory>();

    private struct CellMemory
    {
        public float EfBias, EtaBias;
        public int Shown;
        public bool HasShown;
    }

    public StormForecast(long runSeed) => _seed = unchecked((ulong)runSeed);

    /// <summary>g(d) = clamp01((d − 150) / 650).</summary>
    public static float G(float d) => Mathf.Clamp01((d - ExactRange) / (MaxErrorRange - ExactRange));

    /// <summary>σ(d) = 0.7 · g(d).</summary>
    public static float Sigma(float d) => SigmaMax * G(d);

    /// <summary>Raw estimate without hysteresis.</summary>
    public static int RawShownEf(int ef, float efBias, float d) =>
        Mathf.Clamp(Mathf.RoundToInt(ef + Mathf.Clamp(efBias * Sigma(d), -1f, 1f)), 0, 5);

    /// <summary>ETA estimate in seconds, rounded to 5 s, never negative.</summary>
    public static float ShownEta(float trueSeconds, float etaBias, float d)
    {
        float factor = 1f + Mathf.Clamp(etaBias, -1.5f, 1.5f) * (0.033f + 0.267f * G(d));
        float eta = Mathf.Max(0f, trueSeconds * factor);
        return Mathf.Round(eta / 5f) * 5f;
    }

    /// <summary>Biases for a cell: b_i ~ N(0, 1) clamped ±3, b_t ~ N(0, 1) (clamped ±1.5 at use).</summary>
    public void BiasesFor(int cellId, out float efBias, out float etaBias)
    {
        CellMemory m = Memory(cellId);
        efBias = m.EfBias;
        etaBias = m.EtaBias;
    }

    /// <summary>
    /// Shown EF with hysteresis: a new value is accepted only once the estimate 10 m either side of
    /// <paramref name="d"/> agrees with it, so d oscillating across a flip boundary never flickers the readout.
    /// </summary>
    public int ShownEf(int cellId, int trueEf, float d)
    {
        CellMemory m = Memory(cellId);
        int raw = RawShownEf(trueEf, m.EfBias, d);
        if (!m.HasShown)
        {
            m.Shown = raw;
            m.HasShown = true;
        }
        else if (raw != m.Shown
                 && RawShownEf(trueEf, m.EfBias, Mathf.Max(0f, d - Hysteresis)) == raw
                 && RawShownEf(trueEf, m.EfBias, d + Hysteresis) == raw)
        {
            m.Shown = raw;
        }
        _memory[cellId] = m;
        return m.Shown;
    }

    /// <summary>Forecast row for a live cell as seen from <paramref name="player"/>.</summary>
    public ForecastRow Evaluate(LiveStormCell cell, Vector3 player)
    {
        Vector3 pos = cell.Position;
        Vector3 flat = new Vector3(pos.x - player.x, 0f, pos.z - player.z);
        float d = flat.magnitude;
        PlannedCell p = cell.Cell;
        CellMemory m = Memory(p.Id);

        var row = new ForecastRow
        {
            CellId = p.Id,
            Role = p.Role,
            Bearing = StormTelegraph.Bearing(player, pos),
            ShownEf = ShownEf(p.Id, p.Ef, d),
            Uncertain = d > ExactRange,
            Distance = d,
            EtaSeconds = -1f,
        };
        switch (cell.Phase)
        {
            case StormScale.Phase.Forming:
                row.Status = ForecastStatus.Forming;
                row.EtaSeconds = ShownEta(Mathf.Max(0f, p.Form - cell.Age), m.EtaBias, d);
                break;
            case StormScale.Phase.Mature:
                row.Status = ForecastStatus.OnGround;
                row.EtaSeconds = ShownEta(Mathf.Max(0f, p.Form + p.Mature - cell.Age), m.EtaBias, d);
                break;
            default:
                row.Status = ForecastStatus.RopingOut;
                break;
        }
        return row;
    }

    private CellMemory Memory(int cellId)
    {
        if (_memory.TryGetValue(cellId, out CellMemory m)) return m;
        var rng = new DirectorRng(_seed, ForecastStreamBase + (ulong)Math.Max(0, cellId));
        m = new CellMemory
        {
            EfBias = Mathf.Clamp(Gaussian(rng), -3f, 3f),
            EtaBias = Gaussian(rng),
        };
        _memory[cellId] = m;
        return m;
    }

    private static float Gaussian(DirectorRng rng)
    {
        float u1 = Mathf.Max(1e-7f, rng.NextFloat01());
        float u2 = rng.NextFloat01();
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
