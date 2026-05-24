using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class MusicPlayerController : MonoBehaviour
{
    private const string MusicEnabledKey = "music_enabled";

    [SerializeField] private string gameplaySceneName = "Gameplayscene";
    [SerializeField] private string levelSceneName = "LevelScenes";
    [SerializeField, Range(0f, 1f)] private float baseVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float levelSceneVolumeMultiplier = 0.6f;

    private static MusicPlayerController _instance;
    private AudioSource _src;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _src = GetComponent<AudioSource>();
        _src.loop = true;

        SceneManager.activeSceneChanged += HandleSceneChanged;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            _instance = null;
        }
    }

    private void Start()
    {
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneChanged(Scene from, Scene to)
    {
        ApplyForScene(to.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (_src == null) return;

        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        _src.mute = !musicEnabled;

        if (!musicEnabled)
        {
            if (_src.isPlaying) _src.Pause();
            return;
        }

        bool isGameplay = !string.IsNullOrWhiteSpace(gameplaySceneName) &&
                          !string.IsNullOrWhiteSpace(sceneName) &&
                          sceneName.StartsWith(gameplaySceneName, StringComparison.OrdinalIgnoreCase);
        if (isGameplay)
        {
            if (_src.isPlaying) _src.Pause();
            return;
        }

        _src.UnPause();
        if (!_src.isPlaying) _src.Play();

        float mult = (!string.IsNullOrWhiteSpace(levelSceneName) && sceneName == levelSceneName) ? levelSceneVolumeMultiplier : 1f;
        _src.volume = Mathf.Clamp01(baseVolume * mult);
    }
}

