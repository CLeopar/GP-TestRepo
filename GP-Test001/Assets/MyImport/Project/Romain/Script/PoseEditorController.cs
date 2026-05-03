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
        public bool isTranslationJoint = false;

        [Header("Rotation Constraint")]
        [Tooltip("是否启用本地旋转角度限制")]
        public bool enableConstraint = false;
        [Tooltip("允许的最小本地 Z 角度（-180 ~ 180）")]
        [Range(-180f, 180f)] public float minAngle = -180f;
        [Tooltip("允许的最大本地 Z 角度（-180 ~ 180）")]
        [Range(-180f, 180f)] public float maxAngle =  180f;

        [Header("IK Weight")]
        [Tooltip("子集拖动时该关节跟随的权重，0=完全不动，1=完全跟随")]
        [Range(0f, 1f)] public float ikWeight = 1f;
    }

    [Serializable]
    private enum PlayerType { Player1, Player2 }

    [Header("Player")]
    [SerializeField] private PlayerType playerType = PlayerType.Player1;

    [Header("Cursor")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float ikMoveSpeed = 300f;

    [Header("Joints")]
    [SerializeField] private Joint[] joints;

    [Header("IK")]
    [SerializeField] private int ikIterations = 5;
    [SerializeField] private float ikTolerance = 2f;
    [SerializeField] private bool snapCursorOnSelect = true;

    [Header("Body Translation")]
    [SerializeField] private RectTransform bodyRoot;
    [SerializeField] private float translateAcceleration = 400f;
    [SerializeField] private float translateFriction = 8f;
    [SerializeField] private float maxTranslateSpeed = 400f;

    // ── 内部状态 ──────────────────────────────────────────────────────────────
    private Camera uiCamera;
    private Canvas rootCanvas;
    private Joint hoveredJoint;
    private Joint selectedJoint;

    private readonly List<RectTransform> ikChain = new List<RectTransform>();
    private readonly Dictionary<RectTransform, Joint> boneToJoint = new Dictionary<RectTransform, Joint>();
    private readonly Dictionary<RectTransform, float> boneSmoothedAngles = new Dictionary<RectTransform, float>();

    private Vector2 translateVelocity = Vector2.zero;
    private Vector2 ikTargetPos;

    public Joint[] Joints => joints;
    public RectTransform BodyRoot => bodyRoot;

    // ── 生命周期 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        if (rootCanvas != null) uiCamera = rootCanvas.worldCamera;
    }

    void Update()
    {
        HandleCursorMove();
        UpdateHoveredJoint();
        HandleSelectInput();

        if (selectedJoint == null) return;

        if (selectedJoint.isTranslationJoint)
            HandleTranslateInput();
        else
            HandleIKInput();
    }

    // ── 输入方向读取 ──────────────────────────────────────────────────────────

    Vector2 GetInputDirection()
    {
        Vector2 dir = Vector2.zero;
        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.W)) dir.y += 1f;
            if (Input.GetKey(KeyCode.S)) dir.y -= 1f;
            if (Input.GetKey(KeyCode.A)) dir.x -= 1f;
            if (Input.GetKey(KeyCode.D)) dir.x += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow))    dir.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  dir.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow))  dir.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;
        }
        return dir;
    }

    // ── 光标 / IK目标 移动 ────────────────────────────────────────────────────

    void HandleCursorMove()
    {
        if (selectedJoint != null && selectedJoint.isTranslationJoint) return;

        Vector2 dir = GetInputDirection();
        if (dir.sqrMagnitude == 0f) return;

        dir.Normalize();
        float speed = (selectedJoint != null) ? ikMoveSpeed : moveSpeed;
        Vector2 delta = dir * speed * Time.deltaTime;

        if (selectedJoint != null && !selectedJoint.isTranslationJoint)
            ikTargetPos += delta;
        else
            cursor.anchoredPosition += delta;
    }

    // ── Hover 检测 ────────────────────────────────────────────────────────────

    void UpdateHoveredJoint()
    {
        if (selectedJoint != null)
        {
            if (hoveredJoint != null) { hoveredJoint = null; UpdateJointColors(); }
            return;
        }

        Vector2 cursorScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, cursor.position);
        Joint newHovered = null;
        float bestDist = float.MaxValue;
        const float hoverRadius = 30f;

        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.rect == null) continue;
            Vector2 jointScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, j.rect.position);
            float dist = Vector2.Distance(cursorScreenPos, jointScreenPos);
            if (dist < hoverRadius && dist < bestDist) { bestDist = dist; newHovered = j; }
        }

        if (newHovered != hoveredJoint) { hoveredJoint = newHovered; UpdateJointColors(); }
    }

    // ── 选中/取消 ─────────────────────────────────────────────────────────────

    void HandleSelectInput()
    {
        bool confirm = (playerType == PlayerType.Player1 && Input.GetKeyDown(KeyCode.Space))
                    || (playerType == PlayerType.Player2 && Input.GetKeyDown(KeyCode.Return));
        if (!confirm) return;

        if (selectedJoint != null)
        {
            cursor.position = selectedJoint.rect.position;
            selectedJoint = null;
            translateVelocity = Vector2.zero;
            ikChain.Clear();
            boneSmoothedAngles.Clear();
            cursor.gameObject.SetActive(true);
        }
        else if (hoveredJoint != null)
        {
            selectedJoint = hoveredJoint;
            hoveredJoint = null;

            if (selectedJoint.isTranslationJoint)
            {
                cursor.gameObject.SetActive(false);
            }
            else
            {
                ikTargetPos = selectedJoint.rect.position;
                cursor.gameObject.SetActive(false);
                BuildIKChain(selectedJoint);
            }
        }

        UpdateJointColors();
    }

    // ── IK 链构建 ─────────────────────────────────────────────────────────────

    void BuildIKChain(Joint joint)
    {
        ikChain.Clear();
        boneToJoint.Clear();
        boneSmoothedAngles.Clear();

        if (joint == null || joint.rect == null) return;

        var skeletonSet = new HashSet<Transform>();
        foreach (var j in joints)
        {
            if (j == null || j.rect == null || j.rect.parent == null) continue;
            skeletonSet.Add(j.rect.parent);
            var rt = j.rect.parent as RectTransform;
            if (rt != null && !boneToJoint.ContainsKey(rt))
                boneToJoint[rt] = j;
        }

        Transform current = joint.rect.parent;
        while (current != null)
        {
            if (bodyRoot != null && current == bodyRoot) break;
            if (skeletonSet.Contains(current))
            {
                var rt = current as RectTransform;
                if (rt != null)
                {
                    ikChain.Add(rt);
                    boneSmoothedAngles[rt] = NormalizeAngle(rt.localEulerAngles.z);
                }
            }
            current = current.parent;
        }
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)  angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    void HandleIKInput()
    {
        if (selectedJoint == null || ikChain.Count == 0) return;

        Vector2 targetWorld   = ikTargetPos;
        Transform endEffector = selectedJoint.rect;

        // 步骤一：还原上一帧角度作为求解起点
        for (int i = 0; i < ikChain.Count; i++)
        {
            RectTransform bone = ikChain[i];
            if (bone == null) continue;
            Vector3 euler = bone.localEulerAngles;
            euler.z = boneSmoothedAngles[bone];
            bone.localEulerAngles = euler;
        }

        // 步骤二：CCD 求解，权重来自每个关节自身的 ikWeight
        for (int iter = 0; iter < ikIterations; iter++)
        {
            if (Vector2.Distance(endEffector.position, targetWorld) < ikTolerance) break;

            for (int i = 0; i < ikChain.Count; i++)
            {
                RectTransform bone = ikChain[i];
                if (bone == null) continue;

                // ★ 从该骨骼对应的 Joint 读取 ikWeight，找不到则默认 1
                boneToJoint.TryGetValue(bone, out Joint jc);
                float weight = (jc != null) ? jc.ikWeight : 1f;

                Vector2 toEnd    = (Vector2)endEffector.position - (Vector2)bone.position;
                Vector2 toTarget = targetWorld - (Vector2)bone.position;
                if (toEnd.sqrMagnitude < 0.0001f) continue;

                float delta = Vector2.SignedAngle(toEnd, toTarget) * weight;
                Vector3 euler = bone.localEulerAngles;
                float newZ = NormalizeAngle(euler.z) + delta;

                if (jc != null && jc.enableConstraint)
                    newZ = Mathf.Clamp(newZ, jc.minAngle, jc.maxAngle);

                euler.z = newZ;
                bone.localEulerAngles = euler;
            }
        }

        // 步骤三：直接记录求解结果，不做插值
        for (int i = 0; i < ikChain.Count; i++)
        {
            RectTransform bone = ikChain[i];
            if (bone == null) continue;
            boneSmoothedAngles[bone] = NormalizeAngle(bone.localEulerAngles.z);
        }
    }

    // ── 整体平移 ──────────────────────────────────────────────────────────────

    void HandleTranslateInput()
    {
        if (bodyRoot == null) return;

        Vector2 inputDir = GetInputDirection();

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
            if (translateVelocity.magnitude < 0.1f) translateVelocity = Vector2.zero;
        }

        if (translateVelocity.sqrMagnitude > 0f)
            bodyRoot.anchoredPosition += translateVelocity * Time.deltaTime;
    }

    // ── 颜色更新 ──────────────────────────────────────────────────────────────

    void UpdateJointColors()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Joint j = joints[i];
            if (j == null || j.image == null) continue;
            if (j == selectedJoint)       j.image.color = j.selectedColor;
            else if (j == hoveredJoint)   j.image.color = j.hoverColor;
            else                          j.image.color = j.normalColor;
        }
    }

    // ── Enable / Disable ─────────────────────────────────────────────────────

    public void Disable()
    {
        selectedJoint = null;
        hoveredJoint = null;
        translateVelocity = Vector2.zero;
        ikChain.Clear();
        boneToJoint.Clear();
        boneSmoothedAngles.Clear();
        UpdateJointColors();
        enabled = false;
        if (cursor != null) cursor.gameObject.SetActive(false);
    }

    public void Enable()
    {
        enabled = true;
        if (cursor != null) cursor.gameObject.SetActive(true);
    }
}