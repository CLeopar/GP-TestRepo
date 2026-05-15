using System.Collections.Generic;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

/// <summary>
/// SC单个任务组件
/// 挂在 SC_TaskManagerComponent 下，ISupportedMultiEntity 支持多实例
/// </summary>
public class TaskComponent : Entity, ISupportedMultiEntity
{
    /// <summary>
    /// 食物组合顺序
    /// </summary>
    public List<FoodType> FoodSequence = new List<FoodType>();

    /// <summary>
    /// 当前执行到第几步（0-based）
    /// </summary>
    public int CurrentStep = 0;

    /// <summary>
    /// 任务是否已完成
    /// </summary>
    public bool IsCompleted = false;

    /// <summary>
    /// 任务是否已失败/超时
    /// </summary>
    public bool IsFailed = false;

    /// <summary>
    /// 启动所有 SCItemComponent 的倒计时
    /// </summary>
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

    /// <summary>
    /// 停止所有倒计时
    /// </summary>
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

    /// <summary>
    /// 推进到下一步
    /// </summary>
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

    /// <summary>
    /// 获取当前步骤的食物类型
    /// </summary>
    public FoodType GetCurrentFoodType()
    {
        if (CurrentStep < FoodSequence.Count)
            return FoodSequence[CurrentStep];
        return FoodType.None;
    }

    /// <summary>
    /// 获取当前步骤的 SCItemComponent
    /// </summary>
    public SCItemComponent GetCurrentItem()
    {
        return GetComponent<SCItemComponent>(CurrentStep);
    }
}