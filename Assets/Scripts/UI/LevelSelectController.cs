using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelSelectController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplayscene";

    [SerializeField] private string emptyStarName0 = "Image";
    [SerializeField] private string emptyStarName1 = "Image (1)";
    [SerializeField] private string emptyStarName2 = "Image (2)";

    [SerializeField] private string fullStarName0 = "doluyıldız";
    [SerializeField] private string fullStarName1 = "doluyıldız2";
    [SerializeField] private string fullStarName2 = "doluyıldız3";

    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private bool lockLevelsAboveCurrent = true;
    [SerializeField] private float currentPulse = 0.12f;
    [SerializeField] private float currentPulseSpeed = 2.2f;

    private readonly List<LevelUi> _levels = new();
    private LevelUi _current;
    private bool _loading;

    private void Awake()
    {
        CacheLevels();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        if (_current == null || _current.TargetGraphic == null) return;
        if (currentPulse <= 0f) return;

        float a = 1f - currentPulse + Mathf.Abs(Mathf.Sin(Time.unscaledTime * currentPulseSpeed)) * currentPulse;
        Color c = _current.TargetGraphic.color;
        c.a = a;
        _current.TargetGraphic.color = c;
    }

    public void Refresh()
    {
        int currentLevel = LevelProgression.GetCurrentLevel();
        int maxUnlocked = LevelProgression.GetMaxUnlockedLevel();
        if (lockLevelsAboveCurrent) maxUnlocked = Mathf.Min(maxUnlocked, currentLevel);

        _current = null;

        for (int i = 0; i < _levels.Count; i++)
        {
            LevelUi ui = _levels[i];
            if (ui == null) continue;

            bool unlocked = ui.LevelNumber <= maxUnlocked;
            bool isCurrent = ui.LevelNumber == currentLevel;
            bool completed = LevelProgression.IsCompleted(ui.LevelNumber);

            if (ui.Button != null)
            {
                ui.Button.interactable = unlocked;
                ui.Button.onClick.RemoveListener(ui.ClickHandler);
                if (unlocked) ui.Button.onClick.AddListener(ui.ClickHandler);

                ColorBlock cb = ui.Button.colors;
                if (!unlocked)
                {
                    cb.normalColor = lockedColor;
                    cb.highlightedColor = lockedColor;
                    cb.pressedColor = lockedColor;
                    cb.selectedColor = lockedColor;
                    cb.disabledColor = lockedColor;
                }
                else if (isCurrent)
                {
                    Color c = currentColor;
                    c.a = 1f;
                    cb.normalColor = c;
                    cb.highlightedColor = c;
                    cb.selectedColor = c;
                    cb.disabledColor = c;
                }
                else
                {
                    Color c = unlockedColor;
                    c.a = 1f;
                    cb.normalColor = c;
                    cb.highlightedColor = c;
                    cb.selectedColor = c;
                    cb.disabledColor = c;
                }
                ui.Button.colors = cb;
            }

            if (ui.TargetGraphic != null)
            {
                if (!unlocked)
                {
                    ui.TargetGraphic.color = lockedColor;
                }
                else if (isCurrent)
                {
                    Color c = currentColor;
                    c.a = 1f;
                    ui.TargetGraphic.color = c;
                }
                else
                {
                    Color c = unlockedColor;
                    c.a = 1f;
                    ui.TargetGraphic.color = c;
                }
            }

            if (!unlocked)
            {
                ui.SetStarsVisible(true, false);
                ui.SetStarTint(lockedColor);
            }
            else if (completed && !isCurrent)
            {
                ui.SetStarsVisible(false, true);
                ui.SetStarTint(Color.white);
            }
            else
            {
                ui.SetStarsVisible(true, false);
                ui.SetStarTint(Color.white);
            }

            if (isCurrent && unlocked) _current = ui;
        }
    }

    private void CacheLevels()
    {
        _levels.Clear();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;

            string n = b.gameObject.name;
            if (!TryParseLevelButtonName(n, out int level)) continue;

            Image target = b.targetGraphic as Image;

            LevelUi ui = new LevelUi(level, b, target, b.transform, this);
            _levels.Add(ui);
        }

        _levels.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
    }

    private void LoadLevel(int levelNumber)
    {
        if (_loading) return;
        _loading = true;
        Time.timeScale = 1f;
        LevelProgression.SetCurrentLevel(levelNumber);
        string sceneToLoad = GameplaySceneResolver.ResolveGameplaySceneName(levelNumber, gameplaySceneName);
        if (!string.IsNullOrWhiteSpace(sceneToLoad)) SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    private bool TryParseLevelButtonName(string name, out int levelNumber)
    {
        levelNumber = 0;
        if (string.IsNullOrWhiteSpace(name)) return false;

        string s = name.Trim().ToLowerInvariant();
        if (!s.StartsWith("level", StringComparison.Ordinal)) return false;
        int idx = 5;

        int start = idx;
        while (idx < s.Length && char.IsDigit(s[idx])) idx++;
        if (idx == start) return false;

        if (!int.TryParse(s.Substring(start, idx - start), out levelNumber)) return false;
        if (levelNumber < 1) return false;

        if (idx >= s.Length) return false;
        string tail = s.Substring(idx);
        if (!tail.Contains("buton", StringComparison.Ordinal) &&
            !tail.Contains("button", StringComparison.Ordinal) &&
            !tail.Contains("btn", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private sealed class LevelUi
    {
        public readonly int LevelNumber;
        public readonly Button Button;
        public readonly Image TargetGraphic;
        public readonly UnityEngine.Events.UnityAction ClickHandler;

        private readonly GameObject _empty0;
        private readonly GameObject _empty1;
        private readonly GameObject _empty2;
        private readonly GameObject _full0;
        private readonly GameObject _full1;
        private readonly GameObject _full2;
        private readonly Image _empty0Img;
        private readonly Image _empty1Img;
        private readonly Image _empty2Img;
        private readonly Image _full0Img;
        private readonly Image _full1Img;
        private readonly Image _full2Img;

        public LevelUi(int levelNumber, Button button, Image targetGraphic, Transform root, LevelSelectController owner)
        {
            LevelNumber = levelNumber;
            Button = button;
            TargetGraphic = targetGraphic;

            _empty0 = FindChild(root, owner.emptyStarName0);
            _empty1 = FindChild(root, owner.emptyStarName1);
            _empty2 = FindChild(root, owner.emptyStarName2);

            _full0 = FindChild(root, owner.fullStarName0);
            _full1 = FindChild(root, owner.fullStarName1);
            _full2 = FindChild(root, owner.fullStarName2);

            _empty0Img = _empty0 != null ? _empty0.GetComponent<Image>() : null;
            _empty1Img = _empty1 != null ? _empty1.GetComponent<Image>() : null;
            _empty2Img = _empty2 != null ? _empty2.GetComponent<Image>() : null;

            _full0Img = _full0 != null ? _full0.GetComponent<Image>() : null;
            _full1Img = _full1 != null ? _full1.GetComponent<Image>() : null;
            _full2Img = _full2 != null ? _full2.GetComponent<Image>() : null;

            ClickHandler = () => owner.LoadLevel(LevelNumber);
        }

        public void SetStarsVisible(bool emptyVisible, bool fullVisible)
        {
            if (_empty0 != null) _empty0.SetActive(emptyVisible);
            if (_empty1 != null) _empty1.SetActive(emptyVisible);
            if (_empty2 != null) _empty2.SetActive(emptyVisible);

            if (_full0 != null) _full0.SetActive(fullVisible);
            if (_full1 != null) _full1.SetActive(fullVisible);
            if (_full2 != null) _full2.SetActive(fullVisible);
        }

        public void SetStarTint(Color c)
        {
            if (_empty0Img != null) _empty0Img.color = c;
            if (_empty1Img != null) _empty1Img.color = c;
            if (_empty2Img != null) _empty2Img.color = c;

            if (_full0Img != null) _full0Img.color = Color.white;
            if (_full1Img != null) _full1Img.color = Color.white;
            if (_full2Img != null) _full2Img.color = Color.white;
        }

        private static GameObject FindChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName)) return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == childName) return all[i].gameObject;
            }

            return null;
        }
    }
}

