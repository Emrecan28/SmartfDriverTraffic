using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameplayStartSfx : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplayscene";
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float engineStartDelaySeconds = 0.32f;

    private static GameplayStartSfx _instance;
    private Coroutine _co;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += HandleSceneChanged;
    }

    private void OnEnable()
    {
        if (!playOnEnable) return;
        Play();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            _instance = null;
        }
    }

    private void HandleSceneChanged(Scene from, Scene to)
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName)) return;
        string n = to.name;
        if (string.IsNullOrWhiteSpace(n)) return;
        if (!n.StartsWith(gameplaySceneName, System.StringComparison.OrdinalIgnoreCase)) return;
        Play();
    }

    public void Play()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayHorn();

        if (engineStartDelaySeconds > 0f)
        {
            float t = 0f;
            while (t < engineStartDelaySeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayEngineStart();
        _co = null;
    }
}

