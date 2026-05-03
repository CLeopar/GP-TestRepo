#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 通用 PolygonZone Scene 编辑器（支持多环镂空）
/// 用法：在任意 CustomEditor 里调用 DrawInspectorGUI / DrawSceneGUI
/// </summary>
public static class PolygonZoneEditor
{
    // ── 每个 zone 的编辑状态 ─────────────────────────────────────
    private class ZoneState
    {
        public int  activeContour  = 0;     // 当前正在编辑的环索引
        public bool editMode       = false;

        // 每个环独立的 hover 状态
        public int  hoveredVertex  = -1;
        public int  hoveredEdge    = -1;
        public Vector2 edgeInsertPos;
    }

    private static readonly Dictionary<string, ZoneState> _states = new Dictionary<string, ZoneState>();

    private static ZoneState GetState(string key)
    {
        if (!_states.ContainsKey(key)) _states[key] = new ZoneState();
        return _states[key];
    }

    // ── 颜色工具 ─────────────────────────────────────────────────
    private static Color ColFill(Color c)       => new Color(c.r, c.g, c.b, 0.12f);
    private static Color ColFillHole(Color c)   => new Color(c.r, c.g, c.b, 0.06f);  // 镂空孔洞填充更透明
    private static Color ColEdge(Color c)       => new Color(c.r, c.g, c.b, 0.85f);
    private static Color ColEdgeHole(Color c)   => new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 0.7f); // 内环边颜色偏暗
    private static Color ColEdgeActive(Color c) => new Color(c.r, c.g, c.b, 1f);     // 当前编辑环高亮

    private static readonly Color ColVertex      = new Color(1f, 1f,    1f,  0.95f);
    private static readonly Color ColVertexHover = new Color(1f, 0.85f, 0f,  1f);
    private static readonly Color ColEdgeHover   = new Color(0.4f, 1f,  0.4f,1f);
    private static readonly Color ColInsertDot   = new Color(0.4f, 1f,  0.4f,0.9f);

    private const float VERTEX_RADIUS = 0.05f;
    private const float EDGE_HIT_DIST = 12f;

    // ── Inspector GUI 主入口 ──────────────────────────────────────
    public static void DrawInspectorGUI(
        string key,
        PolygonZone zone,
        SerializedObject serializedObj,
        Color color)
    {
        // 确保至少有一个外环
        if (zone.contours.Count == 0) zone.contours.Add(new Contour());

        var state = GetState(key);
        // 防止 activeContour 越界
        state.activeContour = Mathf.Clamp(state.activeContour, 0, zone.contours.Count - 1);

        // ── 环选择 Toolbar ───────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("环：", GUILayout.Width(28));
        for (int ci = 0; ci < zone.contours.Count; ci++)
        {
            bool isActive = (state.activeContour == ci);
            GUI.backgroundColor = isActive ? new Color(0.4f, 1f, 0.5f) : Color.white;
            string label = ci == 0 ? $"外环({zone.contours[ci].vertices.Count})" : $"孔{ci}({zone.contours[ci].vertices.Count})";
            if (GUILayout.Button(label, GUILayout.Height(20)))
            {
                state.activeContour = ci;
                SceneView.RepaintAll();
            }
        }
        GUI.backgroundColor = Color.white;

        // 添加新环按钮
        GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
        if (GUILayout.Button("+ 添加孔洞", GUILayout.Height(20)))
        {
            Undo.RecordObject(serializedObj.targetObject, "Add Contour");
            zone.AddContour();
            state.activeContour = zone.contours.Count - 1;
            EditorUtility.SetDirty(serializedObj.targetObject);
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // ── 当前环操作行 ─────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        // 编辑模式切换
        GUI.backgroundColor = state.editMode ? new Color(0.4f, 1f, 0.5f) : Color.white;
        string btnLabel = state.editMode ? "✏ 编辑中（点击退出）" : "✏ 编辑多边形";
        if (GUILayout.Button(btnLabel, GUILayout.Height(22)))
        {
            state.editMode = !state.editMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        // 清空当前环
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("清空", GUILayout.Width(48), GUILayout.Height(22)))
        {
            Undo.RecordObject(serializedObj.targetObject, "Clear Contour");
            zone.contours[state.activeContour].vertices.Clear();
            EditorUtility.SetDirty(serializedObj.targetObject);
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        // 删除当前环（外环不可删）
        if (state.activeContour > 0)
        {
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("删除此孔", GUILayout.Width(64), GUILayout.Height(22)))
            {
                Undo.RecordObject(serializedObj.targetObject, "Remove Contour");
                zone.RemoveContour(state.activeContour);
                state.activeContour = Mathf.Max(0, state.activeContour - 1);
                EditorUtility.SetDirty(serializedObj.targetObject);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        // 顶点数量提示
        int vCount = zone.contours[state.activeContour].vertices.Count;
        string hint = vCount < 3
            ? $"  ⚠ 需要至少 3 个顶点（当前 {vCount} 个）"
            : $"  ✓ {vCount} 个顶点";
        EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);

        if (state.editMode)
        {
            string contourType = state.activeContour == 0 ? "外环" : $"孔洞 {state.activeContour}";
            EditorGUILayout.HelpBox(
                $"当前编辑：{contourType}\n" +
                "左键空白处：添加顶点\n左键拖拽顶点：移动\n左键边中间绿点：插入顶点\n右键顶点：删除",
                MessageType.Info);
        }
    }

    // ── Scene GUI 主入口 ──────────────────────────────────────────
    public static void DrawSceneGUI(
        string key,
        PolygonZone zone,
        RectTransform canvasRect,
        UnityEngine.Object targetObject,
        Color color)
    {
        if (canvasRect == null) return;
        if (zone.contours.Count == 0) return;

        var state = GetState(key);
        state.activeContour = Mathf.Clamp(state.activeContour, 0, zone.contours.Count - 1);

        // 先绘制所有环
        DrawAllContours(zone, canvasRect, color, state);

        if (!state.editMode) return;

        // 只对当前选中的环做交互
        var activeVerts = zone.contours[state.activeContour].vertices;
        HandleSceneInput(key, activeVerts, canvasRect, targetObject, state, color);
    }

    // ── 绘制所有环 ────────────────────────────────────────────────
    private static void DrawAllContours(
        PolygonZone zone,
        RectTransform canvasRect,
        Color color,
        ZoneState state)
    {
        for (int ci = 0; ci < zone.contours.Count; ci++)
        {
            var verts = zone.contours[ci].vertices;
            if (verts == null || verts.Count == 0) continue;

            bool isActive   = state.editMode && (ci == state.activeContour);
            bool isHole     = (ci > 0);

            // 转世界坐标
            Vector3[] worldVerts = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
                worldVerts[i] = AnchoredToWorld(verts[i], canvasRect);

            // 填充
            if (verts.Count >= 3)
            {
                Handles.color = isHole ? ColFillHole(color) : ColFill(color);
                Vector3 center = Vector3.zero;
                foreach (var v in worldVerts) center += v;
                center /= worldVerts.Length;

                for (int i = 0; i < worldVerts.Length; i++)
                {
                    int next = (i + 1) % worldVerts.Length;
                    Handles.DrawAAConvexPolygon(center, worldVerts[i], worldVerts[next]);
                }
            }

            // 边
            for (int i = 0; i < worldVerts.Length; i++)
            {
                int next = (i + 1) % worldVerts.Length;

                if (isActive && state.hoveredEdge == i)
                    Handles.color = ColEdgeHover;
                else if (isActive)
                    Handles.color = ColEdgeActive(color);
                else if (isHole)
                    Handles.color = ColEdgeHole(color);
                else
                    Handles.color = ColEdge(color);

                Handles.DrawLine(worldVerts[i], worldVerts[next], isActive ? 2.5f : 1.5f);
            }

            // 插入点预览（只在当前活动环显示）
            if (isActive && state.hoveredEdge >= 0 && state.hoveredVertex < 0)
            {
                Handles.color = ColInsertDot;
                Vector3 insertWorld = AnchoredToWorld(state.edgeInsertPos, canvasRect);
                float sz = HandleUtility.GetHandleSize(insertWorld) * VERTEX_RADIUS * 0.8f;
                Handles.DotHandleCap(0, insertWorld, Quaternion.identity, sz, EventType.Repaint);
            }

            // 顶点
            for (int i = 0; i < worldVerts.Length; i++)
            {
                if (isActive)
                    Handles.color = (state.hoveredVertex == i) ? ColVertexHover : ColVertex;
                else
                    Handles.color = new Color(ColVertex.r, ColVertex.g, ColVertex.b, 0.4f);

                float sz = HandleUtility.GetHandleSize(worldVerts[i]) * VERTEX_RADIUS;
                Handles.DotHandleCap(0, worldVerts[i], Quaternion.identity, sz, EventType.Repaint);

                if (isActive)
                    Handles.Label(worldVerts[i] + Vector3.up * sz * 2.5f,
                        i.ToString(), EditorStyles.boldLabel);
            }

            // 环标签（非编辑模式也显示）
            if (worldVerts.Length > 0)
            {
                Vector3 labelPos = worldVerts[0];
                string contourLabel = ci == 0 ? "外环" : $"孔{ci}";
                Handles.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                Handles.Label(labelPos, contourLabel, EditorStyles.miniLabel);
            }
        }
    }

    // ── 交互：只作用于当前活动环的顶点列表 ──────────────────────
    private static int _draggingVertex = -1;

    private static void HandleSceneInput(
        string key,
        List<Vector2> verts,
        RectTransform canvasRect,
        UnityEngine.Object targetObject,
        ZoneState state,
        Color color)
    {
        Event e = Event.current;

        List<Vector3> worldVerts = new List<Vector3>();
        for (int i = 0; i < verts.Count; i++)
            worldVerts.Add(AnchoredToWorld(verts[i], canvasRect));

        // ── Hover 检测 ───────────────────────────────────────────
        state.hoveredVertex = -1;
        state.hoveredEdge   = -1;

        for (int i = 0; i < worldVerts.Count; i++)
        {
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldVerts[i]);
            if (Vector2.Distance(screenPos, e.mousePosition) < EDGE_HIT_DIST * 0.8f)
            {
                state.hoveredVertex = i;
                break;
            }
        }

        if (state.hoveredVertex < 0 && worldVerts.Count >= 2)
        {
            for (int i = 0; i < worldVerts.Count; i++)
            {
                int next = (i + 1) % worldVerts.Count;
                Vector2 a = HandleUtility.WorldToGUIPoint(worldVerts[i]);
                Vector2 b = HandleUtility.WorldToGUIPoint(worldVerts[next]);
                Vector2 closest = ClosestPointOnSegment(e.mousePosition, a, b);

                if (Vector2.Distance(e.mousePosition, closest) < EDGE_HIT_DIST)
                {
                    state.hoveredEdge = i;
                    float t = Mathf.InverseLerp(0, Vector2.Distance(a, b), Vector2.Distance(a, closest));
                    state.edgeInsertPos = Vector2.Lerp(verts[i], verts[next], t);
                    break;
                }
            }
        }

        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        // ── 拖拽顶点 ─────────────────────────────────────────────
        if (_draggingVertex >= 0 && _draggingVertex < verts.Count)
        {
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 hitWorld = PlaneIntersect(ray, canvasRect);
                Vector2 newAnchored = WorldToAnchored(hitWorld, canvasRect);

                Undo.RecordObject(targetObject, "Move Polygon Vertex");
                verts[_draggingVertex] = newAnchored;
                EditorUtility.SetDirty(targetObject);
                e.Use();
                SceneView.RepaintAll();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _draggingVertex = -1;
                e.Use();
            }
            return;
        }

        // ── 鼠标按下 ─────────────────────────────────────────────
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 1 && state.hoveredVertex >= 0)
            {
                Undo.RecordObject(targetObject, "Delete Polygon Vertex");
                verts.RemoveAt(state.hoveredVertex);
                state.hoveredVertex = -1;
                EditorUtility.SetDirty(targetObject);
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            if (e.button == 0)
            {
                if (state.hoveredVertex >= 0)
                {
                    _draggingVertex = state.hoveredVertex;
                    GUIUtility.hotControl = controlID;
                    e.Use();
                    return;
                }

                if (state.hoveredEdge >= 0)
                {
                    Undo.RecordObject(targetObject, "Insert Polygon Vertex");
                    verts.Insert(state.hoveredEdge + 1, state.edgeInsertPos);
                    _draggingVertex = state.hoveredEdge + 1;
                    GUIUtility.hotControl = controlID;
                    EditorUtility.SetDirty(targetObject);
                    e.Use();
                    SceneView.RepaintAll();
                    return;
                }

                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 hitWorld = PlaneIntersect(ray, canvasRect);
                Vector2 newAnchored = WorldToAnchored(hitWorld, canvasRect);

                Undo.RecordObject(targetObject, "Add Polygon Vertex");
                verts.Add(newAnchored);
                _draggingVertex = verts.Count - 1;
                GUIUtility.hotControl = controlID;
                EditorUtility.SetDirty(targetObject);
                e.Use();
                SceneView.RepaintAll();
            }
        }

        if (e.type == EventType.MouseMove)
            SceneView.RepaintAll();

        HandleUtility.AddDefaultControl(controlID);
    }

    // ── 坐标转换 ─────────────────────────────────────────────────
    private static Vector3 AnchoredToWorld(Vector2 anchored, RectTransform canvasRect) =>
        canvasRect.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));

    private static Vector2 WorldToAnchored(Vector3 worldPos, RectTransform canvasRect)
    {
        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    private static Vector3 PlaneIntersect(Ray ray, RectTransform canvasRect)
    {
        Plane plane = new Plane(canvasRect.forward, canvasRect.position);
        if (plane.Raycast(ray, out float dist)) return ray.GetPoint(dist);
        return canvasRect.position;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return a + t * ab;
    }
}
#endif
