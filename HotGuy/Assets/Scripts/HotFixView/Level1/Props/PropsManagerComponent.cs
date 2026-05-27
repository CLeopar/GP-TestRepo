using System.Collections.Generic;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class PropsManagerComponent : Entity
{
    public void PickUpProp(Transform handParent, string name, int playerIndex)
    {
        long targetId = name == "Spoon_L" ? 0 : 1;
        var spoon = GetComponent<Prop_SpoonComponent>(targetId);
        if (spoon == null)
        {
            Log.Error($"[PropsManager] Spoon not found: {name}");
            return;
        }

        if (!spoon.CanBePickedUp())
        {
            Log.Error($"[PropsManager] Spoon {name} is already held by player {spoon.HolderPlayerIndex}");
            return;
        }

        spoon.PickUpProp(handParent, playerIndex);
    }

    public void DropProp(int playerIndex)
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is Prop_SpoonComponent spoon && spoon.HolderPlayerIndex == playerIndex)
            {
                spoon.DropProp();
                return;
            }
        }
    }

    public bool IsHoldingProp(int playerIndex)
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is Prop_SpoonComponent spoon && spoon.HolderPlayerIndex == playerIndex)
                return true;
        }
        return false;
    }

    public Prop_SpoonComponent GetHeldProp(int playerIndex)
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is Prop_SpoonComponent spoon && spoon.HolderPlayerIndex == playerIndex)
                return spoon;
        }
        return null;
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