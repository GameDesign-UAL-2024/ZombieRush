using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy3Bullet : MonoBehaviour
{
    // —— 新增字段 —— 
    private Vector3 startPosition;
    private const float maxFlightDistance = 12f;

    private float damage;
    private Vector2 direction;
    private float currentSpeed;
    private float maxSpeed;
    private float acceleration;
    private Enemy owner;
    private Vector3 originalScale;
    private float signX;

    /// <summary>
    /// 初始化子弹参数
    /// </summary>
    public void Initialize(Enemy owner, Vector2 dir, float initialSpeed, float maxSpeed, float acceleration, float damage)
    {
        this.owner = owner;
        direction = dir.normalized;
        currentSpeed = initialSpeed;
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;
        this.damage = damage;

        originalScale = transform.localScale;

        // 记录起始位置
        startPosition = transform.position;

        // figure out flip
        signX = direction.x >= 0f ? 1f : -1f;

        // start small, flipped
        float startScale = 0.2f;
        Vector3 baseScale = originalScale * startScale;
        transform.localScale = new Vector3(baseScale.x * signX,
                                           baseScale.y,
                                           baseScale.z);

        // rotate to face flight direction (plus 180° if flipped)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (signX < 0f) angle += 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        // 线性加速
        if (currentSpeed < maxSpeed)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
        }

        // 移动
        transform.position += (Vector3)(direction * currentSpeed * Time.deltaTime);

        // —— 每帧检查飞行距离 —— 
        if (Vector3.Distance(startPosition, transform.position) > maxFlightDistance)
        {
            DestroyBullet();
            return;
        }

        float t = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);
        float uniformScale = Mathf.Lerp(0.2f, 1f, t);

        // 重新应用翻转
        transform.localScale = new Vector3(
            uniformScale * Mathf.Abs(originalScale.x) * signX,
            uniformScale * Mathf.Abs(originalScale.y),
            uniformScale * Mathf.Abs(originalScale.z));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var pc = collision.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ReduceLife(damage);
                DestroyBullet();
            }
        }
        else if (collision.CompareTag("PlayerObjects"))
        {
            var pc = collision.GetComponent<Buildings>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
                DestroyBullet();
            }
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
