using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class FoodBoundaryComponent : Entity
{
    public Collider2D BoundaryCollider;  // 触发器碰撞体
    public Transform ResetPoint;          // 重置位置参考点（FeedMachine）

    public float ResetHeightOffset = 5f;
    public float ResetRandomX = 2f;
    public float DropHorizontalSpeed = 2f;

    public void Init(Collider2D boundaryCollider, Transform resetPoint)
    {
        BoundaryCollider = boundaryCollider;
        ResetPoint = resetPoint;
    }

    public void CheckFoods()
    {
        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null) return;

        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is not FoodComponent food) continue;
            if (food.Fruit_Tr == null) continue;
            if (food.isInPickUp) continue;  // 被玩家拿着的不处理

            // 检查食物是否在边界外
            if (IsOutOfBounds(food.Fruit_Tr.position))
            {
                ResetFood(food);
            }
        }
    }

    private bool IsOutOfBounds(Vector3 position)
    {
        if (BoundaryCollider == null) return false;

        // 如果碰撞体是 Trigger，用 OverlapPoint 检测
        // 或者直接检查位置是否在碰撞体边界框内
        Bounds bounds = BoundaryCollider.bounds;
        return !bounds.Contains(position);
    }

    private void ResetFood(FoodComponent food)
    {
        var rb = food.rigidBody2D;
        if (rb == null) return;

        // ========== 修复：重置到摄像机正上方，水平随机偏移小一点 ==========
        var camera = Camera.main;
        Vector3 resetPos;
    
        if (camera != null)
        {
            // 摄像机底部世界坐标
            float camBottomY = camera.transform.position.y - camera.orthographicSize;
            // 放到摄像机上方（屏幕外）
            float spawnY = camera.transform.position.y + camera.orthographicSize + 2f;
            // 水平范围：摄像机视野内随机
            float camWidth = camera.orthographicSize * camera.aspect;
            float spawnX = Random.Range(-camWidth * 0.5f, camWidth * 0.5f);
        
            resetPos = new Vector3(spawnX, spawnY, 0);
        }
        else
        {
            resetPos = new Vector3(0, 12f, 0);
        }

        // 完全重置物理状态
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.simulated = false;

        // 先禁用碰撞体一帧，避免瞬移时穿透
        var col = food.collider2D;
        if (col != null) col.enabled = false;

        food.Fruit_Tr.position = resetPos;
        food.Fruit_Tr.rotation = Quaternion.identity;

        // 延迟一帧恢复物理
        Scene.TimerComponent.Net.OnceTimer(50, () =>
        {
            if (col != null) col.enabled = true;
            rb.simulated = true;
            rb.gravityScale = 1;
            // 不给水平速度，纯自由落体
            rb.velocity = Vector2.zero;
        });

        if (food.IsBeingEaten)
            food.CancelEat();

        var dogCtrl = Scene.GetComponent<DogControlComponent>();
        if (dogCtrl != null && dogCtrl.CurEatFoodData.Item2 == food.Id)
        {
            dogCtrl.CancelCurrentEating();
            dogCtrl.ChangeDogState(DogState.Normal);
        }

        Log.Error($"[FoodBoundary] Reset food {food.foodType} to {resetPos}");
    }
}

public class FoodBoundaryComponent_Awake : AwakeSystem<FoodBoundaryComponent>
{
    protected override void Awake(FoodBoundaryComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        
        // 从 ReferenceCollector 获取边界碰撞体和重置点
        var boundaryObj = rc.Get<GameObject>("FoodBoundary");
        if (boundaryObj != null)
            self.BoundaryCollider = boundaryObj.GetComponent<Collider2D>();

        self.ResetPoint = rc.Get<Transform>("FeedMachine");

        Log.Error($"[FoodBoundary] Initialized, collider: {self.BoundaryCollider != null}, resetPoint: {self.ResetPoint != null}");
    }
}

public class FoodBoundaryComponent_Update : UpdateSystem<FoodBoundaryComponent>
{
    protected override void Update(FoodBoundaryComponent self)
    {
        self.CheckFoods();
    }
}