using UnityEngine;

public static class GameplaySceneResolver
{
    public static string ResolveGameplaySceneName(int level, string baseGameplaySceneName)
    {
        string baseName = string.IsNullOrWhiteSpace(baseGameplaySceneName) ? "Gameplayscene" : baseGameplaySceneName.Trim();
        if (level <= 1) return baseName;

        string a = baseName + "_Level" + level;
        string b = baseName + level;
        if (Application.CanStreamedLevelBeLoaded(a)) return a;
        if (Application.CanStreamedLevelBeLoaded(b)) return b;
        return a;
    }
}
