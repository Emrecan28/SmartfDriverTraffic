using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LogoFunMotion : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private Vector2 positionAmplitude = new Vector2(18f, 14f);
    [SerializeField] private Vector2 positionFrequency = new Vector2(0.55f, 0.9f);

    [Header("Rotation")]
    [SerializeField] private float rotationAmplitudeDeg = 7f;
    [SerializeField] private float rotationFrequency = 0.6f;

    [Header("Scale")]
    [SerializeField] private float breatheAmplitude = 0.04f;
    [SerializeField] private float breatheFrequency = 0.9f;

    [Header("Pop")]
    [SerializeField] private float popEverySeconds = 2.4f;
    [SerializeField] private float popJitterSeconds = 0.7f;
    [SerializeField] private float popDurationSeconds = 0.22f;
    [SerializeField] private float popStrength = 0.14f;

    private RectTransform _rt;
    private Vector2 _baseAnchoredPos;
    private Vector3 _baseScale;

    private float _phase;
    private float _nextPopAt;
    private float _popStartAt = -1f;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseAnchoredPos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;
        _phase = Random.value * 1000f;
        ScheduleNextPop(Time.unscaledTime);
    }

    private void OnEnable()
    {
        if (_rt == null)
        {
            _rt = (RectTransform)transform;
            _baseAnchoredPos = _rt.anchoredPosition;
            _baseScale = _rt.localScale;
            _phase = Random.value * 1000f;
        }

        ScheduleNextPop(Time.unscaledTime);
        _popStartAt = -1f;
    }

    private void OnDisable()
    {
        if (_rt == null) return;
        _rt.anchoredPosition = _baseAnchoredPos;
        _rt.localRotation = Quaternion.identity;
        _rt.localScale = _baseScale;
    }

    private void Update()
    {
        if (_rt == null) return;

        float now = Time.unscaledTime;
        float t = now + _phase;

        if (popEverySeconds > 0f && now >= _nextPopAt)
        {
            _popStartAt = now;
            ScheduleNextPop(now);
        }

        float sx = Mathf.Sin(t * Mathf.PI * 2f * positionFrequency.x);
        float sy = Mathf.Sin(t * Mathf.PI * 2f * positionFrequency.y + 1.2f);
        Vector2 pos = _baseAnchoredPos + new Vector2(sx * positionAmplitude.x, sy * positionAmplitude.y);
        _rt.anchoredPosition = pos;

        float r = Mathf.Sin(t * Mathf.PI * 2f * rotationFrequency) * rotationAmplitudeDeg;
        _rt.localRotation = Quaternion.Euler(0f, 0f, r);

        float breathe = 1f + Mathf.Sin(t * Mathf.PI * 2f * breatheFrequency) * breatheAmplitude;

        float pop = 0f;
        if (_popStartAt >= 0f && popDurationSeconds > 0f)
        {
            float u = (now - _popStartAt) / popDurationSeconds;
            if (u >= 1f)
            {
                _popStartAt = -1f;
                pop = 0f;
            }
            else
            {
                float wave = Mathf.Sin(u * Mathf.PI);
                pop = wave * popStrength;
            }
        }

        float x = (breathe + pop);
        float y = (breathe - pop * 0.65f);
        _rt.localScale = new Vector3(_baseScale.x * x, _baseScale.y * y, _baseScale.z);
    }

    private void ScheduleNextPop(float now)
    {
        float jitter = popJitterSeconds <= 0f ? 0f : Random.Range(-popJitterSeconds, popJitterSeconds);
        _nextPopAt = now + Mathf.Max(0.05f, popEverySeconds + jitter);
    }
}

