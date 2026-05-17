using UnityEngine;

/// <summary>
/// 子级 SpriteRenderer 排序层统一控制器
/// 挂载在父级物体上，可在 Inspector 直接调整所有子级（含孙级）的
/// Sorting Layer 和 Order In Layer，支持相对偏移模式。
/// </summary>
public class ChildSortingOrderController: MonoBehaviour
{
    // ──────────────────────────────────────────
    //  Inspector 可配置字段
    // ──────────────────────────────────────────

    [Header("Order In Layer")]
    [Tooltip("统一设置所有子级 SpriteRenderer 的 Order In Layer")]
    public int orderInLayer = 0;

    [Tooltip("启用后，每个子级在原始值的基础上加上 orderInLayer，而非直接覆盖")]
    public bool useRelativeOffset = false;

    [Header("Sorting Layer（可选）")]
    [Tooltip("启用后同时修改 Sorting Layer Name（留空则不修改）")]
    public bool overrideSortingLayer = false;

    [Tooltip("目标 Sorting Layer 名称（需在 Project Settings 中已存在）")]
    public string sortingLayerName = "Default";

    [Header("工具")]
    [Tooltip("在 Inspector 修改值时自动实时应用（Editor 模式下有效）")]
    public bool applyOnValidate = true;

    // ──────────────────────────────────────────
    //  私有变量
    // ──────────────────────────────────────────

    // 记录每个 SpriteRenderer 的原始 Order，用于相对偏移模式
    private SpriteRenderer[] _renderers;
    private int[] _originalOrders;

    // ──────────────────────────────────────────
    //  Unity 生命周期
    // ──────────────────────────────────────────

    private void Awake()
    {
        CacheRenderers();
    }

    private void Start()
    {
        Apply();
    }

    // ──────────────────────────────────────────
    //  Editor 实时预览（不进入 Play 也能看到效果）
    // ──────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!applyOnValidate) return;
        // OnValidate 在对象构建期间调用，需延迟一帧避免警告
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            CacheRenderers();
            Apply();
        };
    }
#endif

    // ──────────────────────────────────────────
    //  核心方法（也可从外部代码调用）
    // ──────────────────────────────────────────

    /// <summary>
    /// 立即将当前设置应用到所有子级 SpriteRenderer
    /// </summary>
    public void Apply()
    {
        if (_renderers == null || _renderers.Length == 0)
            CacheRenderers();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            // Order In Layer
            _renderers[i].sortingOrder = useRelativeOffset
                ? _originalOrders[i] + orderInLayer
                : orderInLayer;

            // Sorting Layer（可选）
            if (overrideSortingLayer && !string.IsNullOrEmpty(sortingLayerName))
                _renderers[i].sortingLayerName = sortingLayerName;
        }
    }

    /// <summary>
    /// 重新扫描所有子级 SpriteRenderer 并刷新缓存
    /// （子级动态增减后手动调用，或直接点 Inspector 上的按钮）
    /// </summary>
    public void CacheRenderers()
    {
        // GetComponentsInChildren 默认包含自身，inactive 物体也一并获取
        _renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        _originalOrders = new int[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalOrders[i] = _renderers[i].sortingOrder;
    }
}