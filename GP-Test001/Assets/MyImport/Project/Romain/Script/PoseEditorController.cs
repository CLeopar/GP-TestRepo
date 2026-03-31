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
    [SerializeField] private float rotateSpeed = 90f;

    private Camera uiCamera;
    private Joint hoveredJoint;
    private Joint selectedJoint;

    public Joint[] Joints => joints;

    void Update()
    {
        HandleCursorMove();
        UpdateHoveredJoint();
        HandleSelectInput();
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
            if (Input.GetKey(KeyCode.UpArrow)) dir.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) dir.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) dir.x -= 1f;
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
        if ((playerType == PlayerType.Player1 && Input.GetKeyDown(KeyCode.F)) || (playerType == PlayerType.Player2 && Input.GetKeyDown(KeyCode.Return)))
        {
            if (selectedJoint != null)
            {
                selectedJoint = null;
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
            return;

        float deltaAngle = 0f;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.A))
                deltaAngle += rotateSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D))
                deltaAngle -= rotateSpeed * Time.deltaTime;
        }
        else if (playerType == PlayerType.Player2)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                deltaAngle += rotateSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow))
                deltaAngle -= rotateSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(deltaAngle) > 0f)
        {
            Vector3 euler = selectedJoint.skeleton.localEulerAngles;
            euler.z += deltaAngle;
            selectedJoint.skeleton.localEulerAngles = euler;
        }
    }

    void UpdateJointColors()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.image == null)
                continue;

            if (j == selectedJoint)
            {
                j.image.color = j.selectedColor;
            }
            else if (j == hoveredJoint)
            {
                j.image.color = j.hoverColor;
            }
            else
            {
                j.image.color = j.normalColor;
            }
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