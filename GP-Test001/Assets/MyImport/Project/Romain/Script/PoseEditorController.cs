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

        [Header("Translation")]
        public bool isTranslationJoint = false; // 勾选后此关节用于控制整体平移
    }

    [Serializable]
    private enum PlayerType
    {
        Player1,
        Player2
    }

    [Header("Player")]
    [SerializeField] private PlayerType playerType = PlayerType.Player1;

    [Header("Cursor")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private float moveSpeed = 300f;

    [Header("Joints")]
    [SerializeField] private Joint[] joints;

    [Header("Rotation")]
    [SerializeField] private float rotateAcceleration = 180f;
    [SerializeField] private float rotateFriction = 6f;
    [SerializeField] private float maxRotateSpeed = 270f;

    [Header("Body Translation")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private float translateAcceleration = 400f;
    [SerializeField] private float translateFriction = 8f;
    [SerializeField] private float maxTranslateSpeed = 400f;

    // ── 新增：Screen Space - Camera 模式下必须拿到挂载此 Canvas 的摄像机 ──
    private Camera uiCamera;
    private Canvas rootCanvas;

    private Joint hoveredJoint;
    private Joint selectedJoint;

    private float angularVelocity = 0f;
    private Vector2 translateVelocity = Vector2.zero;

    public Joint[] Joints => joints;
    public RectTransform BodyRoot => bodyRoot;

    void Awake()
    {
        // 向上查找根 Canvas，取其 worldCamera
        // Screen Space - Camera 模式下 worldCamera 就是渲染该 Canvas 的摄像机
        // Screen Space - Overlay 模式下 worldCamera 为 null，WorldToScreenPoint 传 null 也能正常工作
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;          // 确保拿到根 Canvas

        if (rootCanvas != null)
            uiCamera = rootCanvas.worldCamera;           // Screen Space - Camera → 非 null；Overlay → null（可接受）
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
        if (selectedJoint != null)
            return;

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
        // ── 修复：选中状态下光标隐藏且不移动，强制清空 hover，避免残留高亮 ──
        if (selectedJoint != null)
        {
            if (hoveredJoint != null)
            {
                hoveredJoint = null;
                UpdateJointColors();
            }
            return;
        }

        // ── 修复：Screen Space - Camera 下必须用 Canvas 的 worldCamera 作为参数 ──
        // uiCamera 在 Awake 中从根 Canvas.worldCamera 取得；
        // Overlay 模式下为 null，WorldToScreenPoint(null, ...) 行为与旧代码一致。
        Vector2 cursorScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, cursor.position);

        Joint newHovered = null;
        float bestDist = float.MaxValue;
        float hoverRadius = 30f;

        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.rect == null) continue;

            Vector2 jointScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, j.rect.position);
            float dist = Vector2.Distance(cursorScreenPos, jointScreenPos);

            if (dist < hoverRadius && dist < bestDist)
            {
                bestDist = dist;
                newHovered = j;
            }
        }

        if (newHovered != hoveredJoint)
        {
            hoveredJoint = newHovered;
            UpdateJointColors();
        }
    }

    void HandleSelectInput()
    {
        if ((playerType == PlayerType.Player1 && Input.GetKeyDown(KeyCode.Space)) ||
            (playerType == PlayerType.Player2 && Input.GetKeyDown(KeyCode.Return)))
        {
            if (selectedJoint != null)
            {
                // 退出选中时，把光标移到关节的中心位置
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
                    hoveredJoint = null;          // 选中时立刻清空 hover，防止同一关节同时显示两种颜色
                    cursor.gameObject.SetActive(false);
                }
            }

            UpdateJointColors();
        }
    }

    void HandleRotateInput()
    {
        if (selectedJoint == null || selectedJoint.skeleton == null)
        {
            angularVelocity = 0f;
            return;
        }

        float inputDir = 0f;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.A)) inputDir -= 1f;
            if (Input.GetKey(KeyCode.D)) inputDir += 1f;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.LeftArrow))  inputDir -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) inputDir += 1f;
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
            Vector3 euler = selectedJoint.skeleton.localEulerAngles;
            euler.z += angularVelocity * Time.deltaTime;
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
            bodyRoot.anchoredPosition += translateVelocity * Time.deltaTime;
        Debug.Log($"[Translate] velocity={translateVelocity}  pos={bodyRoot.anchoredPosition}");
    }

    void UpdateJointColors()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.image == null) continue;

            if (j == selectedJoint)
                j.image.color = j.selectedColor;
            else if (j == hoveredJoint)
                j.image.color = j.hoverColor;
            else
                j.image.color = j.normalColor;
        }
    }

    public void Disable()
    {
        // 禁用时同时清理选中/hover 状态，避免状态残留到下一次 Enable
        selectedJoint = null;
        hoveredJoint = null;
        angularVelocity = 0f;
        translateVelocity = Vector2.zero;
        UpdateJointColors();

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