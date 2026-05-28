using Fantasy;
using Fantasy.Event;

public class Event_SCTimerUpdate_Handler : EventSystem<SCTimerUpdate>
{
    protected override void Handler(SCTimerUpdate self)
    {
        var uiComp = GameEntry.Instance._scene.GetComponent<SCUIComponent>();
        if (uiComp == null) return;
        if (!uiComp.TaskUIInstances.TryGetValue(self.TaskId, out var taskUI)) return;

        // 【修复2】倒计时独立运行，不受CurrentStep影响
        // 移除对 taskComp.CurrentStep 的检查，倒计时就是倒计时

        // 每个step的timer独立更新自己的UI位置
        // 用 ItemIndex 判断更新哪个食物框的timer显示
        // 但整个任务的倒计时条只显示当前step的timer

        var taskManager = GameEntry.Instance._scene.GetComponent<TaskManagerComponent>();
        var taskComp = taskManager?.GetComponent<TaskComponent>(self.TaskId);
        if (taskComp == null) return;

        // 只更新当前step对应的timer显示到主进度条
        // 但所有step的timer都在独立运行，只是UI只显示当前step的
        if (self.ItemIndex == taskComp.CurrentStep)
        {
            // 如果是新step刚开始（remaining接近total），重置进度条
            if (self.RemainingTime >= self.TotalDuration - 0.5f)
                taskUI.ResetTimerForNewStep(self.TotalDuration);
            else
                taskUI.UpdateTimer(self.RemainingTime, self.TotalDuration);
        }

        // 注意：非当前step的timer也在跑，只是不更新UI而已
        // 这样当step推进时，下一个step的timer已经准备好
    }
}