using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ItemFactory
{
    private static Dictionary<int, Type> itemTypes = new Dictionary<int, Type>();
    private static Dictionary<int, Items.ItemTypes> itemTypeCache = new Dictionary<int, Items.ItemTypes>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFactory()
    {
        RegisterAllItems();
    }

    private static void RegisterAllItems()
    {
        var itemSubclasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Items)));

        foreach (var type in itemSubclasses)
        {
            FieldInfo idField = type.GetField("StaticID", BindingFlags.Static | BindingFlags.Public);
            FieldInfo typeField = type.GetField("StaticType", BindingFlags.Static | BindingFlags.Public);

            if (idField != null && typeField != null)
            {
                int id = (int)idField.GetValue(null);
                var itemType = (Items.ItemTypes)typeField.GetValue(null);

                if (itemTypes.ContainsKey(id))
                {
                    Debug.LogWarning($"重复的道具 ID：{id} 在类 {type.Name} 中已存在。原始类：{itemTypes[id].Name}");
                    continue;
                }

                itemTypes.Add(id, type);
                itemTypeCache.Add(id, itemType);
            }
            else
            {
                Debug.LogWarning($"类 {type.Name} 缺少 StaticID 或 StaticType 字段，无法注册。");
            }
        }
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
