using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 暂停菜单控制器
/// 以预制体形式存在，挂载在暂停面板的根 GameObject 上。
///
/// 预制体层级结构建议：
///   PauseMenuPanel (此脚本挂在这里)
///   ├── Background (半透明遮罩 Image)
///   ├── Panel (主面板)
///   │   ├── BtnResume        继续
///   │   ├── BtnRestart       重新开始
///   │   ├── BtnHints         操作提示
///   │   ├── BtnSettings      设置（暂无反应）
///   │   └── BtnMainMenu      回到主菜单
///   └── HintsOverlay         操作提示子面板
///       ├── HintsImage       显示提示图片的 Image 组件
///       └── BtnCloseHints    关闭提示
///
/// 使用方式：
///   1. 将预制体实例化在你的游戏场景 Canvas 下
///   2. 在每个关卡脚本中调用 PauseMenu.Instance.Open() / .Close()
///   3. 在 Inspector 中为每关配置对应的 hintsSprite
///   4. 在 Inspector 中填写主菜单场景名
///
/// 打开暂停菜单时记得同时设置 Time.timeScale = 0，
/// 关闭时恢复 Time.timeScale = 1（本脚本已处理）。
/// </summary>
public class PauseMenu : MonoBehaviour
{
    // ─── 单例（方便关卡脚本调用）─────────────────────────────────────────────
    public static PauseMenu Instance { get; private set; }

    // ─── 场景配置 ─────────────────────────────────────────────────────────────
    [Header("场景名称")]
    [Tooltip("主菜单场景名，需与 Build Settings 中一致")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // ─── 按钮引用 ─────────────────────────────────────────────────────────────
    [Header("按钮引用")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnHints;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnMainMenu;

    // ─── 操作提示面板 ─────────────────────────────────────────────────────────
    [Header("操作提示面板")]
    [Tooltip("操作提示的子面板根物体")]
    [SerializeField] private GameObject hintsOverlay;

    [Tooltip("显示提示图片的 Image 组件")]
    [SerializeField] private Image hintsImage;

    [Tooltip("关闭提示按钮")]
    [SerializeField] private Button btnCloseHints;

    // ─── 每关操作提示图片 ─────────────────────────────────────────────────────
    [Header("各关卡操作提示图片")]
    [Tooltip("下标对应关卡编号（0 = 第一关，1 = 第二关，以此类推）")]
    [SerializeField] private Sprite[] hintsSprites;

    // ─── 私有状态 ─────────────────────────────────────────────────────────────

    // 当前关卡索引（由外部调用 Open(levelIndex) 传入）
    private int currentLevelIndex = 0;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 预制体跨场景保留（如果你希望它跟随整个游戏生命周期，取消注释下一行）
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RegisterButtonListeners();

        // 确保操作提示子面板默认关闭
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        // 暂停菜单本身默认隐藏（由外部调用 Open() 显示）
        gameObject.SetActive(false);
    }

    // ─── 对外接口 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 打开暂停菜单。
    /// 在关卡脚本中监听暂停键后调用：PauseMenu.Instance.Open(levelIndex);
    /// </summary>
    /// <param name="levelIndex">当前关卡索引（0起），用于显示对应的操作提示图片</param>
    public void Open(int levelIndex = 0)
    {
        currentLevelIndex = levelIndex;
        gameObject.SetActive(true);
        Time.timeScale = 0f; // 暂停游戏时间

        // 确保操作提示子面板关闭（防止上次没关就又打开）
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);
    }

    /// <summary>
    /// 关闭暂停菜单，恢复游戏时间。
    /// </summary>
    public void Close()
    {
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);

        gameObject.SetActive(false);
        Time.timeScale = 1f; // 恢复游戏时间
    }

    // ─── 按钮注册 ─────────────────────────────────────────────────────────────

    private void RegisterButtonListeners()
    {
        if (btnResume   != null) btnResume.onClick.AddListener(OnResume);
        if (btnRestart  != null) btnRestart.onClick.AddListener(OnRestart);
        if (btnHints    != null) btnHints.onClick.AddListener(OnHints);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        if (btnCloseHints != null) btnCloseHints.onClick.AddListener(OnCloseHints);
    }

    // ─── 按钮逻辑 ─────────────────────────────────────────────────────────────

    /// <summary>继续：关闭暂停菜单，恢复游戏。</summary>
    private void OnResume()
    {
        Debug.Log("[PauseMenu] 继续游戏");
        Close();
    }

    /// <summary>重新开始：恢复时间后重新加载当前场景。</summary>
    private void OnRestart()
    {
        Debug.Log("[PauseMenu] 重新开始");
        Time.timeScale = 1f; // 必须先恢复时间，否则场景加载后仍是暂停状态
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>操作提示：显示当前关卡对应的提示图片。</summary>
    private void OnHints()
    {
        if (hintsOverlay == null || hintsImage == null)
        {
            Debug.LogWarning("[PauseMenu] 操作提示面板或 Image 未赋值");
            return;
        }

        // 根据当前关卡索引取对应图片
        if (hintsSprites != null && currentLevelIndex < hintsSprites.Length)
        {
            hintsImage.sprite = hintsSprites[currentLevelIndex];
        }
        else
        {
            Debug.LogWarning($"[PauseMenu] 没有找到第 {currentLevelIndex} 关的操作提示图片，请在 Inspector 中配置 hintsSprites");
        }

        hintsOverlay.SetActive(true);
    }

    /// <summary>关闭操作提示子面板。</summary>
    private void OnCloseHints()
    {
        if (hintsOverlay != null)
            hintsOverlay.SetActive(false);
    }

    /// <summary>设置：暂时无反应，预留接口。</summary>
    private void OnSettings()
    {
        Debug.Log("[PauseMenu] 设置按钮按下 — 暂无界面");
        // TODO: 打开设置面板
    }

    /// <summary>回到主菜单：恢复时间后加载主菜单场景。</summary>
    private void OnMainMenu()
    {
        Debug.Log("[PauseMenu] 回到主菜单");
        Time.timeScale = 1f; // 必须先恢复时间
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (btnResume   != null) btnResume.onClick.RemoveListener(OnResume);
        if (btnRestart  != null) btnRestart.onClick.RemoveListener(OnRestart);
        if (btnHints    != null) btnHints.onClick.RemoveListener(OnHints);
        if (btnSettings != null) btnSettings.onClick.RemoveListener(OnSettings);
        if (btnMainMenu != null) btnMainMenu.onClick.RemoveListener(OnMainMenu);
        if (btnCloseHints != null) btnCloseHints.onClick.RemoveListener(OnCloseHints);

        // 销毁时确保时间恢复，防止场景切换后时间卡在0
        Time.timeScale = 1f;

        if (Instance == this)
            Instance = null;
    }
}
