using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_SCTaskSpawned_Handler : EventSystem<SCTaskSpawned>
{
    protected override void Handler(SCTaskSpawned self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null)
        {
            Log.Error("[SCUI] SCUIComponent not found!");
            return;
        }

        GameObject prefab = null;
        if (self.SCItems.Count > 0)
        {
            prefab = self.SCItems[0].DurationType == SCDurationType.Green_10s 
                ? uiComp.SCTaskPrefab_Green
                : uiComp.SCTaskPrefab_Orange;
        }

        if (prefab == null || uiComp.SCTaskParent == null)
        {
            Log.Error("[SCUI] Prefab or Parent not set!");
            return;
        }

        var taskGo = GameObject.Instantiate(prefab, uiComp.SCTaskParent);
        taskGo.name = $"SCTask_{self.TaskId}";

        var taskUI = taskGo.GetComponent<SCTaskUI>();
        if (taskUI == null)
            taskUI = taskGo.AddComponent<SCTaskUI>();

        taskUI.Init(self.TaskId, self.FoodSequence, self.SCItems);
        uiComp.TaskUIInstances[self.TaskId] = taskUI;

        Log.Error($"[SCUI] Task UI created: {self.TaskId}, type: {self.SCItems[0].DurationType}");
    }
}
