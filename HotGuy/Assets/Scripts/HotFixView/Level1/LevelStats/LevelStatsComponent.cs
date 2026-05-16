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

    public void SaveToPlayerPrefs(int totalScore)
    {
        PlayerPrefs.SetInt("L1_TotalScore", totalScore);
        PlayerPrefs.SetInt("L1_TasksCompleted", TasksCompleted);
        PlayerPrefs.SetInt("L1_ShitEaten", ShitEaten);
        PlayerPrefs.SetInt("L1_FoodEaten", FoodEaten);

        int prevHigh = PlayerPrefs.GetInt("L1_HighScore", 0);
        if (totalScore > prevHigh)
            PlayerPrefs.SetInt("L1_HighScore", totalScore);

        PlayerPrefs.Save();
    }

    public void Reset()
    {
        TasksCompleted = 0;
        ShitEaten = 0;
        FoodEaten = 0;
    }
}

public class LevelStatsComponent_Awake : AwakeSystem<LevelStatsComponent>
{
    protected override void Awake(LevelStatsComponent self)
    {
        self.Reset();
    }
}