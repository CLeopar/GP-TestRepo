using Fantasy;
using Fantasy.Event;

public class Event_SCTaskCompleted_Handler : EventSystem<SCTaskCompleted>
{
    protected override void Handler(SCTaskCompleted self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            taskUI.PlayCompleteAnimation();
        }
    }
}