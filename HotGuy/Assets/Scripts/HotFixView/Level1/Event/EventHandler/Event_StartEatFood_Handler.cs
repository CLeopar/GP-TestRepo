using Fantasy;
using Fantasy.Event;

public class Event_StartEatFood_Handler : EventSystem<StartEatFood>
{
    protected override void Handler(StartEatFood self)
    {
        Log.Error($"StartEatFood {self.fruitId}");
        
        // 原有逻辑：通知 FoodManager 开始吃
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().StartEatFruit(self.fruitId, self.isNormal);
        
        // ========== 新增：通知任务系统 ==========
        NotifyTaskSystem(self.fruitId);
    }

    private void NotifyTaskSystem(long fruitId)
    {
        var foodManager = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>();
        var foodComp = foodManager?.GetFruitComponent(fruitId);
        if (foodComp == null) return;

        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        if (taskManager == null) return;

        foreach (var taskId in taskManager.ActiveTaskIds)
        {
            var taskComp = taskManager.GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) 
                continue;

            var currentItem = taskComp.GetCurrentItem();
            if (currentItem == null) continue;

            // 情况1：正好匹配当前步骤且未开始 → 设为 Eating（正常推进）
            // 用 IsSameGroup 比较，蓝莓A/B/C/D 视为同一种
            if (FoodTypeHelper.IsSameGroup(currentItem.FoodType, foodComp.foodType) && currentItem.UIState == SCUIState.Normal)
            {
                currentItem.SetState(SCUIState.Eating);
                Log.Error($"[Task] Food {foodComp.foodType} started eating, set to Eating state");
                continue;
            }

            // 情况2：不匹配当前步骤，且任务已有进度（CurrentStep>0 或当前在Eating）→ 重置整个任务进度
            // 倒计时继续走，不停止也不重启
            if (taskComp.CurrentStep > 0 || currentItem.UIState != SCUIState.Normal)
            {
                Log.Error($"[Task] Wrong food {foodComp.foodType} for task {taskId} (current step: {taskComp.CurrentStep}, expected: {currentItem.FoodType}). Resetting progress.");
                ResetTaskProgress(taskComp);
            }
        }
    }

    /// <summary>
    /// 重置任务所有进度：已完成/进行中的食物全部变回 Normal，步骤归零
    /// 倒计时保持原样继续计时，不停止也不重启
    /// </summary>
    private void ResetTaskProgress(TaskComponent taskComp)
    {
        // 步骤归零
        taskComp.CurrentStep = 0;
        
        // 遍历所有 item，把非 Normal 状态重置（Completed / Eating → Normal）
        foreach (var item in taskComp.ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
            {
                if (itemComp.UIState != SCUIState.Normal)
                {
                    itemComp.IsCompleted = false;
                    itemComp.SetState(SCUIState.Normal);
                }
            }
        }
    }
}