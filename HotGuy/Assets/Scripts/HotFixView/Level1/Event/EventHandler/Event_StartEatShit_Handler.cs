using Fantasy;
using Fantasy.Event;

public class Event_StartEatShit_Handler : EventSystem<StartEatShit>
{
    protected override void Handler(StartEatShit self)
    {
        Log.Error("StartEatShit");
        GameEntry.Instance._scene.GetComponent<FoodManagerComponent>().EatShit();
    }
}