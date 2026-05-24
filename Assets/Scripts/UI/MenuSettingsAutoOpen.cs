using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuSettingsAutoOpen : MonoBehaviour
{
    private const string OpenSettingsKey = "open_settings";

    [SerializeField] private string settingsPanelName = "settingpanel";

    private void Start()
    {
        if (FindFirstObjectByType<MenuSettingsController>() != null) return;
        if (PlayerPrefs.GetInt(OpenSettingsKey, 0) != 1) return;

        GameObject panel = FindByName(settingsPanelName);
        if (panel != null) panel.SetActive(true);
    }

    private static GameObject FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) return all[i].gameObject;
        }

        return null;
    }
}

