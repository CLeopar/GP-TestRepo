using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在 L1_8SC / L1_10SC Prefab 根物体上
/// </summary>
public class SCTaskUI : MonoBehaviour
{
    [Header("倒计时UI")]
    public Image SCUnFinishedImage;      // SC_UnFinished (Filled Image)
    public Image SCFinishedImage;        // SC_Finished
    public TextMeshProUGUI SCTimerText;  // SCTimerText

    [Header("2食物容器")]
    public GameObject Food2Container;    // Food2
    public Image Food2Frame1;            // FoodFrame1
    public Image Food2Frame2;            // FoodFrame2

    [Header("3食物容器")]
    public GameObject Food3Container;    // Food3
    public Image Food3Frame1;            // FoodFrame1
    public Image Food3Frame2;            // FoodFrame2
    public Image Food3Frame3;            // FoodFrame3

    [Header("状态Sprite（由外部传入）")]
    public Sprite NormalSprite;          // 常规状态
    public Sprite EatingSprite;          // 正在吃状态
    public Sprite CompletedSprite;       // 已完成状态

    /// <summary>
    /// 任务ID
    /// </summary>
    public long TaskId { get; private set; }

    /// <summary>
    /// 总时长
    /// </summary>
    private float _totalDuration;

    /// <summary>
    /// 当前激活的食物Image列表
    /// </summary>
    private List<Image> _activeFoodImages = new List<Image>();

    /// <summary>
    /// 当前食物数量
    /// </summary>
    private int _foodCount;

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init(long taskId, List<FoodType> foodSequence, List<SCItemData> scItems)
    {
        TaskId = taskId;
        _totalDuration = scItems[0].TotalDuration;
        _foodCount = foodSequence.Count;

        // 激活对应食物容器，获取Image引用
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

        // 加载食物图标 + 初始状态
        for (int i = 0; i < _foodCount; i++)
        {
            LoadFoodIcon(i, foodSequence[i]).Coroutine();
            SetFoodState(i, SCUIState.Normal);
        }

        // 倒计时初始状态
        SCUnFinishedImage.fillAmount = 1f;
        
        SCTimerText.text = "00:00"; // 初始显示
    }

    /// <summary>
    /// 加载食物图标
    /// </summary>
    private async FTask LoadFoodIcon(int index, FoodType foodType)
    {
        var tables = GameEntry.Instance._scene.GetComponent<Tables>();
        if (tables == null) return;

        var foodConfig = tables.FoodConfigCategory.Get(foodType);
        if (foodConfig == null) return;

        var sprite = await GameEntry.Instance._scene.GetComponent<ResourceLoaderComponent>()
            .LoadAssetAsync<Sprite>(foodConfig.UIResName);

        if (sprite != null && index < _activeFoodImages.Count)
        {
            _activeFoodImages[index].sprite = sprite;
        }
    }

    /// <summary>
    /// 更新倒计时显示
    /// </summary>
    public void UpdateTimer(float remainingTime)
    {
        float ratio = remainingTime / _totalDuration;
        SCUnFinishedImage.fillAmount = Mathf.Clamp01(ratio);

        // MM:SS 格式
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        SCTimerText.text = $"{minutes:D2}:{seconds:D2}";

        // 删掉这行：if (remainingTime <= 0) { SCFinishedImage.gameObject.SetActive(true); }
    }

    /// <summary>
    /// 设置指定食物的状态（换Sprite）
    /// </summary>
    public void SetFoodState(int index, SCUIState state)
    {
        if (index < 0 || index >= _activeFoodImages.Count) return;

        Sprite stateSprite = state switch
        {
            SCUIState.Normal => NormalSprite,
            SCUIState.Eating => EatingSprite,
            SCUIState.Completed => CompletedSprite,
            _ => NormalSprite
        };

        _activeFoodImages[index].sprite = stateSprite;
    }

    /// <summary>
    /// 播放完成动画
    /// </summary>
    public void PlayCompleteAnimation()
    {
        Log.Error($"[SCTaskUI] Task {TaskId} completed");
        // TODO: 缩放/淡出动画
    }
}