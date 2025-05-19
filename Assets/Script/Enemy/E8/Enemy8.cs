using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Enemy8 : Enemy
{
    public override float max_health { get; set; } = 128f;
    public override float speed { get; set; } = 4f;
    public override float current_health { get; set; }
    public override GameObject target { get; set; }
    float behaviour_time = 4f;
    float behaviour_cool = 0f;
    float hurt_exist;
    Animator animator;
    string Attraction_path = "Prefabs/Attract";
    GameObject attraction_pref;
    string water_drop_step = "Prefabs/WaterDrops";
    string E8B = "Prefabs/E8B";
    GameObject E8B_pref;
    GameObject water_drop_step_prefab;
    GameObject player;
    [SerializeField] AudioClip watermove;
    Coroutine current_move;
    Rigidbody2D RB;
    [SerializeField] AudioClip MaoBGM;
    [SerializeField] AudioClip HaQi;
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
    public void PlayMaoDieBGM()
    {
        AudioSysManager.Instance.PlayBGM(MaoBGM,150);
    }
    public void PlayHaQi()
    {
        AudioSysManager.Instance.PlaySound(gameObject,HaQi,transform.position,1.5f,true);
    }
    private readonly string[] boolParams = {
        "Behaviour",
        "Hit",
        "Jump_Attack",
        "Healing",
        "Attack_Shoot",
        "Moving",
        "Attraction"
    };
    private readonly string[] attack_params = {
        "Jump_Attack",
        "Healing",
        "Attack_Shoot",
        "Moving",
        "Attraction"
    };
    public void ResetAllBoolParameters()
    {
        ChooseTarget();
        foreach (var param in boolParams)
        {
            animator.SetBool(param, false);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        hurt_exist = 0;
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(UnityEngine.Random.value < 0.475f ? "Prefabs/BlackBlock" : (UnityEngine.Random.value < (0.475f / 0.525f) ? "Prefabs/GreenBlock" : "Prefabs/PinkBlock")).WaitForCompletion();
        current_health = max_health;
        animator = GetComponent<Animator>();
        RB = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        attraction_pref = Addressables.LoadAssetAsync<GameObject>(Attraction_path).WaitForCompletion();
        water_drop_step_prefab = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
        E8B_pref = Addressables.LoadAssetAsync<GameObject>(E8B).WaitForCompletion();
        CreateHealthBar();
    }

    void Update()
    {
        if (Globals.Instance.Event.current_state != Globals.Events.GameState.playing)
        {
            return;
        }
        behaviour_cool += Time.deltaTime;
        if (behaviour_cool >= behaviour_time)
        {
            animator.SetBool("Behaviour", true);
        }
        else
        {
            animator.SetBool("Behaviour", false);
        }
        if (RB.velocity.magnitude > 0)
        {
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 0.2f);
        }
        if (current_health < max_health / 2 && behaviour_time > 1f)
        {
            behaviour_time = 1.5f;
        } 
        if (target != null)
        {
            Vector3 scl = transform.localScale;
            scl.x = (target.transform.position.x < transform.position.x) ? -1f : 1f;
            transform.localScale = scl;   
        }
        UpdateHealthBar();
    }
    void FixedUpdate()
    {
        if (animator.GetBool("Hurt"))
        {
            hurt_exist += Time.deltaTime;
            if (hurt_exist > 1f)
            {
                animator.SetBool("Hurt",false);
                hurt_exist = 0f;
            }
        }
        else
        {
            hurt_exist = 0f;
        }
    }
    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }
    public void Healing()
    {
        
        current_health += 5;

        current_health = Math.Clamp(current_health,0,max_health);
    }
    public void Moving()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (current_move != null){StopCoroutine(current_move);}
        current_move = StartCoroutine(MoveCoruntine(player.transform.position,false));
    }
    public void JumpAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (current_move != null){StopCoroutine(current_move);}
        current_move = StartCoroutine(MoveCoruntine(player.transform.position,true));
    }
    public void ShootAttack()
    {
        StartCoroutine(ShootCoruntine());
    }
    public void ActiveAttraction()
    {
        Instantiate(attraction_pref,transform.position,Quaternion.identity);
    }
    private IEnumerator ShootCoruntine()
    {
        int round = current_health > max_health / 2 ? 1 : 2;
        int number = current_health > max_health / 2 ? 2 : 5;
        for (int i = 0; i <= round; i++)
        {
            for (int n = 0; n <= number; n++)
            {
                FireBullet();
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    private IEnumerator MoveCoruntine(Vector2 target_pos , bool is_attack)
    {
        while (Vector2.Distance(transform.position, target_pos) > 0.1f)
        {
            float speed = 20f;
            float step  = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(
                transform.position,
                target_pos,
                step
            );
            yield return null;
        }
    }
    public void MakeDecision()
    {
        ResetAllBoolParameters();

        // 计算距离
        float dist = Vector2.Distance(transform.position, target.transform.position);

        // 动态组装可选状态
        List<string> available = new List<string>();

        // Jump_Attack: 距离小于6
        if (dist <= 7f)
            available.Add("Jump_Attack");

        // Healing: 血量低于 2/3 max
        if (current_health < (max_health / 3f) * 2f)
            available.Add("Healing");

        // Attack_Shoot 和 Moving: 距离大于6
        if (dist > 7f)
        {
            available.Add("Attack_Shoot");
            available.Add("Moving");
        }

        // Attraction: 距离小于4.5
        if (dist < 4.5f)
            available.Add("Attraction");

        // 如果无可用动作，默认Moving（你可以根据实际需求调整）
        if (available.Count == 0)
            available.Add("Moving");

        // 随机挑一个
        int idx = UnityEngine.Random.Range(0, available.Count);
        animator.SetBool(available[idx], true);

        behaviour_cool = 0f;
        animator.SetBool("Behaviour", false);
    }
    public void FireBullet()
    {
        GameObject go = Instantiate(E8B_pref, transform.position, Quaternion.identity);
        var bullet = go.GetComponent<E8_Bullet>();
        if (target != null)
        {
            Vector2 dir = (target.transform.position - transform.position).normalized;
            bullet.Initialize(this, dir, 0f, 8f, 10f, 0.5f);
        }
        
    }
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        current_health -= amount;
         animator.SetBool("Hurt", true);
        if (current_health <= 0f)
        {
            animator.SetBool("Dead", true);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        return true;
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

        target = (bestBuild != null && bestBuildDist < playerDist)
            ? bestBuild 
            : player;
    }
    void PlayMovingSound()
    {
        if (AudioSysManager.Instance == null) return;


        // 2) 检测脚下是否是水
        if (ChunkGenerator.Instance != null && ChunkGenerator.Instance.IsTileOfType(transform.position, ChunkGenerator.Instance.waterTile))
        {
            // 4) 播放水上移动音效
            Instantiate(water_drop_step_prefab,transform.position,Quaternion.identity,transform);
            AudioSysManager.Instance.PlaySound(gameObject, watermove,transform.position,0.8f,false);
        }
    }
    public void AttackSound()
    {
        PlayAttackSound(gameObject, transform.position);
    }
    // if you want to hook actual removal from pool on death animation event:
    public void OnDeath()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            //如果不再是最后一个boss则应用以下代码
            // if (resource_prefab == null)
            //     return;
            // for (int i = 0; i < (UnityEngine.Random.value < 0.5 ? 1 : 2) ; i++)
            // {
            //     var instance = Instantiate(resource_prefab, transform.position, Quaternion.identity);
            //     var rb = instance.GetComponent<Rigidbody2D>();
            //     if (rb != null)
            //         rb.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 10f, ForceMode2D.Impulse);
            // }
            Globals.Datas.EnemyPool.Remove(this);
            GlobalEventBus.OnEnemyDead.Invoke();
        }
        GameObject finishing = Addressables.LoadAssetAsync<GameObject>("Prefabs/FinishChair").WaitForCompletion();
        Instantiate(finishing , transform.position , Quaternion.identity);
        Destroy(gameObject);
    }
}
