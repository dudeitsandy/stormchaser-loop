using UnityEngine;

public abstract class DisasterEntity : MonoBehaviour
{
    [SerializeField] protected DisasterData _data;

    public ThreatClass ThreatClass => _data.ThreatClass;
    public BehaviorPattern BehaviorPattern => _data.BehaviorPattern;
    public float MoveSpeed => _data.MoveSpeed;
    public float ThreatMultiplier => _data.ThreatMultiplier;
}
