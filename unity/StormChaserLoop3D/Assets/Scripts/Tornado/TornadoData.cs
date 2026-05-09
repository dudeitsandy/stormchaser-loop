using UnityEngine;

[CreateAssetMenu(fileName = "TornadoData_EF0", menuName = "StormChaser/TornadoData")]
public class TornadoData : DisasterData
{
    [Header("EF Scale")]
    public string EFRating = "EF0";
    public float StrengthMultiplier = 1f;

    [Header("Visual")]
    public float ConeScale = 1f;

    public override float ThreatMultiplier => StrengthMultiplier;
}
