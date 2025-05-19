using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyNav))]
public class Enemy1 : Enemy
{
    public enum EnemyState { Wait, Moving, Attack }
    private EnemyState _currentState;
    public EnemyState current_state { get => _currentState; private set => _currentState = value; }
    string water_drop_step = "Prefabs/WaterDrops";
    GameObject water_drop_step_prefab;
    //音频
    [SerializeField] AudioClip move;
    [SerializeField] AudioClip watermove1;
    [SerializeField] AudioClip watermove2;
    public override float max_health { get; set; } = 5f;
    public override float current_health { get; set; }
    public override float speed { get; set; } = 4f;
    public override GameObject target { get; set; }

    [Header("Attack Settings")]
    [SerializeField] float dashDuration = 2f;
    [SerializeField] float dashMultiplier = 0.8f;

    [Header("Rotation Settings")]
    [SerializeField] float rotationSpeed = 1f;

    [SerializeField] ParticleSystem attacking_effect;

    private EnemyNav navigation;
    private GlobalTimer g_timer;
    private GameObject player;
    private Animator animator;
    private Rigidbody2D RB;
    private SpriteRenderer sprite_renderer;
    private Vector3 originalScale;
    private float behaviour_time;
    private readonly float behaviour_gap = 3f;
    private bool could_hurt = true;
    private bool dying = false;
    GameObject resource_prefab;
    void Start()
    {
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();
        CreateHealthBar();
        DeactiveAttackEffect();
        current_health = max_health;
        navigation = GetComponent<EnemyNav>();
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite_renderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        water_drop_step_prefab = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
        g_timer = GlobalTimer.Instance;
        behaviour_time = g_timer.GetCurrentTime();
        current_state = EnemyState.Wait;

        player = GameObject.FindGameObjectWithTag("Player");
        target = player;
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

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, 0f, 199f);
        pos.y = Mathf.Clamp(pos.y, 0f, 199f);
        transform.position = pos;
    }


    void Update()
    {
        if (dying) return;
        // —— 修复：只要是 Moving 状态就保证导航激活 —— 
        if (current_state == EnemyState.Moving && !navigation.is_activing)
        {
            navigation.SetNavActive(true);
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        float now = g_timer.GetCurrentTime();

        if (current_state != EnemyState.Attack)
        {
            float sign = player.transform.position.x < transform.position.x ? -1f : 1f;
            transform.localScale = new Vector3(originalScale.x * sign, originalScale.y, originalScale.z);

            var shape = attacking_effect.shape;
            shape.rotation = new Vector3(0f, 0f, sign > 0 ? 90f : -90f);

            if (now - behaviour_time >= behaviour_gap && current_state == EnemyState.Wait)
                SetState(EnemyState.Moving);

            if (dist <= 10f && now - behaviour_time > behaviour_gap)
                SetState(EnemyState.Attack);
        }

        if (current_health <= 0 && !dying)
        {
            dying = true;
            could_hurt = false;
            SetState(EnemyState.Wait);
            animator.SetBool("Dead", true);
            navigation.SetNavActive(false);
        }
        UpdateHealthBar();
    }

    void FixedUpdate()
    {
        if (current_state != EnemyState.Attack)
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
        if (current_state != EnemyState.Attack)
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
    }

    public override bool TakeDamage(Vector3 source, float amount, bool Instant_kill)
    {
        if (!could_hurt) return false;
        current_health -= amount;
        animator.SetTrigger("Hurt");
        Vector2 backDir = -(source - transform.position).normalized;
        RB.velocity = backDir * 2f;
        navigation.SetNavActive(false);
        return true;
    }

    void SetState(EnemyState newState)
    {
        if (newState == current_state) return;

        animator.ResetTrigger("Attacking");
        animator.SetBool("Moving", false);
        navigation.SetNavActive(false);

        current_state = newState;
        switch (newState)
        {
            case EnemyState.Wait:
                behaviour_time = g_timer.GetCurrentTime();
                RB.velocity = Vector2.Lerp(RB.velocity , Vector2.zero , 0.2f);
                break;
            case EnemyState.Moving:
                RB.velocity = Vector2.Lerp(RB.velocity , Vector2.zero , 0.2f);
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
                animator.SetTrigger("Attacking");
                break;
        }
    }
    public void StartAttackDash()
    {
        StartCoroutine(AttackDash());
    }
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
    IEnumerator AttackDash()
    {
        navigation.SetNavActive(false);
        ActiveAttackEffect();

        float start = g_timer.GetCurrentTime();

        while (g_timer.GetCurrentTime() - start <= dashDuration)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (transform.localScale.x < 0) angle += 180f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * 0.25f * Time.deltaTime);

            transform.Translate((transform.localScale.x > 0 ? transform.right : -transform.right) * speed * dashMultiplier * Time.deltaTime, Space.World);
            yield return null;
        }

        DeactiveAttackEffect();
        transform.rotation = Quaternion.identity;
        SetState(EnemyState.Wait);
    }

    public override void SetTarget(GameObject tar) => target = tar;
    public void ActiveAttackEffect() => attacking_effect.Play();
    public void DeactiveAttackEffect() => attacking_effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    public void DestroyEnemy()
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
        Destroy(gameObject);
    }
}


