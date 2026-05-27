using System.Collections;
using UnityEngine;
using TMPro;

public class StatsDisplay : MonoBehaviour
{
    public enum DisplayType
    {
        // ========== 全局统计（第二关及后续使用）==========
        TotalScore,
        HighScore,
        AverageCompletion,
        CompletionBelow60Count,
        CompletionAbove95Count,
        NewRecord,

        // ========== 第一关专用（L1_ 前缀）==========
        L1_TotalScore,
        L1_HighScore,
        L1_TasksCompleted,
        L1_ShitEaten,
        L1_FoodEaten,
        L1_NewRecord,
    }

    [Header("显示类型")]
    [SerializeField] private DisplayType displayType = DisplayType.L1_TotalScore;

    [SerializeField] private GameObject newRecordObject;

    private TMP_Text textUI;
    private TextMeshPro text3D;
    private bool is3D;

    // 运行时只读显示
    [SerializeField, Header("运行时数据（只读）")]
    private DisplayType _currentDisplayType;
    [SerializeField]
    private string _currentValue;

    private void Awake()
    {
        text3D = GetComponent<TextMeshPro>();
        textUI = GetComponent<TMP_Text>();

        if (text3D != null)
            is3D = true;
        else if (textUI != null)
            is3D = false;
        else
            Debug.LogError($"[{nameof(StatsDisplay)}] 未找到 TMP 组件！", this);
    }

    private void Start()
    {
        _currentDisplayType = displayType;

        // L1 类型直接更新
        if (IsL1DisplayType())
        {
            UpdateDisplay();
            return;
        }

        // 全局统计类型：等待 GameStatsManager
        if (GameStatsManager.Instance == null)
            StartCoroutine(WaitForStatsManager());
        else
            UpdateDisplay();
    }

    private void OnEnable()
    {
        // 结算 Panel 被 SetActive(true) 激活时重新读取最新数据
        // Start() 只跑一次，OnEnable 每次激活都会跑，确保拿到 SaveSession 之后的值
        UpdateDisplay();
    }

    private bool IsL1DisplayType()
    {
        return displayType == DisplayType.L1_TotalScore
            || displayType == DisplayType.L1_HighScore
            || displayType == DisplayType.L1_TasksCompleted
            || displayType == DisplayType.L1_ShitEaten
            || displayType == DisplayType.L1_FoodEaten
            || displayType == DisplayType.L1_NewRecord;
    }

    private IEnumerator WaitForStatsManager()
    {
        float timeout = 3f;
        float timer = 0f;

        while (GameStatsManager.Instance == null && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (!is3D && textUI == null) return;
        if (is3D && text3D == null) return;

        string value = displayType switch
        {
            // ========== 全局统计（第二关）==========
            DisplayType.TotalScore => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_TotalScore", 0f)).ToString(),
            DisplayType.HighScore => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_HighScore", 0f)).ToString(),
            DisplayType.AverageCompletion => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_AverageCompletion", 0f) * 100f).ToString() + "%",
            DisplayType.CompletionBelow60Count => PlayerPrefs.GetInt("GameStats_Below60", 0).ToString(),
            DisplayType.CompletionAbove95Count => PlayerPrefs.GetInt("GameStats_Above95", 0).ToString(),
            DisplayType.NewRecord => HandleNewRecord(
                PlayerPrefs.GetFloat("GameStats_TotalScore", 0f),
                PlayerPrefs.GetFloat("GameStats_HighScore", 0f)),

            // ========== 第一关专用 ==========
            DisplayType.L1_TotalScore => PlayerPrefs.GetInt("L1_TotalScore", 0).ToString(),
            DisplayType.L1_HighScore => PlayerPrefs.GetInt("L1_HighScore", 0).ToString(),
            DisplayType.L1_TasksCompleted => PlayerPrefs.GetInt("L1_TasksCompleted", 0).ToString(),
            DisplayType.L1_ShitEaten => PlayerPrefs.GetInt("L1_ShitEaten", 0).ToString(),
            DisplayType.L1_FoodEaten => PlayerPrefs.GetInt("L1_FoodEaten", 0).ToString(),
            DisplayType.L1_NewRecord => HandleNewRecord(
                PlayerPrefs.GetInt("L1_TotalScore", 0),
                PlayerPrefs.GetInt("L1_HighScore", 0)),

            _ => ""
        };

        _currentValue = value;

        if (displayType != DisplayType.NewRecord && displayType != DisplayType.L1_NewRecord)
        {
            if (is3D)
                text3D.text = value;
            else
                textUI.text = value;
        }
    }

    private string HandleNewRecord(float currentScore, float highScore)
    {
        // 新纪录判断：严格大于（同分不算新纪录）
        bool isNewRecord = currentScore > highScore && currentScore > 0;

        if (newRecordObject != null)
            newRecordObject.SetActive(isNewRecord);
        else
            Debug.LogWarning("[StatsDisplay] newRecordObject 未赋值！");

        return "";
    }

    private string HandleNewRecord(int currentScore, int highScore)
    {
        return HandleNewRecord((float)currentScore, (float)highScore);
    }

    // ========== 调试工具 ==========

    [ContextMenu("查看当前数据")]
    private void DebugShowData()
    {
        if (IsL1DisplayType())
        {
            int total = PlayerPrefs.GetInt("L1_TotalScore", 0);
            int high = PlayerPrefs.GetInt("L1_HighScore", 0);
            int tasks = PlayerPrefs.GetInt("L1_TasksCompleted", 0);
            int shit = PlayerPrefs.GetInt("L1_ShitEaten", 0);
            int food = PlayerPrefs.GetInt("L1_FoodEaten", 0);
            Debug.Log($"[StatsDisplay] L1 | 总分:{total} 最高:{high} 任务:{tasks} 吃屎:{shit} 吃食物:{food}");
        }
        else
        {
            float total = PlayerPrefs.GetFloat("GameStats_TotalScore", 0f);
            float high = PlayerPrefs.GetFloat("GameStats_HighScore", 0f);
            float avg = PlayerPrefs.GetFloat("GameStats_AverageCompletion", 0f);
            Debug.Log($"[StatsDisplay] Global | 总分:{total} 最高:{high} 平均:{avg * 100f:F1}%");
        }
    }

    [ContextMenu("清除当前数据")]
    private void ClearCurrentData()
    {
        if (IsL1DisplayType())
        {
            PlayerPrefs.DeleteKey("L1_TotalScore");
            PlayerPrefs.DeleteKey("L1_HighScore");
            PlayerPrefs.DeleteKey("L1_TasksCompleted");
            PlayerPrefs.DeleteKey("L1_ShitEaten");
            PlayerPrefs.DeleteKey("L1_FoodEaten");
        }
        else
        {
            PlayerPrefs.DeleteKey("GameStats_TotalScore");
            PlayerPrefs.DeleteKey("GameStats_HighScore");
            PlayerPrefs.DeleteKey("GameStats_AverageCompletion");
            PlayerPrefs.DeleteKey("GameStats_Below60");
            PlayerPrefs.DeleteKey("GameStats_Above95");
        }
        PlayerPrefs.Save();
        UpdateDisplay();
    }
}