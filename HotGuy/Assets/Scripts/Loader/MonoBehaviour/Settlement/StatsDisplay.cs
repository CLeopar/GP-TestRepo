using UnityEngine;
using TMPro;

/// <summary>
/// 把 GameStatsManager 的数据同步显示到 TextMeshPro
/// 挂载在任意 TextMeshPro GameObject 上
/// </summary>
public class StatsDisplay : MonoBehaviour
{
    public enum DisplayType
    {
        TotalScore,
        HighScore,
        AverageCompletion,
        CompletionBelow60Count,
        CompletionAbove95Count
    }

    [Header("显示内容")]
    [Tooltip("选择要显示的数据类型")]
    [SerializeField] private DisplayType displayType = DisplayType.TotalScore;

    [Header("格式")]
    [Tooltip("显示前缀，例如 '总分：'")]
    [SerializeField] private string prefix = "";

    [Tooltip("显示后缀，例如 ' 分'")]
    [SerializeField] private string suffix = "";

    [Tooltip("小数位数（仅对浮点数生效）")]
    [SerializeField] private int decimalPlaces = 0;

    [Header("刷新频率")]
    [Tooltip("每秒刷新次数，0 = 只在 Start 时刷新一次")]
    [SerializeField] private float refreshRate = 0f;

    private TMP_Text text;
    private float timer;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        UpdateDisplay();
    }

    private void Update()
    {
        if (refreshRate <= 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / refreshRate)
        {
            timer = 0f;
            UpdateDisplay();
        }
    }

    // GameStatsManager.Instance.Refresh() 后手动调用，或者设 refreshRate > 0 自动刷新
    public void UpdateDisplay()
    {
        if (GameStatsManager.Instance == null || text == null) return;

        var stats = GameStatsManager.Instance;
        string value = displayType switch
        {
            DisplayType.TotalScore             => FormatFloat(stats.TotalScore),
            DisplayType.HighScore              => FormatFloat(stats.HighScore),
            DisplayType.AverageCompletion      => FormatFloat(stats.AverageCompletion * 100f) + "%",
            DisplayType.CompletionBelow60Count => stats.CompletionBelow60Count.ToString(),
            DisplayType.CompletionAbove95Count => stats.CompletionAbove95Count.ToString(),
            _                                  => ""
        };

        text.text = prefix + value + suffix;
    }

    private string FormatFloat(float value)
    {
        return value.ToString($"F{decimalPlaces}");
    }
}
