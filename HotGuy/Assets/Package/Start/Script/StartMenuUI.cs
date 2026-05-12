using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 主菜单 UI 控制器
/// 点击开始/继续游戏后，黑色 Image 透明度逐渐提高（渐黑），
/// 完成后加载对应场景。
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    // ─── 场景索引 ─────────────────────────────────────────────────────────────

    [Header("场景索引（对应 Build Settings 中的顺序）")]
    [SerializeField] private int newGameSceneIndex = 1;
    [SerializeField] private int continueSceneIndex = 1;

    // ─── 渐黑图片 ─────────────────────────────────────────────────────────────

    [Header("渐黑遮罩")]
    [Tooltip("黑色全屏 Image，初始 Alpha = 0")]
    [SerializeField] private Image fadeImage;

    [Tooltip("渐黑持续时间（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    // ─── 常驻按钮 ─────────────────────────────────────────────────────────────

    [Header("常驻按钮")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    // ─── 有存档时显示的按钮 ───────────────────────────────────────────────────

    [Header("有存档时显示的按钮")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;

    [Header("可选")]
    [SerializeField] private GameObject saveButtonsGroup;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // 确保初始完全透明
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        RefreshSaveButtonVisibility();
        RegisterButtonListeners();
    }

    // ─── 存档按钮显示逻辑 ─────────────────────────────────────────────────────

    private void RefreshSaveButtonVisibility()
    {
        bool hasSave = PlayerPrefs.GetInt("HasSaveData", 0) == 1;

        if (startButton    != null) startButton.gameObject.SetActive(!hasSave);
        if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
        if (newGameButton  != null) newGameButton.gameObject.SetActive(hasSave);
        if (saveButtonsGroup != null) saveButtonsGroup.SetActive(hasSave);
    }

    // ─── 按钮注册 ─────────────────────────────────────────────────────────────

    private void RegisterButtonListeners()
    {
        if (startButton    != null) startButton.onClick.AddListener(OnStartGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueGame);
        if (newGameButton  != null) newGameButton.onClick.AddListener(OnNewGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (creditsButton  != null) creditsButton.onClick.AddListener(OnCredits);
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuit);
    }

    // ─── 按钮逻辑 ─────────────────────────────────────────────────────────────

    private void OnStartGame()
    {
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();
        FadeAndLoad(newGameSceneIndex);
    }

    private void OnContinueGame()
    {
        FadeAndLoad(continueSceneIndex);
    }

    private void OnNewGame()
    {
        PlayerPrefs.DeleteKey("HasSaveData");
        PlayerPrefs.Save();
        FadeAndLoad(newGameSceneIndex);
    }

    private void OnSettings()
    {
        Debug.Log("[StartMenu] 设置按下 — 暂无界面");
    }

    private void OnCredits()
    {
        Debug.Log("[StartMenu] 制作人员按下 — 暂无界面");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── 渐黑后加载场景 ───────────────────────────────────────────────────────

    private void FadeAndLoad(int sceneIndex)
    {

        if (fadeImage == null)
        {
            Debug.LogWarning("[StartMenu] fadeImage 未赋值，直接跳转。");
            SceneManager.LoadScene(sceneIndex);
            return;
        }

        // 从透明渐变到不透明，完成后加载场景
        fadeImage.DOFade(1f, fadeDuration)
                 .SetEase(Ease.InQuad)
                 .OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (startButton    != null) startButton.interactable    = interactable;
        if (continueButton != null) continueButton.interactable = interactable;
        if (newGameButton  != null) newGameButton.interactable  = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (creditsButton  != null) creditsButton.interactable  = interactable;
        if (quitButton     != null) quitButton.interactable     = interactable;
    }

    // ─── 清理 ─────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (startButton    != null) startButton.onClick.RemoveListener(OnStartGame);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueGame);
        if (newGameButton  != null) newGameButton.onClick.RemoveListener(OnNewGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        if (creditsButton  != null) creditsButton.onClick.RemoveListener(OnCredits);
        if (quitButton     != null) quitButton.onClick.RemoveListener(OnQuit);
    }
}