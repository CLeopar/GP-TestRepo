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
            if (Level_Duration >= 120 * 1000)
            {
                Scene.TimerComponent.Net.Remove(ref Timer);
                Level_Stage = 4;
            }
            else
            {
                if (Level_Duration <= 30 * 1000)
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
        self.Init();
        
          
         
        // 注册分数组件
        if (self.Scene.GetComponent<ScoreComponent>() == null)
            self.Scene.AddComponent<ScoreComponent>();
        
        // 注册分数UI组件
        if (self.Scene.GetComponent<ScoreUIComponent>() == null)
            self.Scene.AddComponent<ScoreUIComponent>();
        
        // ========== 注册粒子特效组件 ==========
        if (self.Scene.GetComponent<FoodParticleEffectComponent>() == null)
            self.Scene.AddComponent<FoodParticleEffectComponent>();
    }
}

public class Level_1_Component_Destroy : DestroySystem<Level_1_Component>
{
    protected override void Destroy(Level_1_Component self)
    {
        self.Scene.TimerComponent.Net.Remove(ref self.Timer);
    }
}