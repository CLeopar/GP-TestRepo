using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 开始吃食物
/// </summary>
public struct StartEatFood
{
    public long fruitId;
    public bool isNormal;
}

/// <summary>
/// 食物被正常吃完
/// </summary>
public struct FoodBeEaten_Normal
{
    public long fruitId;
}

/// <summary>
/// 食物被正常吃完
/// </summary>
public struct FoodBeEaten_Secretly
{
    public long fruitId;
}

/// <summary>
/// 取消食物倒计时
/// </summary>
public struct CancelFoodEaten
{
    public long fruitId;
}

/// <summary>
/// 开始吃屎
/// </summary>
public struct StartEatShit
{
}

/// <summary>
/// 屎被吃完
/// </summary>
public struct ShitBeEaten
{
}

/// <summary>
/// 取消屎倒计时
/// </summary>
public struct CancelShitEaten
{
}

/// <summary>
/// 握住狗
/// </summary>
public struct HoldDog
{
    public bool isL;
    public bool State;
}

public struct HitDog
{
}

// 加到 Level1Event.cs 文件末尾

/// <summary>
/// 分数变化事件
/// </summary>
public struct ScoreChanged
{
    public int Delta;
    public int CurrentScore;
    public long TargetId;
    
    public Vector3 WorldPosition;
    
}

/// <summary>
/// 重置分数事件
/// </summary>
public struct ScoreReset
{
}

/// <summary>
/// 倒计时更新事件（每秒触发）
/// </summary>
public struct LevelTimerUpdate
{
    /// <summary>
    /// 剩余时间（毫秒）
    /// </summary>
    public long RemainingTime;
    
    /// <summary>
    /// 已用时间（毫秒）
    /// </summary>
    public long ElapsedTime;
    
    /// <summary>
    /// 总时长（毫秒）
    /// </summary>
    public long TotalTime;
}

/// <summary>
/// 倒计时结束事件
/// </summary>
public struct LevelTimerFinished
{
}

// ================== SC任务系统事件 ==================

/// <summary>
/// SC任务生成事件
/// </summary>
public struct SCTaskSpawned
{
    public long TaskId;
    public List<FoodType> FoodSequence;
    public List<SCItemData> SCItems;
}

/// <summary>
/// SC食物项状态变化事件
/// </summary>
public struct SCItemStateChanged
{
    public long TaskId;
    public int ItemIndex;
    public SCUIState NewState;
    public float RemainingTime;
}

/// <summary>
/// SC任务完成事件
/// </summary>
public struct SCTaskCompleted
{
    public long TaskId;
}

/// <summary>
/// SC任务超时/消失事件
/// </summary>
public struct SCTaskTimeout
{
    public long TaskId;
}

/// <summary>
/// SC倒计时更新事件（每秒）
/// </summary>
public struct SCTimerUpdate
{
    public long TaskId;
    public int ItemIndex;
    public float RemainingTime;
}

