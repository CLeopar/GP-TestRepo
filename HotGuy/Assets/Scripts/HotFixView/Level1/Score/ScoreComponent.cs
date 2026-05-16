using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class ScoreComponent : Entity
{
    public int CurrentScore { get; private set; }

    // ========== 修改：添加 worldPos 参数（可选，默认 Vector3.zero）==========
    public void AddScore(int delta, long targetId = 0, Vector3 worldPos = default)
    {
        CurrentScore += delta;
        
        Log.Error($"[ScoreComponent] AddScore: {delta}, Current: {CurrentScore}, Pos: {worldPos}");
        
        Scene.EventComponent.Publish(new ScoreChanged
        {
            Delta = delta,
            CurrentScore = CurrentScore,
            TargetId = targetId,
            WorldPosition = worldPos
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
        
        var scoreConfig = Scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        
        return biteCount switch
        {
            1 => scoreConfig.FoodScore1Bite,
            2 => scoreConfig.FoodScore2Bite,
            3 => scoreConfig.FoodScore3Bite,
            _ => scoreConfig.FoodScore1Bite
        };
    }
}

public class ScoreComponent_Awake : AwakeSystem<ScoreComponent>
{
protected override void Awake(ScoreComponent self) => self.Reset();
}