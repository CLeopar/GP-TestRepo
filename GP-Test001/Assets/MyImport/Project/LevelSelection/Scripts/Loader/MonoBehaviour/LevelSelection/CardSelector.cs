using UnityEngine;

public class CardSelector : MonoBehaviour
{
    [Header("CardSetting")]
    public float cardSpacing = 4f;
    public float snapSpeed = 10f;
    public float dragThreshold = 0.5f;

    [Header("ScalingEffect")]
    public float centerScale = 1f;
    public float sideScale = 0.75f;

    [Header("Popup (2D World Space)")]
    [Tooltip("每张卡片对应一个弹窗根物体，顺序与卡片一致。\n弹窗根物体自身需挂有 BoxCollider2D。")]
    public GameObject[] cardPopups;

    [Tooltip("弹窗弹出时的半透明遮罩（2D Sprite，铺满屏幕）。留空则不使用。")]
    public GameObject popupBlocker;

    private bool popupIsOpen = false;
    private int openPopupIndex = -1;
    private bool popupJustOpened = false;
    private bool popupJustClosed = false;

    private int totalCards;
    private int currentIndex = 0;
    private int lastCenterIndex = -1;
    private float targetX;

    private bool isDragging;
    private Vector3 dragStartWorldPos;
    private float containerStartX;

    private CardController[] cardControllers;

    void Start()
    {
        totalCards = transform.childCount;
        cardControllers = new CardController[totalCards];

        for (int i = 0; i < totalCards; i++)
        {
            Transform card = transform.GetChild(i);
            card.localPosition = new Vector3(i * cardSpacing, 0, 0);

            CardController controller = card.GetComponent<CardController>();
            if (controller == null)
                Debug.LogError($"卡片 {card.name} 上没有 CardController 组件！");
            else
                cardControllers[i] = controller;
        }

        targetX = 0;
        transform.position = new Vector3(0, 0, 0);
        lastCenterIndex = currentIndex;

        UpdateCardStates();
        UpdateCardScales();
        CloseAllPopups();
    }

    void Update()
    {
        if (popupIsOpen)
        {
            if (popupJustOpened)
            {
                popupJustOpened = false;
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (!IsClickOnPopup(openPopupIndex))
                    ClosePopup();
            }
            return;
        }

        if (popupJustClosed)
        {
            popupJustClosed = false;
            return;
        }

        HandleInput();

        if (!isDragging)
        {
            float newX = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * snapSpeed);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            UpdateCardScalesSmooth();
            CheckAndUpdateCenterCard();
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartWorldPos = GetMouseWorldPos();
            containerStartX = transform.position.x;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            float deltaX = GetMouseWorldPos().x - dragStartWorldPos.x;
            transform.position = new Vector3(containerStartX + deltaX,
                                             transform.position.y,
                                             transform.position.z);
            UpdateCardScalesSmooth();
            CheckAndUpdateCenterCard();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            float dragDistance = GetMouseWorldPos().x - dragStartWorldPos.x;

            if (Mathf.Abs(dragDistance) > dragThreshold)
            {
                if (dragDistance < 0)
                    currentIndex = Mathf.Min(currentIndex + 1, totalCards - 1);
                else
                    currentIndex = Mathf.Max(currentIndex - 1, 0);
            }

            SnapToCard(currentIndex);
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    void SnapToCard(int index)
    {
        targetX = -index * cardSpacing;
        UpdateCardStates();
        UpdateCardScales();
    }

    void CheckAndUpdateCenterCard()
    {
        int newCenterIndex = GetClosestCardIndex();
        if (newCenterIndex != lastCenterIndex)
        {
            if (lastCenterIndex >= 0 && lastCenterIndex < totalCards && cardControllers[lastCenterIndex] != null)
                cardControllers[lastCenterIndex].SetCenterState(false);

            if (newCenterIndex >= 0 && newCenterIndex < totalCards && cardControllers[newCenterIndex] != null)
                cardControllers[newCenterIndex].SetCenterState(true);

            lastCenterIndex = newCenterIndex;
        }
    }

    int GetClosestCardIndex()
    {
        float minDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < totalCards; i++)
        {
            float cardWorldX = transform.position.x + i * cardSpacing;
            float dist = Mathf.Abs(cardWorldX);
            if (dist < minDist) { minDist = dist; closestIndex = i; }
        }
        return closestIndex;
    }

    void UpdateCardStates()
    {
        for (int i = 0; i < totalCards; i++)
            if (cardControllers[i] != null)
                cardControllers[i].SetCenterState(i == currentIndex);
    }

    void UpdateCardScales()
    {
        for (int i = 0; i < totalCards; i++)
        {
            float s = (i == currentIndex) ? centerScale : sideScale;
            transform.GetChild(i).localScale = Vector3.one * s;
        }
    }

    void UpdateCardScalesSmooth()
    {
        for (int i = 0; i < totalCards; i++)
        {
            float cardWorldX = transform.position.x + i * cardSpacing;
            float dist = Mathf.Abs(cardWorldX);
            float t = Mathf.Clamp01(1f - dist / cardSpacing);
            float scale = Mathf.Lerp(sideScale, centerScale, t);
            transform.GetChild(i).localScale = Vector3.one * scale;
        }
    }

    public void GoToCard(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, totalCards - 1);
        SnapToCard(currentIndex);
    }

    public int GetCurrentIndex() => currentIndex;
    public CardController GetCardController(int i) => (i >= 0 && i < totalCards) ? cardControllers[i] : null;

    public void OnCardClicked(int index)
    {
        if (index != currentIndex) return;
        if (popupIsOpen) return;
        if (popupJustClosed) return;
        OpenPopup(index);
    }

    void OpenPopup(int index)
    {
        if (cardPopups == null || index >= cardPopups.Length || cardPopups[index] == null)
        {
            Debug.LogWarning($"卡片 {index} 没有对应的弹窗，请在 Inspector 里赋值。");
            return;
        }

        CloseAllPopups();
        cardPopups[index].SetActive(true);

        if (popupBlocker != null)
            popupBlocker.SetActive(true);

        popupIsOpen = true;
        openPopupIndex = index;
        popupJustOpened = true;
    }

    void ClosePopup()
    {
        CloseAllPopups();

        if (popupBlocker != null)
            popupBlocker.SetActive(false);

        popupIsOpen = false;
        openPopupIndex = -1;
        popupJustClosed = true;

        isDragging = false;
        currentIndex = GetClosestCardIndex();
        SnapToCard(currentIndex);
    }

    void CloseAllPopups()
    {
        if (cardPopups == null) return;
        foreach (GameObject popup in cardPopups)
            if (popup != null) popup.SetActive(false);
    }

    /// <summary>
    /// 只检测弹窗根物体自身挂的 Collider2D，完全忽略子物体。
    /// </summary>
    bool IsClickOnPopup(int index)
    {
        if (cardPopups == null || index < 0 || index >= cardPopups.Length) return false;
        GameObject popup = cardPopups[index];
        if (popup == null) return false;

        // ── 只取根物体自身的 Collider2D，不查子物体 ──────────────────────
        Collider2D col = popup.GetComponent<Collider2D>();
        if (col == null || !col.enabled) return false;
        // ─────────────────────────────────────────────────────────────────

        float popupZ = popup.transform.position.z;
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(popupZ - Camera.main.transform.position.z);
        Vector2 point2D = Camera.main.ScreenToWorldPoint(screenPos);

        return col.OverlapPoint(point2D);
    }

    public void CloseCurrentPopup() => ClosePopup();
}