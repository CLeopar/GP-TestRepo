using UnityEngine;

/// <summary>
/// 2D 世界空间的星级进度条系统
/// 支持三种数据模式：
/// 1. Level模式：Level_{id}_XXX（通用关卡）
/// 2. L1模式：L1_XXX（第一关特殊兼容）
/// 3. Global模式：GameStats_XXX（全局统计，如第二关）
/// </summary>
public class StarRatingDisplay2D : MonoBehaviour
{
    public enum DataMode
    {
        /// <summary>通用关卡模式：Level_{levelId}_XXX</summary>
        Level,
        /// <summary>第一关兼容模式：L1_XXX</summary>
        L1,
        /// <summary>全局统计模式：GameStats_XXX</summary>
        Global,
    }

    [Header("数据模式")]
    [Tooltip("数据存储模式")]
    [SerializeField] private DataMode dataMode = DataMode.L1;

    [Tooltip("关卡ID（仅 Level 模式使用，第一关=2，第二关=3）")]
    [SerializeField] private int levelId = 2;

    [Header("进度条")]
    [Tooltip("用于裁切的 Mask（必须作为 BarFill 的子物体）")]
    [SerializeField] private SpriteMask barMask;

    [Tooltip("Mask Scale.X 的最大值（满格时的缩放）")]
    [SerializeField] private float maxScaleX = 50f;

    [Tooltip("满分基准（用于进度条填满）")]
    [SerializeField] private float maxScore = 1000f;

    [Tooltip("进度条 Scale.X 最小值")]
    [SerializeField] private float minScaleX = 0f;

    [Header("星星设置")]
    [Tooltip("三颗星星的 SpriteRenderer")]
    [SerializeField] private SpriteRenderer[] starRenderers = new SpriteRenderer[3];

    [Tooltip("解锁后的星星 Sprite（亮色）")]
    [SerializeField] private Sprite starUnlockedSprite;

    [Tooltip("未解锁的星星 Sprite（灰色）")]
    [SerializeField] private Sprite starLockedSprite;

    [Header("分数阈值")]
    [SerializeField] private float star1Threshold = 200f;
    [SerializeField] private float star2Threshold = 400f;
    [SerializeField] private float star3Threshold = 700f;

    private float[] thresholds;

    // ========== 运行时只读属性 ==========
    [SerializeField, Header("运行时数据（只读）")]
    private DataMode _currentDataMode;
    [SerializeField]
    private int _currentLevelId;
    [SerializeField]
    private float _currentScore;
    [SerializeField]
    private float _currentHighScore;
    [SerializeField]
    private float _currentProgressRatio;

    public DataMode CurrentDataMode => dataMode;
    public int CurrentLevelId => levelId;
    public float CurrentScore => GetCurrentScore();
    public float CurrentHighScore => GetCurrentHighScore();
    public float CurrentProgressRatio => GetCurrentProgressRatio();

    private void Awake()
    {
        thresholds = new float[] { star1Threshold, star2Threshold, star3Threshold };
        _currentDataMode = dataMode;
        _currentLevelId = levelId;
        ValidateSetup();
    }

    private void Start()
    {
        UpdateDisplay();
    }

    private void OnEnable()
    {
        // PopUp 每次被激活时重新读取 PlayerPrefs，确保拿到最新分数
        UpdateDisplay();
    }

    private void Update()
    {
        _currentDataMode = dataMode;
        _currentLevelId = levelId;
        _currentScore = GetCurrentScore();
        _currentHighScore = GetCurrentHighScore();
        _currentProgressRatio = GetCurrentProgressRatio();
    }

    // ========== 核心：获取数据 ==========

    private float GetCurrentScore()
    {
        return dataMode switch
        {
            DataMode.L1 => PlayerPrefs.GetInt("L1_TotalScore", 0),
            DataMode.Level => PlayerPrefs.GetInt($"Level_{levelId}_TotalScore", 0),
            DataMode.Global => PlayerPrefs.GetFloat("GameStats_TotalScore", 0f),
            _ => 0f
        };
    }

    private float GetCurrentHighScore()
    {
        return dataMode switch
        {
            DataMode.L1 => PlayerPrefs.GetInt("L1_HighScore", 0),
            DataMode.Level => PlayerPrefs.GetInt($"Level_{levelId}_HighScore", 0),
            DataMode.Global => PlayerPrefs.GetFloat("GameStats_HighScore", 0f),
            _ => 0f
        };
    }

    private float GetCurrentProgressRatio()
    {
        float score = GetCurrentScore();
        return Mathf.Clamp01(score / maxScore);
    }

    // ========== 显示更新 ==========

    /// <summary>
    /// 刷新显示（读取 PlayerPrefs）
    /// </summary>
    public void UpdateDisplay()
    {
        float score = GetCurrentScore();
        UpdateProgressBar(score);
        UpdateStars(score);
        Debug.Log($"[StarRating2D] Mode:{dataMode} | 分数:{score}, 最高:{GetCurrentHighScore()}, 满分基准:{maxScore}");
    }

    /// <summary>
    /// 直接传入分数刷新（用于实时显示）
    /// </summary>
    public void UpdateDisplay(float currentScore)
    {
        UpdateProgressBar(currentScore);
        UpdateStars(currentScore);
    }

    /// <summary>
    /// 关卡结束时调用
    /// </summary>
    public void OnLevelEnd(float finalScore)
    {
        // 保存分数
        switch (dataMode)
        {
            case DataMode.L1:
                PlayerPrefs.SetInt("L1_TotalScore", Mathf.RoundToInt(finalScore));
                int prevHighL1 = PlayerPrefs.GetInt("L1_HighScore", 0);
                if (finalScore > prevHighL1)
                {
                    PlayerPrefs.SetInt("L1_HighScore", Mathf.RoundToInt(finalScore));
                    Debug.Log($"[StarRating2D] 🎉 L1 新纪录！{finalScore} > {prevHighL1}");
                }
                break;

            case DataMode.Level:
                string totalKey = $"Level_{levelId}_TotalScore";
                string highKey = $"Level_{levelId}_HighScore";
                PlayerPrefs.SetInt(totalKey, Mathf.RoundToInt(finalScore));
                int prevHighLevel = PlayerPrefs.GetInt(highKey, 0);
                if (finalScore > prevHighLevel)
                {
                    PlayerPrefs.SetInt(highKey, Mathf.RoundToInt(finalScore));
                    Debug.Log($"[StarRating2D] 🎉 Level {levelId} 新纪录！{finalScore} > {prevHighLevel}");
                }
                break;

            case DataMode.Global:
                PlayerPrefs.SetFloat("GameStats_TotalScore", finalScore);
                float prevHighGlobal = PlayerPrefs.GetFloat("GameStats_HighScore", 0f);
                if (finalScore > prevHighGlobal)
                {
                    PlayerPrefs.SetFloat("GameStats_HighScore", finalScore);
                    Debug.Log($"[StarRating2D] 🎉 Global 新纪录！{finalScore} > {prevHighGlobal}");
                }
                break;
        }

        PlayerPrefs.Save();
        UpdateDisplay(finalScore);
    }

    /// <summary>
    /// 设置数据模式
    /// </summary>
    public void SetDataMode(DataMode mode)
    {
        dataMode = mode;
        _currentDataMode = mode;
        UpdateDisplay();
    }

    /// <summary>
    /// 设置关卡ID（Level模式用）
    /// </summary>
    public void SetLevelId(int id)
    {
        levelId = id;
        _currentLevelId = id;
        if (dataMode == DataMode.Level)
            UpdateDisplay();
    }

    private void UpdateProgressBar(float currentScore)
    {
        if (barMask == null)
        {
            Debug.LogWarning("[StarRating2D] barMask 未赋值！", this);
            return;
        }

        float ratio = Mathf.Clamp01(currentScore / maxScore);
        float targetScaleX = Mathf.Max(ratio * maxScaleX, minScaleX);

        Vector3 maskScale = barMask.transform.localScale;
        maskScale.x = targetScaleX;
        barMask.transform.localScale = maskScale;
    }

    private void UpdateStars(float currentScore)
    {
        for (int i = 0; i < starRenderers.Length; i++)
        {
            if (starRenderers[i] == null || i >= thresholds.Length) continue;
            bool unlocked = currentScore >= thresholds[i];
            starRenderers[i].sprite = unlocked ? starUnlockedSprite : starLockedSprite;
        }
    }

    private void ValidateSetup()
    {
        if (barMask == null)
            Debug.LogError("[StarRating2D] 缺少 barMask！", this);
        if (starUnlockedSprite == null)
            Debug.LogWarning("[StarRating2D] 缺少亮星图片。", this);
        if (starLockedSprite == null)
            Debug.LogWarning("[StarRating2D] 缺少灰星图片。", this);
        if (maxScore <= 0f)
            Debug.LogError("[StarRating2D] maxScore 必须大于 0！", this);
    }

    // ========== 调试工具 ==========

    [ContextMenu("查看当前数据")]
    private void DebugShowData()
    {
        Debug.Log($"[StarRating2D] Mode:{dataMode} | 分数:{GetCurrentScore()}, 最高:{GetCurrentHighScore()}, 满分基准:{maxScore}");
        Debug.Log($"[StarRating2D] 阈值:⭐1={star1Threshold} ⭐2={star2Threshold} ⭐3={star3Threshold}");
    }

    [ContextMenu("清除当前数据")]
    private void ClearCurrentData()
    {
        switch (dataMode)
        {
            case DataMode.L1:
                PlayerPrefs.DeleteKey("L1_TotalScore");
                PlayerPrefs.DeleteKey("L1_HighScore");
                break;
            case DataMode.Level:
                PlayerPrefs.DeleteKey($"Level_{levelId}_TotalScore");
                PlayerPrefs.DeleteKey($"Level_{levelId}_HighScore");
                break;
            case DataMode.Global:
                PlayerPrefs.DeleteKey("GameStats_TotalScore");
                PlayerPrefs.DeleteKey("GameStats_HighScore");
                break;
        }
        PlayerPrefs.Save();
        UpdateDisplay();
    }

    [ContextMenu("测试 0 分")] private void Test0() => TestScore(0f);
    [ContextMenu("测试 250 分")] private void Test250() => TestScore(250f);
    [ContextMenu("测试 500 分")] private void Test500() => TestScore(500f);
    [ContextMenu("测试 750 分")] private void Test750() => TestScore(750f);
    [ContextMenu("测试 1000 分")] private void Test1000() => TestScore(1000f);

    private void TestScore(float score)
    {
        switch (dataMode)
        {
            case DataMode.L1:
                PlayerPrefs.SetInt("L1_TotalScore", (int)score);
                break;
            case DataMode.Level:
                PlayerPrefs.SetInt($"Level_{levelId}_TotalScore", (int)score);
                break;
            case DataMode.Global:
                PlayerPrefs.SetFloat("GameStats_TotalScore", score);
                break;
        }
        UpdateDisplay();
        Debug.Log($"[StarRating2D] 测试分数:{score}");
    }
}