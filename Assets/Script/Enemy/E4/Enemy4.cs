using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Enemy4 : Enemy
{
    [Header("Stats")]
    [SerializeField] private float maxHealth     = 2f;
    [SerializeField] private float speedField    = 2f;
    string Explosive_path = "Prefabs/E4Exp";
    GameObject explosion;
    GameObject resource_prefab;
    private float currentHealth;
    private GameObject currentTarget;

    // the invisible GameObject we’ll move toward
    private GameObject targetPoint;

    private Animator animator;
    private bool Dead;

    #region — Overrides of Enemy abstract members —
    public override float max_health
    {
        get => maxHealth;
        set => maxHealth = value;
    }
    public override float current_health
    {
        get => currentHealth;
        set => currentHealth = value;
    }
    public override float speed
    {
        get => speedField;
        set => speedField = value;
    }
    public override GameObject target
    {
        get => currentTarget;
        set => currentTarget = value;
    }
    public override void SetTarget(GameObject tar)
    {
        currentTarget = tar;
    }
    private RectTransform fillRT;
    private Canvas   healthBarCanvas;
    private Image    healthBarFill;
    Rigidbody2D RB;
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
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        // apply damage
        currentHealth = instantKill ? 0f : currentHealth - amount;

        // spawn & orient hit effect
        var hitFxPrefab = Addressables
            .LoadAssetAsync<GameObject>(hitted_prefab_path)
            .WaitForCompletion();
        if (hitFxPrefab != null)
        {
            var fx = Instantiate(hitFxPrefab, transform.position, Quaternion.identity);
            Vector3 dir = (transform.position - source).normalized;
            fx.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }

        // if dead during idle/moving/… stages
        if (currentHealth <= 0f)
        {
            Dead = true;
            animator.SetBool("Dead", true);
            return true;
        }

        return false;
    }
    #endregion

    void Awake()
    {
        currentHealth = maxHealth;
        animator     = GetComponent<Animator>();
        // default in case we never choose buildings
        currentTarget = GameObject.FindGameObjectWithTag("Player");
    }

    void Start()
    {
        // kick off our AI routine
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();

        explosion = Addressables.LoadAssetAsync<GameObject>(Explosive_path).WaitForCompletion();
        StartCoroutine(BehaviorRoutine());
        CreateHealthBar();
    }
    void Update()
    {
        if (Globals.Instance.Event.current_state == Globals.Events.GameState.pausing)
        {
            return;
        }
                // 每帧把圈圈搬到脚下
        if (rangeGO != null)
            rangeGO.transform.position = transform.position;
        if (RB != null)
        {
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.deltaTime);
        }
        UpdateHealthBar();
    }
    private IEnumerator BehaviorRoutine()
    {
        if (Globals.Instance.Event.current_state == Globals.Events.GameState.pausing)
        {
            yield return null;
        }
        // 1) wait 1.5s before choosing
        while (Globals.Instance.Event.current_state != Globals.Events.GameState.playing){ yield return null;}
        yield return new WaitForSeconds(0.5f);

        // 2) pick at random (50/50)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 pickPos;

        if (Random.value < 0.5f && Vector3.Distance(transform.position, player.transform.position) <= 10f)
        {
            pickPos = player.transform.position;
        }
        else
        {
            // cluster buildings whose pairwise edges ≤ 5f
            var allBuilds = PlayerBuildingManager.current_buildings
                              .Values
                              .Where(b => b != null)
                              .ToList();

            if (allBuilds.Count == 0)
            {
                pickPos = player.transform.position;
            }
            else
            {
                // BFS from a random seed
                var seed = allBuilds[Random.Range(0, allBuilds.Count)];
                var queue = new Queue<Buildings>();
                var seen  = new HashSet<Buildings>();
                queue.Enqueue(seed);
                seen.Add(seed);

                while (queue.Count > 0)
                {
                    var b = queue.Dequeue();
                    foreach (var other in allBuilds)
                    {
                        if (!seen.Contains(other) &&
                            Vector3.Distance(b.transform.position, other.transform.position) <= 5f)
                        {
                            seen.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }

                // center point of that cluster
                var center = Vector3.zero;
                foreach (var b in seen) center += b.transform.position;
                pickPos = center / seen.Count;
            }
        }

        // 3) create & store a stationary "targetPoint"
        targetPoint = new GameObject("Enemy4_TargetPoint");
        targetPoint.transform.position = pickPos;

        // 4) move toward it
        animator.SetBool("Moving", true);
        while (!Dead &&
               Vector3.Distance(transform.position, targetPoint.transform.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPoint.transform.position,
                speedField * Time.deltaTime
            );
            yield return null;
        }
        animator.SetBool("Moving", false);

        // 5) if still alive, attack
        if (!Dead)
            CreateRangeIndicator();
            animator.SetBool("Attack", true);
    }

    // clean up
    void OnDestroy()
    {
        if (targetPoint != null)
            Destroy(targetPoint);
    }
    public void OnExplosive()
    {
        // 播放爆炸特效
        Instantiate(explosion, transform.position, Quaternion.identity);
                // 播完特效后删掉圈圈

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

    // （可选）在 Scene 视图中画出检测范围，方便调试
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 5f);
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
    private void CreateRangeIndicator()
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

    // if you want to hook actual removal from pool on death animation event:
    public void OnDeath()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            if (resource_prefab == null)
                return;
            for (int i = 0; i < (UnityEngine.Random.value < 0.5 ? 1 : 2) ; i++)
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
