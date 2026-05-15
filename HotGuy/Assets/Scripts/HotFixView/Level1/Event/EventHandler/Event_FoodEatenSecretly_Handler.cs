using Fantasy;
using Fantasy.Event;

public class Event_FoodEatenSecretly_Handler : EventSystem<FoodBeEaten_Secretly>
{
    protected override void Handler(FoodBeEaten_Secretly self)
    {
        Log.Error($"FoodBeEaten_Secretly {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().FoodBeEatenSecretly().Coroutine();
        
        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        GameEntry.Instance._scene.GetComponent<ScoreComponent>()?.AddScore(scoreConfig.SecretEatPenalty, self.fruitId);
    }
}