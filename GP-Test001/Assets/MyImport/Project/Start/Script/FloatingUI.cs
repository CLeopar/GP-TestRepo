using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI 图片漂浮动画（气泡效果）
/// 挂载在任意带 RectTransform 的 UI GameObject 上即可。
///
/// 效果：
///   - 上下位移漂浮
///   - 微微缩放呼吸
///   - 轻微旋转摇摆（可选）
///   - 每个参数随机偏移，多个图片同时使用时不会完全同步，更自然
/// </summary>
public class FloatingUI : MonoBehaviour
{
    // ─── 漂浮位移 ─────────────────────────────────────────────────────────────

    [Header("漂浮位移")]
    [Tooltip("上下漂浮的最大距离（像素）")]
    [SerializeField] private float floatDistance = 12f;

    [Tooltip("一次上下漂浮的持续时间（秒）")]
    [SerializeField] private float floatDuration = 2f;

    [Tooltip("位移缓动曲线")]
    [SerializeField] private Ease floatEase = Ease.InOutSine;

    // ─── 缩放呼吸 ─────────────────────────────────────────────────────────────

    [Header("缩放呼吸")]
    [Tooltip("是否启用缩放呼吸效果")]
    [SerializeField] private bool enableScale = true;

    [Tooltip("缩放的最大幅度（例如 0.06 表示在原始大小 ±6% 之间变化）")]
    [SerializeField] private float scaleMagnitude = 0.06f;

    [Tooltip("一次缩放呼吸的持续时间（秒），建议与 floatDuration 接近但不完全相同以产生错位感")]
    [SerializeField] private float scaleDuration = 2.2f;

    [Tooltip("缩放缓动曲线")]
    [SerializeField] private Ease scaleEase = Ease.InOutSine;

    // ─── 旋转摇摆 ─────────────────────────────────────────────────────────────

    [Header("旋转摇摆（可选）")]
    [Tooltip("是否启用轻微旋转摇摆")]
    [SerializeField] private bool enableRotation = false;

    [Tooltip("旋转摇摆的最大角度")]
    [SerializeField] private float rotationMagnitude = 3f;

    [Tooltip("一次旋转来回的持续时间（秒）")]
    [SerializeField] private float rotationDuration = 3f;

    [Tooltip("旋转缓动曲线")]
    [SerializeField] private Ease rotationEase = Ease.InOutSine;

    // ─── 随机偏移 ─────────────────────────────────────────────────────────────

    [Header("随机时间偏移")]
    [Tooltip("开始动画前的随机延迟范围（秒）。多个图片同时存在时让它们不完全同步，更自然")]
    [SerializeField] private float randomDelayRange = 1f;

    // ─── 私有变量 ─────────────────────────────────────────────────────────────

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Vector3 originalRotation;

    private Tween floatTween;
    private Tween scaleTween;
    private Tween rotationTween;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalScale    = rectTransform.localScale;
        originalRotation = rectTransform.localEulerAngles;
    }

    private void Start()
    {
        // 随机延迟，让多个漂浮图片的节奏错开
        float delay = Random.Range(0f, randomDelayRange);

        DOVirtual.DelayedCall(delay, StartFloating, ignoreTimeScale: false);
    }

    // ─── 启动所有动画 ─────────────────────────────────────────────────────────

    private void StartFloating()
    {
        PlayFloatAnimation();

        if (enableScale)
            PlayScaleAnimation();

        if (enableRotation)
            PlayRotationAnimation();
    }

    // ─── 位移漂浮 ─────────────────────────────────────────────────────────────

    private void PlayFloatAnimation()
    {
        floatTween?.Kill();

        // 在原始位置基础上上下来回漂浮
        floatTween = rectTransform
            .DOAnchorPosY(originalPosition.y + floatDistance, floatDuration)
            .SetEase(floatEase)
            .SetLoops(-1, LoopType.Yoyo); // Yoyo = 上去再回来，无限循环
    }

    // ─── 缩放呼吸 ─────────────────────────────────────────────────────────────

    private void PlayScaleAnimation()
    {
        scaleTween?.Kill();

        Vector3 targetScale = originalScale * (1f + scaleMagnitude);

        scaleTween = rectTransform
            .DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ─── 旋转摇摆 ─────────────────────────────────────────────────────────────

    private void PlayRotationAnimation()
    {
        rotationTween?.Kill();

        Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, rotationMagnitude);

        rotationTween = rectTransform
            .DORotate(targetRotation, rotationDuration)
            .SetEase(rotationEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ─── 对外接口（需要时可暂停/恢复漂浮）────────────────────────────────────

    /// <summary>暂停所有漂浮动画，图片停在当前位置。</summary>
    public void PauseFloat()
    {
        floatTween?.Pause();
        scaleTween?.Pause();
        rotationTween?.Pause();
    }

    /// <summary>恢复所有漂浮动画。</summary>
    public void ResumeFloat()
    {
        floatTween?.Play();
        scaleTween?.Play();
        rotationTween?.Play();
    }

    /// <summary>
    /// 平滑归位到原始状态后停止动画。
    /// 适合在图片被点击或切换时调用。
    /// </summary>
    public void StopAndReset(float duration = 0.3f)
    {
        floatTween?.Kill();
        scaleTween?.Kill();
        rotationTween?.Kill();

        rectTransform.DOAnchorPos(originalPosition, duration).SetEase(Ease.OutSine);
        rectTransform.DOScale(originalScale, duration).SetEase(Ease.OutSine);
        rectTransform.DORotate(originalRotation, duration).SetEase(Ease.OutSine);
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        floatTween?.Kill();
        scaleTween?.Kill();
        rotationTween?.Kill();
        DOTween.Kill(rectTransform);
    }
}
