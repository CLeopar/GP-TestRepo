using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("开场动画")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float slideUpDistance = 60f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Ease slideEase = Ease.OutBack;

    [Header("待机抖动")]
    [SerializeField] private float shakeStrength = 3f;
    [SerializeField] private float shakeDuration = 0.6f;

    [Header("Hover 交互")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverScaleDuration = 0.15f;
    [SerializeField] private float snapBackDuration = 0.12f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Tween shakeTween;
    private Vector2 originalPosition;
    private Vector3 originalRotation; // 新增：记录原始旋转

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localEulerAngles; // 记录原始旋转（含Z轴倾斜）
    }

    private void Start()
    {
        PlayAppearAnimation();
    }

    private void PlayAppearAnimation()
    {
        rectTransform.anchoredPosition = originalPosition + Vector2.down * slideUpDistance;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(appearDelay);
        sequence.Append(rectTransform.DOAnchorPos(originalPosition, slideDuration).SetEase(slideEase));
        sequence.Join(canvasGroup.DOFade(1f, fadeDuration));
        sequence.OnComplete(StartShaking);
    }

    private void StartShaking()
    {
        shakeTween?.Kill();

        // 在原始旋转的基础上来回偏移 shakeStrength 度
        shakeTween = rectTransform
            .DORotate(originalRotation + new Vector3(0f, 0f, shakeStrength), shakeDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopShaking()
    {
        shakeTween?.Kill();
        shakeTween = null;

        // 归位到原始旋转，而不是 Vector3.zero
        rectTransform.DORotate(originalRotation, snapBackDuration).SetEase(Ease.OutSine);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopShaking();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartShaking();
    }

    private void OnDestroy()
    {
        shakeTween?.Kill();
        DOTween.Kill(rectTransform);
    }
}