using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneJumpButton : MonoBehaviour
{
    [Header("目标场景")]
    [SerializeField] private int targetSceneIndex = 4;          // ← 在 Inspector 里填这个！
    [SerializeField] private bool autoLoadOnStart = false;      // 是否启动自动跳转（测试用）

    [Header("UI 组件")]
    [SerializeField] private Button jumpButton;                 // 跳转按钮
    [SerializeField] private Image fadeImage;                   // 淡入照片
    [SerializeField] private Text hintText;                   // 提示文字（可选）

    [Header("淡入设置")]
    [SerializeField] private float fadeDuration = 1.5f;         // 淡入时间
    [SerializeField] private float stayDuration = 0.5f;         // 淡入后停留时间
    [SerializeField] private bool useFadeEffect = true;         // 是否使用淡入

    private bool isTransitioning = false;

    void Start()
    {
        // 初始化照片透明
        if (fadeImage != null)
        {
            SetAlpha(fadeImage, 0f);
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false;  // 不阻挡点击
        }

        // 绑定按钮
        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpButtonClicked);

        // 自动跳转（调试用，平时关掉）
        if (autoLoadOnStart)
            StartCoroutine(DoJump());
    }

    void OnDestroy()
    {
        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpButtonClicked);
    }

    /// <summary>
    /// 按钮点击回调
    /// </summary>
    public void OnJumpButtonClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(DoJump());
    }

    /// <summary>
    /// 执行跳转
    /// </summary>
    private IEnumerator DoJump()
    {
        isTransitioning = true;
        if (jumpButton != null) jumpButton.interactable = false;

        // 检查场景是否有效
        if (targetSceneIndex < 0 || targetSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            ShowHint($"场景索引 {targetSceneIndex} 无效！请检查 Build Settings。");
            isTransitioning = false;
            if (jumpButton != null) jumpButton.interactable = true;
            yield break;
        }

        // 淡入效果
        if (useFadeEffect && fadeImage != null)
        {
            yield return StartCoroutine(FadeIn());
            yield return new WaitForSeconds(stayDuration);
        }

        // 跳转场景
        SceneManager.LoadScene(targetSceneIndex);
    }

    /// <summary>
    /// 照片从透明(0)淡入到不透明(1)
    /// </summary>
    private IEnumerator FadeIn()
    {
        Color color = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private void ShowHint(string msg)
    {
        if (hintText != null)
        {
            hintText.text = msg;
            CancelInvoke(nameof(ClearHint));
            Invoke(nameof(ClearHint), 3f);
        }
        else
        {
            Debug.LogWarning(msg);
        }
    }

    private void ClearHint() { if (hintText != null) hintText.text = ""; }
}
