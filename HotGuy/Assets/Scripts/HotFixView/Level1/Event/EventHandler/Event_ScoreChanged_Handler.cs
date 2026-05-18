using Fantasy;
using Fantasy.Event;
using DG.Tweening;
using UnityEngine;

public class Event_ScoreChanged_Handler : EventSystem<ScoreChanged>
{
    protected override void Handler(ScoreChanged self)
    {
        Log.Error($"[ScoreEvent] Delta: {self.Delta}, Current: {self.CurrentScore}, Pos: {self.WorldPosition}");
        
        var ui = GameEntry.Instance._scene.GetComponent<ScoreUIComponent>();
        if (ui == null)
        {
            Log.Error("[ScoreEvent] ScoreUIComponent is null!");
            return;
        }
        
        // 更新主分数（固定位置）
        if (ui.ScoreText != null)
        {
            ui.ScoreText.text = self.CurrentScore.ToString();
            ui.ScoreText.transform.DOKill();
            ui.ScoreText.transform.DOScale(1.3f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => ui.ScoreText.transform.DOScale(1f, 0.15f));
        }

        // 飘字动画（在事件发生位置）
        if (ui.ScoreChangeText != null && self.Delta != 0)
        {
            ShowFloatingTextAtPosition(ui, self.Delta, self.WorldPosition);
        }
    }

    /// <summary>
    /// 在指定位置显示飘字
    /// worldPos 可以是：
    ///   - 3D 世界坐标（食物等场景物体）→ 用 WorldToScreenPoint 转换
    ///   - UI 屏幕坐标（SCTaskUI 等 Overlay UI）→ z==0 且在屏幕范围内，直接用
    /// </summary>
    private void ShowFloatingTextAtPosition(ScoreUIComponent ui, int delta, Vector3 worldPos)
    {
        var textObj = ui.ScoreChangeText;
        var parentRect = textObj.transform.parent as RectTransform;

        // ========== 判断是屏幕坐标还是世界坐标 ==========
        Vector3 screenPos;
        bool isScreenPos = worldPos.z == 0f
            && worldPos.x >= 0 && worldPos.x <= Screen.width
            && worldPos.y >= 0 && worldPos.y <= Screen.height;

        if (isScreenPos)
        {
            // 已经是屏幕坐标，直接用
            screenPos = worldPos;
        }
        else
        {
            // 世界坐标，转成屏幕坐标
            screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 如果物体在相机后方，显示在屏幕中心
            if (screenPos.z < 0)
                screenPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }

        // ScreenPointToLocalPointInRectangle：Overlay 模式 camera 传 null
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            null,  // ← Overlay 模式传 null
            out localPos);

        Log.Error($"[ScoreFloat] World: {worldPos}, Screen: {screenPos}, Local: {localPos}");

        // 激活并设置位置
        textObj.gameObject.SetActive(true);
        textObj.transform.localPosition = localPos;
        textObj.transform.SetAsLastSibling(); // 确保在最上层
        
        // 重置透明度
        textObj.color = new Color(textObj.color.r, textObj.color.g, textObj.color.b, 1f);

        // 设置文本和颜色
        textObj.text = delta > 0 ? $"+{delta}" : delta.ToString();
        textObj.color = delta > 0 ? Color.green : Color.red;

        // 杀掉旧动画
        ui.FloatTween?.Kill();
        DOTween.Kill(textObj.transform);
        DOTween.Kill(textObj);

        // 飘上去（相对当前位置向上飘）
        ui.FloatTween = textObj.transform
            .DOLocalMoveY(localPos.y + 100f, 1.2f)
            .SetEase(Ease.OutCubic);

        // 淡出
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