using System.Linq;
using UnityEngine;

/// <summary>
/// 全局统计管理器（单例，跨场景）
/// 其他脚本可通过 GameStatsManager.Instance 访问统计数据
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

    // PlayerPrefs key
    private const string HighScoreKey = "GameStats_HighScore";

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

        // 读取历史最高分
        HighScore      = PlayerPrefs.GetFloat(HighScoreKey, 0f);
        debugHighScore = HighScore;
    }

    private void OnEnable()
    {
        // 场景加载时 ScoreManager / GameManager 可能还不存在，
        // 所以这里先订阅，等它们 Start 之后再触发
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

    /// <summary>
    /// 手动清除历史最高分（例如重置存档按钮）
    /// </summary>
    public void ResetHighScore()
    {
        HighScore = 0f;
        PlayerPrefs.DeleteKey(HighScoreKey);
        debugHighScore = 0f;
    }
}