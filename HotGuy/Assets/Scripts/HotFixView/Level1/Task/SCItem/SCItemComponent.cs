using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

/// <summary>
/// SC单个食物项组件
/// 挂在 TaskComponent 下，管理单个食物的倒计时和状态
/// </summary>
public class SCItemComponent : Entity, ISupportedMultiEntity
{
    /// <summary>
    /// 所属任务ID
    /// </summary>
    public long TaskId;

    /// <summary>
    /// 在组合中的顺序位置
    /// </summary>
    public int Index;

    /// <summary>
    /// 对应食物类型
    /// </summary>
    public FoodType FoodType;

    /// <summary>
    /// 倒计时总时长（秒）
    /// </summary>
    public float TotalDuration;

    /// <summary>
    /// 剩余时间（秒）
    /// </summary>
    public float RemainingTime;

    /// <summary>
    /// 倒计时类型
    /// </summary>
    public SCDurationType DurationType;

    /// <summary>
    /// 当前UI状态
    /// </summary>
    public SCUIState UIState = SCUIState.Normal;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted = false;

    /// <summary>
    /// 倒计时Timer ID
    /// </summary>
    private long _timerId;

    /// <summary>
    /// 每秒更新Timer ID
    /// </summary>
    private long _updateTimerId;

    /// <summary>
    /// 启动倒计时
    /// </summary>
    public void StartCountdown()
    {
        _timerId = Scene.TimerComponent.Net.OnceTimer((long)(TotalDuration * 1000), OnTimeout);
        _updateTimerId = Scene.TimerComponent.Net.RepeatedTimer(1000, OnTimerUpdate);

        if (Index == 0)
        {
            SetState(SCUIState.Normal);
        }
    }

    /// <summary>
    /// 停止倒计时
    /// </summary>
    public void StopCountdown()
    {
        Scene.TimerComponent.Net.Remove(ref _timerId);
        Scene.TimerComponent.Net.Remove(ref _updateTimerId);
    }

    /// <summary>
    /// 倒计时更新（每秒）
    /// </summary>
    private void OnTimerUpdate()
    {
        RemainingTime -= 1f;
        if (RemainingTime < 0) RemainingTime = 0;

        Scene.EventComponent.Publish(new SCTimerUpdate
        {
            TaskId = TaskId,
            ItemIndex = Index,
            RemainingTime = RemainingTime
        });
    }

    /// <summary>
    /// 超时处理
    /// </summary>
    private void OnTimeout()
    {
        StopCountdown();

        var manager = GetParent<TaskComponent>()?.GetParent<TaskManagerComponent>();
        if (manager != null)
        {
            manager.RemoveTask(TaskId);
        }
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetState(SCUIState newState)
    {
        UIState = newState;

        Scene.EventComponent.Publish(new SCItemStateChanged
        {
            TaskId = TaskId,
            ItemIndex = Index,
            NewState = newState,
            RemainingTime = RemainingTime
        });
    }

    /// <summary>
    /// 标记为完成
    /// </summary>
    public void SetCompleted()
    {
        IsCompleted = true;
        SetState(SCUIState.Completed);
        StopCountdown();
    }
}