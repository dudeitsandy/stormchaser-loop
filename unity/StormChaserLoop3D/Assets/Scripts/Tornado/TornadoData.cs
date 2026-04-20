using UnityEngine;

[CreateAssetMenu(fileName = "TornadoData_EF0", menuName = "StormChaser/TornadoData")]
public class TornadoData : ScriptableObject
{
    [Header("EF Scale")]
    public string EFRating = "EF0";
    public float StrengthMultiplier = 1f;   // used in scoring formula

    [Header("Movement")]
    public float MoveSpeed = 5f;            // units/sec across the field

    [Header("Visual")]
    public float ConeScale = 1f;            // relative scale of the cone mesh
}
