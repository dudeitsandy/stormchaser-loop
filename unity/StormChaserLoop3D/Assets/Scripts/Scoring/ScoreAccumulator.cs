using UnityEngine;

/// <summary>Running tally for the current run.</summary>
public class ScoreAccumulator : MonoBehaviour
{
    public float TotalScore { get; private set; }
    public int PhotoCount { get; private set; }
    public float BestShot { get; private set; }

    /// <summary>Adds one photo's score to the run.</summary>
    public void AddScore(float amount)
    {
        TotalScore += amount;
        PhotoCount++;
        if (amount > BestShot) BestShot = amount;
    }
}
