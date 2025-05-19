using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


public class Enemy3 : Enemy
{
    // —— 实现基类抽象属性 —— 
    public override float max_health { get; set; }
    public override float current_health
    {
        get => currentHealth;
        set => currentHealth = value;
    }
    public override float speed { get; set; }
    public override GameObject target 
    {
        get => currentTarget;
        set => currentTarget = value;
    }

    // —— 引用组件 —— 
    private EnemyNav    nav;
    private Rigidbody2D rb;
    private Animator    animator;

    // —— 音效 & 特效 —— 
    [SerializeField] AudioClip move;
    [SerializeField] AudioClip watermove1;
    [SerializeField] AudioClip watermove2;
    string water_drop_step = "Prefabs/WaterDrops";
    GameObject water_drop_step_prefab;

    // —— 状态机 —— 
    private enum State { Idle, Moving, Attack, Dead }
    private State _state = State.Idle;

    // —— 目标系统 —— 
    private GameObject player;
    private GameObject currentTarget;

    // —— 子弹 —— 
    string bullet_prefab_path = "Prefabs/E3B";
    GameObject prefabBullet;
    private List<Enemy3Bullet> activeBullets = new List<Enemy3Bullet>();

    // —— 连发 & 冷却 —— 
    private int   attackCounter = 0;
    private const int maxAttacks = 2;
    [SerializeField] private float actionInterval       = 2f;  // 每2秒决策一次
    [SerializeField] private float targetSwitchInterval = 5f;  // 每5秒切一次目标
    private float lastActionTime;
    private float lastTargetSwitchTime;

    // —— 距离 & 速度参数 —— 
    [SerializeField] private float minMoveDist = 6f;
    [SerializeField] private float maxMoveDist = 8f;
    [SerializeField] private float bulletRange = 12f;
    // `speed` field is now backing the `speed` property
    [SerializeField] private float speedField   = 3.5f;

    // —— 生命与受击 —— 
    [SerializeField] private float maxHealth      = 3f;
    private        float currentHealth;
    [SerializeField] private float knockbackForce = 5f;
    GameObject resource_prefab;
    // —— 朝向翻转 —— 
    private Vector3 originalScale;
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
        // 把 Inspector 里的 maxHealth/speedField 赋给抽象属性
        CreateHealthBar();
        max_health = maxHealth;
        speed      = speedField;
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.5f ? "Prefabs/BlackBlock" :  "Prefabs/GreenBlock" ).WaitForCompletion();

        // 缓存组件
        prefabBullet = Addressables.LoadAssetAsync<GameObject>(bullet_prefab_path).WaitForCompletion();
        nav          = GetComponent<EnemyNav>();
        rb           = GetComponent<Rigidbody2D>();
        animator     = GetComponent<Animator>();
        originalScale = transform.localScale;

        // 加载脚下特效
        water_drop_step_prefab = Addressables
            .LoadAssetAsync<GameObject>(water_drop_step)
            .WaitForCompletion();

        // 缓存玩家，初始目标
        player = GameObject.FindGameObjectWithTag("Player");
        currentTarget = player;
        target = player;  // 也同步给基类接口

        // 初始化数据
        current_health        = max_health;
        lastActionTime       = Time.time;
        lastTargetSwitchTime = Time.time;
        EnterState(State.Idle);
    }
    public void AttackSound()
    {
        PlayAttackSound(gameObject, transform.position);
    }
    void Update()
    {
        if (_state == State.Dead) return;
        // —— 新增：如果目标丢失，立刻换目标 —— 
        if (currentTarget == null)
        {
            ChooseTarget();
            lastTargetSwitchTime = Time.time;
    }
        // 每5秒切一次 target
        if (Time.time - lastTargetSwitchTime >= targetSwitchInterval)
        {
            ChooseTarget();
            lastTargetSwitchTime = Time.time;
        }

        // 按状态驱动
        switch (_state)
        {
            case State.Idle:
                rb.velocity = Vector2.Lerp(rb.velocity , Vector2.zero , 0.2f);
                if (Time.time - lastActionTime >= actionInterval)
                    DecideNextAction();
                break;
            case State.Moving:
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 2f * Time.deltaTime);
                MoveUpdate();
                break;
            case State.Attack:
                // 由动画事件中的 FireBullet() 控制
                break;
        }

        if (activeBullets.Count > 0)
        {
            activeBullets.RemoveAll(bullet => bullet == null);
        }
        FaceTarget();
        UpdateHealthBar();
    }

    private void MoveUpdate()
    {
        float dist = Vector2.Distance(transform.position, currentTarget.transform.position);

        // 如果在射程内且行动冷却到，切攻击
        if (dist <= bulletRange && Time.time - lastActionTime >= actionInterval)
        {
            EnterState(State.Attack);
            return;
        }

        // 距离判断
        if (dist < minMoveDist)
        {
            nav.SetNavActive(false);
            Vector2 dir = (transform.position - currentTarget.transform.position).normalized;
            rb.velocity = dir * speed;
            PlayMovingSound();
        }
        else if (dist > maxMoveDist)
        {
            animator.SetBool("Moving", true);
            nav.SetTarget(currentTarget);
            nav.SetNavActive(true);
            PlayMovingSound();
        }
        else
        {
            EnterState(State.Idle);
        }
    }

    private void DecideNextAction()
    {
        lastActionTime = Time.time;
        float dist = Vector2.Distance(transform.position, currentTarget.transform.position);
        EnterState(dist <= bulletRange ? State.Attack : State.Moving);
    }

    private void ChooseTarget()
    {
        float playerDist = Vector2.Distance(transform.position, player.transform.position);
        float bestBuildDist = float.MaxValue;
        GameObject bestBuild = null;

        foreach (var kv in PlayerBuildingManager.current_buildings)
        {
            var bComp = kv.Value;
            if (bComp == null) continue;
            float d = Vector2.Distance(transform.position, bComp.transform.position);
            if (d < bestBuildDist)
            {
                bestBuildDist = d;
                bestBuild = bComp.gameObject;
            }
        }

        currentTarget = (bestBuild != null && bestBuildDist < playerDist)
            ? bestBuild 
            : player;
        target = currentTarget;  // 同步给基类
    }

    private void EnterState(State s)
    {
        animator.ResetTrigger("Hurt");
        animator.SetBool("Moving", false);
        animator.SetBool("Attack", false);
        nav.SetNavActive(false);
        rb.velocity = Vector2.zero;

        _state = s;
        switch (s)
        {
            case State.Idle:   animator.Play("Idle");    break;
            case State.Moving: animator.SetBool("Moving", true); break;
            case State.Attack: animator.SetBool("Attack", true); break;
            case State.Dead:   
                animator.SetBool("Dead", true);
                nav.SetNavActive(false);
                break;
        }
    }

    public void FireBullet()
    {
        GameObject go = Instantiate(prefabBullet, transform.position, Quaternion.identity);
        var bullet = go.GetComponent<Enemy3Bullet>();
        Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
        bullet.Initialize(this, dir, 0f, 8f, 10f, 2.5f);
        activeBullets.Add(bullet);

        attackCounter++;
        if (attackCounter >= maxAttacks)
        {
            attackCounter = 0;
            lastActionTime       = Time.time;
            lastTargetSwitchTime = Time.time;
            EnterState(State.Idle);
        }
    }
    private void FaceTarget()
    {
        if (currentTarget == null) return;
        float sign = (currentTarget.transform.position.x >= transform.position.x) ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * sign,
                                           originalScale.y,
                                           originalScale.z);
    }

    public override void SetTarget(GameObject tar)
    {
        currentTarget = tar;
    }

    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        if (_state == State.Dead) return false;
        current_health = instantKill ? 0f : current_health - amount;

        animator.SetTrigger("Hurt");
        Vector2 knock = ((Vector2)transform.position - (Vector2)source).normalized;
        rb.AddForce(knock * knockbackForce, ForceMode2D.Impulse);

        if (current_health <= 0f)
            EnterState(State.Dead);

        return true;
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
        Destroy(this.gameObject);
    }
    private void PlayMovingSound()
    {
        if (AudioSysManager.Instance == null) return;
        Vector3 pos = transform.position;
        if (ChunkGenerator.Instance != null &&
            ChunkGenerator.Instance.IsTileOfType(pos, ChunkGenerator.Instance.waterTile))
        {
            Instantiate(water_drop_step_prefab, pos, Quaternion.identity, transform);
            AudioSysManager.Instance.PlaySound(
                gameObject,
                Random.value < 0.5f ? watermove1 : watermove2,
                pos, 0.8f, false
            );
        }
        else
        {
            AudioSysManager.Instance.PlaySound(
                gameObject, move, pos, 0.6f, true
            );
        }
    }
}
