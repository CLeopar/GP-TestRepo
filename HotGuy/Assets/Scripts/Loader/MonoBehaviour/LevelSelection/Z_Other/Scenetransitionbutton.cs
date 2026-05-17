using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 场景切换按钮组件（含悬浮右移效果）
/// 功能：
///   - 鼠标悬浮时按钮平滑右移，离开时平滑归位
///   - 鼠标悬浮时播放 Hover 音效
///   - 鼠标按下时播放 Click 音效
///   - 点击后屏幕渐黑（Fade Out），同时背景音乐音量同步淡出
///   - 渐黑完成后加载目标场景，过渡期间按钮自动锁定
///
/// 使用方法：
///   1. 将此脚本挂载到任意 Button 物体上
///   2. 在场景中创建一个覆盖全屏的黑色 Image（alpha 初始为 0），赋给 Fade Image
///   3. 在 Inspector 中填写目标场景名、音效、目标音源等字段
///   4. 确保目标场景已添加到 Build Settings
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneTransitionButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    // ──────────────────────────────────────────
    //  Inspector 可配置字段
    // ──────────────────────────────────────────

    [Header("场景设置")]
    [Tooltip("要切换到的目标场景名称（需已添加到 Build Settings）")]
    public string targetSceneName;

    [Header("渐黑设置")]
    [Tooltip("全屏遮罩 Image（黑色，初始 alpha = 0）")]
    public Image fadeImage;

    [Tooltip("渐黑持续时间（秒）")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.0f;

    [Header("音频 - 背景音乐淡出")]
    [Tooltip("需要淡出的背景音乐 AudioSource（留空则不处理）")]
    public AudioSource musicSource;

    [Header("音效配置")]
    [Tooltip("鼠标悬浮时播放的音效（留空则不播放）")]
    public AudioClip hoverSFX;

    [Tooltip("鼠标按下时播放的音效（留空则不播放）")]
    public AudioClip clickSFX;

    [Tooltip("音效播放用的 AudioSource（留空则自动创建）")]
    public AudioSource sfxSource;

    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float sfxVolume = 1.0f;

    [Header("悬浮右移效果")]
    [Tooltip("悬浮时向右偏移的距离（像素）")]
    public float slideDistance = 20f;

    [Tooltip("滑入 / 滑出的动画时长（秒）")]
    [Range(0.05f, 1f)]
    public float slideDuration = 0.15f;

    [Tooltip("动画曲线（EaseInOut 为默认，可自定义）")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ──────────────────────────────────────────
    //  私有变量
    // ──────────────────────────────────────────

    private Button _button;
    private RectTransform _rect;
    private Vector2 _originPosition;
    private Coroutine _slideCoroutine;
    private bool _isTransitioning = false;

    // ──────────────────────────────────────────
    //  Unity 生命周期
    // ──────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rect = GetComponent<RectTransform>();
        _originPosition = _rect.anchoredPosition;

        // 自动创建 SFX 用的 AudioSource（如果没有指定）
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        // 确保 FadeImage 初始完全透明且不阻挡点击
        if (fadeImage != null)
        {
            SetFadeAlpha(0f);
            fadeImage.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning("[SceneTransitionButton] 未指定 FadeImage，渐黑效果将不生效！");
        }
    }

    private void Start()
    {
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
    }

    // ──────────────────────────────────────────
    //  EventSystem 接口
    // ──────────────────────────────────────────

    /// <summary>鼠标进入：右移 + Hover 音效</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isTransitioning) return;
        PlaySFX(hoverSFX);
        StartSlide(_originPosition + new Vector2(slideDistance, 0f));
    }

    /// <summary>鼠标离开：归位</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isTransitioning) return;
        StartSlide(_originPosition);
    }

    /// <summary>鼠标按下：Click 音效</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isTransitioning) return;
        PlaySFX(clickSFX);
    }

    // ──────────────────────────────────────────
    //  按钮点击 → 开始过渡
    // ──────────────────────────────────────────

    private void OnButtonClicked()
    {
        if (_isTransitioning) return;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneTransitionButton] 未设置 targetSceneName！");
            return;
        }

        _isTransitioning = true;

        // 平滑归位后锁定，不再响应悬浮动画
        StartSlide(_originPosition);

        StartCoroutine(FadeAndLoadScene());
    }

    // ──────────────────────────────────────────
    //  协程：渐黑 + 音量淡出 → 加载场景
    // ──────────────────────────────────────────

    private IEnumerator FadeAndLoadScene()
    {
        float startMusicVolume = (musicSource != null) ? musicSource.volume : 0f;

        // 开启遮罩拦截，防止过渡期间重复点击
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            SetFadeAlpha(t);

            if (musicSource != null)
                musicSource.volume = Mathf.Lerp(startMusicVolume, 0f, t);

            yield return null;
        }

        // 确保最终值精准
        SetFadeAlpha(1f);
        if (musicSource != null)
            musicSource.volume = 0f;

        SceneManager.LoadScene(targetSceneName);
    }

    // ──────────────────────────────────────────
    //  悬浮右移：协程
    // ──────────────────────────────────────────

    private void StartSlide(Vector2 targetPos)
    {
        if (_slideCoroutine != null)
            StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideCoroutine(targetPos));
    }

    private IEnumerator SlideCoroutine(Vector2 targetPos)
    {
        Vector2 startPos = _rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            _rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, t);
            yield return null;
        }

        _rect.anchoredPosition = targetPos;
        _slideCoroutine = null;
    }

    // ──────────────────────────────────────────
    //  工具方法
    // ──────────────────────────────────────────

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}