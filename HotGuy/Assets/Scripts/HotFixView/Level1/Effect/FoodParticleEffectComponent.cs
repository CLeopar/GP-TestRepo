using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class FoodParticleEffectComponent : Entity
{
    public GameObject ParticlePrefab;

    public void SpawnEffect(Vector3 position, Color color, float spawnRadius = 0.1f)
    {
        if (ParticlePrefab == null) return;
    
        int count = 5;
        float minSpeed = 5f;
        float maxSpeed = 10f;
        float lifetime = 0.8f;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dir.y = Mathf.Clamp(dir.y, -0.3f, 0.3f);
            dir.Normalize();

            float speed = Random.Range(minSpeed, maxSpeed);

            // ========== 修复：在 spawnRadius 范围内随机位置生成 ==========
            Vector2 offset = dir * Random.Range(0f, spawnRadius);
            Vector3 spawnPos = position + new Vector3(offset.x, offset.y, 0);

            GameObject p = Object.Instantiate(ParticlePrefab, spawnPos, Quaternion.identity);
            p.layer = LayerMask.NameToLayer("Particles");

            var sr = p.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Effects";  // 或 "Particles"
                sr.sortingOrder = 200;             // 比狗高
            }
        
            var bp = p.GetComponent<BouncingParticle>();
            if (bp != null && !bp.IsInitialized)
                bp.Init(dir * speed, lifetime, color);
        }
    }
}


public class FoodParticleEffectComponent_Awake : AwakeSystem<FoodParticleEffectComponent>
{
    protected override void Awake(FoodParticleEffectComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        self.ParticlePrefab = rc.Get<GameObject>("L1_FoodParticle");
    }
}