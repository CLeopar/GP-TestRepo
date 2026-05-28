using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

/// <summary>
/// SC食物项组件 - 只管理自己的状态，不管理倒计时
/// 倒计时统一由 TaskComponent 管理
/// </summary>
public class SCItemComponent : Entity, ISupportedMultiEntity
{
    public long TaskId;
    public int Index;
    public FoodType FoodType;
    public SCDurationType DurationType;

    public SCUIState UIState = SCUIState.Normal;
    public bool IsCompleted = false;

    public void SetState(SCUIState newState)
    {
        UIState = newState;
        Scene.EventComponent.Publish(new SCItemStateChanged
        {
            TaskId = TaskId,
            ItemIndex = Index,
            NewState = newState
        });
    }

    public void SetCompleted()
    {
        IsCompleted = true;
        SetState(SCUIState.Completed);
    }
}