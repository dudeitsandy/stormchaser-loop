using UnityEngine;

[CreateAssetMenu(fileName = "VehicleData", menuName = "StormChaser/VehicleData")]
public class VehicleData : ScriptableObject
{
    [Header("Movement")]
    public float MoveSpeed = 20f;
    public float ReverseSpeed = 8f;
    public float TurnSpeed = 90f;
    public float Acceleration = 18f;
    [Tooltip("Coasting slowdown with no throttle.")]
    public float Deceleration = 12f;
    [Tooltip("Slowdown when throttle opposes current motion.")]
    public float BrakeDeceleration = 45f;

    [Header("Handling")]
    [Tooltip("Turn rate multiplier at top speed (1 = same as low speed). Lower = calmer at speed.")]
    [Range(0.2f, 1f)] public float HighSpeedTurnFactor = 0.6f;
    [Tooltip("Speed below which turn rate ramps down to zero, so the truck can't spin in place.")]
    public float FullTurnSpeed = 4f;
    [Tooltip("How fast actual velocity converges on the heading (units/sec²). Lower = more slide.")]
    public float Grip = 55f;

    [Header("Wind")]
    [Tooltip("Fraction of disaster wind velocity applied to the truck. 1 = fully blown around.")]
    [Range(0f, 1f)] public float WindExposure = 0.85f;
    [Tooltip("Wind speed (units/sec) at which grip is cut in half.")]
    public float WindGripLossAt = 10f;
    [Tooltip("Max random steering wobble (deg/sec) in strong wind.")]
    public float WindWobble = 50f;

    [Header("Durability (see design/gdd/vehicle-damage.md)")]
    [Tooltip("Hit points. Default truck = 3 armor stars.")]
    public int MaxHealth = 3;
    [Tooltip("Seconds of invulnerability after a hit.")]
    public float InvulnerabilitySeconds = 1.5f;
    [Tooltip("Speed the truck is flung away from a disaster on hit.")]
    public float KnockbackSpeed = 18f;
}
