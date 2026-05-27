using System.Linq;
using UnityEngine;

/// <summary>
/// 全局统计管理器（单例，跨场景）
/// </summary>
public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    public float TotalScore { get; private set; }
    public float AverageCompletion { get; private set; }
    public int CompletionBelow60Count { get; private set; }
    public int CompletionAbove95Count { get; private set; }
    public float HighScore { get; private set; }

    [Header("关卡ID（用于存 Level_{id}_TotalScore / HighScore）")]
    [Tooltip("第二关填 2，第三关填 3，以此类推")]
    [SerializeField] private int levelId = 2;

    [Header("Thresholds")]
    [SerializeField] private float lowThreshold = 0.6f;
    [SerializeField] private float highThreshold = 0.95f;

    [Header("Debug Values (Runtime)")]
    [SerializeField] private float debugTotalScore;
    [SerializeField] private float debugAverageCompletion;
    [SerializeField] private int debugBelow60;
    [SerializeField] private int debugAbove95;
    [SerializeField] private float debugHighScore;

    // PlayerPrefs Keys
    private const string HighScoreKey = "GameStats_HighScore";
    private const string TotalScoreKey = "GameStats_TotalScore";
    private const string AverageCompletionKey = "GameStats_AverageCompletion";
    private const string Below60Key = "GameStats_Below60";
    private const string Above95Key = "GameStats_Above95";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllData();
    }

    private void OnEnable()
    {
        ScoreManager.OnScoreChanged += Refresh;
        GameManager.OnLevelCompleted += Refresh;
    }

    private void OnDisable()
    {
        ScoreManager.OnScoreChanged -= Refresh;
        GameManager.OnLevelCompleted -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null || ScoreManager.Instance == null)
            return;

        var gm = GameManager.Instance;
        var sm = ScoreManager.Instance;

        TotalScore = Mathf.Round(sm.TotalScore);
        debugTotalScore = TotalScore;

        if (TotalScore > HighScore)
        {
            HighScore = TotalScore;
            PlayerPrefs.SetFloat(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            debugHighScore = HighScore;
        }

        var playedLevels = gm.LevelList
            .Where(l => l != null && l.similarity > 0f)
            .ToList();

        if (playedLevels.Count == 0)
        {
            AverageCompletion = 0f;
            CompletionBelow60Count = 0;
            CompletionAbove95Count = 0;
        }
        else
        {
            AverageCompletion = Mathf.Clamp01(playedLevels.Average(l => l.similarity));
            CompletionBelow60Count = playedLevels.Count(l => l.similarity < lowThreshold);
            CompletionAbove95Count = playedLevels.Count(l => l.similarity >= highThreshold);
        }

        debugAverageCompletion = AverageCompletion;
        debugBelow60 = CompletionBelow60Count;
        debugAbove95 = CompletionAbove95Count;
    }

    public void SaveSession()
    {
        // ── 先确保 TotalScore 与 ScoreManager 同步 ──────────────
        if (ScoreManager.Instance != null)
        {
            TotalScore = Mathf.Round(ScoreManager.Instance.TotalScore);
            debugTotalScore = TotalScore;

            if (TotalScore > HighScore)
            {
                HighScore = TotalScore;
                PlayerPrefs.SetFloat(HighScoreKey, HighScore);
                debugHighScore = HighScore;
            }
        }

        // ── 全局 key ────────────────────────────────────────────
        PlayerPrefs.SetFloat(TotalScoreKey, TotalScore);
        PlayerPrefs.SetFloat(AverageCompletionKey, AverageCompletion);
        PlayerPrefs.SetInt(Below60Key, CompletionBelow60Count);
        PlayerPrefs.SetInt(Above95Key, CompletionAbove95Count);

        // ── Level_{id} key（供 StarRatingDisplay2D Level模式 读取）─
        string totalKey = $"Level_{levelId}_TotalScore";
        string highKey  = $"Level_{levelId}_HighScore";
        PlayerPrefs.SetInt(totalKey, Mathf.RoundToInt(TotalScore));
        int prevHigh = PlayerPrefs.GetInt(highKey, 0);
        if (Mathf.RoundToInt(TotalScore) > prevHigh)
        {
            PlayerPrefs.SetInt(highKey, Mathf.RoundToInt(TotalScore));
            Debug.Log($"[GameStatsManager] 🎉 Level {levelId} 新纪录！{TotalScore} > {prevHigh}");
        }

        PlayerPrefs.Save();

        // 刷新所有 StatsDisplay
        var displays = FindObjectsOfType<StatsDisplay>();
        foreach (var display in displays)
            display.UpdateDisplay();

        Debug.Log($"[GameStatsManager] 会话数据已保存 | 总分:{TotalScore} 平均:{AverageCompletion * 100f:F1}%");

        // 刷新 2D 星级显示
        var starDisplays = FindObjectsOfType<StarRatingDisplay2D>();
        foreach (var display in starDisplays)
            display.UpdateDisplay();
    }

    private void LoadAllData()
    {
        HighScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
        debugHighScore = HighScore;
    }

    public void LoadSession()
    {
        TotalScore = PlayerPrefs.GetFloat(TotalScoreKey, 0f);
        AverageCompletion = PlayerPrefs.GetFloat(AverageCompletionKey, 0f);
        CompletionBelow60Count = PlayerPrefs.GetInt(Below60Key, 0);
        CompletionAbove95Count = PlayerPrefs.GetInt(Above95Key, 0);

        debugTotalScore = TotalScore;
        debugAverageCompletion = AverageCompletion;
        debugBelow60 = CompletionBelow60Count;
        debugAbove95 = CompletionAbove95Count;

        Debug.Log($"[GameStatsManager] 已读取上次会话 | 总分:{TotalScore} 平均:{AverageCompletion * 100f:F1}%");
    }

    /// <summary>
    /// 每次开始新一局游戏时调用（在 GameManager.Start 里调用）
    /// 只重置本局数据，不动最高分
    /// </summary>
    public void ResetForNewGame()
    {
        TotalScore = 0f;
        AverageCompletion = 0f;
        CompletionBelow60Count = 0;
        CompletionAbove95Count = 0;
        debugTotalScore = 0f;
        debugAverageCompletion = 0f;
        debugBelow60 = 0;
        debugAbove95 = 0;
        Debug.Log("[GameStatsManager] 新一局开始，本局数据已重置（最高分保留）");
    }

    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(TotalScoreKey);
        PlayerPrefs.DeleteKey(AverageCompletionKey);
        PlayerPrefs.DeleteKey(Below60Key);
        PlayerPrefs.DeleteKey(Above95Key);
        PlayerPrefs.Save();

        TotalScore = 0f;
        AverageCompletion = 0f;
        CompletionBelow60Count = 0;
        CompletionAbove95Count = 0;

        Refresh();

        Debug.Log("[GameStatsManager] 会话数据已清除");
    }

    public void ResetHighScore()
    {
        HighScore = 0f;
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
        debugHighScore = 0f;
    }

    public void ClearAllData()
    {
        ClearSession();
        ResetHighScore();

        // 同时清除所有关卡数据
        for (int i = 1; i <= 10; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_TotalScore");
            PlayerPrefs.DeleteKey($"Level_{i}_HighScore");
            PlayerPrefs.DeleteKey($"Level_{i}_TasksCompleted");
            PlayerPrefs.DeleteKey($"Level_{i}_ShitEaten");
            PlayerPrefs.DeleteKey($"Level_{i}_FoodEaten");
        }
        PlayerPrefs.Save();

        Debug.Log("[GameStatsManager] 所有数据已清除（包括关卡数据）");
    }
}