using Fantasy;
using Fantasy.Event;

public class Event_CancelShitEaten_Handler : EventSystem<CancelShitEaten>
{
    protected override void Handler(CancelShitEaten self)
    {
        Log.Error("CancelShitEaten");
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().CancelEatShit();
    }
}