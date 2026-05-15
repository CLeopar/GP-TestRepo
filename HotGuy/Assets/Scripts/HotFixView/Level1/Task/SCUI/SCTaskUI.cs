using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SCTaskUI : MonoBehaviour
{
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

    public long TaskId { get; private set; }
    private List<Image> _activeFoodImages = new List<Image>();
    private int _foodCount;
    private List<FoodType> _foodSequence;
    private float _totalDuration;
    private float _remainingTime;

    // ========== 新增：静态 Sprite 缓存，全局复用 ==========
    private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    public void Init(long taskId, List<FoodType> foodSequence, List<SCItemData> scItems)
    {
        TaskId = taskId;
        _foodSequence = foodSequence;
        _foodCount = foodSequence.Count;

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
        // TODO: 实际播放动画
    }

    private async FTask LoadFoodIcon(int index, FoodType foodType, SCUIState state)
    {
        if (index < 0 || index >= _activeFoodImages.Count) return;

        var scene = GameEntry.Instance._scene;
        var foodConfig = scene.GetComponent<Tables>().FoodConfigCategory.Get(foodType);
        string resName = $"{foodConfig.IconResName}_{state}";

        // ========== 修改：先查缓存，没有再加载 ==========
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