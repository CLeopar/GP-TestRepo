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

    public static event System.Action OnLevelCompleted;

    [System.Serializable]
    public class Level
    {
        public GameObject levelObj;
        [HideInInspector] public List<PoseEditorController> poseEditorControllerList;
        [HideInInspector] public TMP_Text similarityText;
        [HideInInspector] public float similarity = 0f;
        public string promptString;

        public string      enterTrigger = "Enter";
        public string      exitTrigger  = "Exit";
        public PolygonZone zone         = new PolygonZone();

        [Tooltip("该关卡占总分的权重（所有关卡权重之和建议为 700）")]
        public float scoreWeight = 140f;

        [Tooltip("必须触碰区域列表：将场景中挂有 RequiredZone 组件的 GameObject 拖入")]
        public RequiredZone[] requiredZones;

        [Tooltip("组成身体的所有部位 RectTransform（用于必须触碰区域检测）")]
        public RectTransform[] bodyParts;

        [Header("关卡衔接配置")]
        [Tooltip("本关开场动画时长（秒）。新关打开后等这么久再关闭上一关")]
        public float openAnimationDuration = 1.5f;

        [Tooltip("开场动画期间是否禁用玩家控制器（等开场动画完成后再启用）")]
        public bool disableControllersDuringOpen = true;

        [Tooltip("开场动画播放完成后，额外等待多久才启用控制器并开始倒计时")]
        public float openAnimationExtraWait = 0.2f;

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
    [SerializeField] private TMP_Text      timerText;
    [SerializeField] private RectTransform timerImage;
    [SerializeField] private float[] levelTimes = { 60f, 50f, 40f, 30f, 20f };

    [Header("Gameplay BGM")]
    [SerializeField] private AudioClip   gameplayBgmClip;
    [SerializeField] private AudioSource gameplayBgmSource;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolumeGameplay = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolumeCompletion = 0.3f;
    [SerializeField] private float bgmVolumeFadeDuration = 0.5f;

    [Header("Countdown Warning")]
    [SerializeField] private int         countdownWarnSeconds = 3;
    [SerializeField] private AudioClip   countdownClip;
    [SerializeField] private AudioClip   countdownFinishClip;
    [Range(0f, 1f)]
    [SerializeField] private float       countdownVolume = 1f;
    [SerializeField] private AudioSource countdownAudioSource;
    [SerializeField] private float       countdownPunchScale    = 0.4f;
    [SerializeField] private float       countdownPunchDuration = 0.35f;
    [SerializeField] private float       ghostScaleMultiplier   = 2.5f;
    [SerializeField] private float       ghostDuration          = 0.5f;

    [Header("Flow Time")]
    [SerializeField] private float enterWaitTime     = 1.0f;
    [SerializeField] private float betweenLevelDelay = 2.0f;

    [Header("Capture")]
    [SerializeField] private Image flashImage;

    [Header("Animator")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Animator levelTransitionAnimator;
    [SerializeField] private string   levelTransitionTrigger = "Enter";

    [Header("Level")]
    [SerializeField] private List<Level> levelList;
    [SerializeField] private int         levelCount = 5;

    [Header("Prompt")]
    [SerializeField] private TMP_Text[] promptTexts;

    [Header("Picture")]
    [SerializeField] private Image[] pictureImages;

    [Header("Completion UI")]
    [SerializeField] private float completionMinDisplayTime = 8f;

    [Header("Results Screen")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Image[]    resultsImages;

    [Header("Game Over")]
    [SerializeField] private UnityEvent onAllLevelsComplete;

    [Header("Tutorial")]
    [SerializeField] private bool        enableTutorial = false;
    [SerializeField] private Level       fixedFirstLevel;
    [SerializeField] private GameObject  tutorialPanel;
    [SerializeField] private Image[]     tutorialImages;

    // ───────────────────────── Runtime ─────────────────────────
    private float       timer;
    private float       timerImageInitialWidth;
    private float       currentLevelTime;
    private int         currentLevel;
    private List<Level> activeLevels = new List<Level>();

    private readonly List<Texture2D> capturedTextures = new List<Texture2D>();
    private readonly List<Sprite>    capturedSprites  = new List<Sprite>();

    private Tween _bgmFadeTween;
    private Level _pendingCloseLevel;

    public IReadOnlyList<Texture2D> CapturedTextures => capturedTextures;
    public IReadOnlyList<Sprite>    CapturedSprites  => capturedSprites;
    public List<Level>              LevelList        => levelList;
    public int                      CurrentLevel     => currentLevel;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (timerImage != null)
            timerImageInitialWidth = timerImage.sizeDelta.x;

        if (countdownAudioSource == null)
        {
            countdownAudioSource = gameObject.AddComponent<AudioSource>();
            countdownAudioSource.playOnAwake  = false;
            countdownAudioSource.spatialBlend = 0f;
        }

        if (gameplayBgmSource == null)
        {
            gameplayBgmSource = gameObject.AddComponent<AudioSource>();
            gameplayBgmSource.playOnAwake  = false;
            gameplayBgmSource.spatialBlend = 0f;
            gameplayBgmSource.loop         = true;
        }

        foreach (var level in levelList)
        {
            if (level == null || level.levelObj == null) continue;
            level.Init();
            level.levelObj.SetActive(false);
        }

        if (enableTutorial && fixedFirstLevel != null && fixedFirstLevel.levelObj != null)
        {
            if (!levelList.Contains(fixedFirstLevel))
            {
                fixedFirstLevel.Init();
                fixedFirstLevel.levelObj.SetActive(false);
            }

            var pool = levelList
                .Where(l => l != null && l.levelObj != null && l != fixedFirstLevel)
                .OrderBy(_ => Random.value)
                .Take(levelCount - 1)
                .ToList();

            activeLevels = new List<Level> { fixedFirstLevel };
            activeLevels.AddRange(pool);
        }
        else
        {
            activeLevels = levelList
                .Where(l => l != null && l.levelObj != null)
                .OrderBy(_ => Random.value)
                .Take(Mathf.Min(levelCount, levelList.Count))
                .ToList();
        }

        currentLevel       = 0;
        _pendingCloseLevel = null;
        FillAllPromptTexts();
    }

    private float GetLevelTime(int positionIndex)
    {
        if (levelTimes == null || levelTimes.Length == 0) return 30f;
        int idx = Mathf.Clamp(positionIndex, 0, levelTimes.Length - 1);
        return Mathf.Max(1f, levelTimes[idx]);
    }

    private void FillAllPromptTexts()
    {
        if (promptTexts == null) return;
        for (int i = 0; i < promptTexts.Length; i++)
        {
            if (promptTexts[i] == null) continue;
            promptTexts[i].text = i < activeLevels.Count ? activeLevels[i].promptString : "";
        }
    }

    public void ShowPromptForCurrentLevel() { }

    public void StartGame() => StartCoroutine(RunCurrentLevelFlow());

    // ── BGM ──────────────────────────────────────────────────

    private void StartGameplayBgm()
    {
        if (gameplayBgmSource == null || gameplayBgmClip == null) return;
        gameplayBgmSource.clip   = gameplayBgmClip;
        gameplayBgmSource.loop   = true;
        gameplayBgmSource.volume = bgmVolumeGameplay;
        gameplayBgmSource.Play();
    }

    private void FadeBgmVolume(float targetVolume)
    {
        if (gameplayBgmSource == null) return;
        _bgmFadeTween?.Kill();
        _bgmFadeTween = gameplayBgmSource
            .DOFade(targetVolume, bgmVolumeFadeDuration)
            .SetEase(Ease.InOutSine);
    }

    // ── 主流程 ────────────────────────────────────────────────

    private IEnumerator RunCurrentLevelFlow()
    {
        Level level = activeLevels[currentLevel];

        // ══════════════════════════════════════════════════════
        // 1. 立刻打开新关卡并置顶（上一关此时仍然显示着）
        // ══════════════════════════════════════════════════════
        level.levelObj.SetActive(true);
        level.levelObj.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();

        currentLevelTime = GetLevelTime(currentLevel);

        SlideTimerToFull();
        
        if (currentLevel == 0)
            StartGameplayBgm();

        FadeBgmVolume(bgmVolumeGameplay);

        // ══════════════════════════════════════════════════════
        // 2. 触发开场动画，开场期间禁用控制器
        // ══════════════════════════════════════════════════════
        if (level.disableControllersDuringOpen)
            SetControllersEnabled(level, false);
        else
            SetControllersEnabled(level, true);

        TriggerAnimator(targetAnimator, level.enterTrigger);

        if (enterWaitTime > 0f)
            yield return new WaitForSeconds(enterWaitTime);

        if (currentLevel == 0 && enableTutorial)
            yield return StartCoroutine(ShowTutorialPanel());

        // ══════════════════════════════════════════════════════
        // 3. 等开场动画播完，然后关闭上一关
        // ══════════════════════════════════════════════════════
        if (level.disableControllersDuringOpen)
        {
            float openWaitTime = Mathf.Max(0f, level.openAnimationDuration - enterWaitTime);
            if (openWaitTime > 0f)
                yield return new WaitForSeconds(openWaitTime);
        }

        if (_pendingCloseLevel != null && _pendingCloseLevel.levelObj != null)
        {
            _pendingCloseLevel.levelObj.SetActive(false);
            _pendingCloseLevel = null;
        }

        // ══════════════════════════════════════════════════════
        // 4. 启用控制器，开始倒计时
        // ══════════════════════════════════════════════════════
        if (level.disableControllersDuringOpen)
        {
            if (level.openAnimationExtraWait > 0f)
                yield return new WaitForSeconds(level.openAnimationExtraWait);

            SetControllersEnabled(level, true);
        }

        // ══════════════════════════════════════════════════════
        // 5. 正式开始倒计时
        // ══════════════════════════════════════════════════════
        yield return StartCoroutine(TimingCoroutine());

        // ── 关卡结束流程 ───────────────────────────────────────
        if (flashImage != null)
            yield return flashImage.DOFade(1f, 0.05f).WaitForCompletion();

        if (currentLevel < pictureImages.Length && pictureImages[currentLevel] != null)
            pictureImages[currentLevel].gameObject.SetActive(true);

        CheckPose();
        OnLevelCompleted?.Invoke();
        yield return StartCoroutine(CaptureAndSetPicture());

        SetControllersEnabled(level, false);
        TriggerAnimator(targetAnimator, level.exitTrigger);

        if (flashImage != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return flashImage.DOFade(0f, 2f).WaitForCompletion();
        }

        FadeBgmVolume(bgmVolumeCompletion);

        float completionStartTime = Time.time;
        yield return StartCoroutine(
            ScoreManager.Instance.ShowCompletionAndAddScore(level.similarity, level.scoreWeight)
        );

        float elapsed   = Time.time - completionStartTime;
        float remaining = completionMinDisplayTime - elapsed;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (betweenLevelDelay > 0f)
            yield return new WaitForSeconds(betweenLevelDelay);

        // ══════════════════════════════════════════════════════
        // 6. 切换下一关
        // ══════════════════════════════════════════════════════
        int nextLevel = currentLevel + 1;
        if (nextLevel >= activeLevels.Count)
        {
            // ★ 最后一关：不关闭关卡画面，只禁用控制器，画面留在原地
            SetControllersEnabled(level, false);
            GameStatsManager.Instance.SaveSession();
            FadeBgmVolume(0f);
            ScoreManager.Instance.PlayGradeSoundForScore(level.similarity);
            ShowResultsScreen();
            onAllLevelsComplete?.Invoke();
            yield break;
        }

        TriggerAnimator(levelTransitionAnimator, levelTransitionTrigger);
        ScoreManager.Instance.ResetCompletionText();

        _pendingCloseLevel = level;

        currentLevel = nextLevel;
        StartCoroutine(RunCurrentLevelFlow());
    }

    // ── 教程弹窗 ───────────────────────────────────────────────

    private void SlideTimerToFull()
    {
        if (timerImage == null || currentLevelTime <= 0f) return;

        float currentWidth = timerImage.sizeDelta.x;
        float targetWidth  = timerImageInitialWidth;
        if (Mathf.Approximately(currentWidth, targetWidth)) return;

        float fillRatio    = 1f - Mathf.Clamp01(currentWidth / timerImageInitialWidth);
        float slideDuration = Mathf.Lerp(0.05f, 0.5f, fillRatio);

        timerImage.DOKill(false);
        timerImage
            .DOSizeDelta(new Vector2(targetWidth, timerImage.sizeDelta.y), slideDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => timer = currentLevelTime);
    }
    
    private IEnumerator ShowTutorialPanel()
    {
        if (tutorialPanel == null || tutorialImages == null || tutorialImages.Length == 0)
            yield break;

        foreach (var img in tutorialImages)
            if (img != null) img.gameObject.SetActive(false);

        tutorialPanel.SetActive(true);

        for (int i = 0; i < tutorialImages.Length; i++)
        {
            if (tutorialImages[i] != null)
                tutorialImages[i].gameObject.SetActive(true);

            yield return StartCoroutine(WaitForAnyInput());

            if (tutorialImages[i] != null)
                tutorialImages[i].gameObject.SetActive(false);
        }

        tutorialPanel.SetActive(false);
    }

    private IEnumerator WaitForAnyInput()
    {
        yield return new WaitForEndOfFrame();
        while (!Input.anyKeyDown
               && !Input.GetMouseButtonDown(0)
               && !Input.GetMouseButtonDown(1)
               && !Input.GetMouseButtonDown(2))
        {
            yield return null;
        }
    }

    // ── 结果画面 ───────────────────────────────────────────────

    public void ShowResultsScreen()
    {
        if (resultsPanel != null) resultsPanel.SetActive(true);
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
                resultsImages[i].gameObject.SetActive(false);
            }
        }
    }

    // ── 工具方法 ───────────────────────────────────────────────

    private void TriggerAnimator(Animator anim, string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger)) return;
        anim.SetTrigger(trigger);
    }

    private IEnumerator TimingCoroutine()
    {
        int min = (int)(currentLevelTime / 60f);
        int sec = (int)(currentLevelTime % 60f);

        // 确保动画已结束、timer 已同步
        timerImage?.DOKill(false);
        SetTimerValue(currentLevelTime);
        if (timerText != null) timerText.text = FormatTime(min, sec);

        while (min > 0 || sec > 0)
        {
            yield return new WaitForSeconds(1f);

            SetTimerValue(timer - 1f);
            if (sec > 0) sec--;
            else { sec = 59; min--; }

            if (timerText != null) timerText.text = FormatTime(min, sec);

            int totalSec = min * 60 + sec;
            if (totalSec > 0 && totalSec <= countdownWarnSeconds) TriggerCountdownWarning(false);
            if (totalSec == 0) TriggerCountdownWarning(true);
        }
    }

    private string FormatTime(int min, int sec) =>
        (min > 9 ? min.ToString() : "0" + min) + ":" +
        (sec > 9 ? sec.ToString() : "0" + sec);

    private void TriggerCountdownWarning(bool isFinish)
    {
        AudioClip clip = isFinish ? countdownFinishClip : countdownClip;
        if (clip != null) countdownAudioSource.PlayOneShot(clip, countdownVolume);

        if (timerText != null)
        {
            timerText.transform.DOKill(false);
            timerText.transform.DOPunchScale(
                Vector3.one * countdownPunchScale, countdownPunchDuration, 6, 0.5f);
            StartCoroutine(SpawnGhost(timerText));
        }
    }

    private IEnumerator SpawnGhost(TMP_Text source)
    {
        GameObject ghostObj = new GameObject("CountdownGhost");
        ghostObj.transform.SetParent(source.transform.parent, false);
        ghostObj.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        RectTransform ghostRect = ghostObj.AddComponent<RectTransform>();
        RectTransform srcRect   = source.GetComponent<RectTransform>();
        ghostRect.anchorMin        = srcRect.anchorMin;
        ghostRect.anchorMax        = srcRect.anchorMax;
        ghostRect.anchoredPosition = srcRect.anchoredPosition;
        ghostRect.sizeDelta        = srcRect.sizeDelta;
        ghostRect.pivot            = srcRect.pivot;
        ghostRect.localScale       = source.transform.localScale;
        ghostRect.localRotation    = source.transform.localRotation;

        TMP_Text ghost      = ghostObj.AddComponent<TextMeshProUGUI>();
        ghost.text          = source.text;
        ghost.font          = source.font;
        ghost.fontSize      = source.fontSize;
        ghost.fontStyle     = source.fontStyle;
        ghost.alignment     = source.alignment;
        ghost.color         = new Color(source.color.r, source.color.g, source.color.b, 1f);
        ghost.raycastTarget = false;

        Tween scaleTween = ghostRect
            .DOScale(source.transform.localScale * ghostScaleMultiplier, ghostDuration)
            .SetEase(Ease.OutCubic);
        ghost.DOFade(0f, ghostDuration).SetEase(Ease.InCubic);

        yield return scaleTween.WaitForCompletion();
        Destroy(ghostObj);
    }

    private void SetControllersEnabled(Level level, bool enabledState)
    {
        if (level == null || level.poseEditorControllerList == null) return;
        foreach (var controller in level.poseEditorControllerList)
        {
            if (controller == null) continue;
            if (enabledState) controller.Enable();
            else              controller.Disable();
        }
    }

    private void SetTimerValue(float value)
    {
        timer = Mathf.Clamp(value, 0f, currentLevelTime);
        if (timerImage != null && currentLevelTime > 0f)
            timerImage.sizeDelta = new Vector2(
                timer / currentLevelTime * timerImageInitialWidth,
                timerImage.sizeDelta.y);
    }

    private IEnumerator CaptureAndSetPicture()
    {
        Image pictureImage = currentLevel < pictureImages.Length ? pictureImages[currentLevel] : null;
        if (pictureImage == null) yield break;

        GameObject[] hideObjects = GameObject.FindGameObjectsWithTag("Hide");
        var canvasGroups  = new List<CanvasGroup>();
        var originalAlphas = new List<float>();

        foreach (var go in hideObjects)
        {
            if (go == null || !go.activeSelf) continue;
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);
            originalAlphas.Add(cg.alpha);
            cg.alpha = 0f;
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        for (int i = 0; i < canvasGroups.Count; i++)
            if (canvasGroups[i] != null) canvasGroups[i].alpha = originalAlphas[i];

        if (tex == null) yield break;

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

        capturedTextures.Add(tex);
        capturedSprites.Add(sprite);

        pictureImage.gameObject.SetActive(true);
        pictureImage.sprite         = sprite;
        pictureImage.preserveAspect = true;
        pictureImage.color          = Color.white;
    }

    // ── 姿势检测 ──────────────────────────────────────────────

    private void CheckPose()
    {
        if (currentLevel >= activeLevels.Count) return;

        var level = activeLevels[currentLevel];
        Canvas canvas            = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        float baseSimilarity = 0f;
        var zone = level.zone;

        if (zone != null && zone.vertices != null && zone.vertices.Count >= 3)
        {
            float passed = 0f, total = 0f;

            foreach (var controller in level.poseEditorControllerList)
            {
                if (controller == null || controller.Joints == null) continue;
                foreach (var joint in controller.Joints)
                {
                    if (joint == null || joint.rect == null) continue;
                    total++;
                    if (zone.Contains(WorldToAnchored(joint.rect.position, canvasRect))) passed++;
                }
            }

            baseSimilarity = total > 0f ? passed / total : 0f;
        }

        float totalPenalty = 0f;

        if (level.requiredZones != null && level.requiredZones.Length > 0
            && level.bodyParts   != null && level.bodyParts.Length   > 0)
        {
            foreach (var required in level.requiredZones)
            {
                if (required == null) continue;
                if (required.zone == null || required.zone.vertices == null
                    || required.zone.vertices.Count < 3) continue;

                bool touched = false;

                foreach (var bodyPart in level.bodyParts)
                {
                    if (bodyPart == null) continue;
                    if (BodyPartOverlapsZone(bodyPart, required.zone, canvasRect))
                    {
                        touched = true;
                        break;
                    }
                }

                if (!touched)
                {
                    totalPenalty += required.penaltyScore;
                    Debug.Log($"[GameManager] 必须触碰区域 [{required.gameObject.name}] 未命中，扣 {required.penaltyScore} 分");
                }
            }
        }

        float penaltyNormalized = totalPenalty / 100f;
        level.similarity = Mathf.Clamp01(baseSimilarity - penaltyNormalized);

        Debug.Log($"[GameManager] 基础：{baseSimilarity * 100f:F1}%  " +
                  $"扣分：{totalPenalty:F1}  " +
                  $"最终：{level.similarity * 100f:F1}%");
    }

    private bool BodyPartOverlapsZone(RectTransform bodyPart, PolygonZone zone, RectTransform canvasRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        bodyPart.GetWorldCorners(worldCorners);

        Vector3 worldCenter = (worldCorners[0] + worldCorners[2]) * 0.5f;
        if (zone.Contains(WorldToAnchored(worldCenter, canvasRect))) return true;

        const float inset = 0.3f;
        for (int i = 0; i < 4; i++)
        {
            Vector3 insetPoint = Vector3.Lerp(worldCorners[i], worldCenter, inset);
            if (zone.Contains(WorldToAnchored(insetPoint, canvasRect))) return true;
        }

        return false;
    }

    private Vector2 WorldToAnchored(Vector3 worldPos, RectTransform canvasRect)
    {
        if (canvasRect == null) return Vector2.zero;
        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    private void OnDestroy()
    {
        _bgmFadeTween?.Kill();
        foreach (var s in capturedSprites)  if (s != null) Destroy(s);
        foreach (var t in capturedTextures) if (t != null) Destroy(t);
        capturedSprites.Clear();
        capturedTextures.Clear();
    }
}