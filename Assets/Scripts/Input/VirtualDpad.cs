using UnityEngine;

public sealed class VirtualDpad : MonoBehaviour
{
    public enum Dir
    {
        Up,
        Down,
        Left,
        Right
    }

    private int _up;
    private int _down;
    private int _left;
    private int _right;

    public Vector2 Read()
    {
        float x = (_right > 0 ? 1f : 0f) + (_left > 0 ? -1f : 0f);
        float y = (_up > 0 ? 1f : 0f) + (_down > 0 ? -1f : 0f);
        Vector2 v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    public void Set(Dir dir, bool pressed)
    {
        int delta = pressed ? 1 : -1;
        switch (dir)
        {
            case Dir.Up:
                _up = Mathf.Max(0, _up + delta);
                break;
            case Dir.Down:
                _down = Mathf.Max(0, _down + delta);
                break;
            case Dir.Left:
                _left = Mathf.Max(0, _left + delta);
                break;
            case Dir.Right:
                _right = Mathf.Max(0, _right + delta);
                break;
        }
    }
}

