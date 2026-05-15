using Fantasy;
using Fantasy.Event;

public class Event_LevelTimerFinished_Handler : EventSystem<LevelTimerFinished>
{
    protected override void Handler(LevelTimerFinished self)
    {
        Log.Error("[Timer] Level Finished!");
        
        // TODO: 关卡结束逻辑
        // - 显示结算界面
        // - 停止游戏
        // - 保存分数
    }
}