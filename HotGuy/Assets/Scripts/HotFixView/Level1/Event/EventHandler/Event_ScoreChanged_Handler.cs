using Fantasy;
using Fantasy.Event;
using DG.Tweening;
using UnityEngine;

public class Event_ScoreChanged_Handler : EventSystem<ScoreChanged>
{
    protected override void Handler(ScoreChanged self)
    {
        Log.Error($"[ScoreEvent] Delta: {self.Delta}, Current: {self.CurrentScore}");
        
        var ui = GameEntry.Instance._scene.GetComponent<ScoreUIComponent>();
        if (ui == null)
        {
            Log.Error("[ScoreEvent] ScoreUIComponent is null!");
            return;
        }
        
        // 更新主分数
        if (ui.ScoreText != null)
        {
            ui.ScoreText.text = self.CurrentScore.ToString();
            ui.ScoreText.transform.DOKill();
            ui.ScoreText.transform.DOScale(1.3f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => ui.ScoreText.transform.DOScale(1f, 0.15f));
        }

        // 飘字动画
        if (ui.ScoreChangeText != null && self.Delta != 0)
        {
            ShowFloatingText(ui, self.Delta);
        }
    }

    private void ShowFloatingText(ScoreUIComponent ui, int delta)
    {
        // 先确保物体激活
        ui.ScoreChangeText.gameObject.SetActive(true);
        
        // 重置位置和透明度
        ui.ScoreChangeText.transform.localPosition = Vector3.zero;
        ui.ScoreChangeText.color = new Color(
            ui.ScoreChangeText.color.r,
            ui.ScoreChangeText.color.g,
            ui.ScoreChangeText.color.b,
            1f
        );
        
        // 设置文本和颜色
        ui.ScoreChangeText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        ui.ScoreChangeText.color = delta > 0 ? Color.green : Color.red;

        // 杀掉旧动画
        ui.FloatTween?.Kill();
        DOTween.Kill(ui.ScoreChangeText.transform);
        DOTween.Kill(ui.ScoreChangeText);

        // 飘上去
        ui.FloatTween = ui.ScoreChangeText.transform
            .DOLocalMoveY(50f, 0.8f)
            .SetEase(Ease.OutCubic);

        // 淡出（用 DOFade 对 CanvasGroup 或 TextMeshPro 的 faceColor）
        ui.ScoreChangeText.DOFade(0f, 0.6f)
            .SetDelay(0.2f)
            .OnComplete(() => 
            {
                ui.ScoreChangeText.gameObject.SetActive(false);
                // 重置透明度方便下次使用
                ui.ScoreChangeText.color = new Color(
                    ui.ScoreChangeText.color.r,
                    ui.ScoreChangeText.color.g,
                    ui.ScoreChangeText.color.b,
                    1f
                );
            });
    }
}