using Fantasy;
using Fantasy.Event;

public class Event_FoodBeEatenNormal_Handler : EventSystem<FoodBeEaten_Normal>
{
    protected override void Handler(FoodBeEaten_Normal self)
    {
        Log.Error($"FoodBeEaten_Normal {self.fruitId}");
        
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().FoodBeEatenNormal();
        
        // 计分
        var food = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().GetFruitComponent(self.fruitId);
        if (food != null)
        {
            var score = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
            score.AddScore(score.CalculateFoodScore(food.foodType), self.fruitId);
        }

        // 通知任务系统
        CheckTaskProgress(food?.foodType);
    }

    private void CheckTaskProgress(FoodType? eatenFoodType)
    {
        if (!eatenFoodType.HasValue) return;
        
        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        if (taskManager == null) return;

        foreach (var taskId in taskManager.ActiveTaskIds)
        {
            var taskComp = taskManager.GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) 
                continue;

            var currentItem = taskComp.GetCurrentItem();
            if (currentItem == null) continue;

            if (currentItem.FoodType == eatenFoodType.Value && currentItem.UIState == SCUIState.Eating)
            {
                currentItem.SetCompleted();
                taskComp.AdvanceStep();
                
                Log.Error($"[Task] Food {eatenFoodType.Value} eaten, task {taskId} advanced to step {taskComp.CurrentStep}");
            }
        }
    }
}