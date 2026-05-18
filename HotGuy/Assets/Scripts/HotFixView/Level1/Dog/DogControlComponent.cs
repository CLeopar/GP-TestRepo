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

    // ========== 滞后检测：避免边缘抖动导致状态反复切换 ==========
    private bool _foodInNormalRange = false;
    private bool _foodInSecretlyRange = false;

    // Hit 动画期间的取消令牌
    private FCancellationToken hitCancellationToken;

    // 记录进入 Hit 状态时是否在偷吃（用于后续判定正确/错误）
    private bool wasSecretlyEatingWhenHit;

    // ========== 新增：记录狗的初始位置，用于回归 ==========
    private Vector3 _originalPosition;
    private Tweener _returnTween;

    public void Init()
    {
        // 记录初始位置
        if (Dog != null)
            _originalPosition = Dog.position;
        
        ChangeDogSpriteState(DogState.Normal);
        AddEatSecretlyTimer();
    }

    public void AddEatSecretlyTimer()
    {
        var levelComponent = Scene.GetComponent<Level_1_Component>();
        var dura = levelComponent.GetDogEatSecretlyDuration();
        if (dura <= 0) return;
        Timer = Scene.TimerComponent.Net.OnceTimer(dura, () => SetIsOpenPeek(true));
    }

    public void SetIsOpenPeek(bool isOpen)
    {
        if (isOpenPeek)
            Scene.TimerComponent.Net.Remove(ref Timer);
        isOpenPeek = isOpen;
        if (!isOpenPeek)
            AddEatSecretlyTimer();
    }

    public void ChangeDogSpriteState(DogState state, bool isL = true)
    {
        // ========== 新增：防止重复设置导致闪烁 ==========
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

        // Hit 状态：随机显示 Hit_1 或 Hit_2
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

        // 统一的震动控制：只有 Eat_Secretly_3 震动，其他状态都停止
        UpdateCameraShake(newState);

        switch (newState)
        {
            case DogState.Normal:
                // ========== 新增：回到初始位置 ==========
                ReturnToOriginalPosition();
                break;
            case DogState.Eat_Secretly_1:
                DogEatSecretly().Coroutine();
                break;
            case DogState.Eat_Secretly_2:
                break;
            case DogState.Eat_Secretly_3:
                break;
            case DogState.Eat_Secretly_4:
                break;
            case DogState.Hold:
                HoldDog(previousState);
                break;
            case DogState.Hit:
                HitDog().Coroutine();
                break;
            case DogState.Hit_Right:
                HitDogRight().Coroutine();
                break;
            case DogState.Hit_Wrong:
                HitDogWrong().Coroutine();
                break;
            case DogState.Eat_Normal:
                break;
            case DogState.Eat_Normal_Secretly:
                if (previousState != DogState.Eat_Secretly_1)
                    DogEatSecretly().Coroutine();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    /// <summary>
    /// 回到初始位置（平滑移动）
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        if (Dog == null) return;

        // 取消之前的回归动画
        _returnTween?.Kill();

        // 如果已经在初始位置附近，不需要移动
        if (Vector3.Distance(Dog.position, _originalPosition) < 0.01f)
            return;

        var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
        _returnTween = Dog.DOMove(_originalPosition, duration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 统一的震动控制：只有狗在偷吃食物（Secretly_3）时才震动
    /// </summary>
    private void UpdateCameraShake(DogState newState)
    {
        var cameraShake = Scene.GetComponent<CameraShakeComponent>();
        if (cameraShake == null) 
        {
            Log.Error("[DogState] CameraShakeComponent is NULL!");
            return;
        }

        bool shouldShake = (newState == DogState.Eat_Secretly_3);

        if (shouldShake)
        {
            cameraShake.StartShake();
        }
        else
        {
            cameraShake.StopShake();
        }
    }

    /// <summary>
    /// 外部打狗入口：先播放 Hit 动画，再判定正确/错误
    /// </summary>
    public void TriggerHit()
    {
        // 记录当前是否在偷吃，用于后续判定
        wasSecretlyEatingWhenHit =
            dogState == DogState.Eat_Secretly_1 ||
            dogState == DogState.Eat_Secretly_2 ||
            dogState == DogState.Eat_Secretly_3 ||
            dogState == DogState.Eat_Secretly_4 ||
            dogState == DogState.Eat_Normal_Secretly;

        // 先进入 Hit 状态（播放随机挨打动画）
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

    /// <summary>
    /// 统一的挨打动画（先执行这个，然后判定正确/错误）
    /// </summary>
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

        // 1秒结束后，根据之前记录的判定结果切换状态
        isInHit = false;
        ChangeDogState(wasSecretlyEatingWhenHit ? DogState.Hit_Right : DogState.Hit_Wrong);
    }

    public async FTask HitDogRight()
    {
        isInHit = true;

        hitCancellationToken?.Cancel();
        hitCancellationToken = FCancellationToken.ToKen;

        await Scene.TimerComponent.Net.WaitAsync(5000, hitCancellationToken);
        if (hitCancellationToken.IsCancel)
            return;

        isInHit = false;
        ChangeDogState(DogState.Normal);
    }

    public async FTask HitDogWrong()
    {
        isInHit = true;
        hitCancellationToken?.Cancel();
        hitCancellationToken = FCancellationToken.ToKen;

        await Scene.TimerComponent.Net.WaitAsync(5000, hitCancellationToken);
        if (hitCancellationToken.IsCancel) return;

        var scoreConfig = Scene.GetComponent<Tables>().ScoreConfigCategory.Data;
        var scoreComp = Scene.GetComponent<ScoreComponent>();

        // ========== 获取狗的位置 ==========
        Vector3 dogPos = Dog?.position ?? Vector3.zero;

        scoreComp?.AddScore(scoreConfig.WrongHitPenalty, 0, dogPos);

        isInHit = false;
        ChangeDogState(DogState.Normal);
    }

    /// <summary>
    /// 统一取消当前进食状态
    /// </summary>
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

    // ========== 检查当前食物是否还在检测范围内（使用滞后阈值，只用大阈值判断离开） ==========
    private bool IsCurrentFoodInRange()
    {
        if (CurEatFoodData.Item1 == FoodType.None)
            return false;

        var tables = Scene.GetComponent<Tables>();
        float exitDistance = tables.ConstConfigCategory.FoodCheckDistance * 1.15f; // 离开阈值比进入大15%

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

    /// <summary>
    /// 检测食物距离（使用滞后阈值，避免边缘抖动导致状态反复切换）
    /// </summary>
    public void CheckFoodDistance()
    {
        if (dogState == DogState.Hit || dogState == DogState.Hold)
            return;

        if (isInHit && dogState != DogState.Hit_Right && dogState != DogState.Hit_Wrong)
            return;

        // 正在吃食物时，检测是否远离（使用滞后阈值的大阈值判断离开）
        if ((dogState == DogState.Eat_Normal || dogState == DogState.Eat_Normal_Secretly) 
            && !IsCurrentFoodInRange())
        {
            Log.Error($"[Dog] Food moved away, cancel eating. State: {dogState}");
            CancelCurrentEating();
            ChangeDogState(DogState.Normal);
            return;
        }

        // ========== 滞后阈值配置 ==========
        var tables = Scene.GetComponent<Tables>();
        float enterDistance = tables.ConstConfigCategory.FoodCheckDistance;
        float exitDistance  = enterDistance * 1.15f; // 离开阈值比进入大15%，形成缓冲带

        // ========== 普通范围：用滞后逻辑判断是否"在范围内" ==========
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

        // ========== 偷吃范围：用滞后逻辑判断是否"在范围内" ==========
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

        // 检查粪便（粪便也使用滞后阈值）
        ShitComponent shitComponent = null;
        if (isOpenPeek)
        {
            shitComponent = Scene.GetComponent<FoodManagerComponent>().GetShit();
            if (shitComponent != null && shitComponent.shit != null)
            {
                float shitDist = Vector3.Distance(FoodCheckDistance_Gizmos_Secretly.position, shitComponent.shit.transform.position);
                // 粪便的"在范围内"逻辑：用同一个 _foodInSecretlyRange 状态（简化处理）
                // 如果粪便在范围内但水果不在，仍然认为 secret range 有效
                if (!_foodInSecretlyRange && shitDist <= enterDistance)
                    _foodInSecretlyRange = true;
                else if (_foodInSecretlyRange && shitDist > exitDistance && distSecretly > exitDistance)
                    _foodInSecretlyRange = false;
            }
        }

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
        // 偷瞄（Eat_Secretly_1）及 Eat_Normal_Secretly：可以响应玩家喂食
        else if (dogState == DogState.Eat_Secretly_1 ||
                 dogState == DogState.Eat_Normal_Secretly)
        {
            if (foodType_Normal != FoodType.None)
            {
                if (CurEatFoodData.Item2 == fruitType_Normal.Id)
                    return;

                CancelCurrentEating();

                var targetState = (foodType_Secretly == FoodType.None && shitComponent == null)
                    ? DogState.Eat_Normal
                    : DogState.Eat_Normal_Secretly;

                if (dogState == targetState) return;

                ChangeDogState(targetState);

                Scene.EventComponent.Publish(new StartEatFood
                {
                    fruitId = fruitType_Normal.Id,
                    isNormal = true
                });
                CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
            }
            else if (dogState == DogState.Eat_Normal_Secretly)
            {
                // 玩家食物消失，从偷吃偷瞄退回纯偷瞄，由协程接管后续流程
                CancelCurrentEating();
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

        await Scene.TimerComponent.Net.WaitAsync(Scene.GetComponent<Level_1_Component>().GetDogEatSecretlyPerDuration(), cancellationToken);
        if (cancellationToken.IsCancel)
            return;
        SetIsOpenPeek(false);
        ChangeDogState(DogState.Eat_Secretly_2);

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
            if (cancellationToken.IsCancel)
                return;
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
                if (cancellationToken.IsCancel)
                    return;
            }
            else
            {
                ChangeDogState(DogState.Normal);
                return;
            }
        }

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
                ChangeDogState(DogState.Normal);
        }
    }

    /// <summary>
    /// 清理所有动画和令牌（供 DestroySystem 调用）
    /// </summary>
    public void Cleanup()
    {
        currentRotateTween?.Kill();
        _returnTween?.Kill();
        cancellationToken?.Cancel();
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
        if (level != null && level.Level_Stage >= 2 && !self.isOpenPeek && self.Timer == 0)
        {
            self.AddEatSecretlyTimer();
        }
    }
}