using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class E8_ATKrange : MonoBehaviour
{
    // 攻击期间记录伤害数据
    private int hitCount = 0;
    private float lastHitTime = 0f;

    private BoxCollider2D boxCollider;
    // 记录上一帧的 Collider 状态
    private bool lastColliderState;

    // 当 Collider 检测到玩家进入时，根据计数和时间间隔决定是否造成伤害
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hitCount < 2 && (Time.time - lastHitTime) >= 0.5f)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ReduceLife(1);
                    hitCount++;
                    lastHitTime = Time.time;
                }
            }
        }
    }
}
