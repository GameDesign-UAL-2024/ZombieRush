using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class BladeStorm : Items
{
    public override int ID { get; set; } = 9;
    public override ItemRanks Rank { get; set; } = ItemRanks.S;
    public override ItemTypes Type { get; set; } = ItemTypes.Proactive;
    PlayerUI ui_instance;
    // 冷却相关字段
    [Tooltip("子弹发射的冷却时间，单位为秒")] 
    [SerializeField] private float cool_down_time = 5f;    // 总冷却时间
    private float current_cool_time = 0f;                  // 已流逝冷却时间
    public float CurrentCoolRate => Mathf.Clamp01(current_cool_time / cool_down_time); // 冷却进度比率，可供UI展示

    [Tooltip("子弹预制件的 Addressable 地址，如 Prefabs/BladeBullet")]
    public string bulletPrefabReference = "Prefabs/BladeBullet";

    private GameObject bulletPrefab;                      // 缓存的预制件引用
    private List<BladeStormBullet> activeBullets = new List<BladeStormBullet>(); // 活跃子弹池

    private async void Awake()
    {
        // 异步加载 Addressables 预制件，仅执行一次
        var handle = Addressables.LoadAssetAsync<GameObject>(bulletPrefabReference);
        await handle.Task;
        bulletPrefab = handle.Result;
        if (bulletPrefab == null)
            Debug.LogError($"BladeStorm: 无法加载子弹预制件 {bulletPrefabReference}");
    }
    void Start()
    {
        ui_instance = PlayerUI.Instance;
        if (ui_instance.ProactiveItemImage != null)
        {
            string img_path = "Sprites/Items/" + ID.ToString();
            Sprite item_image = Resources.Load<Sprite>(img_path);
            if (item_image == null)
                return;
            ui_instance.ProactiveItemImage.sprite = item_image;
        }
    }
    private void Update()
    {
        // 累加冷却计时
        if (current_cool_time < cool_down_time)
        {
            current_cool_time += Time.deltaTime;
            if (ui_instance.CoolDownText != null)
            {
                ui_instance.CoolDownText.text = Mathf.CeilToInt(cool_down_time - current_cool_time).ToString();
            }
        }
        else
        {
            if (ui_instance.CoolDownText != null)
            {
                ui_instance.CoolDownText.text = "";
            }               
        }
        // 按下空格，且冷却时间已到，才可发射
        if (Input.GetKeyDown(KeyCode.Space) && bulletPrefab != null && current_cool_time >= cool_down_time)
        {
            FireEightDirections();
            current_cool_time = 0f;  // 重置冷却计时
        }
    }

    /// <summary>
    /// 在八个方向各实例化一个子弹，并注册回调维护列表。
    /// </summary>
    private void FireEightDirections()
    {
        Vector2[] dirs = new Vector2[] {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1,1), new Vector2(-1,1), new Vector2(1,-1), new Vector2(-1,-1)
        };

        foreach (var dir in dirs)
        {
            var go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            var bullet = go.GetComponent<BladeStormBullet>();
            if (bullet != null)
            {
                bullet.Initialize(dir);            // 设置方向和自动销毁
                activeBullets.Add(bullet);         // 加入活跃列表
                bullet.OnDestroyed += RemoveBullet; // 注册销毁回调
            }
        }
    }

    /// <summary>
    /// 子弹销毁时，从活跃列表中移除。
    /// </summary>
    private void RemoveBullet(BladeStormBullet bullet)
    {
        activeBullets.Remove(bullet);
    }

    /// <summary>
    /// 提供访问当前活跃子弹列表的接口。
    /// </summary>
    public IReadOnlyList<BladeStormBullet> ActiveBullets => activeBullets.AsReadOnly();
}
