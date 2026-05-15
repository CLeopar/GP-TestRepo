using DG.Tweening;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class ShitComponent : Entity
{
    public Transform shitParent;
    public GameObject shit = null;
    public Vector2 X_Limit = new Vector2(-0.6f, 6.37f);
    public Vector2 Y_Limit = new Vector2(-4.44f, 2);
    
    public Animator animator;
    public bool isLand = false;

    // ========== 新增：持续粒子控制 ==========
    public bool IsBeingEaten = false;
    public long ParticleTimer = 0;

    public void Init()
    {
        shitParent = GameObject.Find("Level_1").transform;
        LoadShit().Coroutine();
    }

    public async FTask LoadShit()
    {
        var bundle = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<GameObject>("L1_Shit");
        shit = GameObject.Instantiate(bundle, shitParent);
        animator = shit.GetComponentInChildren<Animator>();
        var posX = Random.Range(X_Limit.x, X_Limit.y);
        shit.transform.localPosition = new Vector3(posX, Y_Limit.y);
        var dura = Scene.GetComponent<Tables>().ConstConfigCategory.ShitMoveYTime;
        shit.transform.DOMoveY(Y_Limit.x, dura);
        await FTask.Wait(Scene, (long)(dura * 1000));
        animator.SetTrigger("Land");
        isLand = true;
    }

    // ========== 新增：开始吃屎时调用 ==========
    public void StartEat()
    {
        IsBeingEaten = true;
        StartContinuousParticles();
    }

    // ========== 新增：取消吃屎时调用 ==========
    public void CancelEat()
    {
        IsBeingEaten = false;
        StopParticles();
    }

    // ========== 新增：吃完时调用 ==========
    public void FinishEat()
    {
        IsBeingEaten = false;
        StopParticles();
    }

    private void StartContinuousParticles()
    {
        ParticleTimer = Scene.TimerComponent.Net.RepeatedTimer(200, () =>
        {
            if (!IsBeingEaten) return;
            SpawnParticles();
        });
    }

    private void StopParticles()
    {
        IsBeingEaten = false;
        Scene.TimerComponent.Net.Remove(ref ParticleTimer);
    }

    private void SpawnParticles()
    {
        var particleEffect = Scene.GetComponent<FoodParticleEffectComponent>();
        if (particleEffect == null) return;
        
        // 屎的颜色 - 棕色
        Color shitColor = new Color(0.4f, 0.25f, 0.1f);
        
        particleEffect.SpawnEffect(shit.transform.position, shitColor);
    }

    public void RemoveShit()
    {
        if (shit != null)
        {
            GameObject.Destroy(shit);
            shit = null;
            animator = null;
        }
        StopParticles();
    }
}

public class ShitComponent_Awake : AwakeSystem<ShitComponent>
{
    protected override void Awake(ShitComponent self)
    {
        self.Init();
    }
}

public class ShitComponent_Destroy : DestroySystem<ShitComponent>
{
    protected override void Destroy(ShitComponent self)
    {
        self.RemoveShit();
    }
}