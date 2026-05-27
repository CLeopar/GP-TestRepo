using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class LevelStatsComponent : Entity
{
    public int TasksCompleted { get; private set; }
    public int ShitEaten { get; private set; }
    public int FoodEaten { get; private set; }

    public void AddTaskCompleted() => TasksCompleted++;
    public void AddShitEaten() => ShitEaten++;
    public void AddFoodEaten() => FoodEaten++;

    /// <summary>
    /// 保存到 PlayerPrefs（第一关专用 L1_ 前缀）
    /// </summary>
    public void SaveToPlayerPrefs(int totalScore)
    {
        PlayerPrefs.SetInt("L1_TotalScore", totalScore);
        PlayerPrefs.SetInt("L1_TasksCompleted", TasksCompleted);
        PlayerPrefs.SetInt("L1_ShitEaten", ShitEaten);
        PlayerPrefs.SetInt("L1_FoodEaten", FoodEaten);

        // 更新最高分
        int prevHigh = PlayerPrefs.GetInt("L1_HighScore", 0);
        if (totalScore > prevHigh)
        {
            PlayerPrefs.SetInt("L1_HighScore", totalScore);
            Log.Error($"[LevelStats] L1 🎉 新纪录！{totalScore} > {prevHigh}");
        }

        PlayerPrefs.Save();
    }

    /// <summary>获取第一关最高分</summary>
    public static int GetHighScore() => PlayerPrefs.GetInt("L1_HighScore", 0);

    /// <summary>获取第一关本次总分</summary>
    public static int GetTotalScore() => PlayerPrefs.GetInt("L1_TotalScore", 0);

    public void Reset()
    {
        TasksCompleted = 0;
        ShitEaten = 0;
        FoodEaten = 0;
    }

    /// <summary>清除第一关所有存档（调试用）</summary>
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteKey("L1_TotalScore");
        PlayerPrefs.DeleteKey("L1_HighScore");
        PlayerPrefs.DeleteKey("L1_TasksCompleted");
        PlayerPrefs.DeleteKey("L1_ShitEaten");
        PlayerPrefs.DeleteKey("L1_FoodEaten");
        PlayerPrefs.Save();
        Log.Error("[LevelStats] L1 数据已清除");
    }
}

public class LevelStatsComponent_Awake : AwakeSystem<LevelStatsComponent>
{
    protected override void Awake(LevelStatsComponent self)
    {
        self.Reset();
    }
}