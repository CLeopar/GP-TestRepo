using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 鼠标悬停时，水平无限滚动 UI 瓦片（可选方向）；离开时停止并淡出。
/// 支持整体旋转角度。
/// </summary>
public class InfiniteScrollUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ─── Inspector 参数 ───────────────────────────────────────────────────────

    [Header("瓦片设置")]
    [Tooltip("瓦片预制体（Image）")]
    public Image tilePrefab;

    [Tooltip("生成瓦片数量")]
    public int tileCount = 8;

    [Tooltip("瓦片之间的纯空白间距（像素）")]
    [SerializeField] private float tileGap = 80f;

    [Header("滚动")]
    [Tooltip("滚动速度（像素/秒）")]
    public float scrollSpeed = 200f;

    [Tooltip("滚动方向：Right = 向右，Left = 向左")]
    public ScrollDirection scrollDirection = ScrollDirection.Right;

    [Header("整体旋转")]
    [Tooltip("所有瓦片父节点的旋转角度（度）。正值逆时针，负值顺时针")]
    [SerializeField] private float groupAngle = 0f;

    [Header("淡入淡出")]
    [Tooltip("淡入/淡出持续时间（秒）")]
    public float fadeDuration = 0.3f;

    [Header("父节点（可选）")]
    [Tooltip("瓦片挂载的父 RectTransform；留空则挂在自身下")]
    [SerializeField] private RectTransform tileParent;

    // ─── 枚举 ─────────────────────────────────────────────────────────────────

    public enum ScrollDirection { Left, Right }

    // ─── 私有字段 ─────────────────────────────────────────────────────────────

    private Image[]         _images;
    private RectTransform[] _rects;
    private float[]         _posX;
    private float           _tileW;
    private float           _tileH;
    private float           _step;
    private float           _totalLength;
    private bool            _scrolling;
    private RectTransform   _group;       // 旋转容器

    // ─── 生命周期 ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // ── 创建旋转容器 ──────────────────────────────────────────────────────
        // 所有瓦片放在同一个空 RectTransform 下，只旋转这个容器即可控制整体角度
        RectTransform baseParent = tileParent != null ? tileParent : (RectTransform)transform;

        GameObject groupGO = new GameObject("TileGroup", typeof(RectTransform));
        _group = groupGO.GetComponent<RectTransform>();
        _group.SetParent(baseParent, false);
        _group.anchorMin        = Vector2.zero;
        _group.anchorMax        = Vector2.zero;
        _group.pivot            = Vector2.zero;
        _group.anchoredPosition = Vector2.zero;
        _group.sizeDelta        = Vector2.zero;

        // 应用初始旋转角度
        ApplyGroupAngle();

        // ── 读取瓦片尺寸 ──────────────────────────────────────────────────────
        var prefabRect = tilePrefab.GetComponent<RectTransform>();
        _tileW = prefabRect.sizeDelta.x;
        _tileH = prefabRect.sizeDelta.y;

        _step        = _tileW + tileGap;
        _totalLength = _step * tileCount;

        // ── 生成瓦片 ──────────────────────────────────────────────────────────
        _images = new Image[tileCount];
        _rects  = new RectTransform[tileCount];
        _posX   = new float[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            Image tile = Instantiate(tilePrefab, _group);
            tile.gameObject.SetActive(true);
            tile.name = $"Tile_{i}";

            RectTransform rt = tile.GetComponent<RectTransform>();
            rt.anchorMin     = Vector2.zero;
            rt.anchorMax     = Vector2.zero;
            rt.pivot         = Vector2.zero;
            rt.sizeDelta     = new Vector2(_tileW, _tileH);

            _posX[i]         = i * _step;
            rt.localPosition = new Vector3(_posX[i], 0f, 0f);

            _images[i] = tile;
            _rects[i]  = rt;

            SetAlpha(tile, 0f);
        }

        tilePrefab.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_scrolling) return;

        // 根据方向决定符号：向右 +delta，向左 -delta
        float sign  = scrollDirection == ScrollDirection.Right ? 1f : -1f;
        float delta = sign * scrollSpeed * Time.deltaTime;

        for (int i = 0; i < tileCount; i++)
        {
            _posX[i] += delta;

            if (scrollDirection == ScrollDirection.Right)
            {
                // 瓦片完全移出右侧后接到最左边
                if (_posX[i] > _totalLength - _step)
                    _posX[i] -= _totalLength;
            }
            else
            {
                // 瓦片完全移出左侧后接到最右边
                if (_posX[i] + _tileW < 0f)
                    _posX[i] += _totalLength;
            }

            _rects[i].localPosition = new Vector3(_posX[i], 0f, 0f);
        }
    }

    // ─── 角度应用 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 将 groupAngle 应用到容器的 localRotation。
    /// 可在运行时随时调用（或通过编辑器回调）。
    /// </summary>
    private void ApplyGroupAngle()
    {
        if (_group != null)
            _group.localRotation = Quaternion.Euler(0f, 0f, groupAngle);
    }

    // 在编辑器中拖动 groupAngle 滑条时实时预览（Editor only）
#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyGroupAngle();
    }
#endif

    // ─── 悬停事件 ─────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        _scrolling = true;
        StopAllCoroutines();
        StartCoroutine(FadeAll(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _scrolling = false;
        StopAllCoroutines();
        StartCoroutine(FadeAll(0f));
    }

    // ─── 淡入淡出协程 ─────────────────────────────────────────────────────────

    private IEnumerator FadeAll(float target)
    {
        float[] startAlphas = new float[tileCount];
        for (int i = 0; i < tileCount; i++)
            startAlphas[i] = _images[i].color.a;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            for (int i = 0; i < tileCount; i++)
                SetAlpha(_images[i], Mathf.Lerp(startAlphas[i], target, t));
            yield return null;
        }

        for (int i = 0; i < tileCount; i++)
            SetAlpha(_images[i], target);
    }

    // ─── 工具方法 ─────────────────────────────────────────────────────────────

    private static void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}