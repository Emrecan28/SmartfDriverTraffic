using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MenuSettingsController : MonoBehaviour
{
    private const string OpenSettingsKey = "open_settings";
    private const string SoundEnabledKey = "sound_enabled";
    private const string MusicEnabledKey = "music_enabled";

    [SerializeField] private string settingsButtonName = "Settingsbuton";
    [SerializeField] private string settingsPanelName = "settingpanel";
    [SerializeField] private string closeButtonName = "kapat";

    [SerializeField] private string soundOnName = "soundon";
    [SerializeField] private string soundOffName = "soundof";
    [SerializeField] private string musicOnName = "musicon";
    [SerializeField] private string musicOffName = "MUSİCOF";

    private GameObject _panel;
    private Button _settingsButton;
    private Button _closeButton;

    private GameObject _soundOn;
    private GameObject _soundOff;
    private GameObject _musicOn;
    private GameObject _musicOff;

    private GameObject _soundOffDim;
    private GameObject _musicOffDim;

    private GameObject _blocker;

    private void Awake()
    {
        _panel = FindByName(settingsPanelName);
        _settingsButton = FindButtonByName(settingsButtonName);
        _closeButton = FindButtonByName(closeButtonName);

        _soundOn = FindByName(soundOnName);
        _soundOff = FindByName(soundOffName);
        _musicOn = FindByName(musicOnName);
        _musicOff = FindByName(musicOffName);

        if (_settingsButton != null) _settingsButton.onClick.AddListener(Open);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);

        WireToggle(_soundOn, true, SetSoundEnabled);
        WireToggle(_soundOff, false, SetSoundEnabled);
        WireToggle(_musicOn, true, SetMusicEnabled);
        WireToggle(_musicOff, false, SetMusicEnabled);

        _soundOffDim = EnsureDimOverlay(_soundOff);
        _musicOffDim = EnsureDimOverlay(_musicOff);

        EnsureBlocker();
    }

    private void Start()
    {
        bool openFromGameplay = PlayerPrefs.GetInt(OpenSettingsKey, 0) == 1;
        if (openFromGameplay) PlayerPrefs.DeleteKey(OpenSettingsKey);

        EnsureButtonsActive();
        ApplyVisuals();
        ApplyAudioToScene();

        if (openFromGameplay) Open();
        else Close();
    }

    private void OnDestroy()
    {
        if (_settingsButton != null) _settingsButton.onClick.RemoveListener(Open);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (_panel != null) _panel.SetActive(true);
        ReorderBlocker();
        if (_blocker != null) _blocker.SetActive(true);
        ApplyVisuals();
    }

    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);
        if (_blocker != null) _blocker.SetActive(false);
    }

    private void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVisuals();
        ApplyAudioToScene();
    }

    private void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVisuals();
        ApplyAudioToScene();
    }

    private void ApplyVisuals()
    {
        bool soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;

        if (_soundOffDim != null) _soundOffDim.SetActive(!soundEnabled);
        if (_musicOffDim != null) _musicOffDim.SetActive(!musicEnabled);
    }

    private static void ApplyAudioToScene()
    {
        bool soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource s = sources[i];
            if (s == null) continue;

            bool isMusic = s.loop;
            if (isMusic) s.mute = !musicEnabled;
            else s.mute = !soundEnabled;
        }
    }

    private void EnsureBlocker()
    {
        if (_blocker != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTr = canvas.transform;
        Transform settingsTr = FindTransformByName(canvasTr, settingsButtonName);

        _blocker = new GameObject("SettingsBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _blocker.transform.SetParent(canvasTr, false);

        RectTransform rt = (RectTransform)_blocker.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        Image img = _blocker.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        img.raycastTarget = true;

        _blocker.SetActive(false);
    }

    private void ReorderBlocker()
    {
        if (_blocker == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTr = canvas.transform;
        Transform settingsTr = FindTransformByName(canvasTr, settingsButtonName);
        if (settingsTr == null) return;

        settingsTr.SetAsLastSibling();
        int below = Mathf.Max(0, settingsTr.GetSiblingIndex() - 1);
        _blocker.transform.SetSiblingIndex(below);
        _blocker.SetActive(false);
    }

    private static void WireToggle(GameObject go, bool enableValue, System.Action<bool> setter)
    {
        if (go == null || setter == null) return;
        Button b = go.GetComponent<Button>();
        if (b == null) return;
        b.onClick.AddListener(() => setter(enableValue));
    }

    private static GameObject EnsureDimOverlay(GameObject go)
    {
        if (go == null) return null;
        Transform existing = go.transform.Find("Dim");
        if (existing != null) return existing.gameObject;

        Image parentImage = go.GetComponent<Image>();
        if (parentImage == null) return null;

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dim.transform.SetParent(go.transform, false);

        RectTransform rt = (RectTransform)dim.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        Image img = dim.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.35f);
        img.raycastTarget = false;

        return dim;
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

    private static Button FindButtonByName(string name)
    {
        GameObject go = FindByName(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static Transform FindTransformByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name)) return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) return all[i];
        }
        return null;
    }

    private void EnsureButtonsActive()
    {
        if (_soundOn != null) _soundOn.SetActive(true);
        if (_soundOff != null) _soundOff.SetActive(true);
        if (_musicOn != null) _musicOn.SetActive(true);
        if (_musicOff != null) _musicOff.SetActive(true);
    }
}

