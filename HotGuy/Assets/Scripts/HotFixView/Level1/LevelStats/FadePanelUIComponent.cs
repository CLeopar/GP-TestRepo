using DG.Tweening;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using UnityEngine.UI;

public class FadePanelUIComponent : Entity
{
    public Image FadeImage;
    public float FadeDuration = 1.5f;
    
    private Tweener _fadeTween;

    public void FadeIn(System.Action onComplete = null)
    {
        _fadeTween?.Kill();
        
        FadeImage.gameObject.SetActive(true);
        FadeImage.color = new Color(0, 0, 0, 0);
        
        _fadeTween = FadeImage.DOFade(1f, FadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void FadeOut(System.Action onComplete = null)
    {
        _fadeTween?.Kill();
        
        FadeImage.color = new Color(0, 0, 0, 1);
        
        _fadeTween = FadeImage.DOFade(0f, FadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                FadeImage.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
}

public class FadePanelUIComponent_Awake : AwakeSystem<FadePanelUIComponent>
{
    protected override void Awake(FadePanelUIComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        var fadeObj = rc.Get<GameObject>("FadePanel");
        
        if (fadeObj != null)
        {
            self.FadeImage = fadeObj.GetComponent<Image>();
            fadeObj.SetActive(false);
        }
        
        Log.Error($"[FadePanel] FadeImage: {self.FadeImage != null}");
    }
}