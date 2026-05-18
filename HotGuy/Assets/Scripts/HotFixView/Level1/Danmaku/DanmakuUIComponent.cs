using System.Collections.Generic;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class DanmakuUIComponent : Entity
{
    public GameObject DanmakuPrefab;
    public Transform DanmakuParent;

    public List<DanmakuUI> ActiveDanmakus = new List<DanmakuUI>();

    public float ItemHeight = 60f;
    public float Spacing = 80f;
    public int MaxCount = 5;

    public void Init()
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        DanmakuPrefab = rc.Get<GameObject>("DanmakuPrefab");

        var parentGo = rc.Get<GameObject>("DanmakuParent");
        if (parentGo != null)
            DanmakuParent = parentGo.transform;
    }

    public void CreateDanmaku(DanmakuData data)
    {
        // 超出数量：最老的（列表末尾）滑出销毁
        while (ActiveDanmakus.Count >= MaxCount)
        {
            var oldest = ActiveDanmakus[ActiveDanmakus.Count - 1];
            ActiveDanmakus.RemoveAt(ActiveDanmakus.Count - 1);
            oldest?.SlideOutAndDestroy();
        }

        if (DanmakuPrefab == null || DanmakuParent == null)
        {
            Debug.LogWarning("[DanmakuUI] Prefab or Parent not set!");
            return;
        }

        // 新条目目标 Y = index 0（最下方）
        float newTargetY = 0f;

        // 已有弹幕往上移一格
        for (int i = 0; i < ActiveDanmakus.Count; i++)
        {
            float targetY = (i + 1) * (ItemHeight + Spacing);
            ActiveDanmakus[i]?.MoveTo(targetY);
        }

        // 实例化新弹幕
        var go = GameObject.Instantiate(DanmakuPrefab, DanmakuParent);
        var danmakuUI = go.GetComponent<DanmakuUI>();
        if (danmakuUI == null)
            danmakuUI = go.AddComponent<DanmakuUI>();

        // Init 内部会自动播放飞入动画
        danmakuUI.Init(data, newTargetY);

        // 插入列表最前面（index 0 = 最新/最下）
        ActiveDanmakus.Insert(0, danmakuUI);
    }

    public void ClearAll()
    {
        foreach (var d in ActiveDanmakus)
            d?.ForceDestroy();

        ActiveDanmakus.Clear();
    }
}

public class DanmakuUIComponent_Awake : AwakeSystem<DanmakuUIComponent>
{
    protected override void Awake(DanmakuUIComponent self) => self.Init();
}

public class DanmakuUIComponent_Destroy : DestroySystem<DanmakuUIComponent>
{
    protected override void Destroy(DanmakuUIComponent self) => self.ClearAll();
}