using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_FoodBeEatenNormal_Handler : EventSystem<FoodBeEaten_Normal>
{
    protected override void Handler(FoodBeEaten_Normal self)
    {
        Log.Error($"FoodBeEaten_Normal {self.fruitId}");
        
        var dogCtrl = GameEntry.Instance._scene.GetComponent<DogControlComponent>();
        if (dogCtrl != null)
            dogCtrl.PauseEatSfx();

        dogCtrl?.FoodBeEatenNormal();

        var food = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().GetFruitComponent(self.fruitId);
        if (food != null)
        {
            var score = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
            int foodScore = score.CalculateFoodScore(food.foodType);
            Vector3 foodPos = food.Fruit_Tr?.position ?? Vector3.zero;
            score.AddScore(foodScore, self.fruitId, foodPos);
        }

        CheckTaskProgress(food?.foodType);

        GameEntry.Instance._scene.GetComponent<LevelStatsComponent>()?.AddFoodEaten();
    }

    private void CheckTaskProgress(FoodType? eatenFoodType)
    {
        if (eatenFoodType == null || eatenFoodType == FoodType.None) return;

        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        if (taskManager == null) return;

        foreach (var taskId in taskManager.ActiveTaskIds)
        {
            var taskComp = taskManager.GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) continue;

            var currentItem = taskComp.GetCurrentItem();
            if (currentItem == null) continue;

            if (currentItem.UIState != SCUIState.Eating) continue;
            if (!FoodTypeHelper.IsSameGroup(currentItem.FoodType, eatenFoodType.Value)) continue;

            currentItem.SetCompleted();
            taskComp.AdvanceStep();

            // 步骤推进后立即检查新步骤所需食物是否在场上
            if (!taskComp.IsCompleted)
                taskComp.CheckAndSupplementCurrentFood().Coroutine();
        }
    }
}