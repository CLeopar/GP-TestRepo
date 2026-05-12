using Fantasy;
using Fantasy.Event;

public class Event_StartEatFood_Handler : EventSystem<StartEatFood>
{
    protected override void Handler(StartEatFood self)
    {
        Log.Error($"StartEatFood {self.fruitId}");
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().StartEatFruit(self.fruitId, self.isNormal);
    }
}