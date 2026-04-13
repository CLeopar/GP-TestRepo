using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PolygonZone
{
    public List<Vector2> vertices = new List<Vector2>();

    public bool Contains(Vector2 point)
    {
        int count = vertices.Count;
        if (count < 3) return false;

        bool inside = false;
        int j = count - 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 vi = vertices[i];
            Vector2 vj = vertices[j];
            if (((vi.y > point.y) != (vj.y > point.y)) &&
                (point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x))
                inside = !inside;
            j = i;
        }
        return inside;
    }
}