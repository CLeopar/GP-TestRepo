using Fantasy.Async;
using Fantasy;     
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class Prop_SpoonComponent : Entity, ISupportedMultiEntity
{
    public Transform parent;
    public Transform Spoon;
    public Rigidbody2D rigidbody2D;
    public SpriteRenderer SpriteRenderer;
    public PolygonCollider2D polygonCollider2D;

    /// <summary>当前拿着这把勺子的玩家索引，-1=无人拿，0=Player1(左)，1=Player2(右)</summary>
    public int HolderPlayerIndex = -1;

    /// <summary>被拿起前的父物体</summary>
    public Transform OriginalParent;

    public void AwakeInit()
    {
        OriginalParent = parent;
    }

    public bool CanBePickedUp()
    {
        return HolderPlayerIndex == -1;
    }

    public void PickUpProp(Transform handParent, int playerIndex)
    {
        if (HolderPlayerIndex != -1 && HolderPlayerIndex != playerIndex)
        {
            Log.Error($"[Prop_Spoon] Spoon is already held by player {HolderPlayerIndex}");
            return;
        }

        HolderPlayerIndex = playerIndex;

        if (OriginalParent == null)
            OriginalParent = Spoon.parent;

        // 完全禁用物理
        rigidbody2D.simulated = false;
        rigidbody2D.velocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0;

        // 挂到手下，隐藏
        Spoon.SetParent(handParent);
        Spoon.localPosition = Vector3.zero;
        Spoon.localRotation = Quaternion.identity;
        SpriteRenderer.sortingOrder = 100;
        Spoon.gameObject.SetActive(false);
    }

    public void DropProp()
    {
        if (HolderPlayerIndex == -1) return;

        HolderPlayerIndex = -1;

        // 回到 Props
        Spoon.SetParent(parent);

        // 安全位置：稍微向上偏移
        Vector3 safePos = Spoon.position + new Vector3(0, 0.3f, 0);

        // 先禁用碰撞体，避免弹射
        polygonCollider2D.enabled = false;

        // 恢复显示和物理
        Spoon.gameObject.SetActive(true);
        Spoon.position = safePos;
        rigidbody2D.simulated = true;
        rigidbody2D.gravityScale = 1;
        rigidbody2D.velocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0;
        SpriteRenderer.sortingOrder = 0;

        // 延迟启用碰撞体
        Scene.TimerComponent.Net.OnceTimer(50, () =>
        {
            if (polygonCollider2D != null)
                polygonCollider2D.enabled = true;
        });
    }
}

public class Prop_SpoonComponent_Awake : AwakeSystem<Prop_SpoonComponent>
{
    protected override void Awake(Prop_SpoonComponent self)
    {
        bool isL = (int)self.Id == 0;
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        self.parent = rc.Get<Transform>("Props");
        self.Spoon = rc.Get<Transform>(isL ? "Spoon_L" : "Spoon_R");
        self.SpriteRenderer = self.Spoon.GetComponent<SpriteRenderer>();
        self.rigidbody2D = self.Spoon.GetComponent<Rigidbody2D>();
        self.polygonCollider2D = self.Spoon.GetComponent<PolygonCollider2D>();

        self.AwakeInit();
    }
}

public class Prop_SpoonComponent_Destroy : DestroySystem<Prop_SpoonComponent>
{
    protected override void Destroy(Prop_SpoonComponent self)
    {
        // 如果还被拿着，强制归还场景
        if (self.HolderPlayerIndex != -1)
        {
            self.HolderPlayerIndex = -1;
            self.Spoon.SetParent(self.OriginalParent ?? self.parent);
            self.Spoon.gameObject.SetActive(true);
            if (self.rigidbody2D != null)
            {
                self.rigidbody2D.simulated = true;
                self.rigidbody2D.gravityScale = 1;
            }
        }
    }
}