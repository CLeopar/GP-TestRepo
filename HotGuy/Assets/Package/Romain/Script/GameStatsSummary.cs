using System.Linq;
using UnityEngine;

/// <summary>
/// 全局统计管理器（单例，跨场景）
/// 其他脚本可通过 GameStatsManager.Instance 访问统计数据
/// 支持整局数据持久化（PlayerPrefs）
/// </summary>
public class GameStatsManager : MonoBehaviour
{
    // ───── 单例 ─────
    public static GameStatsManager Instance { get; private set; }

    // ───── 公开属性（供其他场景调用） ─────
    public float TotalScore           { get; private set; }
    public float AverageCompletion    { get; private set; }
    public int   CompletionBelow60Count { get; private set; }
    public int   CompletionAbove95Count { get; private set; }

    /// <summary>历史最高分（PlayerPrefs 持久化，关闭游戏后保留）</summary>
    public float HighScore { get; private set; }

    // ───── Inspector ─────
    [Header("Thresholds")]
    [SerializeField] private float lowThreshold  = 0.6f;
    [SerializeField] private float highThreshold = 0.95f;

    [Header("Debug Values (Runtime)")]
    [SerializeField] private float debugTotalScore;
    [SerializeField] private float debugAverageCompletion;
    [SerializeField] private int   debugBelow60;
    [SerializeField] private int   debugAbove95;
    [SerializeField] private float debugHighScore;

    // ───── PlayerPrefs Keys ─────
    private const string HighScoreKey           = "GameStats_HighScore";
    private const string TotalScoreKey          = "GameStats_TotalScore";
    private const string AverageCompletionKey   = "GameStats_AverageCompletion";
    private const string Below60Key             = "GameStats_Below60";
    private const string Above95Key             = "GameStats_Above95";

    // ───── 生命周期 ─────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 读取持久化数据
        LoadAllData();
    }

    private void OnEnable()
    {
        ScoreManager.OnScoreChanged   += Refresh;
        GameManager.OnLevelCompleted  += Refresh;
    }

    private void OnDisable()
    {
        ScoreManager.OnScoreChanged   -= Refresh;
        GameManager.OnLevelCompleted  -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    // ───── 核心逻辑 ─────

    /// <summary>
    /// 刷新所有统计数据。
    /// 由 ScoreManager.OnScoreChanged 或 GameManager.OnLevelCompleted 自动触发，
    /// 也可以在任意场景手动调用 GameStatsManager.Instance.Refresh()。
    /// </summary>
    public void Refresh()
    {
        if (GameManager.Instance == null || ScoreManager.Instance == null)
            return;

        var gm = GameManager.Instance;
        var sm = ScoreManager.Instance;

        // ── 总分 ──────────────────────────────────────────────
        TotalScore      = Mathf.Round(sm.TotalScore);
        debugTotalScore = TotalScore;

        // ── 最高分（仅在本局总分更高时覆盖）────────────────────
        if (TotalScore > HighScore)
        {
            HighScore = TotalScore;
            PlayerPrefs.SetFloat(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            debugHighScore = HighScore;
        }

        // ── 已游玩关卡（similarity > 0）──────────────────────
        var playedLevels = gm.LevelList
            .Where(l => l != null && l.similarity > 0f)
            .ToList();

        if (playedLevels.Count == 0)
        {
            AverageCompletion       = 0f;
            CompletionBelow60Count  = 0;
            CompletionAbove95Count  = 0;
        }
        else
        {
            AverageCompletion      = Mathf.Clamp01(playedLevels.Average(l => l.similarity));
            CompletionBelow60Count = playedLevels.Count(l => l.similarity <  lowThreshold);
            CompletionAbove95Count = playedLevels.Count(l => l.similarity >= highThreshold);
        }

        debugAverageCompletion = AverageCompletion;
        debugBelow60           = CompletionBelow60Count;
        debugAbove95           = CompletionAbove95Count;
    }

    // ───── 持久化保存/读取 ─────

    /// <summary>
    /// 保存当前整局数据到 PlayerPrefs（每关结束时调用）
    /// </summary>
    public void SaveSession()
    {
        PlayerPrefs.SetFloat(TotalScoreKey, TotalScore);
        PlayerPrefs.SetFloat(AverageCompletionKey, AverageCompletion);
        PlayerPrefs.SetInt(Below60Key, CompletionBelow60Count);
        PlayerPrefs.SetInt(Above95Key, CompletionAbove95Count);
        PlayerPrefs.Save(); // 立即写入硬盘
        
        // ★★★ 强制刷新所有 StatsDisplay ★★★
        var displays = FindObjectsOfType<StatsDisplay>();
        foreach (var display in displays)
            display.UpdateDisplay();
        
        Debug.Log($"[GameStatsManager] 会话数据已保存 | 总分:{TotalScore} 平均:{AverageCompletion * 100f:F1}% 低于60%:{CompletionBelow60Count} 高于95%:{CompletionAbove95Count}");
        
        // 刷新 2D 星级显示
        var starDisplays = FindObjectsOfType<StarRatingDisplay2D>();
        foreach (var display in starDisplays)
            display.UpdateDisplay();
    }

    /// <summary>
    /// 读取所有持久化数据（Awake 时调用）
    /// </summary>
    private void LoadAllData()
    {
        // 历史最高分
        HighScore      = PlayerPrefs.GetFloat(HighScoreKey, 0f);
        debugHighScore = HighScore;

        // 会话数据（可选：是否继承上次未完成的游戏？默认不自动加载，需要时手动调用 LoadSession）
        // LoadSession(); // 取消注释以启用自动续局功能
    }

    /// <summary>
    /// 读取上次会话数据（继续未完成的游戏）
    /// </summary>
    public void LoadSession()
    {
        TotalScore          = PlayerPrefs.GetFloat(TotalScoreKey, 0f);
        AverageCompletion   = PlayerPrefs.GetFloat(AverageCompletionKey, 0f);
        CompletionBelow60Count = PlayerPrefs.GetInt(Below60Key, 0);
        CompletionAbove95Count = PlayerPrefs.GetInt(Above95Key, 0);

        // 同步 debug 字段
        debugTotalScore        = TotalScore;
        debugAverageCompletion = AverageCompletion;
        debugBelow60           = CompletionBelow60Count;
        debugAbove95           = CompletionAbove95Count;

        Debug.Log($"[GameStatsManager] 已读取上次会话 | 总分:{TotalScore} 平均:{AverageCompletion * 100f:F1}%");
    }

    /// <summary>
    /// 清除会话数据（新游戏/重置按钮）
    /// </summary>
    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(TotalScoreKey);
        PlayerPrefs.DeleteKey(AverageCompletionKey);
        PlayerPrefs.DeleteKey(Below60Key);
        PlayerPrefs.DeleteKey(Above95Key);
        PlayerPrefs.Save();

        TotalScore          = 0f;
        AverageCompletion   = 0f;
        CompletionBelow60Count = 0;
        CompletionAbove95Count = 0;

        Refresh();

        Debug.Log("[GameStatsManager] 会话数据已清除");
    }

    /// <summary>
    /// 手动清除历史最高分（例如重置存档按钮）
    /// </summary>
    public void ResetHighScore()
    {
        HighScore = 0f;
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
        debugHighScore = 0f;
    }

    /// <summary>
    /// 清除所有存档数据（彻底重置）
    /// </summary>
    public void ClearAllData()
    {
        ClearSession();
        ResetHighScore();
        Debug.Log("[GameStatsManager] 所有数据已清除");
    }
}