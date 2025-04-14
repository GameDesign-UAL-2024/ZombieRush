using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class Item2 : Items
{
    public override int ID { get; set; } = 1;
    public override ItemRanks Rank { get; set; } = ItemRanks.A;
    public override ItemTypes Type { get; set; } = ItemTypes.ShootBehaviour_Bullet;
    string bullet_prefab = "Prefabs/DefultBullet";
    public List<Bullet> bullets = new List<Bullet>();
    Vector2 mouse_position;
    public int fire_rate = 3;  // 每秒最多发射 3 颗子弹
    private float nextFireTime = 0f; // 记录下次可以射击的时间
    PlayerController player;

    public void SetBullet(string prefab_adress)
    {
        bullet_prefab = "Prefabs/DefultBullet";
    }
    void Start()
    {
        player = transform.GetComponent<PlayerController>();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && Time.time >= nextFireTime)
        {
            // 计算下次射击时间
            nextFireTime = Time.time + (1f / fire_rate);

            // 生成子弹
            Addressables.LoadAssetAsync<GameObject>(bullet_prefab).Completed += OnBulletLoaded;

            // 获取鼠标点击的世界坐标
            Vector3 screenPos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            mouse_position = new Vector2(worldPos.x, worldPos.y);
        }
    }
    private bool IsPointerOverUI()
    {
        // 创建一个指针事件数据，使用当前鼠标位置
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        // 射线检测所有UI元素
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        // 如果检测到的UI元素数量大于0，则返回true
        return results.Count > 0;
    }
    private void OnBulletLoaded(AsyncOperationHandle<GameObject> handle)
    {
        GameObject bulletPrefab = handle.Result;
        if (player != null && player.player_properties != null)
        {
            // 子弹初始位置：中间子弹使用 transform.position，其左右两侧偏移 0.5 个单位
            Vector3 spawnPosMiddle = transform.position;
            Vector3 spawnPosLeft = transform.position - new Vector3(0.5f, 0, 0);
            Vector3 spawnPosRight = transform.position + new Vector3(0.5f, 0, 0);

            // 计算从 spawnPos 到目标鼠标位置的方向（归一化）
            Vector2 dirMiddle = ((Vector2)spawnPosMiddle - mouse_position).normalized;
            // 注意：如果你希望子弹飞向鼠标，则方向应为 (mouse_position - spawnPos) 的归一化
            // 这里假设中间子弹正朝向鼠标方向
            dirMiddle = (mouse_position - (Vector2)spawnPosMiddle).normalized;

            // 分别旋转 15 度：左侧子弹顺时针旋转15度，右侧逆时针旋转15度（或反之，根据实际效果调整）
            Vector2 dirLeft = Quaternion.AngleAxis(15f, Vector3.forward) * dirMiddle;
            Vector2 dirRight = Quaternion.AngleAxis(-15f, Vector3.forward) * dirMiddle;

            // 为了使子弹按照指定方向发射，这里设置一个足够大的距离来计算目标点
            float distance = 1000f;
            Vector2 targetMiddle = (Vector2)spawnPosMiddle + dirMiddle * distance;
            Vector2 targetLeft   = (Vector2)spawnPosLeft   + dirLeft   * distance;
            Vector2 targetRight  = (Vector2)spawnPosRight  + dirRight  * distance;

            // 生成中间子弹
            GameObject bulletMiddle = Instantiate(bulletPrefab, spawnPosMiddle, Quaternion.identity);
            Bullet bulletCompMiddle = bulletMiddle.GetComponent<Bullet>();
            bulletCompMiddle.Initialize(spawnPosMiddle, targetMiddle, 
                bulletMiddle.GetComponent<Rigidbody2D>(), 
                player.player_properties.bullet_speed,
                player.player_properties.damage,
                player.player_properties.bullet_exist_time);
            bullets.Add(bulletCompMiddle);

            // 生成左侧子弹
            GameObject bulletLeft = Instantiate(bulletPrefab, spawnPosLeft, Quaternion.identity);
            Bullet bulletCompLeft = bulletLeft.GetComponent<Bullet>();
            bulletCompLeft.Initialize(spawnPosLeft, targetLeft, 
                bulletLeft.GetComponent<Rigidbody2D>(), 
                player.player_properties.bullet_speed,
                player.player_properties.damage,
                player.player_properties.bullet_exist_time);
            bullets.Add(bulletCompLeft);

            // 生成右侧子弹
            GameObject bulletRight = Instantiate(bulletPrefab, spawnPosRight, Quaternion.identity);
            Bullet bulletCompRight = bulletRight.GetComponent<Bullet>();
            bulletCompRight.Initialize(spawnPosRight, targetRight, 
                bulletRight.GetComponent<Rigidbody2D>(), 
                player.player_properties.bullet_speed,
                player.player_properties.damage,
                player.player_properties.bullet_exist_time);
            bullets.Add(bulletCompRight);
            if (PlayerItemManager.Instance != null && this.Type == ItemTypes.ShootBehaviour_Bullet)
            {
                if (PlayerItemManager.Instance.current_BulletEffects.Count > 0)
                {
                    foreach(int id in PlayerItemManager.Instance.current_BulletEffects)
                    {
                        if (ItemFactory.GetTypeByID(id) == ItemTypes.BulletEffect)
                        {
                            ItemFactory.CreateItemByID(id , bulletMiddle);
                            ItemFactory.CreateItemByID(id , bulletLeft);
                            ItemFactory.CreateItemByID(id , bulletRight);
                        }
                    }
                }
            }
        }
    }
    public void RemoveBulletFromList(Bullet bullet)
    {
        if (bullets.Contains(bullet))
        {
            bullets.Remove(bullet);
        }
    }
}
