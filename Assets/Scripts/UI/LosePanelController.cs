using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LosePanelController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.45f;
    [SerializeField] private bool pauseGame = true;

    [SerializeField] private string[] popTargetNames = { "defeat", "Image", "Image (1)", "Image (2)" };
    [SerializeField] private string crashedTextName = "crashed";
    [SerializeField] private string timeoutTextName = "timeout";
    [SerializeField] private float popOffsetY = 120f;
    [SerializeField] private float popDurationSeconds = 0.22f;
    [SerializeField] private float staggerSeconds = 0.06f;

    [SerializeField] private string menuSceneName = "Menuscenes";
    [SerializeField] private string levelSceneName = "LevelScenes";
    [SerializeField] private string gameplaySceneName = "Gameplayscene";

    private RectTransform[] _targets;
    private Vector2[] _basePos;
    private Vector3[] _baseScale;

    private GameObject _blocker;
    private Coroutine _introRoutine;
    private bool _loading;
    private LoseReason _reason = LoseReason.Timeout;
    private GameObject _crashedTextGo;
    private GameObject _timeoutTextGo;

    private void Awake()
    {
        CacheTargets();
        EnsureBlocker();
        WireButtons();

        _crashedTextGo = FindChildByNameInsensitive(transform, crashedTextName);
        _timeoutTextGo = FindChildByNameInsensitive(transform, timeoutTextName);
        if (_crashedTextGo != null) _crashedTextGo.SetActive(false);
        if (_timeoutTextGo != null) _timeoutTextGo.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseGame) Time.timeScale = 0f;
        ShowBlocker(true);
        ApplyReasonVisuals();
        CacheTargets();
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayLose();
        PlayIntro();
    }

    private void OnDisable()
    {
        if (_introRoutine != null)
        {
            StopCoroutine(_introRoutine);
            _introRoutine = null;
        }

        ShowBlocker(false);
        if (pauseGame) Time.timeScale = 1f;
    }

    public void Open()
    {
        Open(LoseReason.Timeout);
    }

    public void Open(LoseReason reason)
    {
        _reason = reason;
        if (gameObject.activeSelf) return;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
    }

    private void PlayIntro()
    {
        if (_targets == null || _targets.Length == 0) return;

        if (_introRoutine != null) StopCoroutine(_introRoutine);
        _introRoutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            RectTransform t = _targets[i];
            if (t == null) continue;

            t.anchoredPosition = _basePos[i] + Vector2.down * popOffsetY;
            t.localScale = Vector3.zero;
        }

        yield return null;

        for (int i = 0; i < _targets.Length; i++)
        {
            RectTransform t = _targets[i];
            if (t == null) continue;
            yield return AnimatePop(t, _basePos[i], _baseScale[i], popDurationSeconds);
            if (staggerSeconds > 0f) yield return WaitUnscaled(staggerSeconds);
        }
    }

    private static IEnumerator AnimatePop(RectTransform t, Vector2 endPos, Vector3 endScale, float duration)
    {
        float d = Mathf.Max(0.01f, duration);
        float time = 0f;

        Vector2 startPos = t.anchoredPosition;
        Vector3 startScale = t.localScale;

        while (time < d)
        {
            time += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(time / d);
            float e = EaseOutBack(u);
            t.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);
            t.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
            yield return null;
        }

        t.anchoredPosition = endPos;
        t.localScale = endScale;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = x - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    private void CacheTargets()
    {
        string reasonName = _reason == LoseReason.Crashed ? crashedTextName : timeoutTextName;
        bool includeReason = !string.IsNullOrWhiteSpace(reasonName) && !ArrayContains(popTargetNames, reasonName);
        int baseLen = popTargetNames != null ? popTargetNames.Length : 0;
        int len = baseLen + (includeReason ? 1 : 0);

        _targets = new RectTransform[len];
        _basePos = new Vector2[_targets.Length];
        _baseScale = new Vector3[_targets.Length];

        int dst = 0;
        if (includeReason)
        {
            RectTransform rt = GetRect(reasonName);
            _targets[dst] = rt;
            if (rt != null)
            {
                _basePos[dst] = rt.anchoredPosition;
                _baseScale[dst] = rt.localScale;
            }
            dst++;
        }

        for (int i = 0; i < baseLen; i++)
        {
            string n = popTargetNames[i];
            if (string.IsNullOrWhiteSpace(n)) continue;

            RectTransform rt = GetRect(n);
            _targets[dst] = rt;

            if (rt != null)
            {
                _basePos[dst] = rt.anchoredPosition;
                _baseScale[dst] = rt.localScale;
            }
            dst++;
        }
    }

    private void WireButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;

            string n = b.gameObject.name.Trim().ToLowerInvariant();

            if (n == "mainmenuscene" || n.Contains("mainmenu") || n.Contains("menu"))
            {
                b.onClick.AddListener(GoMenu);
            }
            else if (n.Contains("levelscene") || n.Contains("level"))
            {
                b.onClick.AddListener(GoLevelScene);
            }
            else if (n.Contains("tryagain") || n.Contains("retry") || n.Contains("again") || n.Contains("restart"))
            {
                b.onClick.AddListener(Retry);
            }
        }
    }

    private void GoMenu()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(menuSceneName)) SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }

    private void GoLevelScene()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(levelSceneName)) SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
    }

    private void Retry()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        int level = LevelProgression.GetCurrentLevel();
        string sceneToLoad = GameplaySceneResolver.ResolveGameplaySceneName(level, gameplaySceneName);
        if (!string.IsNullOrWhiteSpace(sceneToLoad)) SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    private void EnsureBlocker()
    {
        if (_blocker != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        _blocker = new GameObject("LoseBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            Transform panelTr = transform;
            panelTr.SetAsLastSibling();
            int below = Mathf.Max(0, panelTr.GetSiblingIndex() - 1);
            _blocker.transform.SetSiblingIndex(below);
        }

        _blocker.SetActive(show);
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null) return null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) return all[i];
        }
        return null;
    }

    private void ApplyReasonVisuals()
    {
        if (_crashedTextGo != null) _crashedTextGo.SetActive(_reason == LoseReason.Crashed);
        if (_timeoutTextGo != null) _timeoutTextGo.SetActive(_reason == LoseReason.Timeout);
    }

    private RectTransform GetRect(string name)
    {
        GameObject go = FindChildByNameInsensitive(transform, name);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static bool ArrayContains(string[] arr, string v)
    {
        if (arr == null || string.IsNullOrWhiteSpace(v)) return false;
        for (int i = 0; i < arr.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(arr[i])) continue;
            if (arr[i].Trim().ToLowerInvariant() == v.Trim().ToLowerInvariant()) return true;
        }
        return false;
    }

    private static GameObject FindChildByNameInsensitive(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name)) return null;
        string want = name.Trim().ToLowerInvariant();

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t.name != null && t.name.Trim().ToLowerInvariant() == want) return t.gameObject;
        }
        return null;
    }
}

