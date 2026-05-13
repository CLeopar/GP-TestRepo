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

    private Transform[] tiles;
    private SpriteRenderer[] renderers;
    private float[] tileOffsets;
    private bool isHorizontal;
    private float totalLength;
    private BoxCollider2D bounds;

    void OnEnable()
    {
        if (renderers != null)
            StartCoroutine(FadeInAll());
    }

    void Start()
    {
        bounds = GetComponent<BoxCollider2D>();
        bounds.isTrigger = true; // 只做范围判断，不参与物理碰撞

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
        }

        tilePrefab.gameObject.SetActive(false);
        StartCoroutine(FadeInAll());
    }

    void Update()
    {
        // 每帧获取碰撞体在世界坐标中的边界
        // bounds.bounds 会自动跟随父级动画移动
        float boundsMin = isHorizontal
            ? bounds.bounds.min.x
            : bounds.bounds.min.y;

        float boundsMax = isHorizontal
            ? bounds.bounds.max.x
            : bounds.bounds.max.y;

        float delta = (isHorizontal ? scrollVelocity.x : scrollVelocity.y) * Time.deltaTime;

        for (int i = 0; i < tileCount; i++)
        {
            tileOffsets[i] += delta;

            // 当前Tile的世界坐标位置
            float tileWorldPos = (isHorizontal ? transform.position.x : transform.position.y)
                                 + tileOffsets[i];

            // 滚出左边/下边 → 接到右边/上边
            if (scrollVelocity.x < 0 || scrollVelocity.y < 0)
            {
                if (tileWorldPos + tileSize < boundsMin)
                    tileOffsets[i] += totalLength;
            }
            // 滚出右边/上边 → 接到左边/下边
            else
            {
                if (tileWorldPos - tileSize > boundsMax)
                    tileOffsets[i] -= totalLength;
            }

            // 每帧根据父级当前位置更新世界坐标
            tiles[i].position = isHorizontal
                ? new Vector3(transform.position.x + tileOffsets[i],
                              transform.position.y,
                              transform.position.z)
                : new Vector3(transform.position.x,
                              transform.position.y + tileOffsets[i],
                              transform.position.z);
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