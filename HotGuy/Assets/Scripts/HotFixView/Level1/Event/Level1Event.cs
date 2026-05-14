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
}

/// <summary>
/// 重置分数事件
/// </summary>
public struct ScoreReset
{
}