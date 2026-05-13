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
        NewRecord           // ★ 新增：是否刷新记录
    }

    [Header("显示内容")]
    [SerializeField] private DisplayType displayType = DisplayType.TotalScore;

    [Header("新记录显示")]
    [Tooltip("当 DisplayType 为 NewRecord 时，如果刷新记录显示这个物体")]
    [SerializeField] private GameObject newRecordObject;

    private TMP_Text textUI;
    private TextMeshPro text3D;
    private bool is3D;

    private void Awake()
    {
        text3D = GetComponent<TextMeshPro>();
        textUI = GetComponent<TMP_Text>();

        if (text3D != null)
        {
            is3D = true;
        }
        else if (textUI != null)
        {
            is3D = false;
        }
        else
        {
            Debug.LogError($"[{nameof(StatsDisplay)}] 未找到 TMP 组件！", this);
        }
    }

    private void Start()
    {
        if (GameStatsManager.Instance == null)
        {
            StartCoroutine(WaitForStatsManager());
        }
        else
        {
            UpdateDisplay();
        }
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
        
        if (GameStatsManager.Instance == null)
        {
            Debug.LogError("[StatsDisplay] 等待超时，GameStatsManager 未找到！");
            yield break;
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
            DisplayType.NewRecord              => HandleNewRecord(),  // ★ 新增
            _                                  => ""
        };

        if (displayType != DisplayType.NewRecord)
        {
            if (is3D)
                text3D.text = value;
            else
                textUI.text = value;
        }
    }

    /// <summary>
    /// 处理新记录显示：如果本次总分 > 之前的最高分，显示物体
    /// </summary>
    private string HandleNewRecord()
    {
        float currentScore = PlayerPrefs.GetFloat("GameStats_TotalScore", 0f);
        float highScore = PlayerPrefs.GetFloat("GameStats_HighScore", 0f);
        
        // 注意：如果当前分数等于最高分，且之前没有记录过（首次游戏），也算新记录
        bool isNewRecord = currentScore >= highScore && currentScore > 0;
        
        if (newRecordObject != null)
        {
            newRecordObject.SetActive(isNewRecord);
            Debug.Log($"[StatsDisplay] 新记录检测: 当前{currentScore}, 最高{highScore}, 结果:{isNewRecord}");
        }
        else
        {
            Debug.LogWarning("[StatsDisplay] newRecordObject 未赋值！");
        }

        return ""; // NewRecord 不需要返回文本
    }
}