using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string OpenSettingsKey = "open_settings";

    [SerializeField] private string panelObjectName = "w";
    [SerializeField] private string menuSceneName = "Menuscenes";
    [SerializeField] private string mapsSceneName = "LevelScenes";
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.45f;

    private GameObject _panel;
    private Button _pauseButton;
    private bool _paused;

    private TopDownStepMove2D _stepMove;
    private VirtualDpadButton[] _dpadButtons;

    private GameObject _blocker;

    private void Awake()
    {
        _pauseButton = GetComponent<Button>();
        if (_pauseButton != null) _pauseButton.onClick.AddListener(Open);

        Transform panelTr = FindChildByName(transform, panelObjectName);
        _panel = panelTr != null ? panelTr.gameObject : null;
        if (_panel != null) _panel.SetActive(false);

        _stepMove = FindFirstObjectByType<TopDownStepMove2D>();
        _dpadButtons = FindObjectsByType<VirtualDpadButton>(FindObjectsSortMode.None);

        WirePanelButtons();
        EnsureBlocker();
        SetPaused(false);
    }

    private void OnDestroy()
    {
        if (_pauseButton != null) _pauseButton.onClick.RemoveListener(Open);
    }

    public void Open()
    {
        if (_panel == null) return;
        _panel.SetActive(true);
        ShowBlocker(true);
        SetPaused(true);
    }

    public void Resume()
    {
        if (_panel != null) _panel.SetActive(false);
        ShowBlocker(false);
        SetPaused(false);
    }

    public void Close()
    {
        Resume();
    }

    public void GoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }

    public void GoMaps()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mapsSceneName, LoadSceneMode.Single);
    }

    public void GoSettings()
    {
        PlayerPrefs.SetInt(OpenSettingsKey, 1);
        PlayerPrefs.Save();
        GoMenu();
    }

    private void SetPaused(bool paused)
    {
        _paused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (_stepMove != null) _stepMove.enabled = !paused;

        if (_dpadButtons != null)
        {
            for (int i = 0; i < _dpadButtons.Length; i++)
            {
                if (_dpadButtons[i] != null) _dpadButtons[i].enabled = !paused;
            }
        }
    }

    private void WirePanelButtons()
    {
        if (_panel == null) return;

        Button[] buttons = _panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;

            string n = b.gameObject.name.Trim().ToLowerInvariant();

            if (n == "resume")
            {
                b.onClick.AddListener(Resume);
            }
            else if (n == "menu")
            {
                b.onClick.AddListener(GoMenu);
            }
            else if (n == "backtomenu" || n.Contains("backtomenu") || n == "mainmenu" || n.Contains("mainmenu"))
            {
                b.onClick.AddListener(GoMenu);
            }
            else if (n == "maps")
            {
                b.onClick.AddListener(GoMaps);
            }
            else if (n == "settings")
            {
                b.onClick.AddListener(GoSettings);
            }
            else if (n.Contains("close") || n.Contains("kapat") || n == "x" || n.Contains("exit"))
            {
                b.onClick.AddListener(Close);
            }
        }
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name)) return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) return all[i];
        }

        return null;
    }

    private void EnsureBlocker()
    {
        if (_blocker != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        _blocker = new GameObject("PauseBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _blocker.transform.SetParent(canvas.transform, false);

        RectTransform rt = (RectTransform)_blocker.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        Image img = _blocker.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, dimAlpha);
        img.raycastTarget = true;

        _blocker.SetActive(false);
    }

    private void ShowBlocker(bool show)
    {
        if (_blocker == null) return;

        if (show)
        {
            if (_panel != null)
            {
                Transform panelTr = _panel.transform;
                panelTr.SetAsLastSibling();
                int below = Mathf.Max(0, panelTr.GetSiblingIndex() - 1);
                _blocker.transform.SetSiblingIndex(below);
            }
        }

        _blocker.SetActive(show);
    }
}

