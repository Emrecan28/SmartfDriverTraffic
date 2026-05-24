using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UIPoke : MonoBehaviour
{
    [SerializeField] private float intervalSeconds = 1.5f;
    [SerializeField] private float intervalJitterSeconds = 0.5f;
    [SerializeField] private float pokeDurationSeconds = 0.18f;

    [SerializeField] private float rotationDeg = 6f;
    [SerializeField] private float positionPx = 7f;
    [SerializeField] private float scaleAmount = 0.06f;

    private RectTransform _rt;
    private Vector2 _baseAnchoredPos;
    private Vector3 _baseScale;
    private Quaternion _baseLocalRotation;

    private float _phase;
    private float _nextAt;
    private float _startAt = -1f;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseAnchoredPos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;
        _baseLocalRotation = _rt.localRotation;
        _phase = Random.value * 1000f;
        ScheduleNext(Time.unscaledTime);
    }

    private void OnEnable()
    {
        if (_rt == null)
        {
            _rt = (RectTransform)transform;
            _baseAnchoredPos = _rt.anchoredPosition;
            _baseScale = _rt.localScale;
            _baseLocalRotation = _rt.localRotation;
            _phase = Random.value * 1000f;
        }

        ScheduleNext(Time.unscaledTime);
        _startAt = -1f;
    }

    private void OnDisable()
    {
        ResetToBase();
    }

    private void Update()
    {
        if (_rt == null) return;

        float now = Time.unscaledTime;
        if (intervalSeconds > 0f && now >= _nextAt)
        {
            _startAt = now;
            ScheduleNext(now);
        }

        if (_startAt < 0f || pokeDurationSeconds <= 0f)
        {
            ResetToBase();
            return;
        }

        float u = (now - _startAt) / pokeDurationSeconds;
        if (u >= 1f)
        {
            _startAt = -1f;
            ResetToBase();
            return;
        }

        float env = Mathf.Sin(u * Mathf.PI);
        float wig = Mathf.Sin((u * 4f + _phase) * Mathf.PI * 2f);

        float rot = wig * rotationDeg * env;
        _rt.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, rot);

        float offset = wig * positionPx * env;
        _rt.anchoredPosition = _baseAnchoredPos + new Vector2(offset, 0f);

        float pop = 1f + env * scaleAmount;
        _rt.localScale = new Vector3(_baseScale.x * pop, _baseScale.y * pop, _baseScale.z);
    }

    private void ResetToBase()
    {
        if (_rt == null) return;
        _rt.anchoredPosition = _baseAnchoredPos;
        _rt.localScale = _baseScale;
        _rt.localRotation = _baseLocalRotation;
    }

    private void ScheduleNext(float now)
    {
        float jitter = intervalJitterSeconds <= 0f ? 0f : Random.Range(-intervalJitterSeconds, intervalJitterSeconds);
        _nextAt = now + Mathf.Max(0.05f, intervalSeconds + jitter);
    }
}

