using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

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
    string water_drop_step = "Prefabs/WaterDrops";
    GameObject water_drop_step_prefab;
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
    GameObject resource_prefab;
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
        CreateHealthBar();
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();
        current_health = max_health;
        self_nav = transform.GetComponent<EnemyNav>();
        water_drop_step_prefab = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
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
        RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.deltaTime);
        UpdateHealthBar();
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
            RB.velocity = direction * 10f;
        }
    }
    public void AttackSound()
    {
        PlayAttackSound(gameObject, transform.position);
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
        Destroy(this.gameObject);
    }
}
