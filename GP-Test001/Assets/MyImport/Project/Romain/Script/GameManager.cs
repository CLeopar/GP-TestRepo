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

    // ───────────────────────── Level ─────────────────────────
    [System.Serializable]
    public class Level
    {
        public GameObject levelObj;
        [HideInInspector] public List<PoseEditorController> poseEditorControllerList;
        [HideInInspector] public TMP_Text similarityText;
        [HideInInspector] public float similarity = 0f;
        public string promptString;

        public string enterTrigger = "Enter";
        public string exitTrigger = "Exit";
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

    // ───────────────────── Milestone Sound Config ─────────────────────
    [System.Serializable]
    public class MilestoneSound
    {
        [Tooltip("触发的百分比（整数 0~100，例如 50 表示 50%）")]
        public int percent;
        [Tooltip("该里程碑要播放的音效（可为空，为空则不播放）")]
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    // ───────────────────────── Inspector ─────────────────────────
    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerImage;
    [Tooltip("每关的倒计时时长（秒），按出场顺序填写。index 0 = 第1关，以此类推。数组不足时使用最后一个值。")]
    [SerializeField] private float[] levelTimes = { 60f, 50f, 40f, 30f, 20f };

    [Header("Gameplay BGM")]
    [Tooltip("第一关开始后全程循环播放的背景音乐")]
    [SerializeField] private AudioClip gameplayBgmClip;
    [SerializeField] private AudioSource gameplayBgmSource;
    [Tooltip("游玩阶段（倒计时进行中）的音量")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolumeGameplay = 1f;
    [Tooltip("分数结算阶段的音量")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolumeCompletion = 0.3f;
    [Tooltip("音量切换的淡入淡出时间（秒）")]
    [SerializeField] private float bgmVolumeFadeDuration = 0.5f;

    [Header("Countdown Warning")]
    [Tooltip("倒计时警告从剩余第几秒开始")]
    [SerializeField] private int countdownWarnSeconds = 3;
    [Tooltip("警告期间每秒播放的音效")]
    [SerializeField] private AudioClip countdownClip;
    [Tooltip("归零时播放的专属音效")]
    [SerializeField] private AudioClip countdownFinishClip;
    [Range(0f, 1f)]
    [SerializeField] private float countdownVolume = 1f;
    [SerializeField] private AudioSource countdownAudioSource;
    [SerializeField] private float countdownPunchScale = 0.4f;
    [SerializeField] private float countdownPunchDuration = 0.35f;
    [SerializeField] private float ghostScaleMultiplier = 2.5f;
    [SerializeField] private float ghostDuration = 0.5f;

    [Header("Flow Time")]
    [SerializeField] private float enterWaitTime = 1.0f;
    [SerializeField] private float betweenLevelDelay = 2.0f;

    [Header("Capture")]
    [SerializeField] private Image flashImage;

    [Header("Animator")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Animator levelTransitionAnimator;
    [SerializeField] private string levelTransitionTrigger = "Enter";

    [Header("Level")]
    [SerializeField] private List<Level> levelList;
    [SerializeField] private int levelCount = 5;

    [Header("Prompt")]
    [Tooltip("每个元素对应一关，promptTexts[0] 显示第一关，以此类推")]
    [SerializeField] private TMP_Text[] promptTexts;

    [Header("Picture")]
    [SerializeField] private Image[] pictureImages;

    [Header("Completion UI")]
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private Animator completionTextAnimator;
    [SerializeField] private string completionNextLevelTrigger = "Next";
    [SerializeField] private float completionShowDelay = 0.5f;
    [SerializeField] private float completionCountDuration = 0.8f;
    [Tooltip("百分比动画最短播放时长")]
    [SerializeField] private float completionCountDurationMin = 0.3f;
    [SerializeField] private float completionTriggerDelay = 1.0f;
    [SerializeField] private string completionFinishTrigger = "Finish";
    [SerializeField] private float completionScaleMin = 0.8f;
    [SerializeField] private float completionScaleMax = 2.0f;
    [Tooltip("结算页面最短停留时间（秒）")]
    [SerializeField] private float completionMinDisplayTime = 8f;

    [Header("Milestone Punch")]
    public int milestoneInterval = 10;
    [SerializeField] private float punchDuration = 0.22f;
    [SerializeField] private float punchDurationAt100 = 0.55f;
    [SerializeField] private float punchRotationAngle = 9f;
    [SerializeField] private float punchScaleAmount = 0.28f;

    [Header("Score Sound")]
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private float tickPitchMin = 0.8f;
    [SerializeField] private float tickPitchMax = 2.0f;
    [SerializeField] private AudioSource tickAudioSource;

    [Header("Milestone Sounds (Per Percent)")]
    [SerializeField] private MilestoneSound[] milestoneSounds;
    [SerializeField] private AudioSource milestoneAudioSource;

    [Header("Score Grades")]
    public ScoreGrade[] scoreGrades;

    [System.Serializable]
    public class ScoreGrade
    {
        public int threshold;
        public GameObject[] objects;
    }

    [Header("Results Screen")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Image[] resultsImages;

    [Header("Game Over")]
    [SerializeField] private UnityEvent onAllLevelsComplete;

    [Header("Tutorial")]
    [Tooltip("开启后：第一关使用固定关卡，其余随机抽选；关闭则全部随机（原逻辑）")]
    [SerializeField] private bool enableTutorial = false;
    [Tooltip("固定的第一关 Level（enableTutorial 开启时使用）")]
    [SerializeField] private Level fixedFirstLevel;
    [Tooltip("教程弹窗的根 GameObject（显示/隐藏整体）")]
    [SerializeField] private GameObject tutorialPanel;
    [Tooltip("教程纸条的图片列表，按顺序填入，支持任意数量；每次按键/点击翻到下一张")]
    [SerializeField] private Image[] tutorialImages;

    // ───────────────────────── Runtime ─────────────────────────
    private float timer;
    private float timerImageInitialWidth;
    private float currentLevelTime;
    private int currentLevel;
    private List<Level> activeLevels = new();

    private readonly List<Texture2D> capturedTextures = new();
    private readonly List<Sprite> capturedSprites = new();

    private float _scoreCurrentPercent;
    private bool _scoreAnimRunning;

    // BGM 淡入淡出 tween 缓存，防止重叠
    private Tween _bgmFadeTween;

    public IReadOnlyList<Texture2D> CapturedTextures => capturedTextures;
    public IReadOnlyList<Sprite> CapturedSprites => capturedSprites;
    public List<Level> LevelList => levelList;
    public int CurrentLevel => currentLevel;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (timerImage != null)
            timerImageInitialWidth = timerImage.sizeDelta.x;

        if (tickAudioSource == null)
        {
            tickAudioSource = gameObject.AddComponent<AudioSource>();
            tickAudioSource.playOnAwake = false;
            tickAudioSource.spatialBlend = 0f;
        }

        if (milestoneAudioSource == null)
        {
            milestoneAudioSource = gameObject.AddComponent<AudioSource>();
            milestoneAudioSource.playOnAwake = false;
            milestoneAudioSource.spatialBlend = 0f;
        }

        if (countdownAudioSource == null)
        {
            countdownAudioSource = gameObject.AddComponent<AudioSource>();
            countdownAudioSource.playOnAwake = false;
            countdownAudioSource.spatialBlend = 0f;
        }

        if (gameplayBgmSource == null)
        {
            gameplayBgmSource = gameObject.AddComponent<AudioSource>();
            gameplayBgmSource.playOnAwake = false;
            gameplayBgmSource.spatialBlend = 0f;
            gameplayBgmSource.loop = true;
        }

        foreach (var level in levelList)
        {
            if (level == null || level.levelObj == null) continue;
            level.Init();
            level.levelObj.SetActive(false);
        }

        // ── 构建 activeLevels ──────────────────────────────────
        if (enableTutorial && fixedFirstLevel != null && fixedFirstLevel.levelObj != null)
        {
            // 固定第一关，其余从 levelList 中排除固定关卡后随机抽选
            var pool = levelList
                .Where(l => l != null && l.levelObj != null && l != fixedFirstLevel)
                .OrderBy(_ => Random.value)
                .Take(Mathf.Max(0, Mathf.Min(levelCount - 1, levelList.Count - 1)))
                .ToList();

            activeLevels = new List<Level> { fixedFirstLevel };
            activeLevels.AddRange(pool);
        }
        else
        {
            // 原逻辑：全部随机
            activeLevels = levelList
                .Where(l => l != null && l.levelObj != null)
                .OrderBy(_ => Random.value)
                .Take(Mathf.Min(levelCount, levelList.Count))
                .ToList();
        }

        currentLevel = 0;
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

    // ── BGM 控制 ──────────────────────────────────────────────

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
        level.levelObj.SetActive(true);

        currentLevelTime = GetLevelTime(currentLevel);

        if (currentLevel == 0)
            StartGameplayBgm();

        FadeBgmVolume(bgmVolumeGameplay);

        SetControllersEnabled(level, false);
        TriggerAnimator(targetAnimator, level.enterTrigger);

        if (enterWaitTime > 0f)
            yield return new WaitForSeconds(enterWaitTime);

        // ── 第一关 + 教程开启：先显示教程弹窗，结束后再开始倒计时 ──
        if (currentLevel == 0 && enableTutorial)
            yield return StartCoroutine(ShowTutorialPanel());

        SetControllersEnabled(level, true);
        yield return StartCoroutine(TimingCoroutine());

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

        FadeBgmVolume(bgmVolumeCompletion);

        float completionStartTime = Time.time;
        yield return StartCoroutine(ShowCompletionForCurrentLevel(level));

        float elapsed   = Time.time - completionStartTime;
        float remaining = completionMinDisplayTime - elapsed;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (betweenLevelDelay > 0f)
            yield return new WaitForSeconds(betweenLevelDelay);

        level.levelObj.SetActive(false);

        int nextLevel = currentLevel + 1;
        if (nextLevel >= activeLevels.Count)
        {
            FadeBgmVolume(0f);
            ShowResultsScreen();
            onAllLevelsComplete?.Invoke();
            yield break;
        }

        currentLevel = nextLevel;
        TriggerAnimator(completionTextAnimator, completionNextLevelTrigger);
        TriggerAnimator(levelTransitionAnimator, levelTransitionTrigger);

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
            completionText.text = "0%";
            completionText.transform.localScale = Vector3.one * completionScaleMin;
            completionText.transform.localRotation = Quaternion.identity;
        }

        StartCoroutine(RunCurrentLevelFlow());
    }

    // ── 教程弹窗 ───────────────────────────────────────────────

    /// <summary>
    /// 逐张显示 tutorialImages，每次按任意键/鼠标键翻到下一张，全部看完后隐藏弹窗。
    /// </summary>
    private IEnumerator ShowTutorialPanel()
    {
        if (tutorialPanel == null || tutorialImages == null || tutorialImages.Length == 0)
            yield break;

        // 先隐藏所有图片，再显示弹窗根节点
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

    /// <summary>
    /// 等待玩家按下任意键盘键或鼠标键（左/中/右键）。
    /// 在帧末尾采样，避免触发本协程的同一帧输入被立即消费。
    /// </summary>
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

    private void TriggerAnimator(Animator animator, string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }

    private IEnumerator TimingCoroutine()
    {
        int min = (int)(currentLevelTime / 60f);
        int sec = (int)(currentLevelTime % 60f);

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

        Tween scaleTween = ghostRect.DOScale(source.transform.localScale * ghostScaleMultiplier, ghostDuration).SetEase(Ease.OutCubic);
                           ghost.DOFade(0f, ghostDuration).SetEase(Ease.InCubic);

        yield return scaleTween.WaitForCompletion();
        Destroy(ghostObj);
    }

    private IEnumerator ShowCompletionForCurrentLevel(Level level)
    {
        if (completionText == null || level == null) yield break;

        if (completionShowDelay > 0f) yield return new WaitForSeconds(completionShowDelay);

        if (!completionText.gameObject.activeSelf)
        {
            completionText.text = "0%";
            completionText.transform.localScale = Vector3.one * completionScaleMin;
            completionText.transform.localRotation = Quaternion.identity;
            completionText.gameObject.SetActive(true);
        }

        float targetPercent = Mathf.Round(level.similarity * 100f);
        float duration = Mathf.Max(completionCountDurationMin,
            completionCountDuration * (targetPercent / 100f));

        if (duration <= 0f)
        {
            completionText.text = Mathf.RoundToInt(targetPercent) + "%";
            completionText.transform.localScale = Vector3.one *
                Mathf.Lerp(completionScaleMin, completionScaleMax, level.similarity);
        }
        else
        {
            _scoreCurrentPercent = 0f;
            _scoreAnimRunning    = true;

            StartCoroutine(ScoreSoundCoroutine(targetPercent));

            yield return DOTween.To(
                () => _scoreCurrentPercent,
                x =>
                {
                    _scoreCurrentPercent = x;
                    completionText.text = Mathf.RoundToInt(x) + "%";
                    completionText.transform.localScale = Vector3.one *
                        Mathf.Lerp(completionScaleMin, completionScaleMax, x / 100f);
                },
                targetPercent, duration
            ).SetEase(Ease.OutCubic).WaitForCompletion();

            _scoreAnimRunning = false;
            yield return null;
        }

        ActivateScoreGrade(level.similarity);

        if (completionTriggerDelay > 0f) yield return new WaitForSeconds(completionTriggerDelay);

        TriggerAnimator(completionTextAnimator, completionFinishTrigger);
    }

    private IEnumerator ScoreSoundCoroutine(float targetPercent)
    {
        int lastTickDisplayed = -1, lastMilestone = 0;
        int interval  = Mathf.Max(1, milestoneInterval);
        int targetInt = Mathf.RoundToInt(targetPercent);

        while (_scoreAnimRunning || lastTickDisplayed < targetInt)
        {
            int displayed = Mathf.RoundToInt(_scoreCurrentPercent);

            if (displayed != lastTickDisplayed && displayed > 0)
            {
                if (tickSound != null)
                {
                    tickAudioSource.pitch = Mathf.Lerp(tickPitchMin, tickPitchMax, displayed / 100f);
                    tickAudioSource.PlayOneShot(tickSound);
                }
                lastTickDisplayed = displayed;
            }

            if (displayed > lastMilestone)
            {
                for (int i = lastMilestone + 1; i <= displayed; i++)
                {
                    if (i % interval == 0)
                    {
                        lastMilestone = i;

                        if (completionText != null)
                            PunchCompletionText(completionText.transform,
                                Mathf.Lerp(completionScaleMin, completionScaleMax, i / 100f), i == 100);

                        if (milestoneSounds != null)
                            foreach (var ms in milestoneSounds)
                                if (ms != null && ms.percent == i && ms.clip != null)
                                {
                                    milestoneAudioSource.PlayOneShot(ms.clip, ms.volume);
                                    break;
                                }

                        break;
                    }
                    lastMilestone = i;
                }
            }

            yield return null;
        }
    }

    private void ActivateScoreGrade(float similarity)
    {
        if (scoreGrades == null || scoreGrades.Length == 0) return;
        foreach (var grade in scoreGrades)
        {
            if (similarity * 100f >= grade.threshold)
            {
                if (grade.objects != null)
                    foreach (var obj in grade.objects)
                        if (obj != null) obj.SetActive(true);
                return;
            }
        }
    }

    private void PunchCompletionText(Transform tf, float baseScale, bool isMax = false)
    {
        tf.DOKill(false);
        tf.localScale    = Vector3.one * baseScale;
        tf.localRotation = Quaternion.identity;

        float dur = isMax ? punchDurationAt100 : punchDuration;
        tf.DOPunchRotation(new Vector3(0f, 0f, punchRotationAngle), dur, isMax ? 12 : 7, 0.4f);
        tf.DOPunchScale(Vector3.one * (isMax ? punchScaleAmount * 1.5f : punchScaleAmount),
                dur, isMax ? 10 : 6, 0.5f)
           .OnComplete(() => { if (tf != null) tf.localScale = Vector3.one * baseScale; });
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

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

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
                if (zone.Contains(WorldToAnchored(joint.rect.position, canvasRect))) passed++;
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

    private void OnDestroy()
    {
        _bgmFadeTween?.Kill();
        _scoreAnimRunning = false;
        foreach (var s in capturedSprites) if (s != null) Destroy(s);
        foreach (var t in capturedTextures) if (t != null) Destroy(t);
        capturedSprites.Clear();
        capturedTextures.Clear();
    }
}
