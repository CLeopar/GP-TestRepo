using DG.Tweening;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using TMPro;
using UnityEngine;

public class ScoreUIComponent : Entity
{
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI ScoreChangeText;
    public Tweener FloatTween;
}

public class ScoreUIComponent_Awake : AwakeSystem<ScoreUIComponent>
{
protected override void Awake(ScoreUIComponent self)
{
    var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        
    // 从 GameObject 获取 TextMeshProUGUI 组件
    var scoreTextObj = rc.Get<GameObject>("ScoreText");
    if (scoreTextObj != null)
        self.ScoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();
            
    var scoreChangeObj = rc.Get<GameObject>("ScoreChangeText");
    if (scoreChangeObj != null)
        self.ScoreChangeText = scoreChangeObj.GetComponent<TextMeshProUGUI>();
        
    if (self.ScoreChangeText != null)
        self.ScoreChangeText.gameObject.SetActive(false);
            
    Log.Error($"[ScoreUI] Awake - ScoreText: {self.ScoreText != null}, ScoreChangeText: {self.ScoreChangeText != null}");
}
}

public class ScoreUIComponent_Destroy : DestroySystem<ScoreUIComponent>  // << → 
{
    protected override void Destroy(ScoreUIComponent self)
    {
        self.FloatTween?.Kill();
    }
}