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
            // 先播放动画，动画回调里销毁
            taskUI.PlayCompleteAnimation();
            uiComp.TaskUIInstances.Remove(self.TaskId);
            
            // 延迟销毁 GameObject（给动画时间）
            GameEntry.Instance._scene.TimerComponent.Net.OnceTimer(1000, () =>
            {
                if (taskUI != null && taskUI.gameObject != null)
                    GameObject.Destroy(taskUI.gameObject);
            });
            
            Log.Error($"[SCUI] Task UI destroyed (timeout): {self.TaskId}");
        }
    }
}
