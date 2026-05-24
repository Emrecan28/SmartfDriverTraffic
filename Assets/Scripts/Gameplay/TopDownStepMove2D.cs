using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TopDownStepMove2D : MonoBehaviour
{
    [SerializeField] private float stepDistance = 0.6f;
    [SerializeField] private float stepDurationSeconds = 0.09f;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private VirtualDpad dpad;
    [SerializeField] private bool runUntilBlockedOnPress = true;
    [SerializeField] private float runStepIntervalSeconds;

    private Rigidbody2D _rb;
    private bool _moving;
    private Vector2 _prevDpadDir;
    private Coroutine _runCo;
    private Vector2 _runDir;
    private bool _stopAfterStep;

    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    private ContactFilter2D _filter;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };
    }

    private void OnValidate()
    {
        if (stepDistance < 0.01f) stepDistance = 0.01f;
        if (stepDurationSeconds < 0.01f) stepDurationSeconds = 0.01f;
    }

    private void Update()
    {
        Vector2 dir = ReadKeyboardStep();
        if (dir != Vector2.zero)
        {
            HandleInputDir(dir);
            return;
        }

        Vector2 dpadDir = ReadDpadStep();
        if (dpadDir == Vector2.zero) return;
        HandleInputDir(dpadDir);
    }

    public void StepUp() => TryStep(Vector2.up);
    public void StepDown() => TryStep(Vector2.down);
    public void StepLeft() => TryStep(Vector2.left);
    public void StepRight() => TryStep(Vector2.right);

    private static Vector2 ReadKeyboardStep()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return Vector2.zero;

        if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) return Vector2.up;
        if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) return Vector2.down;
        if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) return Vector2.left;
        if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) return Vector2.right;

        return Vector2.zero;
    }

    private Vector2 ReadDpadStep()
    {
        if (dpad == null) return Vector2.zero;

        Vector2 v = dpad.Read();
        Vector2 dir = Quantize(v);

        if (dir == Vector2.zero)
        {
            _prevDpadDir = Vector2.zero;
            return Vector2.zero;
        }

        if (_prevDpadDir == Vector2.zero || dir != _prevDpadDir)
        {
            _prevDpadDir = dir;
            return dir;
        }

        return Vector2.zero;
    }

    private void HandleInputDir(Vector2 dir)
    {
        if (runUntilBlockedOnPress)
        {
            if (_runCo != null)
            {
                if (Vector2.Dot(_runDir, dir) < -0.5f) { _stopAfterStep = true; return; }
                _runDir = dir;
                return;
            }

            _runDir = dir;
            _stopAfterStep = false;
            if (_runCo == null) _runCo = StartCoroutine(RunUntilBlocked());
            return;
        }

        TryStep(dir);
    }

    private IEnumerator RunUntilBlocked()
    {
        while (true)
        {
            while (_moving) yield return null;

            if (_stopAfterStep) break;

            Vector2 dir = _runDir;
            if (!CanStep(dir)) break;

            Vector2 start = _rb.position;
            Vector2 target = start + dir * stepDistance;
            yield return MoveStep(start, target);

            if (runStepIntervalSeconds > 0f)
            {
                float t = 0f;
                while (t < runStepIntervalSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        _runCo = null;
        _stopAfterStep = false;
    }

    private static Vector2 Quantize(Vector2 v)
    {
        if (v.sqrMagnitude < 0.25f) return Vector2.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            return v.x >= 0f ? Vector2.right : Vector2.left;
        }

        return v.y >= 0f ? Vector2.up : Vector2.down;
    }

    private void TryStep(Vector2 dir)
    {
        if (_moving) return;
        if (dir == Vector2.zero) return;

        if (!CanStep(dir)) return;

        Vector2 start = _rb.position;
        Vector2 target = start + dir * stepDistance;
        StartCoroutine(MoveStep(start, target));
    }

    private bool CanStep(Vector2 dir)
    {
        _filter.layerMask = obstacleMask;
        int hitCount = _rb.Cast(dir, _filter, _hits, stepDistance);
        if (hitCount == 0) return true;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D c = _hits[i].collider;
            if (c == null) continue;

            PuzzleCarController pc = c.GetComponentInParent<PuzzleCarController>();
            if (pc != null && !pc.HasMoved)
            {
                if (GameplayTimerAndLevelUI.Instance != null) GameplayTimerAndLevelUI.Instance.TriggerLose(LoseReason.Crashed);
                return false;
            }
        }

        return false;
    }

    private IEnumerator MoveStep(Vector2 start, Vector2 target)
    {
        _moving = true;

        float t = 0f;
        while (t < stepDurationSeconds)
        {
            t += Time.fixedDeltaTime;
            float u = Mathf.Clamp01(t / stepDurationSeconds);
            _rb.MovePosition(Vector2.LerpUnclamped(start, target, u));
            yield return new WaitForFixedUpdate();
        }

        _rb.MovePosition(target);
        _moving = false;
    }
}

