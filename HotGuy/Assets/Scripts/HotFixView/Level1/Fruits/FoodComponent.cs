using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using DG.Tweening;

public class FoodComponent : Entity, ISupportedMultiEntity
{
    public Transform Fruit_Tr { get; set; }
    public GameObject Fruit_Go { get; set; }
    public Rigidbody2D rigidBody2D;
    public Collider2D collider2D;
    public Transform parent;
    public SpriteRenderer spriteRenderer;

    public FoodType foodType;
    public FruitStateType fruitStateType = FruitStateType.Normal;
    public FCancellationToken CancellationToken;
    public bool isInPickUp { get; set; } = false;
    public List<GameObject> stateGameObjects = new List<GameObject>();

    public bool isStayHands { get; set; } = false;

    // 持续粒子控制
    public bool IsBeingEaten = false;
    public long ParticleTimer = 0;

    public async FTask Init(Transform fruitParent, FoodType fruitTypes)
    {
        foodType = fruitTypes;
        await LoadItem(fruitParent);
    }

    private async FTask LoadItem(Transform fruitParent)
    {
        var foodConfig = Scene.GetComponent<Tables>().FoodConfigCategory.Get(foodType);
        var bundle = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<GameObject>(foodConfig.UIResName);
        Fruit_Go = GameObject.Instantiate(bundle, fruitParent);
        Fruit_Tr = Fruit_Go.transform;
        Fruit_Tr.localPosition = Vector3.zero;
        Fruit_Go.name = Id.ToString();

        rigidBody2D = Fruit_Tr.GetComponent<Rigidbody2D>();
        collider2D = Fruit_Tr.GetComponent<Collider2D>();

        for (int i = 0; i < foodConfig.FoodStateCount; i++)
        {
            var child = Fruit_Tr.GetChild(i).gameObject;
            stateGameObjects.Add(child);
            if (i > 0)
                child.SetActive(false);
        }

        var unityEventTrigger_Palm = Fruit_Tr.GetComponent<UnityEventTrigger>();
        unityEventTrigger_Palm.Register(action_OnTriggerEnter2D: OnColliderEnter2D, action_OnTriggerExit2D: OnColliderExit2D);
    }

    public void OnColliderEnter2D(Collider2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "HandsUp")
        {
            isStayHands = true;
            Log.Error("isStayHands true");
        }
    }

    public void OnColliderExit2D(Collider2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "HandsUp")
        {
            isStayHands = false;
            Log.Error("isStayHands false");
        }
    }

    public void OnCollisionEnter2D(Collision2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        Log.Error($"enter Layer {layer}");
        if (layer == "Props")
        {
            isStayHands = true;
            Log.Error("1111");
        }
    }

    public void OnCollisionExit2D(Collision2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        Log.Error($"exit Layer {layer}");
        if (layer == "Props")
        {
            isStayHands = false;
            Log.Error("2222");
        }
    }

    public void PickUp(Transform parent)
    {
        collider2D.enabled = false;
        rigidBody2D.gravityScale = 0;
        ChangeSpriteSortOrder(101);
        Fruit_Tr.SetParent(parent);
        Fruit_Tr.localPosition = Vector3.zero;
        rigidBody2D.velocity = Vector2.zero;
        rigidBody2D.angularVelocity = 0;

        isInPickUp = true;
    }

    public void Drop()
    {
        Fruit_Tr.SetParent(parent);
        rigidBody2D.gravityScale = 1;
        collider2D.enabled = true;
        ChangeSpriteSortOrder(0);

        isInPickUp = false;
        
        // 确保放下时停止抖动和粒子
        StopShake();
        StopParticles();
    }

    public async FTask StartEat(bool isNormal)
    {
        Log.Error($"[Food] StartEat fruitStateType={fruitStateType}, IsBeingEaten={IsBeingEaten}");
        
        IsBeingEaten = true;
        CancellationToken = FCancellationToken.ToKen;
        
     
        
        // 启动持续粒子 + 抖动
        StartContinuousParticles();
        
        var duration = Scene.GetComponent<Tables>().ConstConfigCategory.FoodChangeStateInterval;

        if (fruitStateType == FruitStateType.Normal)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel) { StopAllEffects(); return; }
            ShowState(1, isNormal);
        }

        if (fruitStateType == FruitStateType.Eat_2)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel) { StopAllEffects(); return; }
            ShowState(2, isNormal);
        }

        if (fruitStateType == FruitStateType.Eat_3)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel) { StopAllEffects(); return; }
            ShowState(3, isNormal);
        }
        
        StopAllEffects();
    }

    public void CancelEat()
    {
        CancellationToken?.Cancel();
        StopAllEffects();  // ← 统一停止所有效果
    }

    /// <summary>
    /// 统一停止所有效果（抖动 + 粒子）
    /// </summary>
    private void StopAllEffects()
    {
        StopShake();
        StopParticles();
    }

    /// <summary>
    /// 启动持续粒子
    /// </summary>
    private void StartContinuousParticles()
    {
        StartShake();
        ParticleTimer = Scene.TimerComponent.Net.RepeatedTimer(200, () =>
        {
            if (!IsBeingEaten) return;
            SpawnParticles();
        });
    }

    private Tween _shakeTween;

    private void StartShake()
    {
        // 先停止旧的，防止叠加
        StopShake();
        
        foreach (var go in stateGameObjects)
        {
            if (!go.activeSelf) continue;
            var tr = go.transform;
            
            // 确保从原点开始
            tr.localPosition = Vector3.zero;
            
            _shakeTween = DOTween.Sequence()
                .Append(tr.DOLocalMove(new Vector3(0.2f, 0.15f, 0), 0.1f).SetRelative(true))
                .Append(tr.DOLocalMove(new Vector3(-0.2f, -0.15f, 0), 0.1f).SetRelative(true))
                .SetLoops(-1, LoopType.Restart);
            break;
        }
    }

    private void StopShake()
    {
        if (_shakeTween != null)
        {
            _shakeTween.Kill();
            _shakeTween = null;
        }
        
        // 复位所有 stateGameObjects，杀掉残留 tween
        foreach (var go in stateGameObjects)
        {
            if (go != null)
            {
                DOTween.Kill(go.transform);
                go.transform.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 停止粒子
    /// </summary>
    private void StopParticles()
    {
        IsBeingEaten = false;
        Scene.TimerComponent.Net.Remove(ref ParticleTimer);
    }
    
    /// <summary>
    /// 单次粒子释放
    /// </summary>
    private void SpawnParticles()
    {
        var particleEffect = Scene.GetComponent<FoodParticleEffectComponent>();
        if (particleEffect == null) return;
        
        Color foodColor = GetCurrentFoodColor();
        particleEffect.SpawnEffect(Fruit_Tr.position, foodColor);
    }

    /// <summary>
    /// 获取食物颜色（硬编码）
    /// </summary>
    private Color GetCurrentFoodColor()
    {
        return foodType switch
        {
            FoodType.Cucumber     => new Color(0.4f, 0.8f, 0.2f),
            FoodType.Pumpkin      => new Color(1.0f, 1.0f, 1.0f),
            FoodType.ChickenLeg   => new Color(0.9f, 0.5f, 0.2f),
            FoodType.Apple        => new Color(0.9f, 0.2f, 0.2f),
            FoodType.Broccoli     => new Color(0.2f, 0.6f, 0.2f),
            FoodType.Biscuit      => new Color(0.9f, 0.7f, 0.4f),
            FoodType.Blueberry_A  => new Color(0.3f, 0.4f, 0.8f),
            FoodType.Blueberry_B  => new Color(0.3f, 0.4f, 0.8f),
            FoodType.Blueberry_C  => new Color(0.3f, 0.4f, 0.8f),
            FoodType.Blueberry_D  => new Color(0.3f, 0.4f, 0.8f),
            FoodType.Carrot       => new Color(1.0f, 0.5f, 0.1f),
            FoodType.Egg          => new Color(1.0f, 0.9f, 0.7f),
            FoodType.Salmon       => new Color(1.0f, 0.6f, 0.5f),
            FoodType.Shrimp       => new Color(1.0f, 0.7f, 0.6f),
            _                     => Color.white
        };
    }

    public void ShowState(int idx, bool isNormal)
    {
        Log.Error($"ShowState {idx}, {stateGameObjects.Count}");
        for (int i = 0; i < stateGameObjects.Count; i++)
        {
            stateGameObjects[i].SetActive(i == idx);
        }

        if (idx >= stateGameObjects.Count - 1)
        {
            // 吃完后停止所有效果
            StopAllEffects();
            
            if (isNormal)
            {
                Scene.EventComponent.Publish(new FoodBeEaten_Normal
                {
                    fruitId = Id
                });
            }
            else
            {
                Scene.EventComponent.Publish(new FoodBeEaten_Secretly
                {
                    fruitId = Id
                });
            }

            var foodConfig = Scene.GetComponent<Tables>().FoodConfigCategory.Get(foodType);
            if (foodConfig.HasReamin)
            {
                fruitStateType = FruitStateType.BeEaten;
                GetParent<FoodManagerComponent>().AddNewFruit().Coroutine();
            }
            else
                GetParent<FoodManagerComponent>().RemoveAndAddNewFruit(this).Coroutine();
        }
        else
            fruitStateType = (FruitStateType)idx;
    }

    public void ChangeSpriteSortOrder(int order)
    {
        if (stateGameObjects.Count > 0)
        {
            for (int i = 0; i < stateGameObjects.Count; i++)
            {
                stateGameObjects[i].GetComponent<SpriteRenderer>().sortingOrder = order;
            }
        }
        else
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    public UnityEngine.Vector3 GetPosition()
    {
        return Fruit_Tr.position;
    }

    public void AddForce(UnityEngine.Vector3 force)
    {
    }
}

public class FruitComponent_Destroy : DestroySystem<FoodComponent>
{
    protected override void Destroy(FoodComponent self)
    {
        if (self.Fruit_Go != null)
            GameObject.Destroy(self.Fruit_Go);
    }
}