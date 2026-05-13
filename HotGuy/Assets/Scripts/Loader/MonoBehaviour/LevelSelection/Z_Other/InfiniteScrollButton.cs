using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class InfiniteScrollButton : MonoBehaviour
{
    [Header("瓦片")]
    public SpriteRenderer tilePrefab;
    public int tileCount = 3;

    [Header("滚动")]
    public Vector2 scrollVelocity = new Vector2(-2f, 0f);
    public float tileSize = 10f;

    [Header("淡入效果")]
    public float fadeDuration = 1f;
    public float fadeStagger = 0.15f;

    [Header("悬停交互")]
    [Tooltip("悬停时的滚动速度倍率（1=原速，0=停止，2=双倍速）")]
    public float hoverSpeedMultiplier = 1f;
    [Tooltip("是否只在悬停时播放动画")]
    public bool playOnlyOnHover = true;
    [Tooltip("鼠标离开时的淡出时间")]
    public float hoverFadeOutDuration = 0.3f;

    private Transform[] tiles;
    private SpriteRenderer[] renderers;
    private float[] tileOffsets;
    private bool isHorizontal;
    private float totalLength;
    private BoxCollider2D bounds;
    
    // 运行时状态
    private bool isHovering = false;
    private Vector2 currentVelocity;
    private Coroutine fadeOutCoroutine;

    void OnEnable()
    {
        if (renderers != null && !playOnlyOnHover)
            StartCoroutine(FadeInAll());
    }

    void Start()
    {
        bounds = GetComponent<BoxCollider2D>();
        bounds.isTrigger = true;

        isHorizontal = Mathf.Abs(scrollVelocity.x) >= Mathf.Abs(scrollVelocity.y);
        totalLength = tileSize * tileCount;
        tiles = new Transform[tileCount];
        renderers = new SpriteRenderer[tileCount];
        tileOffsets = new float[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            SpriteRenderer tile = Instantiate(tilePrefab, transform);
            tileOffsets[i] = i * tileSize;
            tiles[i] = tile.transform;
            renderers[i] = tile;
            // 初始隐藏
            SetAlpha(tile, 0f);
        }

        tilePrefab.gameObject.SetActive(false);
        
        // 如果不需要悬停才播放，直接开始
        if (!playOnlyOnHover)
        {
            currentVelocity = scrollVelocity;
            StartCoroutine(FadeInAll());
        }
        else
        {
            currentVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        // 计算实际速度
        Vector2 targetVelocity = isHovering ? scrollVelocity * hoverSpeedMultiplier : Vector2.zero;
        
        // 平滑过渡速度变化（可选，让启停更自然）
        if (!playOnlyOnHover)
            currentVelocity = scrollVelocity; // 非悬停模式下保持常速
        else
            currentVelocity = targetVelocity;

        // 速度为0时不更新位置，节省性能
        if (currentVelocity == Vector2.zero) return;

        float boundsMin = isHorizontal ? bounds.bounds.min.x : bounds.bounds.min.y;
        float boundsMax = isHorizontal ? bounds.bounds.max.x : bounds.bounds.max.y;
        float delta = (isHorizontal ? currentVelocity.x : currentVelocity.y) * Time.deltaTime;

        for (int i = 0; i < tileCount; i++)
        {
            tileOffsets[i] += delta;
            float tileWorldPos = (isHorizontal ? transform.position.x : transform.position.y) + tileOffsets[i];

            // 循环重置逻辑
            if (currentVelocity.x < 0 || currentVelocity.y < 0)
            {
                if (tileWorldPos + tileSize < boundsMin)
                    tileOffsets[i] += totalLength;
            }
            else
            {
                if (tileWorldPos - tileSize > boundsMax)
                    tileOffsets[i] -= totalLength;
            }

            tiles[i].position = isHorizontal
                ? new Vector3(transform.position.x + tileOffsets[i], transform.position.y, transform.position.z)
                : new Vector3(transform.position.x, transform.position.y + tileOffsets[i], transform.position.z);
        }
    }

    // ========== 鼠标交互事件 ==========
    
    void OnMouseEnter()
    {
        isHovering = true;
        
        // 停止之前的淡出
        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);
        
        // 开始淡入
        StartCoroutine(FadeInAll());
    }

    void OnMouseExit()
    {
        isHovering = false;
        
        // 如果设置了悬停才播放，鼠标离开时淡出
        if (playOnlyOnHover)
        {
            fadeOutCoroutine = StartCoroutine(FadeOutAll());
        }
    }

    // ========== 淡入淡出协程 ==========

    IEnumerator FadeInAll()
    {
        for (int i = 0; i < renderers.Length; i++)
            StartCoroutine(FadeTile(renderers[i], i * fadeStagger, 1f, fadeDuration));
        yield break;
    }

    IEnumerator FadeOutAll()
    {
        for (int i = 0; i < renderers.Length; i++)
            StartCoroutine(FadeTile(renderers[i], i * fadeStagger * 0.5f, 0f, hoverFadeOutDuration));
        yield break;
    }

    IEnumerator FadeTile(SpriteRenderer sr, float delay, float targetAlpha, float duration)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float startAlpha = sr.color.a;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(sr, Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }
        SetAlpha(sr, targetAlpha);
    }

    void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    // ========== 公共方法 ==========

    public void SetVelocity(Vector2 v) => scrollVelocity = v;
    public void Stop() => scrollVelocity = Vector2.zero;
    
    /// <summary>
    /// 强制设置悬停状态（可用于代码触发）
    /// </summary>
    public void SetHovering(bool hovering)
    {
        if (hovering) OnMouseEnter();
        else OnMouseExit();
    }
}