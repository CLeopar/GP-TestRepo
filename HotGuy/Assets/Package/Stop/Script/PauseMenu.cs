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
///   ├── Background (半透明遮罩)
///   ├── Panel (主面板)
///   │   ├── BtnResume        继续
///   │   ├── BtnRestart       重新开始（重新加载当前关卡）
///   │   ├── BtnHints         操作提示
///   │   ├── BtnSettings      设置（暂无反应）
///   │   └── BtnMainMenu      回到主菜单（场景0）
///   └── HintsOverlay         操作提示子面板
///       ├── HintsImage       显示提示图片的 Image 组件
///       └── BtnCloseHints    关闭提示
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    // ─── 渐黑遮罩 ─────────────────────────────────────────────────────────────

    [Header("渐黑遮罩")]
    [Tooltip("黑色全屏 Image，初始 Alpha = 0")]
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
        // 确保 fadeImage 初始透明
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

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
        currentLevelIndex = levelIndex;
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);
    }

    /// <summary>关闭暂停菜单，恢复游戏时间。</summary>
    public void Close()
    {
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        gameObject.SetActive(false);
        Time.timeScale = 1f;
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
        // 恢复时间，否则 DOTween SetUpdate(false) 的 tween 不会播放
        Time.timeScale = 1f;

        if (fadeImage == null)
        {
            Debug.LogWarning("[PauseMenu] fadeImage 未赋值，直接跳转。");
            SceneManager.LoadScene(sceneIndex);
            return;
        }

        // 重置透明度后渐黑
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        fadeImage.DOFade(1f, fadeDuration)
                 .SetEase(Ease.InQuad)
                 .SetUpdate(true) // timeScale=1 后其实不需要，但保险
                 .OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (btnResume    != null) btnResume.onClick.RemoveListener(OnResume);
        if (btnRestart   != null) btnRestart.onClick.RemoveListener(OnRestart);
        if (btnHints     != null) btnHints.onClick.RemoveListener(OnHints);
        if (btnSettings  != null) btnSettings.onClick.RemoveListener(OnSettings);
        if (btnMainMenu  != null) btnMainMenu.onClick.RemoveListener(OnMainMenu);
        if (btnCloseHints != null) btnCloseHints.onClick.RemoveListener(OnCloseHints);

        Time.timeScale = 1f;

        if (Instance == this)
            Instance = null;
    }
}