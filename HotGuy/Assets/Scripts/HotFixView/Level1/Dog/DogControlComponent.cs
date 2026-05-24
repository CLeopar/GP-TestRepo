using System;
using DG.Tweening;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using Random = UnityEngine.Random;

public class DogControlComponent : Entity
{
    public Transform Dog;
    public GameObject Dog_Front;
    public GameObject Dog_1;
    public GameObject Dog_2;
    public GameObject Dog_3;
    public GameObject Dog_4;
    public GameObject Dog_Hold_L;
    public GameObject Dog_Hold_R;
    public GameObject Dog_Eat;
    public GameObject Dog_Eat_Secretly;
    public GameObject Dog_Hit;
    public GameObject Dog_Hit_1;
    public GameObject Dog_Hit_2;
    public GameObject Dog_Hit_Right;
    public GameObject Dog_Hit_Wrong;

    public DogState dogState = DogState.Normal;

    public FCancellationToken cancellationToken;

    public (FoodType, long) CurEatFoodData;

    public long Timer;

    public bool isInHit = false;

    public Tweener currentRotateTween;
    public Transform FoodCheckDistance_Gizmos;
    public Transform FoodCheckDistance_Gizmos_Secretly;

    public bool isOpenPeek = false;

    // 滞后检测：避免边缘抖动导致状态反复切换
    private bool _foodInNormalRange = false;
    private bool _foodInSecretlyRange = false;

    // Hit 动画期间的取消令牌
    private FCancellationToken hitCancellationToken;

    // 打对后的偷瞄冷却（3秒内不进入偷瞄）
    public bool _peekCooldown = false;

    // 记录进入 Hit 状态时是否在偷吃（用于后续判定正确/错误）
    private bool wasSecretlyEatingWhenHit;

    // 记录狗的初始位置，用于回归
    private Vector3 _originalPosition;
    private Tweener _returnTween;

    // Update 只负责检测喂食，偷吃流程完全由协程驱动
    private bool _normalFoodPresent = false;
    private long _nearestNormalFoodId = 0;
    private FoodType _nearestNormalFoodType = FoodType.None;

    // ========== 新增：吃食物时循环播放 Eating1 ==========
    private long _eatSfxTimer = 0;
    private const long EatSfxInterval = 1000; // 每1秒播放一次
    private bool _isEatSfxPaused = false;

    public void Init()
    {
        if (Dog != null)
            _originalPosition = Dog.position;

        ChangeDogSpriteState(DogState.Normal);
        AddEatSecretlyTimer();
    }

    public void AddEatSecretlyTimer()
    {
        if (_peekCooldown) return; // 打对后冷却期内不重启偷瞄
        var levelComponent = Scene.GetComponent<Level_1_Component>();
        var dura = levelComponent.GetDogEatSecretlyDuration();
        if (dura <= 0) return;
        Timer = Scene.TimerComponent.Net.OnceTimer(dura, () => SetIsOpenPeek(true));
    }

    public void SetIsOpenPeek(bool isOpen)
    {
        if (_peekCooldown && isOpen) return; // 冷却期内不开启偷瞄
        if (isOpenPeek)
            Scene.TimerComponent.Net.Remove(ref Timer);
        isOpenPeek = isOpen;
        if (!isOpenPeek)
            AddEatSecretlyTimer();
    }

    public void ChangeDogSpriteState(DogState state, bool isL = true)
    {
        if (this.dogState == state) return;

        this.dogState = state;
        Dog_Front.SetActive(state == DogState.Normal);
        Dog_1.SetActive(state == DogState.Eat_Secretly_1);
        Dog_2.SetActive(state == DogState.Eat_Secretly_2);

        if (state == DogState.Eat_Secretly_3)
        {
            Dog_3.transform.position = Dog_2.transform.position;
            Dog_3.transform.rotation = Dog_2.transform.rotation;
        }

        Dog_3.SetActive(state == DogState.Eat_Secretly_3);
        Dog_4.SetActive(state == DogState.Eat_Secretly_4);
        Dog_Hold_L.SetActive(state == DogState.Hold && isL);
        Dog_Hold_R.SetActive(state == DogState.Hold && !isL);
        Dog_Eat.SetActive(state == DogState.Eat_Normal);
        Dog_Eat_Secretly.SetActive(state == DogState.Eat_Normal_Secretly);

        Dog_Hit.SetActive(state == DogState.Hit);
        if (state == DogState.Hit)
        {
            var hitState = Random.Range(1, 3);
            Dog_Hit_1.SetActive(hitState == 1);
            Dog_Hit_2.SetActive(hitState == 2);
        }
        else
        {
            Dog_Hit_1.SetActive(false);
            Dog_Hit_2.SetActive(false);
        }

        Dog_Hit_Right.SetActive(state == DogState.Hit_Right);
        Dog_Hit_Wrong.SetActive(state == DogState.Hit_Wrong);
    }

    public void ChangeDogState(DogState newState, bool isL = true)
    {
        if (dogState == newState) return;
        Log.Error($"[DogState] {dogState} -> {newState}");

        var previousState = dogState;
        ChangeDogSpriteState(newState, isL);

        UpdateCameraShake(newState);

        switch (newState)
        {
            case DogState.Normal:
                StopEatSfx();
                ReturnToOriginalPosition();
                break;
            case DogState.Eat_Secretly_1:
                DogEatSecretly().Coroutine();
                break;
            case DogState.Eat_Secretly_2:
                break;
            case DogState.Eat_Secretly_3:
                StartEatSfx();
                break;
            case DogState.Eat_Secretly_4:
                StopEatSfx();
                break;
            case DogState.Hold:
                StopEatSfx();
                HoldDog(previousState);
                break;
            case DogState.Hit:
                StopEatSfx();
                HitDog().Coroutine();
                break;
            case DogState.Hit_Right:
                HitDogRight().Coroutine();
                break;
            case DogState.Hit_Wrong:
                HitDogWrong().Coroutine();
                break;
            case DogState.Eat_Normal:
                StartEatSfx();
                break;
            case DogState.Eat_Normal_Secretly:
                if (previousState != DogState.Eat_Secretly_1)
                    DogEatSecretly().Coroutine();
                StartEatSfx();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    // ========== Eating1 用 AudioSource loop，Start/Stop 直接控制 ==========
    private void StartEatSfx()
    {
        Scene.GetComponent<AudioManagerComponent>()?.Play(SFXType.Eating1, Dog?.position).Coroutine();
    }

    // PauseEatSfx / ResumeEatSfx 保留供外部调用，实际等同于 Stop/Start
    public void PauseEatSfx()
    {
        Scene.GetComponent<AudioManagerComponent>()?.StopEating();
    }

    public void ResumeEatSfx()
    {
        Scene.GetComponent<AudioManagerComponent>()?.Play(SFXType.Eating1, Dog?.position).Coroutine();
    }

    private void StopEatSfx()
    {
        Scene.TimerComponent.Net.Remove(ref _eatSfxTimer);
        Scene.GetComponent<AudioManagerComponent>()?.StopEating();
    }

    private void ReturnToOriginalPosition()
    {
        if (Dog == null) return;
        _returnTween?.Kill();
        if (Vector3.Distance(Dog.position, _originalPosition) < 0.01f)
            return;
        var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
        _returnTween = Dog.DOMove(_originalPosition, duration).SetEase(Ease.Linear);
    }

    private void UpdateCameraShake(DogState newState)
    {
        var cameraShake = Scene.GetComponent<CameraShakeComponent>();
        if (cameraShake == null)
        {
            Log.Error("[DogState] CameraShakeComponent is NULL!");
            return;
        }

        if (newState == DogState.Eat_Secretly_3 || newState == DogState.Hit)
            cameraShake.StartShake();
        else
            cameraShake.StopShake();
    }

    public void TriggerHit()
    {
        wasSecretlyEatingWhenHit =
            dogState == DogState.Eat_Secretly_1 ||
            dogState == DogState.Eat_Secretly_2 ||
            dogState == DogState.Eat_Secretly_3 ||
            dogState == DogState.Eat_Secretly_4 ||
            dogState == DogState.Eat_Normal_Secretly;

        ChangeDogState(DogState.Hit); 
    }

    public void HoldDog(DogState previousState)
    {
        if (previousState == DogState.Eat_Secretly_1 ||
            previousState == DogState.Eat_Secretly_2 ||
            previousState == DogState.Eat_Secretly_3 ||
            previousState == DogState.Eat_Secretly_4 ||
            previousState == DogState.Eat_Normal_Secretly ||
            previousState == DogState.Eat_Normal)
        {
            CancelCurrentEating();
        }
    }

    public async FTask HitDog()
    {
        isInHit = true;
        CancelCurrentEating();

        hitCancellationToken?.Cancel();
        hitCancellationToken = FCancellationToken.ToKen;

        await Scene.TimerComponent.Net.WaitAsync(500, hitCancellationToken);
        if (hitCancellationToken.IsCancel)
        {
            isInHit = false;
            return;
        }

        isInHit = false;
        ChangeDogState(wasSecretlyEatingWhenHit ? DogState.Hit_Right : DogState.Hit_Wrong);
    }

    public async FTask HitDogRight()
    {
        isInHit = true;
        CancelCurrentEating();

        hitCancellationToken?.Cancel();
        hitCancellationToken = FCancellationToken.ToKen;

        // 打对播 hurt
        Scene.EventComponent.Publish(new PlaySFX
        {
            Type = SFXType.Hurt,
            WorldPos = Dog?.position
        });

        await Scene.TimerComponent.Net.WaitAsync(5000, hitCancellationToken);
        if (hitCancellationToken.IsCancel)
            return;

        isInHit = false;
        ChangeDogState(DogState.Normal);

        // 打对后3秒内不进入偷瞄
        _peekCooldown = true;
        Scene.TimerComponent.Net.OnceTimer(3000, () => _peekCooldown = false);
    }

    public async FTask HitDogWrong()
    {
        isInHit = true;
        hitCancellationToken?.Cancel();
        hitCancellationToken = FCancellationToken.ToKen;

        // 打错播 HitWrongBruh
        Scene.EventComponent.Publish(new PlaySFX
        {
            Type = SFXType.HitWrongBruh,
            WorldPos = Dog?.position
        });

        // 打错立刻扣分，不等动画播完
        var scoreConfig = Scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        var scoreComp = Scene.GetComponent<ScoreComponent>();
        Vector3 dogPos = Dog?.position ?? Vector3.zero;
        scoreComp?.AddScore(scoreConfig.WrongHitPenalty, 0, dogPos);

        await Scene.TimerComponent.Net.WaitAsync(5000, hitCancellationToken);
        if (hitCancellationToken.IsCancel) return;

        isInHit = false;
        ChangeDogState(DogState.Normal);
    }

    private void CancelCurrentEating()
    {
        cancellationToken?.Cancel();
        currentRotateTween?.Kill();

        if (CurEatFoodData.Item1 == FoodType.Shit)
        {
            Scene.GetComponent<FoodManagerComponent>().CancelEatShit();
        }
        else if (CurEatFoodData.Item1 != FoodType.None)
        {
            Scene.EventComponent.Publish(new CancelFoodEaten
            {
                fruitId = CurEatFoodData.Item2
            });
        }

        CurEatFoodData = (FoodType.None, 0);
    }

    public void FoodBeEatenNormal()
    {
        ChangeDogState(DogState.Normal);
    }

    public async FTask FoodBeEatenSecretly()
    {
        if (dogState == DogState.Eat_Secretly_3)
        {
            ChangeDogState(DogState.Eat_Secretly_4);
            await Scene.TimerComponent.Net.WaitAsync(1000);
            if (dogState == DogState.Eat_Secretly_4)
                ChangeDogState(DogState.Normal);
        }
    }

    public async FTask ShitBeEaten()
    {
        if (dogState == DogState.Eat_Secretly_3)
        {
            ChangeDogState(DogState.Eat_Secretly_4);
            await Scene.TimerComponent.Net.WaitAsync(1000);
            if (dogState == DogState.Eat_Secretly_4)
                ChangeDogState(DogState.Normal);
        }
    }

    private bool IsCurrentFoodInRange()
    {
        if (CurEatFoodData.Item1 == FoodType.None)
            return false;

        var tables = Scene.GetComponent<Tables>();
        float exitDistance = tables.ConstConfigCategory.FoodCheckDistance * 1.15f;

        if (CurEatFoodData.Item1 == FoodType.Shit)
        {
            var shit = Scene.GetComponent<FoodManagerComponent>().GetShit();
            if (shit == null || !shit.isLand || shit.shit == null)
                return false;
            float dist = Vector3.Distance(FoodCheckDistance_Gizmos.position, shit.shit.transform.position);
            return dist <= exitDistance;
        }
        else
        {
            var food = Scene.GetComponent<FoodManagerComponent>().GetFruitComponent(CurEatFoodData.Item2);
            if (food == null || food.Fruit_Tr == null)
                return false;
            float dist = Vector3.Distance(FoodCheckDistance_Gizmos.position, food.Fruit_Tr.position);
            return dist <= exitDistance;
        }
    }

    public void CheckFoodDistance()
    {
        if (dogState == DogState.Hit || dogState == DogState.Hold)
            return;

        if (isInHit && dogState != DogState.Hit_Right && dogState != DogState.Hit_Wrong)
            return;

        // 正在吃食物时检测食物是否离开
        if ((dogState == DogState.Eat_Normal || dogState == DogState.Eat_Normal_Secretly)
            && !IsCurrentFoodInRange())
        {
            Log.Error($"[Dog] Food moved away, cancel eating. State: {dogState}");
            CancelCurrentEating();
            ChangeDogState(DogState.Normal);
            return;
        }

        // 滞后阈值配置
        var tables = Scene.GetComponent<Tables>();
        float enterDistance = tables.ConstConfigCategory.FoodCheckDistance;
        float exitDistance = enterDistance * 1.15f;

        // 普通食物范围检测（滞后）
        var fruitType_Normal = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(FoodCheckDistance_Gizmos.position);
        float distNormal = fruitType_Normal != null && fruitType_Normal.Fruit_Tr != null
            ? Vector3.Distance(FoodCheckDistance_Gizmos.position, fruitType_Normal.Fruit_Tr.position)
            : float.MaxValue;

        if (!_foodInNormalRange && distNormal <= enterDistance)
            _foodInNormalRange = true;
        else if (_foodInNormalRange && distNormal > exitDistance)
            _foodInNormalRange = false;

        FoodType foodType_Normal = (_foodInNormalRange && fruitType_Normal != null)
            ? fruitType_Normal.foodType
            : FoodType.None;

        // 偷吃范围检测（滞后）
        FoodComponent fruitType_Secretly = null;
        float distSecretly = float.MaxValue;
        if (isOpenPeek)
        {
            fruitType_Secretly = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(FoodCheckDistance_Gizmos_Secretly.position, false);
            distSecretly = fruitType_Secretly != null && fruitType_Secretly.Fruit_Tr != null
                ? Vector3.Distance(FoodCheckDistance_Gizmos_Secretly.position, fruitType_Secretly.Fruit_Tr.position)
                : float.MaxValue;

            if (!_foodInSecretlyRange && distSecretly <= enterDistance)
                _foodInSecretlyRange = true;
            else if (_foodInSecretlyRange && distSecretly > exitDistance)
                _foodInSecretlyRange = false;
        }
        else
        {
            _foodInSecretlyRange = false;
        }

        FoodType foodType_Secretly = (_foodInSecretlyRange && fruitType_Secretly != null)
            ? fruitType_Secretly.foodType
            : FoodType.None;

        // 粪便检测（滞后）
        ShitComponent shitComponent = null;
        if (isOpenPeek)
        {
            shitComponent = Scene.GetComponent<FoodManagerComponent>().GetShit();
            if (shitComponent != null && shitComponent.shit != null)
            {
                float shitDist = Vector3.Distance(FoodCheckDistance_Gizmos_Secretly.position, shitComponent.shit.transform.position);
                if (!_foodInSecretlyRange && shitDist <= enterDistance)
                    _foodInSecretlyRange = true;
                else if (_foodInSecretlyRange && shitDist > exitDistance && distSecretly > exitDistance)
                    _foodInSecretlyRange = false;
            }
        }

        // 更新协程可读取的 flag
        _normalFoodPresent = (foodType_Normal != FoodType.None);
        _nearestNormalFoodId = _normalFoodPresent ? fruitType_Normal.Id : 0;
        _nearestNormalFoodType = _normalFoodPresent ? foodType_Normal : FoodType.None;

        // 待机状态
        if (dogState == DogState.Normal)
        {
            if (foodType_Normal != FoodType.None)
            {
                if (foodType_Secretly == FoodType.None && shitComponent == null)
                {
                    ChangeDogState(DogState.Eat_Normal);
                    Scene.EventComponent.Publish(new StartEatFood
                    {
                        fruitId = fruitType_Normal.Id,
                        isNormal = true
                    });
                    CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
                }
                else
                {
                    ChangeDogState(DogState.Eat_Normal_Secretly);
                    Scene.EventComponent.Publish(new StartEatFood
                    {
                        fruitId = fruitType_Normal.Id,
                        isNormal = true
                    });
                    CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
                }
            }
            else if (foodType_Secretly != FoodType.None || shitComponent != null)
            {
                ChangeDogState(DogState.Eat_Secretly_1);
            }
        }
        // 正常咀嚼
        else if (dogState == DogState.Eat_Normal)
        {
            if (foodType_Normal != FoodType.None)
            {
                if (foodType_Secretly == FoodType.None && shitComponent == null)
                {
                    if (fruitType_Normal.Id != CurEatFoodData.Item2)
                    {
                        Scene.EventComponent.Publish(new CancelFoodEaten
                        {
                            fruitId = CurEatFoodData.Item2
                        });
                        Scene.EventComponent.Publish(new StartEatFood
                        {
                            fruitId = fruitType_Normal.Id,
                            isNormal = true
                        });
                        CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
                    }
                }
                else
                {
                    ChangeDogState(DogState.Eat_Normal_Secretly);
                    if (shitComponent != null)
                    {
                        if (CurEatFoodData.Item1 != FoodType.Shit)
                        {
                            Scene.EventComponent.Publish(new CancelFoodEaten
                            {
                                fruitId = CurEatFoodData.Item2
                            });
                            Scene.EventComponent.Publish(new StartEatShit());
                            CurEatFoodData = (FoodType.Shit, shitComponent.Id);
                        }
                    }
                    else if (fruitType_Normal.Id != CurEatFoodData.Item2)
                    {
                        Scene.EventComponent.Publish(new CancelFoodEaten
                        {
                            fruitId = CurEatFoodData.Item2
                        });
                        Scene.EventComponent.Publish(new StartEatFood
                        {
                            fruitId = fruitType_Normal.Id,
                            isNormal = true
                        });
                        CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
                    }
                }
            }
            else if (foodType_Secretly != FoodType.None || shitComponent != null)
            {
                ChangeDogState(DogState.Eat_Secretly_1);
            }
        }
        // 偷吃状态（Eat_Secretly_2/3/4）：屏蔽玩家喂食
        else if (dogState == DogState.Eat_Secretly_2 ||
                 dogState == DogState.Eat_Secretly_3 ||
                 dogState == DogState.Eat_Secretly_4)
        {
            return;
        }
        // 正确/错误被打状态下，玩家喂食可打断
        else if (dogState == DogState.Hit_Right || dogState == DogState.Hit_Wrong)
        {
            if (foodType_Normal != FoodType.None)
            {
                hitCancellationToken?.Cancel();
                isInHit = false;

                var targetState = (foodType_Secretly == FoodType.None && shitComponent == null)
                    ? DogState.Eat_Normal
                    : DogState.Eat_Normal_Secretly;

                ChangeDogState(targetState);

                Scene.EventComponent.Publish(new StartEatFood
                {
                    fruitId = fruitType_Normal.Id,
                    isNormal = true
                });
                CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
            }
        }
    }
    

public async FTask DogEatSecretly()
{
    cancellationToken?.Cancel();
    cancellationToken = FCancellationToken.ToKen;

    // ===== 偷瞄阶段（Eat_Secretly_1 / Eat_Normal_Secretly）=====
    var perDuration = Scene.GetComponent<Level_1_Component>().GetDogEatSecretlyPerDuration();
    var elapsed = 0L;
    var pollInterval = 100L;

    while (elapsed < perDuration)
    {
        var waitTime = Math.Min(pollInterval, perDuration - elapsed);
        await Scene.TimerComponent.Net.WaitAsync(waitTime, cancellationToken);
        if (cancellationToken.IsCancel) return;

        elapsed += waitTime;

        if (_normalFoodPresent)
        {
            CancelCurrentEating();

            var hasSecretFood = CheckSecretFoodExists();
            var targetState = hasSecretFood ? DogState.Eat_Normal_Secretly : DogState.Eat_Normal;

            if (dogState != targetState)
                ChangeDogState(targetState);

            Scene.EventComponent.Publish(new StartEatFood
            {
                fruitId = _nearestNormalFoodId,
                isNormal = true
            });
            CurEatFoodData = (_nearestNormalFoodType, _nearestNormalFoodId);
            return;
        }
    }

    if (_normalFoodPresent)
    {
        CancelCurrentEating();
        var hasSecretFood = CheckSecretFoodExists();
        var targetState = hasSecretFood ? DogState.Eat_Normal_Secretly : DogState.Eat_Normal;
        if (dogState != targetState)
            ChangeDogState(targetState);
        Scene.EventComponent.Publish(new StartEatFood
        {
            fruitId = _nearestNormalFoodId,
            isNormal = true
        });
        CurEatFoodData = (_nearestNormalFoodType, _nearestNormalFoodId);
        return;
    }

    // ===== 没有玩家食物，进入偷吃移动阶段（Eat_Secretly_2）=====
    SetIsOpenPeek(false);
    ChangeDogState(DogState.Eat_Secretly_2);

    // 新增：播放移动音效
    Scene.GetComponent<AudioManagerComponent>()?.Play(SFXType.Werwerwer, Dog?.position).Coroutine();

    var shitComponent_1 = Scene.GetComponent<FoodManagerComponent>().GetShit();
    if (shitComponent_1 != null)
    {
        var target = shitComponent_1.shit.transform;
        Vector3 direction = target.position - Dog.position;

        currentRotateTween?.Kill();
        float moveDistance = 1f;
        Vector3 moveDirection = direction.normalized * moveDistance;
        var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
        currentRotateTween = Dog.DOMove(Dog.position + moveDirection, duration).SetEase(Ease.Linear);

        await Scene.TimerComponent.Net.WaitAsync((long)(duration * 1000), cancellationToken);
        if (cancellationToken.IsCancel) return;
    }
    else
    {
        var fruitType_Secretly_1 = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(Dog.position, false);
        if (fruitType_Secretly_1 != null && fruitType_Secretly_1.foodType != FoodType.None)
        {
            var target = fruitType_Secretly_1.Fruit_Tr;
            Vector3 direction = target.position - Dog.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle -= 90f;
            Vector3 targetRotation = new Vector3(0, 0, angle);

            currentRotateTween?.Kill();
            float moveDistance = 1f;
            Vector3 moveDirection = direction.normalized * moveDistance;
            var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
            currentRotateTween = Dog.DOMove(Dog.position + moveDirection, duration).SetEase(Ease.Linear);

            await Scene.TimerComponent.Net.WaitAsync((long)(duration * 1000), cancellationToken);
            if (cancellationToken.IsCancel) return;
        }
        else
        {
            ChangeDogState(DogState.Normal);
            return;
        }
    }

    // 新增：停止移动音效
    Scene.GetComponent<AudioManagerComponent>()?.StopWerwerwer();

    // ===== 到达食物，开始偷吃（Eat_Secretly_3）=====
    ChangeDogState(DogState.Eat_Secretly_3);
    var shitComponent_2 = Scene.GetComponent<FoodManagerComponent>().GetShit();
    if (shitComponent_2 != null)
    {
        Scene.EventComponent.Publish(new StartEatShit());
        CurEatFoodData = (FoodType.Shit, shitComponent_2.Id);
    }
    else
    {
        var fruitType_Secretly_2 = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(Dog.position, false);
        if (fruitType_Secretly_2 != null && fruitType_Secretly_2.foodType != FoodType.None)
        {
            Scene.EventComponent.Publish(new StartEatFood()
            {
                fruitId = fruitType_Secretly_2.Id,
                isNormal = false
            });
            CurEatFoodData = (fruitType_Secretly_2.foodType, fruitType_Secretly_2.Id);
        }
        else
        {
            ChangeDogState(DogState.Normal);
        }
    }
}

    /// <summary>
    /// 检查当前偷吃范围内是否有偷食目标（供协程内部判断用）
    /// </summary>
    private bool CheckSecretFoodExists()
    {
        if (!isOpenPeek) return false;

        var shitComponent = Scene.GetComponent<FoodManagerComponent>().GetShit();
        if (shitComponent != null) return true;

        var tables = Scene.GetComponent<Tables>();
        float enterDistance = tables.ConstConfigCategory.FoodCheckDistance;

        var fruitSecretly = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(FoodCheckDistance_Gizmos_Secretly.position, false);
        if (fruitSecretly != null && fruitSecretly.Fruit_Tr != null)
        {
            float dist = Vector3.Distance(FoodCheckDistance_Gizmos_Secretly.position, fruitSecretly.Fruit_Tr.position);
            return dist <= enterDistance * 1.15f;
        }

        return false;
    }

    /// <summary>
    /// 清理所有动画和令牌（供 DestroySystem 调用）
    /// </summary>
    public void Cleanup()
    {
        currentRotateTween?.Kill();
        _returnTween?.Kill();
        cancellationToken?.Cancel();
        StopEatSfx();
    }
}

public class DogControlComponent_Awake : AwakeSystem<DogControlComponent>
{
    protected override void Awake(DogControlComponent self)
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        self.Dog = rc.Get<Transform>("Dog");
        self.Dog_Front = rc.Get<GameObject>("Dog_Front");
        self.Dog_1 = rc.Get<GameObject>("Dog_1");
        self.Dog_2 = rc.Get<GameObject>("Dog_2");
        self.Dog_3 = rc.Get<GameObject>("Dog_3");
        self.Dog_4 = rc.Get<GameObject>("Dog_4");
        self.Dog_Hold_L = rc.Get<GameObject>("Dog_Hold_L");
        self.Dog_Hold_R = rc.Get<GameObject>("Dog_Hold_R");
        self.Dog_Eat = rc.Get<GameObject>("Dog_Eat");
        self.Dog_Eat_Secretly = rc.Get<GameObject>("Dog_Eat_Secretly");
        self.Dog_Hit = rc.Get<GameObject>("Dog_Hit");
        self.Dog_Hit_1 = rc.Get<GameObject>("Dog_Hit_1");
        self.Dog_Hit_2 = rc.Get<GameObject>("Dog_Hit_2");
        self.Dog_Hit_Right = rc.Get<GameObject>("Dog_Hit_Right");
        self.Dog_Hit_Wrong = rc.Get<GameObject>("Dog_Hit_Wrong");
        self.FoodCheckDistance_Gizmos = rc.Get<Transform>("FoodCheckDistance_Gizmos");
        self.FoodCheckDistance_Gizmos_Secretly = rc.Get<Transform>("FoodCheckDistance_Gizmos_Secretly");

        self.Init();
    }
}

public class DogControlComponent_Destroy : DestroySystem<DogControlComponent>
{
    protected override void Destroy(DogControlComponent self)
    {
        self.Cleanup();
        self.Scene.TimerComponent.Net.Remove(ref self.Timer);
    }
}

public class DogControlComponent_Update : UpdateSystem<DogControlComponent>
{
    protected override void Update(DogControlComponent self)
    {
        self.CheckFoodDistance();

        var level = self.Scene.GetComponent<Level_1_Component>();
        if (level != null && level.Level_Stage >= 2 && !self.isOpenPeek && self.Timer == 0 && !self._peekCooldown)
        {
            self.AddEatSecretlyTimer();
        }
    }
}