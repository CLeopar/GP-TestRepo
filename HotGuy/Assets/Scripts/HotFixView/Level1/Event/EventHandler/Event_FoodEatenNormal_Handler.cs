using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_FoodBeEatenNormal_Handler : EventSystem<FoodBeEaten_Normal>
{
    protected override void Handler(FoodBeEaten_Normal self)
    {
        Log.Error($"FoodBeEaten_Normal {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().FoodBeEatenNormal();

        var food = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().GetFruitComponent(self.fruitId);
        if (food != null)
        {
            var score = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
            int foodScore = score.CalculateFoodScore(food.foodType);
        
            // 在食物位置加分
            Vector3 foodPos = food.Fruit_Tr?.position ?? Vector3.zero;
            score.AddScore(foodScore, self.fruitId, foodPos);
        }

        CheckTaskProgress(food?.foodType);
    
        // 统计吃食物
        GameEntry.Instance._scene.GetComponent<LevelStatsComponent>()?.AddFoodEaten();
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