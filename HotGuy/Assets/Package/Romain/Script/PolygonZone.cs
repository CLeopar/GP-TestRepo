using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PolygonZone
{
    /// <summary>
    /// 多环轮廓列表。
    /// contours[0] = 外环（判定区域主体）
    /// contours[1], [2]... = 镂空孔洞
    /// 使用奇偶规则（Even-Odd Rule）：点穿越奇数个轮廓边界时判定为"内部"。
    /// 画法：外环正常画，在外环内部再画一个内环即为镂空，无需关心顺逆时针方向。
    /// </summary>
    public List<Contour> contours = new List<Contour>();

    // ── 向后兼容：旧版直接访问 vertices 的代码仍可用 ────────────
    /// <summary>返回第一个轮廓的顶点列表（兼容旧代码）。若不存在则自动创建。</summary>
    public List<Vector2> vertices
    {
        get
        {
            if (contours.Count == 0)
                contours.Add(new Contour());
            return contours[0].vertices;
        }
    }

    // ── Contains：奇偶规则，支持镂空 ────────────────────────────
    /// <summary>判断点是否在有效区域内（奇偶规则，支持多环镂空）。</summary>
    public bool Contains(Vector2 point)
    {
        int crossings = 0;
        foreach (var contour in contours)
        {
            if (contour == null || contour.vertices == null) continue;
            crossings += CountCrossings(contour.vertices, point);
        }
        return (crossings % 2) == 1;
    }

    private static int CountCrossings(List<Vector2> verts, Vector2 point)
    {
        int count = verts.Count;
        if (count < 3) return 0;

        int crossings = 0;
        int j = count - 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 vi = verts[i];
            Vector2 vj = verts[j];
            if (((vi.y > point.y) != (vj.y > point.y)) &&
                (point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x))
                crossings++;
            j = i;
        }
        return crossings;
    }

    // ── 轮廓管理工具 ─────────────────────────────────────────────
    public void AddContour()               => contours.Add(new Contour());
    public void RemoveContour(int index)   { if (index >= 0 && index < contours.Count) contours.RemoveAt(index); }
    public int  ContourCount               => contours.Count;
}

// ── Contour：单个环的顶点列表 ────────────────────────────────────
[System.Serializable]
public class Contour
{
    public List<Vector2> vertices = new List<Vector2>();
}
