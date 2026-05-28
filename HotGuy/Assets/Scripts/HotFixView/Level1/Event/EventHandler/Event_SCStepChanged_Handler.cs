using Fantasy;
using Fantasy.Event;

public class Event_SCStepChanged_Handler : EventSystem<SCStepChanged>
{
protected override void Handler(SCStepChanged self)
{
    var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
    if (uiComp == null) return;
    if (!uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI)) return;

    var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
    var taskComp = taskManager?.GetComponent<TaskComponent>(self.TaskId);
    if (taskComp == null) return;

    for (int i = 0; i < taskComp.FoodSequence.Count; i++)
    {
        var item = taskComp.GetItem(i);
        if (item != null)
        {
            taskUI.SetFoodState(i, item.UIState);
        }
    }
}
}