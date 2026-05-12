using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class Prop_SpoonComponent : Entity, ISupportedMultiEntity
{
    public Transform parent;
    public Transform Spoon;
    public Rigidbody2D rigidbody2D;
    public SpriteRenderer SpriteRenderer;

    public void PickUpProp(Transform parent)
    {
        Spoon.SetParent(parent);
        Spoon.localPosition = Vector3.zero;
        rigidbody2D.gravityScale = 0;
        SpriteRenderer.sortingOrder = 100;
        Spoon.gameObject.SetActive(false);
    }

    public void DropProp()
    {
        Spoon.SetParent(parent);
        rigidbody2D.gravityScale = 1;
        SpriteRenderer.sortingOrder = 0;
        Spoon.gameObject.SetActive(true);
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
    }
}