using UnityEngine;

/// <summary>
/// 食物吃完粒子生成器 — 事件驱动，不需要点击检测
/// </summary>
public class EatParticleSpawner : MonoBehaviour
{
    [Header("Particle Settings")]
    public GameObject particlePrefab;
    public int count = 20;
    public float minSpeed = 200f;
    public float maxSpeed = 500f;
    public float lifetime = 3f;

    [Header("Color Settings")]
    public bool useManualColor = false;
    public Color manualColor = Color.white;

    /// <summary>
    /// 在指定位置生成粒子爆发
    /// </summary>
    public void SpawnBurst(Vector3 origin)
    {
        Color particleColor = useManualColor ? manualColor : Color.white;

        for (int i = 0; i < count; i++)
        {
            float baseAngle = (i / (float)count) * 360f * Mathf.Deg2Rad;
            float jitter = Random.Range(-0.5f, 0.5f) * (360f / count) * Mathf.Deg2Rad * 0.5f;
            float angle = baseAngle + jitter;

            // 限制向上速度，粒子主要向四周和下方散开
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dir.y = Mathf.Clamp(dir.y, -1f, 0.4f);
            dir.Normalize();

            float speed = minSpeed + Random.Range(0f, maxSpeed - minSpeed);

            GameObject p = Instantiate(particlePrefab, origin, Quaternion.identity);
            BouncingParticle bp = p.GetComponent<BouncingParticle>();
            if (bp != null)
                bp.Init(dir * speed, lifetime, particleColor);
        }
    }
}