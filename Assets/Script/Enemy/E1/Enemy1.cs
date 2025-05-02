using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(EnemyNav))]
public class Enemy1 : Enemy
{
    public enum EnemyState
    {
        Wait,
        Moving,
        Attack
    }

    private EnemyState _currentState;
    public EnemyState current_state
    {
        get => _currentState;
        set => _currentState = value;
    }

    public override float max_health { get; set; } = 4f;
    public override float current_health { get; set; }
    public override float speed { get; set; } = 4f;
    public override GameObject target { get; set; }

    bool could_hurt;
    bool dying;
    float behaviour_time;
    float behaviour_gap = 3f;

    [SerializeField] ParticleSystem attacking_effect;

    EnemyNav navigation;
    GlobalTimer g_timer;
    GameObject player;
    GameObject hitted_prefab;
    Animator animator;
    Rigidbody2D RB;
    SpriteRenderer sprite_renderer;
    Vector3 originalScale;

    void Start()
    {
        DeactiveAttackEffect();
        current_health = max_health;
        hitted_prefab = Addressables.LoadAssetAsync<GameObject>(hitted_prefab_path).WaitForCompletion();

        navigation = GetComponent<EnemyNav>();
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite_renderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        current_state = EnemyState.Wait;
        could_hurt = true;
        g_timer = GlobalTimer.Instance;
        behaviour_time = g_timer.GetCurrentTime();

        // 查找玩家
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.GetComponent<PlayerController>() != null)
            {
                player = p; break;
            }
        }
        target = player;
    }

    void LateUpdate()
    {
        Vector2 newPos = transform.position;
        newPos.x = Mathf.Clamp(newPos.x, 0, 199);
        newPos.y = Mathf.Clamp(newPos.y, 0, 199);
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
    }

    void Update()
    {
        // 非攻击态持续右移并朝向玩家
        if (current_state != EnemyState.Attack)
        {
            // 默认稳步右移
            transform.position += Vector3.right * 0.1f * Time.deltaTime;

            // 朝向玩家：左侧设为 -1，右侧为 +1
            float sign = player.transform.position.x < transform.position.x ? -1f : 1f;
            transform.localScale = new Vector3(originalScale.x * sign, originalScale.y, originalScale.z);
        }

        if (!dying)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (g_timer.GetCurrentTime() - behaviour_time >= behaviour_gap && current_state == EnemyState.Wait)
            {
                SetState(EnemyState.Moving);
            }
            else if (distance <= 10f && g_timer.GetCurrentTime() - behaviour_time > behaviour_gap && current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Attack);
            }
            else if (distance <= 10f && current_state != EnemyState.Attack)
            {
                navigation.SetNavActive(false);
                SetState(EnemyState.Wait);
            }

            if (current_state == EnemyState.Moving && !navigation.is_activing)
                navigation.SetNavActive(true);
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
        if (current_state != EnemyState.Attack)
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
    }

    public override void SetTarget(GameObject tar) => target = tar;

    public override bool TakeDamage(Vector3 source, float amount, bool Instant_kill)
    {
        if (!could_hurt) return false;
        current_health -= amount;
        animator.SetTrigger("Hurt");
        Vector2 backDir = -((Vector2)source - (Vector2)transform.position).normalized;
        RB.velocity = backDir * 2f;
        navigation.SetNavActive(false);
        return true;
    }

    public void OnAttackDashEvent() => StartCoroutine(AttackDash());

    IEnumerator AttackDash()
    {
        float dashDuration = 2f;
        float dashSpeed = speed * 2f;
        Vector2 dir = (target.transform.position - transform.position).normalized;
        float start = g_timer.GetCurrentTime();

        navigation.SetNavActive(false);
        ActiveAttackEffect();

        // 根据方向设置旋转：0° 向右，180° 向左
        float angle = dir.x < 0 ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        while (g_timer.GetCurrentTime() - start <= dashDuration)
        {
            transform.Translate(dir * dashSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        DeactiveAttackEffect();
        // 重置旋转
        transform.rotation = Quaternion.identity;
        OnAttackEnd();
    }

    public void OnAttackEnd()
    {
        behaviour_time = g_timer.GetCurrentTime();
        SetState(EnemyState.Wait);
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
                break;
            case EnemyState.Moving:
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
                animator.SetTrigger("Attacking");
                break;
        }
    }

    public void ActiveAttackEffect() => attacking_effect.Play();
    public void DeactiveAttackEffect() => attacking_effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

    public void DestroyEnemy()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
            Globals.Datas.EnemyPool.Remove(this);
        Destroy(gameObject);
    }
}
