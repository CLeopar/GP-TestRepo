using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_SCTaskCompleted_Handler : EventSystem<SCTaskCompleted>
{
    protected override void Handler(SCTaskCompleted self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            taskUI.PlayCompleteAnimation();
        }

        GameEntry.Instance._scene.GetComponent<LevelStatsComponent>()?.AddTaskCompleted();
        CalculateAndAddTaskBonus(self.TaskId);

        // 延迟移除，等动画播完
        GameEntry.Instance._scene.TimerComponent.Net.OnceTimer(1000, () =>
        {
            var manager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
            manager?.RemoveTask(self.TaskId);
        });
    }

    private void CalculateAndAddTaskBonus(long taskId)
    {
        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        var taskComp = taskManager?.GetComponent<TaskComponent>(taskId);
        if (taskComp == null) return;

        // 获取任务的持续时间类型（同一个任务所有 item 类型相同，取第一个即可）
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

        // 根据颜色应用倍率：橙色 x2，绿色 x1.5
        float multiplier = durationType == SCDurationType.Orange_8s 
            ? scoreConfig.TaskMultiplierOrange 
            : scoreConfig.TaskMultiplierGreen;
            
        int bonusScore = Mathf.RoundToInt(totalBaseScore * multiplier);

        // 直接加到总体分数
        scoreComp.AddScore(bonusScore);

        Log.Error($"[TaskBonus] Task {taskId} completed! Type: {durationType}, BaseScore: {totalBaseScore}, Multiplier: {multiplier}, Bonus: {bonusScore}");
    }
}