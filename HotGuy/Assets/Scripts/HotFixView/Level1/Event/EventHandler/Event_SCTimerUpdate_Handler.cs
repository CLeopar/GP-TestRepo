using Fantasy;
using Fantasy.Event;

public class Event_SCTimerUpdate_Handler : EventSystem<SCTimerUpdate>
{
    protected override void Handler(SCTimerUpdate self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            taskUI.UpdateTimer(self.RemainingTime);
        }
    }
}
