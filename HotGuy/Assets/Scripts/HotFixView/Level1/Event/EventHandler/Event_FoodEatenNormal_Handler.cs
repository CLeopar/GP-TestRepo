using Fantasy;
using Fantasy.Event;

public class Event_FoodEatenNormal_Handler : EventSystem<FoodBeEaten_Normal>
{
    protected override void Handler(FoodBeEaten_Normal self)
    {
        Log.Error($"FoodBeEaten_Normal {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().FoodBeEatenNormal();
    }
}