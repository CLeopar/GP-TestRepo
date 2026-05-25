using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class InfiniteScrollButton : MonoBehaviour
{
    [Header("瓦片")]
    public GameObject tilePrefab;          // ← 改为 GameObject
    public int tileCount = 3;

    [Header("滚动")]
    public Vector2 scrollVelocity = new Vector2(-2f, 0f);
    public float tileSize = 10f;

    [Header("淡入效果")]
    public float fadeDuration = 1f;
    public float fadeStagger = 0.15f;

    [Header("悬停交互")]
    public float hoverSpeedMultiplier = 1f;
    public bool playOnlyOnHover = true;
    public float hoverFadeOutDuration = 0.3f;

    private Transform[] tiles;
    private SpriteRenderer[][] renderers;  // ← 每个tile对应一组SpriteRenderer
    private float[] tileOffsets;
    private bool isHorizontal;
    private float totalLength;
    private BoxCollider2D bounds;

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
        renderers = new SpriteRenderer[tileCount][];
        tileOffsets = new float[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = Instantiate(tilePrefab, transform);
            tileOffsets[i] = i * tileSize;
            tiles[i] = tile.transform;

            // 获取该tile下所有SpriteRenderer（包括自身和所有子物体）
            renderers[i] = tile.GetComponentsInChildren<SpriteRenderer>(true);

            // 初始隐藏
            foreach (var sr in renderers[i])
                SetAlpha(sr, 0f);
        }

        tilePrefab.SetActive(false);

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
        Vector2 targetVelocity = isHovering ? scrollVelocity * hoverSpeedMultiplier : Vector2.zero;

        if (!playOnlyOnHover)
            currentVelocity = scrollVelocity;
        else
            currentVelocity = targetVelocity;

        if (currentVelocity == Vector2.zero) return;

        float boundsMin = isHorizontal ? bounds.bounds.min.x : bounds.bounds.min.y;
        float boundsMax = isHorizontal ? bounds.bounds.max.x : bounds.bounds.max.y;
        float delta = (isHorizontal ? currentVelocity.x : currentVelocity.y) * Time.deltaTime;

        for (int i = 0; i < tileCount; i++)
        {
            tileOffsets[i] += delta;
            float tileWorldPos = (isHorizontal ? transform.position.x : transform.position.y) + tileOffsets[i];

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

    void OnMouseEnter()
    {
        isHovering = true;
        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);
        StartCoroutine(FadeInAll());
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (playOnlyOnHover)
            fadeOutCoroutine = StartCoroutine(FadeOutAll());
    }

    IEnumerator FadeInAll()
    {
        for (int i = 0; i < tileCount; i++)
            foreach (var sr in renderers[i])
                StartCoroutine(FadeTile(sr, i * fadeStagger, 1f, fadeDuration));
        yield break;
    }

    IEnumerator FadeOutAll()
    {
        for (int i = 0; i < tileCount; i++)
            foreach (var sr in renderers[i])
                StartCoroutine(FadeTile(sr, i * fadeStagger * 0.5f, 0f, hoverFadeOutDuration));
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

    public void SetVelocity(Vector2 v) => scrollVelocity = v;
    public void Stop() => scrollVelocity = Vector2.zero;

    public void SetHovering(bool hovering)
    {
        if (hovering) OnMouseEnter();
        else OnMouseExit();
    }
}