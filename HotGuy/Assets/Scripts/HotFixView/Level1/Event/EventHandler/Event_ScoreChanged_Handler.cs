// Event_ScoreChanged_Handler.cs
using Fantasy;
using Fantasy.Event;
using DG.Tweening;
using UnityEngine;

public class Event_ScoreChanged_Handler : EventSystem<ScoreChanged>
{
    protected override void Handler(ScoreChanged self)
    {
        Log.Error($"[ScoreEvent] Delta: {self.Delta}, Current: {self.CurrentScore}, Pos: {self.WorldPosition}");
        
        // 播放加分/减分音效
        var sfxType = self.Delta >= 0 ? SFXType.ScoreWin : SFXType.ScoreWrong;
        GameEntry.Instance._scene.EventComponent.Publish(new PlaySFX
        {
            Type = sfxType,
            WorldPos = self.WorldPosition
        });
        
        var ui = GameEntry.Instance._scene.GetComponent<ScoreUIComponent>();
        if (ui == null)
        {
            Log.Error("[ScoreEvent] ScoreUIComponent is null!");
            return;
        }
        
        if (ui.ScoreText != null)
        {
            ui.ScoreText.text = self.CurrentScore.ToString();
            ui.ScoreText.transform.DOKill();
            ui.ScoreText.transform.DOScale(1.3f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => ui.ScoreText.transform.DOScale(1f, 0.15f));
        }

        if (ui.ScoreChangeText != null && self.Delta != 0)
        {
            ShowFloatingTextAtPosition(ui, self.Delta, self.WorldPosition);
        }
    }

    private void ShowFloatingTextAtPosition(ScoreUIComponent ui, int delta, Vector3 worldPos)
    {
        var textObj = ui.ScoreChangeText;
        var parentRect = textObj.transform.parent as RectTransform;

        Vector3 screenPos;
        
        bool isProbablyWorldPos = Mathf.Abs(worldPos.x) < 50 && Mathf.Abs(worldPos.y) < 50;

        if (isProbablyWorldPos)
        {
            screenPos = Camera.main != null 
                ? Camera.main.WorldToScreenPoint(worldPos)
                : new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }
        else
        {
            screenPos = worldPos;
        }
        
        if (screenPos.z < 0)
            screenPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            null,
            out localPos);

        Log.Error($"[ScoreFloat] World: {worldPos}, Screen: {screenPos}, Local: {localPos}");

        textObj.gameObject.SetActive(true);
        textObj.transform.localPosition = localPos;
        textObj.transform.SetAsLastSibling();
        
        textObj.color = new Color(textObj.color.r, textObj.color.g, textObj.color.b, 1f);

        textObj.text = delta > 0 ? $"+{delta}" : delta.ToString();
        textObj.color = delta > 0 ? Color.green : Color.red;

        ui.FloatTween?.Kill();
        DOTween.Kill(textObj.transform);
        DOTween.Kill(textObj);

        ui.FloatTween = textObj.transform
            .DOLocalMoveY(localPos.y + 100f, 1.2f)
            .SetEase(Ease.OutCubic);

        textObj.DOFade(0f, 0.8f)
            .SetDelay(0.4f)
            .OnComplete(() => 
            {
                textObj.gameObject.SetActive(false);
                textObj.color = new Color(
                    textObj.color.r,
                    textObj.color.g,
                    textObj.color.b,
                    1f
                );
            });
    }
}