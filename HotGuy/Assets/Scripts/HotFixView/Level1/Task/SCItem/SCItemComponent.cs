using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class SCItemComponent : Entity, ISupportedMultiEntity
{
    public long TaskId;
    public int Index;
    public FoodType FoodType;
    public SCDurationType DurationType;

    // ========== 总时长 - 只从 Config 读 ==========
    public float TotalDuration 
    { 
        get 
        {
            var config = Scene.GetComponent<Tables>().ConstConfigCategory.Data;
            return DurationType == SCDurationType.Green_10s 
                ? config.SCGreenDuration 
                : config.SCOrangeDuration;
        }
    }

    // ========== 剩余时间 - 运行时状态 ==========
    public float RemainingTime { get; private set; }

    public SCUIState UIState = SCUIState.Normal;
    public bool IsCompleted = false;

    private long _timerId;
    private long _updateTimerId;

    // ========== 启动倒计时 - 统一入口 ==========
    public void StartCountdown()
    {
        RemainingTime = TotalDuration;  // 从配置重新初始化
        
        Log.Error($"[SCItem] StartCountdown: TaskId={TaskId}, Index={Index}, Type={DurationType}, Total={TotalDuration}s, Remaining={RemainingTime}s");

        _timerId = Scene.TimerComponent.Net.OnceTimer((long)(TotalDuration * 1000), OnTimeout);
        _updateTimerId = Scene.TimerComponent.Net.RepeatedTimer(1000, OnTimerUpdate);

        // 发布初始状态事件，UI 收到后显示正确时间
        Scene.EventComponent.Publish(new SCTimerUpdate
        {
            TaskId = TaskId,
            ItemIndex = Index,
            RemainingTime = RemainingTime
        });

        if (Index == 0)
        {
            SetState(SCUIState.Normal);
        }
    }

    public void StopCountdown()
    {
        Scene.TimerComponent.Net.Remove(ref _timerId);
        Scene.TimerComponent.Net.Remove(ref _updateTimerId);
    }

    private void OnTimerUpdate()
    {
        RemainingTime -= 1f;
        if (RemainingTime < 0) RemainingTime = 0;

        Log.Error($"[SCItem] OnTimerUpdate: TaskId={TaskId}, Remaining={RemainingTime}s");

        Scene.EventComponent.Publish(new SCTimerUpdate
        {
            TaskId = TaskId,
            ItemIndex = Index,
            RemainingTime = RemainingTime
        });
    }

    private void OnTimeout()
    {
        Log.Error($"[SCItem] OnTimeout: TaskId={TaskId}, Index={Index}");
        StopCountdown();

        var manager = GetParent<TaskComponent>()?.GetParent<TaskManagerComponent>();
        if (manager != null)
        {
            manager.RemoveTask(TaskId);
        }
    }

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

    public void SetCompleted()
    {
        IsCompleted = true;
        SetState(SCUIState.Completed);
        StopCountdown();
    }
}