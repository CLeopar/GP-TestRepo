using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardController : MonoBehaviour
{
    [Header("State")]
    public bool isHovering;
    public bool isDragging;

    [Header("Animation")]
    public string centerStateTrigger = "IsCenter";
    public Animator[] cardAnimators;

    // ── NEW: 拖拽判定阈值（世界单位），超过此距离视为拖拽，不触发点击 ──────
    [Header("Click Detection")]
    [Tooltip("鼠标移动超过该距离（世界单位）视为拖拽，松手时不触发弹窗。")]
    public float clickMoveThreshold = 0.15f;
    // ─────────────────────────────────────────────────────────────────────

    private int cardIndex;
    private bool isCenter = false;

    // ── NEW ──────────────────────────────────────────────────────────────
    private Vector3 mouseDownWorldPos;  // 按下时记录位置
    private bool movedTooFar;        // 是否超过阈值（拖拽中）
    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        cardIndex = transform.GetSiblingIndex();

        if (cardAnimators == null || cardAnimators.Length == 0)
            cardAnimators = GetComponentsInChildren<Animator>();
    }

    private void OnMouseEnter() => isHovering = true;
    private void OnMouseExit() => isHovering = false;

    private void OnMouseDown()
    {
        isDragging = true;
        movedTooFar = false;
        mouseDownWorldPos = GetMouseWorldPos();
    }

    // ── NEW: 在 OnMouseDrag 里持续检测是否移动过远 ──────────────────────
    private void OnMouseDrag()
    {
        if (!movedTooFar)
        {
            float dist = Vector3.Distance(GetMouseWorldPos(), mouseDownWorldPos);
            if (dist > clickMoveThreshold)
                movedTooFar = true;
        }
    }
    // ─────────────────────────────────────────────────────────────────────

    private void OnMouseUp()
    {
        isDragging = false;
    }

    // ── CHANGED: 只有未发生拖拽时才通知 CardSelector ────────────────────
    private void OnMouseUpAsButton()
    {
        if (movedTooFar) return;    // 拖拽结束，忽略

        CardSelector selector = GetComponentInParent<CardSelector>();
        if (selector != null)
            selector.OnCardClicked(cardIndex);
    }
    // ─────────────────────────────────────────────────────────────────────

    public void SetCenterState(bool center)
    {
        if (isCenter == center) return;
        isCenter = center;
        foreach (Animator anim in cardAnimators)
            if (anim != null)
                anim.SetBool(centerStateTrigger, center);
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mp);
    }

    public float ParentIndex() => cardIndex;
    public int GetCardIndex() => cardIndex;
    public bool IsCenter() => isCenter;
    public bool IsHovering() => isHovering;
    public bool IsDragging() => isDragging;
}