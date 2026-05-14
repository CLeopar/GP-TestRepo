using Fantasy;
using Fantasy.Event;

public class Event_FoodBeEatenNormal_Handler : EventSystem<FoodBeEaten_Normal>
{
    protected override void Handler(FoodBeEaten_Normal self)
    {
        Log.Error($"FoodBeEaten_Normal {self.fruitId}");
        
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().FoodBeEatenNormal();
        
        // ===== 计分 =====
        var food = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().GetFruitComponent(self.fruitId);
        if (food != null)
        {
            var score = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
            score.AddScore(score.CalculateFoodScore(food.foodType), self.fruitId);
        }
    }
}