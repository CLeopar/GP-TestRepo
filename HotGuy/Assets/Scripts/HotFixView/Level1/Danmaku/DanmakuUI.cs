using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuUI : MonoBehaviour
{
    [Header("UI引用")]
    public Image AvatarImage;
    public TextMeshProUGUI ContentText;

    [Header("头像库")]
    public Sprite[] AvatarSprites;

    public void Init(DanmakuData data)
    {
        ContentText.text = data.Content;
        
        // 随机头像
        if (AvatarSprites != null && AvatarSprites.Length > 0)
        {
            int index = Random.Range(0, AvatarSprites.Length);
            AvatarImage.sprite = AvatarSprites[index];
        }
    }

    public void ForceDestroy()
    {
        Destroy(gameObject);
    }
}