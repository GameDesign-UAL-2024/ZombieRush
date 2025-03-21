using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class B1 : Buildings
{
    public override float current_health { get; set; }
    public override float max_health { get; set; } = 20f;
    public override BuildingType this_type { get; set;} = BuildingType.Attack;
    public Transform shot_point;
    bool initialized;
    int max_attack_number = 3;
    float attack_range = 15f;
    float attack_gap = 2f;
    public float attack_point {get; private set;} = 2f;
    List<B1_Bullet> bullets;
    const string bullet_prefab = "Prefabs/B1_Bullet";
    // 缓存加载的子弹预制件，避免重复加载
    GameObject bulletPrefabCache;
    Globals global;
    UnityEvent<Buildings> destroy_event;
    GlobalTimer timer;

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
        StartCoroutine(Attack_Coruntine());
    }

    void Update()
    {
        // 可添加其他逻辑
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
    }

    void DestroyBullet(B1_Bullet bullet)
    {
        if (bullets.Contains(bullet))
        {
            bullets.Remove(bullet);
            Destroy(bullet.gameObject);
        }
    }

    public void BuildingDestroy()
    {
        destroy_event.Invoke(this);
    }
    public void Initialize(UnityAction<Buildings> on_building_destroy)
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
