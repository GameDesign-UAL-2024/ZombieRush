using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerBuildingManager : MonoBehaviour
{
    public static PlayerBuildingManager Instance;

    // 用单一整数键（例如：x * 1000 + y）记录当前已放置的建筑
    public static Dictionary<int, Buildings> current_buildings = new Dictionary<int, Buildings>();

    // 缓存已加载的预制体，避免重复加载
    Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 对外接口，启动协程加载并放置建筑
    public void SpawnBuilding(string prefabAddress, Vector2 position)
    {
        StartCoroutine(SpawnBuildingCoroutine(prefabAddress, position));
    }

    private IEnumerator SpawnBuildingCoroutine(string prefabAddress, Vector2 position)
    {
        // 将传入的位置转换为整数网格坐标
        Vector2Int gridPosition = Vector2Int.FloorToInt(position);

        // 首先检查 ObjectSpawner.objectRecords 中是否已有记录，且所有记录位置必须与新位置间隔至少2个单位
        if (!IsPositionValid(gridPosition))
        {
            Debug.Log("无法放置建筑:位置与已有建筑或记录距离不足2个单位");
            yield break;
        }

        // 从缓存中尝试获取预制体，否则使用 Addressables 异步加载
        GameObject prefab;
        if (!loadedPrefabs.TryGetValue(prefabAddress, out prefab))
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabAddress);
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("加载预制体失败: " + prefabAddress);
                yield break;
            }
            prefab = handle.Result;
            loadedPrefabs[prefabAddress] = prefab;
        }

        // 获取预制体上的 Buildings 组件，检查是否存在
        Buildings prefabBuildingComponent = prefab.GetComponent<Buildings>();
        if (prefabBuildingComponent == null)
        {
            Debug.LogError("预制体缺少 Buildings 组件: " + prefabAddress);
            yield break;
        }

        // 再次检查新建筑放置是否合法（此处如果需要针对建筑类型做其他判断，可额外处理）
        if (!IsPositionValid(gridPosition))
        {
            Debug.Log("无法放置建筑：位置不合法");
            yield break;
        }

        // 实例化建筑到指定网格位置
        GameObject newBuilding = Instantiate(prefab, (Vector2)gridPosition, Quaternion.identity);
        Buildings newBuildingComponent = newBuilding.GetComponent<Buildings>();
        if (newBuildingComponent != null)
        {
            int key = gridPosition.x * 1000 + gridPosition.y;
            newBuildingComponent.Initialize(OnBuildingDestroyed);
            current_buildings.Add(key, newBuildingComponent);
        }
    }

    /// <summary>
    /// 检查给定网格位置是否合法。
    /// 新建筑的位置必须与当前所有建筑（记录在 current_buildings 中）以及 ObjectSpawner.objectRecords 中的所有位置
    /// 保持至少2个单位的距离，无论建筑类型如何。
    /// </summary>
    public bool IsPositionValid(Vector2Int position)
    {
        // 检查 current_buildings 中的所有建筑位置
        foreach (var kv in current_buildings)
        {
            Vector2Int existingPos = new Vector2Int(kv.Key / 1000, kv.Key % 1000);
            if (Vector2Int.Distance(position, existingPos) < 2f)
            {
                return false;
            }
        }
        // 检查 ObjectSpawner.objectRecords 中的所有记录位置
        foreach (var kv in ObjectSpawner.objectRecords)
        {
            if (kv.Value != null && Vector2.Distance(position, kv.Key) < 2f)
            {
                return false;
            }
        }
        return true;
    }

    // 建筑被销毁时，清理相关记录
    public void OnBuildingDestroyed(Buildings building)
    {
        Vector2Int position = Vector2Int.FloorToInt(building.transform.position);
        int key = position.x * 1000 + position.y;

        if (current_buildings.ContainsKey(key))
        {
            current_buildings.Remove(key);
        }
        Destroy(building.gameObject);
    }
}
