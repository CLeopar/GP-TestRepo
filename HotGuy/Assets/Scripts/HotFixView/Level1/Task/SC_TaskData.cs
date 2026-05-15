using System.Collections.Generic;
// SC_TaskData.cs

using System.Collections.Generic;

/// <summary>
/// SC倒计时类型
/// </summary>
public enum SCDurationType
{
    Green_10s = 10,   // 10秒绿色
    Orange_8s = 8,    // 8秒橙色
}

/// <summary>
/// SC UI状态
/// </summary>
public enum SCUIState
{
    Normal,      // 常规状态（示意图）
    Eating,      // 正在吃
    Completed,   // 已完成
}

/// <summary>
/// SC数据项（用于事件传输，无ECS依赖）
/// </summary>
public struct SCItemData
{
    public int Index;
    public FoodType FoodType;
    public SCDurationType DurationType;  // ← 只传类型，时间从 Config 读
    
    // 保留兼容，但不再作为时间源
    public float TotalDuration;  // ← 可选：完全删除或保留不用
}