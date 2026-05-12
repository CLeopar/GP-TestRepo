using Fantasy;
using Fantasy.Event;

public class Event_HoldDog_Handler : EventSystem<HoldDog>
{
    protected override void Handler(HoldDog self)
    {
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().ChangeDogState(self.State ? DogState.Hold : DogState.Normal, self.isL);
    }
}