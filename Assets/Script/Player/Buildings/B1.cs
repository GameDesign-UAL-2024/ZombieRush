using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
public class B1 : Buildings
{
    public override float current_health { get; set; }
    public override float max_health { get; set; } = 8f;
    public override BuildingType this_type { get; set;} = BuildingType.Attack;
    public Transform shot_point;
    public override int ID {get; set;} = 1;
    bool initialized;
    int max_attack_number = 3;
    float attack_range = 15f;
    float attack_gap = 1.5f;
    public float attack_point {get; private set;} = 0.5f;
    List<B1_Bullet> bullets;
    const string bullet_prefab = "Prefabs/B1_Bullet";
    // 缓存加载的子弹预制件，避免重复加载
    GameObject bulletPrefabCache;
    Globals global;
    UnityEvent<Buildings> destroy_event = new UnityEvent<Buildings>();
    GlobalTimer timer;
    // Audio
    [SerializeField] AudioClip FIRE;
    [SerializeField]AudioClip HIT;
    [SerializeField]AudioClip HURT;
    void Awake()
    {
        current_health = max_health;
    }
    void Start()
    {
        global = Globals.Instance;
        timer = GlobalTimer.Instance;
        bullets = new List<B1_Bullet>();
        // 启动攻击协程
        CreateHealthBar();
        UpdateHealthBar();
        StartCoroutine(Attack_Coruntine());
    }

    void Update()
    {
        // 可添加其他逻辑
        // 如果未初始化，则不执行后续逻辑
        if (!initialized || Globals.Instance.Event.current_state == Globals.Events.GameState.pausing)
            return;
        
    }
    private Canvas   healthBarCanvas;
    private Image    healthBarFill;
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
    private IEnumerator Attack_Coruntine()
    {
        // 如果子弹预制件还未加载，则加载一次
        if(bulletPrefabCache == null)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(bullet_prefab);
            yield return handle;
            bulletPrefabCache = handle.Result;
        }
        
        while(true)
        {
            // 限制同时存在的子弹数量
            if(bullets.Count < max_attack_number)
            {
                // 在 attack_range 范围内查找最近的敌人（使用平方距离比较）
                Enemy closestEnemy = null;
                // 只考虑在攻击范围内的敌人
                float maxSqrRange = attack_range * attack_range;
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
                    }
                }
        
                // 如果找到目标，并且射击点有效，则发射子弹
                if(closestEnemy != null && shot_point != null)
                {
                    // 使用缓存好的预制件实例化子弹
                    GameObject bulletObj = Instantiate(bulletPrefabCache, shot_point.position, Quaternion.identity);
                    B1_Bullet bullet = bulletObj.GetComponent<B1_Bullet>();
                    if(bullet != null)
                    {
                        bullets.Add(bullet);
                        // 设置追踪转向速度（单位：度/秒），可根据需求调整
                        float trackingTurnSpeed = 180f;
                        // 调用子弹 Initialize 方法传入目标、追踪速度、飞行结束回调以及攻击力
                        bullet.Initialize(closestEnemy.transform, trackingTurnSpeed, DestroyBullet, attack_point);
                        AudioSysManager.Instance.PlaySound(gameObject,FIRE,transform.position,1,false);
                    }
                }
            }
        
            // 使用 attack_gap 作为发射间隔（attack_gap 为 0 时默认间隔为 0.5 秒）
            float gap = attack_gap > 0 ? attack_gap : 0.5f;
            yield return new WaitForSeconds(gap);
        }
    }

    public override void TakeDamage(float amount)
    {
        current_health -= amount;
        AudioSysManager.Instance.PlaySound(gameObject,HURT,transform.position,1,false);
        if (current_health <= 0)
        {
            BuildingDestroy();
        }
        UpdateHealthBar();
    }

    void DestroyBullet(B1_Bullet bullet)
    {
        AudioSysManager.Instance.PlaySound(bullet.gameObject,HIT,bullet.transform.position,1,false);
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

    public void SetAtk(float amount)
    {
        attack_point = amount;
    }
    public void SetRange(float amount)
    {
        attack_range = amount;
    }
    public void SetAtkGap(float amount , bool forever , float time = 2f)
    {
        
    }
}
