using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class BackgroundScrollComponent : Entity
{
    [Header("渲染设置")]
    public SpriteRenderer SpriteRenderer;
    
    [Header("滚动配置")]
    public float ScrollSpeed = 2f;
    public float ScrollAngle = 45f;
    
    [Header("瓦片尺寸")]
    public float TileSize = 10f;
    
    [Header("尺寸设置")]
    [Tooltip("瓦片缩放比例（0.5 = 半个原始大小）")]
    public float TileScale = 0.5f;
    [Tooltip("覆盖宽度（世界单位）")]
    public float CoverageWidth = 42.5f;
    [Tooltip("覆盖高度（世界单位）")]
    public float CoverageHeight = 30.3f;
    
    private Vector2 _scrollDirection;
    private Vector2 _currentOffset;
    private Material _material;
    private Texture _sourceTexture;
    
    public void Init()
    {
        if (SpriteRenderer == null)
        {
            Log.Error("[BackgroundScroll] SpriteRenderer is null!");
            return;
        }
        
        // 获取纹理
        if (SpriteRenderer.sprite != null)
        {
            _sourceTexture = SpriteRenderer.sprite.texture;
            Log.Error($"[BackgroundScroll] Got texture from sprite: {_sourceTexture.name}");
        }
        else
        {
            _sourceTexture = SpriteRenderer.sharedMaterial?.mainTexture;
            Log.Error($"[BackgroundScroll] Sprite is null, using material texture: {_sourceTexture != null}");
        }
        
        if (_sourceTexture == null)
        {
            Log.Error("[BackgroundScroll] No texture found! Please assign a sprite in Inspector.");
            return;
        }
        
        // 设置纹理 Repeat
        _sourceTexture.wrapMode = TextureWrapMode.Repeat;
        
        // 创建支持 UV 偏移的材质
        _material = new Material(Shader.Find("Unlit/Texture"));
        _material.mainTexture = _sourceTexture;
        SpriteRenderer.material = _material;
        
        Log.Error($"[BackgroundScroll] Material ready. Shader: {_material.shader.name}, Texture: {_material.mainTexture != null}");
        
        // 计算滚动方向
        float rad = ScrollAngle * Mathf.Deg2Rad;
        _scrollDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        
        // 关键：设置 Draw Mode 为 Tiled
        SpriteRenderer.drawMode = SpriteDrawMode.Tiled;
        
        // 应用尺寸
        ApplySizeAndScale();
    }
    
    public void UpdateScroll(float deltaTime)
    {
        if (_material == null) return;
        
        _currentOffset += _scrollDirection * (ScrollSpeed * deltaTime / TileSize);
        _currentOffset.x = Mathf.Repeat(_currentOffset.x, 1f);
        _currentOffset.y = Mathf.Repeat(_currentOffset.y, 1f);
        
        _material.mainTextureOffset = _currentOffset;
    }
    
    private void ApplySizeAndScale()
    {
        if (SpriteRenderer == null) return;
        
        // 设置 scale（控制单个瓦片大小）
        SpriteRenderer.transform.localScale = new Vector3(TileScale, TileScale, TileScale);
        
        // 设置 size（控制总覆盖范围，Tiled 模式下会重复平铺）
        SpriteRenderer.size = new Vector2(CoverageWidth, CoverageHeight);
        
        Log.Error($"[BackgroundScroll] Scale: {TileScale}, Size: {CoverageWidth}x{CoverageHeight}");
    }
    
    public Vector2 GetOffset() => _currentOffset;
    
    public void SetSpeed(float speed) => ScrollSpeed = speed;
    
    public void SetAngle(float angle)
    {
        ScrollAngle = angle;
        float rad = angle * Mathf.Deg2Rad;
        _scrollDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
    
    // 运行时修改尺寸
    public void SetTileScale(float scale)
    {
        TileScale = scale;
        ApplySizeAndScale();
    }
    
    public void SetCoverage(float width, float height)
    {
        CoverageWidth = width;
        CoverageHeight = height;
        ApplySizeAndScale();
    }
}

public class BackgroundScrollComponent_Awake : AwakeSystem<BackgroundScrollComponent>
{
    protected override void Awake(BackgroundScrollComponent self)
    {
        var rc = GameObject.Find("Level_1")?.GetComponent<ReferenceCollector>();
        if (rc != null)
        {
            var bgObj = rc.Get<GameObject>("Background_Scroll");
            if (bgObj != null)
            {
                self.SpriteRenderer = bgObj.GetComponent<SpriteRenderer>();
                Log.Error($"[BackgroundScroll] Found object: {bgObj.name}, SpriteRenderer: {self.SpriteRenderer != null}");
            }
            else
            {
                Log.Error("[BackgroundScroll] Background_Scroll not found in ReferenceCollector!");
            }
        }
        
        self.Init();
    }
}

public class BackgroundScrollComponent_Destroy : DestroySystem<BackgroundScrollComponent>
{
    protected override void Destroy(BackgroundScrollComponent self)
    {
        if (self.SpriteRenderer != null && self.SpriteRenderer.material != null)
        {
            Object.Destroy(self.SpriteRenderer.material);
        }
    }
}