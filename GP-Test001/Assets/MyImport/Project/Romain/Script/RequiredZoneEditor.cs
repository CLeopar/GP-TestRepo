#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RequiredZone))]
public class RequiredZoneEditor : Editor
{
    private static readonly Color ZoneColor = new Color(1f, 0.4f, 0.2f); // 橙红色，与关卡主区域区分

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var rz = (RequiredZone)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("penaltyScore"));
        EditorGUILayout.Space(4);

        Canvas canvas = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        if (canvasRect == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 Canvas，无法编辑多边形。", MessageType.Warning);
        }
        else
        {
            PolygonZoneEditor.DrawInspectorGUI(
                target.GetInstanceID().ToString(),
                rz.zone,
                serializedObject,
                ZoneColor);
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

    private void OnSceneGUI()
    {
        var rz = (RequiredZone)target;
        Canvas canvas = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        PolygonZoneEditor.DrawSceneGUI(
            target.GetInstanceID().ToString(),
            rz.zone,
            canvasRect,
            target,
            new Color(1f, 0.4f, 0.2f));
    }
}
#endif