using UnityEngine;

/// <summary>Persists the best Sprint score across sessions (PlayerPrefs).</summary>
public static class BestScoreStore
{
    private const string Key = "Doomsday.Sprint.BestScore";

    public static float Load() => PlayerPrefs.GetFloat(Key, 0f);

    /// <summary>Saves <paramref name="score"/> if it beats the stored best. Returns true if it did.</summary>
    public static bool TrySave(float score)
    {
        if (score <= Load()) return false;
        PlayerPrefs.SetFloat(Key, score);
        PlayerPrefs.Save();
        return true;
    }
}
