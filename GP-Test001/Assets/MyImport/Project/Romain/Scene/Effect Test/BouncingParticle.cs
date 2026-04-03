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
    public float bounciness = 0.65f;
    public float rotationSpeed = 180f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        _rb.gravityScale = 2f;
        _rb.angularDrag = 0.5f;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 10f));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 10f));
        _minX = bl.x; _maxX = tr.x;
        _minY = bl.y; _maxY = tr.y;
    }

    public void Init(Vector2 velocity, float lifetime, Color color)
    {
        _rb.velocity = velocity;
        _rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
        _lifetime = lifetime;
        _color = color;
        _sr.color = color;

        float s = Random.Range(0.1f, 0.25f);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _lifetime;

        float alpha = t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f;
        _sr.color = new Color(_color.r, _color.g, _color.b, alpha);

        if (_elapsed >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 pos = _rb.position;
        Vector2 vel = _rb.velocity;
        float r = transform.localScale.x * 0.5f;

        if (pos.x - r < _minX) { pos.x = _minX + r; vel.x =  Mathf.Abs(vel.x) * bounciness; }
        if (pos.x + r > _maxX) { pos.x = _maxX - r; vel.x = -Mathf.Abs(vel.x) * bounciness; }
        if (pos.y - r < _minY) { pos.y = _minY + r; vel.y =  Mathf.Abs(vel.y) * bounciness; }
        if (pos.y + r > _maxY) { pos.y = _maxY - r; vel.y = -Mathf.Abs(vel.y) * bounciness; }

        _rb.position = pos;
        _rb.velocity = vel;
    }
}