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

        // 如果当前步骤的食物类型匹配，设为 Eating
        if (currentItem.FoodType == foodComp.foodType && currentItem.UIState == SCUIState.Normal)
        {
            currentItem.SetState(SCUIState.Eating);
            Log.Error($"[Task] Food {foodComp.foodType} started eating, set to Eating state");
        }
    }
}
}