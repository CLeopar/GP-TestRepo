// Event_StartEatFood_Handler.cs
using Fantasy;
using Fantasy.Event;

public class Event_StartEatFood_Handler : EventSystem<StartEatFood>
{
    protected override void Handler(StartEatFood self)
    {
        Log.Error($"StartEatFood {self.fruitId}");
        
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().StartEatFruit(self.fruitId, self.isNormal);
        
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
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) continue;

            var currentItem = taskComp.GetCurrentItem();
            if (currentItem == null) continue;

            if (FoodTypeHelper.IsSameGroup(currentItem.FoodType, foodComp.foodType) && currentItem.UIState == SCUIState.Normal)
            {
                currentItem.SetState(SCUIState.Eating);
                continue;
            }

            bool hasProgress = taskComp.CurrentStep > 0 || currentItem.UIState != SCUIState.Normal;
            if (hasProgress)
            {
                ResetTaskProgress(taskComp);
            }
        }
    }

    private void ResetTaskProgress(TaskComponent taskComp)
    {
        taskComp.CurrentStep = 0;
        
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