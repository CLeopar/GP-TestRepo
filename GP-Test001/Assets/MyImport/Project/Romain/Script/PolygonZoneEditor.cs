#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 通用 PolygonZone Scene 编辑器
/// 用法：在任意 CustomEditor 里调用 PolygonZoneEditor.Draw(zone, canvasRect, color, serializedObject)
/// </summary>
public static class PolygonZoneEditor
{
    // 每个 zone 独立的编辑状态
    private class ZoneState
    {
        public bool editMode = false;
        public int hoveredVertex = -1;
        public int hoveredEdge = -1;      // 鼠标悬停在哪条边上（用于插入顶点预览）
        public Vector2 edgeInsertPos;     // 插入点预览位置
    }

    private static Dictionary<string, ZoneState> _states = new Dictionary<string, ZoneState>();

    private static ZoneState GetState(string key)
    {
        if (!_states.ContainsKey(key))
            _states[key] = new ZoneState();
        return _states[key];
    }

    // ─── 颜色常量 ───────────────────────────────────────────────
    private static Color ColFill(Color c) => new Color(c.r, c.g, c.b, 0.12f);
    private static Color ColEdge(Color c) => new Color(c.r, c.g, c.b, 0.85f);
    private static Color ColVertex      = new Color(1f, 1f, 1f, 0.95f);
    private static Color ColVertexHover = new Color(1f, 0.85f, 0f, 1f);
    private static Color ColEdgeHover   = new Color(0.4f, 1f, 0.4f, 1f);
    private static Color ColInsertDot   = new Color(0.4f, 1f, 0.4f, 0.9f);

    private const float VERTEX_RADIUS   = 0.05f;   // HandleUtility.GetHandleSize 倍数
    private const float EDGE_HIT_DIST   = 12f;      // 鼠标距边多少像素触发悬停（屏幕像素）

    // ─── 主入口：在 CustomEditor.OnInspectorGUI 里调用 ─────────
    /// <summary>
    /// 绘制 Inspector 内的编辑按钮，并注册 Scene 绘制。
    /// key 用于区分同一个 Inspector 里的多个 zone（如 "joint_0"）。
    /// </summary>
    public static void DrawInspectorGUI(
        string key,
        PolygonZone zone,
        SerializedObject serializedObj,
        Color color)
    {
        var state = GetState(key);

        EditorGUILayout.BeginHorizontal();

        // 编辑模式切换按钮
        GUI.backgroundColor = state.editMode ? new Color(0.4f, 1f, 0.5f) : Color.white;
        if (GUILayout.Button(state.editMode ? "✏ 编辑中（点击退出）" : "✏ 编辑多边形", GUILayout.Height(22)))
        {
            state.editMode = !state.editMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        // 清空按钮
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("清空", GUILayout.Width(48), GUILayout.Height(22)))
        {
            Undo.RecordObject(serializedObj.targetObject, "Clear PolygonZone");
            zone.vertices.Clear();
            EditorUtility.SetDirty(serializedObj.targetObject);
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // 顶点数量提示
        string hint = zone.vertices.Count < 3
            ? $"  ⚠ 需要至少 3 个顶点（当前 {zone.vertices.Count} 个）"
            : $"  ✓ {zone.vertices.Count} 个顶点";
        EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);

        if (state.editMode)
        {
            EditorGUILayout.HelpBox(
                "左键空白处：添加顶点\n左键拖拽顶点：移动\n左键边中间绿点：插入顶点\n右键顶点：删除",
                MessageType.Info);
        }
    }

    // ─── Scene 绘制入口：在 CustomEditor.OnSceneGUI 里调用 ─────
    public static void DrawSceneGUI(
        string key,
        PolygonZone zone,
        RectTransform canvasRect,
        UnityEngine.Object targetObject,
        Color color)
    {
        if (canvasRect == null) return;
        var state = GetState(key);

        // 始终绘制多边形轮廓（不管是否在编辑模式）
        DrawPolygon(zone, canvasRect, color, state);

        if (!state.editMode) return;

        // 编辑模式：处理交互
        HandleSceneInput(key, zone, canvasRect, targetObject, state, color);
    }

    // ─── 绘制多边形 ──────────────────────────────────────────────
    private static void DrawPolygon(
        PolygonZone zone,
        RectTransform canvasRect,
        Color color,
        ZoneState state)
    {
        var verts = zone.vertices;
        if (verts == null || verts.Count == 0) return;

        // 转换所有顶点到世界坐标
        Vector3[] worldVerts = new Vector3[verts.Count];
        for (int i = 0; i < verts.Count; i++)
            worldVerts[i] = AnchoredToWorld(verts[i], canvasRect);

        // 填充
        if (verts.Count >= 3)
        {
            Handles.color = ColFill(color);
            // 用扇形三角剖分填充（适合凸多边形；凹多边形用射线法时填充会有瑕疵，但足够直观）
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
        Handles.color = ColEdge(color);
        for (int i = 0; i < worldVerts.Length; i++)
        {
            int next = (i + 1) % worldVerts.Length;

            // 悬停边用绿色高亮
            if (state.editMode && state.hoveredEdge == i)
                Handles.color = ColEdgeHover;
            else
                Handles.color = ColEdge(color);

            Handles.DrawLine(worldVerts[i], worldVerts[next], 2f);
        }

        // 插入点预览
        if (state.editMode && state.hoveredEdge >= 0 && state.hoveredVertex < 0)
        {
            Handles.color = ColInsertDot;
            Vector3 insertWorld = AnchoredToWorld(state.edgeInsertPos, canvasRect);
            float sz = HandleUtility.GetHandleSize(insertWorld) * VERTEX_RADIUS * 0.8f;
            Handles.DotHandleCap(0, insertWorld, Quaternion.identity, sz, EventType.Repaint);
        }

        // 顶点
        for (int i = 0; i < worldVerts.Length; i++)
        {
            Handles.color = (state.editMode && state.hoveredVertex == i)
                ? ColVertexHover : ColVertex;
            float sz = HandleUtility.GetHandleSize(worldVerts[i]) * VERTEX_RADIUS;
            Handles.DotHandleCap(0, worldVerts[i], Quaternion.identity, sz, EventType.Repaint);

            // 顶点序号标签
            if (state.editMode)
                Handles.Label(worldVerts[i] + Vector3.up * sz * 2.5f,
                    i.ToString(), EditorStyles.boldLabel);
        }
    }

    // ─── 交互处理 ────────────────────────────────────────────────
    private static int _draggingVertex = -1;

    private static void HandleSceneInput(
        string key,
        PolygonZone zone,
        RectTransform canvasRect,
        UnityEngine.Object targetObject,
        ZoneState state,
        Color color)
    {
        Event e = Event.current;
        var verts = zone.vertices;

        // 把所有顶点转为世界坐标备用
        List<Vector3> worldVerts = new List<Vector3>();
        for (int i = 0; i < verts.Count; i++)
            worldVerts.Add(AnchoredToWorld(verts[i], canvasRect));

        // ── 每帧更新 hover 状态 ──────────────────────────────────
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

        // 悬停在边上（只在没悬停顶点时检测）
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

                    // 计算插入点的 anchoredPosition（线性插值）
                    float t = Mathf.InverseLerp(0, Vector2.Distance(a, b),
                                Vector2.Distance(a, closest));
                    state.edgeInsertPos = Vector2.Lerp(verts[i], verts[next], t);
                    break;
                }
            }
        }

        // 阻止 Unity 默认选中行为
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        // ── 拖拽顶点 ─────────────────────────────────────────────
        if (_draggingVertex >= 0 && _draggingVertex < verts.Count)
        {
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                // 把鼠标位置从屏幕坐标转为 anchoredPosition
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
            // 右键 → 删除顶点
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
                // 左键点顶点 → 开始拖拽
                if (state.hoveredVertex >= 0)
                {
                    _draggingVertex = state.hoveredVertex;
                    GUIUtility.hotControl = controlID;
                    e.Use();
                    return;
                }

                // 左键点边中间 → 插入顶点
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

                // 左键点空白 → 添加新顶点
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

        // 持续重绘（更新 hover 效果）
        if (e.type == EventType.MouseMove)
            SceneView.RepaintAll();

        HandleUtility.AddDefaultControl(controlID);
    }

    // ─── 坐标转换工具 ────────────────────────────────────────────

    private static Vector3 AnchoredToWorld(Vector2 anchored, RectTransform canvasRect)
    {
        return canvasRect.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));
    }

    private static Vector2 WorldToAnchored(Vector3 worldPos, RectTransform canvasRect)
    {
        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    /// <summary>射线与 Canvas 平面求交</summary>
    private static Vector3 PlaneIntersect(Ray ray, RectTransform canvasRect)
    {
        Plane plane = new Plane(canvasRect.forward, canvasRect.position);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return canvasRect.position;
    }

    /// <summary>点到线段最近点</summary>
    private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return a + t * ab;
    }
}
#endif