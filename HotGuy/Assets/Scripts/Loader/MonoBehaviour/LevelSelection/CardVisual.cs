using UnityEngine;

public class CardVisual : MonoBehaviour
{
    [Header("References")]
    public Transform tiltParent;          // 必须是子物体！负责旋转
    public CardController parentCard;     // 必须是父物体！负责碰撞检测
    
    [Header("Settings")]
    public float manualTiltAmount = 15f;  // 最大倾斜角度
    public float autoTiltAmount = 2f;     // 自动晃动的幅度
    public float tiltSpeed = 15f;         // 倾斜平滑速度

    private float savedIndex;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        if (parentCard == null) parentCard = GetComponentInParent<CardController>();
        if (tiltParent == null) tiltParent = this.transform; 
    }

    void Update()
    {
        if (parentCard == null || tiltParent == null) return;

        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        
        float wobbleModifier = parentCard.isHovering ? 0.05f : 1f;
        float sine = Mathf.Sin(Time.time + savedIndex) * autoTiltAmount * wobbleModifier;
        float cosine = Mathf.Cos(Time.time + savedIndex) * autoTiltAmount * wobbleModifier;

        float targetTiltX = 0f;
        float targetTiltY = 0f;

        if (parentCard.isHovering)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(mainCam.transform.position.z - parentCard.transform.position.z); 
            Vector3 worldMousePos = mainCam.ScreenToWorldPoint(mousePos);
            
            // 依赖永远不转的父物体来定位鼠标
            Vector3 localPos = parentCard.transform.InverseTransformPoint(worldMousePos);

            // 计算目标角度（如果发现倾斜方向反了，把这里的负号去掉即可）
            targetTiltX = Mathf.Clamp(localPos.y * manualTiltAmount, -manualTiltAmount, manualTiltAmount);
            targetTiltY = Mathf.Clamp(-localPos.x * manualTiltAmount, -manualTiltAmount, manualTiltAmount);
        }

        
        Quaternion targetRotation = Quaternion.Euler(targetTiltX + sine, targetTiltY + cosine, 0f);
        tiltParent.localRotation = Quaternion.Lerp(tiltParent.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
    }
}