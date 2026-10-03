using UnityEngine;
using UnityEngine.Events;

/// <summary>Takes damage when the truck enters a disaster's damage radius. Raises <see cref="GameEvents.PlayerDamaged"/>.</summary>
[RequireComponent(typeof(PlayerVehicle))]
public class VehicleHealth : MonoBehaviour
{
    [Tooltip("HP lost per disaster contact.")]
    [SerializeField] private int _contactDamage = 1;

    /// <summary>Fired once when HP reaches zero.</summary>
    public UnityEvent OnWrecked = new UnityEvent();

    private PlayerVehicle _vehicle;
    private VehicleDamageModel _model;

    public int CurrentHealth => _model.CurrentHealth;
    public int MaxHealth => _model.MaxHealth;
    public bool IsWrecked => _model.IsWrecked;
    public bool IsInvulnerable => _model.IsInvulnerable(Time.time);

    /// <summary>vehicle-damage.md stages: Damaged at ≤ floor(Max·2/3) HP, Critical at ≤ floor(Max/3) HP.</summary>
    public DamageStage Stage => StageFor(_model.CurrentHealth, _model.MaxHealth);

    /// <summary>Pure stage mapping (vehicle-damage.md Formulas).</summary>
    public static DamageStage StageFor(int current, int max)
    {
        if (current <= max / 3) return DamageStage.Critical;
        if (current <= max * 2 / 3) return DamageStage.Damaged;
        return DamageStage.Healthy;
    }

    /// <summary>When false, contact damage is ignored (title / results).</summary>
    public bool Vulnerable { get; set; } = true;

    private void Awake()
    {
        _vehicle = GetComponent<PlayerVehicle>();
        _model = new VehicleDamageModel(_vehicle.Data.MaxHealth, _vehicle.Data.InvulnerabilitySeconds);
    }

    /// <summary>
    /// Applies F10 impact HP (vehicle-feel.md) with the usual post-hit invulnerability. Called by
    /// <see cref="PlayerVehicle"/> only while <c>ImpactsCostHp</c> is on. Returns true if HP changed.
    /// </summary>
    public bool ApplyImpactDamage(int hpLoss)
    {
        if (!Vulnerable || hpLoss <= 0 || !_model.TryDamage(hpLoss, Time.time)) return false;
        GameEvents.RaisePlayerDamaged(_model.CurrentHealth, _model.MaxHealth);
        if (_model.IsWrecked) OnWrecked.Invoke();
        return true;
    }

    private void Update()
    {
        if (!Vulnerable || _model.IsWrecked) return;

        Vector3 pos = transform.position;
        foreach (DisasterEntity e in DisasterEntity.Active)
        {
            Vector3 delta = pos - e.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > e.DamageRadius * e.DamageRadius) continue;

            if (_model.TryDamage(_contactDamage, Time.time))
            {
                Vector3 away = delta.sqrMagnitude > 0.01f ? delta : -transform.forward;
                _vehicle.ApplyKnockback(away, _vehicle.Data.KnockbackSpeed);
                GameEvents.RaisePlayerDamaged(_model.CurrentHealth, _model.MaxHealth);
                if (_model.IsWrecked) OnWrecked.Invoke();
            }
            break;
        }
    }
}
