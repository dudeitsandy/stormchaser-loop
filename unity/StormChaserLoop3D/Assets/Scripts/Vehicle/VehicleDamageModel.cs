/// <summary>Pure HP + invulnerability bookkeeping, separated from MonoBehaviour for unit tests.</summary>
public class VehicleDamageModel
{
    private readonly float _invulnerabilitySeconds;
    private float _invulnerableUntil = float.NegativeInfinity;

    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsWrecked => CurrentHealth <= 0;

    public VehicleDamageModel(int maxHealth, float invulnerabilitySeconds)
    {
        MaxHealth = maxHealth < 1 ? 1 : maxHealth;
        CurrentHealth = MaxHealth;
        _invulnerabilitySeconds = invulnerabilitySeconds;
    }

    /// <summary>True while post-hit invulnerability is active at <paramref name="now"/>.</summary>
    public bool IsInvulnerable(float now) => now < _invulnerableUntil;

    /// <summary>Applies damage unless wrecked or invulnerable. Returns true if HP changed.</summary>
    public bool TryDamage(int amount, float now)
    {
        if (amount <= 0 || IsWrecked || IsInvulnerable(now)) return false;

        CurrentHealth = CurrentHealth - amount < 0 ? 0 : CurrentHealth - amount;
        _invulnerableUntil = now + _invulnerabilitySeconds;
        return true;
    }
}
