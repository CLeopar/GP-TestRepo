using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在场景里的 Button 上，点击后清除所有存档数据（新游戏）
/// 可选：清除后显示确认面板 / 跳转场景
/// </summary>
public class NewGameButton : MonoBehaviour
{
    [Header("绑定 Button（留空则自动获取自身）")]
    [SerializeField] private Button button;

    [Header("可选：清除后激活的确认提示 GameObject")]
    [SerializeField] private GameObject confirmPanel;

    [Header("可选：清除后跳转的场景名（留空则不跳转）")]
    [SerializeField] private string loadSceneName = "";

    [Header("可选：清除后延迟跳转秒数")]
    [SerializeField] private float loadSceneDelay = 0f;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnNewGameClicked);
        else
            Debug.LogError("[NewGameButton] 未找到 Button 组件！", this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnNewGameClicked);
    }

    private void OnNewGameClicked()
    {
        ClearAllData();

        if (confirmPanel != null)
            confirmPanel.SetActive(true);

        // 刷新场景内所有 StatsDisplay
        var displays = FindObjectsOfType<StatsDisplay>();
        foreach (var d in displays)
            d.UpdateDisplay();

        // 同步内存里的 GameStatsManager（如果存在）
        if (GameStatsManager.Instance != null)
            GameStatsManager.Instance.ResetForNewGame();

        if (!string.IsNullOrEmpty(loadSceneName))
        {
            if (loadSceneDelay > 0f)
                StartCoroutine(LoadSceneDelayed());
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(loadSceneName);
        }

        Debug.Log("[NewGameButton] 所有存档已清除，新游戏就绪");
    }

    private void ClearAllData()
    {
        // ── 全局统计 ──────────────────────────────────────────
        PlayerPrefs.DeleteKey("GameStats_TotalScore");
        PlayerPrefs.DeleteKey("GameStats_HighScore");
        PlayerPrefs.DeleteKey("GameStats_AverageCompletion");
        PlayerPrefs.DeleteKey("GameStats_Below60");
        PlayerPrefs.DeleteKey("GameStats_Above95");

        // ── 第一关专用 ────────────────────────────────────────
        PlayerPrefs.DeleteKey("L1_TotalScore");
        PlayerPrefs.DeleteKey("L1_HighScore");
        PlayerPrefs.DeleteKey("L1_TasksCompleted");
        PlayerPrefs.DeleteKey("L1_ShitEaten");
        PlayerPrefs.DeleteKey("L1_FoodEaten");

        // ── 关卡数据（Level_1 ~ Level_10）────────────────────
        for (int i = 1; i <= 10; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_TotalScore");
            PlayerPrefs.DeleteKey($"Level_{i}_HighScore");
            PlayerPrefs.DeleteKey($"Level_{i}_TasksCompleted");
            PlayerPrefs.DeleteKey($"Level_{i}_ShitEaten");
            PlayerPrefs.DeleteKey($"Level_{i}_FoodEaten");
        }

        PlayerPrefs.Save();
    }

    private System.Collections.IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(loadSceneDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(loadSceneName);
    }
}