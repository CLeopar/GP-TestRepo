using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

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
            //默认显示第一状态
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
        // spriteRenderer.sortingOrder = 101;
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
        // spriteRenderer.sortingOrder = 0;

        isInPickUp = false;
    }

    public async FTask StartEat(bool isNormal)
    {
        //四个阶段，目前用颜色区分
        //红、黄、蓝、黑
        CancellationToken = FCancellationToken.ToKen;
        var duration = Scene.GetComponent<Tables>().ConstConfigCategory.FoodChangeStateInterval;
        if (fruitStateType == FruitStateType.Normal)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel)
                return;
            ShowState(1, isNormal);
        }

        if (fruitStateType == FruitStateType.Eat_2)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel)
                return;
            ShowState(2, isNormal);
        }

        if (fruitStateType == FruitStateType.Eat_3)
        {
            await Scene.TimerComponent.Net.WaitAsync(duration, CancellationToken);
            if (CancellationToken.IsCancel)
                return;
            ShowState(3, isNormal);
        }
    }

    public void CancelEat()
    {
        CancellationToken?.Cancel();
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
            if (isNormal)
            {
                //通知食物已被吃完
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