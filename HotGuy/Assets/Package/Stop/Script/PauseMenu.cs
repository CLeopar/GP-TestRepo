using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 暂停菜单控制器
/// 以预制体形式存在，挂载在暂停面板的根 GameObject 上。
///
/// 预制体层级结构建议：
///   PauseMenuPanel (此脚本挂在这里)
///   ├── Background (半透明遮罩 Image)
///   ├── Panel (主面板)
///   │   ├── TopGroup (上方 UI 的父物体)
///   │   │   ├── Title
///   │   │   └── BtnResume
///   │   └── BottomGroup (下方 UI 的父物体)
///   │       ├── BtnSettings
///   │       └── BtnMainMenu
///   └── HintsOverlay         操作提示子面板
///       ├── HintsImage       显示提示图片的 Image 组件
///       └── BtnCloseHints    关闭提示
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    // ─── 开场动画 ─────────────────────────────────────────────────────────────

    [Header("开场动画")]
    [Tooltip("Background 的 Image（用于淡入淡出）")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("上方 UI 元素的父物体（如标题、继续按钮）")]
    [SerializeField] private RectTransform topGroup;

    [Tooltip("下方 UI 元素的父物体（如设置、返回主菜单）")]
    [SerializeField] private RectTransform bottomGroup;

    [Tooltip("动画时长（秒）")]
    [SerializeField] private float animDuration = 0.4f;

    [Tooltip("滑入距离（像素）")]
    [SerializeField] private float slideDistance = 80f;

    // ─── 渐黑遮罩（用于场景切换）──────────────────────────────────────────────

    [Header("渐黑遮罩")]
    [Tooltip("黑色全屏 Image，初始 Alpha = 0，用于切换场景时渐黑")]
    [SerializeField] private Image fadeImage;

    [Tooltip("渐黑持续时间（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    // ─── 按钮引用 ─────────────────────────────────────────────────────────────

    [Header("按钮引用")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnHints;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnMainMenu;

    // ─── 操作提示面板 ─────────────────────────────────────────────────────────

    [Header("操作提示面板")]
    [SerializeField] private GameObject hintsOverlay;
    [SerializeField] private Image hintsImage;
    [SerializeField] private Button btnCloseHints;

    // ─── 各关卡操作提示图片 ───────────────────────────────────────────────────

    [Header("各关卡操作提示图片")]
    [Tooltip("下标对应关卡（0 = 第一关，1 = 第二关）")]
    [SerializeField] private Sprite[] hintsSprites;

    // ─── 私有状态 ─────────────────────────────────────────────────────────────

    private int currentLevelIndex = 0;

    // 缓存原始位置，用于动画
    private Vector2 topGroupOriginalPos;
    private Vector2 bottomGroupOriginalPos;

    // 防止重复触发场景切换
    private bool _isFading = false;

    // 缓存 Level_1 引用
    private GameObject _levelRoot;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 确保渐黑遮罩初始透明
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // 确保背景初始透明
        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }

        // 缓存原始位置
        if (topGroup != null)
            topGroupOriginalPos = topGroup.anchoredPosition;
        if (bottomGroup != null)
            bottomGroupOriginalPos = bottomGroup.anchoredPosition;

        RegisterButtonListeners();

        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        gameObject.SetActive(false);
    }

    // ─── 对外接口 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 打开暂停菜单。
    /// 在 LevelController 里调用：PauseMenu.Instance.Open(levelIndex);
    /// </summary>
    public void Open(int levelIndex = 0)
    {
        Level1PauseState.IsPaused = true;

        // 先杀掉可能残留的动画，防止快速连按 Escape 导致 tween 堆积
        KillAllTweens();

        currentLevelIndex = levelIndex;
        gameObject.SetActive(true);

        // 禁用 Level_1（冻结 ECS）
        _levelRoot = GameObject.Find("Level_1");
        if (_levelRoot != null)
            _levelRoot.SetActive(false);

        // 暂停所有 DOTween
        DOTween.PauseAll();

        // 冻结所有 Rigidbody2D
        var allRBs = FindObjectsOfType<Rigidbody2D>();
        foreach (var rb in allRBs)
            rb.simulated = false;

        // 清理弹幕
        var scene = GameEntry.Instance?._scene;
        if (scene != null)
        {
            var danmakuUIComp = scene.GetComponent<DanmakuUIComponent>();
            danmakuUIComp?.ClearAll();
        }

        _isFading = false;

        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        // 恢复按钮可交互状态
        SetAllButtonsInteractable(true);

        PlayOpenAnimation();
    }

    /// <summary>关闭暂停菜单，恢复游戏时间。</summary>
    public void Close()
    {
        Level1PauseState.IsPaused = false;

        // 如果有动画在播放，先杀掉避免冲突
        KillAllTweens();

        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        gameObject.SetActive(false);

        // 恢复 Level_1
        if (_levelRoot != null)
            _levelRoot.SetActive(true);

        // 恢复 DOTween
        DOTween.PlayAll();

        // 恢复所有 Rigidbody2D
        var allRBs = FindObjectsOfType<Rigidbody2D>();
        foreach (var rb in allRBs)
            rb.simulated = true;
    }

    // ─── 动画工具 ─────────────────────────────────────────────────────────────

    /// <summary>杀掉所有相关 tween，防止堆积。</summary>
    private void KillAllTweens()
    {
        if (backgroundImage != null) DOTween.Kill(backgroundImage);
        if (topGroup != null) DOTween.Kill(topGroup);
        if (bottomGroup != null) DOTween.Kill(bottomGroup);
        if (fadeImage != null) DOTween.Kill(fadeImage);
    }

    // ─── 开场动画 ─────────────────────────────────────────────────────────────

    private void PlayOpenAnimation()
    {
        // ── Background 淡入 ──────────────────────────────────────────
        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;

            backgroundImage.DOFade(1f, animDuration)
                           .SetUpdate(true); // timeScale=0 下也能播放
        }

        // ── 上方元素从上滑入 ─────────────────────────────────────────
        if (topGroup != null)
        {
            topGroup.anchoredPosition = topGroupOriginalPos + Vector2.up * slideDistance;

            topGroup.DOAnchorPos(topGroupOriginalPos, animDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
        }

        // ── 下方元素从下滑入 ─────────────────────────────────────────
        if (bottomGroup != null)
        {
            bottomGroup.anchoredPosition = bottomGroupOriginalPos + Vector2.down * slideDistance;

            bottomGroup.DOAnchorPos(bottomGroupOriginalPos, animDuration)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(true);
        }
    }

    // ─── 按钮注册 ─────────────────────────────────────────────────────────────

    private void RegisterButtonListeners()
    {
        if (btnResume    != null) btnResume.onClick.AddListener(OnResume);
        if (btnRestart   != null) btnRestart.onClick.AddListener(OnRestart);
        if (btnHints     != null) btnHints.onClick.AddListener(OnHints);
        if (btnSettings  != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMainMenu  != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        if (btnCloseHints != null) btnCloseHints.onClick.AddListener(OnCloseHints);
    }

    // ─── 按钮交互控制 ─────────────────────────────────────────────────────────

    /// <summary>批量设置所有菜单按钮的 interactable 状态。</summary>
    private void SetAllButtonsInteractable(bool interactable)
    {
        if (btnResume    != null) btnResume.interactable = interactable;
        if (btnRestart   != null) btnRestart.interactable = interactable;
        if (btnHints     != null) btnHints.interactable = interactable;
        if (btnSettings  != null) btnSettings.interactable = interactable;
        if (btnMainMenu  != null) btnMainMenu.interactable = interactable;
    }

    // ─── 按钮逻辑 ─────────────────────────────────────────────────────────────

    /// <summary>继续：关闭暂停菜单，恢复游戏。</summary>
    private void OnResume()
    {
        Close();
    }

    /// <summary>重新开始：渐黑后重新加载当前场景。</summary>
    private void OnRestart()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        FadeAndLoad(currentScene);
    }

    /// <summary>操作提示：显示当前关卡对应的提示图片。</summary>
    private void OnHints()
    {
        if (hintsOverlay == null || hintsImage == null)
        {
            Debug.LogWarning("[PauseMenu] 操作提示面板或 Image 未赋值");
            return;
        }

        if (hintsSprites != null && currentLevelIndex < hintsSprites.Length)
            hintsImage.sprite = hintsSprites[currentLevelIndex];
        else
            Debug.LogWarning($"[PauseMenu] 没有找到第 {currentLevelIndex} 关的操作提示图片");

        hintsOverlay.SetActive(true);
    }

    /// <summary>关闭操作提示。</summary>
    private void OnCloseHints()
    {
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);
    }

    /// <summary>设置：暂时无反应。</summary>
    private void OnSettings()
    {
        Debug.Log("[PauseMenu] 设置按下 — 暂无界面");
    }

    /// <summary>回到主菜单：渐黑后加载场景0。</summary>
    private void OnMainMenu()
    {
        FadeAndLoad(0);
    }

    // ─── 渐黑后加载场景 ───────────────────────────────────────────────────────

    private void FadeAndLoad(int sceneIndex)
    {
        if (_isFading) return;  // 防止重复调用
        _isFading = true;

        // 恢复 ECS（如果还在暂停）
        Level1PauseState.IsPaused = false;
        if (_levelRoot != null)
            _levelRoot.SetActive(true);
        DOTween.PlayAll();
        var allRBs = FindObjectsOfType<Rigidbody2D>();
        foreach (var rb in allRBs)
            rb.simulated = true;

        // 禁用所有按钮，防止点击穿透和重复触发
        SetAllButtonsInteractable(false);

        if (fadeImage == null)
        {
            Debug.LogWarning("[PauseMenu] fadeImage 未赋值，直接跳转。");
            SceneManager.LoadScene(sceneIndex);
            return;
        }

        // 杀掉可能残留的 fade tween
        DOTween.Kill(fadeImage);

        // 重置透明度后渐黑
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        fadeImage.DOFade(1f, fadeDuration)
                 .SetEase(Ease.InQuad)
                 .OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        // 修正：所有按钮都应该是 RemoveListener，原代码中 btnRestart 误写为 AddListener
        if (btnResume    != null) btnResume.onClick.RemoveListener(OnResume);
        if (btnRestart   != null) btnRestart.onClick.RemoveListener(OnRestart);  // ← 已修复
        if (btnHints     != null) btnHints.onClick.RemoveListener(OnHints);
        if (btnSettings  != null) btnSettings.onClick.RemoveListener(OnSettings);
        if (btnMainMenu  != null) btnMainMenu.onClick.RemoveListener(OnMainMenu);
        if (btnCloseHints != null) btnCloseHints.onClick.RemoveListener(OnCloseHints);

        // 清理所有 tween，防止场景切换时 DOTween 持有已销毁对象的引用
        KillAllTweens();

        // 确保恢复
        Level1PauseState.IsPaused = false;
        if (_levelRoot != null)
            _levelRoot.SetActive(true);
        DOTween.PlayAll();
        var allRBs = FindObjectsOfType<Rigidbody2D>();
        foreach (var rb in allRBs)
            rb.simulated = true;

        if (Instance == this)
            Instance = null;
    }
}