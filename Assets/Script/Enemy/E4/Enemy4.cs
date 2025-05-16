using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Enemy4 : Enemy
{
    [Header("Stats")]
    [SerializeField] private float maxHealth     = 2f;
    [SerializeField] private float speedField    = 2f;
    string Explosive_path = "Prefabs/E4Exp";
    GameObject explosion;
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
        explosion = Addressables.LoadAssetAsync<GameObject>(Explosive_path).WaitForCompletion();
        StartCoroutine(BehaviorRoutine());
    }
    void Update()
    {
                // 每帧把圈圈搬到脚下
        if (rangeGO != null)
            rangeGO.transform.position = transform.position;
    }
    private IEnumerator BehaviorRoutine()
    {
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
