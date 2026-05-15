using Fantasy;
using Fantasy.Event;

public class Event_SCItemStateChanged_Handler : EventSystem<SCItemStateChanged>
{
protected override void Handler(SCItemStateChanged self)
{
    var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
    if (uiComp == null) return;

    if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
    {
        taskUI.SetFoodState(self.ItemIndex, self.NewState);
    }
}
}
