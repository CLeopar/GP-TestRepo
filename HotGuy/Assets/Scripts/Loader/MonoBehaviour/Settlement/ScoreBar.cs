using UnityEngine;
using System.Collections;

public class ScoreBar : MonoBehaviour
{
    public enum DisplayType
    {
        TotalScore,
        HighScore,
        AverageCompletion,
        CompletionBelow60Count,
        CompletionAbove95Count,
        L1_TotalScore,
        L1_HighScore,
        L1_TasksCompleted,
        L1_ShitEaten,
        L1_FoodEaten,
    }

    [Header("数据来源")]
    [SerializeField] private DisplayType displayType = DisplayType.TotalScore;

    [Header("分数 → 宽度 映射")]
    [SerializeField] private float widthAtZero = 0f;
    [SerializeField] private float widthAtMax = 5f;
    [SerializeField] private float maxScore = 1000f;

    [Header("进度条动画")]
    [SerializeField] private float lerpSpeed = 3f;

    [Header("星星配置（按顺序）")]
    [SerializeField] private StarThreshold[] stars = new StarThreshold[3];

    [Header("开始延迟")]
    [SerializeField] private float startDelay = 1f;

    [Header("星星音效")]
    [SerializeField] private AudioSource starAudioSource;
    [SerializeField] private AudioClip starSound;

    [System.Serializable]
    public class StarThreshold
    {
        [Tooltip("达到该真实分数时触发")]
        public float scoreThreshold = 200f;

        public GameObject starObject;
        public float delayAfterPrevious = 0.15f;

        [HideInInspector] public bool activated;
    }

    private SpriteRenderer sr;
    private float targetWidth;
    private bool initialized;

    private bool allowStarTrigger;
    private bool isPlayingStarSequence;

    [Header("运行时（只读）")]
    [SerializeField] private float _currentScore;
    [SerializeField] private float _currentWidth;
    [SerializeField] private float _targetWidth;

    // ===================== Unity =====================

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            Debug.LogError("[ScoreBar] 未找到 SpriteRenderer！", this);

        SetWidth(widthAtZero);
        targetWidth = widthAtZero;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;

        allowStarTrigger = false;
        isPlayingStarSequence = false;

        ResetStars();
        SetWidth(widthAtZero);
        targetWidth = widthAtZero;

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(startDelay);

        UpdateBar();

        // 只有有分数才允许触发
        if (ReadScore() > 0f)
            allowStarTrigger = true;
    }

    private void Update()
    {
        if (sr == null) return;

        float current = sr.size.x;
        float next = lerpSpeed <= 0f
            ? targetWidth
            : Mathf.Lerp(current, targetWidth, Time.deltaTime * lerpSpeed);

        if (Mathf.Abs(next - targetWidth) < 0.001f)
            next = targetWidth;

        SetWidth(next);

        _currentWidth = sr.size.x;

        // ⭐ 关键修复：用真实 score，不用 visualScore
        CheckStarTriggers(ReadScore());
    }

    // ===================== 核心 =====================

    public void UpdateBar()
    {
        float score = ReadScore();
        _currentScore = score;

        if (score <= 0f)
        {
            targetWidth = widthAtZero;
            _targetWidth = targetWidth;
            allowStarTrigger = false;
            return;
        }

        float ratio = maxScore > 0f ? Mathf.Clamp01(score / maxScore) : 0f;
        targetWidth = Mathf.Lerp(widthAtZero, widthAtMax, ratio);
        _targetWidth = targetWidth;

        if (lerpSpeed <= 0f)
        {
            SetWidth(targetWidth);
            CheckStarTriggers(score);
        }
    }

    private void CheckStarTriggers(float score)
    {
        if (!allowStarTrigger || isPlayingStarSequence) return;

        for (int i = 0; i < stars.Length; i++)
        {
            var star = stars[i];
            if (star.activated || star.starObject == null) continue;

            if (score >= star.scoreThreshold)
            {
                StartCoroutine(PlayStarSequence(i));
                break;
            }
        }
    }

    private IEnumerator PlayStarSequence(int startIndex)
    {
        isPlayingStarSequence = true;

        float score = ReadScore(); // 锁定当前真实分数

        for (int i = startIndex; i < stars.Length; i++)
        {
            var star = stars[i];
            if (star.activated || star.starObject == null) continue;

            // ⭐ 再次确认（防止中途变化）
            if (score < star.scoreThreshold)
                break;

            star.starObject.SetActive(true);
            star.activated = true;

            PlayStarSound();

            yield return new WaitForSeconds(star.delayAfterPrevious);
        }

        isPlayingStarSequence = false;
    }

    // ===================== 工具 =====================

    private float ReadScore()
    {
        return displayType switch
        {
            DisplayType.TotalScore             => PlayerPrefs.GetFloat("GameStats_TotalScore", 0f),
            DisplayType.HighScore              => PlayerPrefs.GetFloat("GameStats_HighScore", 0f),
            DisplayType.AverageCompletion      => PlayerPrefs.GetFloat("GameStats_AverageCompletion", 0f),
            DisplayType.CompletionBelow60Count => PlayerPrefs.GetInt("GameStats_Below60", 0),
            DisplayType.CompletionAbove95Count => PlayerPrefs.GetInt("GameStats_Above95", 0),
            DisplayType.L1_TotalScore          => PlayerPrefs.GetInt("L1_TotalScore", 0),
            DisplayType.L1_HighScore           => PlayerPrefs.GetInt("L1_HighScore", 0),
            DisplayType.L1_TasksCompleted      => PlayerPrefs.GetInt("L1_TasksCompleted", 0),
            DisplayType.L1_ShitEaten           => PlayerPrefs.GetInt("L1_ShitEaten", 0),
            DisplayType.L1_FoodEaten           => PlayerPrefs.GetInt("L1_FoodEaten", 0),
            _ => 0f
        };
    }

    private void SetWidth(float width)
    {
        if (sr == null) return;
        sr.size = new Vector2(width, sr.size.y);
    }

    private void ResetStars()
    {
        foreach (var star in stars)
        {
            star.activated = false;
            if (star.starObject != null)
                star.starObject.SetActive(false);
        }
    }

    private void PlayStarSound()
    {
        if (starAudioSource == null || starSound == null) return;
        starAudioSource.PlayOneShot(starSound);
    }

    // ===================== Debug =====================

    [ContextMenu("刷新")]
    private void DebugRefresh()
    {
        StopAllCoroutines();
        allowStarTrigger = false;
        isPlayingStarSequence = false;
        ResetStars();
        SetWidth(widthAtZero);
        targetWidth = widthAtZero;
        StartCoroutine(DelayedStart());
    }

    [ContextMenu("满分")]
    private void DebugFull()
    {
        float score = ReadScore();
        SetWidth(widthAtMax);
        allowStarTrigger = true;
        CheckStarTriggers(score);
    }

    [ContextMenu("清零")]
    private void DebugZero()
    {
        StopAllCoroutines();
        allowStarTrigger = false;
        isPlayingStarSequence = false;
        ResetStars();
        SetWidth(widthAtZero);
        targetWidth = widthAtZero;
    }
}