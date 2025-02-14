using System.Collections;
using System.Collections.Generic;
using System.Linq; // 必须导入，以便使用 LINQ 的 FirstOrDefault 方法
using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    // 单例实例
    public static PlayerItemManager Instance { get; private set; }

    // 当前玩家拥有的道具列表
    private List<int> current_items = new List<int>(){0,1};

    GameObject player;

    // 在 Awake 中初始化单例
    private void Awake()
    {
        // 如果已有实例且不是当前对象，则销毁当前对象，保证只有一个实例存在
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (! player.TryGetComponent<Items>(out Items component))
        {
            int itemToAdd = current_items.Find(item => ItemFactory.GetTypeByID(item) == Items.ItemTypes.ShootBehaviour); 
            ItemFactory.CreateItemByID(itemToAdd , player);
        }
    }
    // 添加 Item，确保只有一个 ShootBehaviour 类型的 Item
    public void AddItem(int newItemID)
    {
        if (ItemFactory.GetTypeByID(newItemID) == Items.ItemTypes.ShootBehaviour)
        {
            int existingShootItem = -100;
            // 查找现有的 ShootBehaviour 道具
            existingShootItem = current_items.FirstOrDefault(item => ItemFactory.GetTypeByID(item) == Items.ItemTypes.ShootBehaviour);

            if (existingShootItem >= 0)
            {
                // 替换已有的 ShootBehaviour 道具
                current_items.Remove(existingShootItem);
                if ( player.TryGetComponent<Items>(out Items component))
                {
                    if ( component.Type == Items.ItemTypes.ShootBehaviour)
                    {
                        Destroy(component);
                    }
                }
                Debug.Log($"Replaced {existingShootItem.GetType().Name} with {newItemID.GetType().Name}");
            }
        }

        // 添加新道具
        current_items.Add(newItemID);
    }

}
