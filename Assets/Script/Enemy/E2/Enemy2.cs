using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Enemy2 : Enemy
{
    // —— 内部状态机 —— 
    private enum State { Idle, Moving, Attack, Block }
    private State _state;
    GameObject resource_prefab;
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
    static string block_prefab_path = "Prefabs/BlockEffect";
    GameObject block_effect;
    //—— 内部数据 —— 
    private float lastActionTime;
    private float _currentHealth;
    string water_drop_step = "Prefabs/WaterDrops";
    GameObject water_drop_step_prefab;
    //音频
    [SerializeField] AudioClip move;
    [SerializeField] AudioClip watermove1;
    [SerializeField] AudioClip watermove2;
    [SerializeField] AudioClip In_Block;
    [SerializeField] AudioClip Block;
    //—— 基类抽象属性实现 —— 
    public override float max_health { get; set; }
    public override float current_health 
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }
    public override float speed { get; set; }
    public override GameObject target { get; set; }
    void PlayMovingSound()
    {
        if (AudioSysManager.Instance == null) return;


        // 2) 检测脚下是否是水
        if (ChunkGenerator.Instance != null &&
            ChunkGenerator.Instance.IsTileOfType(transform.position, ChunkGenerator.Instance.waterTile))
        {
            // 4) 播放水上移动音效
            Instantiate(water_drop_step_prefab,transform.position,Quaternion.identity,transform);
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
    public void AttackSound()
    {
        PlayAttackSound(gameObject, transform.position);
    }
    private RectTransform fillRT;
    private Canvas   healthBarCanvas;
    private Image    healthBarFill;
    private void CreateHealthBar()
    {
        // 1) Canvas
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = Vector3.up * 2f; // 根据模型高度调节
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvas.sortingOrder   =  100;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2) 背景
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = Color.gray;
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(2f, 0.025f);

        // 3) 填充条
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fill = fillGO.AddComponent<Image>();
        fill.color = Color.green;
        // 不用 Filled 类型了，改用 scale
        fill.type = Image.Type.Simple;

        // 拿到 RectTransform 并设置 pivot.x = 0
        fillRT = fill.GetComponent<RectTransform>();
        // 让它和背景同尺寸、左对齐
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 5f);
        fillRT.pivot     = new Vector2(0f, 0.5f);
        fillRT.sizeDelta = new Vector2(bgRT.sizeDelta.x, bgRT.sizeDelta.y);

        // 缓存引用
        healthBarCanvas = canvas;
        healthBarFill   = fill;

        // 默认满血时隐藏
        canvasGO.SetActive(false);
    }
    private void UpdateHealthBar()
    {
        if (healthBarCanvas == null) return;
        float t = Mathf.Clamp01(current_health / max_health);

        // 只改 X 方向 scale（从左侧伸缩）
        fillRT.localScale = new Vector3(t, 1f, 1f);

        healthBarCanvas.gameObject.SetActive(t < 1f);
    }
    void Start()
    {
        // 缓存组件
        CreateHealthBar();
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();
        nav      = GetComponent<EnemyNav>();
        animator = GetComponent<Animator>();
        rb2d     = GetComponent<Rigidbody2D>();
        block_effect = Addressables.LoadAssetAsync<GameObject>(block_prefab_path).WaitForCompletion();
        water_drop_step_prefab = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
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
        rb2d.velocity = Vector2.Lerp(rb2d.velocity, Vector2.zero, 2f * Time.deltaTime);
        if (rb2d.velocity.magnitude > 10f)
        {
            rb2d.velocity = rb2d.velocity.normalized * 10f;
        }

        if (current_health <= 0)
        {
            nav.SetNavActive(false);
            animator.SetBool("Dead",true);
        }
        UpdateHealthBar();
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
        const float minBlockInterval = 0.15f;
        // —— 新增 —— 如果上次动作距今还不到 minBlockInterval，就免伤
        if (Time.time - lastActionTime < minBlockInterval)
        {
            // 同时延长一下免伤冷却，防止紧接着又被打进来
            blockImmunityEndTime = Time.time + minBlockInterval;
            return false;
        }

        // 1) 如果还在 block 免伤冷却内，直接免伤
        if (Time.time < blockImmunityEndTime)
            return false;

        // 2) 正在 Block 状态时：触发一次免伤，设置后续 1 秒免伤
        if (_state == State.Block)
        {
            blockImmunityEndTime = Time.time + 1f;
            AudioSysManager.Instance.PlaySound(gameObject,Block,transform.position,2,true);
            // 镜头抖动
            CameraEffects.Instance.Shake(
                shakeDuration: 0.25f,
                shakeAmplitude: 0.9f,
                shakeFrequency: 2.5f,
                zoomAmount: 0.7f,
                0.3f
            );
            Instantiate(block_effect,transform.position+new Vector3(0,1.5f,-0.1f),Quaternion.identity);
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
        if (instantKill) _currentHealth = 1f;
        else             _currentHealth -= amount;

        // 4) 血量归零：播放死亡动画
        if (_currentHealth <= 0f && ! animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
        {
            nav.SetNavActive(false);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        else
        {

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
                rb2d.velocity = Vector2.Lerp(rb2d.velocity , Vector2.zero , 0.2f);
                break;

            case State.Moving:
                rb2d.velocity = Vector2.Lerp(rb2d.velocity , Vector2.zero , 0.2f);
                nav.SetNavActive(true);
                nav.SetTarget(target);
                animator.SetBool("Moving", true);
                break;

            case State.Attack:
                animator.SetTrigger("Attack");
                break;

            case State.Block:
                animator.SetTrigger("Block");
                AudioSysManager.Instance.PlaySound(gameObject,In_Block,transform.position,2,true);
                break;
        }
    }
    public void DestroyEnemy()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            if (resource_prefab == null)
                return;
            for (int i = 0; i < (UnityEngine.Random.value < 0.5 ? 2 : 4) ; i++)
            {
                var instance = Instantiate(resource_prefab, transform.position, Quaternion.identity);
                var rb = instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 10f, ForceMode2D.Impulse);
            }
            Globals.Datas.EnemyPool.Remove(this);
            GlobalEventBus.OnEnemyDead.Invoke();
        }
        Destroy(gameObject);
    }
}
