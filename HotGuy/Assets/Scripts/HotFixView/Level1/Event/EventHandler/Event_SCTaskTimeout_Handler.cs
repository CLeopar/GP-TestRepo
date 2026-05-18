using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_SCTaskTimeout_Handler : EventSystem<SCTaskTimeout>
{
    protected override void Handler(SCTaskTimeout self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;

        Log.Error($"[SCTaskTimeout] triggered, TaskId: {self.TaskId}, total instances: {uiComp.TaskUIInstances.Count}");

        if (uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI))
        {
            Log.Error($"[SCTaskTimeout] TaskUI found, RootContainer is {(taskUI.RootContainer == null ? "NULL" : "OK")}");
            
            // 播放超时动画
            taskUI.PlayTimeoutAnimation();
            
            // 从字典移除（但引用还在，动画继续）
            uiComp.TaskUIInstances.Remove(self.TaskId);
            
            // 延迟销毁 GameObject（给动画时间：抖动0.4s + 缩小0.25s = 0.65s，1s足够）
            GameEntry.Instance._scene.TimerComponent.Net.OnceTimer(1000, () =>
            {
                if (taskUI != null && taskUI.gameObject != null)
                {
                    Log.Error($"[SCTaskTimeout] Destroying GameObject for TaskId: {self.TaskId}");
                    GameObject.Destroy(taskUI.gameObject);
                }
                else
                {
                    Log.Error($"[SCTaskTimeout] GameObject already null for TaskId: {self.TaskId}");
                }
            });
        }
        else
        {
            Log.Error($"[SCTaskTimeout] TaskUI NOT found for TaskId: {self.TaskId}");
        }
    }
}