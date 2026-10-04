using UnityEngine;

[CreateAssetMenu(fileName = "TornadoData_EF0", menuName = "StormChaser/TornadoData")]
public class TornadoData : DisasterData
{
    [Header("EF Scale")]
    public string EFRating = "EF0";
    public float StrengthMultiplier = 1f;

    [Header("Storm scale (storm-director.md F3)")]
    [Tooltip("Damage radius D in metres at full intensity (the core).")]
    public float DamageRadius = 1.5f;
    [Tooltip("Wind radius R in metres at full intensity.")]
    public float WindRadius = 12f;
    [Tooltip("Peak wind P in m/s at the axis at full intensity.")]
    public float PeakWind = 8f;
    [Tooltip("Forming seconds (Epic; compact mode scales these).")]
    public float FormSeconds = 20f;
    [Tooltip("Mature (peak window) seconds.")]
    public float MatureSeconds = 30f;
    [Tooltip("Roping-out seconds.")]
    public float RopeSeconds = 10f;
    [Tooltip("Wander turn rate ω in degrees/second (F5); big storms are ponderous.")]
    public float TurnRateDeg = 24f;

    [Header("Visual")]
    [Tooltip("Funnel width factor, visual only: ≈ DamageRadius / 4.3.")]
    public float ConeScale = 1f;

    public override float ThreatMultiplier => StrengthMultiplier;
}
