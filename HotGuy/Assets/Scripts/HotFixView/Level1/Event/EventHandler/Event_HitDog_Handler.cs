using Fantasy.Event;

public class Event_HitDog_Handler : EventSystem<HitDog>
{
    protected override void Handler(HitDog a)
    {
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().TriggerHit();
        GameEntry.Instance._scene.EventComponent.Publish(new PlaySFX
        {
            Type = SFXType.Dong
        });
    
    }
}