using UnityEngine;

public class AlphaMaskReader_2D : MonoBehaviour
{
    public int groupID = 0;
    public bool isMultiGroup = false;
    public int[] multiGroupIDs;

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
            if (isMultiGroup && multiGroupIDs != null && multiGroupIDs.Length > 0)
            {
                AlphaMaskManager_2D.Instance.SetupReaderMulti(gameObject, multiGroupIDs);
            }
            else
            {
                AlphaMaskManager_2D.Instance.SetupReader(gameObject, groupID);
            }
            isInitialized = true;
        }
    }

    void OnDestroy()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.material != null)
        {
            Destroy(renderer.material);
        }
    }
}