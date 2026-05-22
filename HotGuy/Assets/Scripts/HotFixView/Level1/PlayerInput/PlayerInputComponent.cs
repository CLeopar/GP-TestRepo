using System;
using System.Collections.Generic;
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

    public PolygonCollider2D collider_Fist;
    public PolygonCollider2D collider_Palm;
    public PolygonCollider2D collider_Prop;
    public CircleCollider2D collider_Tissue_UnUse;
    public CircleCollider2D collider_Tissue_Used;
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
    public bool clampRotation { get; set; } = false;

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

    // ========== 缓存Sprite，避免每次切换手型重新加载 ==========
    private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    // ========== 缓存Layer整数，避免每帧字符串比较 ==========
    public int _layerDog;
    public int _layerShit;
    public int _layerFruits;
    public int _layerProps;
    public int _layerTissueBox;
    public int _layerTissue;

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

        collider_Fist = HandRoot.Find("Hand_Quan").GetComponent<PolygonCollider2D>();
        collider_Palm = HandRoot.Find("Hand_Bu").GetComponent<PolygonCollider2D>();
        collider_Prop = HandRoot.Find("Hand_Prop").GetComponent<PolygonCollider2D>();
        collider_Tissue_UnUse = HandRoot.Find("Hand_Tissue_UnUse").GetComponent<CircleCollider2D>();
        collider_Tissue_Used = HandRoot.Find("Hand_Tissue_Used").GetComponent<CircleCollider2D>();
        Hand_Up = HandRoot.Find("Hand_Up").gameObject;
        ChangeHandType(HandType.Palm).Coroutine();

        initialRotation = HandRoot.rotation;

        var unityEventTrigger_Hand = HandRoot.GetComponent<UnityEventTrigger>();
        unityEventTrigger_Hand.Register(OnCollisionEnter2D_Hand, OnCollisionExit2D_Hand, OnCollisionStay2D_Hand);

        // 缓存Layer整数
        _layerDog = LayerMask.NameToLayer("Dog");
        _layerShit = LayerMask.NameToLayer("Shit");
        _layerFruits = LayerMask.NameToLayer("Fruits");
        _layerProps = LayerMask.NameToLayer("Props");
        _layerTissueBox = LayerMask.NameToLayer("TissueBox");
        _layerTissue = LayerMask.NameToLayer("Tissue");

        // 预加载所有手型Sprite
        PreloadSprites().Coroutine();
        clampRotation = false;
    }

    public async FTask PreloadSprites()
    {
        var loader = Scene.GetComponent<ResourceLoaderComponent>();
        string[] keys = {
            "L1_LHand_1", "L1_LHand_2", "L1_LHand_3", "L1_LHand_4", "L1_LHand_5",
            "L1_RHand_1", "L1_RHand_2", "L1_RHand_3", "L1_RHand_4", "L1_RHand_5"
        };
        foreach (var key in keys)
        {
            var sprite = await loader.LoadAssetAsync<Sprite>(key);
            _spriteCache[key] = sprite;
        }
    }

    public void OnCollisionEnter2D_Hand(Collision2D collider)
    {
        if (HandType == HandType.Prop || HandType == HandType.None)
            return;
        var layer = collider.gameObject.layer;
        if (layer == _layerDog) isStayDog = true;
        // Tissue_UnUse时屎的检测改为Update里OverlapCircle，这里不再处理Shit
        if (layer == _layerShit && HandType != HandType.Tissue_UnUse) isStayShit = true;
    }

    public void OnCollisionExit2D_Hand(Collision2D collider)
    {
        if (HandType == HandType.None)
            return;
        var layer = collider.gameObject.layer;
        if (layer == _layerDog) isStayDog = false;
        if (layer == _layerShit && HandType != HandType.Tissue_UnUse) isStayShit = false;
        if (layer == _layerFruits || layer == _layerProps) isStayFruitsOrProps = false;
    }

    public void OnCollisionStay2D_Hand(Collision2D collider)
    {
        var layer = collider.gameObject.layer;
        bool isFruits = layer == _layerFruits;
        bool isProps = layer == _layerProps;
        bool isTissueBox = layer == _layerTissueBox;
        bool isTissue = layer == _layerTissue;

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

            Scene.GetComponent<FoodManagerComponent>().RemoveShit(true);
            return;
        }

        if (isStayDog)
        {
            Scene.EventComponent.Publish(new HitDog());
            return;
        }

        if (HandType != HandType.Fist)
            return;
        //获取道具
        Scene.GetComponent<FoodManagerComponent>().AddForce(HandRoot.position, 5f);
    }

    public void PickUpFruit(string fruitName)
    {
        var fruitId = long.Parse(fruitName);
        Scene.GetComponent<FoodManagerComponent>().PickUpFruit(fruitId, HandRoot);
        pickUpFruitId = fruitId;
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
        isPickUpProp = true;
        ChangeHandType(HandType.Prop).Coroutine();
    }

    public void DropProps()
    {
        if (isPickUpProp)
        {
            Scene.GetComponent<PropsManagerComponent>().DropProp();
            pickUpFruitId = 0;
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
        float angleDelta = PlayerRotate * RotationSpeed * Time.deltaTime;
        float newAngle = currentAngle + angleDelta;

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
        if (Mathf.Abs(currentAngle) < deadzoneAngle)
        {
            currentAngle = 0f;
            return;
        }

        float returnDelta = Mathf.Sign(-currentAngle) * returnSpeed * Time.deltaTime;

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
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, currentAngle);
        HandRoot.rotation = targetRotation;
    }

    public void SetAngle(float angle)
    {
        currentAngle = clampRotation ? Mathf.Clamp(angle, minAngle, maxAngle) : angle;
        ApplyLimitedRotation();
    }

    public void AddAngle(float deltaAngle)
    {
        float newAngle = currentAngle + deltaAngle;
        currentAngle = clampRotation ? Mathf.Clamp(newAngle, minAngle, maxAngle) : newAngle;
        ApplyLimitedRotation();
    }

    public void ResetAngle()
    {
        currentAngle = 0f;
        ApplyLimitedRotation();
    }

    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    public float GetAnglePercentage()
    {
        if (!clampRotation) return 0f;
        return Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
    }

    public void SetAngleLimits(float newMinAngle, float newMaxAngle)
    {
        minAngle = newMinAngle;
        maxAngle = newMaxAngle;

        if (clampRotation)
        {
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
            ApplyLimitedRotation();
        }
    }

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
    public async FTask ChangeHandType(HandType type)
    {
        isChangeToFist = type == HandType.Fist && HandType == HandType.Palm && isStayFruitsOrProps;
        HandType = type;
        Hand_Up.SetActive(type == HandType.Prop);
        switch (type)
        {
            case HandType.None:
                HandRoot.gameObject.SetActive(false);
                break;
            case HandType.Fist:
            {
                _spriteCache.TryGetValue(isL() ? "L1_LHand_2" : "L1_RHand_2", out var sprite);
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                collider_Tissue_UnUse.enabled = false;
                collider_Tissue_Used.enabled = false;
                HandRoot_PolygonCollider2D.enabled = true;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Fist);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
                break;
            }
            case HandType.Palm:
            {
                _spriteCache.TryGetValue(isL() ? "L1_LHand_1" : "L1_RHand_1", out var sprite);
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                collider_Tissue_UnUse.enabled = false;
                collider_Tissue_Used.enabled = false;
                HandRoot_PolygonCollider2D.enabled = true;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Palm);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
                break;
            }
            case HandType.Prop:
            {
                _spriteCache.TryGetValue(isL() ? "L1_LHand_3" : "L1_RHand_3", out var sprite);
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                collider_Tissue_UnUse.enabled = false;
                collider_Tissue_Used.enabled = false;
                HandRoot_PolygonCollider2D.enabled = true;
                HandRoot_PolygonCollider2D.CopyFrom(collider_Prop);
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Props");
                break;
            }
            case HandType.Tissue_UnUse:
            {
                _spriteCache.TryGetValue(isL() ? "L1_LHand_4" : "L1_RHand_4", out var sprite);
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                // CircleCollider2D 不能 CopyFrom 到 PolygonCollider2D，改为禁用 Polygon 启用 Circle
                HandRoot_PolygonCollider2D.enabled = false;
                collider_Tissue_UnUse.enabled = true;
                collider_Tissue_Used.enabled = false;
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
                break;
            }
            case HandType.Tissue_Used:
            {
                _spriteCache.TryGetValue(isL() ? "L1_LHand_5" : "L1_RHand_5", out var sprite);
                if (sprite != null)
                    HandRoot_SpriteRenderer.sprite = sprite;
                HandRoot_PolygonCollider2D.enabled = false;
                collider_Tissue_UnUse.enabled = false;
                collider_Tissue_Used.enabled = true;
                HandRoot.gameObject.SetActive(true);
                HandRoot.gameObject.layer = LayerMask.NameToLayer("Hands");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        await FTask.CompletedTask;
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
            Vector3 moveDirection = self.HandRoot.transform.right * movement.x + self.HandRoot.transform.up * movement.y;
            moveDirection.Normalize();

            self.HandRoot.Translate(movement * self.PlayerSpeed * Time.deltaTime, Space.World);

            if (Mathf.Abs(self.PlayerRotate) > 0.1f)
            {
                self.ApplyInputRotation();
            }
            else if (self.autoReturnToCenter)
            {
                self.ReturnToCenter();
            }

            self.ApplyLimitedRotation();
        }

        // 拿着纸巾时，用OverlapCircle检测屎，避免复杂PolygonCollider2D碰撞开销
        if (self.HandType == HandType.Tissue_UnUse && self.HandRoot != null)
        {
            var shitLayerMask = 1 << self._layerShit;
            var hit = Physics2D.OverlapCircle(self.HandRoot.position, 0.3f, shitLayerMask);
            self.isStayShit = hit != null;
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