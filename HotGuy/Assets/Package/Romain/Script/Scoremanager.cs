using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 负责所有分数相关逻辑：单关结算动画、总分累加、音效、里程碑、评级。
/// 挂在场景内任意常驻 GameObject 上，通过 ScoreManager.Instance 访问。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ───────────────────── Completion UI ─────────────────────
    [Header("Completion UI")]
    [SerializeField] private TMP_Text  completionText;
    [SerializeField] private Animator  completionTextAnimator;
    [SerializeField] private string    completionNextLevelTrigger = "Next";
    [SerializeField] private float     completionShowDelay        = 0.5f;
    [SerializeField] private float     completionCountDuration    = 0.8f;
    [Tooltip("百分比动画最短播放时长")]
    [SerializeField] private float     completionCountDurationMin = 0.3f;
    [SerializeField] private float     completionTriggerDelay     = 1.0f;
    [SerializeField] private string    completionFinishTrigger    = "Finish";
    [SerializeField] private float     completionScaleMin         = 0.8f;
    [SerializeField] private float     completionScaleMax         = 2.0f;

    // ───────────────────── Total Score UI ─────────────────────
    [Header("Total Score UI")]
    [Tooltip("始终显示在关卡上的总分文本")]
    [SerializeField] private TMP_Text totalScoreText;
    [Tooltip("completionFinishTrigger 触发后，延迟多少秒再开始滚动总分")]
    [SerializeField] private float    totalScoreDelay         = 1.5f;
    [Tooltip("总分滚动动画时长（秒）")]
    [SerializeField] private float    totalScoreCountDuration = 0.6f;
    [Tooltip("总分满分值（仅用于显示参考）")]
    [SerializeField] private float    totalScoreMax           = 700f;
    [Tooltip("总分滚动时每次数字变化的抖动强度")]
    [SerializeField] private float    totalScorePunchScale    = 0.15f;
    [Tooltip("总分滚动时每次数字变化的抖动时长")]
    [SerializeField] private float    totalScorePunchDuration = 0.12f;
    [Tooltip("总分每跳动一次播放的音效")]
    [SerializeField] private AudioClip totalScoreTickSound;
    [Tooltip("总分音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float    totalScoreTickVolume   = 0.8f;
    [Tooltip("总分音效音调范围下限")]
    [SerializeField] private float    totalScoreTickPitchMin = 0.9f;
    [Tooltip("总分音效音调范围上限")]
    [SerializeField] private float    totalScoreTickPitchMax = 1.3f;
    [Tooltip("总分音效 AudioSource（留空则自动创建）")]
    [SerializeField] private AudioSource totalScoreAudioSource;

    // ───────────────────── Milestone Punch ─────────────────────
    [Header("Milestone Punch")]
    public int milestoneInterval = 10;
    [SerializeField] private float punchDuration      = 0.22f;
    [SerializeField] private float punchDurationAt100 = 0.55f;
    [SerializeField] private float punchRotationAngle = 9f;
    [SerializeField] private float punchScaleAmount   = 0.28f;

    // ───────────────────── Score Sound ─────────────────────────
    [Header("Score Sound")]
    [SerializeField] private AudioClip   tickSound;
    [SerializeField] private float       tickPitchMin = 0.8f;
    [SerializeField] private float       tickPitchMax = 2.0f;
    [SerializeField] private AudioSource tickAudioSource;

    // ───────────────────── Milestone Sounds ────────────────────
    [Header("Milestone Sounds (Per Percent)")]
    [SerializeField] private MilestoneSound[] milestoneSounds;
    [SerializeField] private AudioSource      milestoneAudioSource;

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

    // ───────────────────── Score Grades ────────────────────────
    [Header("Score Grades")]
    public ScoreGrade[] scoreGrades;

    [System.Serializable]
    public class ScoreGrade
    {
        public int          threshold;
        public GameObject[] objects;
    }

    // ───────────────────── 事件 ────────────────────────────────
    /// <summary>总分数值发生变化时触发（GameStatsManager 订阅此事件）</summary>
    public static event System.Action OnScoreChanged;

    // ───────────────────── Runtime ─────────────────────────────
    private float _totalScore;
    private float _scoreCurrentPercent;
    private bool  _scoreAnimRunning;
    private Tween _totalScoreTween;

    /// <summary>当前累计总分（只读）</summary>
    public float TotalScore => _totalScore;

    // ───────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        _totalScore = 0f;
        if (totalScoreText != null)
            totalScoreText.text = "0";

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

        if (totalScoreAudioSource == null)
        {
            totalScoreAudioSource = gameObject.AddComponent<AudioSource>();
            totalScoreAudioSource.playOnAwake  = false;
            totalScoreAudioSource.spatialBlend = 0f;
        }
    }

    // ── 对外主入口：GameManager 在每关结束时调用 ───────────────

    /// <summary>
    /// 播放单关结算百分比动画，动画结束后自动累加到总分。
    /// </summary>
    /// <param name="similarity">该关得分率 0~1</param>
    /// <param name="scoreWeight">该关权重分值</param>
    public IEnumerator ShowCompletionAndAddScore(float similarity, float scoreWeight)
    {
        if (completionText == null) yield break;

        if (completionShowDelay > 0f)
            yield return new WaitForSeconds(completionShowDelay);

        // 重置并显示
        if (!completionText.gameObject.activeSelf)
        {
            completionText.text = "0%";
            completionText.transform.localScale    = Vector3.one * completionScaleMin;
            completionText.transform.localRotation = Quaternion.identity;
            completionText.gameObject.SetActive(true);
        }

        float targetPercent = Mathf.Round(similarity * 100f);
        float duration = Mathf.Max(completionCountDurationMin,
            completionCountDuration * (targetPercent / 100f));

        if (duration <= 0f)
        {
            completionText.text = Mathf.RoundToInt(targetPercent) + "%";
            completionText.transform.localScale =
                Vector3.one * Mathf.Lerp(completionScaleMin, completionScaleMax, similarity);
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

        ActivateScoreGrade(similarity);

        if (completionTriggerDelay > 0f)
            yield return new WaitForSeconds(completionTriggerDelay);

        TriggerAnimator(completionTextAnimator, completionFinishTrigger);

        // 累加总分
        AddToTotalScore(similarity * scoreWeight);
    }

    /// <summary>重置结算文本到初始状态（关卡切换时调用）</summary>
    public void ResetCompletionText()
    {
        if (completionText == null) return;
        TriggerAnimator(completionTextAnimator, completionNextLevelTrigger);
        completionText.gameObject.SetActive(false);
        completionText.text = "0%";
        completionText.transform.localScale    = Vector3.one * completionScaleMin;
        completionText.transform.localRotation = Quaternion.identity;
    }

    // ── 总分累加 ──────────────────────────────────────────────

    private void AddToTotalScore(float addAmount)
    {
        StartCoroutine(AddToTotalScoreCoroutine(addAmount));
    }

    private IEnumerator AddToTotalScoreCoroutine(float addAmount)
    {
        if (totalScoreDelay > 0f)
            yield return new WaitForSeconds(totalScoreDelay);

        if (totalScoreText == null) yield break;

        float fromScore = _totalScore;
        float toScore   = _totalScore + addAmount;
        _totalScore     = toScore;
        OnScoreChanged?.Invoke();

        int lastDisplayed = Mathf.RoundToInt(fromScore);

        _totalScoreTween?.Kill();

        float displayed = fromScore;
        _totalScoreTween = DOTween.To(
            ()  => displayed,
            x   =>
            {
                displayed = x;
                int current = Mathf.RoundToInt(x);

                if (current != lastDisplayed)
                {
                    lastDisplayed = current;
                    totalScoreText.text = current.ToString();

                    totalScoreText.transform.DOKill(false);
                    totalScoreText.transform.DOPunchScale(
                        Vector3.one * totalScorePunchScale,
                        totalScorePunchDuration, 5, 0.4f);

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

    // ── 单关百分比滚动音效与里程碑 ────────────────────────────

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

    // ── 评级激活 ──────────────────────────────────────────────

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

    // ── 结算文本抖动 ──────────────────────────────────────────

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

    // ── 工具 ─────────────────────────────────────────────────

    private void TriggerAnimator(Animator anim, string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger)) return;
        anim.SetTrigger(trigger);
    }

    private void OnDestroy()
    {
        _totalScoreTween?.Kill();
        _scoreAnimRunning = false;
    }
}