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
            PlayerPrefs.Save();
        }

        // 渐黑后跳转场景
        var fadePanel = GameEntry.Instance._scene.GetComponent<FadePanelUIComponent>();
        if (fadePanel != null)
        {
            fadePanel.FadeIn(() =>
            {
                // 渐黑完成后跳转
                SceneManager.LoadScene(4);
            });
        }
        else
        {
            // 没有渐黑组件，直接跳转
            SceneManager.LoadScene(4);
        }
    }
}