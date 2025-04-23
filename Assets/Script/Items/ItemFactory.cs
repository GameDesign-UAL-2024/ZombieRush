using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ItemFactory
{
    private static Dictionary<int, Type> itemTypes = new Dictionary<int, Type>();
    private static Dictionary<int, Items.ItemTypes> itemTypeCache = new Dictionary<int, Items.ItemTypes>();

    public static List<int> PropertieItems {get; private set;} =  new List<int>();
    public static List<int> BulletEffectItems {get; private set;} =  new List<int>();
    public static List<int> AdvancedItems {get; private set;} =  new List<int>();
    public static List<int> AdditionalAttack {get; private set;} =  new List<int>();
    public static List<int> ProactiveItems {get; private set;} =  new List<int>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFactory()
    {
        PropertieItems = new List<int>();
        BulletEffectItems = new List<int>();
        AdvancedItems = new List<int>();
        RegisterAllItems();
    }

    private static void RegisterAllItems()
    {
        var itemSubclasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Items)));

        // 创建一个临时 GameObject 用于生成 MonoBehaviour 实例
        GameObject tempContainer = new GameObject("TempItemsContainer");

        foreach (var type in itemSubclasses)
        {
            // 添加组件以创建实例（注意：MonoBehaviour 不能通过 new 操作符直接创建）
            Items instance = tempContainer.AddComponent(type) as Items;
            if (instance == null)
            {
                Debug.LogWarning($"无法为类型 {type.Name} 创建实例。");
                continue;
            }

            int id = instance.ID;
            Items.ItemTypes itemType = instance.Type;

            if (itemTypes.ContainsKey(id))
            {
                Debug.LogWarning($"重复的道具 ID:{id} 在类 {type.Name} 中已存在。原始类：{itemTypes[id].Name}");
                GameObject.DestroyImmediate(instance);
                continue;
            }

            itemTypes.Add(id, type);
            itemTypeCache.Add(id, itemType);

            //将物品按类别分类储存在列表，方便生成道具区分道具池
            if (itemType == Items.ItemTypes.Properties)
                PropertieItems.Add(id);
            else if ((itemType == Items.ItemTypes.ShootBehaviour_Bullet || itemType == Items.ItemTypes.ShootBehaviour_Lazer) && id != 0)
                AdvancedItems.Add(id);
            else if (itemType == Items.ItemTypes.BulletEffect)
                BulletEffectItems.Add(id);
            else if (itemType == Items.ItemTypes.Additional_Attack)
                AdditionalAttack.Add(id);
            else
                ProactiveItems.Add(id);

            // 注册后移除组件，避免多余的实例存在
            GameObject.DestroyImmediate(instance);
        }

        // 最后销毁临时容器
        GameObject.DestroyImmediate(tempContainer);
    }

    public static Items.ItemTypes GetTypeByID(int id)
    {
        if (itemTypeCache.TryGetValue(id, out var itemType))
        {
            return itemType;
        }

        Debug.LogWarning($"Item ID {id} 未找到对应类型！");
        return Items.ItemTypes.None;
    }

    public static Items.ItemRanks? GetRankByID(int id)
    {
        GameObject tempContainer = new GameObject("TempItemsContainer");
        if (itemTypes.ContainsKey(id))
        {
            CreateItemByID(id, tempContainer);
            Items instance = tempContainer.GetComponent<Items>();
            return instance.Rank;
        }
        Debug.Log("Cannot Find item");
        return null;
    }
    public static Items CreateItemByID(int id, GameObject target)
    {
        if (itemTypes.TryGetValue(id, out Type type))
        {
            return target.AddComponent(type) as Items;
        }

        Debug.LogWarning($"Item ID {id} 未找到对应类！");
        return null;
    }
}
