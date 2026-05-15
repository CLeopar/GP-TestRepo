using Fantasy;
using Fantasy.Event;
using DG.Tweening;
using UnityEngine;

public class Event_ScoreChanged_Handler : EventSystem<ScoreChanged>
{
    protected override void Handler(ScoreChanged self)
    {
        var ui = GameEntry.Instance._scene.GetComponent<ScoreUIComponent>();
        if (ui == null) return;
        
        // 更新主分数
        if (ui.ScoreText != null)
        {
            ui.ScoreText.text = self.CurrentScore.ToString();
            // 缩放动画
            ui.ScoreText.transform.DOScale(1.3f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => ui.ScoreText.transform.DOScale(1f, 0.15f));
        }

        // 飘字动画（自动判断红/绿）
        if (ui.ScoreChangeText != null && self.Delta != 0)
        {
            ShowFloatingText(ui, self.Delta);
        }
    }

    private void ShowFloatingText(ScoreUIComponent ui, int delta)
    {
        ui.ScoreChangeText.gameObject.SetActive(true);
        ui.ScoreChangeText.transform.localPosition = Vector3.zero;
        ui.ScoreChangeText.color = new Color(
            ui.ScoreChangeText.color.r,
            ui.ScoreChangeText.color.g,
            ui.ScoreChangeText.color.b,
            1f
        );
        
        // 设置文本：+15 或 -50
        ui.ScoreChangeText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        
        // ========== 颜色自动切换 ==========
        ui.ScoreChangeText.color = delta > 0 ? Color.green : Color.red;

        // 飘上去 + 淡出
        ui.FloatTween = ui.ScoreChangeText.transform
            .DOLocalMoveY(50f, 0.8f)
            .SetEase(Ease.OutCubic);

        ui.ScoreChangeText.DOFade(0f, 0.6f)
            .SetDelay(0.2f)
            .OnComplete(() => 
            {
                ui.ScoreChangeText.gameObject.SetActive(false);
                ui.ScoreChangeText.color = new Color(
                    ui.ScoreChangeText.color.r,
                    ui.ScoreChangeText.color.g,
                    ui.ScoreChangeText.color.b,
                    1f
                );
            });
    }
}