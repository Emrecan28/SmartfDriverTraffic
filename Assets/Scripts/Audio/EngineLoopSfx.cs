using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class EngineLoopSfx : MonoBehaviour
{
    private const string SoundEnabledKey = "sound_enabled";

    [SerializeField] private string gameplaySceneName = "Gameplayscene";
    [SerializeField] private AudioClip engineLoopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.15f;

    private AudioSource _src;

    private void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
    }

    private void Update()
    {
        if (_src == null) return;

        bool soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        string active = SceneManager.GetActiveScene().name;
        bool inGameplay = string.IsNullOrWhiteSpace(gameplaySceneName) ||
                          (!string.IsNullOrWhiteSpace(active) && active.StartsWith(gameplaySceneName, StringComparison.OrdinalIgnoreCase));
        bool running = Time.timeScale > 0f;
        bool shouldPlay = soundEnabled && inGameplay && running && engineLoopClip != null;

        if (!shouldPlay)
        {
            if (_src.isPlaying) _src.Stop();
            return;
        }

        _src.volume = volume;
        if (!_src.isPlaying)
        {
            _src.clip = engineLoopClip;
            _src.Play();
        }
    }
}

