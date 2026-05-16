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
    
    // ========== 新增：Canvas 引用（用于世界坐标转屏幕坐标）==========
    public Canvas MainCanvas;
    public Camera UICamera;
}

public class ScoreUIComponent_Awake : AwakeSystem<ScoreUIComponent>
{
    protected override void Awake(ScoreUIComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        
        var scoreTextObj = rc.Get<GameObject>("ScoreText");
        if (scoreTextObj != null)
            self.ScoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();
            
        var scoreChangeObj = rc.Get<GameObject>("ScoreChangeText");
        if (scoreChangeObj != null)
            self.ScoreChangeText = scoreChangeObj.GetComponent<TextMeshProUGUI>();
        
        // ========== 新增：获取 Canvas 和 Camera ==========
        self.MainCanvas = scoreChangeObj?.GetComponentInParent<Canvas>();
        self.UICamera = self.MainCanvas?.worldCamera ?? Camera.main;
        
        if (self.ScoreChangeText != null)
            self.ScoreChangeText.gameObject.SetActive(false);
            
        Log.Error($"[ScoreUI] Awake - ScoreText: {self.ScoreText != null}, ScoreChangeText: {self.ScoreChangeText != null}");
    }
}

public class ScoreUIComponent_Destroy : DestroySystem<ScoreUIComponent>
{
    protected override void Destroy(ScoreUIComponent self)
    {
        self.FloatTween?.Kill();
    }
}