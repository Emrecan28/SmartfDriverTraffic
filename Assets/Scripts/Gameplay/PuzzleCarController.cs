using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PuzzleCarController : MonoBehaviour
{
    [SerializeField] private float stepDistance = 0.6f;
    [SerializeField] private float stepDurationSeconds = 0.12f;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private bool disableButtonsWhileMoving = true;
    [SerializeField] private bool showControlsOnStart = true;
    [SerializeField] private bool faceDirectionOnly = true;
    [SerializeField] private bool hideUnavailableDirections = true;

    private enum FacingDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    [SerializeField] private FacingDirection facingDirection = FacingDirection.Up;
    [SerializeField] private bool forceWorldSpaceCanvas = true;
    [SerializeField] private Vector3 controlsLocalOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector3 controlsWorldScale = new Vector3(0.01f, 0.01f, 0.01f);
    [SerializeField] private bool lockControlsToWorld = true;
    [SerializeField] private Vector2 arrowSize = new Vector2(80f, 80f);
    [SerializeField] private bool useRotatingSingleArrow = true;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private float arrowBaseRotationDeg;
    [SerializeField] private bool autoRefreshAvailability = true;
    [SerializeField] private float availabilityRefreshInterval = 0.12f;

    private Rigidbody2D _rb;
    private bool _moving;
    private bool _hasMoved;

    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    private ContactFilter2D _filter;
    private Button[] _buttons;

    private Canvas _controlsCanvas;
    private Button _up;
    private Button _down;
    private Button _left;
    private Button _right;
    private Button _arrowButton;
    private Vector2 _cachedFacingDir;
    private FacingDirection _lastFacingDirection;
    private bool _lastCanMove;
    private float _nextAvailabilityRefreshTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _lastFacingDirection = facingDirection;

        _filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };

        CacheButtons();
        WireButtons();
    }

    private void Start()
    {
        CacheButtons();
        WireButtons();

        if (_controlsCanvas != null)
        {
            if (forceWorldSpaceCanvas)
            {
                _controlsCanvas.renderMode = RenderMode.WorldSpace;
                _controlsCanvas.worldCamera = Camera.main;
            }

            if (lockControlsToWorld && forceWorldSpaceCanvas)
            {
                _controlsCanvas.transform.position = transform.position + ComputeControlsLocalOffset();
                _controlsCanvas.transform.rotation = Quaternion.identity;
            }
            else
            {
                _controlsCanvas.transform.localPosition = ComputeControlsLocalOffset();
                _controlsCanvas.transform.localRotation = Quaternion.identity;
            }
            _controlsCanvas.transform.localScale = ComputeCanvasLocalScale();

            _controlsCanvas.gameObject.SetActive(showControlsOnStart);
            _controlsCanvas.enabled = true;
        }
        NormalizeArrowUi();
        RefreshButtons();
        _lastFacingDirection = facingDirection;
        _lastCanMove = faceDirectionOnly && _cachedFacingDir != Vector2.zero && CanStep(_cachedFacingDir);
    }

    private void Update()
    {
        if (_moving) return;
        if (!faceDirectionOnly) return;

        if (facingDirection != _lastFacingDirection)
        {
            _lastFacingDirection = facingDirection;
            RefreshButtons();
            _lastCanMove = _cachedFacingDir != Vector2.zero && CanStep(_cachedFacingDir);
            _nextAvailabilityRefreshTime = Time.time + availabilityRefreshInterval;
            return;
        }

        if (!autoRefreshAvailability) return;
        if (Time.time < _nextAvailabilityRefreshTime) return;
        _nextAvailabilityRefreshTime = Time.time + availabilityRefreshInterval;

        if (_cachedFacingDir == Vector2.zero) _cachedFacingDir = QuantizeFacingDirection();
        bool canMove = _cachedFacingDir != Vector2.zero && CanStep(_cachedFacingDir);
        if (canMove == _lastCanMove) return;

        _lastCanMove = canMove;
        ApplyFacingArrow(canMove);
    }

    private void LateUpdate()
    {
        if (_controlsCanvas == null) return;
        if (!forceWorldSpaceCanvas) return;
        if (!lockControlsToWorld) return;

        _controlsCanvas.transform.position = transform.position + ComputeControlsLocalOffset();
        _controlsCanvas.transform.rotation = Quaternion.identity;
    }

    private void OnValidate()
    {
        if (stepDistance < 0.01f) stepDistance = 0.01f;
        if (stepDurationSeconds < 0.01f) stepDurationSeconds = 0.01f;
    }

    public void MoveUp() => TryStep(Vector2.up);
    public void MoveDown() => TryStep(Vector2.down);
    public void MoveLeft() => TryStep(Vector2.left);
    public void MoveRight() => TryStep(Vector2.right);
    public bool HasMoved => _hasMoved;

    private void CacheButtons()
    {
        _buttons = GetComponentsInChildren<Button>(true);
        _controlsCanvas = GetComponentInChildren<Canvas>(true);

        for (int i = 0; i < _buttons.Length; i++)
        {
            Button b = _buttons[i];
            if (b == null) continue;

            string n = b.gameObject.name.Trim().ToLowerInvariant();
            if (n == "up" || n == "yukari" || n == "yukarı") _up = b;
            else if (n == "down" || n == "asagi" || n == "aşağı") _down = b;
            else if (n == "left" || n == "sol") _left = b;
            else if (n == "right" || n == "sag" || n == "sağ") _right = b;
        }

        _arrowButton = _up != null ? _up : (_right != null ? _right : (_down != null ? _down : _left));
    }

    private void WireButtons()
    {
        if (faceDirectionOnly)
        {
            if (_up != null) { _up.onClick.RemoveAllListeners(); _up.onClick.AddListener(ArrowClickedUp); }
            if (_down != null) { _down.onClick.RemoveAllListeners(); _down.onClick.AddListener(ArrowClickedDown); }
            if (_left != null) { _left.onClick.RemoveAllListeners(); _left.onClick.AddListener(ArrowClickedLeft); }
            if (_right != null) { _right.onClick.RemoveAllListeners(); _right.onClick.AddListener(ArrowClickedRight); }
            return;
        }

        if (_up != null) { _up.onClick.RemoveAllListeners(); _up.onClick.AddListener(MoveUp); }
        if (_down != null) { _down.onClick.RemoveAllListeners(); _down.onClick.AddListener(MoveDown); }
        if (_left != null) { _left.onClick.RemoveAllListeners(); _left.onClick.AddListener(MoveLeft); }
        if (_right != null) { _right.onClick.RemoveAllListeners(); _right.onClick.AddListener(MoveRight); }
    }

    private void TryStep(Vector2 dir)
    {
        if (_moving) return;
        if (dir == Vector2.zero) return;

        _filter.layerMask = obstacleMask;
        int hitCount = _rb.Cast(dir, _filter, _hits, stepDistance);
        if (hitCount > 0) return;

        _hasMoved = true;
        Vector2 start = _rb.position;
        Vector2 target = start + dir * stepDistance;
        StartCoroutine(MoveStep(start, target));
    }

    private IEnumerator MoveStep(Vector2 start, Vector2 target)
    {
        _moving = true;
        SetButtonsInteractable(false);

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
        SetButtonsInteractable(true);
        RefreshButtons();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (!disableButtonsWhileMoving) return;
        if (_buttons == null) return;

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] != null) _buttons[i].interactable = interactable;
        }
    }

    private void RefreshButtons()
    {
        if (_moving) return;

        if (faceDirectionOnly)
        {
            _cachedFacingDir = QuantizeFacingDirection();
            bool canMove = _cachedFacingDir != Vector2.zero && CanStep(_cachedFacingDir);
            ApplyFacingArrow(canMove);
            return;
        }

        ApplyButtonState(_up, CanStep(Vector2.up));
        ApplyButtonState(_down, CanStep(Vector2.down));
        ApplyButtonState(_left, CanStep(Vector2.left));
        ApplyButtonState(_right, CanStep(Vector2.right));
    }

    private void ArrowClicked()
    {
        if (_moving) return;
        if (_cachedFacingDir == Vector2.zero) _cachedFacingDir = QuantizeFacingDirection();
        TryStep(_cachedFacingDir);
    }

    private void ArrowClickedUp() => ArrowClicked(Vector2.up);
    private void ArrowClickedDown() => ArrowClicked(Vector2.down);
    private void ArrowClickedLeft() => ArrowClicked(Vector2.left);
    private void ArrowClickedRight() => ArrowClicked(Vector2.right);

    private void ArrowClicked(Vector2 dir)
    {
        if (_moving) return;
        TryStep(dir);
    }

    private Vector2 QuantizeFacingDirection()
    {
        switch (facingDirection)
        {
            case FacingDirection.Right: return Vector2.right;
            case FacingDirection.Down: return Vector2.down;
            case FacingDirection.Left: return Vector2.left;
            default: return Vector2.up;
        }
    }

    private bool CanStep(Vector2 dir)
    {
        _filter.layerMask = obstacleMask;
        return _rb.Cast(dir, _filter, _hits, stepDistance) == 0;
    }

    private void ApplyButtonState(Button b, bool canMove)
    {
        if (b == null) return;

        b.interactable = canMove;
        if (hideUnavailableDirections) b.gameObject.SetActive(canMove);
        else b.gameObject.SetActive(true);
    }

    private void ApplyFacingArrow(bool canMove)
    {
        if (_up != null) _up.gameObject.SetActive(false);
        if (_down != null) _down.gameObject.SetActive(false);
        if (_left != null) _left.gameObject.SetActive(false);
        if (_right != null) _right.gameObject.SetActive(false);

        if (useRotatingSingleArrow)
        {
            if (_arrowButton == null) return;

            if (hideUnavailableDirections) _arrowButton.gameObject.SetActive(canMove);
            else _arrowButton.gameObject.SetActive(true);
            _arrowButton.interactable = canMove;

            float angle = 0f;
            if (_cachedFacingDir == Vector2.right) angle = -90f;
            else if (_cachedFacingDir == Vector2.left) angle = 90f;
            else if (_cachedFacingDir == Vector2.down) angle = 180f;
            else angle = 0f;

            RectTransform rt = _arrowButton.transform as RectTransform;
            if (rt != null) rt.localRotation = Quaternion.Euler(0f, 0f, angle + arrowBaseRotationDeg);
            return;
        }

        Button active = null;
        if (_cachedFacingDir == Vector2.up) active = _up;
        else if (_cachedFacingDir == Vector2.down) active = _down;
        else if (_cachedFacingDir == Vector2.left) active = _left;
        else if (_cachedFacingDir == Vector2.right) active = _right;

        if (active == null) return;

        if (hideUnavailableDirections) active.gameObject.SetActive(canMove);
        else active.gameObject.SetActive(true);
        active.interactable = canMove;
    }

    private void NormalizeArrowUi()
    {
        if (_controlsCanvas == null) return;

        bool center = faceDirectionOnly;
        NormalizeButtonRect(_up, center);
        NormalizeButtonRect(_down, center);
        NormalizeButtonRect(_left, center);
        NormalizeButtonRect(_right, center);

        if (faceDirectionOnly && useRotatingSingleArrow && _arrowButton != null)
        {
            RectTransform rt = _arrowButton.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            Image img = _arrowButton.targetGraphic as Image;
            if (img != null)
            {
                if (arrowSprite != null) img.sprite = arrowSprite;
                img.preserveAspect = false;
            }
        }

        if (faceDirectionOnly)
        {
            if (_up != null) _up.gameObject.SetActive(false);
            if (_down != null) _down.gameObject.SetActive(false);
            if (_left != null) _left.gameObject.SetActive(false);
            if (_right != null) _right.gameObject.SetActive(false);
        }
    }

    private void NormalizeButtonRect(Button b, bool center)
    {
        if (b == null) return;
        RectTransform rt = b.transform as RectTransform;
        if (rt == null) return;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (center) rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = arrowSize;
        rt.localScale = Vector3.one;
    }

    private Vector3 ComputeControlsLocalOffset()
    {
        return controlsLocalOffset;
    }

    private Vector3 ComputeCanvasLocalScale()
    {
        Vector3 s = transform.lossyScale;
        float sx = Mathf.Abs(s.x) < 0.0001f ? 0.0001f : s.x;
        float sy = Mathf.Abs(s.y) < 0.0001f ? 0.0001f : s.y;
        float sz = Mathf.Abs(s.z) < 0.0001f ? 0.0001f : s.z;

        return new Vector3(controlsWorldScale.x / sx, controlsWorldScale.y / sy, controlsWorldScale.z / sz);
    }
}

