using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ButtonClickSoundHook : MonoBehaviour
{
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
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
    }
}

