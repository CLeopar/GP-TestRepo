using Fantasy;
using Fantasy.Event;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Event_LevelTimerFinished_Handler : EventSystem<LevelTimerFinished>
{
    protected override void Handler(LevelTimerFinished self)
    {
        Log.Error("[Timer] Level Finished!");

        // 保存数据
        var stats = GameEntry.Instance._scene.GetComponent<LevelStatsComponent>();
        var score = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
    
        if (stats != null && score != null)
        {
            stats.SaveToPlayerPrefs(score.CurrentScore);
            PlayerPrefs.Save(); // 强制立即写入磁盘
            Debug.Log($"[Save] L1 data saved. Score={score.CurrentScore}");
        }
        else
        {
            Debug.LogError($"[Save] Failed! stats={stats != null}, score={score != null}");
        }

        // 延迟跳转
        GameEntry.Instance._scene.TimerComponent.Net.OnceTimer(2000, () =>
        {
            SceneManager.LoadScene(4);
        });
    }
}