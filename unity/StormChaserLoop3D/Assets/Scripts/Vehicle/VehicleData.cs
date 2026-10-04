using UnityEngine;

/// <summary>
/// Per-archetype vehicle identity (ADR-0005): star ratings, wheel/body layout, durability.
/// Feel comes from stars via vehicle-feel.md F13 — tuning knobs live in the shared VehicleFeelConfig.
/// </summary>
[CreateAssetMenu(fileName = "VehicleData", menuName = "StormChaser/VehicleData")]
public class VehicleData : ScriptableObject
{
    [Header("Archetype (vision-1.0 stars; Armor ✗ = 0)")]
    public Stars Stars = Stars.Pickup;

    [Header("Layout")]
    public WheelLayout Layout = WheelLayout.Pickup;

    [Header("Durability (see design/gdd/vehicle-damage.md)")]
    [Tooltip("Hit points. Pickup = 6 (S8-C1 interim: 2 × its 3 armor stars until the S8-C2 durability redesign).")]
    public int MaxHealth = 6;
    [Tooltip("Seconds of invulnerability after a hit.")]
    public float InvulnerabilitySeconds = 1.5f;
    [Tooltip("Speed the truck is flung away from a disaster on funnel contact.")]
    public float KnockbackSpeed = 18f;
}
