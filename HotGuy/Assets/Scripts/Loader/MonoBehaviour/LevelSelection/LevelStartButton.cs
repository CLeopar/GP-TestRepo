using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelStartButton : MonoBehaviour
{
    [Header("目标场景索引")]
    [SerializeField] private int targetSceneIndex = 2;

    [Header("渐黑遮罩 (2D)")]
    [SerializeField] private SpriteRenderer fadeRenderer;      // 改为 SpriteRenderer
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("层级")]
    [SerializeField] private LayerMask Clickable;

    private bool isLoading = false;

    private void Awake()
    {
        // 初始化时确保遮罩完全透明
        if (fadeRenderer != null)
        {
            Color c = fadeRenderer.color;
            c.a = 0f;
            fadeRenderer.color = c;
        }
    }

    private void Update()
    {
        if (isLoading) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, Clickable);

        if (hit != null && hit.gameObject == gameObject)
        {
            Debug.Log("点击触发：" + gameObject.name);
            StartLoad();
        }
    }

    private void StartLoad()
    {
        isLoading = true;

        // 如果没有设置遮罩，直接加载场景
        if (fadeRenderer == null)
        {
            SceneManager.LoadScene(targetSceneIndex);
            return;
        }

        // 使用 DOFade 对 SpriteRenderer 的 alpha 做过渡
        fadeRenderer.DOFade(1f, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => SceneManager.LoadScene(targetSceneIndex));
    }
}