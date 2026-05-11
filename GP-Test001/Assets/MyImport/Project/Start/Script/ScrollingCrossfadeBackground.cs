using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 斜45度无缝滚动 + 多张图渐变切换背景
///
/// 效果：
///   - 背景由小图块铺成网格，持续斜45度滚动
///   - 每隔 displayDuration 秒，整体渐变切换到下一张图
///
/// 使用方式：
///   1. Canvas 下创建空 GameObject，挂载此脚本
///   2. 在 Inspector 中赋值 backgrounds 数组（至少2张）
///   3. 调整 tileSize、speed、displayDuration 等参数
///
/// Canvas 模式：Screen Space - Overlay
/// </summary>
public class ScrollingCrossfadeBackground : MonoBehaviour
{
    // ─── 背景图片 ─────────────────────────────────────────────────────────────

    [Header("背景图片（至少 2 张）")]
    [Tooltip("轮播的背景图片数组，按顺序切换")]
    [SerializeField] private Sprite[] backgrounds;

    // ─── 图块尺寸与滚动 ───────────────────────────────────────────────────────

    [Header("图块与滚动")]
    [Tooltip("每个图片块的显示大小（像素）。原图 2048x2048，填 512 = 缩小4倍")]
    [SerializeField] private float tileSize = 512f;

    [Tooltip("滚动速度（像素/秒）")]
    [SerializeField] private float speed = 60f;

    [Tooltip("勾选后向右上滚动，不勾向左下滚动")]
    [SerializeField] private bool reverseDirection = false;

    // ─── 渐变切换 ─────────────────────────────────────────────────────────────

    [Header("渐变切换")]
    [Tooltip("每张图片的显示时长（秒）")]
    [SerializeField] private float displayDuration = 4f;

    [Tooltip("渐变过渡持续时间（秒）")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("渐变缓动曲线")]
    [SerializeField] private Ease fadeEase = Ease.InOutSine;

    // ─── 私有变量 ─────────────────────────────────────────────────────────────

    // 两个网格容器，交替作为上下层实现 crossfade
    private RectTransform containerA;
    private RectTransform containerB;
    private CanvasGroup cgA;
    private CanvasGroup cgB;

    private int currentIndex = 0;
    private bool isAOnTop = true;

    private float offset = 0f;
    private int cols;
    private int rows;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (backgrounds == null || backgrounds.Length < 2)
        {
            Debug.LogWarning("[ScrollingCrossfade] 至少需要 2 张背景图片");
            return;
        }

        // 计算网格行列数（多2格保证斜向滚动时边缘无空白）
        cols = Mathf.CeilToInt(Screen.width  / tileSize) + 2;
        rows = Mathf.CeilToInt(Screen.height / tileSize) + 2;

        // 创建两个网格容器
        containerA = BuildContainer("ContainerA", backgrounds[0]);
        containerB = BuildContainer("ContainerB", backgrounds[1 % backgrounds.Length]);

        cgA = containerA.gameObject.GetComponent<CanvasGroup>();
        cgB = containerB.gameObject.GetComponent<CanvasGroup>();

        // 初始状态：A 在上完全不透明，B 在下
        cgA.alpha = 1f;
        cgB.alpha = 1f;
        containerA.SetAsLastSibling();  // A 在上
        containerB.SetAsFirstSibling(); // B 在下

        // 等待后开始第一次切换
        DOVirtual.DelayedCall(displayDuration, StartNextTransition, ignoreTimeScale: true);
    }

    // ─── 构建网格容器 ─────────────────────────────────────────────────────────

    private RectTransform BuildContainer(string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        CanvasGroup cg = go.AddComponent<CanvasGroup>();

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot     = Vector2.zero;
        rt.sizeDelta = new Vector2(cols * tileSize, rows * tileSize);
        rt.anchoredPosition = new Vector2(-tileSize, -tileSize);

        // 填充图块
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject tile = new GameObject($"Tile_{col}_{row}");
                tile.transform.SetParent(rt, false);

                RectTransform tileRt = tile.AddComponent<RectTransform>();
                tileRt.anchorMin        = Vector2.zero;
                tileRt.anchorMax        = Vector2.zero;
                tileRt.pivot            = Vector2.zero;
                tileRt.sizeDelta        = new Vector2(tileSize, tileSize);
                tileRt.anchoredPosition = new Vector2(col * tileSize, row * tileSize);

                Image img = tile.AddComponent<Image>();
                img.sprite          = sprite;
                img.type            = Image.Type.Simple;
                img.preserveAspect  = false;
            }
        }

        return rt;
    }

    // ─── 更新容器内所有图块的图片 ─────────────────────────────────────────────

    private void UpdateContainerSprite(RectTransform container, Sprite sprite)
    {
        foreach (Transform child in container)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.sprite = sprite;
        }
    }

    // ─── 滚动更新 ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (containerA == null || containerB == null) return;

        float dir = reverseDirection ? 1f : -1f;
        offset += speed * Time.unscaledDeltaTime;

        if (offset >= tileSize)
            offset -= tileSize;

        Vector2 pos = new Vector2(
            -tileSize + dir * offset,
            -tileSize + dir * offset
        );

        // 两个容器始终保持相同位置，保证切换时没有位移跳动
        containerA.anchoredPosition = pos;
        containerB.anchoredPosition = pos;
    }

    // ─── 渐变切换逻辑 ─────────────────────────────────────────────────────────

    private void StartNextTransition()
    {
        int nextIndex      = (currentIndex + 1) % backgrounds.Length;
        int afterNextIndex = (currentIndex + 2) % backgrounds.Length;

        if (isAOnTop)
        {
            // A 在上 → 准备好 B 的下一张图，然后 A 淡出露出 B
            UpdateContainerSprite(containerB, backgrounds[nextIndex]);
            containerB.SetAsLastSibling();  // B 移到下层等待
            containerA.SetAsLastSibling();  // A 仍在最上淡出

            cgA.DOFade(0f, fadeDuration)
               .SetEase(fadeEase)
               .SetUpdate(true)
               .OnComplete(() =>
               {
                   currentIndex = nextIndex;
                   cgA.alpha = 1f;
                   UpdateContainerSprite(containerA, backgrounds[afterNextIndex]);
                   containerA.SetAsFirstSibling(); // A 沉到底层备用
                   isAOnTop = false;

                   DOVirtual.DelayedCall(displayDuration, StartNextTransition, ignoreTimeScale: true);
               });
        }
        else
        {
            // B 在上 → 准备好 A 的下一张图，然后 B 淡出露出 A
            UpdateContainerSprite(containerA, backgrounds[nextIndex]);
            containerA.SetAsLastSibling();  // A 移到下层等待
            containerB.SetAsLastSibling();  // B 仍在最上淡出

            cgB.DOFade(0f, fadeDuration)
               .SetEase(fadeEase)
               .SetUpdate(true)
               .OnComplete(() =>
               {
                   currentIndex = nextIndex;
                   cgB.alpha = 1f;
                   UpdateContainerSprite(containerB, backgrounds[afterNextIndex]);
                   containerB.SetAsFirstSibling(); // B 沉到底层备用
                   isAOnTop = true;

                   DOVirtual.DelayedCall(displayDuration, StartNextTransition, ignoreTimeScale: true);
               });
        }
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        DOTween.Kill(cgA);
        DOTween.Kill(cgB);
    }
}
