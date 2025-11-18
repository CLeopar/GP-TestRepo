using UnityEngine;

public class CatController : MonoBehaviour
{
    [Header("小猫设置")]
    public GameObject catPrefab;        // 小猫预制体
    public int catCount = 5;            // 小猫数量
    public Transform catContainer;      // 存放小猫的父物体
    
    void Start()
    {
        // 检查是否分配了预制体
        if (catPrefab == null)
        {
            Debug.LogError("请将小猫预制体拖拽到CatController的catPrefab字段！");
            return;
        }
        
        // 生成指定数量的小猫
        for (int i = 0; i < catCount; i++)
        {
            CreateCat();
        }
    }
    
    void CreateCat()
    {
        // 实例化小猫
        GameObject newCat = Instantiate(catPrefab);
        
        // 如果有容器，将小猫放入容器中
        if (catContainer != null)
        {
            newCat.transform.SetParent(catContainer);
        }
        
        // 设置随机名字（方便识别）
        newCat.name = "Cat_" + Random.Range(1000, 9999);
        
        // 设置随机位置
        SetRandomCatPosition(newCat);
    }
    
    void SetRandomCatPosition(GameObject cat)
    {
        Camera mainCamera = Camera.main;
        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector2 cameraCenter = mainCamera.transform.position;
        
        // 设置随机位置
        cat.transform.position = new Vector2(
            Random.Range(cameraCenter.x - cameraWidth, cameraCenter.x + cameraWidth),
            Random.Range(cameraCenter.y - cameraHeight, cameraCenter.y + cameraHeight)
        );
    }
}