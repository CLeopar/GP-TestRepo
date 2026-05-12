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
        //TODO:掉落动画结束
        animator.SetTrigger("Land");
        isLand = true;
    }

    public void RemoveShit()
    {
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