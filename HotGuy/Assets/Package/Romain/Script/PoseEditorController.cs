using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseEditorController : MonoBehaviour
{
    [System.Serializable]
    public class Joint
    {
        public RectTransform rect;
        public Image image;
        public RectTransform skeleton;

        public Color normalColor = Color.white;
        public Color hoverColor = Color.yellow;
        public Color selectedColor = Color.green;

        [Header("Shape Override")]
        public Sprite normalSprite;
        public Sprite selectedSprite;

        [Header("Translation")]
        public bool isTranslationJoint = false;

        [Header("Hit Detection")]
        public RectTransform hitRect;        // 留空则使用 image.rectTransform
        public bool overrideHoverRadius;     // 勾选后启用自定义半径
        public float customHoverRadius = 15f;

        [Header("Angle Limit")]
        public bool useAngleLimit = false;
        [Range(-180f, 180f)] public float minAngle = -45f;
        [Range(-180f, 180f)] public float maxAngle = 45f;
    }

    [Serializable]
    private enum PlayerType { Player1, Player2 }

    [Header("Player")]
    [SerializeField] private PlayerType playerType = PlayerType.Player1;

    [Header("Cursor")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private float moveSpeed = 300f;

    [Header("Joints")]
    [SerializeField] private Joint[] joints;
    [SerializeField] private float hoverRadius = 15f; // 所有关节统一扩展的命中半径

    [Header("Rotation")]
    [SerializeField] private float rotateAcceleration = 180f;
    [SerializeField] private float rotateFriction = 6f;
    [SerializeField] private float maxRotateSpeed = 270f;

    [Header("Body Translation")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private float translateAcceleration = 400f;
    [SerializeField] private float translateFriction = 8f;
    [SerializeField] private float maxTranslateSpeed = 400f;

    private Camera uiCamera;
    private Canvas rootCanvas;

    private Joint hoveredJoint;
    private Joint selectedJoint;

    private float angularVelocity = 0f;
    private Vector2 translateVelocity = Vector2.zero;

    private float[] jointTrackedAngles;

    public Joint[] Joints => joints;
    public RectTransform BodyRoot => bodyRoot;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;
        if (rootCanvas != null)
            uiCamera = rootCanvas.worldCamera;

        jointTrackedAngles = new float[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            // 自动记录初始 Sprite，Inspector 留空时生效
            if (joints[i] != null && joints[i].image != null && joints[i].normalSprite == null)
                joints[i].normalSprite = joints[i].image.sprite;

            if (joints[i] != null && joints[i].skeleton != null)
                jointTrackedAngles[i] = NormalizeAngle(joints[i].skeleton.localEulerAngles.z);
            else
                jointTrackedAngles[i] = 0f;
        }
    }

    void Update()
    {
        HandleCursorMove();
        UpdateHoveredJoint();
        HandleSelectInput();

        if (selectedJoint != null && selectedJoint.isTranslationJoint)
            HandleTranslateInput();
        else
            HandleRotateInput();
    }

    void HandleCursorMove()
    {
        if (selectedJoint != null) return;

        Vector2 dir = Vector2.zero;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.W)) dir.y += 1f;
            if (Input.GetKey(KeyCode.S)) dir.y -= 1f;
            if (Input.GetKey(KeyCode.A)) dir.x -= 1f;
            if (Input.GetKey(KeyCode.D)) dir.x += 1f;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.UpArrow))    dir.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  dir.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow))  dir.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;
        }

        if (dir.sqrMagnitude > 0f)
        {
            dir.Normalize();
            cursor.anchoredPosition += dir * moveSpeed * Time.deltaTime;
        }
    }

    void UpdateHoveredJoint()
    {
        if (selectedJoint != null)
        {
            if (hoveredJoint != null)
            {
                hoveredJoint = null;
                UpdateJointVisuals();
            }
            return;
        }

        Vector2 cursorScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, cursor.position);

        Joint newHovered = null;

        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.rect == null || j.image == null) continue;

            float radius = (j.overrideHoverRadius) ? j.customHoverRadius : hoverRadius;

            if (!CircleContainsScreenPoint(j, cursorScreenPos, radius))
                continue;

            newHovered = j;
            break;
        }

        if (newHovered != hoveredJoint)
        {
            hoveredJoint = newHovered;
            UpdateJointVisuals();
        }
    }

    // 圆形命中检测：以 RectTransform 中心为圆心，半径 = 图片短边的一半 + hoverRadius 扩展量
    // 圆形命中检测：以 RectTransform 中心为圆心，半径 = 图片短边的一半 + 全局 hoverRadius
    private bool CircleContainsScreenPoint(Joint j, Vector2 screenPoint, float extraRadius)
    {
        RectTransform hitRect = (j.hitRect != null) ? j.hitRect : j.image.rectTransform;
    
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hitRect, screenPoint, uiCamera, out Vector2 localPoint);
    
        Rect r = hitRect.rect;
        Vector2 center = r.center;
        float imageRadius = Mathf.Min(r.width, r.height) * 0.5f;
        float hitRadius = imageRadius + extraRadius;
    
        return (localPoint - center).sqrMagnitude <= hitRadius * hitRadius;
    }

    void HandleSelectInput()
    {
        if ((playerType == PlayerType.Player1 && Input.GetKeyDown(KeyCode.Space)) ||
            (playerType == PlayerType.Player2 && Input.GetKeyDown(KeyCode.Keypad0)))
        {
            if (selectedJoint != null)
            {
                cursor.position = selectedJoint.rect.position;
                selectedJoint = null;
                angularVelocity = 0f;
                translateVelocity = Vector2.zero;
                cursor.gameObject.SetActive(true);
            }
            else
            {
                if (hoveredJoint != null)
                {
                    selectedJoint = hoveredJoint;
                    hoveredJoint = null;

                    int idx = Array.IndexOf(joints, selectedJoint);
                    if (idx >= 0 && selectedJoint.skeleton != null)
                        jointTrackedAngles[idx] = NormalizeAngle(selectedJoint.skeleton.localEulerAngles.z);

                    cursor.gameObject.SetActive(false);
                }
            }

            UpdateJointVisuals();
        }
    }

    void HandleRotateInput()
    {
        if (selectedJoint == null || selectedJoint.skeleton == null)
        {
            angularVelocity = 0f;
            return;
        }

        int idx = Array.IndexOf(joints, selectedJoint);
        if (idx < 0)
        {
            angularVelocity = 0f;
            return;
        }

        float inputDir = 0f;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.J)) inputDir -= 1f;
            if (Input.GetKey(KeyCode.K)) inputDir += 1f;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.Keypad1))  inputDir -= 1f;
            if (Input.GetKey(KeyCode.Keypad2)) inputDir += 1f;
        }

        if (inputDir != 0f)
        {
            angularVelocity += inputDir * rotateAcceleration * Time.deltaTime;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxRotateSpeed, maxRotateSpeed);
        }
        else
        {
            angularVelocity = Mathf.Lerp(angularVelocity, 0f, rotateFriction * Time.deltaTime);
            if (Mathf.Abs(angularVelocity) < 0.1f)
                angularVelocity = 0f;
        }

        if (Mathf.Abs(angularVelocity) > 0f)
        {
            float newAngle = jointTrackedAngles[idx] + angularVelocity * Time.deltaTime;

            if (selectedJoint.useAngleLimit)
            {
                float clamped = Mathf.Clamp(newAngle, selectedJoint.minAngle, selectedJoint.maxAngle);
                if (Mathf.Abs(clamped - newAngle) > 0.001f)
                    angularVelocity = 0f;
                newAngle = clamped;
            }

            jointTrackedAngles[idx] = newAngle;

            Vector3 euler = selectedJoint.skeleton.localEulerAngles;
            euler.z = newAngle;
            selectedJoint.skeleton.localEulerAngles = euler;
        }
    }

    void HandleTranslateInput()
    {
        if (bodyRoot == null) return;

        Vector2 inputDir = Vector2.zero;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.W)) inputDir.y += 1f;
            if (Input.GetKey(KeyCode.S)) inputDir.y -= 1f;
            if (Input.GetKey(KeyCode.A)) inputDir.x -= 1f;
            if (Input.GetKey(KeyCode.D)) inputDir.x += 1f;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.UpArrow))    inputDir.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  inputDir.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow))  inputDir.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) inputDir.x += 1f;
        }

        if (inputDir.sqrMagnitude > 0f)
        {
            inputDir.Normalize();
            translateVelocity += inputDir * translateAcceleration * Time.deltaTime;
            if (translateVelocity.magnitude > maxTranslateSpeed)
                translateVelocity = translateVelocity.normalized * maxTranslateSpeed;
        }
        else
        {
            translateVelocity = Vector2.Lerp(translateVelocity, Vector2.zero, translateFriction * Time.deltaTime);
            if (translateVelocity.magnitude < 0.1f)
                translateVelocity = Vector2.zero;
        }

        if (translateVelocity.sqrMagnitude > 0f)
        {
            bodyRoot.anchoredPosition += translateVelocity * Time.deltaTime;
            ClampBodyByTranslationJoint();
        }

        //Debug.Log($"[Translate] velocity={translateVelocity}  pos={bodyRoot.anchoredPosition}");
    }
    void ClampBodyByTranslationJoint()
    {
        if (selectedJoint == null || !selectedJoint.isTranslationJoint) return;

        // 取 hitRect，没有则用 image.rectTransform
        RectTransform hitRect = (selectedJoint.hitRect != null)
            ? selectedJoint.hitRect
            : selectedJoint.image.rectTransform;

        // 获取画布根节点的 RectTransform
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();

        // 将 hitRect 的屏幕坐标转为画布本地坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, hitRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCamera, out Vector2 localPos);

        // 画布的可用范围（以画布中心为原点）
        Vector2 canvasHalf = canvasRect.rect.size * 0.5f;

        // hitRect 自身的半径
        Rect r = hitRect.rect;
        float jointRadius = Mathf.Min(r.width, r.height) * 0.5f;

        // 允许的本地坐标范围
        float minX = -canvasHalf.x + jointRadius;
        float maxX =  canvasHalf.x - jointRadius;
        float minY = -canvasHalf.y + jointRadius;
        float maxY =  canvasHalf.y - jointRadius;

        // 计算需要修正的偏移量
        Vector2 correction = Vector2.zero;
        if (localPos.x < minX) correction.x = minX - localPos.x;
        if (localPos.x > maxX) correction.x = maxX - localPos.x;
        if (localPos.y < minY) correction.y = minY - localPos.y;
        if (localPos.y > maxY) correction.y = maxY - localPos.y;

        if (correction.sqrMagnitude > 0f)
        {
            bodyRoot.anchoredPosition += correction;
            // 撞边时清除对应方向的速度
            if (correction.x != 0f) translateVelocity.x = 0f;
            if (correction.y != 0f) translateVelocity.y = 0f;
        }
    }
    
    // 原 UpdateJointColors() 改名为 UpdateJointVisuals()，同时处理颜色和 Sprite 切换
    void UpdateJointVisuals()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.image == null) continue;

            if (j == selectedJoint)
            {
                j.image.color = j.selectedColor;
                // 切换到选中形状
                if (j.selectedSprite != null)
                    j.image.sprite = j.selectedSprite;
            }
            else
            {
                j.image.color = (j == hoveredJoint) ? j.hoverColor : j.normalColor;
                // 恢复原始形状
                if (j.normalSprite != null)
                    j.image.sprite = j.normalSprite;
            }
        }
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)  angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void Disable()
    {
        selectedJoint = null;
        hoveredJoint = null;
        angularVelocity = 0f;
        translateVelocity = Vector2.zero;
        UpdateJointVisuals();

        enabled = false;
        if (cursor != null)
            cursor.gameObject.SetActive(false);
    }

    public void Enable()
    {
        enabled = true;
        if (cursor != null)
            cursor.gameObject.SetActive(true);
    }
}