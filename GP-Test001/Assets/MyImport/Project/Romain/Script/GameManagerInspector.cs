#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerInspector : Editor
{
    private static Color[] LevelColors = new Color[]
    {
        new Color(0.2f, 0.7f, 1.0f),
        new Color(1.0f, 0.5f, 0.2f),
        new Color(0.4f, 1.0f, 0.4f),
        new Color(1.0f, 0.3f, 0.5f),
        new Color(0.9f, 0.8f, 0.2f),
        new Color(0.7f, 0.4f, 1.0f),
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gm = (GameManager)target;
        if (gm.LevelList == null) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("── 关卡判定区域 ──", EditorStyles.boldLabel);

        for (int i = 0; i < gm.LevelList.Count; i++)
        {
            var level = gm.LevelList[i];
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

        for (int i = 0; i < gm.LevelList.Count; i++)
        {
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