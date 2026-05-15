using Fantasy;
using Fantasy.Event;

public class Event_ShitBeEaten_Handler : EventSystem<ShitBeEaten>
{
    protected override void Handler(ShitBeEaten self)
    {
        Log.Error("ShitBeEaten");
        
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().ShitBeEaten().Coroutine();
        
        var scoreConfig = GameEntry.Instance._scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        GameEntry.Instance._scene.GetComponent<ScoreComponent>()?.AddScore(scoreConfig.EatShitPenalty);
        
        GameEntry.Instance._scene.GetComponent<LevelStatsComponent>()?.AddShitEaten();
        
        // 停止粒子（保险）
        var shit = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>()?.GetComponent<ShitComponent>();
        shit?.FinishEat();
    }
}