using UnityEngine;

public class Circle2D_Gizmos : MonoBehaviour
{
    [Header("圆参数")]
    public float radius = 2f;          // 半径

    public Color lineColor = Color.white;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = lineColor;
        // 绘制线框圆
        DrawWireCircle(transform.position, radius);
        // 也可绘制实心圆（圆盘）
        // Gizmos.DrawSphere(transform.position, radius); // 这是球体，从顶部看是圆
    }

    private void DrawWireCircle(Vector3 center, float r)
    {
        int segments = 64;
        Vector3 prevPoint = center + new Vector3(r, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;
            Vector3 newPoint = center + new Vector3(x, y, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}