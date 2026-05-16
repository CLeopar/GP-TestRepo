using System.Collections.Generic;
using System.Linq;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class DanmakuManagerComponent : Entity
{
    private DanmakuPoolConfigCategory _poolConfig;
    private DanmakuContentConfigCategory _contentConfig;
    private long _spawnTimer;
    private DanmakuPoolConfig _currentPoolConfig;

    public void Init()
    {
        _poolConfig = Scene.GetComponent<Tables>().DanmakuPoolConfigCategory;
        _contentConfig = Scene.GetComponent<Tables>().DanmakuContentConfigCategory;
        
        UpdateCurrentPool(DanmakuStateType.Anytime);
        StartSpawnTimer();
        
        Log.Error("[DanmakuManager] Initialized");
    }

    private void UpdateCurrentPool(DanmakuStateType stateType)
    {
        _currentPoolConfig = _poolConfig.GetOrDefault((int)stateType);
        if (_currentPoolConfig == null)
            _currentPoolConfig = _poolConfig.GetOrDefault((int)DanmakuStateType.Anytime);
    }

    private void StartSpawnTimer()
    {
        if (_currentPoolConfig == null) return;
        float interval = Random.Range(_currentPoolConfig.MinInterval, _currentPoolConfig.MaxInterval);
        _spawnTimer = Scene.TimerComponent.Net.OnceTimer((long)(interval * 1000), OnSpawnTick);
    }

    private void OnSpawnTick()
    {
        TrySpawnDanmaku();
        StartSpawnTimer();
    }

    private void TrySpawnDanmaku()
    {
        var uiComp = Scene.GetComponent<DanmakuUIComponent>();
        if (uiComp == null) return;

        var stateType = GetCurrentDanmakuStateType();
        UpdateCurrentPool(stateType);
        
        var contentData = PickContentByState(stateType);
        if (contentData == null)
            contentData = PickContentByState(DanmakuStateType.Anytime);

        if (contentData == null)
        {
            Log.Error("[DanmakuManager] No content available");
            return;
        }
        
        var danmakuData = new DanmakuData
        {
            ConfigId = contentData.Id,
            Content = contentData.Content,
            StateType = stateType
        };

        uiComp.CreateDanmaku(danmakuData);
    }

    private DanmakuStateType GetCurrentDanmakuStateType()
    {
        var dogCtrl = Scene.GetComponent<DogControlComponent>();
        if (dogCtrl == null) return DanmakuStateType.Anytime;

        var state = dogCtrl.dogState;

        if (state == DogState.Eat_Normal || state == DogState.Eat_Normal_Secretly)
            return DanmakuStateType.Eating;
        if (state == DogState.Hit || state == DogState.Hit_Right || state == DogState.Hit_Wrong)
            return DanmakuStateType.Hit;
        if (state == DogState.Eat_Secretly_3)
        {
            var foodData = dogCtrl.CurEatFoodData;
            if (foodData.Item1 == FoodType.Shit)
                return DanmakuStateType.EatShit;
        }
        return DanmakuStateType.Anytime;
    }

    private DanmakuContentConfig PickContentByState(DanmakuStateType stateType)
    {
        var pool = _contentConfig.DataList
            .Where(x => x.StateType == (int)stateType)
            .ToList();

        if (pool.Count == 0) return null;

        int totalWeight = pool.Sum(x => x.Weight);
        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var item in pool)
        {
            cumulative += item.Weight;
            if (rand < cumulative)
                return item;
        }
        return pool.Last();
    }

    public void OnShitSpawned()
    {
        var uiComp = Scene.GetComponent<DanmakuUIComponent>();
        if (uiComp == null) return;

        var contentData = PickContentByState(DanmakuStateType.ShitSpawn);
        if (contentData == null) return;
        
        var danmakuData = new DanmakuData
        {
            ConfigId = contentData.Id,
            Content = contentData.Content,
            StateType = DanmakuStateType.ShitSpawn
        };

        uiComp.CreateDanmaku(danmakuData);
    }

    public void Clear()
    {
        Scene.TimerComponent.Net.Remove(ref _spawnTimer);
    }
}

public class DanmakuManagerComponent_Awake : AwakeSystem<DanmakuManagerComponent>
{
    protected override void Awake(DanmakuManagerComponent self) => self.Init();
}

public class DanmakuManagerComponent_Destroy : DestroySystem<DanmakuManagerComponent>
{
    protected override void Destroy(DanmakuManagerComponent self) => self.Clear();
}