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

        public Vector2 standardRotationZRange;

        [Header("Translation")]
        public bool isTranslationJoint = false;     // 勾选后此关节用于控制整体平移
        public Vector2 standardPositionXRange;      // Pos X 的合法区间
        public Vector2 standardPositionYRange;      // Pos Y 的合法区间
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
    [SerializeField] private float rotateAcceleration = 180f;   // 每秒角加速度（度/秒²）
    [SerializeField] private float rotateFriction = 6f;         // 惯性摩擦系数（越大停得越快）
    [SerializeField] private float maxRotateSpeed = 270f;       // 最大角速度（度/秒）

    [Header("Body Translation")]
    [SerializeField] private RectTransform bodyRoot;            // 整个身体的根节点
    [SerializeField] private float translateAcceleration = 400f;
    [SerializeField] private float translateFriction = 8f;
    [SerializeField] private float maxTranslateSpeed = 400f;

    private Camera uiCamera;
    private Joint hoveredJoint;
    private Joint selectedJoint;

    private float angularVelocity = 0f;
    private Vector2 translateVelocity = Vector2.zero;

    public Joint[] Joints => joints;
    public RectTransform BodyRoot => bodyRoot;

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
        Joint newHovered = null;

        Vector2 cursorScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, cursor.position);

        int bestSiblingIndex = int.MinValue;

        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.rect == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(j.rect, cursorScreenPos, uiCamera))
            {
                int siblingIndex = j.rect.GetSiblingIndex();

                if (siblingIndex >= bestSiblingIndex)
                {
                    bestSiblingIndex = siblingIndex;
                    newHovered = j;
                }
            }
        }

        hoveredJoint = newHovered;
        UpdateJointColors();
    }

    void HandleSelectInput()
    {
        if ((playerType == PlayerType.Player1 && Input.GetKeyDown(KeyCode.F)) ||
            (playerType == PlayerType.Player2 && Input.GetKeyDown(KeyCode.Return)))
        {
            if (selectedJoint != null)
            {
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
            if (Input.GetKey(KeyCode.A)) inputDir += 1f;
            if (Input.GetKey(KeyCode.D)) inputDir -= 1f;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.LeftArrow))  inputDir += 1f;
            if (Input.GetKey(KeyCode.RightArrow)) inputDir -= 1f;
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
    }

    void UpdateJointColors()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.image == null)
                continue;

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
        enabled = false;
        cursor.gameObject.SetActive(false);
    }

    public void Enable()
    {
        enabled = true;
        cursor.gameObject.SetActive(true);
    }
}