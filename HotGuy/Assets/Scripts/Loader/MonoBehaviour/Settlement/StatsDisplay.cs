using System.Collections;
using UnityEngine;
using TMPro;

public class StatsDisplay : MonoBehaviour
{
    public enum DisplayType
    {
        TotalScore,
        HighScore,
        AverageCompletion,
        CompletionBelow60Count,
        CompletionAbove95Count,
        NewRecord,
        L1_TotalScore,
        L1_HighScore,
        L1_TasksCompleted,
        L1_ShitEaten,
        L1_FoodEaten,
        L1_NewRecord,
    }

    [SerializeField] private DisplayType displayType = DisplayType.TotalScore;
    [SerializeField] private GameObject newRecordObject;

    private TMP_Text textUI;
    private TextMeshPro text3D;
    private bool is3D;

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
        // ========== 修改：L1 数据直接更新，不等待 GameStatsManager ==========
        if (IsL1DisplayType())
        {
            UpdateDisplay();
            return;
        }

        // 其他关卡保持原有逻辑
        if (GameStatsManager.Instance == null)
            StartCoroutine(WaitForStatsManager());
        else
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
            DisplayType.TotalScore             => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_TotalScore", 0f)).ToString(),
            DisplayType.HighScore              => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_HighScore", 0f)).ToString(),
            DisplayType.AverageCompletion      => Mathf.RoundToInt(PlayerPrefs.GetFloat("GameStats_AverageCompletion", 0f) * 100f).ToString() + "%",
            DisplayType.CompletionBelow60Count => PlayerPrefs.GetInt("GameStats_Below60", 0).ToString(),
            DisplayType.CompletionAbove95Count => PlayerPrefs.GetInt("GameStats_Above95", 0).ToString(),
            DisplayType.NewRecord              => HandleNewRecord(
                                                        PlayerPrefs.GetFloat("GameStats_TotalScore", 0f),
                                                        PlayerPrefs.GetFloat("GameStats_HighScore", 0f)),

            DisplayType.L1_TotalScore          => PlayerPrefs.GetInt("L1_TotalScore", 0).ToString(),
            DisplayType.L1_HighScore           => PlayerPrefs.GetInt("L1_HighScore", 0).ToString(),
            DisplayType.L1_TasksCompleted      => PlayerPrefs.GetInt("L1_TasksCompleted", 0).ToString(),
            DisplayType.L1_ShitEaten           => PlayerPrefs.GetInt("L1_ShitEaten", 0).ToString(),
            DisplayType.L1_FoodEaten           => PlayerPrefs.GetInt("L1_FoodEaten", 0).ToString(),
            DisplayType.L1_NewRecord           => HandleNewRecord(
                                                        PlayerPrefs.GetInt("L1_TotalScore", 0),
                                                        PlayerPrefs.GetInt("L1_HighScore", 0)),

            _                                  => ""
        };

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
        bool isNewRecord = currentScore >= highScore && currentScore > 0;
        
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
}