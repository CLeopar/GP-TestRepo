using System;
using System.Collections.Generic;
using DG.Tweening;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using Random = UnityEngine.Random;

public class FoodManagerComponent : Entity
{
    public Transform FruitParent;
    public Transform FeedMachine;
    public Vector3 endPos = new Vector3(8.88f, 3.32f, 0);
    public Vector3 startPos = new Vector3(13.7f, 3.32f, 0);
    public int maxFruitCount = 7;
    public Dictionary<FoodType, int> foodCount = new Dictionary<FoodType, int>();

    public int OnFeedMachineFruitsCount = 0;

    public long Timer_EatShit;
    public long Timer_BornShit;

    public async FTask Init()
    {
        StartBornShit();
        FeedMachine.position = startPos;
        foreach (var obj in Enum.GetValues(typeof(FoodType)))
        {
            var foodType = (FoodType)obj;
            if (foodType == FoodType.None)
                continue;
            foodCount.Add(foodType, 10);
        }

        var unityEventTrigger = FeedMachine.GetComponent<UnityEventTrigger>();
        unityEventTrigger.Register(action_OnTriggerEnter2D: OnColliderEnter2D, action_OnTriggerExit2D: OnColliderExit2D);

        await FeedMachineAni(true);
        for (int i = 0; i < maxFruitCount; i++)
        {
            var fruitComponent = AddComponent<FoodComponent>();
            var foodType = (FoodType)Random.Range(1, Enum.GetValues(typeof(FoodType)).Length - 1);
            foodCount[foodType]--;
            await fruitComponent.Init(FruitParent, foodType);
            await FTask.Wait(Scene, 1000);
        }
    }

    public void StartBornShit()
    {
        Timer_BornShit = Scene.TimerComponent.Net.OnceTimer(Scene.GetComponent<Tables>().ConstConfigCategory.NewShitTime, () => 
        { 
            AddComponent<ShitComponent>(); 
        
            // 屎生成时触发弹幕
            Scene.GetComponent<DanmakuManagerComponent>()?.OnShitSpawned();
        });
    }
    public void CancelBornShit()
    {
        Scene.TimerComponent.Net.Remove(ref Timer_BornShit);
    }

    public void OnColliderEnter2D(Collider2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "Fruits")
        {
            OnFeedMachineFruitsCount++;
        }
    }

    public void OnColliderExit2D(Collider2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "Fruits")
        {
            OnFeedMachineFruitsCount--;
            if (OnFeedMachineFruitsCount <= 0)
                FeedMachineAni(false).Coroutine();
        }
    }

    public async FTask FeedMachineAni(bool isShow)
    {
        FeedMachine.DOMove(isShow ? endPos : startPos, 0.3f);
        await FTask.Wait(Scene, 1000);
    }

    public async FTask RemoveAndAddNewFruit(FoodComponent foodComponent)
    {
        RemoveComponent(foodComponent);
        await AddNewFruit();
    }

    public async FTask AddNewFruit()
    {
        var list = new List<FoodType>();
        foreach (var item in foodCount)
        {
            if (item.Value <= 0)
                continue;
            list.Add(item.Key);
        }

        if (list.Count == 0)
            return;
        await FTask.Wait(Scene, Scene.GetComponent<Tables>().ConstConfigCategory.NewFruitTime);
        await FeedMachineAni(true);
        var fruit = AddComponent<FoodComponent>();
        var listIdx = Random.Range(0, list.Count);  // ← 顺便修bug，原来是 list.Count - 1
        var foodType = list[listIdx];
        foodCount[foodType]--;
        await fruit.Init(FruitParent, foodType);
    }

// ========== 新增：指定类型生成食物 ==========
    public async FTask AddNewFruitOfType(FoodType foodType)
    {
        if (!foodCount.ContainsKey(foodType) || foodCount[foodType] <= 0)
        {
            Log.Error($"[FoodManager] Cannot spawn {foodType}, no stock left!");
            return;
        }

        await FTask.Wait(Scene, Scene.GetComponent<Tables>().ConstConfigCategory.NewFruitTime);
        await FeedMachineAni(true);
        var fruit = AddComponent<FoodComponent>();
        foodCount[foodType]--;
        await fruit.Init(FruitParent, foodType);
    
        Log.Error($"[FoodManager] Spawned {foodType} for task supplement");
    }

    public void PickUpFruit(long fruitId, Transform parent)
    {
        GetFruitComponent(fruitId).PickUp(parent);
    }

    public void DropFruit(long fruitId)
    {
        GetFruitComponent(fruitId).Drop();
    }

    public void StartEatFruit(long fruitId, bool isNormal)
    {
        GetFruitComponent(fruitId).StartEat(isNormal).Coroutine();
    }

    public void CancelEatFruit(long fruitId)
    {
        GetFruitComponent(fruitId).CancelEat();
    }

    public FoodComponent GetFruitComponent(long fruitId)
    {
        return GetComponent<FoodComponent>(fruitId);
    }

    public FoodComponent GetMinFruitDistance(Vector3 position, bool isInHand = true)
    {
        float distance = float.MaxValue;
        float fruitDistance = float.MaxValue;
        var checkDistance = isInHand ? Scene.GetComponent<Tables>().ConstConfigCategory.FoodCheckDistance : Scene.GetComponent<Tables>().ConstConfigCategory.PeekCheckDistance;
        FoodComponent foodComponent = null;

        foreach (var item in ForEachMultiEntity)
        {
            if (item is FoodComponent { isInPickUp: false } fruitComponent)
            {
                if (fruitComponent.fruitStateType == FruitStateType.BeEaten)
                    continue;
                if (fruitComponent.isStayHands != isInHand)
                    continue;
                if (fruitComponent.Fruit_Go == null || fruitComponent.Fruit_Tr == null)
                    continue;
                fruitDistance = Vector3.Distance(position, fruitComponent.GetPosition());
                if (fruitDistance < checkDistance && fruitDistance < distance)
                {
                    distance = fruitDistance;
                    foodComponent = fruitComponent;
                }
            }
        }

        return foodComponent;
    }

    public void AddForce(Vector2 position, float radius)
    {
        var collider2Ds = Physics2D.OverlapCircleAll(position, radius);
        foreach (var hit in collider2Ds)
        {
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 3. 计算方向：从爆炸点指向目标物体
                Vector2 direction = (rb.position - position).normalized;

                // 4. 计算距离衰减因子：距离越近力越大，超过半径则为0
                float distance = Vector2.Distance(position, rb.position);
                float falloff = 1 - Mathf.Clamp01(distance / radius);

                // 5. 计算最终力的大小
                float forceMagnitude = 1000f * falloff;

                // 6. 施加力
                // 注意：ForceMode2D.Impulse 适合爆炸这种瞬间力
                rb.AddForce(direction * forceMagnitude, ForceMode2D.Impulse);
            }
        }
    }

    public ShitComponent GetShit()
    {
        var shitComponent = GetComponent<ShitComponent>();
        if (shitComponent == null)
            return null;
        if (shitComponent.isLand)
            return shitComponent;
        return null;
    }

    public void EatShit()
    {
        var shit = GetComponent<ShitComponent>();
        shit?.StartEat();  // ← 新增：启动粒子
    
        //3.5s吃完屎
        Timer_EatShit = Scene.TimerComponent.Net.OnceTimer(Scene.GetComponent<Tables>().ConstConfigCategory.ShitBeEatenTime, () =>
        {
            Scene.EventComponent.Publish(new ShitBeEaten());
            RemoveShit();
        });
    }

    public void CancelEatShit()
    {
        var shit = GetComponent<ShitComponent>();
        shit?.CancelEat();  // ← 新增：停止粒子
    
        Scene.TimerComponent.Net.Remove(ref Timer_EatShit);
    }

    public void RemoveShit(bool isWipedByPlayer = false)
    {
        var shit = GetComponent<ShitComponent>();
        shit?.FinishEat();

        if (GetComponent<ShitComponent>() != null)
        {
            RemoveComponent<ShitComponent>();
            if (isWipedByPlayer)
            {
                var delay = Scene.GetComponent<Tables>().ConstConfigCategory.NewShitTimeAfterWipe;
                Timer_BornShit = Scene.TimerComponent.Net.OnceTimer(delay, () =>
                {
                    AddComponent<ShitComponent>();
                    Scene.GetComponent<DanmakuManagerComponent>()?.OnShitSpawned();
                });
            }
            else
            {
                StartBornShit();
            }
        }
    }
}

public class FruitsManagerComponent_Awake : AwakeSystem<FoodManagerComponent>
{
    protected override void Awake(FoodManagerComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        self.FruitParent = rc.Get<Transform>("Fruits");
        self.FeedMachine = rc.Get<Transform>("FeedMachine");

        self.Init().Coroutine();
    }
}

public class FoodManagerComponent_Destroy : DestroySystem<FoodManagerComponent>
{
    protected override void Destroy(FoodManagerComponent self)
    {
        self.CancelBornShit();
        self.CancelEatShit();
    }
}