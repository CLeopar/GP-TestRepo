using Fantasy;
using Fantasy.Event;

public class Event_ShitBeEaten_Handler : EventSystem<ShitBeEaten>
{
    protected override void Handler(ShitBeEaten self)
    {
        Log.Error("ShitBeEaten");
        
        GameEntry.Instance._scene.GetComponent<DogControlComponent>().ShitBeEaten().Coroutine();
        
        // 扣100分
        GameEntry.Instance._scene.GetComponent<ScoreComponent>()?.AddScore(-100);
        
        // 停止粒子（保险）
        var shit = GameEntry.Instance._scene.GetComponent<FoodManagerComponent>()?.GetComponent<ShitComponent>();
        shit?.FinishEat();
    }
}