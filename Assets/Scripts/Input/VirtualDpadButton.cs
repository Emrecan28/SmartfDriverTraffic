using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class VirtualDpadButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private VirtualDpad dpad;
    [SerializeField] private VirtualDpad.Dir direction;

    private bool _pressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dpad == null || _pressed) return;
        _pressed = true;
        dpad.Set(direction, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private void Release()
    {
        if (dpad == null || !_pressed) return;
        _pressed = false;
        dpad.Set(direction, false);
    }
}

