using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class B0 : Buildings
{
    // —— 实现基类接口 —— 
    public override BuildingType this_type    { get; set; } = BuildingType.Trap;
    public override float max_health          { get; set; } = 3f;
    public override float current_health      { get; set; }
    public override int ID {get; set;} = 0;
    private UnityEvent<Buildings> destroyEvent = new UnityEvent<Buildings>();
    private bool initialized = false;
    private const string snailIconAddress = "Prefabs/snail_icon";
    // 缓存加载好的图标预制件
    private GameObject snailIconPrefab;
    // 记录每个被减速敌人对应的图标实例
    private Dictionary<Enemy, GameObject> snailIcons = new Dictionary<Enemy, GameObject>();
    // —— 减速逻辑参数 —— 
    [Header("Slow Effect")]
    [Tooltip("减速半径")]
    public float slowRadius   = 10f;
    [Tooltip("减速系数，如 0.5 表示原速 * 0.5")]
    public float slowFactor   = 0.5f;
    [Tooltip("扫描并应用减速的时间间隔（秒）")]
    public float interval     = 0.5f;

    // —— 存储已经被减速的敌人及其原速 —— 
    private Dictionary<Enemy, float> slowedEnemies = new Dictionary<Enemy, float>();

    void Awake()
    {
        // 初始化血量
        current_health = max_health;
    }

    void Start()
    {
        // 1) 先同步加载一次图标预制件
        snailIconPrefab = Addressables
            .LoadAssetAsync<GameObject>(snailIconAddress)
            .WaitForCompletion();

        // 启动减速循环
        StartCoroutine(SlowRoutine());
    }

    /// <summary>
    /// 定期扫描全局敌人列表，应用或移除减速
    /// </summary>
    private IEnumerator SlowRoutine()
    {
        var enemyPool = Globals.Datas.EnemyPool;
        var wait = new WaitForSeconds(interval);
        float r2 = slowRadius * slowRadius;

        while (true)
        {
            Vector2 myPos = transform.position;

            // 扫描每个敌人，应用或移除减速+图标
            foreach (Enemy e in enemyPool)
            {
                if (e == null) continue;
                Vector2 ep = e.transform.position;
                float dist2 = (ep - myPos).sqrMagnitude;

                if (dist2 <= r2)
                {
                    // 进入范围，且尚未减速
                    if (!slowedEnemies.ContainsKey(e))
                    {
                        slowedEnemies[e] = e.speed;
                        e.speed *= slowFactor;

                        // 实例化图标并记录
                        if (snailIconPrefab != null)
                        {
                            var icon = Instantiate(
                                snailIconPrefab,
                                e.transform.position + Vector3.up * 3f,
                                Quaternion.identity
                            );
                            snailIcons[e] = icon;
                        }
                    }
                }
                else
                {
                    // 出范围、并且之前减速过
                    if (slowedEnemies.TryGetValue(e, out float orig))
                    {
                        e.speed = orig;
                        slowedEnemies.Remove(e);

                        // 销毁对应图标
                        if (snailIcons.TryGetValue(e, out var icon))
                        {
                            Destroy(icon);
                            snailIcons.Remove(e);
                        }
                    }
                }
            }

            // 更新存活图标的位置
            // 用 ToList() 复制键值，避免遍历时修改字典报错
            foreach (var kv in new List<KeyValuePair<Enemy, GameObject>>(snailIcons))
            {
                var enemy = kv.Key;
                var icon  = kv.Value;
                if (enemy != null && icon != null)
                {
                    icon.transform.position = enemy.transform.position + Vector3.up * 3f;
                }
            }

            // 清理已销毁/不在列表中的敌人残留
            var toRemove = new List<Enemy>();
            foreach (var kv in snailIcons)
                if (kv.Key == null || !enemyPool.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            foreach (var e in toRemove)
            {
                if (snailIcons[e] != null) Destroy(snailIcons[e]);
                snailIcons.Remove(e);
            }

            yield return wait;
        }
    }

    /// <summary>
    /// 受到伤害接口
    /// </summary>
    public override void TakeDamage(float amount)
    {
        current_health -= amount;
        if (current_health <= 0f)
        {
            // 1) 清理所有残留减速效果
            foreach (var kv in slowedEnemies)
                if (kv.Key != null) kv.Key.speed = kv.Value;
            slowedEnemies.Clear();

            // 2) 清理所有图标实例
            foreach (var icon in snailIcons.Values)
                if (icon != null) Destroy(icon);
            snailIcons.Clear();

            // 3) 通知外部销毁自己
            if (initialized) destroyEvent.Invoke(this);
        }
    }


    /// <summary>
    /// 初始化：绑定“建筑销毁”回调
    /// </summary>
    public override void Initialize(UnityAction<Buildings> onBuildingDestroy)
    {
        destroyEvent.AddListener(onBuildingDestroy);
        initialized = true;
    }
}
