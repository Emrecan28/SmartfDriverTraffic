using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class LoadSceneOnClick : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool setCurrentLevelBeforeLoad;
    [SerializeField] private int currentLevelValue = 1;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        if (setCurrentLevelBeforeLoad)
        {
            LevelProgression.SetCurrentLevel(Mathf.Max(1, currentLevelValue));
        }

        _button.interactable = false;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}

