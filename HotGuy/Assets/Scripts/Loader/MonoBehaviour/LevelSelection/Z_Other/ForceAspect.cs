using UnityEngine;

public class ForceAspect : MonoBehaviour
{
    // 目标宽高比，16:9 = 1.777...
    public float targetAspect = 16f / 9f;

    private int lastWidth;
    private int lastHeight;
    private Rect defaultRect; // 保存默认的 camera.rect

    void Start()
    {
        // 保存摄像机的默认 rect
        Camera camera = GetComponent<Camera>();
        if (camera != null)
        {
            defaultRect = camera.rect;
        }

        ApplyLetterbox();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        // **只在运行时执行**
        if (!Application.isPlaying) return;

        // 只在窗口大小改变时更新
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            ApplyLetterbox();
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }
    }

    void ApplyLetterbox()
    {
        Camera camera = GetComponent<Camera>();
        if (camera == null) return;

        // **非运行模式下恢复默认 rect 并退出**
        if (!Application.isPlaying)
        {
            camera.rect = defaultRect;
            return;
        }

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect = camera.rect;

        if (scaleHeight < 1f)  // 屏幕比16:9更窄（上下黑边）
        {
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1f - scaleHeight) / 2f;
        }
        else  // 屏幕比16:9更宽（左右黑边）
        {
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) / 2f;
            rect.y = 0;
        }

        camera.rect = rect;
    }

    // 当脚本被禁用或销毁时恢复默认 rect
    void OnDisable()
    {
        if (Camera.main != null)
        {
            Camera.main.rect = defaultRect;
        }
    }

    void OnDestroy()
    {
        if (Camera.main != null)
        {
            Camera.main.rect = defaultRect;
        }
    }
}