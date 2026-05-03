using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制试玩流程。
/// 
/// ✅ enableTutorial = true  → 方案A：弹窗询问 → 指引图片 → 试玩关卡 → 正式开头
/// ✅ enableTutorial = false → 原始方案：直接进入 IntroManager 开头流程
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // ── 开关 ──────────────────────────────────────────────────
    [Header("Tutorial Switch")]
    [Tooltip("开启 = 方案A（有教程弹窗）；关闭 = 直接进入正式开头")]
    [SerializeField] private bool enableTutorial = true;

    // ── 弹窗（外部 UI，由你设计） ──────────────────────────────
    [Header("Ask Popup (你设计的 UI)")]
    [Tooltip("询问弹窗的根 GameObject，本脚本只控制显示/隐藏")]
    [SerializeField] private GameObject askPopupRoot;
    [Tooltip("弹窗「是」按钮")]
    [SerializeField] private Button yesButton;
    [Tooltip("弹窗「否」按钮")]
    [SerializeField] private Button noButton;

    // ── 指引图片 ──────────────────────────────────────────────
    [Header("Guide Images")]
    [Tooltip("用于显示指引图片的 Image 组件")]
    [SerializeField] private Image guideImage;
    [Tooltip("指引图片列表，按顺序填入")]
    [SerializeField] private Sprite[] guideSprites;
    [Tooltip("指引图片的根 GameObject")]
    [SerializeField] private GameObject guideRoot;
    [Tooltip("最后一张图片时显示的提示 GameObject（可选）")]
    [SerializeField] private GameObject pressAnyKeyHint;

    // ── 试玩关卡 ──────────────────────────────────────────────
    [Header("Tutorial Level")]
    [Tooltip("试玩用的 Level GameObject（单独指定）")]
    [SerializeField] private GameObject tutorialLevelObj;
    [Tooltip("「开始游戏」按钮")]
    [SerializeField] private Button startGameButton;
    [Tooltip("包含「开始游戏」按钮的根 GameObject")]
    [SerializeField] private GameObject startGameRoot;

    // ── Canvas（Screen Space - Camera 必须填） ─────────────────
    [Header("Canvas (Screen Space - Camera)")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;

    // ── 引用 ──────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private IntroManager introManager;

    // ── 内部状态 ──────────────────────────────────────────────
    private GameManager.Level _tutorialLevel;
    private bool _startGamePressed;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // ── Screen Space - Camera 修复 ─────────────────────────
        if (targetCanvas != null)
        {
            if (uiCamera != null)
                targetCanvas.worldCamera = uiCamera;
            else if (targetCanvas.worldCamera == null)
            {
                targetCanvas.worldCamera = Camera.main;
                Debug.LogWarning("[TutorialManager] uiCamera 未赋值，已回退使用 Camera.main。");
            }

            if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.LogWarning("[TutorialManager] Canvas 缺少 GraphicRaycaster，已自动添加。");
            }
        }

        // ── 关闭开关：直接跳过教程，进入正式开头 ──────────────
        if (!enableTutorial)
        {
            SetActive(askPopupRoot, false);
            SetActive(guideRoot, false);
            SetActive(startGameRoot, false);
            LaunchIntro();
            return;
        }

        // ── 开启开关：走完整教程流程 ───────────────────────────
        if (tutorialLevelObj != null)
        {
            _tutorialLevel = new GameManager.Level { levelObj = tutorialLevelObj };
            _tutorialLevel.Init();
            tutorialLevelObj.SetActive(false);
        }

        SetActive(guideRoot, false);
        SetActive(startGameRoot, false);

        if (yesButton != null)      yesButton.onClick.AddListener(OnPlayerChooseYes);
        if (noButton != null)       noButton.onClick.AddListener(OnPlayerChooseNo);
        if (startGameButton != null) startGameButton.onClick.AddListener(() => _startGamePressed = true);

        SetActive(askPopupRoot, true);
    }

    // ── 弹窗回调 ──────────────────────────────────────────────

    public void OnPlayerChooseYes()
    {
        SetActive(askPopupRoot, false);
        StartCoroutine(RunTutorial());
    }

    public void OnPlayerChooseNo()
    {
        SetActive(askPopupRoot, false);
        LaunchIntro();
    }

    // ── 试玩流程 ──────────────────────────────────────────────
    private IEnumerator RunTutorial()
    {
        // 阶段1：逐张显示指引图片
        if (guideSprites != null && guideSprites.Length > 0 && guideImage != null)
        {
            SetActive(guideRoot, true);

            for (int i = 0; i < guideSprites.Length; i++)
            {
                guideImage.sprite = guideSprites[i];
                SetActive(pressAnyKeyHint, i == guideSprites.Length - 1);
                yield return WaitForAnyInput();
            }

            SetActive(pressAnyKeyHint, false);
            SetActive(guideRoot, false);
        }

        // 阶段2：可操作试玩关卡（无倒计时）
        if (_tutorialLevel != null && tutorialLevelObj != null)
        {
            tutorialLevelObj.SetActive(true);
            EnableTutorialControllers(true);

            _startGamePressed = false;
            SetActive(startGameRoot, true);
            yield return new WaitUntil(() => _startGamePressed);
            SetActive(startGameRoot, false);

            EnableTutorialControllers(false);
            tutorialLevelObj.SetActive(false);
        }

        // 阶段3：进入正式开头
        LaunchIntro();
    }

    // ── 工具 ──────────────────────────────────────────────────

    private IEnumerator WaitForAnyInput()
    {
        yield return null;
        yield return null;
        while (!Input.anyKeyDown) yield return null;
    }

    private void EnableTutorialControllers(bool enable)
    {
        if (_tutorialLevel?.poseEditorControllerList == null) return;

        foreach (var controller in _tutorialLevel.poseEditorControllerList)
        {
            if (controller == null) continue;
            if (enable) controller.Enable();
            else        controller.Disable();
        }
    }

    private void LaunchIntro()
    {
        if (introManager != null)
            introManager.StartIntro();
        else
            Debug.LogWarning("[TutorialManager] IntroManager 未赋值，无法启动开头流程。");
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}