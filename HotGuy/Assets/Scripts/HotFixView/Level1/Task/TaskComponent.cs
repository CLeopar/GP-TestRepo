using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

/// <summary>
/// 任务组件 - 统一管理整个任务的倒计时
/// 吃错食物不影响倒计时，只有超时或完成才停止
/// </summary>
public class TaskComponent : Entity, ISupportedMultiEntity
{
    public List<FoodType> FoodSequence = new List<FoodType>();
    public int CurrentStep = 0;
    public bool IsCompleted = false;
    public bool IsFailed = false;

    // ========== 倒计时状态 ==========
    public float TotalDuration { get; private set; }
    public float RemainingTime { get; private set; }
    private long _timerId;
    private long _updateTimerId;
    private bool _isCountdownRunning = false;

    public void StartCountdown(float duration)
    {
        StopCountdown();
        TotalDuration = duration;
        RemainingTime = duration;
        _isCountdownRunning = true;

        Log.Error($"[TaskComponent] StartCountdown Task={Id}, Duration={duration}s");

        // 超时timer
        _timerId = Scene.TimerComponent.Net.OnceTimer((long)(duration * 1000), OnTimeout);

        // 【修复】第一次更新延迟1秒，避免扣2秒
        _updateTimerId = Scene.TimerComponent.Net.OnceTimer(1000, OnFirstTimerUpdate);

        // 发布初始事件
        PublishTimerUpdate();

        // 设置第一个食物为Normal状态
        var firstItem = GetCurrentItem();
        firstItem?.SetState(SCUIState.Normal);
    }

    private void OnFirstTimerUpdate()
    {
        if (!_isCountdownRunning) return;
        OnTimerUpdate();
        if (_isCountdownRunning)
        {
            _updateTimerId = Scene.TimerComponent.Net.RepeatedTimer(1000, OnTimerUpdate);
        }
    }

    private void OnTimerUpdate()
    {
        if (!_isCountdownRunning) return;

        RemainingTime -= 1f;
        if (RemainingTime < 0) RemainingTime = 0;

        Log.Error($"[TaskComponent] TimerUpdate Task={Id}, Remaining={RemainingTime}s");
        PublishTimerUpdate();
    }

    private void PublishTimerUpdate()
    {
        Scene.EventComponent.Publish(new SCTimerUpdate
        {
            TaskId = Id,
            ItemIndex = CurrentStep,  // 始终用 CurrentStep，UI只显示当前step
            RemainingTime = RemainingTime,
            TotalDuration = TotalDuration
        });
    }

    private void OnTimeout()
    {
        Log.Error($"[TaskComponent] Timeout Task={Id}");
        StopCountdown();
        IsFailed = true;

        var manager = GetParent<TaskManagerComponent>();
        manager?.RemoveTask(Id);
    }

    public void StopCountdown()
    {
        _isCountdownRunning = false;
        Scene.TimerComponent.Net.Remove(ref _timerId);
        Scene.TimerComponent.Net.Remove(ref _updateTimerId);
    }

    public void AdvanceStep()
    {
        if (CurrentStep < FoodSequence.Count - 1)
        {
            // 标记当前item完成
            var currentItem = GetCurrentItem();
            currentItem?.SetCompleted();

            CurrentStep++;
            Log.Error($"[TaskComponent] Task {Id} advanced to step {CurrentStep}");

            // 设置新step的食物状态
            var nextItem = GetCurrentItem();
            nextItem?.SetState(SCUIState.Normal);

            // 发布step变更事件，UI更新食物高亮
            Scene.EventComponent.Publish(new SCStepChanged
            {
                TaskId = Id,
                NewStep = CurrentStep
            });
        }
        else
        {
            // 所有步骤完成
            IsCompleted = true;
            StopCountdown();
            var manager = GetParent<TaskManagerComponent>();
            manager?.CompleteTask(Id);
        }
    }

    public FoodType GetCurrentFoodType()
    {
        if (CurrentStep < FoodSequence.Count)
            return FoodSequence[CurrentStep];
        return FoodType.None;
    }

    public SCItemComponent GetCurrentItem()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent scItem && scItem.Index == CurrentStep)
                return scItem;
        }
        return null;
    }

    public SCItemComponent GetItem(int index)
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent scItem && scItem.Index == index)
                return scItem;
        }
        return null;
    }

    public async FTask CheckAndSupplementCurrentFood()
    {
        var currentFoodType = GetCurrentFoodType();
        if (currentFoodType == FoodType.None) return;

        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null) return;

        var dogCtrl = Scene.GetComponent<DogControlComponent>();

        bool hasFoodInScene = false;
        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is not FoodComponent food) continue;
            if (food.foodType != currentFoodType) continue;
            if (food.fruitStateType == FruitStateType.BeEaten) continue;
            if (food.isInPickUp) continue;
            if (food.Fruit_Go == null || food.Fruit_Tr == null) continue;
            if (dogCtrl != null && dogCtrl.CurEatFoodData.Item2 == food.Id) continue;

            hasFoodInScene = true;
            break;
        }

        if (!hasFoodInScene)
        {
            Log.Error($"[TaskComponent] Task {Id} step {CurrentStep} needs {currentFoodType} but none in scene, spawning...");
            await foodManager.AddNewFruitOfType(currentFoodType);
        }
    }
}

