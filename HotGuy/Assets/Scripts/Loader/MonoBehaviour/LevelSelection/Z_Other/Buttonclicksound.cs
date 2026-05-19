using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载到含有 Button 组件的 GameObject 上。
/// 点击后播放音效，并触发"错误摇晃"动画，可选摇晃结束后向右平移。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [Header("音效设置")]
    [Tooltip("点击按钮时播放的音效")]
    public AudioClip clickSound;

    [Range(0f, 1f)]
    [Tooltip("音量大小（0 = 静音，1 = 最大）")]
    public float volume = 1f;

    [Header("摇晃动画设置")]
    [Tooltip("每次摇晃的水平偏移像素")]
    public float shakeStrength = 18f;

    [Tooltip("摇晃总持续时间（秒）")]
    public float shakeDuration = 0.45f;

    [Tooltip("摇晃次数（来回算一次）")]
    public int shakeCount = 5;

    [Header("摇晃后平移设置")]
    [Tooltip("摇晃结束后是否向右平移")]
    public bool enableSlideAfterShake = false;

    [Tooltip("平移距离（像素），正值向右，负值向左")]
    public float slideMoveDistance = 100f;

    [Tooltip("平移动画持续时间（秒）")]
    public float slideDuration = 0.3f;

    private Button _button;
    private AudioSource _audioSource;
    private RectTransform _rectTransform;
    private Coroutine _shakeCoroutine;
    private Vector2 _originalPosition;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        PlayClickSound();
        TriggerShake();
    }

    // ── 音效 ──────────────────────────────────────────────
    private void PlayClickSound()
    {
        if (clickSound == null)
        {
            Debug.LogWarning($"[ButtonClickSound] {gameObject.name} 上未设置 clickSound，请在 Inspector 中指定音频片段。");
            return;
        }
        _audioSource.PlayOneShot(clickSound, volume);
    }

    // ── 摇晃 ──────────────────────────────────────────────
    private void TriggerShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _rectTransform.anchoredPosition = _originalPosition;
        }
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        _originalPosition = _rectTransform.anchoredPosition;

        float elapsed = 0f;
        float stepTime = shakeDuration / (shakeCount * 2f);

        for (int i = 0; i < shakeCount * 2; i++)
        {
            float progress = elapsed / shakeDuration;
            float decay = 1f - progress;
            float direction = (i % 2 == 0) ? 1f : -1f;
            float targetX = _originalPosition.x + direction * shakeStrength * decay;

            float stepElapsed = 0f;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector2 endPos = new Vector2(targetX, _originalPosition.y);

            while (stepElapsed < stepTime)
            {
                stepElapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, stepElapsed / stepTime);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            elapsed += stepTime;
        }

        // 平滑归位
        float returnElapsed = 0f;
        float returnTime = stepTime;
        Vector2 returnStart = _rectTransform.anchoredPosition;
        while (returnElapsed < returnTime)
        {
            returnElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, returnElapsed / returnTime);
            _rectTransform.anchoredPosition = Vector2.Lerp(returnStart, _originalPosition, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = _originalPosition;

        // ── 可选：摇晃完毕后向右平移 ──────────────────────
        if (enableSlideAfterShake)
        {
            yield return StartCoroutine(SlideRoutine());
        }

        _shakeCoroutine = null;
    }

    // ── 平移 ──────────────────────────────────────────────
    private IEnumerator SlideRoutine()
    {
        Vector2 slideStart = _rectTransform.anchoredPosition;
        Vector2 slideEnd   = slideStart + new Vector2(slideMoveDistance, 0f);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            // EaseOutCubic：快起步、缓落定，有"弹出去"的感觉
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);
            _rectTransform.anchoredPosition = Vector2.Lerp(slideStart, slideEnd, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = slideEnd;
        // 平移后将原始位置同步更新，避免下次点击从旧位置归位
        _originalPosition = slideEnd;
    }
}