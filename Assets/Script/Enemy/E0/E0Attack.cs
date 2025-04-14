using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class E0Attack : MonoBehaviour
{
    // 攻击期间记录伤害数据
    private int hitCount = 0;
    private float lastHitTime = 0f;

    private BoxCollider2D boxCollider;
    // 记录上一帧的 Collider 状态
    private bool lastColliderState;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // 初始状态下关闭 Collider，由动画控制开启
            boxCollider.enabled = false;
            lastColliderState = boxCollider.enabled;
        }
    }

    void Update()
    {
        if (boxCollider != null)
        {
            // 检查状态变化：如果上一帧为开启，而本帧变为关闭，则认为一次攻击周期结束
            if (lastColliderState && !boxCollider.enabled)
            {
                ResetAttackCounters();
            }
            lastColliderState = boxCollider.enabled;
        }
    }

    // 重置攻击期间的记录
    private void ResetAttackCounters()
    {
        hitCount = 0;
        lastHitTime = 0f;
    }

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
