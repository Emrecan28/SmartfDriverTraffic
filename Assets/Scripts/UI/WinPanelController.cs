using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WinPanelController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.45f;
    [SerializeField] private bool pauseGame = true;

    [SerializeField] private string[] popTargetNames = { "levelcomplogo", "Image", "Image (1)", "Image (2)" };
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

    private void Awake()
    {
        CacheTargets();
        EnsureBlocker();
        WireButtons();
    }

    private void OnEnable()
    {
        if (pauseGame) Time.timeScale = 0f;
        ShowBlocker(true);
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayWin();
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
        _targets = new RectTransform[popTargetNames != null ? popTargetNames.Length : 0];
        _basePos = new Vector2[_targets.Length];
        _baseScale = new Vector3[_targets.Length];

        for (int i = 0; i < _targets.Length; i++)
        {
            string n = popTargetNames[i];
            if (string.IsNullOrWhiteSpace(n)) continue;

            Transform child = FindChildByName(transform, n);
            RectTransform rt = child != null ? child.GetComponent<RectTransform>() : null;
            _targets[i] = rt;

            if (rt != null)
            {
                _basePos[i] = rt.anchoredPosition;
                _baseScale[i] = rt.localScale;
            }
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

            if (n == "mainmenuscene" || n == "maınmenuscene")
            {
                b.onClick.AddListener(GoMenu);
            }
            else if (n == "levelscene")
            {
                b.onClick.AddListener(GoLevelScene);
            }
            else if (n == "nextlevelbuton" || n == "levelpass" || n.Contains("nextlevel") || (n.Contains("next") && n.Contains("level")))
            {
                b.onClick.AddListener(GoNextLevel);
            }
        }
    }

    private void GoMenu()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
        }
    }

    private void GoLevelScene()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(levelSceneName))
        {
            SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
        }
    }

    private void GoNextLevel()
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;

        int current = LevelProgression.GetCurrentLevel();
        int next = Mathf.Max(1, current + 1);
        string sceneToLoad = GameplaySceneResolver.ResolveGameplaySceneName(next, gameplaySceneName);
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            _loading = false;
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            if (!string.IsNullOrWhiteSpace(levelSceneName)) SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
            return;
        }

        LevelProgression.UnlockLevel(next);
        LevelProgression.SetCurrentLevel(next);
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    private void EnsureBlocker()
    {
        if (_blocker != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        _blocker = new GameObject("WinBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
}

