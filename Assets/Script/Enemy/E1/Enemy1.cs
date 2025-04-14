using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyNav))]
public class Enemy1 : Enemy
{
    public override float max_health { get; set; } = 8f;
    public override float current_health { get; set; }
    public override float speed { get; set; } = 4f;
    bool could_hurt;
    public override GameObject target { get; set; }
    public override EnemyState current_state { get; set; }
    [SerializeField] ParticleSystem attacking_effect;
    [SerializeField] Vector3 rightDirectionRotation = Vector3.zero;
    [SerializeField] Vector3 leftDirectionRotation = new Vector3(0f, 0f, 180f); // 或其他合适角度
    private Vector3 particleOriginalLocalPos;
    EnemyNav navigation;
    GlobalTimer g_timer;
    GameObject player;
    Animator animator;
    Rigidbody2D RB;
    SpriteRenderer sprite_renderer;
    // 行为起始时刻和间隔
    float behaviour_time;
    float behaviour_gap = 3f;
    bool dying;
    EnemyNav self_nav;
    Dictionary<Vector2, GameObject> player_objects;

    void Start()
    {
        DeactiveAttackEffect();
        current_health = max_health;
        self_nav = transform.GetComponent<EnemyNav>();
        particleOriginalLocalPos = attacking_effect.transform.localPosition;

        // 查找场景中拥有 PlayerController 组件的玩家对象
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.GetComponent<PlayerController>() != null)
            {
                player = p;
            }
        }
        current_state = EnemyState.Wait;
        could_hurt = true;
        g_timer = GlobalTimer.Instance;
        navigation = transform.GetComponent<EnemyNav>();
        RB = transform.GetComponent<Rigidbody2D>();
        animator = transform.GetComponent<Animator>();
        sprite_renderer = transform.GetComponent<SpriteRenderer>();
        behaviour_time = g_timer.GetCurrentTime();

        // 备用查找玩家对象（可选）
        GameObject player_obj = GameObject.FindGameObjectWithTag("Player");
        if (player_obj != null)
        {
            if (player_obj.GetComponent<PlayerController>() != null)
            {
                player = player_obj;
            }
        }
        target = player;
    }

    void Update()
    {
        transform.position += Vector3.right * 0.1f * Time.deltaTime;
        
        if (!dying)
        {
            // 计算敌人与玩家之间的距离
            float distance = Vector2.Distance(transform.position, player.transform.position);
            // 等待状态到移动状态的转换
            if (g_timer.GetCurrentTime() - behaviour_time >= behaviour_gap && current_state == EnemyState.Wait)
            {
                SetState(EnemyState.Moving);
            }
            // 优先攻击逻辑（在攻击范围内且冷却时间到）
            if (distance <= 10f &&
                (g_timer.GetCurrentTime() - behaviour_time) > behaviour_gap &&
                current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Attack);
            }
            // 进入范围但冷却未到 → 等待（只有在非攻击状态时才设置）
            else if (distance <= 10f &&
                    current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Wait);
            }
            
            if (current_state != EnemyState.Attack)
            {
                float xOffset = target.transform.position.x - transform.position.x;
                bool facingLeft = xOffset < 0;

                if (sprite_renderer.flipX != facingLeft)
                {
                    // 设置视觉翻转
                    sprite_renderer.flipX = facingLeft;

                    // 设置粒子朝向
                    var shape = attacking_effect.shape;
                    shape.rotation = facingLeft ? leftDirectionRotation : rightDirectionRotation;

                    // 设置粒子挂点位置翻转
                    Vector3 flippedLocalPos = particleOriginalLocalPos;
                    flippedLocalPos.x *= facingLeft ? -1 : 1;
                    attacking_effect.transform.localPosition = flippedLocalPos;
                }

                // 启动导航（如需要）
                if (!navigation.is_activing && current_state == EnemyState.Moving)
                {
                    navigation.SetNavActive(true);
                }
            }
        }
        else
        {
            // 如果处于死亡状态，确保寻路关闭
            navigation.SetNavActive(false);
        }

        // 当生命值耗尽时，切换状态并执行死亡相关逻辑
        if (current_health <= 0)
        {
            SetState(EnemyState.Wait);
            dying = true;
            could_hurt = false;
            animator.SetBool("Dead", true);
        }
    }

    void FixedUpdate()
    {
        RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
    }

    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }

    public override bool TakeDamage(Vector3 source, float amount, bool Instant_kill)
    {
        if (could_hurt)
        {
            current_health -= amount;
            animator.SetTrigger("Hurt");
            Vector2 goback_direction = -((Vector2)source - (Vector2)transform.position).normalized;
            RB.velocity = goback_direction * 2f;
            navigation.SetNavActive(false);
            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerator AttackDash()
    {
        float startTime = GlobalTimer.Instance.GetCurrentTime();

        Vector2 currentDirection = (target.transform.position - transform.position).normalized;
        float maxDegreesPerSecond = 20f;
        bool facingLeft;
        while ((GlobalTimer.Instance.GetCurrentTime() - startTime) <= 2f)
        {
            // 平滑朝向目标调整
            Vector2 toTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector2.Angle(currentDirection, toTarget);

            if (angleToTarget < 90f)
            {
                float cross = currentDirection.x * toTarget.y - currentDirection.y * toTarget.x;
                float sign = Mathf.Sign(cross);

                float maxAngleStep = maxDegreesPerSecond * Time.deltaTime;
                float actualAngle = Mathf.Min(maxAngleStep, angleToTarget);

                currentDirection = Quaternion.Euler(0, 0, actualAngle * sign) * currentDirection;
            }

            currentDirection.Normalize();

            // 移动
            if (RB != null)
            {
                RB.velocity = currentDirection * 10f;
            }

// 设置对象朝向：Z轴旋转朝向当前冲刺方向
            float zAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            // 如果默认朝左（flipX为true），则修正角度
            if (sprite_renderer.flipX)
            {
                zAngle -= 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, zAngle);

            // 更新视觉朝向（基于当前方向，而非目标位置）
            facingLeft = currentDirection.x < 0;
            sprite_renderer.flipX = facingLeft;

            // 翻转粒子位置
            Vector3 flippedLocalPos = particleOriginalLocalPos;
            flippedLocalPos.x *= facingLeft ? -1 : 1;
            attacking_effect.transform.localPosition = flippedLocalPos;

            // 翻转粒子发射方向
            var shape = attacking_effect.shape;
            shape.rotation = facingLeft ? leftDirectionRotation : rightDirectionRotation;

            yield return null;
        }

        // 冲刺结束后恢复Z轴旋转为正常
        facingLeft = currentDirection.x < 0;
        sprite_renderer.flipX = facingLeft;
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
    // 然后在攻击动画事件末尾调用：
    public void OnAttackEnd()
    {
        SetState(EnemyState.Wait);
        behaviour_time = g_timer.GetCurrentTime(); // 真正刷新冷却时间
    }
    private void SetState(EnemyState newState)
    {
        // 重置所有状态对应的动画参数和关闭寻路
        animator.SetBool("Moving", false);
        navigation.SetNavActive(false);
        current_state = newState;
        switch (newState)
        {
            case EnemyState.Wait:
                navigation.SetNavActive(false);
                break;
            case EnemyState.Moving:
                // 进入移动状态时，开启移动动画和寻路
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
                // 进入攻击状态时触发攻击动画，并关闭寻路
                animator.SetTrigger("Attacking");
                navigation.SetNavActive(false);
                break;
        }
    }

    public void ActiveAttackEffect()
    {
        attacking_effect.Play();
    }

    public void DeactiveAttackEffect()
    {
        attacking_effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void DestroyEnemy()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            Globals.Datas.EnemyPool.Remove(this);
            
        }
        Destroy(this.gameObject);
    }
}
