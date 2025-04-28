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
    private static Dictionary<int, Items.ItemRanks> rankCache = new();
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
        // 扫描所有 Items 子类
        var itemSubclasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Items)));

        // 临时容器，用于创建 MonoBehaviour 实例
        GameObject tempContainer = new GameObject("TempItemsContainer");

        foreach (var type in itemSubclasses)
        {
            // 在临时容器上添加组件以实例化
            Items instance = tempContainer.AddComponent(type) as Items;
            if (instance == null)
            {
                Debug.LogWarning($"无法为类型 {type.Name} 创建实例。");
                continue;
            }

            int id = instance.ID;
            var itemType = instance.Type;
            var itemRank = instance.Rank;

            // 检查重复 ID
            if (itemTypes.ContainsKey(id))
            {
                Debug.LogWarning($"重复的道具 ID:{id} 在类 {type.Name} 中已存在。原始类：{itemTypes[id].Name}");
                GameObject.DestroyImmediate(instance);
                continue;
            }

            // 缓存类型和 Rank
            itemTypes.Add(id, type);
            itemTypeCache.Add(id, itemType);
            rankCache.Add(id, itemRank);

            // 根据类型分类存入不同的列表
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

            // 销毁刚才挂载的临时实例
            GameObject.DestroyImmediate(instance);
        }

        // 销毁临时容器
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
        if (rankCache.TryGetValue(id, out var rank))
            return rank;
        Debug.LogWarning($"Item ID {id} 未找到等级信息！");
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
