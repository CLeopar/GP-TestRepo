using System.Collections;
using UnityEngine;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("对话内容")]
    [TextArea]
    public string[] dialogues;

    [Header("计时设置")]
    public float minInterval = 3f;
    public float maxInterval = 8f;
    public float displayDuration = 3f;

    [Header("UI")]
    public GameObject dialogBubble;
    public TextMeshProUGUI dialogText;

    [Header("动画")]
    public Animator targetAnimator;
    public string triggerName = "Talk";
    public string allowedStateName = "Idle";
    public int layerIndex = 0;

    [Header("气泡宽度")]
    public float bubbleMinWidth = 80f;
    public float bubbleMaxWidth = 280f;
    public float charWidth = 14f;
    public float bubblePadding = 24f;

    private RectTransform bubbleRect;

    void Start()
    {
        bubbleRect = dialogBubble.GetComponent<RectTransform>();

        bubbleRect.anchorMin = new Vector2(0f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0f, 0.5f);
        bubbleRect.pivot     = new Vector2(0f, 0.5f);

        dialogBubble.SetActive(false);
        dialogText.gameObject.SetActive(false);

        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        while (true)
        {
            // ★ 关键：只有 Idle 才允许进入行为循环
            yield return new WaitUntil(CanSpeak);

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            // 再次确认（防止中途状态变化）
            if (!CanSpeak())
                continue;

            string line = dialogues[Random.Range(0, dialogues.Length)];
            ShowDialogue(line);

            yield return new WaitForSeconds(displayDuration);

            HideDialogue();
        }
    }

    void ShowDialogue(string text)
    {
        dialogText.text = text;
        dialogText.enableWordWrapping = false;

        int charCount = text.Length;
        float targetWidth = charCount * charWidth + bubblePadding;
        targetWidth = Mathf.Clamp(targetWidth, bubbleMinWidth, bubbleMaxWidth);

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            targetWidth
        );

        dialogBubble.SetActive(true);
        dialogText.gameObject.SetActive(true);

        // ★ 说话动画（只在 Idle 才会进来）
        targetAnimator.SetTrigger(triggerName);
    }

    void HideDialogue()
    {
        dialogBubble.SetActive(false);
        dialogText.gameObject.SetActive(false);
    }

    bool CanSpeak()
    {
        if (targetAnimator == null)
            return false;

        if (targetAnimator.IsInTransition(layerIndex))
            return false;

        AnimatorStateInfo state =
            targetAnimator.GetCurrentAnimatorStateInfo(layerIndex);

        return state.IsName(allowedStateName);
    }
}