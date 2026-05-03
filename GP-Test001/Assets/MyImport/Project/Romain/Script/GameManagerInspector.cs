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

    // 固定关卡专用颜色（白色系，便于和随机关卡区分）
    private static readonly Color FixedLevelColor = new Color(1.0f, 1.0f, 0.6f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gm = (GameManager)target;
        if (gm.LevelList == null) return;

        EditorGUILayout.Space(8);

        // ── 固定关卡 zone 编辑（仅 enableTutorial 开启时显示）──────
        // 通过反射读取私有字段，避免修改 GameManager 的访问权限
        var enableTutorialField = typeof(GameManager).GetField("enableTutorial",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fixedFirstLevelField = typeof(GameManager).GetField("fixedFirstLevel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool enableTutorial = enableTutorialField != null && (bool)enableTutorialField.GetValue(gm);
        GameManager.Level fixedLevel = fixedFirstLevelField?.GetValue(gm) as GameManager.Level;

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
                key: $"{target.GetInstanceID()}_fixed",
                zone: fixedLevel.zone,
                serializedObj: serializedObject,
                color: FixedLevelColor
            );

            EditorGUILayout.Space(8);
        }

        // ── 随机关卡池 zone 编辑 ─────────────────────────────────
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
                key: $"{target.GetInstanceID()}_level_{i}",
                zone: level.zone,
                serializedObj: serializedObject,
                color: c
            );
        }
    }

    private void OnSceneGUI()
    {
        GameManager gm = (GameManager)target;
        if (gm.LevelList == null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // ── 固定关卡 zone ─────────────────────────────────────────
        var enableTutorialField = typeof(GameManager).GetField("enableTutorial",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fixedFirstLevelField = typeof(GameManager).GetField("fixedFirstLevel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool enableTutorial = enableTutorialField != null && (bool)enableTutorialField.GetValue(gm);
        GameManager.Level fixedLevel = fixedFirstLevelField?.GetValue(gm) as GameManager.Level;

        if (enableTutorial && fixedLevel != null)
        {
            PolygonZoneEditor.DrawSceneGUI(
                key: $"{target.GetInstanceID()}_fixed",
                zone: fixedLevel.zone,
                canvasRect: canvasRect,
                targetObject: target,
                color: FixedLevelColor
            );
        }

        // ── 随机关卡池 zone ───────────────────────────────────────
        for (int i = 0; i < gm.LevelList.Count; i++)
        {
            if (gm.LevelList[i] == null) continue;
            Color c = LevelColors[i % LevelColors.Length];
            PolygonZoneEditor.DrawSceneGUI(
                key: $"{target.GetInstanceID()}_level_{i}",
                zone: gm.LevelList[i].zone,
                canvasRect: canvasRect,
                targetObject: target,
                color: c
            );
        }
    }
}
#endif
