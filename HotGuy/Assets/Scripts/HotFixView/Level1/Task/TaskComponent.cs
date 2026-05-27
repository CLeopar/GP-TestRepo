using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TaskComponent : Entity, ISupportedMultiEntity
{
    public List<FoodType> FoodSequence = new List<FoodType>();
    public int CurrentStep = 0;
    public bool IsCompleted = false;
    public bool IsFailed = false;

    // ========== 新增：补充检测定时器 ==========
    private long _supplementTimerId = 0;
    private const long SupplementCheckInterval = 5000; // 5秒

    public void StartCountdown()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
            {
                itemComp.StartCountdown();
            }
        }
        
        // 启动5秒循环检测
        StartSupplementCheck();
    }

    public void StopAllCountdowns()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
            {
                itemComp.StopCountdown();
            }
        }
        
        // 停止补充检测
        StopSupplementCheck();
    }

    public void AdvanceStep()
    {
        if (CurrentStep < FoodSequence.Count - 1)
        {
            CurrentStep++;
            Log.Error($"[TaskComponent] Task {Id} advanced to step {CurrentStep}");
        }
        else
        {
            IsCompleted = true;
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

    // ========== 新增：启动5秒循环检测 ==========
    private void StartSupplementCheck()
    {
        StopSupplementCheck(); // 先停旧的
        _supplementTimerId = Scene.TimerComponent.Net.RepeatedTimer(SupplementCheckInterval, OnSupplementCheck);
    }

    private void StopSupplementCheck()
    {
        Scene.TimerComponent.Net.Remove(ref _supplementTimerId);
    }

    private void OnSupplementCheck()
    {
        if (IsCompleted || IsFailed) 
        {
            StopSupplementCheck();
            return;
        }

        CheckAndSupplementCurrentFood().Coroutine();
    }

    // ========== 新增：检查并补充当前步骤所需食物 ==========
    private async FTask CheckAndSupplementCurrentFood()
    {
        var currentFoodType = GetCurrentFoodType();
        if (currentFoodType == FoodType.None) return;

        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null) return;

        // 检查场景里是否还有这种食物
        bool hasFoodInScene = false;
        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is FoodComponent food 
                && food.foodType == currentFoodType
                && food.fruitStateType != FruitStateType.BeEaten
                && !food.isInPickUp
                && food.Fruit_Go != null)
            {
                // 排除正在被狗吃的
                var dogCtrl = Scene.GetComponent<DogControlComponent>();
                if (dogCtrl != null && dogCtrl.CurEatFoodData.Item2 == food.Id)
                    continue;
                    
                hasFoodInScene = true;
                break;
            }
        }

        if (!hasFoodInScene)
        {
            Log.Error($"[TaskComponent] Task {Id} step {CurrentStep} needs {currentFoodType} but none in scene! Spawning...");
            await foodManager.AddNewFruitOfType(currentFoodType);
        }
    }
}