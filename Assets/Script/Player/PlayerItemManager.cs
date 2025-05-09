using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class PlayerItemManager : MonoBehaviour
{
    // 单例实例
    public static PlayerItemManager Instance { get; private set; }

    // 当前持有的子弹特效（可以多个）
    public List<int> current_BulletEffects { get; private set; } = new List<int>();

    // 当前持有的射击行为道具（只能一个）
    public int current_ShootBehaviour{ get; private set; } = 0; // -1 表示没有
    public int current_Proactive{ get; private set; } = -1;
    public List<int> Additional_Attacks{ get; private set; } = new List<int>(){};

    GameObject player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }
        
        // 挂载当前子弹特效道具（如果未挂载）
        foreach (int id in current_BulletEffects)
        {
            // 如果玩家身上不存在该 ID 的道具，则添加
            bool exists = player.GetComponents<Items>().Any(item => item.ID == id);
            if (!exists)
            {
                ItemFactory.CreateItemByID(id, player);
            }
        }
        
        // 挂载当前额外攻击道具（如果未挂载）
        foreach (int id in Additional_Attacks)
        {
            bool exists = player.GetComponents<Items>().Any(item => item.ID == id);
            if (!exists)
            {
                ItemFactory.CreateItemByID(id, player);
            }
        }
        if (!player.GetComponents<Items>().Any(item => item.ID == current_Proactive) && current_Proactive >=0)
        {
            ItemFactory.CreateItemByID(current_Proactive,player);
        }
        if (!player.GetComponents<Items>().Any(item => item.ID == current_ShootBehaviour))
        {
            ItemFactory.CreateItemByID(current_ShootBehaviour,player);
        }
    }
    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // 如果玩家身上没有射击行为组件，就重新挂上
        if (!player.TryGetComponent<Items>(out Items component))
        {
            if (ItemFactory.GetTypeByID(current_ShootBehaviour) == Items.ItemTypes.ShootBehaviour_Bullet ||
                ItemFactory.GetTypeByID(current_ShootBehaviour) == Items.ItemTypes.ShootBehaviour_Lazer)
            {
                ItemFactory.CreateItemByID(current_ShootBehaviour, player);
            }
        }
    }

    // 添加新道具
    public void AddItem(int newItemID)
    {
        Items.ItemTypes newItemType = ItemFactory.GetTypeByID(newItemID);

        if (newItemType == Items.ItemTypes.ShootBehaviour_Bullet || newItemType == Items.ItemTypes.ShootBehaviour_Lazer)
        {
            // 如果已有射击行为，先移除
            if (current_ShootBehaviour >= 0)
            {
                if (player.TryGetComponent<Items>(out Items component))
                {
                    if (component.Type == Items.ItemTypes.ShootBehaviour_Bullet || component.Type == Items.ItemTypes.ShootBehaviour_Lazer)
                    {
                        Destroy(component);
                    }
                }
            }

            // 替换成新射击行为
            current_ShootBehaviour = newItemID;
            ItemFactory.CreateItemByID(current_ShootBehaviour, player);
        }
        else if (newItemType == Items.ItemTypes.BulletEffect)
        {
            // 如果是子弹特效类，加入特效列表
            if (!current_BulletEffects.Contains(newItemID))
            {
                current_BulletEffects.Add(newItemID);
                ItemFactory.CreateItemByID(newItemID, player); // 也挂载组件
            }
        }
        else if (newItemType == Items.ItemTypes.Properties)
        {
            ItemFactory.CreateItemByID(newItemID, player); // 也挂载组件
        }
        else if (newItemType == Items.ItemTypes.Additional_Attack)
        {
            // —— 新增：只有当前列表里没有该 ID 才添加 —— 
            if (!Additional_Attacks.Contains(newItemID))
            {
                Additional_Attacks.Add(newItemID);
                ItemFactory.CreateItemByID(newItemID, player);
            }
        }
        else if (newItemType == Items.ItemTypes.Proactive)
        {
            // 先查找并移除已有的 Proactive 行为组件
            var allItems = player.GetComponents<Items>();
            foreach (var itemComp in allItems)
            {
                if (itemComp.Type == Items.ItemTypes.Proactive)
                {
                    Destroy(itemComp);
                }
            }

            // 更新当前记录并挂载新的 Proactive 组件
            current_Proactive = newItemID;
            ItemFactory.CreateItemByID(newItemID, player);
        }
            }

    public List<int> GetBulletEffects()
    {
        return current_BulletEffects;
    }

    public int GetCurrentShootBehaviour()
    {
        return current_ShootBehaviour;
    }
}
