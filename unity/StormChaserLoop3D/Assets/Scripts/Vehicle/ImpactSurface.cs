using UnityEngine;

/// <summary>
/// Tags a collider with its <see cref="ImpactKind"/> (vehicle-feel.md F10). Untagged colliders are World.
/// Destructible props and Ram &amp; Unblock event obstacles add this; event obstacles are exempt from F10 HP.
/// </summary>
public sealed class ImpactSurface : MonoBehaviour
{
    [SerializeField] private ImpactKind _kind = ImpactKind.World;

    public ImpactKind Kind
    {
        get => _kind;
        set => _kind = value;
    }
}
