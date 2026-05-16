using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_ShitBeEaten_Handler : EventSystem<ShitBeEaten>
{
    protected override void Handler(ShitBeEaten self)
    {
        Log.Error("========== ShitBeEaten EVENT FIRED ==========");
        
        // 1. 狗动画
        var dogCtrl = GameEntry.Instance._scene.GetComponent<DogControlComponent>();
        Log.Error($"[ShitBeEaten] DogControl: {dogCtrl != null}");
        dogCtrl?.ShitBeEaten().Coroutine();
        
        // 2. 扣分
        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>()?.ScoreConfigCategory?.Data;
        Log.Error($"[ShitBeEaten] ScoreConfig: {scoreConfig != null}");
        
        var scoreComp = GameEntry.Instance._scene.GetComponent<ScoreComponent>();
        Log.Error($"[ShitBeEaten] ScoreComponent: {scoreComp != null}");
        
        if (scoreConfig != null && scoreComp != null)
        {
            scoreComp.AddScore(scoreConfig.EatShitPenalty);
            Log.Error($"[ShitBeEaten] Deducted: {scoreConfig.EatShitPenalty}");
        }
        
        // 3. 统计吃屎（关键！）
        var stats = GameEntry.Instance._scene.GetComponent<LevelStatsComponent>();
        Log.Error($"[ShitBeEaten] LevelStatsComponent: {stats != null}");
        
        if (stats != null)
        {
            stats.AddShitEaten();
            Log.Error($"[ShitBeEaten] ShitEaten count after add: {stats.ShitEaten}");
        }
        else
        {
            Log.Error("[ShitBeEaten] ERROR: LevelStatsComponent is NULL!");
        }
        
        // 4. 清理屎
        var shit = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>()?.GetComponent<ShitComponent>();
        Log.Error($"[ShitBeEaten] ShitComponent: {shit != null}");
        shit?.FinishEat();
    }
}