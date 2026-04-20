using UnityEngine;

[CreateAssetMenu(fileName = "VehicleData", menuName = "StormChaser/VehicleData")]
public class VehicleData : ScriptableObject
{
    [Header("Movement")]
    public float MoveSpeed = 20f;
    public float ReverseSpeed = 8f;
    public float TurnSpeed = 90f;
    public float Acceleration = 10f;
    public float Deceleration = 15f;
}
