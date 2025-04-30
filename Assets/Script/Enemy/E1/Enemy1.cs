using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(EnemyNav))]
public class Enemy1 : Enemy
{
    // —— 新增：内部状态枚举 —— 
    public enum EnemyState
    {
        Wait,
        Moving,
        Attack
    }

    // —— 新增：存储当前状态的字段与属性 —— 
    private EnemyState _currentState;
    public EnemyState current_state
    {
        get => _currentState;
        set => _currentState = value;
    }

    public override float max_health { get; set; } = 4f;
    public override float current_health { get; set; }
    public override float speed { get; set; } = 4f;
    bool could_hurt;
    public override GameObject target { get; set; }

    [SerializeField] ParticleSystem attacking_effect;
    [SerializeField] Vector3 rightDirectionRotation = Vector3.zero;
    [SerializeField] Vector3 leftDirectionRotation = new Vector3(0f, 0f, 180f);

    private Vector3 particleOriginalLocalPos;
    EnemyNav navigation;
    GlobalTimer g_timer;
    GameObject player;
    GameObject hitted_prefab;
    Animator animator;
    Rigidbody2D RB;
    SpriteRenderer sprite_renderer;
    float behaviour_time;
    float behaviour_gap = 3f;
    bool dying;
    EnemyNav self_nav;
    Dictionary<Vector2, GameObject> player_objects;

    void Start()
    {
        DeactiveAttackEffect();
        current_health = max_health;
        self_nav = GetComponent<EnemyNav>();
        particleOriginalLocalPos = attacking_effect.transform.localPosition;
        hitted_prefab = Addressables.LoadAssetAsync<GameObject>(hitted_prefab_path).WaitForCompletion();

        // 初始化内部状态
        current_state = EnemyState.Wait;

        could_hurt = true;
        g_timer = GlobalTimer.Instance;
        navigation = GetComponent<EnemyNav>();
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite_renderer = GetComponent<SpriteRenderer>();
        behaviour_time = g_timer.GetCurrentTime();

        // 查找玩家对象
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.GetComponent<PlayerController>() != null)
            {
                player = p;
                break;
            }
        }
        target = player;
    }
    void LateUpdate()
    {
        Vector2 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(newPosition.x, 0, 199);
        newPosition.y = Mathf.Clamp(newPosition.y, 0, 199);
        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
    }
    void Update()
    {
        // 简单地不断右移
        transform.position += Vector3.right * 0.1f * Time.deltaTime;

        if (!dying)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            // Wait -> Moving
            if (g_timer.GetCurrentTime() - behaviour_time >= behaviour_gap &&
                current_state == EnemyState.Wait)
            {
                SetState(EnemyState.Moving);
            }
            // 近距离且冷却到 -> Attack
            else if (distance <= 10f &&
                     (g_timer.GetCurrentTime() - behaviour_time) > behaviour_gap &&
                     current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Attack);
            }
            // 进入范围但冷却未到 -> Wait
            else if (distance <= 10f &&
                     current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Wait);
            }

            // 朝向及移动
            if (current_state != EnemyState.Attack)
            {
                float xOffset = player.transform.position.x - transform.position.x;
                bool facingLeft = xOffset < 0;

                if (sprite_renderer.flipX != facingLeft)
                {
                    sprite_renderer.flipX = facingLeft;

                    var shape = attacking_effect.shape;
                    shape.rotation = facingLeft ? leftDirectionRotation : rightDirectionRotation;

                    Vector3 flippedLocalPos = particleOriginalLocalPos;
                    flippedLocalPos.x *= facingLeft ? -1 : 1;
                    attacking_effect.transform.localPosition = flippedLocalPos;
                }

                if (!navigation.is_activing && current_state == EnemyState.Moving)
                {
                    navigation.SetNavActive(true);
                }
            }
        }
        else
        {
            navigation.SetNavActive(false);
        }

        if (current_health <= 0)
        {
            SetState(EnemyState.Wait);
            navigation.SetNavActive(false);
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
        return false;
    }

    IEnumerator AttackDash()
    {
        float startTime = g_timer.GetCurrentTime();
        Vector2 currentDirection = (target.transform.position - transform.position).normalized;

        while (g_timer.GetCurrentTime() - startTime <= 2f)
        {
            // 平滑旋转与移动逻辑...
            // 保持与原有实现一致
            yield return null;
        }

        // 恢复朝向
        bool facingLeft = currentDirection.x < 0;
        sprite_renderer.flipX = facingLeft;
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void OnAttackEnd()
    {
        SetState(EnemyState.Wait);
        behaviour_time = g_timer.GetCurrentTime();
    }

    private void SetState(EnemyState newState)
    {
        if (current_state == newState) return;

        animator.SetBool("Moving", false);
        navigation.SetNavActive(false);
        current_state = newState;

        switch (newState)
        {
            case EnemyState.Wait:
                navigation.SetNavActive(false);
                break;
            case EnemyState.Moving:
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
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
        Destroy(gameObject);
    }
}
