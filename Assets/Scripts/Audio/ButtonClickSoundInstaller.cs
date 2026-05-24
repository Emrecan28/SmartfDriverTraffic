using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ButtonClickSoundInstaller : MonoBehaviour
{
    private static ButtonClickSoundInstaller _instance;

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

    private void Start()
    {
        InstallIntoScene();
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
        InstallIntoScene();
    }

    private static void InstallIntoScene()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;
            if (b.GetComponent<ButtonClickSoundHook>() != null) continue;
            b.gameObject.AddComponent<ButtonClickSoundHook>();
        }
    }
}

