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
    public Vector2 Y_Limit = new Vector2(-4.8f, 2);
    
    public Animator animator;
    public bool isLand = false;

    // 持续粒子控制
    public bool IsBeingEaten = false;
    public long ParticleTimer = 0;

    // ========== 新增：Heaven 音效控制 ==========
    private long _heavenStopTimer = 0;
    private const long HeavenDuration = 4000; // 5秒后暂停

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
        
        // ========== 下落时播放 Heaven，5秒后停 ==========
        PlayHeaven();
        
        shit.transform.DOMoveY(Y_Limit.x, dura);
        await FTask.Wait(Scene, (long)(dura * 1000));
        
        animator.SetTrigger("Land");
        isLand = true;
    }

    // ========== 新增：播放 Heaven，5秒后自动停 ==========
    private void PlayHeaven()
    {
        if (shit == null) return;
        
        var audioMgr = Scene.GetComponent<AudioManagerComponent>();
        if (audioMgr != null)
        {
            audioMgr.Play(SFXType.Heaven, shit.transform.position).Coroutine();
        }
        
        // 5秒后停止
        _heavenStopTimer = Scene.TimerComponent.Net.OnceTimer(HeavenDuration, StopHeaven);
    }

    private void StopHeaven()
    {
        Scene.TimerComponent.Net.Remove(ref _heavenStopTimer);
        Scene.GetComponent<AudioManagerComponent>()?.StopHeaven();
    }

    // 开始吃屎时调用
    public void StartEat()
    {
        IsBeingEaten = true;
        StartContinuousParticles();
    }

    // 取消吃屎时调用
    public void CancelEat()
    {
        IsBeingEaten = false;
        StopParticles();
    }

    // 吃完时调用
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
        
        Color shitColor = new Color(0.4f, 0.25f, 0.1f);
        particleEffect.SpawnEffect(shit.transform.position, shitColor);
    }
    
    public void RemoveShit()
    {
        StopParticles();
        Scene.TimerComponent.Net.Remove(ref _heavenStopTimer); // 清理定时器
        
        if (shit != null)
        {
            GameObject.Destroy(shit);
            shit = null;
            animator = null;
        }
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