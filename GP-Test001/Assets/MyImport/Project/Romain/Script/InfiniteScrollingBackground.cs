using UnityEngine;
using UnityEngine.UI;

public class InfiniteScrollingBackground : MonoBehaviour
{
    [Header("滚动设置")]
    [Tooltip("滚动的速度向量 (x: 水平速度, y: 垂直速度)")]
    public Vector2 scrollSpeed = new Vector2(0.5f, 0.3f);
    
    [Header("贴图设置")]
    [Tooltip("使用的四方连续贴图")]
    public Texture2D sourceTexture;
    
    [Tooltip("贴图在屏幕上显示的大小（像素单位）")]
    public Vector2 tileSize = new Vector2(512, 512);
    
    [Header("画布适配")]
    [Tooltip("是否自动适配画布大小（勾选后会在画布上平铺显示，不勾选则只显示一个贴图大小）")]
    public bool fillCanvas = true;
    
    // 内部组件引用
    private RawImage rawImage;
    private RectTransform rectTransform;
    private Canvas canvas;
    
    // 用于滚动的UV偏移量
    private Vector2 uvOffset = Vector2.zero;
    
    void Awake()
    {
        // 获取或添加必要的组件
        rawImage = GetComponent<RawImage>();
        if (rawImage == null)
        {
            rawImage = gameObject.AddComponent<RawImage>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        
        canvas = GetComponentInParent<Canvas>();
    }
    
    void Start()
    {
        // 检查必要组件
        if (sourceTexture == null)
        {
            Debug.LogError("未设置四方连续贴图！请在Inspector中指定sourceTexture。");
            enabled = false;
            return;
        }
        
        if (canvas == null)
        {
            Debug.LogError("该对象必须位于Canvas下！");
            enabled = false;
            return;
        }
        
        // 设置纹理的Wrap Mode为Repeat
        sourceTexture.wrapMode = TextureWrapMode.Repeat;
        
        // 设置RawImage的贴图
        rawImage.texture = sourceTexture;
        
        // 设置显示区域
        SetupDisplayArea();
        
        // 初始化UV偏移
        uvOffset = Vector2.zero;
        UpdateUVOffset();
    }
    
    void SetupDisplayArea()
    {
        if (fillCanvas)
        {
            // 铺满整个画布
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 计算UV平铺次数
            Rect canvasRect = canvas.pixelRect;
            float tileX = canvasRect.width / tileSize.x;
            float tileY = canvasRect.height / tileSize.y;
            
            // 设置UV平铺
            Rect uvRect = new Rect(0, 0, tileX, tileY);
            rawImage.uvRect = uvRect;
        }
        else
        {
            // 不铺满，只显示一个贴图大小
            rectTransform.sizeDelta = tileSize;
            
            // 设置中心锚点
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // 不进行UV平铺，只显示一个贴图
            rawImage.uvRect = new Rect(0, 0, 1, 1);
        }
    }
    
    void Update()
    {
        if (rawImage == null || sourceTexture == null)
            return;
        
        // 更新UV偏移量
        uvOffset += scrollSpeed * Time.deltaTime;
        UpdateUVOffset();
    }
    
    void UpdateUVOffset()
    {
        if (fillCanvas)
        {
            // 铺满模式下，直接偏移UV坐标
            Rect currentUVRect = rawImage.uvRect;
            currentUVRect.position = uvOffset;
            rawImage.uvRect = currentUVRect;
        }
        else
        {
            // 非铺满模式下，偏移UV坐标实现滚动
            Rect currentUVRect = new Rect(uvOffset.x, uvOffset.y, 1, 1);
            rawImage.uvRect = currentUVRect;
        }
    }
    
    // 公共方法：设置贴图大小
    public void SetTileSize(Vector2 newSize)
    {
        tileSize = newSize;
        SetupDisplayArea();
        ResetScrollPosition();
    }
    
    // 公共方法：切换铺满模式
    public void SetFillCanvas(bool fill)
    {
        fillCanvas = fill;
        SetupDisplayArea();
        ResetScrollPosition();
    }
    
    // 公共方法：改变滚动速度
    public void SetScrollSpeed(Vector2 newSpeed)
    {
        scrollSpeed = newSpeed;
    }
    
    public void SetScrollSpeed(float xSpeed, float ySpeed)
    {
        scrollSpeed = new Vector2(xSpeed, ySpeed);
    }
    
    // 公共方法：重置滚动位置
    public void ResetScrollPosition()
    {
        uvOffset = Vector2.zero;
        UpdateUVOffset();
    }
    
    // 公共方法：更换贴图
    public void ChangeTexture(Texture2D newTexture)
    {
        if (newTexture == null)
            return;
            
        sourceTexture = newTexture;
        sourceTexture.wrapMode = TextureWrapMode.Repeat;
        rawImage.texture = sourceTexture;
        ResetScrollPosition();
    }
    
    // 编辑器调试
    void OnValidate()
    {
        if (sourceTexture != null && rawImage != null)
        {
            rawImage.texture = sourceTexture;
        }
    }
}