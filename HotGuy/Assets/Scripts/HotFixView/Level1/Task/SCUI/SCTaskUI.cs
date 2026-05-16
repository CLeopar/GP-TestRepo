using System.Collections.Generic;
using DG.Tweening;
using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SCTaskUI : MonoBehaviour
{
    [Header("动画容器")]
    public RectTransform Content;

    [Header("倒计时UI")]
    public Image SCUnFinishedImage;
    public Image SCFinishedImage;
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

    private Vector2 _contentTargetPos;

    public void Init(long taskId, List<FoodType> foodSequence, List<SCItemData> scItems)
    {
        TaskId = taskId;
        _foodSequence = foodSequence;
        _foodCount = foodSequence.Count;

        if (Content != null)
        {
            _contentTargetPos = Content.anchoredPosition;
            Content.anchoredPosition = new Vector2(_contentTargetPos.x - 300f, _contentTargetPos.y);
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

    public void PlayCompleteAnimation()
    {
        if (Content == null) return;
        
        Content.DOShakeAnchorPos(0.4f, new Vector2(10f, 0f), 20, 0)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Content.DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => gameObject.SetActive(false));
            });
    }

    private void PlaySpawnAnimation()
    {
        if (Content == null) return;
        
        Content.DOAnchorPos(_contentTargetPos, 0.4f)
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