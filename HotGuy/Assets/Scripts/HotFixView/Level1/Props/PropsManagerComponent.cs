using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class PropsManagerComponent : Entity
{
    public int curPickUpId = 0;
    
    public void PickUpProp(Transform parent, string name)
    {
        if (name == "Spoon_L")
        {
            curPickUpId = 0;
            GetComponent<Prop_SpoonComponent>(0).PickUpProp(parent);
        }
        else
        {
            curPickUpId = 1;
            GetComponent<Prop_SpoonComponent>(1).PickUpProp(parent);
        }
    }

    public void DropProp()
    {
        if (curPickUpId == 0 || curPickUpId == 1)
            GetComponent<Prop_SpoonComponent>(curPickUpId).DropProp();
    }
}

public class PropsManagerComponent_Awake : AwakeSystem<PropsManagerComponent>
{
    protected override void Awake(PropsManagerComponent self)
    {
        self.AddComponent<Prop_SpoonComponent>(0);
        self.AddComponent<Prop_SpoonComponent>(1);
    }
}