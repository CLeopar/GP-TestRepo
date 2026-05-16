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
        // 超数量：销毁最老的（最上面的）
        while (ActiveDanmakus.Count >= 3)
        {
            var oldest = ActiveDanmakus[ActiveDanmakus.Count - 1];
            ActiveDanmakus.RemoveAt(ActiveDanmakus.Count - 1);
            oldest.ForceDestroy();
        }

        if (DanmakuPrefab == null || DanmakuParent == null)
        {
            Debug.Log("[DanmakuUI] Prefab or Parent not set!");
            return;
        }

        var go = GameObject.Instantiate(DanmakuPrefab, DanmakuParent);
        var danmakuUI = go.GetComponent<DanmakuUI>();
        if (danmakuUI == null)
            danmakuUI = go.AddComponent<DanmakuUI>();

        danmakuUI.Init(data);
        
        // 插入到最前面（新生成在最下方）
        ActiveDanmakus.Insert(0, danmakuUI);

        // 重新排列
        RefreshPositions();
    }

    /// <summary>
    /// 从下往上排列
    /// 第0个在最下方，新的往上推老的
    /// </summary>
    private void RefreshPositions()
    {
        for (int i = 0; i < ActiveDanmakus.Count; i++)
        {
            var danmaku = ActiveDanmakus[i];
            if (danmaku == null) continue;
            
            var rect = danmaku.GetComponent<RectTransform>();
            if (rect != null)
            {
                // i=0 在最下面，i越大越往上
                float targetY = i * (ItemHeight + Spacing);
                rect.anchoredPosition = new Vector2(0, targetY);
            }
        }
    }

    public void ClearAll()
    {
        foreach (var d in ActiveDanmakus)
        {
            if (d != null)
                d.ForceDestroy();
        }
        ActiveDanmakus.Clear();
    }
}

public class DanmakuUIComponent_Awake : AwakeSystem<DanmakuUIComponent>
{
    protected override void Awake(DanmakuUIComponent self)
    {
        self.Init();
    }
}

public class DanmakuUIComponent_Destroy : DestroySystem<DanmakuUIComponent>
{
    protected override void Destroy(DanmakuUIComponent self)
    {
        self.ClearAll();
    }
}