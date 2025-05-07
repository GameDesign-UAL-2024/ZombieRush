using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BulletBehaviour_0 : Bullet
{
    public int pass_over_time = 0;
    Vector2 direction;
    float moveSpeed     = 10;
    float damage;
    float acceleration  = 2;
    float exist_time    = 2;
    float timer;
    bool initialzed     = false;
    public override string hit_sound_path { get; set; } = "Audios/Player/HitSound";

    Rigidbody2D RB;
    Vector3 source;

    void FixedUpdate()
    {
        if (initialzed && RB != null)
        {
            timer += Time.deltaTime;

            // 推进
            if (RB.velocity.magnitude < moveSpeed)
                RB.velocity += acceleration * direction;

            // 朝向飞行方向
            RotateToVelocity();

            // 超时自毁
            if (timer > exist_time)
                Destroy(gameObject);
        }
    }

    // 计算当前速度方向并让子弹朝该方向“抬头”（贴图默认朝上）
    private void RotateToVelocity()
    {
        Vector2 vel = RB.velocity;
        if (vel.sqrMagnitude > 0.01f)
        {
            // 计算角度：Atan2 返回 [-180,180]，这里要减 90° 让“贴图向上”对齐
            float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public override void Initialize(
        Vector3 Source,
        Vector2 target,
        Rigidbody2D rb,
        float bullet_speed      = 10,
        float bullet_damage     = 1,
        float bullet_exist_time = 2
    )
    {
        RB           = rb;
        source       = Source;
        moveSpeed    = bullet_speed;
        damage       = bullet_damage;
        exist_time   = bullet_exist_time;
        // 原点到目标向量
        direction    = (target - (Vector2)rb.transform.position).normalized;
        initialzed   = true;

        // 立刻设置一次初始旋转
        float initAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, initAngle);
    }

    public override void AddSpeed(float speed)
    {
        moveSpeed += speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                GlobalEventBus.OnHitEnemy.Invoke(enemy.gameObject);
                if (enemy.TakeDamage(source, damage, false))
                    pass_over_time -= 1;
            }

            if (pass_over_time <= 0)
                Destroy(gameObject);
        }
    }
}
