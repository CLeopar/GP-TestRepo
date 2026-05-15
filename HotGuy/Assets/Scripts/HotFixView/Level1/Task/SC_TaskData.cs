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
    Normal,      // 常规状态（半透明/灰色）
    Eating,      // 正在吃（玩家开始喂这个食物）
    Completed,   // 已完成（吃对了）
}

/// <summary>
/// SC数据项（用于事件传输，无ECS依赖）
/// </summary>
public struct SCItemData
{
    public int Index;
    public FoodType FoodType;
    public float TotalDuration;
    public SCDurationType DurationType;
}
