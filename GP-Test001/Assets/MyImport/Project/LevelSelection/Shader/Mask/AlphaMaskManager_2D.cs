using System.Collections.Generic;
using UnityEngine;

public class AlphaMaskManager_2D: MonoBehaviour
{
    private static AlphaMaskManager_2D _instance;
    public static AlphaMaskManager_2D Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AlphaMaskManager_2D>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AlphaMaskManager_2D");
                    _instance = go.AddComponent<AlphaMaskManager_2D>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private Dictionary<int, int> groupRenderQueue = new Dictionary<int, int>();
    private int baseRenderQueue = 3000;

    // 为物体分配材质
    public void SetupWriter(GameObject obj, int groupID)
    {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        // 创建材质实例
        Material mat = new Material(Shader.Find("Custom/AlphaMaskWrite_2D"));
        mat.SetInt("_GroupID", groupID);
        
        // 设置渲染顺序，确保Write在Read之前
        if (!groupRenderQueue.ContainsKey(groupID))
        {
            groupRenderQueue[groupID] = baseRenderQueue + groupID * 10;
        }
        mat.renderQueue = groupRenderQueue[groupID];
        
        renderer.material = mat;
    }

    public void SetupReader(GameObject obj, int groupID)
    {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        Material mat = new Material(Shader.Find("Custom/AlphaMaskRead_2D"));
        mat.SetInt("_GroupID", groupID);
        
        // Reader的渲染顺序要在Writer之后
        if (groupRenderQueue.ContainsKey(groupID))
        {
            mat.renderQueue = groupRenderQueue[groupID] + 5;
        }
        else
        {
            mat.renderQueue = baseRenderQueue + groupID * 10 + 5;
        }
        
        renderer.material = mat;
    }

    public void SetupReaderMulti(GameObject obj, int[] groupIDs)
    {
        // 对于多组读取，需要使用更复杂的shader逻辑
        // 这里简化处理，使用第一个组
        if (groupIDs.Length > 0)
        {
            SetupReader(obj, groupIDs[0]);
        }
    }
}