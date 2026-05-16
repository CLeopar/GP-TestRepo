using Fantasy;
using Fantasy.Event;

// 事件定义已移到 Level1Event.cs，这里只保留 Handler
public class Event_StartCameraShake_Handler : EventSystem<StartCameraShake>
{
    protected override void Handler(StartCameraShake self)
    {
        GameEntry.Instance._scene.GetComponent<CameraShakeComponent>()?.StartShake();
    }
}