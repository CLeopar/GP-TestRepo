using Fantasy;
using Fantasy.Event;

// 事件定义已移到 Level1Event.cs，这里只保留 Handler
public class Event_StopCameraShake_Handler : EventSystem<StopCameraShake>
{
    protected override void Handler(StopCameraShake self)
    {
        GameEntry.Instance._scene.GetComponent<CameraShakeComponent>()?.StopShake();
    }
}