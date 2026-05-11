using UnityEngine;

public class RequiredZone : MonoBehaviour
{
    public PolygonZone zone = new PolygonZone();

    [Tooltip("该区域没有被身体任何部位触碰时扣除的分数（0~100）")]
    [Range(0f, 100f)]
    public float penaltyScore = 5f;
}