using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class Level
    {
        public GameObject levelObj;
        [HideInInspector] public List<PoseEditorController> poseEditorControllerList;
        [HideInInspector] public TMP_Text similarityText;
        [HideInInspector] public float similarity = 0f;
        public string promptString;

        [Tooltip("进入关卡时触发的 Trigger")]
        public string enterTrigger = "Enter";

        [Tooltip("倒计时结束时触发的 Trigger")]
        public string exitTrigger = "Exit";

        [Tooltip("所有关节都需要移动到这个区域内才能得到 100% 完成度")]
        public PolygonZone zone = new PolygonZone();

        public void Init()
        {
            poseEditorControllerList = new List<PoseEditorController>();
            if (levelObj == null) return;

            foreach (var controller in levelObj.GetComponentsInChildren<PoseEditorController>(true))
                poseEditorControllerList.Add(controller);

            Transform similarityTf = levelObj.transform.Find("Similarity");
            if (similarityTf != null)
                similarityText = similarityTf.GetComponent<TMP_Text>();
        }
    }

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerImage;
    [SerializeField] private float time = 30f;

    [Header("Flow Time")]
    [Tooltip("进关卡触发 enterTrigger 后等待时长（秒）")]
    [SerializeField] private float enterWaitTime = 1.0f;
    [Tooltip("闪光灯完全结束后，到下一关前的等待时长（秒）")]
    [SerializeField] private float betweenLevelDelay = 2.0f;

    [Header("Capture")]
    [SerializeField] private Image flashImage;

    [Header("Animator")]
    [Tooltip("全关卡共用的目标 Animator")]
    [SerializeField] private Animator targetAnimator;
    [Tooltip("每次切到下一关时额外触发的 Animator（可选）")]
    [SerializeField] private Animator levelTransitionAnimator;
    [Tooltip("切到下一关时触发的 Trigger 名称")]
    [SerializeField] private string levelTransitionTrigger = "Enter";

    [Header("Level")]
    [Tooltip("将所有关卡拖入此列表（资源池）")]
    [SerializeField] private List<Level> levelList;
    [Tooltip("每局正式关卡数量，从资源池中随机抽取")]
    [SerializeField] private int levelCount = 5;

    [Header("Prompt")]
    [Tooltip("在场景中放置与 levelCount 数量相同的 TMP Text，按顺序拖入（第0个对应第1关，以此类推）")]
    [SerializeField] private TMP_Text[] promptTexts;

    [Header("Picture")]
    [Tooltip("在场景中放置与 levelCount 数量相同的 Image，按顺序拖入（第0个对应第1关，以此类推）")]
    [SerializeField] private Image[] pictureImages;

    [Header("Completion UI")]
    [Tooltip("所有关卡共用的完成度文本")]
    [SerializeField] private TMP_Text completionText;
    [Tooltip("完成度文本上的 Animator（用于下一关触发动画）")]
    [SerializeField] private Animator completionTextAnimator;
    [Tooltip("切到下一关时，在完成度文本 Animator 上触发的 Trigger")]
    [SerializeField] private string completionNextLevelTrigger = "Next";
    [Tooltip("每关结束后，延迟多少秒显示完成度文本")]
    [SerializeField] private float completionShowDelay = 0.5f;
    [Tooltip("完成度从 0 增长到目标值的动画时长（秒）")]
    [SerializeField] private float completionCountDuration = 0.8f;
    [Tooltip("分数显示完成后，延迟多少秒触发 completionTextAnimator 上的 Trigger")]
    [SerializeField] private float completionTriggerDelay = 1.0f;
    [Tooltip("分数显示完成后触发的 Trigger 名称")]
    [SerializeField] private string completionFinishTrigger = "Finish";

    [Tooltip("完成度为 0% 时的字体缩放倍数")]
    [SerializeField] private float completionScaleMin = 0.8f;
    [Tooltip("完成度为 100% 时的字体缩放倍数")]
    [SerializeField] private float completionScaleMax = 2.0f;

    [Header("Results Screen")]
    [Tooltip("结算面板根节点，游戏结束时自动激活")]
    [SerializeField] private GameObject resultsPanel;
    [Tooltip("结算面板上用于展示截图的 Image 列表，顺序对应第1关、第2关……")]
    [SerializeField] private Image[] resultsImages;

    [Header("Game Over")]
    [Tooltip("所有关卡结束后触发的事件列表")]
    [SerializeField] private UnityEvent onAllLevelsComplete;

    // ── 运行时数据 ──────────────────────────────────
    private float timer = 0f;
    private float timerImageInitialWidth;
    private int currentLevel = 0;
    private List<Level> activeLevels = new List<Level>();

    private readonly List<Texture2D> capturedTextures = new List<Texture2D>();
    private readonly List<Sprite>    capturedSprites   = new List<Sprite>();

    // ── 公开只读访问，供其他脚本直接使用内存数据 ──
    /// <summary>本局所有截图的 Texture2D，适合赋给 RawImage 或材质</summary>
    public IReadOnlyList<Texture2D> CapturedTextures => capturedTextures;
    /// <summary>本局所有截图的 Sprite，适合赋给 Image.sprite</summary>
    public IReadOnlyList<Sprite>    CapturedSprites   => capturedSprites;

    public List<Level> LevelList    => levelList;
    public int         CurrentLevel => currentLevel;

    // ───────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (timerImage != null)
            timerImageInitialWidth = timerImage.sizeDelta.x;

        foreach (var level in levelList)
        {
            if (level == null || level.levelObj == null) continue;
            level.Init();
            level.levelObj.SetActive(false);
        }

        int count = Mathf.Min(levelCount, levelList.Count);
        activeLevels = levelList
            .Where(l => l != null && l.levelObj != null)
            .OrderBy(_ => Random.value)
            .Take(count)
            .ToList();

        if (activeLevels.Count == 0) return;

        for (int i = 0; i < promptTexts.Length; i++)
        {
            if (promptTexts[i] == null) continue;
            promptTexts[i].text = i < activeLevels.Count ? activeLevels[i].promptString : string.Empty;
        }

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
            completionText.transform.localScale = Vector3.one * completionScaleMin;
        }

        // 结算面板初始隐藏
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        currentLevel = 0;
        StartCoroutine(RunCurrentLevelFlow());
    }

    // ── 主流程 ──────────────────────────────────────
    private IEnumerator RunCurrentLevelFlow()
    {
        Level level = activeLevels[currentLevel];
        level.levelObj.SetActive(true);

        SetControllersEnabled(level, false);
        TriggerAnimator(targetAnimator, level.enterTrigger);

        if (enterWaitTime > 0f)
            yield return new WaitForSeconds(enterWaitTime);

        SetControllersEnabled(level, true);
        yield return StartCoroutine(TimingCoroutine());

        // 闪光
        if (flashImage != null)
            yield return flashImage.DOFade(1f, 0.05f).WaitForCompletion();

        if (currentLevel < pictureImages.Length && pictureImages[currentLevel] != null)
            pictureImages[currentLevel].gameObject.SetActive(true);

        CheckPose();
        yield return StartCoroutine(CaptureAndSetPicture());

        SetControllersEnabled(level, false);
        TriggerAnimator(targetAnimator, level.exitTrigger);

        if (flashImage != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return flashImage.DOFade(0f, 2f).WaitForCompletion();
        }

        yield return StartCoroutine(ShowCompletionForCurrentLevel(level));

        if (betweenLevelDelay > 0f)
            yield return new WaitForSeconds(betweenLevelDelay);

        level.levelObj.SetActive(false);

        int nextLevel = currentLevel + 1;
        if (nextLevel >= activeLevels.Count)
        {
            // 所有关卡完成 → 显示结算画面 → 触发事件
            ShowResultsScreen();
            onAllLevelsComplete?.Invoke();
            yield break;
        }

        TriggerAnimator(completionTextAnimator, completionNextLevelTrigger);
        TriggerAnimator(levelTransitionAnimator, levelTransitionTrigger);

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
            completionText.text = "0%";
            completionText.transform.localScale = Vector3.one * completionScaleMin;
        }

        currentLevel = nextLevel;
        StartCoroutine(RunCurrentLevelFlow());
    }

    // ── 结算画面 ─────────────────────────────────────
    /// <summary>
    /// 激活结算面板，并将本局所有截图依次填入 resultsImages。
    /// 也可从外部调用：GameManager.Instance.ShowResultsScreen();
    /// </summary>
    public void ShowResultsScreen()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (resultsImages == null || resultsImages.Length == 0) return;

        for (int i = 0; i < resultsImages.Length; i++)
        {
            if (resultsImages[i] == null) continue;

            if (i < capturedSprites.Count && capturedSprites[i] != null)
            {
                resultsImages[i].sprite         = capturedSprites[i];
                resultsImages[i].preserveAspect = true;
                resultsImages[i].color          = Color.white;
                resultsImages[i].gameObject.SetActive(true);
            }
            else
            {
                // 本局关卡数不足时隐藏多余槽位
                resultsImages[i].gameObject.SetActive(false);
            }
        }
    }

    // ── 工具方法 ─────────────────────────────────────
    private void TriggerAnimator(Animator animator, string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }

    private IEnumerator ShowCompletionForCurrentLevel(Level level)
    {
        if (completionText == null || level == null) yield break;

        if (completionShowDelay > 0f)
            yield return new WaitForSeconds(completionShowDelay);

        if (!completionText.gameObject.activeSelf)
        {
            completionText.text = "0%";
            completionText.transform.localScale = Vector3.one * completionScaleMin;
            completionText.gameObject.SetActive(true);
        }

        float targetPercent  = Mathf.Round(level.similarity * 100f);
        float currentPercent = 0f;
        float duration       = Mathf.Max(0f, completionCountDuration);

        if (duration <= 0f)
        {
            completionText.text = Mathf.RoundToInt(targetPercent) + "%";
            float finalScale = Mathf.Lerp(completionScaleMin, completionScaleMax, level.similarity);
            completionText.transform.localScale = Vector3.one * finalScale;
        }
        else
        {
            yield return DOTween.To(
                () => currentPercent,
                x =>
                {
                    currentPercent = x;
                    completionText.text = Mathf.RoundToInt(currentPercent) + "%";
                    float t     = currentPercent / 100f;
                    float scale = Mathf.Lerp(completionScaleMin, completionScaleMax, t);
                    completionText.transform.localScale = Vector3.one * scale;
                },
                targetPercent,
                duration
            ).SetEase(Ease.OutCubic).WaitForCompletion();
        }

        if (completionTriggerDelay > 0f)
            yield return new WaitForSeconds(completionTriggerDelay);

        TriggerAnimator(completionTextAnimator, completionFinishTrigger);
    }

    private void SetControllersEnabled(Level level, bool enabledState)
    {
        if (level == null || level.poseEditorControllerList == null) return;

        foreach (var controller in level.poseEditorControllerList)
        {
            if (controller == null) continue;
            if (enabledState) controller.Enable();
            else controller.Disable();
        }
    }

    private void SetTimerValue(float value)
    {
        timer = Mathf.Clamp(value, 0f, time);

        if (timerImage != null && time > 0f)
            timerImage.sizeDelta = new Vector2(timer / time * timerImageInitialWidth, timerImage.sizeDelta.y);
    }

    private IEnumerator TimingCoroutine()
    {
        int min = (int)(time / 60f), sec = (int)(time % 60f);
        SetTimerValue(time);

        if (timerText != null)
            timerText.text = (min > 9 ? min.ToString() : "0" + min) + ":" +
                             (sec > 9 ? sec.ToString() : "0" + sec);

        while (min > 0 || sec > 0)
        {
            yield return new WaitForSeconds(1f);

            SetTimerValue(timer - 1f);

            if (sec > 0) sec--;
            else { sec = 59; min--; }

            if (timerText != null)
                timerText.text = (min > 9 ? min.ToString() : "0" + min) + ":" +
                                 (sec > 9 ? sec.ToString() : "0" + sec);
        }
    }

    private IEnumerator CaptureAndSetPicture()
    {
        Image pictureImage = currentLevel < pictureImages.Length ? pictureImages[currentLevel] : null;
        if (pictureImage == null) yield break;

        GameObject[] hideObjects = GameObject.FindGameObjectsWithTag("Hide");
        var canvasGroups = new List<(CanvasGroup cg, float originalAlpha)>();

        foreach (var go in hideObjects)
        {
            if (go == null || !go.activeSelf) continue;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            canvasGroups.Add((cg, cg.alpha));
            cg.alpha = 0f;
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        foreach (var (cg, originalAlpha) in canvasGroups)
            if (cg != null) cg.alpha = originalAlpha;

        if (tex == null) yield break;

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        capturedTextures.Add(tex);
        capturedSprites.Add(sprite);

        pictureImage.gameObject.SetActive(true);
        pictureImage.sprite         = sprite;
        pictureImage.preserveAspect = true;
        pictureImage.color          = Color.white;
    }

    private void CheckPose()
    {
        if (currentLevel >= activeLevels.Count) return;

        var level = activeLevels[currentLevel];
        var zone  = level.zone;

        if (zone == null || zone.vertices == null || zone.vertices.Count < 3) return;

        Canvas canvas            = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        float passed = 0f, total = 0f;

        foreach (var controller in level.poseEditorControllerList)
        {
            if (controller == null || controller.Joints == null) continue;

            foreach (var joint in controller.Joints)
            {
                if (joint == null || joint.rect == null) continue;
                total++;

                Vector2 anchoredPos = WorldToAnchored(joint.rect.position, canvasRect);
                if (zone.Contains(anchoredPos)) passed++;
            }
        }

        level.similarity = total > 0f ? passed / total : 0f;
        Debug.Log($"[GameManager] 完成度：{level.similarity * 100f:F2}% ({passed}/{total})");
    }

    private Vector2 WorldToAnchored(Vector3 worldPos, RectTransform canvasRect)
    {
        if (canvasRect == null) return Vector2.zero;
        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    // ── 生命周期 ─────────────────────────────────────
    private void OnDestroy()
    {
        foreach (var s in capturedSprites)
            if (s != null) Destroy(s);

        foreach (var t in capturedTextures)
            if (t != null) Destroy(t);

        capturedSprites.Clear();
        capturedTextures.Clear();
    }
}