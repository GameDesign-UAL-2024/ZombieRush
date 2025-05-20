using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Enemy5 : Enemy
{
    public override float max_health {get; set;} = 128f;
    public override float speed {get; set;} = 4f;
    public override float current_health { get; set;}
    public override GameObject target{get; set;}
    string sword_path = "Prefabs/FlySword";
    GameObject sword_pref;
    [SerializeField] AudioClip watermove;
    string bullet_prefab_path = "Prefabs/E3B";
    string water_drop_step = "Prefabs/WaterDrops";
    GameObject bullet_pref;
    GameObject water_pref;
    Rigidbody2D rb2d;
    private List<Enemy3Bullet> activeBullets = new List<Enemy3Bullet>();
    Animator animator;
    float behaviour_time = 3f;
    private float idleTimer = 0f;
    enum states {IDLE,ATK1,ATK2,ATK3,MOVE_FAR,MOVE_CLOSE};
    states current_state;

    private RectTransform fillRT;
    private Canvas   healthBarCanvas;
    private Image    healthBarFill;
    GameObject resource_prefab;
        
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
        bgRT.sizeDelta = new Vector2(5f, 0.05f);

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
    void Awake()
    {
        current_health = max_health;
    }
    void Start()
    {
        CreateHealthBar();
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();
        target = GameObject.FindGameObjectWithTag("Player");
        current_state = states.IDLE;
        animator = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        bullet_pref = Addressables.LoadAssetAsync<GameObject>(bullet_prefab_path).WaitForCompletion();
        water_pref = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
        sword_pref = Addressables.LoadAssetAsync<GameObject>(sword_path).WaitForCompletion();
    }
    void Update()
    {
        if (Globals.Instance.Event.current_state == Globals.Events.GameState.pausing){ return; }
        // —— 翻转面朝 ——  
        if ((current_state == states.IDLE
            || current_state == states.MOVE_CLOSE
            || current_state == states.MOVE_FAR)
            && target != null)
        {
            Vector3 scl = transform.localScale;
            scl.x = (target.transform.position.x < transform.position.x) ? -1f : 1f;
            transform.localScale = scl;
        }
        UpdateHealthBar();
        if (rangeGO != null)
            rangeGO.transform.position = transform.position;
        // always apply a little drag
        rb2d.velocity = Vector2.Lerp(rb2d.velocity, Vector2.zero, 1f * Time.deltaTime);

        switch (current_state)
        {
            case states.IDLE:
                idleTimer += Time.deltaTime;
                if (idleTimer >= behaviour_time)
                {
                    idleTimer = 0f;

                    // 1) decide close vs far
                    bool doClose = Random.value < 0.5f;

                    // 2) decide attack (1,2,3) but if far only 1 or 2
                    int atkChoice;
                    if (doClose)
                        atkChoice = Random.Range(1, 4);   // 1,2 or 3
                    else
                        atkChoice = Random.Range(1, 3);   // 1 or 2

                    if (atkChoice == 3)
                        CreateRangeIndicator();
                    // 3) set animator flags
                    animator.SetBool("Moving", true);
                    animator.SetBool("ATK1", atkChoice == 1);
                    animator.SetBool("ATK2", atkChoice == 2);
                    animator.SetBool("ATK3", atkChoice == 3);

                    // 4) trigger movement state & call your move logic
                    if (doClose)
                    {
                        current_state = states.MOVE_CLOSE;
                        MoveClose();
                    }
                    else
                    {
                        current_state = states.MOVE_FAR;
                        MoveFar();
                    }
                }
                break;
            default:
                // nothing—waiting for animation events to reset to IDLE
                break;
        }

    }

    public void OnIdle()
    {
        animator.SetBool("Moving", false);
        animator.SetBool("ATK1", false);
        animator.SetBool("ATK2", false);
        animator.SetBool("ATK3", false);
        current_state = states.IDLE;
        idleTimer = 0f;        
    }
    private IEnumerator SpawnSwordsAround()
    {
        // 随机决定生成数量：2 到 4
        int count = Random.Range(2, 5);

        for (int i = 0; i < count; i++)
        {
            // 在半径为 4 的圆内选一个随机点
            Vector2 offset = Random.insideUnitCircle * 4f;
            Vector3 spawnPos = (Vector2)transform.position + offset;

            // 随机朝向（可选）
            Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // 实例化
            Instantiate(sword_pref, spawnPos, rot);

            yield return new WaitForSeconds(0.2f);
        }
    }
    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        current_health -= amount;
        if (current_health <= max_health / 2)
        {
            behaviour_time = 1f;
        }
        if (current_health <= 0f && ! animator.GetBool("Dead"))
        {
            animator.SetBool("Dead",true);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        return true;
    }
    public void IDLE()
    {
        if (rangeGO != null)
        {
            Destroy(rangeGO);
            rangeGO = null;
        } 
    }

    // 通用协程：在 duration 秒内把 transform 平滑移动到 targetPos
    private IEnumerator MoveToPosition(Vector2 targetPos, float duration)
    {
        Vector2 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        // 确保最后一帧精确到位
        transform.position = targetPos;
    }

    // 贴近目标：0.5 秒内移动到距离目标 1.5f 处
    void MoveClose()
    {
        if (target == null) return;
        if (current_health <= max_health * 0.4f)
        {
            StartCoroutine(Globals.Instance.LoadAndSpawnEnemyWave("Prefabs/Enemy4", 2, transform.position));
        }
        StartCoroutine(SpawnSwordsAround());
        Vector2 toTarget    = (Vector2)target.transform.position - (Vector2)transform.position;
        Vector2 dir         = toTarget.normalized;
        Vector2 desiredPos  = (Vector2)target.transform.position - dir * 4f;

        StartCoroutine(MoveToPosition(desiredPos, 0.5f));
    }

    // 远离目标：0.5 秒内移动到距离目标 7～10f 处
    void MoveFar()
    {
        if (current_health <= max_health * 0.4f)
        {
            StartCoroutine(Globals.Instance.LoadAndSpawnEnemyWave("Prefabs/Enemy4", 2, transform.position));
        }
        if (target == null) return;
        StartCoroutine(SpawnSwordsAround());
        Vector2 awayDir     = ((Vector2)transform.position - (Vector2)target.transform.position).normalized;
        float farDistance   = Random.Range(7f, 10f);
        Vector2 desiredPos  = (Vector2)target.transform.position + awayDir * farDistance;

        StartCoroutine(MoveToPosition(desiredPos, 0.5f));
    }




    public void ATK1()
    {
        // 由动画事件或外部调用时触发协程
        StartCoroutine(ATK1Routine());
    }

    private IEnumerator ATK1Routine()
    {
        if (target == null)
            yield break;

        // 基础方向：指向目标
        Vector2 baseDir = (target.transform.position - transform.position).normalized;

        // —— 第 1 发 (-30°) ——
        {
            GameObject go1 = Instantiate(bullet_pref, transform.position, Quaternion.identity);
            Enemy3Bullet b1 = go1.GetComponent<Enemy3Bullet>();
            Vector2 dir1 = Quaternion.Euler(0, 0, -30f) * baseDir;
            b1.transform.localScale = new Vector3(2,1,1);
            b1.Initialize(this, dir1, 0f, 8f, 10f, 2.5f);
            activeBullets.Add(b1);
        }
        yield return new WaitForSeconds(0.4f);

        // —— 第 2 发 (0°) ——（中间那颗，go2）
        {
            GameObject go2 = Instantiate(bullet_pref, transform.position, Quaternion.identity);
            Enemy3Bullet b2 = go2.GetComponent<Enemy3Bullet>();
            Vector2 dir2 = baseDir;
            b2.transform.localScale = new Vector3(2,1,1);
            b2.Initialize(this, dir2, 0f, 8f, 10f, 2.5f);
            activeBullets.Add(b2);
        }
        yield return new WaitForSeconds(0.4f);

        // —— 第 3 发 (+30°) ——
        {
            GameObject go3 = Instantiate(bullet_pref, transform.position, Quaternion.identity);
            Enemy3Bullet b3 = go3.GetComponent<Enemy3Bullet>();
            Vector2 dir3 = Quaternion.Euler(0, 0, 30f) * baseDir;
            b3.transform.localScale = new Vector3(2,1,1);
            b3.Initialize(this, dir3, 0f, 8f, 10f, 2.5f);
            activeBullets.Add(b3);
        }
    }

    public void ATK2()
    {
        Vector2 dir = (target.transform.position - transform.position).normalized;
        rb2d.velocity = Vector2.zero;
        rb2d.AddForce(dir * 10f, ForceMode2D.Impulse);
    }

    public void ATK3_2()
    {
        Vector2 dir = (target.transform.position - transform.position).normalized;
        rb2d.velocity = Vector2.zero;
        rb2d.AddForce(dir * 15f, ForceMode2D.Impulse);

        StartCoroutine(ATK1Routine());
    }


    public void ATK3_1()
    {
        const float radius = 5f;
        const float damageAmount = 5f;
     
        // 在 2D 物理里扫圆，拿到所有碰撞
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var c in hits)
        {
            // 1) 玩家
            if (c.CompareTag("Player"))
            {
                var pc = c.GetComponent<PlayerController>();
                if (pc != null)
                    pc.ReduceLife(damageAmount);
            }
            // 2) 建筑
            else if (c.CompareTag("PlayerObjects"))
            {
                var b = c.GetComponent<Buildings>();
                if (b != null)
                    b.TakeDamage(damageAmount);
            }
        }
    }
    // 放在类体里
    // range indicator
    private GameObject rangeGO;
    private LineRenderer rangeRenderer;
    [SerializeField] private int circleSegments = 60;
    [SerializeField] private float rangeWidth   = 1f;
    [SerializeField] private Color rangeColor   = Color.red;
    private const float explosionRange = 5f;
    // 在 Start() 或你想开始显示范围的时机调用
    void CreateRangeIndicator()
    {
        rangeGO = new GameObject("Enemy4_RangeIndicator");
        // <-- Make sure it is at root (no parent), or under some 'FX' container with (1,1,1) scale
        rangeGO.transform.SetParent(null);

        rangeRenderer = rangeGO.AddComponent<LineRenderer>();
        rangeRenderer.useWorldSpace   = false;        // ← now positions are LOCAL to rangeGO
        rangeRenderer.loop            = true;
        rangeRenderer.positionCount   = circleSegments + 1;
        rangeRenderer.widthMultiplier = rangeWidth;
        rangeRenderer.material        = new Material(Shader.Find("Sprites/Default"));
        rangeRenderer.startColor      = rangeColor;
        rangeRenderer.endColor        = rangeColor;

        // build a unit circle in local space
        Vector3[] pts = new Vector3[circleSegments + 1];
        for (int i = 0; i <= circleSegments; i++)
        {
            float ang = 2f * Mathf.PI * i / circleSegments;
            pts[i] = new Vector3(
                Mathf.Cos(ang) * explosionRange,
                Mathf.Sin(ang) * explosionRange,
                0f
            );
        }
        rangeRenderer.SetPositions(pts);
    }
    public void AttackSound ()
    {
        PlayAttackSound(gameObject,transform.position);
    }
    // if you want to hook actual removal from pool on death animation event:
    public void OnDeath()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            if (resource_prefab == null)
                return;
            for (int i = 0; i < (UnityEngine.Random.value < 0.5 ? 3 : 7) ; i++)
            {
                var instance = Instantiate(resource_prefab, transform.position, Quaternion.identity);
                var rb = instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 10f, ForceMode2D.Impulse);
            }
            Globals.Datas.EnemyPool.Remove(this);
            GlobalEventBus.OnEnemyDead.Invoke();
        }
        if (rangeGO != null)
        {
            Destroy(rangeGO);
            rangeGO = null;
        }  
        Destroy(gameObject);
    }
}
