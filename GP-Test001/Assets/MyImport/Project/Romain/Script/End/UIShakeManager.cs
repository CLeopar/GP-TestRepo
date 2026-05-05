using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIShakeManager : MonoBehaviour
{
    [System.Serializable]
    public class UIEntry
    {
        public RectTransform target;    // 悬停抖动 + 点击的UI组件
        public GameObject highlight;    // 对应的描边图片
    }

    [Header("UI Entries")]
    public UIEntry[] entries;

    [Header("Shake Settings")]
    public float shakeDuration = 0.4f;
    [Range(0f, 3f)]
    public float rotateAmount = 1.5f;

    private class RuntimeEntry
    {
        public UIEntry data;
        public Tween tween;
        public Tween highlightTween;
        public bool isHovered;
        public bool isSelected;
    }

    private readonly List<RuntimeEntry> _entries = new List<RuntimeEntry>();

    private void Start()
    {
        if (entries == null) return;

        foreach (var e in entries)
        {
            if (e.target == null) continue;

            if (e.highlight != null)
                e.highlight.SetActive(false);

            var runtime = new RuntimeEntry { data = e };

            var listener = e.target.gameObject.AddComponent<UIEntryListener>();
            listener.Init(
                onEnter: () => StartShake(runtime),
                onExit:  () => StopShake(runtime),
                onClick: () => OnSelect(runtime)
            );

            _entries.Add(runtime);
        }
    }

    private void OnDestroy()
    {
        foreach (var e in _entries)
        {
            e.tween?.Kill(complete: true);
            e.highlightTween?.Kill(complete: true);
        }
    }
    
    public List<int> GetSelectedIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].isSelected)
                indices.Add(i);
        }
        return indices;
    }

    // ── 抖动 ──────────────────────────────────────────

    private void StartShake(RuntimeEntry entry)
    {
        entry.isHovered = true;
        entry.tween?.Kill(complete: true);
        entry.data.target.localRotation = Quaternion.identity;
        LoopShake(entry);
    }

    private void LoopShake(RuntimeEntry entry)
    {
        entry.data.target.localRotation = Quaternion.identity;

        // 主体抖动
        entry.tween = DOTween.Sequence()
            .Append(entry.data.target.DOLocalRotate(new Vector3(0, 0,  rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.target.DOLocalRotate(new Vector3(0, 0, -rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.target.DOLocalRotate(new Vector3(0, 0,  rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.target.DOLocalRotate(Vector3.zero,                     shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                entry.data.target.localRotation = Quaternion.identity;
                if (entry.isHovered) LoopShake(entry);
            })
            .SetUpdate(true);

        // 高亮同步抖动（如果已选中）
        if (entry.isSelected && entry.data.highlight != null)
        {
            entry.highlightTween?.Kill(complete: true);
            entry.data.highlight.transform.localRotation = Quaternion.identity;
            LoopHighlightShake(entry);
        }
    }

    private void LoopHighlightShake(RuntimeEntry entry)
    {
        if (entry.data.highlight == null) return;
        entry.data.highlight.transform.localRotation = Quaternion.identity;

        entry.highlightTween = DOTween.Sequence()
            .Append(entry.data.highlight.transform.DOLocalRotate(new Vector3(0, 0,  rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.highlight.transform.DOLocalRotate(new Vector3(0, 0, -rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.highlight.transform.DOLocalRotate(new Vector3(0, 0,  rotateAmount), shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(entry.data.highlight.transform.DOLocalRotate(Vector3.zero,                     shakeDuration * 0.25f).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                entry.data.highlight.transform.localRotation = Quaternion.identity;
                if (entry.isHovered) LoopHighlightShake(entry);
            })
            .SetUpdate(true);
    }

    private void StopShake(RuntimeEntry entry)
    {
        entry.isHovered = false;
        entry.tween?.Kill(complete: true);
        entry.tween = null;

        entry.data.target.DOLocalRotate(Vector3.zero, 0.15f)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true);

        // 高亮也停止抖动
        if (entry.data.highlight != null)
        {
            entry.highlightTween?.Kill(complete: true);
            entry.highlightTween = null;
            entry.data.highlight.transform.DOLocalRotate(Vector3.zero, 0.15f)
                 .SetEase(Ease.OutQuad)
                 .SetUpdate(true);
        }
    }

    // ── 选中 ──────────────────────────────────────────

    private void OnSelect(RuntimeEntry selected)
    {
        if (selected.isSelected)
        {
            // 已选中，取消选中
            selected.isSelected = false;
            if (selected.data.highlight != null)
            {
                selected.highlightTween?.Kill(complete: true);
                selected.highlightTween = null;
                selected.data.highlight.transform.localRotation = Quaternion.identity;
                selected.data.highlight.SetActive(false);
            }
        }
        else
        {
            // 未选中，选中并显示高亮
            selected.isSelected = true;
            if (selected.data.highlight != null)
            {
                selected.data.highlight.SetActive(true);
                // 如果当前正在悬停，立即同步抖动
                if (selected.isHovered)
                    LoopHighlightShake(selected);
            }
        }
    }

    // ── 监听器 ────────────────────────────────────────

    private class UIEntryListener : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private System.Action _onEnter;
        private System.Action _onExit;
        private System.Action _onClick;

        public void Init(System.Action onEnter, System.Action onExit, System.Action onClick)
        {
            _onEnter = onEnter;
            _onExit  = onExit;
            _onClick = onClick;
        }

        public void OnPointerEnter(PointerEventData _) => _onEnter?.Invoke();
        public void OnPointerExit(PointerEventData _)  => _onExit?.Invoke();
        public void OnPointerClick(PointerEventData _) => _onClick?.Invoke();
    }
}