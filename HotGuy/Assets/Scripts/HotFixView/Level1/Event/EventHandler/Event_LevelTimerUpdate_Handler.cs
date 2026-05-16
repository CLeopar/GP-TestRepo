using Fantasy;
using Fantasy.Event;
using UnityEngine;

public class Event_LevelTimerUpdate_Handler : EventSystem<LevelTimerUpdate>
{
    protected override void Handler(LevelTimerUpdate self)
    {
        var ui = GameEntry.Instance._scene.GetComponent<LevelTimerUIComponent>();
        if (ui?.TimerText == null) return;
        
        // 转换为秒显示
        long remainingSeconds = self.RemainingTime / 1000;
        long minutes = remainingSeconds / 60;
        long seconds = remainingSeconds % 60;
        
        ui.TimerText.text = $"{minutes:D2}:{seconds:D2}";
        
        // 最后10秒变红
        if (remainingSeconds <= 10)
            ui.TimerText.color = Color.red;
        else
            ui.TimerText.color = new Color(90f/255f, 57f/255f, 57f/255f, 255f/255f);
    }
}