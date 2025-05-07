using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(EnemyNav))]
public class Enemy0 : Enemy
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

    public override float max_health { get; set;} = 3f;
    public override float current_health { get; set;}
    public override float speed { get; set;} = 3.5f;
    bool could_hurt;
    public override GameObject target { get; set;}
    EnemyNav navigation;
    GlobalTimer g_timer;
    GameObject player;
    Animator animator;
    Rigidbody2D RB;
    SpriteRenderer sprite_renderer;
    float behaviour_time;
    float behaviour_gap = 2f;
    bool dying;
    EnemyNav self_nav;
    Dictionary<Vector2 , GameObject> player_objects;
    //音频
    [SerializeField] AudioClip move;
    [SerializeField] AudioClip watermove1;
    [SerializeField] AudioClip watermove2;
    static GameObject hitted_prefab;
    void Start()
    {
        current_health = max_health;
        self_nav = transform.GetComponent<EnemyNav>();
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.GetComponent<PlayerController>() != null)
            {
                player = p;
            }
        }
        hitted_prefab = Addressables.LoadAssetAsync<GameObject>(hitted_prefab_path).WaitForCompletion();

        // 使用内部枚举来初始化
        current_state = EnemyState.Wait;

        could_hurt = true;
        g_timer = GlobalTimer.Instance;
        navigation = transform.GetComponent<EnemyNav>();
        RB = transform.GetComponent<Rigidbody2D>();
        animator = transform.GetComponent<Animator>();
        behaviour_time = g_timer.GetCurrentTime();
        sprite_renderer = transform.GetComponent<SpriteRenderer>();
        GameObject player_obj = GameObject.FindGameObjectWithTag("Player");
        if (player_obj != null)
        {
            if (player_obj.GetComponent<PlayerController>()!=null)
            {
                player = player_obj;
            }
        }
        target = player;
    }
    void Update()
    {
        if (!dying)
        {
            // 等待状态到移动状态的转换
            if (g_timer.GetCurrentTime() - behaviour_time >= behaviour_gap && current_state == EnemyState.Wait)
            {
                SetState(EnemyState.Moving);
            }

            // 如果在攻击状态并且动画播放到“Attack”，则切换回等待状态
            if (current_state == EnemyState.Attack && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
            {
                SetState(EnemyState.Wait);
            }

            // 检测目标距离，如果足够近且行为间隔到达，则切换为攻击状态
            if (Vector2.Distance(transform.position, target.transform.position) < 2f &&
                (g_timer.GetCurrentTime() - behaviour_time) > behaviour_gap)
            {
                behaviour_time = g_timer.GetCurrentTime();
                SetState(EnemyState.Attack);
            }
            
            if (current_state == EnemyState.Moving || current_state == EnemyState.Wait)
            {
                if (target.transform.position.x - transform.position.x < 0)
                {
                    sprite_renderer.flipX = true;
                }
                else
                {
                    sprite_renderer.flipX = false;
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
    void PlayMovingSound()
    {
        if (AudioSysManager.Instance == null) return;


        // 2) 检测脚下是否是水
        if (ChunkGenerator.Instance != null &&
            ChunkGenerator.Instance.IsTileOfType(transform.position, ChunkGenerator.Instance.waterTile))
        {
            // 4) 播放水上移动音效
            AudioSysManager.Instance
                .PlaySound(gameObject,
                        UnityEngine.Random.value < 0.5f ? watermove1 : watermove2,
                        transform.position,
                        0.8f,
                        false);
        }
        else
        {
            AudioSource moveSrc = AudioSysManager.Instance
                .PlaySound(gameObject, move , transform.position, 0.6f, true);
        }
    }
    void LateUpdate()
    {
        Vector2 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(newPosition.x, 0, 199);
        newPosition.y = Mathf.Clamp(newPosition.y, 0, 199);
        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
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
        if (!could_hurt) 
            return false;
        // 1. 只取 X/Y 构建方向向量（忽略 Z）
        Vector2 dir = new Vector2(
            transform.position.x - source.x,
            transform.position.y - source.y
        ).normalized;

        // 2. 计算相对于世界 X 轴的角度（度），再减去 90°
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        // 3. 只在 Z 轴上旋转，X/Y 轴保持 0
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // 4. 在本体位置（含本体的 Z）生成特效，并赋予上面算好的旋转
        Instantiate(hitted_prefab, transform.position, rot);

        // 剩下就是普通受击逻辑
        current_health -= amount;
        animator.SetTrigger("Hit");
        RB.velocity = -dir * 2f;
        navigation.SetNavActive(false);
        return true;
    }

    public void AttackDash()
    {
        Vector2 direction = (target.transform.position - transform.position).normalized;
        if (RB != null)
        {
            RB.velocity = direction * 5f;
        }
    }
    private void SetState(EnemyState newState)
    {
        // 如果状态没有变化，直接返回
        if (current_state == newState) return;

        // 重置所有状态对应的参数
        animator.SetBool("Moving", false);
        animator.SetBool("Attack", false);
        navigation.SetNavActive(false);

        // 切换状态并设置相应参数
        current_state = newState;
        switch (newState)
        {
            case EnemyState.Wait:
                navigation.SetNavActive(false);
                break;
            case EnemyState.Moving:
                // 设置为移动状态，设置目标，并激活导航
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
                // 设置为攻击状态
                animator.SetBool("Attack", true);
                navigation.SetNavActive(false);
                break;
        }
    }

    public void DestroyEnemy()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            Globals.Datas.EnemyPool.Remove(this);
            Destroy(this.gameObject);
        }
    }
}
