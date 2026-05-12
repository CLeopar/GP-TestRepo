using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将子对象图片在指定范围内无限循环滚动（适用于 Unity UI / Canvas）
/// 挂载到包含所有滚动图片的父 GameObject 上
/// </summary>
public class InfiniteScroll : MonoBehaviour
{
    public enum ScrollDirection { Horizontal, Vertical, Both }

    [Header("滚动设置")]
    [Tooltip("滚动方向")]
    public ScrollDirection direction = ScrollDirection.Horizontal;

    [Tooltip("滚动速度（像素/秒，负值反向）")]
    public float speedX = -100f;
    public float speedY = 0f;

    [Header("范围设置")]
    [Tooltip("水平滚动范围（超出后重置），通常等于单张图片宽度 × 列数")]
    public float scrollRangeX = 800f;

    [Tooltip("垂直滚动范围")]
    public float scrollRangeY = 600f;

    // 内部状态
    private List<RectTransform> _items = new List<RectTransform>();
    private Vector2 _offset = Vector2.zero;

    void Start()
    {
        // 收集所有直接子对象的 RectTransform
        foreach (Transform child in transform)
        {
            var rt = child.GetComponent<RectTransform>();
            if (rt != null) _items.Add(rt);
        }
    }

    void Update()
    {
        // 累加偏移量
        if (direction != ScrollDirection.Vertical)
            _offset.x += speedX * Time.deltaTime;

        if (direction != ScrollDirection.Horizontal)
            _offset.y += speedY * Time.deltaTime;

        // 边界检测 & 重置（支持正负两个方向）
        if (direction != ScrollDirection.Vertical)
        {
            if (_offset.x <= -scrollRangeX) _offset.x += scrollRangeX;
            if (_offset.x >= scrollRangeX)  _offset.x -= scrollRangeX;
        }

        if (direction != ScrollDirection.Horizontal)
        {
            if (_offset.y <= -scrollRangeY) _offset.y += scrollRangeY;
            if (_offset.y >= scrollRangeY)  _offset.y -= scrollRangeY;
        }

        // 应用偏移到所有子对象
        foreach (var item in _items)
        {
            var pos = item.anchoredPosition;

            if (direction != ScrollDirection.Vertical)
                pos.x = GetItemBaseX(item) + _offset.x;

            if (direction != ScrollDirection.Horizontal)
                pos.y = GetItemBaseY(item) + _offset.y;

            item.anchoredPosition = pos;
        }
    }

    // 获取子对象的基准位置（初始位置缓存）
    private Dictionary<RectTransform, Vector2> _basePositions;

    void Awake()
    {
        _basePositions = new Dictionary<RectTransform, Vector2>();
        foreach (Transform child in transform)
        {
            var rt = child.GetComponent<RectTransform>();
            if (rt != null)
                _basePositions[rt] = rt.anchoredPosition;
        }
    }

    private float GetItemBaseX(RectTransform rt) =>
        _basePositions.TryGetValue(rt, out var v) ? v.x : 0f;

    private float GetItemBaseY(RectTransform rt) =>
        _basePositions.TryGetValue(rt, out var v) ? v.y : 0f;
}