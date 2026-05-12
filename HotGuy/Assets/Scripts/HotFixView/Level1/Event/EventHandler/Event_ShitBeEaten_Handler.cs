using Fantasy;
using Fantasy.Event;

public class Event_ShitBeEaten_Handler : EventSystem<ShitBeEaten>
{
    protected override void Handler(ShitBeEaten self)
    {
        Log.Error("ShitBeEaten");
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().ShitBeEaten().Coroutine();
    }
}