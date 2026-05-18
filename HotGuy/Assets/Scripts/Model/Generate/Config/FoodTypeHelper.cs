/// <summary>
/// 食物类型工具类
/// 处理同类食物的分组匹配（如蓝莓A/B/C/D 视为同一种）
/// </summary>
public static class FoodTypeHelper
{
    /// <summary>
    /// 判断两个 FoodType 是否属于同一"逻辑种类"
    /// 用于任务匹配：场景中的食物 和 任务要求的食物 做比较
    /// </summary>
    public static bool IsSameGroup(FoodType a, FoodType b)
    {
        if (a == b) return true;
        return GetGroup(a) == GetGroup(b);
    }

    /// <summary>
    /// 获取食物的分组类型
    /// 同一分组内的食物在任务中视为等价
    /// </summary>
    public static FoodType GetGroup(FoodType foodType)
    {
        switch (foodType)
        {
            case FoodType.Blueberry_A:
            case FoodType.Blueberry_B:
            case FoodType.Blueberry_C:
            case FoodType.Blueberry_D:
                return FoodType.Blueberry_A; // 统一用 A 作为分组代表
            default:
                return foodType;
        }
    }
}