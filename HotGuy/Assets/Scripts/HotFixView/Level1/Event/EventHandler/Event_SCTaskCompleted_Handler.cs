using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_SCTaskCompleted_Handler : EventSystem<SCTaskCompleted>
{
    protected override void Handler(SCTaskCompleted self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        // 先拿到 UI 位置再播动画
        Vector3 bonusWorldPos = Vector3.zero;
        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            Log.Error($"[SCTaskCompleted] TaskUI found, playing success animation. RootContainer is {(taskUI.RootContainer == null ? "NULL" : "OK")}");
            
            // 用 SCTaskUI 的屏幕坐标作为飘字位置
            bonusWorldPos = taskUI.transform.position;
            taskUI.PlaySuccessAnimation();
        }
        else
        {
            Log.Error($"[SCTaskCompleted] TaskUI NOT found for TaskId: {self.TaskId}");
        }

        GameEntry.Instance._scene.GetComponent<LevelStatsComponent>()?.AddTaskCompleted();
        
        // 播放任务完成音效
        GameEntry.Instance._scene.EventComponent.Publish(new PlaySFX
        {
            Type = SFXType.Complete,
            WorldPos = bonusWorldPos
        });
        
        CalculateAndAddTaskBonus(self.TaskId, bonusWorldPos);

        // 延迟移除，等动画播完（成功动画约 0.12+0.08+0.1+0.25+0.2 = 0.75s，1s足够）
        GameEntry.Instance._scene.TimerComponent.Net.OnceTimer(1000, () =>
        {
            var manager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
            manager?.RemoveTask(self.TaskId, silent: true);
        });
    }

    private void CalculateAndAddTaskBonus(long taskId, Vector3 worldPos)
    {
        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        var taskComp = taskManager?.GetComponent<TaskComponent>(taskId);
        if (taskComp == null) return;

        // 获取任务的持续时间类型
        SCDurationType durationType = SCDurationType.Green_10s;
        foreach (var item in taskComp.ForEachMultiEntity)
        {
            if (item is SCItemComponent scItem)
            {
                durationType = scItem.DurationType;
                break;
            }
        }

        // 计算任务中所有食物的基础分总和
        var scoreComp = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        
        int totalBaseScore = 0;
        foreach (var foodType in taskComp.FoodSequence)
        {
            totalBaseScore += scoreComp.CalculateFoodScore(foodType);
        }

        // 根据颜色应用倍率
        float multiplier = durationType == SCDurationType.Orange_8s 
            ? scoreConfig.TaskMultiplierOrange 
            : scoreConfig.TaskMultiplierGreen;
            
        int bonusScore = Mathf.RoundToInt(totalBaseScore * multiplier);

        // 在 SCTaskUI 位置显示飘字
        scoreComp.AddScore(bonusScore, taskId, worldPos);

        Log.Error($"[TaskBonus] Task {taskId} completed! Type: {durationType}, BaseScore: {totalBaseScore}, Multiplier: {multiplier}, Bonus: {bonusScore}");
    }
}