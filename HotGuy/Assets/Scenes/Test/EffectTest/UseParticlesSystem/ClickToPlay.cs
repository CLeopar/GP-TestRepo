using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToPlay : MonoBehaviour
{
    public ParticleSystem clickEffect; // 在 Inspector 里把粒子系统拖进来

    // 这个方法会在鼠标点击带碰撞体的物体时自动触发
    void OnMouseDown()
    {
        if (clickEffect != null)
        {
            clickEffect.Play(); // 播放粒子
        }
    }
}