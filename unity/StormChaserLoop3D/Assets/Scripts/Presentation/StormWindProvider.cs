using UnityEngine;
using System.Collections.Generic;

/// <summary>Small read-only source boundary to replace with the director query when available.</summary>
public static class StormWindProvider
{
    /// <summary>Samples summed magnitudes, strongest bearing and EF5 exposure without changing physics.</summary>
    public static void Sample(Vector3 position, out float exposure, out Vector3 bearing, out float ef5Exposure)
    {
        Sample(DisasterEntity.Active, position, out exposure, out bearing, out ef5Exposure);
        exposure = StormTelegraph.EnvironmentIntensity(position);
    }

    /// <summary>Explicit registry overload for native testing; runtime uses the active registry above.</summary>
    public static void Sample(IReadOnlyList<DisasterEntity> disasters, Vector3 position, out float exposure, out Vector3 bearing, out float ef5Exposure)
    {
        float total = 0f, strongest = 0f, ef5 = 0f;
        bearing = Vector3.zero;
        for (int i = 0; i < disasters.Count; i++)
        {
            var disaster = disasters[i];
            if (disaster == null) continue;
            Vector3 wind = disaster.GetWindAt(position);
            float magnitude = wind.magnitude;
            if (float.IsNaN(magnitude) || float.IsInfinity(magnitude)) continue;
            total += magnitude;
            if (magnitude > strongest) { strongest = magnitude; bearing = wind.normalized; }
            if (disaster is TornadoController tornado && tornado.EFRating == "EF5") ef5 += magnitude;
        }
        exposure = StormCueLevels.Exposure(total);
        ef5Exposure = StormCueLevels.Exposure(ef5);
    }
}
