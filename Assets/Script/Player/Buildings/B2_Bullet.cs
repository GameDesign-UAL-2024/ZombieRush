using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class B2_Bullet : MonoBehaviour
{
    bool initialized;
    float fly_speed = 10f;
    Transform target;
    float attack_point;
    // 新增变量：
    // 追踪时角度变化的速度（单位：度/秒）
    float trackingTurnSpeed;
    // 用于累计追踪过程中旋转的总角度
    float accumulatedTrackingAngle = 0f;
    // 最大允许追踪角度（超过后停止追踪）
    float maxTrackingAngle = 90f;
    // 当前运动方向（初始时指向初始化时target的位置）
    Vector2 currentDirection;
    // 是否已经进入追踪阶段
    bool isTracking = false;
    // 一旦达到最大追踪角度，则停止追踪（保持最后旋转角度）
    bool trackingEnded = false;
    
    // 控制显示与碰撞的组件引用
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;
    Animator animator;
    // 飞行起始时间（通过GlobalTimer获得时间）
    float launchTime;

    // 用于通知发射者（子弹池）当前子弹飞行结束
    UnityEvent<B2_Bullet> onFlightEnd = new UnityEvent<B2_Bullet>();

    //是否困住了敌人
    bool is_caged;

    /// <summary>
    /// 初始化方法：设置目标，追踪量，和飞行结束时调用的事件（用于回收子弹）
    /// </summary>
    /// <param name="target">追踪目标</param>
    /// <param name="trackingTurnSpeed">追踪角度变化速度（度/秒）</param>
    /// <param name="flightEndEvent">飞行结束通知事件</param>
    public void Initialize(Transform target, float trackingTurnSpeed, UnityAction<B2_Bullet> flightEndEvent , float atk)
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
        animator = transform.GetComponent<Animator>();
        if (!initialized)
        {
            if (spriteRenderer) spriteRenderer.enabled = false;
            if (boxCollider) boxCollider.enabled = false;
        }
    }

    void Update()
    {
        // 如果未初始化，则不执行后续逻辑
        if (!initialized || Globals.Instance.Event.current_state == Globals.Events.GameState.pausing)
            return;

        // 检查总飞行时间（3秒）
        if (GlobalTimer.Instance.GetCurrentTime() - launchTime >= 3f && ! is_caged)
        {
            EndFlight();
            return;
        }

        if (is_caged)
        {
            return;
        }

        float rotateSpeed = 3240f; // 每秒旋转角度
        float deltaRotation = rotateSpeed * Time.deltaTime;

        // 当前角度
        float z = transform.eulerAngles.z;

        // 转换为 -180 ~ 180 区间
        z = NormalizeAngle(z);

        // 逆时针旋转
        z -= deltaRotation;

        // 再次规范到 -180 ~ 180 区间
        z = NormalizeAngle(z);

        // 应用旋转
        Vector3 euler = transform.eulerAngles;
        euler.z = z;
        transform.eulerAngles = euler;

        // 子弹沿当前方向运动
        transform.position += (Vector3)(currentDirection * fly_speed * Time.deltaTime);
    }
    float timer = 0f;
    GameObject cage_target;
    Vector2 recorded_pos;
    void LateUpdate()
    {
        if (timer <= 1f && is_caged && cage_target != null && recorded_pos != null)
        {
            cage_target.transform.position = new Vector3(recorded_pos.x,recorded_pos.y,cage_target.transform.position.z);
            timer += Time.deltaTime;
        }

        if (is_caged && timer > 1f)
        {
            EndFlight();
        }
    }
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
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
                is_caged = true;
                cage_target = collision.gameObject;
                recorded_pos = transform.position;
                animator.SetBool("Hitted",true);
                enemy.TakeDamage(transform.position,attack_point,false);
            }
        }            
    }
}
