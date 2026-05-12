using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class ClickAnimatorActivator : MonoBehaviour
{
    [Header("Animator")]
    public Animator targetAnimator;
    public string triggerName = "MyTrigger";

    private Camera mainCam;
    private HashSet<Collider2D> selfColliders;
    private int clickLayerMask;

    private void Awake()
    {
        mainCam = Camera.main;

        // 只收集【本体】上的 Collider2D（不含子物体）
        selfColliders = new HashSet<Collider2D>(
            GetComponents<Collider2D>()
        );

        // 只检测 Clickable Layer
        clickLayerMask = LayerMask.GetMask("Clickable");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos =
                mainCam.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(
                mouseWorldPos,
                Vector2.zero,
                0f,
                clickLayerMask
            );

            if (hit.collider == null) return;

            // 只允许命中“本体 Collider”
            if (selfColliders.Contains(hit.collider))
            {
                StartCoroutine(FireAndResetTrigger());
            }
        }
    }

    private IEnumerator FireAndResetTrigger()
    {
        if (targetAnimator == null) yield break;

        targetAnimator.SetTrigger(triggerName);
        yield return null; // 等一帧
        targetAnimator.ResetTrigger(triggerName);
    }
}