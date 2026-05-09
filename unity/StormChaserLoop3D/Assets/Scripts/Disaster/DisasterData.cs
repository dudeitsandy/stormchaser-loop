using UnityEngine;

public enum ThreatClass { I, II, III, IV, V }
public enum BehaviorPattern { Move, Charge, Stationary, Patrol, Swarm }

[CreateAssetMenu(fileName = "DisasterData", menuName = "StormChaser/DisasterData")]
public class DisasterData : ScriptableObject
{
    [Header("Threat")]
    public ThreatClass ThreatClass = ThreatClass.I;
    public BehaviorPattern BehaviorPattern = BehaviorPattern.Move;

    [Header("Movement")]
    public float MoveSpeed = 5f;

    public virtual float ThreatMultiplier => 1f;
}
