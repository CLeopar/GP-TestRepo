using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_FoodEatenSecretly_Handler : EventSystem<FoodBeEaten_Secretly>
{
    protected override void Handler(FoodBeEaten_Secretly self)
    {
        // 暂停狗的吃食物音效
        var dogCtrl = GameEntry.Instance._scene.GetComponent<DogControlComponent>();
        if (dogCtrl != null)
        {
            dogCtrl.PauseEatSfx();
        }

        dogCtrl?.FoodBeEatenSecretly().Coroutine();
        
        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        var scoreComp = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
        var foodManager = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>();
        
        var food = foodManager?.GetFruitComponent(self.fruitId);
        Vector3 foodPos = food?.Fruit_Tr?.position ?? Vector3.zero;
        
        scoreComp?.AddScore(scoreConfig.SecretEatPenalty, self.fruitId, foodPos);
    }
}