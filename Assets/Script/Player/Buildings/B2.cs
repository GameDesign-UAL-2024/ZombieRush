using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.UI;

public class B2 : Buildings
{
    private Canvas   healthBarCanvas;
    private Image    healthBarFill;
    Animator animator;
    // —— 实现基类接口 —— 
    public override BuildingType this_type    { get; set; } = BuildingType.Trap;
    public override float max_health          { get; set; } = 8f;
    public override float current_health      { get; set; }
    public override int ID {get; set;} = 3;
    private UnityEvent<Buildings> destroyEvent = new UnityEvent<Buildings>();
    private bool initialized = false;
    private const string snailIconAddress = "Prefabs/snail_icon";
    string bullet_prefab = "Prefabs/Qiu_Bullet";
    List<B2_Bullet> bullets = new List<B2_Bullet>();
    GameObject bulletPrefabCache;
    float attack_gap = 3.5f;
    UnityEvent<Buildings> destroy_event = new UnityEvent<Buildings>();
    void Awake()
    {
        // 初始化血量
        current_health = max_health;
        animator = transform.GetComponent<Animator>();
        CreateHealthBar();
        UpdateHealthBar();
    }

    [SerializeField] AudioClip FIRE;
    [SerializeField]AudioClip HIT;
    [SerializeField]AudioClip HURT;
    private RectTransform fillRT;
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
        bgRT.sizeDelta = new Vector2(1f, 0.1f);

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
        fillRT.anchorMax = new Vector2(0f, 1f);
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
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Attack_Coruntine());
        CreateHealthBar();
        UpdateHealthBar();
    }

    // Update is called once per frame
    void Update()
    {
    
    }
    Enemy closestEnemy;
    private IEnumerator Attack_Coruntine()
    {
        // 如果子弹预制件还未加载，则加载一次
        if(bulletPrefabCache == null)
        {
            bulletPrefabCache = Addressables.LoadAssetAsync<GameObject>(bullet_prefab).WaitForCompletion();
        }
        
        while(true)
        {
            // 如果未初始化，则不执行后续逻辑
            if (!initialized || Globals.Instance.Event.current_state == Globals.Events.GameState.pausing)
                yield return null;

            // 限制同时存在的子弹数量
            if(bullets.Count < 1)
            {
                // 在 attack_range 范围内查找最近的敌人（使用平方距离比较）
                
                // 只考虑在攻击范围内的敌人
                float maxSqrRange = 10 * 10;
                float minSqrDistance = maxSqrRange;
                Vector2 buildingPosition = transform.position;
                foreach(Enemy enemy in Globals.Datas.EnemyPool)
                {
                    if(enemy == null) continue;
                    float sqrDist = ((Vector2)enemy.transform.position - buildingPosition).sqrMagnitude;
                    if(sqrDist < minSqrDistance)
                    {
                        minSqrDistance = sqrDist;
                        closestEnemy = enemy;
                        // 判断 target 相对于当前对象的位置
                        if (closestEnemy.transform.position.x < transform.position.x)
                        {
                            // target 在左边，角色朝左：scale.x = 1
                            Vector3 scale = transform.localScale;
                            scale.x = Mathf.Abs(scale.x); // 保证为正
                            transform.localScale = scale;
                        }
                        else
                        {
                            // target 在右边，角色朝右：scale.x = -1
                            Vector3 scale = transform.localScale;
                            scale.x = -Mathf.Abs(scale.x); // 保证为负
                            transform.localScale = scale;
                        }
                    }
                }
        
                // 如果找到目标，并且射击点有效，则发射子弹
                if(closestEnemy != null)
                {
                    // 使用缓存好的预制件实例化子弹
                    animator.SetTrigger("Shoot");

                }
            }
        
            // 使用 attack_gap 作为发射间隔（attack_gap 为 0 时默认间隔为 0.5 秒）
            float gap = attack_gap > 0 ? attack_gap : 0.5f;
            yield return new WaitForSeconds(gap);
        }
    }
    public void ShootBullet()
    {
        GameObject bulletObj = Instantiate(bulletPrefabCache, transform.position, Quaternion.identity);
        B2_Bullet bullet = bulletObj.GetComponent<B2_Bullet>();
        if(bullet != null && closestEnemy != null)
        {
            bullets.Add(bullet);
            // 设置追踪转向速度（单位：度/秒），可根据需求调整
            float trackingTurnSpeed = 180f;
            // 调用子弹 Initialize 方法传入目标、追踪速度、飞行结束回调以及攻击力
            bullet.Initialize(closestEnemy.transform, trackingTurnSpeed, DestroyBullet, 2f);
            AudioSysManager.Instance.PlaySound(gameObject,FIRE,transform.position,1,false);
        }
    }
    void DestroyBullet(B2_Bullet bullet)
    {
        AudioSysManager.Instance.PlaySound(bullet.gameObject,HIT,bullet.transform.position,1,false);
        closestEnemy = null;
        if (bullets.Contains(bullet))
        {
            bullets.Remove(bullet);
            Destroy(bullet.gameObject);
        }
    }

    public void BuildingDestroy()
    {
        if (initialized)
            destroy_event.Invoke(this);
    }
    public override void Initialize(UnityAction<Buildings> on_building_destroy)
    {
        
        destroy_event.AddListener(on_building_destroy);
        initialized = true;
    }
    public override void TakeDamage(float amount)
    {
        current_health -= amount;
        UpdateHealthBar();

        if (current_health <= 0f)
        {
            // 销毁血条 Canvas
            if (healthBarCanvas != null)
                Destroy(healthBarCanvas.gameObject);

            if (initialized) destroyEvent.Invoke(this);
        }
    }
}
