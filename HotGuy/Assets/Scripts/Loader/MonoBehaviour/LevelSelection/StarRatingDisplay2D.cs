using UnityEngine;

/// <summary>
/// 2D 世界空间的星级进度条系统
/// 挂载在包含进度条和星星的父物体上
/// </summary>
public class StarRatingDisplay2D : MonoBehaviour
{
    [Header("进度条")]
    [Tooltip("用于裁切的 Mask（必须作为 BarFill 的子物体）")]
    [SerializeField] private SpriteMask barMask;

    [Tooltip("Mask Scale.X 的最大值（满格时的缩放，可在 Inspector 自定义）")]
    [SerializeField] private float maxScaleX = 50f;

    [Tooltip("最高记录（对应 Mask Scale.X = maxScaleX）")]
    [SerializeField] private float maxScore = 1000f;

    [Header("星星设置")]
    [Tooltip("三颗星星的 SpriteRenderer")]
    [SerializeField] private SpriteRenderer[] starRenderers = new SpriteRenderer[3];

    [Tooltip("解锁后的星星 Sprite（亮色）")]
    [SerializeField] private Sprite starUnlockedSprite;

    [Tooltip("未解锁的星星 Sprite（灰色）")]
    [SerializeField] private Sprite starLockedSprite;

    [Header("分数阈值")]
    [Tooltip("第一颗星解锁分数")]
    [SerializeField] private float star1Threshold = 200f;

    [Tooltip("第二颗星解锁分数")]
    [SerializeField] private float star2Threshold = 400f;

    [Tooltip("第三颗星解锁分数")]
    [SerializeField] private float star3Threshold = 700f;

    private float[] thresholds;

    private void Awake()
    {
        thresholds = new float[] { star1Threshold, star2Threshold, star3Threshold };
    }

    private void Start()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void UpdateDisplay()
    {
        float currentScore = PlayerPrefs.GetFloat("GameStats_TotalScore", 0f);

        UpdateProgressBar(currentScore);
        UpdateStars(currentScore);

        Debug.Log($"[StarRating2D] 分数:{currentScore}, 满分:{maxScore}, MaxScaleX:{maxScaleX}");
    }

    private void UpdateProgressBar(float currentScore)
    {
        if (barMask == null) return;

        // 公式：Mask Scale.X = (当前分数 / 最高记录) * maxScaleX
        float ratio = Mathf.Clamp01(currentScore / maxScore);
        float targetScaleX = ratio * maxScaleX;

        Vector3 maskScale = barMask.transform.localScale;
        maskScale.x = targetScaleX;
        barMask.transform.localScale = maskScale;

        Debug.Log($"[StarRating2D] 比例:{ratio:P0}, Mask Scale.X:{targetScaleX} (最大:{maxScaleX})");
    }

    private void UpdateStars(float currentScore)
    {
        for (int i = 0; i < starRenderers.Length; i++)
        {
            if (starRenderers[i] == null) continue;

            bool unlocked = currentScore >= thresholds[i];
            starRenderers[i].sprite = unlocked ? starUnlockedSprite : starLockedSprite;
        }
    }

    // ========== 测试工具 ==========

    [ContextMenu("测试 0 分")]
    private void Test0() => TestScore(0f);

    [ContextMenu("测试 250 分")]
    private void Test250() => TestScore(250f);

    [ContextMenu("测试 500 分")]
    private void Test500() => TestScore(500f);

    [ContextMenu("测试 750 分")]
    private void Test750() => TestScore(750f);

    [ContextMenu("测试 1000 分")]
    private void Test1000() => TestScore(1000f);

    private void TestScore(float score)
    {
        PlayerPrefs.SetFloat("GameStats_TotalScore", score);
        UpdateDisplay();
    }
}