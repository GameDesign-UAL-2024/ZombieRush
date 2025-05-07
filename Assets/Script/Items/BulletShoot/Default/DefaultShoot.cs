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
    public override ItemRanks Rank { get; set; } = ItemRanks.N;
    public override ItemTypes Type { get; set; } = ItemTypes.ShootBehaviour_Bullet;
    static string bullet_prefab_path = "Prefabs/DefultBullet";
    GameObject bullet_prefab;
    AudioClip hit_sound;
    public List<Bullet> bullets = new List<Bullet>();
    Vector2 mouse_position;
    private float nextFireTime = 0f; // 记录下次可以射击的时间
    PlayerController player;

    void Start()
    {
        player = transform.GetComponent<PlayerController>();
        bullet_prefab = Addressables.LoadAssetAsync<GameObject>(bullet_prefab_path).WaitForCompletion();
        hit_sound = Addressables.LoadAssetAsync<AudioClip>(bullet_prefab.GetComponent<Bullet>().hit_sound_path).WaitForCompletion();
        
        GlobalEventBus.OnHitEnemy.AddListener(PlayHitSound);
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && Time.time >= nextFireTime)
        {
            // 1. 先计算下次射击时间
            float atkSpeed = player.player_properties.atk_speed;
            if (atkSpeed <= 0f) atkSpeed = 1f;
            nextFireTime = Time.time + 1f / atkSpeed;

            // 2. **先获取本次点击的世界坐标**，并存到 mouse_position
            Vector3 screenPos = Input.mousePosition;
            Vector3 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;  // 忽略 Z 轴
            mouse_position = new Vector2(worldPos.x, worldPos.y);

            // 3. 再生成子弹（使用已经更新过的 mouse_position）
            GenerateBullet();
        }

        bullets.RemoveAll(b => b == null);
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
    private void GenerateBullet()
    {
        GameObject new_bullet = Instantiate(bullet_prefab, new Vector3(transform.position.x,transform.position.y,-1),Quaternion.identity);
        if (PlayerItemManager.Instance != null && this.Type == ItemTypes.ShootBehaviour_Bullet)
        {
            if (PlayerItemManager.Instance.current_BulletEffects.Count > 0)
            {
                foreach(int id in PlayerItemManager.Instance.current_BulletEffects)
                {
                    if (ItemFactory.GetTypeByID(id) == ItemTypes.BulletEffect)
                    {
                        ItemFactory.CreateItemByID(id , new_bullet);
                    }
                }
            }
        }
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

    void PlayHitSound(GameObject source)
    {
        if (AudioSysManager.Instance != null && hit_sound != null)
        {
            AudioSysManager.Instance.PlaySound(source,hit_sound,source.transform.position,0.8f,false);
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
