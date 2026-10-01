using System.Collections.Generic;
using UnityEngine;

/// <summary>Base for every disaster. Maintains a registry of live instances so nobody has to FindObjectsByType per frame.</summary>
public abstract class DisasterEntity : MonoBehaviour
{
    private static readonly List<DisasterEntity> _active = new List<DisasterEntity>();

    /// <summary>All enabled disasters in the scene.</summary>
    public static IReadOnlyList<DisasterEntity> Active => _active;

    [SerializeField] protected DisasterData _data;

    public ThreatClass ThreatClass => _data.ThreatClass;
    public BehaviorPattern BehaviorPattern => _data.BehaviorPattern;
    public float MoveSpeed => _data.MoveSpeed;
    public float ThreatMultiplier => _data.ThreatMultiplier;

    /// <summary>Radius inside which the player vehicle takes damage.</summary>
    public virtual float DamageRadius => 3f;

    /// <summary>Wind velocity (units/sec, XZ) this disaster imposes at a world position. Zero by default.</summary>
    public virtual Vector3 GetWindAt(Vector3 position) => Vector3.zero;

    /// <summary>Sum of wind from every active disaster at a world position.</summary>
    public static Vector3 TotalWindAt(Vector3 position)
    {
        Vector3 wind = Vector3.zero;
        foreach (DisasterEntity e in _active) wind += e.GetWindAt(position);
        return wind;
    }

    /// <summary>
    /// Lift as a fraction of a vehicle's weight at <paramref name="position"/> (vehicle-feel.md F12). Zero by default.
    /// </summary>
    public virtual float GetLiftFractionAt(Vector3 position, float exposure, float liftCoefficient) => 0f;

    /// <summary>Strongest lift from any active disaster at a position (fraction of weight), with its EF strength.</summary>
    public static float MaxLiftFractionAt(Vector3 position, float exposure, float liftCoefficient, out float efStrength)
    {
        float best = 0f;
        efStrength = 0f;
        foreach (DisasterEntity e in _active)
        {
            float lift = e.GetLiftFractionAt(position, exposure, liftCoefficient);
            if (lift > best)
            {
                best = lift;
                efStrength = e.ThreatMultiplier;
            }
        }
        return best;
    }

    protected virtual void OnEnable() => _active.Add(this);
    protected virtual void OnDisable() => _active.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _active.Clear();
}
