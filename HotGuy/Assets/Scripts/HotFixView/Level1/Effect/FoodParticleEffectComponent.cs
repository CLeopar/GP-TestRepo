using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class FoodParticleEffectComponent : Entity
{
    public GameObject ParticlePrefab;

    public void SpawnEffect(Vector3 position, Color color)
    {
        if (ParticlePrefab == null) return;
    
        // 力度大幅调小
        int count = 3;              // 粒子数量减少
        float minSpeed = 5f;       // 速度降低
        float maxSpeed = 10f;       // 速度降低
        float lifetime = 0.8f;      // 生命周期缩短

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            dir.y = Mathf.Clamp(dir.y, -0.3f, 0.3f);  // 更平缓
            dir.Normalize();

            float speed = Random.Range(minSpeed, maxSpeed);

            GameObject p = Object.Instantiate(ParticlePrefab, position, Quaternion.identity);
            p.layer = LayerMask.NameToLayer("Particles");
        
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