using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class ScoreComponent : Entity
{
    public int CurrentScore { get; private set; }

    public void AddScore(int delta, long targetId = 0)
    {
        CurrentScore += delta;
        
        Log.Error($"[ScoreComponent] AddScore: {delta}, Current: {CurrentScore}");
        
        Scene.EventComponent.Publish(new ScoreChanged
        {
            Delta = delta,
            CurrentScore = CurrentScore,
            TargetId = targetId
        });
    }

    public void Reset()
    {
        CurrentScore = 0;
        Scene.EventComponent.Publish(new ScoreReset());
    }

    public int CalculateFoodScore(FoodType foodType)
    {
        var config = Scene.GetComponent<Tables>().FoodConfigCategory.Get(foodType);
        int biteCount = config == null ? 1 : System.Math.Max(1, config.FoodStateCount - 1);
        
        return biteCount switch
        {
            1 => 10,
            2 => 15,
            3 => 20,
            _ => 10
        };
    }
}

public class ScoreComponent_Awake : AwakeSystem<ScoreComponent>
{
protected override void Awake(ScoreComponent self) => self.Reset();
}