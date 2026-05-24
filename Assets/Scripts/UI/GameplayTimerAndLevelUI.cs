using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayTimerAndLevelUI : MonoBehaviour
{
    public static GameplayTimerAndLevelUI Instance { get; private set; }

    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private int baseSeconds = 60;
    [SerializeField] private int extraSecondsPerLevel = 30;
    [SerializeField] private LosePanelController losePanel;

    private float _remaining;
    private int _lastShownSecond = int.MinValue;
    private bool _ended;

    private TopDownStepMove2D _stepMove;
    private VirtualDpadButton[] _dpadButtons;

    private void Awake()
    {
        Instance = this;
        _stepMove = FindFirstObjectByType<TopDownStepMove2D>();
        _dpadButtons = FindObjectsByType<VirtualDpadButton>(FindObjectsSortMode.None);
        if (losePanel == null) losePanel = FindFirstObjectByType<LosePanelController>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        int level = LevelProgression.GetCurrentLevel();
        int totalSeconds = Mathf.Max(1, baseSeconds + (level - 1) * extraSecondsPerLevel);
        _remaining = totalSeconds;

        if (levelText != null) levelText.text = "Level " + level;
        UpdateTimeText(force: true);
    }

    private void Update()
    {
        if (_ended) return;
        if (Time.timeScale <= 0f) return;

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f)
        {
            _remaining = 0f;
            UpdateTimeText(force: true);
            TriggerLose(LoseReason.Timeout);
            return;
        }

        UpdateTimeText(force: false);
    }

    private void UpdateTimeText(bool force)
    {
        if (timeText == null) return;

        int sec = Mathf.CeilToInt(_remaining);
        if (!force && sec == _lastShownSecond) return;
        _lastShownSecond = sec;

        if (sec < 60)
        {
            timeText.text = "00:" + sec.ToString("00");
            return;
        }

        int m = sec / 60;
        int s = sec % 60;
        timeText.text = m.ToString("00") + ":" + s.ToString("00");
    }

    public void TriggerLose() => TriggerLose(LoseReason.Timeout);

    public void TriggerLose(LoseReason reason)
    {
        if (_ended) return;
        _ended = true;

        if (reason == LoseReason.Crashed && UIAudioManager.Instance != null) UIAudioManager.Instance.PlayCrash();

        if (_stepMove != null) _stepMove.enabled = false;

        if (_dpadButtons != null)
        {
            for (int i = 0; i < _dpadButtons.Length; i++)
            {
                if (_dpadButtons[i] != null) _dpadButtons[i].enabled = false;
            }
        }

        Time.timeScale = 0f;

        if (losePanel != null) losePanel.Open(reason);
        else if (timeText != null) timeText.text = "GAME OVER";
    }
}

