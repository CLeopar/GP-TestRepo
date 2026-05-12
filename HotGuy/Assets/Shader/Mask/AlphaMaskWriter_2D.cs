using UnityEngine;

public class AlphaMaskWriter_2D : MonoBehaviour
{
    public int groupID = 0;
    public string groupName = "Default";
    
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;
        
        if (AlphaMaskManager_2D.Instance != null)
        {
            AlphaMaskManager_2D.Instance.SetupWriter(gameObject, groupID);
            isInitialized = true;
        }
    }

    void OnDestroy()
    {
        // 清理材质
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.material != null)
        {
            Destroy(renderer.material);
        }
    }
}