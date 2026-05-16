using DG.Tweening;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class CameraShakeComponent : Entity
{
    public Transform MainCamera;
    private Sequence _shakeSequence;
    private Vector3 _cameraOriginPos;
    private bool _isShaking;

    public void Init()
    {
        MainCamera = GameObject.Find("MainCamera")?.transform;
        if (MainCamera == null)
        {
            Log.Error("[CameraShake] MainCamera not found!");
        }
    }

    public void StartShake()
    {
        if (_isShaking) return; // 防止重复启动
        if (MainCamera == null) return;

        _isShaking = true;
        _cameraOriginPos = MainCamera.position;
        
        // 先停掉旧的，防止叠加
        StopShake(false);
        
        // 创建震动循环：左右上下轻微随机偏移
        _shakeSequence = DOTween.Sequence();
        
        // 添加持续的随机震动
        _shakeSequence.Append(
            MainCamera.DOMove(_cameraOriginPos + new Vector3(0.08f, 0.05f, 0), 0.05f)
        );
        _shakeSequence.Append(
            MainCamera.DOMove(_cameraOriginPos + new Vector3(-0.08f, -0.05f, 0), 0.05f)
        );
        _shakeSequence.Append(
            MainCamera.DOMove(_cameraOriginPos + new Vector3(0.05f, -0.08f, 0), 0.05f)
        );
        _shakeSequence.Append(
            MainCamera.DOMove(_cameraOriginPos + new Vector3(-0.05f, 0.08f, 0), 0.05f)
        );
        
        _shakeSequence.SetLoops(-1, LoopType.Restart);
    }

    /// <summary>
    /// 停止震动
    /// </summary>
    /// <param name="smoothReturn">是否平滑回到原位</param>
    public void StopShake(bool smoothReturn = true)
    {
        if (!_isShaking && _shakeSequence == null) return;
        
        _isShaking = false;
        
        if (_shakeSequence != null)
        {
            _shakeSequence.Kill(true); // true = 完成当前循环后停止，这里直接停
            _shakeSequence = null;
        }

        if (MainCamera != null && smoothReturn)
        {
            // 平滑回到原位，覆盖之前可能残留的 tween
            MainCamera.DOKill(); // 杀掉相机上所有 DOTween
            MainCamera.DOMove(_cameraOriginPos, 0.3f).SetEase(Ease.OutQuad);
        }
    }
}

public class CameraShakeComponent_Awake : AwakeSystem<CameraShakeComponent>
{
    protected override void Awake(CameraShakeComponent self)
    {
        self.Init();
    }
}

public class CameraShakeComponent_Destroy : DestroySystem<CameraShakeComponent>
{
    protected override void Destroy(CameraShakeComponent self)
    {
        self.StopShake(false); // 场景销毁时直接停，不平滑
    }
}