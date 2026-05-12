using UnityEngine;

public static class ColliderHelper
{
    /// <summary>
    /// 将源 PolygonCollider2D 的所有路径复制到目标 PolygonCollider2D
    /// </summary>
    public static void CopyFrom(this PolygonCollider2D target, PolygonCollider2D source)
    {
        if (source == null || target == null) return;

        // 设置路径数量
        int pathCount = source.pathCount;
        target.pathCount = pathCount;

        // 复制每个路径的点
        for (int i = 0; i < pathCount; i++)
        {
            var points = new System.Collections.Generic.List<Vector2>();
            source.GetPath(i, points);
            target.SetPath(i, points);
        }
        
        // 2. 复制 Layer Overrides 相关属性
        target.layerOverridePriority = source.layerOverridePriority;

        // Unity 2021.2+ 支持 includeLayers / excludeLayers
        // 如果使用旧版本，这些属性可能不存在，请根据实际情况条件编译
        target.includeLayers = source.includeLayers;
        target.excludeLayers = source.excludeLayers;
        
        target.forceSendLayers = source.forceSendLayers;
        target.forceReceiveLayers = source.forceReceiveLayers;
        
        target.contactCaptureLayers = source.contactCaptureLayers;
        target.callbackLayers = source.callbackLayers;
    }
}