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

        public string      enterTrigger = "Enter";
        public string      exitTrigger  = "Exit";
        public PolygonZone zone         = new PolygonZone();

        [Tooltip("该关卡占总分的权重（所有关卡权重之和建议为 700）")]
        public float scoreWeight = 140f;

        [Tooltip("必须触碰区域列表：将场景中挂有 RequiredZone 组件的 GameObject 拖入")]
        public RequiredZone[] requiredZones;

        [Tooltip("组成身体的所有部位 RectTransform（用于必须触碰区域检测）")]
        public RectTransform[] bodyParts;

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
    [SerializeField] private float countdownPunchScale    = 0.4f;
    [SerializeField] private float countdownPunchDuration = 0.35f;
    [SerializeField] private float ghostScaleMultiplier   = 2.5f;
    [SerializeField] private float ghostDuration          = 0.5f;

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
    [SerializeField] private int levelCount = 5;

    [Header("Prompt")]
    [Tooltip("每个元素对应一关，promptTexts[0] 显示第一关，以此类推")]
    [SerializeField] private TMP_Text[] promptTexts;

    [Header("Picture")]
    [SerializeField] private Image[] pictureImages;

    [Header("Completion UI")]
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private Animator completionTextAnimator;
    [SerializeField] private string   completionNextLevelTrigger = "Next";
    [SerializeField] private float    completionShowDelay        = 0.5f;
    [SerializeField] private float    completionCountDuration    = 0.8f;
    [Tooltip("百分比动画最短播放时长")]
    [SerializeField] private float completionCountDurationMin = 0.3f;
    [SerializeField] private float completionTriggerDelay     = 1.0f;
    [SerializeField] private string completionFinishTrigger   = "Finish";
    [SerializeField] private float  completionScaleMin        = 0.8f;
    [SerializeField] private float  completionScaleMax        = 2.0f;
    [Tooltip("结算页面最短停留时间（秒）")]
    [SerializeField] private float completionMinDisplayTime = 8f;

    [Header("Total Score UI")]
    [Tooltip("始终显示在关卡上的总分文本")]
    [SerializeField] private TMP_Text totalScoreText;
    [Tooltip("completionFinishTrigger 触发后，延迟多少秒再开始滚动总分")]
    [SerializeField] private float totalScoreDelay = 1.5f;
    [Tooltip("总分滚动动画时长（秒）")]
    [SerializeField] private float totalScoreCountDuration = 0.6f;
    [Tooltip("总分满分值（仅用于显示参考）")]
    [SerializeField] private float totalScoreMax = 700f;
    [Tooltip("总分滚动时每次数字变化的抖动强度")]
    [SerializeField] private float totalScorePunchScale = 0.15f;
    [Tooltip("总分滚动时每次数字变化的抖动时长")]
    [SerializeField] private float totalScorePunchDuration = 0.12f;
    [Tooltip("总分每跳动一次播放的音效")]
    [SerializeField] private AudioClip totalScoreTickSound;
    [Tooltip("总分音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float totalScoreTickVolume = 0.8f;
    [Tooltip("总分音效音调范围下限")]
    [SerializeField] private float totalScoreTickPitchMin = 0.9f;
    [Tooltip("总分音效音调范围上限")]
    [SerializeField] private float totalScoreTickPitchMax = 1.3f;
    [Tooltip("总分音效 AudioSource（留空则自动创建）")]
    [SerializeField] private AudioSource totalScoreAudioSource;

    [Header("Milestone Punch")]
    public int milestoneInterval = 10;
    [SerializeField] private float punchDuration      = 0.22f;
    [SerializeField] private float punchDurationAt100 = 0.55f;
    [SerializeField] private float punchRotationAngle = 9f;
    [SerializeField] private float punchScaleAmount   = 0.28f;

    [Header("Score Sound")]
    [SerializeField] private AudioClip   tickSound;
    [SerializeField] private float       tickPitchMin = 0.8f;
    [SerializeField] private float       tickPitchMax = 2.0f;
    [SerializeField] private AudioSource tickAudioSource;

    [Header("Milestone Sounds (Per Percent)")]
    [SerializeField] private MilestoneSound[] milestoneSounds;
    [SerializeField] private AudioSource      milestoneAudioSource;

    [Header("Score Grades")]
    public ScoreGrade[] scoreGrades;

    [System.Serializable]
    public class ScoreGrade
    {
        public int          threshold;
        public GameObject[] objects;
    }

    [Header("Results Screen")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Image[]    resultsImages;

    [Header("Game Over")]
    [SerializeField] private UnityEvent onAllLevelsComplete;

    [Header("Tutorial")]
    [Tooltip("开启后：第一关使用固定关卡，其余随机抽选；关闭则全部随机（原逻辑）")]
    [SerializeField] private bool  enableTutorial = false;
    [Tooltip("固定的第一关 Level（enableTutorial 开启时使用）")]
    [SerializeField] private Level fixedFirstLevel;
    [Tooltip("教程弹窗的根 GameObject（显示/隐藏整体）")]
    [SerializeField] private GameObject tutorialPanel;
    [Tooltip("教程纸条的图片列表，按顺序填入，支持任意数量；每次按键/点击翻到下一张")]
    [SerializeField] private Image[] tutorialImages;

    // ───────────────────────── Runtime ─────────────────────────
    private float       timer;
    private float       timerImageInitialWidth;
    private float       currentLevelTime;
    private int         currentLevel;
    private List<Level> activeLevels = new();

    private readonly List<Texture2D> capturedTextures = new();
    private readonly List<Sprite>    capturedSprites  = new();

    private float _scoreCurrentPercent;
    private bool  _scoreAnimRunning;
    private Tween _bgmFadeTween;

    private float _totalScore;
    private Tween _totalScoreTween;

    public IReadOnlyList<Texture2D> CapturedTextures => capturedTextures;
    public IReadOnlyList<Sprite>    CapturedSprites  => capturedSprites;
    public List<Level>              LevelList        => levelList;
    public int                      CurrentLevel     => currentLevel;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (timerImage != null)
            timerImageInitialWidth = timerImage.sizeDelta.x;

        if (tickAudioSource == null)
        {
            tickAudioSource = gameObject.AddComponent<AudioSource>();
            tickAudioSource.playOnAwake  = false;
            tickAudioSource.spatialBlend = 0f;
        }

        if (milestoneAudioSource == null)
        {
            milestoneAudioSource = gameObject.AddComponent<AudioSource>();
            milestoneAudioSource.playOnAwake  = false;
            milestoneAudioSource.spatialBlend = 0f;
        }

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

        // ── 构建 activeLevels ──────────────────────────────────
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
                .Take(Mathf.Max(0, Mathf.Min(levelCount - 1, levelList.Count - 1)))
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

        currentLevel = 0;
        FillAllPromptTexts();

        // ── 总分初始化 ─────────────────────────────────────────
        _totalScore = 0f;
        if (totalScoreText != null)
            totalScoreText.text = "0";
        if (totalScoreAudioSource == null)
        {
            totalScoreAudioSource = gameObject.AddComponent<AudioSource>();
            totalScoreAudioSource.playOnAwake  = false;
            totalScoreAudioSource.spatialBlend = 0f;
        }
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
            completionText.transform.localScale    = Vector3.one * completionScaleMin;
            completionText.transform.localRotation = Quaternion.identity;
        }

        StartCoroutine(RunCurrentLevelFlow());
    }

    // ── 教程弹窗 ───────────────────────────────────────────────

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

    // ── 结算 UI ───────────────────────────────────────────────

    private IEnumerator ShowCompletionForCurrentLevel(Level level)
    {
        if (completionText == null || level == null) yield break;

        if (completionShowDelay > 0f) yield return new WaitForSeconds(completionShowDelay);

        if (!completionText.gameObject.activeSelf)
        {
            completionText.text = "0%";
            completionText.transform.localScale    = Vector3.one * completionScaleMin;
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
                    completionText.text  = Mathf.RoundToInt(x) + "%";
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

        // ── Completion Finish Trigger 触发，同时开始滚动累加总分 ──
        TriggerAnimator(completionTextAnimator, completionFinishTrigger);
        AddToTotalScore(level.similarity * level.scoreWeight);
    }

    // ── 总分累加 ──────────────────────────────────────────────

    private void AddToTotalScore(float addAmount)
    {
        StartCoroutine(AddToTotalScoreCoroutine(addAmount));
    }
    private IEnumerator AddToTotalScoreCoroutine(float addAmount)
    {
        // 延迟
        if (totalScoreDelay > 0f)
            yield return new WaitForSeconds(totalScoreDelay);

        if (totalScoreText == null) yield break;

        float fromScore = _totalScore;
        float toScore   = _totalScore + addAmount;
        _totalScore     = toScore;

        int lastDisplayed = Mathf.RoundToInt(fromScore);

        _totalScoreTween?.Kill();

        float displayed = fromScore;
        _totalScoreTween = DOTween.To(
            ()  => displayed,
            x   =>
            {
                displayed = x;
                int current = Mathf.RoundToInt(x);

                // 数字发生变化时触发抖动和音效
                if (current != lastDisplayed)
                {
                    lastDisplayed = current;
                    totalScoreText.text = current.ToString();

                    // 抖动
                    totalScoreText.transform.DOKill(false);
                    totalScoreText.transform.DOPunchScale(
                        Vector3.one * totalScorePunchScale,
                        totalScorePunchDuration, 5, 0.4f);

                    // 音效：音调随分数进度升高
                    if (totalScoreTickSound != null && totalScoreAudioSource != null)
                    {
                        float progress = toScore > 0f
                            ? Mathf.Clamp01((x - fromScore) / (toScore - fromScore))
                            : 0f;
                        totalScoreAudioSource.pitch =
                            Mathf.Lerp(totalScoreTickPitchMin, totalScoreTickPitchMax, progress);
                        totalScoreAudioSource.PlayOneShot(totalScoreTickSound, totalScoreTickVolume);
                    }
                }
            },
            toScore,
            totalScoreCountDuration
        ).SetEase(Ease.OutCubic);
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

    // ── 姿势检测 ──────────────────────────────────────────────

    private void CheckPose()
    {
        if (currentLevel >= activeLevels.Count) return;

        var level = activeLevels[currentLevel];
        Canvas canvas            = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        // ── 1. 关节在主 zone 内的占比 ─────────────────────────
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

        // ── 2. 必须触碰区域扣分 ───────────────────────────────
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

        // ── 3. 合并结果 ───────────────────────────────────────
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
        _totalScoreTween?.Kill();
        _scoreAnimRunning = false;
        foreach (var s in capturedSprites)  if (s != null) Destroy(s);
        foreach (var t in capturedTextures) if (t != null) Destroy(t);
        capturedSprites.Clear();
        capturedTextures.Clear();
    }
}