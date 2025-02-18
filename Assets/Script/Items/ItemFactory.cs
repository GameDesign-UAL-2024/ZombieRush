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
        var itemSubclasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Items)));

        foreach (var type in itemSubclasses)
        {
            GameObject tempObject = new GameObject("TempItemObject");
            Items itemInstance = tempObject.AddComponent(type) as Items;
            int id = itemInstance.ID;
            // 销毁临时创建的 GameObject
            GameObject.Destroy(tempObject);
            if (!itemTypes.ContainsKey(id))
            {
                itemTypes.Add(id, type);
            }
        }
    }

    public static Items.ItemTypes GetTypeByID(int id)
    {
        if (itemTypes.TryGetValue(id, out Type type))
        {
            GameObject tempObject = new GameObject("TempItemObject");

            // 使用 AddComponent 将反射得到的类型作为组件挂载
            Items itemInstance = tempObject.AddComponent(type) as Items;

            // 获取该实例的 Type 属性
            Items.ItemTypes itemType = itemInstance.Type;

            // 销毁临时创建的 GameObject
            GameObject.Destroy(tempObject);

            // 返回该类型
            return itemType;      
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
