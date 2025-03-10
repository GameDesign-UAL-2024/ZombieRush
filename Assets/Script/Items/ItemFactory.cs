using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ItemFactory
{
    private static Dictionary<int, Type> itemTypes = new Dictionary<int, Type>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFactory()
    {
        RegisterAllItems();
    }

    private static void RegisterAllItems()
    {
        // 创建一个临时的 GameObject 用于添加组件
        GameObject tempGO = new GameObject("TempItemObject");
        
        var itemSubclasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Items)));

        foreach (var type in itemSubclasses)
        {
            // 在同一个临时对象上添加组件
            Items itemInstance = tempGO.AddComponent(type) as Items;
            if (itemInstance != null)
            {
                int id = itemInstance.ID;
                if (!itemTypes.ContainsKey(id))
                {
                    itemTypes.Add(id, type);
                }
                // 立即销毁刚添加的组件，保持临时对象干净
                UnityEngine.Object.DestroyImmediate(itemInstance);
            }
        }
        
        // 完成后销毁临时 GameObject
        UnityEngine.Object.DestroyImmediate(tempGO);
    }

    public static Items.ItemTypes GetTypeByID(int id)
    {
        if (itemTypes.TryGetValue(id, out Type type))
        {
            GameObject tempGO = new GameObject("TempItemObject");
            Items itemInstance = tempGO.AddComponent(type) as Items;
            Items.ItemTypes result = itemInstance != null ? itemInstance.Type : Items.ItemTypes.None;
            UnityEngine.Object.DestroyImmediate(tempGO);
            return result;
        }
        Debug.LogWarning($"Item ID {id} not found!");
        return Items.ItemTypes.None;
    }

    public static Items CreateItemByID(int id, GameObject target)
    {
        if (itemTypes.TryGetValue(id, out Type type))
        {
            // 使用 AddComponent 将组件添加到 target 上，并转换为 Items 类型
            return target.AddComponent(type) as Items;
        }
        Debug.LogWarning($"Item ID {id} not found!");
        return null;
    }

}
