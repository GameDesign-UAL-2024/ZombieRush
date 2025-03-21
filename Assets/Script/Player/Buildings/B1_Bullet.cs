using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class B1_Bullet : MonoBehaviour
{
    bool initialized;
    float fly_speed = 3f;
    Transform target;
    float attack_point;
    // 新增变量：
    // 追踪时角度变化的速度（单位：度/秒）
    float trackingTurnSpeed;
    // 用于累计追踪过程中旋转的总角度
    float accumulatedTrackingAngle = 0f;
    // 最大允许追踪角度（超过后停止追踪）
    float maxTrackingAngle = 60f;
    // 当前运动方向（初始时指向初始化时target的位置）
    Vector2 currentDirection;
    // 是否已经进入追踪阶段
    bool isTracking = false;
    // 一旦达到最大追踪角度，则停止追踪（保持最后旋转角度）
    bool trackingEnded = false;
    
    // 控制显示与碰撞的组件引用
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;

    // 飞行起始时间（通过GlobalTimer获得时间）
    float launchTime;

    // 用于通知发射者（子弹池）当前子弹飞行结束
    UnityEvent<B1_Bullet> onFlightEnd = new UnityEvent<B1_Bullet>();

    /// <summary>
    /// 初始化方法：设置目标，追踪量，和飞行结束时调用的事件（用于回收子弹）
    /// </summary>
    /// <param name="target">追踪目标</param>
    /// <param name="trackingTurnSpeed">追踪角度变化速度（度/秒）</param>
    /// <param name="flightEndEvent">飞行结束通知事件</param>
    public void Initialize(Transform target, float trackingTurnSpeed, UnityAction<B1_Bullet> flightEndEvent , float atk)
    {
        this.target = target;
        this.trackingTurnSpeed = trackingTurnSpeed;
        this.attack_point = atk;
        onFlightEnd.AddListener(flightEndEvent);
        // 标记已初始化
        initialized = true;
        // 记录发射时间（全局时间）
        launchTime = GlobalTimer.Instance.GetCurrentTime();

        // 获取组件并设置启用状态
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (boxCollider) boxCollider.enabled = true;

        // 计算初始发射方向：朝向初始化时target的位置
        Vector3 diff = target.position - transform.position;
        currentDirection = diff.normalized;
    }

    // 在Awake中预先获取组件，如果未初始化则禁用显示与碰撞
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (!initialized)
        {
            if (spriteRenderer) spriteRenderer.enabled = false;
            if (boxCollider) boxCollider.enabled = false;
        }
    }

    void Update()
    {
        // 如果未初始化，则不执行后续逻辑
        if (!initialized)
            return;

        // 检查总飞行时间（3秒）
        if (GlobalTimer.Instance.GetCurrentTime() - launchTime >= 3f)
        {
            EndFlight();
            return;
        }

        // 如果 target 不为 null，则进行追踪逻辑
        if (target != null)
        {
            // 判断是否满足追踪条件：当子弹距离当前 target 位置小于等于2f时开始追踪
            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (!isTracking && distanceToTarget <= 2f)
            {
                isTracking = true;
            }

            // 如果处于追踪阶段且尚未达到最大追踪角度，则进行角度调整
            if (isTracking && !trackingEnded)
            {
                // 计算从当前位置指向 target 的期望方向
                Vector2 desiredDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
                // 计算当前方向与期望方向的夹角（正负表示旋转方向）
                float angleDiff = Vector2.SignedAngle(currentDirection, desiredDirection);
                // 计算本帧允许旋转的最大角度
                float maxRotation = trackingTurnSpeed * Time.deltaTime;
                // 限制旋转量在允许范围内
                float rotation = Mathf.Clamp(angleDiff, -maxRotation, maxRotation);

                // 判断累计旋转角度是否会超出最大追踪角度
                if (Mathf.Abs(accumulatedTrackingAngle + rotation) > maxTrackingAngle)
                {
                    // 若超出，则仅允许旋转到达最大追踪角度，并停止追踪
                    float allowedRotation = maxTrackingAngle - Mathf.Abs(accumulatedTrackingAngle);
                    rotation = Mathf.Sign(rotation) * allowedRotation;
                    trackingEnded = true;
                }
                else
                {
                    accumulatedTrackingAngle += rotation;
                }

                // 将当前运动方向旋转相应角度
                currentDirection = Quaternion.Euler(0, 0, rotation) * currentDirection;
            }
        }
        // 若 target 为 null，则不进行追踪，但依然会沿 currentDirection 前进

        // 子弹沿当前方向运动
        transform.position += (Vector3)(currentDirection * fly_speed * Time.deltaTime);
        // 始终更新旋转，使子弹正朝向其运动方向（注意默认朝上，所以需要偏移-90度）
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 飞行结束时调用的方法：通知发射者回收该子弹，并关闭自身
    void EndFlight()
    {
        if (onFlightEnd != null)
        {
            onFlightEnd.Invoke(this);
        }
        // 使用对象池时通常采用SetActive(false)进行回收
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Enemy")
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(transform.position,attack_point,false);
            }
        }            
    }
}
