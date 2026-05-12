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

    public DogState dogState = DogState.Normal;

    public FCancellationToken cancellationToken;

    /// <summary>
    /// 包含屎
    /// </summary>
    public (FoodType, long) CurEatFoodData;

    public long Timer;

    public bool isInHit = false;

    public Tweener currentRotateTween;
    public Transform FoodCheckDistance_Gizmos;
    public Transform FoodCheckDistance_Gizmos_Secretly;

    public bool isOpenPeek = false;

    public void Init()
    {
        ChangeDogSpriteState(DogState.Normal);
        AddEatSecretlyTimer();
    }

    public void AddEatSecretlyTimer()
    {
        var dura = Scene.GetComponent<Level_1_Component>().GetDogEatSecretlyDuration();
        Log.Error($"dura : {dura}");
        if (dura <= 0)
            return;
        Timer = Scene.TimerComponent.Net.OnceTimer(dura, () => SetIsOpenPeek(true));
    }

    public void SetIsOpenPeek(bool isOpen)
    {
        Log.Error($"SetIsOpenPeek {isOpen}");
        if (isOpenPeek)
            Scene.TimerComponent.Net.Remove(ref Timer);
        isOpenPeek = isOpen;
        if (!isOpenPeek)
            AddEatSecretlyTimer();
    }

    public void ChangeDogSpriteState(DogState dogState, bool isL = true)
    {
        this.dogState = dogState;
        Dog_Front.SetActive(dogState == DogState.Normal);
        Dog_1.SetActive(dogState == DogState.Eat_Secretly_1);
        Dog_2.SetActive(dogState == DogState.Eat_Secretly_2);
        Dog_3.SetActive(dogState == DogState.Eat_Secretly_3);
        Dog_4.SetActive(dogState == DogState.Eat_Secretly_4);
        Dog_Hold_L.SetActive(dogState == DogState.Hold && isL);
        Dog_Hold_R.SetActive(dogState == DogState.Hold && !isL);
        Dog_Eat.SetActive(dogState == DogState.Eat_Normal);
        Dog_Eat_Secretly.SetActive(dogState == DogState.Eat_Normal_Secretly);
        Dog_Hit.SetActive(dogState == DogState.Hit);
        if (dogState == DogState.Hit)
        {
            var hitState = Random.Range(1, 3);
            Dog_Hit_1.SetActive(hitState == 1);
            Dog_Hit_2.SetActive(hitState == 2);
        }
    }

    public void ChangeDogState(DogState DogState, bool isL = true)
    {
        if (dogState == DogState)
            return;
        Log.Error($"ChangeDogState {dogState}, {DogState}");
        ChangeDogSpriteState(DogState, isL);
        switch (dogState)
        {
            case DogState.Normal:
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
                HoldDog();
                break;
            case DogState.Hit:
                HitDog().Coroutine();
                break;
            case DogState.Eat_Normal:
                break;
            case DogState.Eat_Normal_Secretly:
                DogEatSecretly().Coroutine();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dogState), dogState, null);
        }
    }

    public void HoldDog()
    {
        //上个状态为Eat_Norma，则打断进食倒计时
        if (dogState == DogState.Eat_Normal || dogState == DogState.Eat_Normal_Secretly)
        {
            cancellationToken?.Cancel();
            if (CurEatFoodData.Item1 != FoodType.None)
            {
                Scene.EventComponent.Publish(new CancelFoodEaten
                {
                    fruitId = CurEatFoodData.Item2
                });
                CurEatFoodData = (FoodType.None, 0);
            }
        }
    }

    public async FTask HitDog()
    {
        isInHit = true;
        if (CurEatFoodData.Item1 != FoodType.None)
        {
            Scene.EventComponent.Publish(new CancelFoodEaten
            {
                fruitId = CurEatFoodData.Item2
            });
            CurEatFoodData = (FoodType.None, 0);
        }

        await Scene.TimerComponent.Net.WaitAsync(Scene.GetComponent<Tables>().ConstConfigCategory.HitDuration);
        //如果还是hit状态则切换为none
        isInHit = false;
        ChangeDogState(DogState.Normal);
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

    /// <summary>
    /// 检测食物距离
    /// </summary>
    /// <param name="self"></param>
    public void CheckFoodDistance()
    {
        if (dogState == DogState.Hit || dogState == DogState.Hold)
            return;
        if (isInHit)
            return;
        //优先检测偷瞄的食物
        var fruitType_Normal = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(FoodCheckDistance_Gizmos.position);
        FoodComponent fruitType_Secretly = null;
        if (isOpenPeek)
            fruitType_Secretly = Scene.GetComponent<FoodManagerComponent>().GetMinFruitDistance(FoodCheckDistance_Gizmos_Secretly.position, false);
        FoodType foodType_Normal = fruitType_Normal?.foodType ?? FoodType.None;
        FoodType foodType_Secretly = fruitType_Secretly?.foodType ?? FoodType.None;
        ShitComponent shitComponent = null;
        if(isOpenPeek)
            shitComponent = Scene.GetComponent<FoodManagerComponent>().GetShit();
        // if (foodType_Normal != FoodType.None || foodType_Secretly != FoodType.None)
        //     Log.Error($"foodType_Normal {foodType_Normal}, foodType_Secretly {foodType_Secretly} ");
        //待机状态
        if (dogState == DogState.Normal)
        {
            //喂食
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
                    //咀嚼偷瞄
                    ChangeDogState(DogState.Eat_Normal_Secretly);
                    Scene.EventComponent.Publish(new StartEatFood
                    {
                        fruitId = fruitType_Normal.Id,
                        isNormal = true
                    });
                    CurEatFoodData = (fruitType_Normal.foodType, fruitType_Normal.Id);
                }
            }
            //偷瞄
            else if (foodType_Secretly != FoodType.None || shitComponent != null)
            {
                ChangeDogState(DogState.Eat_Secretly_1);
            }
        }
        //咀嚼
        else if (dogState == DogState.Eat_Normal)
        {
            //喂食
            if (foodType_Normal != FoodType.None)
            {
                if (foodType_Secretly == FoodType.None && shitComponent == null)
                {
                    //切换食物
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
                    //切换食物
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
    }

    public async FTask DogEatSecretly()
    {
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
            // --- 第1步：计算目标方向的角度 ---
            Vector3 direction = target.position - Dog_2.transform.position; // 计算从当前物体指向目标的方向向量
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 计算角度（0度指向右）
            angle -= 90f; // 如果想让物体的上方（transform.up）指向目标，需要减去90°偏移

            // --- 第2步：使用DOTween平滑地旋转到目标角度 ---
            Vector3 targetRotation = new Vector3(0, 0, angle); // 创建目标欧拉角

            // 如果已经有正在播放的旋转动画，则先终止它，以避免动画冲突
            currentRotateTween?.Kill();

            var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
            currentRotateTween = Dog_2.transform.DORotate(targetRotation, duration).SetEase(Ease.Linear);
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
                // --- 第1步：计算目标方向的角度 ---
                Vector3 direction = target.position - Dog_2.transform.position; // 计算从当前物体指向目标的方向向量
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 计算角度（0度指向右）
                angle -= 90f; // 如果想让物体的上方（transform.up）指向目标，需要减去90°偏移

                // --- 第2步：使用DOTween平滑地旋转到目标角度 ---
                Vector3 targetRotation = new Vector3(0, 0, angle); // 创建目标欧拉角

                // 如果已经有正在播放的旋转动画，则先终止它，以避免动画冲突
                currentRotateTween?.Kill();

                var duration = Scene.GetComponent<Tables>().ConstConfigCategory.TurnRotateToFoodDuration;
                currentRotateTween = Dog_2.transform.DORotate(targetRotation, duration).SetEase(Ease.Linear);
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
        self.FoodCheckDistance_Gizmos = rc.Get<Transform>("FoodCheckDistance_Gizmos");
        self.FoodCheckDistance_Gizmos_Secretly = rc.Get<Transform>("FoodCheckDistance_Gizmos_Secretly");

        self.Init();
    }
}

public class DogControlComponent_Destroy : DestroySystem<DogControlComponent>
{
    protected override void Destroy(DogControlComponent self)
    {
        self.currentRotateTween?.Kill();
        self.cancellationToken?.Cancel();
        self.Scene.TimerComponent.Net.Remove(ref self.Timer);
    }
}

public class DogControlComponent_Update : UpdateSystem<DogControlComponent>
{
    protected override void Update(DogControlComponent self)
    {
        self.CheckFoodDistance();
    }
}