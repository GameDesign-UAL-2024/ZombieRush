using UnityEngine;

public class Enemy2 : Enemy
{
    // —— 内部状态机 —— 
    private enum State { Idle, Moving, Attack, Block }
    private State _state;

    [Header("Stats")]
    [SerializeField] private float dashForce = 10f;              
    [SerializeField] private float knockbackForce = 5f;          
    [SerializeField] private float blockKnockbackForce = 6f;     
    [SerializeField] private float actionCooldown = 2f;          
    private Vector3 _originalScale;
    private float blockImmunityEndTime = 0f;  
    //—— 缓存组件 —— 
    private EnemyNav nav;
    private Animator animator;
    private Rigidbody2D rb2d;
    private Transform player;

    //—— 内部数据 —— 
    private float lastActionTime;
    private float _currentHealth;

    //—— 基类抽象属性实现 —— 
    public override float max_health { get; set; }
    public override float current_health 
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }
    public override float speed { get; set; }
    public override GameObject target { get; set; }

    void Start()
    {
        // 缓存组件
        nav      = GetComponent<EnemyNav>();
        animator = GetComponent<Animator>();
        rb2d     = GetComponent<Rigidbody2D>();

        // 找到玩家对象
        target = GameObject.FindGameObjectWithTag("Player");
        if (target != null) player = target.transform;

        // 初始化数值
        max_health     = 10f;
        _currentHealth = max_health;
        speed          = 3f;
        _originalScale = transform.localScale;
        // 进入初始内部状态 Idle
        EnterState(State.Idle);
    }
    public void FacePlayer()
    {
        if (player == null) return;

        var scale = transform.localScale;
        scale.x = player.position.x >= transform.position.x
            ?  Mathf.Abs(_originalScale.x)
            : -Mathf.Abs(_originalScale.x);
        transform.localScale = scale;
    }
    void Update()
    {
        // —— 非 Attack 状态下，实时跟随玩家翻转朝向 —— 
        if (_state != State.Attack)
            FacePlayer();
        switch (_state)
        {
            case State.Moving:
                // 寻路中：看距离决定是否切换近战
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= 5f)
                {
                    nav.SetNavActive(false);
                    animator.SetBool("Moving", false);
                    EnterState(Random.value < 0.5f ? State.Attack : State.Block);
                }
                break;

            case State.Idle:
                // 待机，等冷却
                if (Time.time - lastActionTime >= actionCooldown)
                    DecideNext();
                break;

            case State.Attack:
            case State.Block:
                // 动画播完且不在任何过渡中，就回 Idle
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.normalizedTime >= 1f && !animator.IsInTransition(0))
                    EnterState(State.Idle);
                break;
        }
        rb2d.velocity = Vector2.Lerp(rb2d.velocity, Vector2.zero, 1f * Time.deltaTime);
        if (rb2d.velocity.magnitude > 10f)
        {
            rb2d.velocity = rb2d.velocity.normalized * 10f;
        }

        if (current_health <= 0)
        {
            nav.SetNavActive(false);
            animator.SetBool("Dead",true);
        }
    }

    public override void SetTarget(GameObject t)
    {
        target = t;
        nav.SetTarget(t);
    }

    /// <summary>
    /// 受击接口：返回 true 表示扣血，false 表示免疫（Block 或 1秒内免疫）
    /// </summary>
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        // 1) 如果还在 block 免伤冷却内，直接免伤
        if (Time.time < blockImmunityEndTime)
            return false;

        // 2) 正在 Block 状态时：触发一次免伤，设置后续 1 秒免伤
        if (_state == State.Block)
        {
            blockImmunityEndTime = Time.time + 1f;

            // 镜头抖动
            CameraEffects.Instance.Shake(
                shakeDuration: 0.25f,
                shakeAmplitude: 0.9f,
                shakeFrequency: 2.5f,
                zoomAmount: 0.7f
            );

            var prb = target.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                // 计算从玩家指向敌人的方向
                Vector2 dirToEnemy = ((Vector2)transform.position - (Vector2)player.position).normalized;
                prb.AddForce(dirToEnemy * blockKnockbackForce, ForceMode2D.Impulse);
            }

            // 立即切换到攻击
            EnterState(State.Attack);
            return false;
        }

        // 3) 普通受击——扣血或秒杀
        if (instantKill) _currentHealth = 0f;
        else             _currentHealth -= amount;

        // 4) 血量归零：播放死亡动画
        if (_currentHealth <= 0f)
        {
            nav.SetNavActive(false);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        else
        {
            // 5) 活着：击退自己 + 受击动画
            Vector2 knockDir = ((Vector2)transform.position - (Vector2)source).normalized;
            rb2d.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

            if (_state != State.Attack)
                animator.SetTrigger("Hurt");
        }

        // 6) 所有执行到这里的分支都算“伤害生效”
        return true;
    }
    void LateUpdate()
    {
        Vector2 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(newPosition.x, 0, 199);
        newPosition.y = Mathf.Clamp(newPosition.y, 0, 199);
        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
    }
    // Animation Event 调用：冲刺到玩家
    public void DashToPlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb2d.velocity = Vector2.zero;
        rb2d.AddForce(dir * dashForce, ForceMode2D.Impulse);
    }

    //—— 内部状态机方法 —— 

    private void DecideNext()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > 2f) EnterState(State.Moving);
        else           EnterState(Random.value < 0.5f ? State.Attack : State.Block);
    }

    private void EnterState(State next)
    {
        _state = next;
        lastActionTime = Time.time;

        // 重置所有触发器和移动标记
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Block");
        animator.SetBool("Moving", false);
        nav.SetNavActive(false);

        // 激活对应状态
        switch (next)
        {
            case State.Idle:
                // 待机：无需额外动作
                break;

            case State.Moving:
                nav.SetNavActive(true);
                nav.SetTarget(target);
                animator.SetBool("Moving", true);
                break;

            case State.Attack:
                animator.SetTrigger("Attack");
                break;

            case State.Block:
                animator.SetTrigger("Block");
                break;
        }
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
