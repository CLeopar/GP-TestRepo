using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_FoodEatenSecretly_Handler : EventSystem<FoodBeEaten_Secretly>
{
    protected override void Handler(FoodBeEaten_Secretly self)
    {
        var dogCtrl = GameEntry.Instance._scene.GetComponent<DogControlComponent>();
        if (dogCtrl != null)
            dogCtrl.PauseEatSfx();

        dogCtrl?.FoodBeEatenSecretly().Coroutine();

        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        var scoreComp = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
        var foodManager = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>();

        var food = foodManager?.GetFruitComponent(self.fruitId);
        Vector3 foodPos = food?.Fruit_Tr?.position ?? Vector3.zero;
        scoreComp?.AddScore(scoreConfig.SecretEatPenalty, self.fruitId, foodPos);

        // 偷吃后检查所有任务当前步骤所需食物是否还在场上
        CheckTaskSupplement(food?.foodType);
    }

    private void CheckTaskSupplement(FoodType? eatenFoodType)
    {
        if (eatenFoodType == null || eatenFoodType == FoodType.None) return;

        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        if (taskManager == null) return;

        foreach (var taskId in taskManager.ActiveTaskIds)
        {
            var taskComp = taskManager.GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) continue;

            // 只有被偷吃的食物类型正好是该任务当前步骤需要的，才补货
            if (taskComp.GetCurrentFoodType() != eatenFoodType.Value) continue;

            taskComp.CheckAndSupplementCurrentFood().Coroutine();
        }
    }
}