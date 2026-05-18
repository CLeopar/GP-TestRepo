using System.Collections.Generic;
using DG.Tweening;
using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SCTaskUI : MonoBehaviour
{
    // ========== 新增：整个卡片的根容器，所有位移动画都操作它 ==========
    [Header("根容器（所有位移动画操作对象）")]
    public RectTransform RootContainer;

    [Header("闪光Image（仅成功动画使用）")]
    public Image SCFinishedImage;

    [Header("倒计时UI")]
    public Image SCUnFinishedImage;
    public TextMeshProUGUI SCTimerText;

    [Header("2食物容器")]
    public GameObject Food2Container;
    public Image Food2Frame1;
    public Image Food2Frame2;

    [Header("3食物容器")]
    public GameObject Food3Container;
    public Image Food3Frame1;
    public Image Food3Frame2;
    public Image Food3Frame3;

    [Header("随机话语")]
    public TextMeshProUGUI RandomQuoteText;

    public long TaskId { get; private set; }
    private List<Image> _activeFoodImages = new List<Image>();
    private int _foodCount;
    private List<FoodType> _foodSequence;
    private float _totalDuration;
    private float _remainingTime;

    private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    // 入场动画目标位置
    private Vector2 _rootTargetPos;

    public void Init(long taskId, List<FoodType> foodSequence, List<SCItemData> scItems)
    {
        TaskId = taskId;
        _foodSequence = foodSequence;
        _foodCount = foodSequence.Count;

        // ========== 入场动画：从左侧滑入 ==========
        if (RootContainer != null)
        {
            _rootTargetPos = RootContainer.anchoredPosition;
            RootContainer.anchoredPosition = new Vector2(_rootTargetPos.x - 300f, _rootTargetPos.y);
        }

        // 确保闪光 Image 初始状态正确
        if (SCFinishedImage != null)
        {
            SCFinishedImage.gameObject.SetActive(false);
            var c = SCFinishedImage.color;
            SCFinishedImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (_foodCount == 2)
        {
            Food2Container.SetActive(true);
            Food3Container.SetActive(false);
            _activeFoodImages.Add(Food2Frame1);
            _activeFoodImages.Add(Food2Frame2);
        }
        else
        {
            Food2Container.SetActive(false);
            Food3Container.SetActive(true);
            _activeFoodImages.Add(Food3Frame1);
            _activeFoodImages.Add(Food3Frame2);
            _activeFoodImages.Add(Food3Frame3);
        }

        for (int i = 0; i < _foodCount; i++)
        {
            LoadFoodIcon(i, foodSequence[i], SCUIState.Normal).Coroutine();
        }

        var config = GameEntry.Instance._scene.GetComponent<Tables>().ConstConfigCategory.Data;
        _totalDuration = scItems[0].DurationType == SCDurationType.Green_10s
            ? config.SCGreenDuration
            : config.SCOrangeDuration;

        _remainingTime = _totalDuration;
        SCUnFinishedImage.fillAmount = 1f;
        UpdateTimerDisplay();

        SetupRandomQuote();

        PlaySpawnAnimation();
    }

    public void UpdateTimer(float remainingTime)
    {
        _remainingTime = remainingTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        float ratio = _totalDuration > 0 ? _remainingTime / _totalDuration : 0;
        SCUnFinishedImage.fillAmount = Mathf.Clamp01(ratio);

        int totalSeconds = Mathf.FloorToInt(_remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        SCTimerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void SetFoodState(int index, SCUIState state)
    {
        if (index < 0 || index >= _activeFoodImages.Count) return;
        LoadFoodIcon(index, _foodSequence[index], state).Coroutine();
    }

    // ========== 成功动画：RootContainer 缩放弹跳 + SCFinishedImage 闪光 + 缩小消失 ==========
    public void PlaySuccessAnimation()
    {
        if (RootContainer == null)
        {
            Log.Error($"[SCTaskUI] PlaySuccessAnimation: RootContainer is null! TaskId={TaskId}");
            return;
        }

        // 隐藏倒计时相关UI（反正整个UI马上消失，不需要还原）
        if (SCUnFinishedImage != null)
            SCUnFinishedImage.gameObject.SetActive(false);
        if (SCTimerText != null)
            SCTimerText.gameObject.SetActive(false);

        // 准备闪光 Image
        if (SCFinishedImage != null)
        {
            var c = SCFinishedImage.color;
            SCFinishedImage.color = new Color(c.r, c.g, c.b, 0f);
            SCFinishedImage.gameObject.SetActive(true);
        }

        var seq = DOTween.Sequence();

        // 1. RootContainer 弹跳放大（成功感）
        seq.Append(RootContainer.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack));
        seq.Append(RootContainer.DOScale(1f, 0.08f).SetEase(Ease.InOutSine));

        // 2. SCFinishedImage 闪光：快速亮起再淡出
        if (SCFinishedImage != null)
        {
            seq.Append(SCFinishedImage.DOFade(1f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(SCFinishedImage.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
        }
        else
        {
            seq.AppendInterval(0.35f);
        }

        // 3. RootContainer 整体缩放消失
        seq.Append(RootContainer.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        seq.OnComplete(() => gameObject.SetActive(false));
    }

    // ========== 超时动画：RootContainer 横向抖动 + 缩小消失 ==========
    public void PlayTimeoutAnimation()
    {
        if (RootContainer == null)
        {
            Log.Error($"[SCTaskUI] PlayTimeoutAnimation: RootContainer is null! TaskId={TaskId}");
            return;
        }

        // 杀掉可能正在进行的其他动画
        DOTween.Kill(RootContainer);

        RootContainer.DOShakeAnchorPos(0.4f, new Vector2(10f, 0f), 20, 0)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                RootContainer.DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => gameObject.SetActive(false));
            });
    }

    // ========== 入场动画：从左侧滑入 ==========
    private void PlaySpawnAnimation()
    {
        if (RootContainer == null)
        {
            Log.Error($"[SCTaskUI] PlaySpawnAnimation: RootContainer is null! TaskId={TaskId}");
            return;
        }

        DOTween.Kill(RootContainer);
        RootContainer.DOAnchorPos(_rootTargetPos, 0.4f)
            .SetEase(Ease.OutCubic);
    }

    private void SetupRandomQuote()
    {
        var quoteConfig = GameEntry.Instance._scene
            .GetComponent<Tables>()
            .SCTaskQuoteConfigCategory;

        var allQuotes = quoteConfig.DataList;
        if (allQuotes == null || allQuotes.Count == 0 || RandomQuoteText == null)
            return;

        int index = UnityEngine.Random.Range(0, allQuotes.Count);
        RandomQuoteText.text = allQuotes[index].Content;
    }

    private async FTask LoadFoodIcon(int index, FoodType foodType, SCUIState state)
    {
        if (index < 0 || index >= _activeFoodImages.Count) return;

        var scene = GameEntry.Instance._scene;
        var foodConfig = scene.GetComponent<Tables>().FoodConfigCategory.Get(foodType);
        string resName = $"{foodConfig.IconResName}_{state}";

        if (!_spriteCache.TryGetValue(resName, out var sprite))
        {
            sprite = await scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(resName);
            if (sprite != null)
                _spriteCache[resName] = sprite;
        }

        if (_activeFoodImages[index] != null && sprite != null)
            _activeFoodImages[index].sprite = sprite;
    }
}