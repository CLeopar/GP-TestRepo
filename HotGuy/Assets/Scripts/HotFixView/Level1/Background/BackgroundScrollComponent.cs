using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class BackgroundScrollComponent : Entity
{
    [Header("渲染设置")]
    public SpriteRenderer SpriteRenderer;
    
    [Header("滚动配置")]
    public float ScrollSpeed = 5f;
    public float ScrollAngle = 45f;
    
    [Header("尺寸设置")]
    public float CoverageWidth = 42.5f;
    public float CoverageHeight = 30.3f;
    
    private Vector2 _scrollDirection;
    private Vector2 _currentOffset;
    private MaterialPropertyBlock _propBlock;
    private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
    
    public void Init()
    {
        if (SpriteRenderer == null)
        {
            Log.Error("[BackgroundScroll] SpriteRenderer is null!");
            return;
        }
        
        SpriteRenderer.drawMode = SpriteDrawMode.Tiled;
        SpriteRenderer.size = new Vector2(CoverageWidth, CoverageHeight);
        
        _propBlock = new MaterialPropertyBlock();
        
        float rad = ScrollAngle * Mathf.Deg2Rad;
        _scrollDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        
        Log.Error($"[BackgroundScroll] Init. Direction: {_scrollDirection}");
    }
    
    public void UpdateScroll(float deltaTime)
    {
        if (SpriteRenderer == null || _propBlock == null) return;
        
        _currentOffset += _scrollDirection * ScrollSpeed * deltaTime;
        
        float offsetX = Mathf.Repeat(_currentOffset.x, 1f);
        float offsetY = Mathf.Repeat(_currentOffset.y, 1f);
        
        SpriteRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetVector(MainTexStId, new Vector4(1f, 1f, offsetX, offsetY));
        SpriteRenderer.SetPropertyBlock(_propBlock);
    }
    
    public Vector2 GetOffset() => _currentOffset;  // ← 就是这行
    
    public void SetSpeed(float speed) => ScrollSpeed = speed;
    
    public void SetAngle(float angle)
    {
        ScrollAngle = angle;
        float rad = angle * Mathf.Deg2Rad;
        _scrollDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
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
            }
        }
        self.Init();
    }
}