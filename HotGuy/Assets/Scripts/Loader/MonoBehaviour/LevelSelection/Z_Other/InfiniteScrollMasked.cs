using UnityEngine;
using System.Collections;

/// <summary>
/// 按钮内无限滚动背景（SpriteMask裁剪）
///
/// 场景层级：
///   ButtonRoot                    ← 你的按钮物体（SpriteRenderer + Collider）
///     └── ScrollMasked            ← 空物体，挂此脚本
///           └── MaskObject        ← 空物体，挂 SpriteMask 组件，赋值给 maskRoot
///                                    Sprite 设为与按钮同形状的白色矩形Sprite
/// 脚本会在 MaskObject 下自动生成 Tile。
/// </summary>
public class InfiniteScrollMasked : MonoBehaviour
{
    [Header("瓦片")]
    [Tooltip("背景Sprite物体，脚本自动复制排列")]
    public SpriteRenderer tilePrefab;
    public int tileCount = 3;

    [Header("蒙版")]
    [Tooltip("挂有 SpriteMask 组件的子物体")]
    public Transform maskRoot;

    [Header("滚动")]
    public Vector2 scrollVelocity = new Vector2(-2f, 0f);
    public float tileSize = 5f;

    [Header("淡入效果")]
    public float fadeDuration = 1f;
    public float fadeStagger = 0.15f;

    private SpriteRenderer[] renderers;
    private bool isHorizontal;
    private float totalLength;
    private float scrollOffset;

    void OnEnable()
    {
        if (renderers != null)
            StartCoroutine(FadeInAll());
    }

    void Start()
    {
        isHorizontal = Mathf.Abs(scrollVelocity.x) >= Mathf.Abs(scrollVelocity.y);
        totalLength = tileSize * tileCount;
        scrollOffset = 0f;
        renderers = new SpriteRenderer[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            SpriteRenderer tile = Instantiate(tilePrefab, maskRoot);
            tile.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            tile.transform.localPosition = isHorizontal
                ? new Vector3(i * tileSize, 0, 0)
                : new Vector3(0, i * tileSize, 0);
            renderers[i] = tile;
        }

        tilePrefab.gameObject.SetActive(false);
        StartCoroutine(FadeInAll());
    }

    void Update()
    {
        // 只移动内容的 localPosition，maskRoot 本身永远不动
        scrollOffset += (isHorizontal ? scrollVelocity.x : scrollVelocity.y) * Time.deltaTime;

        // 将 offset 限制在一个 totalLength 范围内防止浮点数无限增长
        scrollOffset %= totalLength;

        for (int i = 0; i < tileCount; i++)
        {
            float pos = (i * tileSize + scrollOffset) % totalLength;

            // 处理负数取模（反方向滚动时）
            if (pos < 0) pos += totalLength;

            // 让排列从0开始向正方向延伸，滚出左边/下边的接到右边/上边
            if (pos > tileSize * (tileCount - 1) + tileSize * 0.5f)
                pos -= totalLength;

            renderers[i].transform.localPosition = isHorizontal
                ? new Vector3(pos, 0, 0)
                : new Vector3(0, pos, 0);
        }
    }

    IEnumerator FadeInAll()
    {
        for (int i = 0; i < renderers.Length; i++)
            StartCoroutine(FadeTile(renderers[i], i * fadeStagger));
        yield break;
    }

    IEnumerator FadeTile(SpriteRenderer sr, float delay)
    {
        SetAlpha(sr, 0f);
        if (delay > 0) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(sr, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(sr, 1f);
    }

    void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    public void SetVelocity(Vector2 v) => scrollVelocity = v;
    public void Stop() => scrollVelocity = Vector2.zero;
}