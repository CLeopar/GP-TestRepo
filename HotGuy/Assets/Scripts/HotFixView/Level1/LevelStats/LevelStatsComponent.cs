using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;  // ← 确保有这个
using UnityEngine.SceneManagement;

public class LevelStatsComponent : Entity
{
    public int TasksCompleted { get; private set; }
    public int ShitEaten { get; private set; }
    public int FoodEaten { get; private set; }

    public void AddTaskCompleted() => TasksCompleted++;
    public void AddShitEaten() => ShitEaten++;
    public void AddFoodEaten() => FoodEaten++;

    public void SaveToPlayerPrefs(int totalScore)
    {
        // 累计数据（+=）
        int prevTasks = PlayerPrefs.GetInt("L1_TasksCompleted", 0);
        int prevShit = PlayerPrefs.GetInt("L1_ShitEaten", 0);
        int prevFood = PlayerPrefs.GetInt("L1_FoodEaten", 0);

        PlayerPrefs.SetInt("L1_TasksCompleted", prevTasks + TasksCompleted);
        PlayerPrefs.SetInt("L1_ShitEaten", prevShit + ShitEaten);
        PlayerPrefs.SetInt("L1_FoodEaten", prevFood + FoodEaten);

        // 本次分数
        PlayerPrefs.SetInt("L1_TotalScore", totalScore);

        // 最高分数（取 max）
        int prevHigh = PlayerPrefs.GetInt("L1_HighScore", 0);
        PlayerPrefs.SetInt("L1_HighScore", Mathf.Max(prevHigh, totalScore));

        PlayerPrefs.Save();

        Log.Error($"[LevelStats] Saved - Tasks:{TasksCompleted}, Shit:{ShitEaten}, Food:{FoodEaten}, Score:{totalScore}, HighScore:{Mathf.Max(prevHigh, totalScore)}");
    }
}

public class LevelStatsComponent_Awake : AwakeSystem<LevelStatsComponent>
{
    protected override void Awake(LevelStatsComponent self) { }
}