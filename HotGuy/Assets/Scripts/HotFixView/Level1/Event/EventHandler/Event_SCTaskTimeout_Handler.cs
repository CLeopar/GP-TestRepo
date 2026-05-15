using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_SCTaskTimeout_Handler : EventSystem<SCTaskTimeout>
{
    protected override void Handler(SCTaskTimeout self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            GameObject.Destroy(taskUI.gameObject);
            uiComp.TaskUIInstances.Remove(self.TaskId);
            Log.Error($"[SCUI] Task UI destroyed (timeout): {self.TaskId}");
        }
    }
}
