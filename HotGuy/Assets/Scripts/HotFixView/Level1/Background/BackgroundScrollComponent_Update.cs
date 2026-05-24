using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class BackgroundScrollComponent_Update : UpdateSystem<BackgroundScrollComponent>
{
    private float _logTimer = 0f;
    
    protected override void Update(BackgroundScrollComponent self)
    {
        self.UpdateScroll(UnityEngine.Time.deltaTime);
        
        // 调试用：每 2 秒确认 Update 在跑
        _logTimer += UnityEngine.Time.deltaTime;
        if (_logTimer > 2f)
        {
            _logTimer = 0f;
            Log.Error($"[BackgroundScroll] Update running. Offset: {self.GetOffset()}");
        }
    }
}