#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerInspector : Editor
{
    private static readonly Color[] LevelColors = new Color[]
    {
        new Color(0.2f, 0.7f, 1.0f),
        new Color(1.0f, 0.5f, 0.2f),
        new Color(0.4f, 1.0f, 0.4f),
        new Color(1.0f, 0.3f, 0.5f),
        new Color(0.9f, 0.8f, 0.2f),
        new Color(0.7f, 0.4f, 1.0f),
    };

    // RequiredZone 固定用橙红色，与主 zone 颜色区分
    private static readonly Color RequiredZoneColor = new Color(1.0f, 0.35f, 0.2f);
    private static readonly Color FixedLevelColor   = new Color(1.0f, 1.0f, 0.6f);

    // 每个 level 的 requiredZones 折叠状态
    // key = "fixed" 或 levelIndex，value = 每个 zone 的折叠布尔列表
    private readonly Dictionary<string, List<bool>> _rzFoldouts = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gm = (GameManager)target;
        if (gm.LevelList == null) return;

        EditorGUILayout.Space(8);

        var enableTutorialField  = typeof(GameManager).GetField("enableTutorial",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fixedFirstLevelField = typeof(GameManager).GetField("fixedFirstLevel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool enableTutorial      = enableTutorialField  != null && (bool)enableTutorialField.GetValue(gm);
        GameManager.Level fixedLevel = fixedFirstLevelField?.GetValue(gm) as GameManager.Level;

        // ── 固定关卡 ──────────────────────────────────────────────
        if (enableTutorial && fixedLevel != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── 固定关卡判定区域 ──", EditorStyles.boldLabel);

            var prevColor = GUI.color;
            GUI.color = FixedLevelColor;
            string fixedName = fixedLevel.levelObj != null ? fixedLevel.levelObj.name : "Fixed Level";
            EditorGUILayout.LabelField($"★  {fixedName}（固定）", EditorStyles.boldLabel);
            GUI.color = prevColor;

            PolygonZoneEditor.DrawInspectorGUI(
                key:           $"{target.GetInstanceID()}_fixed",
                zone:          fixedLevel.zone,
                serializedObj: serializedObject,
                color:         FixedLevelColor);

            DrawRequiredZonesGUI("fixed", fixedLevel, serializedObject);

            EditorGUILayout.Space(8);
        }

        // ── 随机关卡池 ────────────────────────────────────────────
        EditorGUILayout.LabelField("── 关卡判定区域 ──", EditorStyles.boldLabel);

        for (int i = 0; i < gm.LevelList.Count; i++)
        {
            var level = gm.LevelList[i];
            if (level == null) continue;

            Color c = LevelColors[i % LevelColors.Length];

            EditorGUILayout.Space(4);
            var prevColor = GUI.color;
            GUI.color = c;
            string levelName = level.levelObj != null ? level.levelObj.name : $"Level {i}";
            EditorGUILayout.LabelField($"▶  {levelName}", EditorStyles.boldLabel);
            GUI.color = prevColor;

            PolygonZoneEditor.DrawInspectorGUI(
                key:           $"{target.GetInstanceID()}_level_{i}",
                zone:          level.zone,
                serializedObj: serializedObject,
                color:         c);

            DrawRequiredZonesGUI(i.ToString(), level, serializedObject);
        }
    }

    // ── Required Zones 编辑区 ─────────────────────────────────
    private void DrawRequiredZonesGUI(string levelKey, GameManager.Level level, SerializedObject so)
    {
        if (!_rzFoldouts.ContainsKey(levelKey))
            _rzFoldouts[levelKey] = new List<bool>();

        var foldouts = _rzFoldouts[levelKey];

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("必须触碰区域 (Required Zones)", EditorStyles.boldLabel);

        // 同步 requiredZones 数组（若为 null 先初始化）
        if (level.requiredZones == null)
            level.requiredZones = new RequiredZone[0];

        // 同步折叠列表长度
        while (foldouts.Count < level.requiredZones.Length) foldouts.Add(true);
        while (foldouts.Count > level.requiredZones.Length) foldouts.RemoveAt(foldouts.Count - 1);

        Canvas canvas            = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        if (canvasRect == null)
            EditorGUILayout.HelpBox("场景中未找到 Canvas，无法在 Scene 视图中编辑多边形。", MessageType.Warning);

        for (int zi = 0; zi < level.requiredZones.Length; zi++)
        {
            var rz = level.requiredZones[zi];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行：折叠 + 删除
            EditorGUILayout.BeginHorizontal();
            foldouts[zi] = EditorGUILayout.Foldout(foldouts[zi],
                $"Zone {zi}  (扣分：{(rz != null ? rz.penaltyScore.ToString("F0") : "—")})",
                true, EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
            {
                Undo.RecordObject(target, "Remove RequiredZone");
                var list = new List<RequiredZone>(level.requiredZones);
                list.RemoveAt(zi);
                level.requiredZones = list.ToArray();
                foldouts.RemoveAt(zi);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (!foldouts[zi])
            {
                EditorGUILayout.EndVertical();
                continue;
            }

            // 对象引用槽 + 新建按钮
            EditorGUILayout.BeginHorizontal();
            var newRef = (RequiredZone)EditorGUILayout.ObjectField(
                "组件引用", rz, typeof(RequiredZone), true);
            if (newRef != rz)
            {
                Undo.RecordObject(target, "Assign RequiredZone");
                level.requiredZones[zi] = newRef;
                rz = newRef;
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("新建", GUILayout.Width(44)))
            {
                var go = new GameObject($"RequiredZone_L{levelKey}_Z{zi}");
                Undo.RegisterCreatedObjectUndo(go, "Create RequiredZone");
                var newRz = go.AddComponent<RequiredZone>();
                Undo.RecordObject(target, "Assign RequiredZone");
                level.requiredZones[zi] = newRz;
                rz = newRz;
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            // 有组件引用时，绘制 penaltyScore + 多边形编辑器
            if (rz != null)
            {
                SerializedObject rzSO = new SerializedObject(rz);
                rzSO.Update();

                EditorGUILayout.PropertyField(
                    rzSO.FindProperty("penaltyScore"),
                    new GUIContent("未触碰扣分"));

                if (canvasRect != null)
                {
                    PolygonZoneEditor.DrawInspectorGUI(
                        key:           RzKey(levelKey, zi),
                        zone:          rz.zone,
                        serializedObj: rzSO,
                        color:         RequiredZoneColor);
                }

                rzSO.ApplyModifiedProperties();
                if (GUI.changed) EditorUtility.SetDirty(rz);
            }
            else
            {
                EditorGUILayout.HelpBox("拖入已有 RequiredZone 组件，或点新建在场景中创建。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // 添加按钮
        if (GUILayout.Button("+ 添加 Required Zone"))
        {
            Undo.RecordObject(target, "Add RequiredZone Slot");
            var list = new List<RequiredZone>(level.requiredZones) { null };
            level.requiredZones = list.ToArray();
            foldouts.Add(true);
            EditorUtility.SetDirty(target);
        }

        EditorGUI.indentLevel--;
    }

    // ── Scene GUI ─────────────────────────────────────────────
    private void OnSceneGUI()
    {
        GameManager gm = (GameManager)target;
        if (gm.LevelList == null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        var enableTutorialField  = typeof(GameManager).GetField("enableTutorial",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fixedFirstLevelField = typeof(GameManager).GetField("fixedFirstLevel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool enableTutorial      = enableTutorialField  != null && (bool)enableTutorialField.GetValue(gm);
        GameManager.Level fixedLevel = fixedFirstLevelField?.GetValue(gm) as GameManager.Level;

        // ── 固定关卡主 zone + requiredZones ──────────────────────
        if (enableTutorial && fixedLevel != null)
        {
            PolygonZoneEditor.DrawSceneGUI(
                key:          $"{target.GetInstanceID()}_fixed",
                zone:         fixedLevel.zone,
                canvasRect:   canvasRect,
                targetObject: target,
                color:        FixedLevelColor);

            DrawRequiredZonesSceneGUI("fixed", fixedLevel, canvasRect);
        }

        // ── 随机关卡池主 zone + requiredZones ────────────────────
        for (int i = 0; i < gm.LevelList.Count; i++)
        {
            if (gm.LevelList[i] == null) continue;
            Color c = LevelColors[i % LevelColors.Length];

            PolygonZoneEditor.DrawSceneGUI(
                key:          $"{target.GetInstanceID()}_level_{i}",
                zone:         gm.LevelList[i].zone,
                canvasRect:   canvasRect,
                targetObject: target,
                color:        c);

            DrawRequiredZonesSceneGUI(i.ToString(), gm.LevelList[i], canvasRect);
        }
    }

    private void DrawRequiredZonesSceneGUI(
        string levelKey,
        GameManager.Level level,
        RectTransform canvasRect)
    {
        if (level.requiredZones == null) return;

        for (int zi = 0; zi < level.requiredZones.Length; zi++)
        {
            var rz = level.requiredZones[zi];
            if (rz == null) continue;

            PolygonZoneEditor.DrawSceneGUI(
                key:          RzKey(levelKey, zi),
                zone:         rz.zone,
                canvasRect:   canvasRect,
                targetObject: rz,
                color:        RequiredZoneColor);
        }
    }

    private static string RzKey(string levelKey, int zoneIndex) =>
        $"{levelKey}_rz_{zoneIndex}";
}
#endif