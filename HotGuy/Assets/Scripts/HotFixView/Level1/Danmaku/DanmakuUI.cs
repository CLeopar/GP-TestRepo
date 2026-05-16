using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuUI : MonoBehaviour
{
    [Header("UI引用")]
    public Image AvatarImage;
    public TextMeshProUGUI ContentText;
    public RectTransform ContentImageRect;  // 拖入 ContentImage

    [Header("头像库")]
    public Sprite[] AvatarSprites;

    [Header("布局参数")]
    public float AvatarWidth = 100f;    // 头像宽度
    public float AvatarSpacing = 20f;   // 头像和气泡之间的间距

    [Header("动画参数")]
    public float SlideInDuration = 0.35f;
    public float SlideOutDuration = 0.25f;
    public float SlideOffsetY = 80f;

    private RectTransform _rect;
    private RectTransform _avatarRect;
    private CanvasGroup _canvasGroup;
    private Coroutine _moveCoroutine;
    private bool _isDestroying = false;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _avatarRect = AvatarImage.GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(DanmakuData data, float targetY)
    {
        ContentText.text = data.Content;

        if (AvatarSprites != null && AvatarSprites.Length > 0)
        {
            int index = Random.Range(0, AvatarSprites.Length);
            AvatarImage.sprite = AvatarSprites[index];
        }

        _rect.anchoredPosition = new Vector2(0, targetY - SlideOffsetY);
        _canvasGroup.alpha = 0f;

        StartCoroutine(LayoutThenSlideIn(targetY));
    }

    private IEnumerator LayoutThenSlideIn(float targetY)
    {
        // 等一帧让 ContentSizeFitter 算好气泡宽度
        yield return null;

        // 头像固定在最左边
        _avatarRect.anchorMin = new Vector2(0, 0.5f);
        _avatarRect.anchorMax = new Vector2(0, 0.5f);
        _avatarRect.pivot = new Vector2(0, 0.5f);
        _avatarRect.anchoredPosition = new Vector2(0, 0);
        _avatarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, AvatarWidth);
        _avatarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, AvatarWidth);

        // 气泡紧跟在头像右边
        if (ContentImageRect != null)
        {
            ContentImageRect.anchorMin = new Vector2(0, 0.5f);
            ContentImageRect.anchorMax = new Vector2(0, 0.5f);
            ContentImageRect.pivot = new Vector2(0, 0.5f);
            ContentImageRect.anchoredPosition = new Vector2(AvatarWidth + AvatarSpacing, 0);
        }

        PlaySlideIn(targetY);
    }

    /// <summary>
    /// 平滑移动到新目标位置（已在场景中的弹幕被新条目顶上去时调用）
    /// </summary>
    public void MoveTo(float targetY)
    {
        if (_isDestroying) return;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(SmoothMove(_rect.anchoredPosition.y, targetY, SlideInDuration));
    }

    /// <summary>
    /// 向上滑出并销毁（被顶出时调用）
    /// </summary>
    public void SlideOutAndDestroy()
    {
        if (_isDestroying) return;
        _isDestroying = true;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        StartCoroutine(SlideOutRoutine());
    }

    public void ForceDestroy()
    {
        _isDestroying = true;
        Destroy(gameObject);
    }

    // ── 内部协程 ─────────────────────────────────────────────

    private void PlaySlideIn(float targetY)
    {
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(SlideInRoutine(targetY));
    }

    private IEnumerator SlideInRoutine(float targetY)
    {
        float elapsed = 0f;
        float startY = _rect.anchoredPosition.y;

        while (elapsed < SlideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SlideInDuration);
            float eased = EaseOutCubic(t);

            _rect.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, eased));
            _canvasGroup.alpha = eased;
            yield return null;
        }

        _rect.anchoredPosition = new Vector2(0, targetY);
        _canvasGroup.alpha = 1f;
        _moveCoroutine = null;
    }

    private IEnumerator SmoothMove(float fromY, float toY, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            _rect.anchoredPosition = new Vector2(0, Mathf.Lerp(fromY, toY, eased));
            yield return null;
        }

        _rect.anchoredPosition = new Vector2(0, toY);
        _moveCoroutine = null;
    }

    private IEnumerator SlideOutRoutine()
    {
        float elapsed = 0f;
        float startY = _rect.anchoredPosition.y;
        float endY = startY + SlideOffsetY; // 向上滑出
        float startAlpha = _canvasGroup.alpha;

        while (elapsed < SlideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SlideOutDuration);
            float eased = EaseInCubic(t);

            _rect.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, eased));
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── 缓动函数 ──────────────────────────────────────────────

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t) => t * t * t;
}