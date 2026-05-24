using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class Level_1_Component : Entity
{
    public int Level_Stage { get; private set; } = 1;
    public long Level_Duration { get; private set; } = 0;
    public long Timer;

    public void Init()
    {
        Timer = Scene.TimerComponent.Net.RepeatedTimer(1000, () =>
        {
            Level_Duration += 1000;
            
            // ========== 新增：发布倒计时更新事件 ==========
            PublishTimerUpdate();
            
            
            if (Level_Duration >= 120 * 1000)// 这是关卡总时长，120秒 = 120000毫秒
            {
                Scene.TimerComponent.Net.Remove(ref Timer);
                Level_Stage = 4;
                
                // ========== 新增：发布倒计时结束事件 ==========
                Scene.EventComponent.Publish(new LevelTimerFinished());
            }
            else
            {
                if (Level_Duration <= 15 * 1000)// 30 * 1000  = 30000ms = 30秒  → P1结束/P2开始
                {
                    Level_Stage = 1;
                }
                else if (Level_Duration <= 60 * 1000) // 60 * 1000  = 60000ms = 60秒  → P2结束/P3开始 
                    Level_Stage = 2;
                else if (Level_Duration <= 100 * 1000)  // 100 * 1000 = 100000ms = 100秒 → P3结束
                    Level_Stage = 3;
            }
        });
    }

    // ========== 新增：发布倒计时更新 ==========
    private void PublishTimerUpdate()
    {
        // ═══════════════════════════════════════════════════════════
        // 【改这里】120 * 1000 → 配置表 TotalDuration
        // 总时长用于计算剩余时间
        // ═══════════════════════════════════════════════════════════
        long remaining = 120 * 1000 - Level_Duration;
        Scene.EventComponent.Publish(new LevelTimerUpdate
        {
            RemainingTime = remaining,
            ElapsedTime = Level_Duration,
            TotalTime = 120 * 1000// ← 【改这里】
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 【改这里】SC发布时间 → 配置表 SCSpawnInterval
    // 每个阶段SC多久刷一次
    // ═══════════════════════════════════════════════════════════
    
    public long GetSCDuration()
    {
        if (Level_Stage == 1)
            return 10 * 1000;// ← P1: 10秒 = 10000ms
        if (Level_Stage == 2)
            return 8 * 1000;// ← P2: 8秒 = 8000ms 
        if (Level_Stage == 3)
            return 6 * 1000;// ← P3: 6秒 = 6000ms
        return 3500; // ← P4: 3.5秒 = 3500ms
    }
    
// ═══════════════════════════════════════════════════════════
    // 【改这里】偷吃行为的I阶段时间（偷瞄状态）→ 配置表 DogEatPerDuration
    // 偷瞄持续多久
    // ═══════════════════════════════════════════════════════════
    
    public long GetDogEatSecretlyDuration()
    {
        if (Level_Stage == 1)
            return 0;// ← P1: 不偷吃
        if (Level_Stage == 2)
            return Random.Range(8, 11) * 1000; // ← P2: 8-10秒随机
        if (Level_Stage == 3)
            return Random.Range(5, 7) * 1000;// ← P3: 5-6秒随机
        return 4000;// ← P4: 4秒固定
    }
    
    public long GetDogEatSecretlyPerDuration()
    {
        if (Level_Stage == 1)
            return 0;// ← P1: 无偷瞄
        if (Level_Stage == 2)
            return 4000;// ← P2: 4秒
        if (Level_Stage == 3)
            return 3500;// ← P3: 3.5秒
        return 2000; // ← P4: 2秒
    }
}

public class Level_1_Component_Awake : AwakeSystem<Level_1_Component>
{
    protected override void Awake(Level_1_Component self)
    {
        self.Init();
        
        if (self.Scene.GetComponent<ScoreComponent>() == null)
            self.Scene.AddComponent<ScoreComponent>();
        
        if (self.Scene.GetComponent<ScoreUIComponent>() == null)
            self.Scene.AddComponent<ScoreUIComponent>();
            
        if (self.Scene.GetComponent<FoodParticleEffectComponent>() == null)
            self.Scene.AddComponent<FoodParticleEffectComponent>();
        
        // ========== 新增：注册倒计时UI组件 ==========
        if (self.Scene.GetComponent<LevelTimerUIComponent>() == null)
            self.Scene.AddComponent<LevelTimerUIComponent>();
        
        // ========== 新增：注册SC任务系统组件 ==========
        if (self.Scene.GetComponent<TaskManagerComponent>() == null)
            self.Scene.AddComponent<TaskManagerComponent>();

        if (self.Scene.GetComponent<SCUIComponent>() == null)
            self.Scene.AddComponent<SCUIComponent>();
        
        // ========== 新增：注册统计组件 ==========
        if (self.Scene.GetComponent<LevelStatsComponent>() == null)
        {
            self.Scene.AddComponent<LevelStatsComponent>();
            Log.Error("[Level_1] LevelStatsComponent ADDED");
        }
        
        // 新增：注册渐黑面板
        if (self.Scene.GetComponent<FadePanelUIComponent>() == null)
            self.Scene.AddComponent<FadePanelUIComponent>();
// 注册弹幕系统
        if (self.Scene.GetComponent<DanmakuUIComponent>() == null)
            self.Scene.AddComponent<DanmakuUIComponent>();

        if (self.Scene.GetComponent<DanmakuManagerComponent>() == null)
            self.Scene.AddComponent<DanmakuManagerComponent>();
        
        // 
        if (self.Scene.GetComponent<CameraShakeComponent>() == null)
            self.Scene.AddComponent<CameraShakeComponent>();
        
        // 注册音效管理器
        if (self.Scene.GetComponent<AudioManagerComponent>() == null)
        {
            self.Scene.AddComponent<AudioManagerComponent>();
            Log.Error("[Level_1] AudioManagerComponent ADDED");
        }
        
        // 注册背景滚动组件
        if (self.Scene.GetComponent<BackgroundScrollComponent>() == null)
            self.Scene.AddComponent<BackgroundScrollComponent>();
    }
}

public class Level_1_Component_Destroy : DestroySystem<Level_1_Component>
{
    protected override void Destroy(Level_1_Component self)
    {
        self.Scene.TimerComponent.Net.Remove(ref self.Timer);
    }
}