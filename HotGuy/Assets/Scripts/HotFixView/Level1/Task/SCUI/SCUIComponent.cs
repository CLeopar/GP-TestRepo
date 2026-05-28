using System.Collections.Generic;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

/// <summary>
/// SC任务UI管理组件
/// 挂在Scene上，只管理数据引用，事件处理拆分到独立脚本
/// </summary>
public class SCUIComponent : Entity
{
    public GameObject SCTaskPrefab_Green;
    public GameObject SCTaskPrefab_Orange;
    public Transform SCTaskParent;
    public Dictionary<long, SCTaskUI> TaskUIInstances = new Dictionary<long, SCTaskUI>();
}

public class SCUIComponent_Awake : AwakeSystem<SCUIComponent>
{
    protected override void Awake(SCUIComponent self)
    {
        self.Scene.TimerComponent.Net.OnceTimer(100, () =>
        {
            var rc = GameObject.Find("Level_1")?.GetComponent<ReferenceCollector>();
            if (rc == null)
            {
                Log.Error("[SCUIComponent] ReferenceCollector not found!");
                return;
            }

            self.SCTaskPrefab_Green = rc.Get<GameObject>("SCTaskPrefab_Green");
            self.SCTaskPrefab_Orange = rc.Get<GameObject>("SCTaskPrefab_Orange");

            var parentGo = rc.Get<GameObject>("SCTaskParent");
            if (parentGo != null)
                self.SCTaskParent = parentGo.transform;
            else
                Log.Error("[SCUIComponent] SCTaskParent is null!");
        });
    }
}
