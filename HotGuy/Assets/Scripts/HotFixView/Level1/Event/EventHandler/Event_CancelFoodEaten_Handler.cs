using Fantasy;
using Fantasy.Event;

public class Event_CancelFoodEaten_Handler : EventSystem<CancelFoodEaten>
{
    protected override void Handler(CancelFoodEaten self)
    {
        Log.Error($"CancelFoodEaten {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().CancelEatFruit(self.fruitId);
    }
}