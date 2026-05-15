using System.Collections.Generic;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TaskComponent : Entity, ISupportedMultiEntity
{
    public List<FoodType> FoodSequence = new List<FoodType>();
    public int CurrentStep = 0;
    public bool IsCompleted = false;
    public bool IsFailed = false;

    public void StartCountdown()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
            {
                itemComp.StartCountdown();
            }
        }
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

    // ========== 修复：按 Index 遍历匹配，不用 GetComponent<T>(id) ==========
    public SCItemComponent GetCurrentItem()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent scItem && scItem.Index == CurrentStep)
                return scItem;
        }
        return null;
    }
}