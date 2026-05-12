using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentSystem : MonoBehaviour
{
    [Header("数据池")]
    public string[] textPool;
    public Sprite[] avatarPool;

    [Header("3个槽位（左上对齐）")]
    public RectTransform slot1;
    public RectTransform slot2;
    public RectTransform slot3;

    [Header("Prefab")]
    public GameObject commentPrefab;

    [Header("生成频率")]
    public float minInterval = 1f;
    public float maxInterval = 2.5f;

    [Header("动画速度")]
    public float moveSpeed = 6f;

    [Header("气泡宽度参数（可调）")]
    public float charWidth = 14f;
    public float bubblePadding = 24f;
    public float minBubbleWidth = 80f;
    public float maxBubbleWidth = 260f;

    [Header("弹性动画")]
    public float fadeSpeed = 6f;
    public float moveDistanceUp = 60f;

    void Start()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            Spawn();
        }
    }

    void Spawn()
    {
        string text = textPool[Random.Range(0, textPool.Length)];
        Sprite avatar = avatarPool[Random.Range(0, avatarPool.Length)];

        Shift();
        Create(slot1, text, avatar);
    }

    // =========================
    // 核心移动
    // =========================
    void Shift()
    {
        if (slot3.childCount > 0)
        {
            RectTransform c3 = slot3.GetChild(0) as RectTransform;
            CanvasGroup cg3 = GetOrAddCG(c3);

            StartCoroutine(MoveUpAndDestroy(c3, cg3));
        }

        if (slot2.childCount > 0)
        {
            RectTransform c2 = slot2.GetChild(0) as RectTransform;

            c2.SetParent(slot3, false);
            c2.anchoredPosition = Vector2.zero;

            StartCoroutine(MoveToSlot(c2));
        }

        if (slot1.childCount > 0)
        {
            RectTransform c1 = slot1.GetChild(0) as RectTransform;

            c1.SetParent(slot2, false);
            c1.anchoredPosition = Vector2.zero;

            StartCoroutine(MoveToSlot(c1));
        }
    }

    // =========================
    // 创建
    // =========================
    void Create(RectTransform slot, string text, Sprite avatar)
    {
        GameObject obj = Instantiate(commentPrefab, slot, false);

        RectTransform rt = obj.GetComponent<RectTransform>();

        Image avatarImg = obj.transform.Find("Avatar").GetComponent<Image>();
        TextMeshProUGUI textUI = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        RectTransform bubble = obj.transform.Find("Bubble").GetComponent<RectTransform>();

        CanvasGroup cg = GetOrAddCG(rt);

        avatarImg.sprite = avatar;
        textUI.text = text;

        // =========================
        // ⭐ 文本左对齐（关键）
        // =========================
        textUI.alignment = TextAlignmentOptions.Left;

        // =========================
        // ⭐ 气泡左侧固定（关键）
        // =========================
        bubble.anchorMin = new Vector2(0f, 0.5f);
        bubble.anchorMax = new Vector2(0f, 0.5f);
        bubble.pivot     = new Vector2(0f, 0.5f);

        // =========================
        // 气泡宽度（完全可调）
        // =========================
        float width = text.Length * charWidth + bubblePadding;
        width = Mathf.Clamp(width, minBubbleWidth, maxBubbleWidth);

        bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        // 初始状态
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one * 0.85f;
        cg.alpha = 0f;

        StartCoroutine(FadeIn(rt, cg));
    }

    // =========================
    // 动画
    // =========================
    IEnumerator FadeIn(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;

            cg.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        cg.alpha = 1f;
        rt.localScale = Vector3.one;
    }

    IEnumerator MoveToSlot(RectTransform rt)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 target = Vector2.zero;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;

            rt.anchoredPosition = Vector2.Lerp(start, target, EaseOutCubic(t));

            yield return null;
        }

        rt.anchoredPosition = target;
    }

    IEnumerator MoveUpAndDestroy(RectTransform rt, CanvasGroup cg)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 target = start + Vector2.up * moveDistanceUp;

        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;

            rt.anchoredPosition = Vector2.Lerp(start, target, EaseOutCubic(t));
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        Destroy(rt.gameObject);
    }

    // =========================
    // 工具
    // =========================
    CanvasGroup GetOrAddCG(RectTransform rt)
    {
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}