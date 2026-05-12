using Fantasy.Event;

public class Event_HitDog_Handler : EventSystem<HitDog>
{
    protected override void Handler(HitDog a)
    {
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().ChangeDogState(DogState.Hit);
    }
}