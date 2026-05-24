using UnityEngine;

public static class LevelProgression
{
    private const string CurrentLevelKey = "current_level";
    private const string MaxUnlockedLevelKey = "max_unlocked_level";
    private const string CompletedPrefix = "completed_level_";

    public static int GetCurrentLevel()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(CurrentLevelKey, 1));
    }

    public static void SetCurrentLevel(int level)
    {
        int v = Mathf.Max(1, level);
        PlayerPrefs.SetInt(CurrentLevelKey, v);
        PlayerPrefs.Save();
    }

    public static int GetMaxUnlockedLevel()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(MaxUnlockedLevelKey, 1));
    }

    public static void UnlockLevel(int level)
    {
        int v = Mathf.Max(1, level);
        if (GetMaxUnlockedLevel() < v)
        {
            PlayerPrefs.SetInt(MaxUnlockedLevelKey, v);
            PlayerPrefs.Save();
        }
    }

    public static void MarkCompleted(int level)
    {
        int v = Mathf.Max(1, level);
        PlayerPrefs.SetInt(CompletedPrefix + v, 1);
        PlayerPrefs.Save();
    }

    public static bool IsCompleted(int level)
    {
        int v = Mathf.Max(1, level);
        return PlayerPrefs.GetInt(CompletedPrefix + v, 0) == 1;
    }
}

