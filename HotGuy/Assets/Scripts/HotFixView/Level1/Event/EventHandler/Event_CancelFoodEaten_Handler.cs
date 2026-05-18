using Fantasy;
using Fantasy.Event;

public class Event_CancelFoodEaten_Handler : EventSystem<CancelFoodEaten>
{
    protected override void Handler(CancelFoodEaten self)
    {
        Log.Error($"CancelFoodEaten {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().CancelEatFruit(self.fruitId);
        
        // 任务状态回退
        var food = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().GetFruitComponent(self.fruitId);
        if (food == null) return;

        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        if (taskManager == null) return;

        foreach (var taskId in taskManager.ActiveTaskIds)
        {
            var taskComp = taskManager.GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) 
                continue;

            var currentItem = taskComp.GetCurrentItem();
            if (currentItem == null) continue;

            if (FoodTypeHelper.IsSameGroup(currentItem.FoodType, food.foodType) && currentItem.UIState == SCUIState.Eating)
            {
                currentItem.SetState(SCUIState.Normal);
                Log.Error($"[Task] Food {food.foodType} eating cancelled, back to Normal");
            }
        }
    }
}