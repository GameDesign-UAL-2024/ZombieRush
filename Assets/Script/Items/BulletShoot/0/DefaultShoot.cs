using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class DefaultShoot : Items
{
    public override int ID { get; set; } = 0;
    public override ItemTypes Type { get; set; } = ItemTypes.ShootBehaviour_Bullet;
    static string bullet_prefab = "Prefabs/DefultBullet";
    public List<Bullet> bullets = new List<Bullet>();
    Vector2 mouse_position;
    public int fire_rate = 3;  // 每秒最多发射 3 颗子弹
    private float nextFireTime = 0f; // 记录下次可以射击的时间
    PlayerController player;

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
        GameObject new_bullet = Instantiate(handle.Result, new Vector3(transform.position.x,transform.position.y,-1),Quaternion.identity);
        ItemFactory.CreateItemByID(1,new_bullet);
        Bullet items_component = new_bullet.GetComponent<Bullet>();
        bullets.Add(items_component);
        if (player != null)
        {
            if (player.player_properties != null)
            {
                items_component.Initialize(transform.position,mouse_position,new_bullet.GetComponent<Rigidbody2D>() 
                , player.player_properties.bullet_speed 
                , player.player_properties.damage 
                , player.player_properties.bullet_exist_time);
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
    public override void OnShoot(){}
    public override void ActiveBulletEffects(){}

}
