using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class BouncingParticle : MonoBehaviour
{
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _lifetime;
    private float _elapsed;
    private Color _color;
    private float _minX, _maxX, _minY, _maxY;
    
    public bool IsInitialized { get; private set; } = false;  // 防止重复初始化

    [Header("Physics")]
    public float bounciness = 0.3f;      // 降低弹性
    public float rotationSpeed = 90f;     // 降低旋转

    [Header("Size Range")]
    public float minSize = 0.15f;        // 更小
    public float maxSize = 0.3f;        // 更小

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        
        // 初始状态：静止，等待Init
        _rb.gravityScale = 0f;
        _rb.velocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
            _minX = bl.x; _maxX = tr.x;
            _minY = bl.y; _maxY = tr.y;
        }
    }

    public void Init(Vector2 velocity, float lifetime, Color color)
    {
        if (IsInitialized) return;  // 防止重复调用
        
        IsInitialized = true;
        _lifetime = lifetime;
        _color = color;
        _sr.color = color;

        // 设置物理
        _rb.gravityScale = 1f;  // 启用重力
        _rb.velocity = velocity;
        _rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);

        // 设置大小
        float s = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void Update()
    {
        if (!IsInitialized) return;  // 未初始化不更新
        
        _elapsed += Time.deltaTime;
        float t = _elapsed / _lifetime;

        // 淡出
        float alpha = t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f;
        _sr.color = new Color(_color.r, _color.g, _color.b, alpha);

        if (_elapsed >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 边界反弹（无Collider版本）
        Vector2 pos = transform.position;
        Vector2 vel = _rb.velocity;
        float r = transform.localScale.x * 0.5f;

        if (pos.x - r < _minX) { pos.x = _minX + r; vel.x = Mathf.Abs(vel.x) * bounciness; }
        if (pos.x + r > _maxX) { pos.x = _maxX - r; vel.x = -Mathf.Abs(vel.x) * bounciness; }
        if (pos.y - r < _minY) { pos.y = _minY + r; vel.y = Mathf.Abs(vel.y) * bounciness; }
        if (pos.y + r > _maxY) { pos.y = _maxY - r; vel.y = -Mathf.Abs(vel.y) * bounciness; }

        transform.position = pos;
        _rb.velocity = vel;
    }
}