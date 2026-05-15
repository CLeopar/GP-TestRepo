using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using TMPro;
using UnityEngine;

public class LevelTimerUIComponent : Entity
{
    public TextMeshProUGUI TimerText;
}

public class LevelTimerUIComponent_Awake : AwakeSystem<LevelTimerUIComponent>
{
    protected override void Awake(LevelTimerUIComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        var timerObj = rc.Get<GameObject>("TimerText");
        if (timerObj != null)
            self.TimerText = timerObj.GetComponent<TextMeshProUGUI>();
            
        Log.Error($"[TimerUI] TimerText: {self.TimerText != null}");
    }
}