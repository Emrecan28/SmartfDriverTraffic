using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class UIAudioManager : MonoBehaviour
{
    private const string SoundEnabledKey = "sound_enabled";

    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;
    [SerializeField] private AudioClip crashClip;
    [SerializeField, Range(0f, 1f)] private float crashVolume = 1f;
    [SerializeField] private AudioClip loseClip;
    [SerializeField, Range(0f, 1f)] private float loseVolume = 1f;
    [SerializeField] private AudioClip winClip;
    [SerializeField, Range(0f, 1f)] private float winVolume = 1f;
    [SerializeField] private AudioClip hornClip;
    [SerializeField, Range(0f, 1f)] private float hornVolume = 1f;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField, Range(0f, 1f)] private float engineStartVolume = 1f;

    private static UIAudioManager _instance;
    private AudioSource _src;

    public static UIAudioManager Instance => _instance;

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
        _src.loop = false;
        _src.playOnAwake = false;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(clickClip, clickVolume);
    }

    public void PlayCrash()
    {
        if (crashClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(crashClip, crashVolume);
    }

    public void PlayLose()
    {
        if (loseClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(loseClip, loseVolume);
    }

    public void PlayWin()
    {
        if (winClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(winClip, winVolume);
    }

    public void PlayHorn()
    {
        if (hornClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(hornClip, hornVolume);
    }

    public void PlayEngineStart()
    {
        if (engineStartClip == null) return;
        if (PlayerPrefs.GetInt(SoundEnabledKey, 1) != 1) return;
        if (_src == null) return;
        _src.PlayOneShot(engineStartClip, engineStartVolume);
    }
}

