using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

public enum HandType
{
    None = 0,
    Fist,
    Palm,
    Prop,
    Tissue_UnUse,
    Tissue_Used,
}

public class PlayerInputComponent : Entity, ISupportedMultiEntity
{
    public int playerIndex;
    public InputAction_Player controls;
    public Transform HandRoot;
    public SpriteRenderer HandRoot_SpriteRenderer;
    public PolygonCollider2D HandRoot_PolygonCollider2D;
    public Rigidbody2D HandRoot_Rigidbody2D;
    public HandType HandType = HandType.None;
    public bool isStayFruitsOrProps = false;

    // /// <summary>
    // /// 拳头
    // /// </summary>
    // public GameObject Hand_Fist;
    //
    // /// <summary>
    // /// 手掌
    // /// </summary>
    // public GameObject Hand_Palm;
    //
    // /// <summary>
    // /// 手拿勺子
    // /// </summary>
    // public GameObject Hand_Prop;

    public PolygonCollider2D collider_Fist;
    public PolygonCollider2D collider_Palm;
    public PolygonCollider2D collider_Prop;
    public PolygonCollider2D collider_Tissue_UnUse;
    public PolygonCollider2D collider_Tissue_Used;
    public GameObject Hand_Up;

    /// <summary>
    /// 是否切换为拳头
    /// </summary>
    public bool isChangeToFist = false;

    /// <summary>
    /// 是否已经捡起水果
    /// </summary>
    public long pickUpFruitId = 0;

    /// <summary>
    /// 是否已经捡起道具
    /// </summary>
    public bool isPickUpProp = false;

    public long pickUpTissueId = 0;

    public float PlayerSpeed { get; set; } = 5f;
    public Vector2 PlayerMove { get; set; }
    public float PlayerRotate { get; set; }

    //每秒旋转角度
    public float RotationSpeed { get; set; } = 180f;

    public float minAngle = -360f;

    public float maxAngle = 360f;

    //是否启用角度限制
    public bool clampRotation { get; set; } = true;

    //自动回正
    public bool autoReturnToCenter { get; set; } = false;

    public float returnSpeed { get; set; } = 90f;

    //回正死区
    public float deadzoneAngle { get; set; } = 90f;

    //当前角度
    public float currentAngle { get; set; } = 0f;

    //初始旋转
    public Quaternion initialRotation { get; set; }

    //触碰狗
    public bool isStayDog { get; set; } = false;

    public bool isHoldDog { get; set; } = false;

    public bool isStayShit { get; set; } = false;

    public bool isL()
    {
        return playerIndex == 0;
    }

    public void InitProperty()
    {
        var rc = GameObject.Find("Level_1").GetComponent<ReferenceCollector>();
        HandRoot = rc.Get<Transform>(isL() ? "Hand_L" : "Hand_R");
        HandRoot_SpriteRenderer = HandRoot.GetComponent<SpriteRenderer>();
        HandRoot_PolygonCollider2D = HandRoot.GetComponent<PolygonCollider2D>();
        HandRoot_Rigidbody2D = HandRoot.GetComponent<Rigidbody2D>();
        // Hand_Rotate = rc.Get<Transform>(isL() ? "Hand_Rotate_L" : "Hand_Rotate_R");
        // Hand_Fist = HandRoot.Find("Hand_Quan").gameObject;
        // Hand_Palm = HandRoot.Find("Hand_Bu").gameObject;
        // Hand_Prop = HandRoot.Find("Hand_Prop").gameObject;

        collider_Fist = HandRoot.Find("Hand_Quan").GetComponent<PolygonCollider2D>();
        collider_Palm = HandRoot.Find("Hand_Bu").GetComponent<PolygonCollider2D>();
        collider_Prop = HandRoot.Find("Hand_Prop").GetComponent<PolygonCollider2D>();
        collider_Tissue_UnUse = HandRoot.Find("Hand_Tissue_UnUse").GetComponent<PolygonCollider2D>();
        collider_Tissue_Used = HandRoot.Find("Hand_Tissue_Used").GetComponent<PolygonCollider2D>();
        Hand_Up = HandRoot.Find("Hand_Up").gameObject;
        ChangeHandType(HandType.Palm).Coroutine();

        initialRotation = HandRoot.rotation;

        var unityEventTrigger_Hand = HandRoot.GetComponent<UnityEventTrigger>();
        unityEventTrigger_Hand.Register(OnCollisionEnter2D_Hand, OnCollisionExit2D_Hand, OnCollisionStay2D_Hand);
    }

    public void OnCollisionEnter2D_Hand(Collision2D collider)
    {
        if (HandType == HandType.Prop || HandType == HandType.None)
            return;
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "Dog")
            isStayDog = true;
        if (layer == "Shit")
            isStayShit = true;
    }

    public void OnCollisionExit2D_Hand(Collision2D collider)
    {
        if (HandType == HandType.None)
            return;
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        if (layer == "Dog")
            isStayDog = false;

        if (layer == "Shit")
            isStayShit = false;

        bool isFruits = layer == "Fruits";
        bool isProps = layer == "Props";
        if (isFruits || isProps)
            isStayFruitsOrProps = false;
    }

    public void OnCollisionStay2D_Hand(Collision2D collider)
    {
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        bool isFruits = layer == "Fruits";
        bool isProps = layer == "Props";
        bool isTissueBox = layer == "TissueBox";
        bool isTissue = layer == "Tissue";
        Log.Error($"isProps {isProps}, isTissue {isTissue}");
        if (HandType == HandType.Palm && (isFruits || isProps || isTissue || isTissueBox))
            isStayFruitsOrProps = true;
        else
            isStayFruitsOrProps = false;

        if (HandType != HandType.Fist)
            return;
        //手上有东西也无法操作
        if (HandHasSomething())
            return;
        if (!isChangeToFist)
            return;
        isChangeToFist = false;
        isStayFruitsOrProps = false;
        //需要以手掌移动到食物、道具上，再切换为拳头，才能做操作
        if (isTissueBox)
        {
            PickUpTissueByBox();
            return;
        }

        if (isTissue)
        {
            PickUpTissue(collider.gameObject.name);
            return;
        }

        if (isProps)
        {
            PickUpProps(collider.gameObject.name);
            return;
        }

        if (isFruits)
        {
            if (pickUpFruitId != 0)
                return;
            var fruitName = collider.gameObject.name;
            PickUpFruit(fruitName);
        }
    }

    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        PlayerMove = context.ReadValue<Vector2>();
    }

    public void OnPlayerRotate(InputAction.CallbackContext context)
    {
        PlayerRotate = context.ReadValue<float>();
    }

    public void OnPlayerSwitchHand(InputAction.CallbackContext context)
    {
        if (HandRoot == null)
            return;
        if (isHoldDog)
        {
            isHoldDog = false;
            ChangeHandType(HandType.Palm).Coroutine();
            Scene.EventComponent.Publish(new HoldDog
            {
                isL = isL(),
                State = false
            });
            return;
        }

        if (isStayDog && !HandHasSomething() && HandType != HandType.None)
        {
            isHoldDog = true;
            ChangeHandType(HandType.None).Coroutine();
            // HandRoot.gameObject.SetActive(false);
            Scene.EventComponent.Publish(new HoldDog
            {
                isL = isL(),
                State = true
            });
            return;
        }

        if (HandType != HandType.Palm)
        {
            //拳切换为布
            ChangeHandType(HandType.Palm).Coroutine();
            DropFruit();
            DropProps();
            DropTissue();
        }
        else
        {
            ChangeHandType(HandType.Fist).Coroutine();
        }
    }

    public void OnPlayerHit(InputAction.CallbackContext context)
    {
        if (HandType == HandType.Tissue_UnUse && isStayShit)
        {
            ChangeHandType(HandType.Tissue_Used).Coroutine();
            var tissueComponent = Scene.GetComponent<TissueManagerComponent>().GetTissue(pickUpTissueId);
            if (tissueComponent != null)
                tissueComponent.ChangeState(true);
                
            Scene.GetComponent<FoodManagerComponent>().RemoveShit();
            return;
        }

        if (HandType != HandType.Fist)
            return;
        if (isStayDog)
            Scene.EventComponent.Publish(new HitDog());
        else
        {
            //获取道具
            Scene.GetComponent<FoodManagerComponent>().AddForce(HandRoot.position, 5f);
        }
    }

    public void PickUpFruit(string fruitName)
    {
        var fruitId = long.Parse(fruitName);
        Scene.GetComponent<FoodManagerComponent>().PickUpFruit(fruitId, HandRoot);
        pickUpFruitId = fruitId;
        // collider.enabled = false;
    }

    public void DropFruit()
    {
        if (pickUpFruitId == 0)
            return;
        Scene.GetComponent<FoodManagerComponent>().DropFruit(pickUpFruitId);
        pickUpFruitId = 0;
    }

    public void PickUpProps(string name)
    {
        Scene.GetComponent<PropsManagerComponent>().PickUpProp(HandRoot, name);
        // collider.enabled = false;
        isPickUpProp = true;
        ChangeHandType(HandType.Prop).Coroutine();
    }

    public void DropProps()
    {
        if (isPickUpProp)
        {
            Scene.GetComponent<PropsManagerComponent>().DropProp();
            pickUpFruitId = 0;
            // collider.enabled = true;
            isPickUpProp = false;
            ChangeHandType(HandType.Palm).Coroutine();
        }
    }

    public bool HandHasSomething()
    {
        if (isHoldDog)
            return true;
        return pickUpFruitId != 0 || isPickUpProp || pickUpTissueId != 0;
    }

    public void ApplyInputRotation()
    {
        // 计算角度变化
        float angleDelta = PlayerRotate * RotationSpeed * Time.deltaTime;
        float newAngle = currentAngle + angleDelta;

        // 应用角度限制
        if (clampRotation)
        {
            currentAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);
        }
        else
        {
            currentAngle = newAngle;
        }
    }

    public void ReturnToCenter()
    {
        // 检查是否在死区内
        if (Mathf.Abs(currentAngle) < deadzoneAngle)
        {
            currentAngle = 0f;
            return;
        }

        // 平滑返回中心
        float returnDelta = Mathf.Sign(-currentAngle) * returnSpeed * Time.deltaTime;

        // 确保不会过度返回
        if (Mathf.Abs(currentAngle + returnDelta) < Mathf.Abs(currentAngle))
        {
            currentAngle += returnDelta;
        }
        else
        {
            currentAngle = 0f;
        }
    }

    public void ApplyLimitedRotation()
    {
        // if(Hand_Rotate == null)
        //     return;
        // 应用角度到transform
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, currentAngle);
        HandRoot.rotation = targetRotation;
    }

    // 强制设置角度
    public void SetAngle(float angle)
    {
        currentAngle = clampRotation ? Mathf.Clamp(angle, minAngle, maxAngle) : angle;
        ApplyLimitedRotation();
    }

    // 添加角度
    public void AddAngle(float deltaAngle)
    {
        float newAngle = currentAngle + deltaAngle;
        currentAngle = clampRotation ? Mathf.Clamp(newAngle, minAngle, maxAngle) : newAngle;
        ApplyLimitedRotation();
    }

    // 重置到初始角度
    public void ResetAngle()
    {
        currentAngle = 0f;
        ApplyLimitedRotation();
    }

    // 获取当前角度（相对于初始方向）
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    // 获取角度百分比（0到1）
    public float GetAnglePercentage()
    {
        if (!clampRotation) return 0f;
        return Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
    }

    // 设置角度限制
    public void SetAngleLimits(float newMinAngle, float newMaxAngle)
    {
        minAngle = newMinAngle;
        maxAngle = newMaxAngle;

        // 确保当前角度在新限制内
        if (clampRotation)
        {
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
            ApplyLimitedRotation();
        }
    }

    // 检查是否到达限制
    public bool IsAtMinLimit()
    {
        return clampRotation && Mathf.Approximately(currentAngle, minAngle);
    }

    public bool IsAtMaxLimit()
    {
        return clampRotation && Mathf.Approximately(currentAngle, maxAngle);
    }

    /// <summary>
    /// 0 拳
    /// 1 手掌
    /// 2 拿勺子
    /// </summary>
    /// <param name="self"></param>
    /// <param name="type"></param>
    public async FTask ChangeHandType(HandType type)
    {
        // Hand_Fist.SetActive(type == 0);
        // Hand_Palm.SetActive(type == 1);
        // Hand_Prop.SetActive(type == 2);
        isChangeToFist = type == HandType.Fist && HandType == HandType.Palm && isStayFruitsOrProps;
        Log.Error($"ChangeHandType {isChangeToFist}");
        HandType = type;
        Hand_Up.SetActive(type == HandType.Prop);
        switch (type)
        {
            case HandType.None:
                HandRoot.gameObject.SetActive(false);
                break;
            case HandType.Fist:
            {
                var sprite = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(isL() ? "L1_LHand_2" : "L1_RHand_2");
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;

                HandRoot_PolygonCollider2D.CopyFrom(collider_Fist);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
                break;
            }
            case HandType.Palm:
            {
                var sprite = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(isL() ? "L1_LHand_1" : "L1_RHand_1");
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Palm);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
            }
                break;
            case HandType.Prop:
            {
                var sprite = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(isL() ? "L1_LHand_3" : "L1_RHand_3");
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Prop);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Props");
            }
                break;
            case HandType.Tissue_UnUse:
            {
                var sprite = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(isL() ? "L1_LHand_4" : "L1_RHand_4");
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Tissue_UnUse);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
            }
                break;
            case HandType.Tissue_Used:
            {
                var sprite = await Scene.GetComponent<ResourceLoaderComponent>().LoadAssetAsync<Sprite>(isL() ? "L1_LHand_5" : "L1_RHand_5");
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Tissue_Used);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void PickUpTissueByBox()
    {
        var tissueComponent = Scene.GetComponent<TissueManagerComponent>().CreateTissue(HandRoot);
        pickUpTissueId = tissueComponent.Id;
        ChangeHandType(HandType.Tissue_UnUse).Coroutine();
    }

    public void PickUpTissue(string name)
    {
        long id = long.Parse(name);
        var tissueComponent = Scene.GetComponent<TissueManagerComponent>().GetTissue(id);
        if (tissueComponent != null)
        {
            pickUpTissueId = tissueComponent.Id;
            bool isUsed = tissueComponent.isUsed;
            ChangeHandType(isUsed ? HandType.Tissue_Used : HandType.Tissue_UnUse).Coroutine();
            tissueComponent.PickUpByHand(HandRoot);
        }
    }

    public void DropTissue()
    {
        if (pickUpTissueId != 0)
        {
            Scene.GetComponent<TissueManagerComponent>().DropTissue(pickUpTissueId);
            pickUpTissueId = 0;
            ChangeHandType(HandType.Palm).Coroutine();
        }
    }
}

public class PlayerInputComponent_Awake : AwakeSystem<PlayerInputComponent>
{
    protected override void Awake(PlayerInputComponent self)
    {
        self.playerIndex = (int)self.Id;
        self.controls = new InputAction_Player();
        self.InitProperty();

        if (self.isL())
        {
            self.controls.Player1.Enable();
            self.controls.Player1.Move.performed += self.OnPlayerMove;
            self.controls.Player1.Move.canceled += self.OnPlayerMove;
            self.controls.Player1.Rotate.performed += self.OnPlayerRotate;
            self.controls.Player1.Rotate.canceled += self.OnPlayerRotate;
            self.controls.Player1.SwitchHand.performed += self.OnPlayerSwitchHand;
            self.controls.Player1.Hit.performed += self.OnPlayerHit;
        }
        else
        {
            self.controls.Player2.Enable();
            self.controls.Player2.Move.performed += self.OnPlayerMove;
            self.controls.Player2.Move.canceled += self.OnPlayerMove;
            self.controls.Player2.Rotate.performed += self.OnPlayerRotate;
            self.controls.Player2.Rotate.canceled += self.OnPlayerRotate;
            self.controls.Player2.SwitchHand.performed += self.OnPlayerSwitchHand;
            self.controls.Player2.Hit.performed += self.OnPlayerHit;
        }
    }
}

public class PlayerInputComponent_Update : UpdateSystem<PlayerInputComponent>
{
    protected override void Update(PlayerInputComponent self)
    {
        if (self.HandRoot != null && !self.isHoldDog)
        {
            Vector3 movement = new Vector3(self.PlayerMove.x, self.PlayerMove.y, 0);
            // 计算局部移动方向
            Vector3 moveDirection = self.HandRoot.transform.right * movement.x + self.HandRoot.transform.up * movement.y;
            moveDirection.Normalize();

            // 应用速度（适用于持续移动）
            // if (self.PlayerMove.x == 0 && self.PlayerMove.y == 0)
            // {
            //     self.HandRoot_Rigidbody2D.velocity = Vector2.zero;
            //     Log.Error("000");
            // }
            // else
            //     self.HandRoot_Rigidbody2D.velocity = moveDirection * 2;

            // 或者使用 AddForce（平滑加速）
            // self.HandRoot_Rigidbody2D.AddForce(moveDirection * 5, ForceMode2D.Force);
            self.HandRoot.Translate(movement * self.PlayerSpeed * Time.deltaTime, Space.World);
            // 应用输入旋转
            if (Mathf.Abs(self.PlayerRotate) > 0.1f)
            {
                self.ApplyInputRotation();
            }
            else if (self.autoReturnToCenter)
            {
                self.ReturnToCenter();
            }

            // 应用限制后的旋转
            self.ApplyLimitedRotation();
        }
    }
}

public class PlayerInputComponent_Destroy : DestroySystem<PlayerInputComponent>
{
    protected override void Destroy(PlayerInputComponent self)
    {
        if (self.isL())
        {
            self.controls.Player1.Move.performed -= self.OnPlayerMove;
            self.controls.Player1.Move.canceled -= self.OnPlayerMove;
            self.controls.Player1.Rotate.performed -= self.OnPlayerRotate;
            self.controls.Player1.Rotate.canceled -= self.OnPlayerRotate;
            self.controls.Player1.SwitchHand.performed -= self.OnPlayerSwitchHand;
            self.controls.Player1.Hit.performed -= self.OnPlayerHit;
        }
        else
        {
            self.controls.Player2.Move.performed -= self.OnPlayerMove;
            self.controls.Player2.Move.canceled -= self.OnPlayerMove;
            self.controls.Player2.Rotate.performed -= self.OnPlayerRotate;
            self.controls.Player2.Rotate.canceled -= self.OnPlayerRotate;
            self.controls.Player2.SwitchHand.performed -= self.OnPlayerSwitchHand;
            self.controls.Player2.Hit.performed -= self.OnPlayerHit;
        }
    }
}