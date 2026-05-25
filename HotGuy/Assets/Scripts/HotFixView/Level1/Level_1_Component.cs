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
        // 新增：开始播放关卡BGM
        Scene.GetComponent<AudioManagerComponent>()?.Play(SFXType.EatBadEgg).Coroutine();

        Timer = Scene.TimerComponent.Net.RepeatedTimer(1000, () =>
        {
            Level_Duration += 1000;
            
            PublishTimerUpdate();
            
            if (Level_Duration >= 120 * 1000)
            {
                Scene.TimerComponent.Net.Remove(ref Timer);
                Level_Stage = 4;
                
                Scene.EventComponent.Publish(new LevelTimerFinished());
            }
            else
            {
                if (Level_Duration <= 15 * 1000)
                {
                    Level_Stage = 1;
                }
                else if (Level_Duration <= 60 * 1000)
                    Level_Stage = 2;
                else if (Level_Duration <= 100 * 1000)
                    Level_Stage = 3;
            }
        });
    }

    private void PublishTimerUpdate()
    {
        long remaining = 120 * 1000 - Level_Duration;
        Scene.EventComponent.Publish(new LevelTimerUpdate
        {
            RemainingTime = remaining,
            ElapsedTime = Level_Duration,
            TotalTime = 120 * 1000
        });
    }
    
    public long GetSCDuration()
    {
        if (Level_Stage == 1)
            return 10 * 1000;
        if (Level_Stage == 2)
            return 8 * 1000;
        if (Level_Stage == 3)
            return 6 * 1000;
        return 3500;
    }
    
    public long GetDogEatSecretlyDuration()
    {
        if (Level_Stage == 1)
            return 0;
        if (Level_Stage == 2)
            return Random.Range(8, 11) * 1000;
        if (Level_Stage == 3)
            return Random.Range(5, 7) * 1000;
        return 4000;
    }
    
    public long GetDogEatSecretlyPerDuration()
    {
        if (Level_Stage == 1)
            return 0;
        if (Level_Stage == 2)
            return 4000;
        if (Level_Stage == 3)
            return 3500;
        return 2000;
    }
}

public class Level_1_Component_Awake : AwakeSystem<Level_1_Component>
{
    protected override void Awake(Level_1_Component self)
    {
        // 先注册音效管理器，确保 Init() 里能播放BGM
        if (self.Scene.GetComponent<AudioManagerComponent>() == null)
        {
            self.Scene.AddComponent<AudioManagerComponent>();
            Log.Error("[Level_1] AudioManagerComponent ADDED");
        }
        
        self.Init();
        
        if (self.Scene.GetComponent<ScoreComponent>() == null)
            self.Scene.AddComponent<ScoreComponent>();
        
        if (self.Scene.GetComponent<ScoreUIComponent>() == null)
            self.Scene.AddComponent<ScoreUIComponent>();
            
        if (self.Scene.GetComponent<FoodParticleEffectComponent>() == null)
            self.Scene.AddComponent<FoodParticleEffectComponent>();
        
        if (self.Scene.GetComponent<LevelTimerUIComponent>() == null)
            self.Scene.AddComponent<LevelTimerUIComponent>();
        
        if (self.Scene.GetComponent<TaskManagerComponent>() == null)
            self.Scene.AddComponent<TaskManagerComponent>();

        if (self.Scene.GetComponent<SCUIComponent>() == null)
            self.Scene.AddComponent<SCUIComponent>();
        
        if (self.Scene.GetComponent<LevelStatsComponent>() == null)
        {
            self.Scene.AddComponent<LevelStatsComponent>();
            Log.Error("[Level_1] LevelStatsComponent ADDED");
        }
        
        if (self.Scene.GetComponent<FadePanelUIComponent>() == null)
            self.Scene.AddComponent<FadePanelUIComponent>();

        if (self.Scene.GetComponent<DanmakuUIComponent>() == null)
            self.Scene.AddComponent<DanmakuUIComponent>();

        if (self.Scene.GetComponent<DanmakuManagerComponent>() == null)
            self.Scene.AddComponent<DanmakuManagerComponent>();
        
        if (self.Scene.GetComponent<CameraShakeComponent>() == null)
            self.Scene.AddComponent<CameraShakeComponent>();
        
        if (self.Scene.GetComponent<BackgroundScrollComponent>() == null)
            self.Scene.AddComponent<BackgroundScrollComponent>();
    }
}

public class Level_1_Component_Destroy : DestroySystem<Level_1_Component>
{
    protected override void Destroy(Level_1_Component self)
    {
        self.Scene.TimerComponent.Net.Remove(ref self.Timer);
        self.Scene.GetComponent<AudioManagerComponent>()?.StopEatBadEgg();
    }
}