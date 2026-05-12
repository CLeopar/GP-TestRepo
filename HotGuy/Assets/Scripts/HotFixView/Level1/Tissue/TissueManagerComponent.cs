using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TissueManagerComponent : Entity
{
    public Transform TissueParent;

    public void Init()
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        TissueParent = rc.Get<Transform>("TissueParent");
    }

    public TissueComponent CreateTissue(Transform hand)
    {
        var tissueComponent = AddComponent<TissueComponent>();
        tissueComponent.Init(hand);
        return tissueComponent;
    }

    public TissueComponent GetTissue(long id)
    {
        return GetComponent<TissueComponent>(id);
    }

    public void DropTissue(long id)
    {
        var tissueComponent = GetTissue(id);
        if(tissueComponent != null)
            tissueComponent.DropByHand(TissueParent);
    }
}

public class TissueManagerComponent_Awake : AwakeSystem<TissueManagerComponent>
{
    protected override void Awake(TissueManagerComponent self)
    {
        self.Init();
    }
}