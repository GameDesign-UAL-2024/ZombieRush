using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] rockPrefabs;
    [SerializeField]
    int dictionary_range;
    public static Dictionary<Vector3, string> objectRecords { get; private set; }
    static Dictionary<Vector3, GameObject> spawnedObjects;
    private int spawnRange = 20;

    // 预制体查找字典，改用延迟初始化，避免空引用问题
    private Dictionary<string, GameObject> prefabLookup;

    // 延迟初始化预制体查找字典的方法
    private void EnsurePrefabLookupInitialized()
    {
        if (prefabLookup == null)
        {
            prefabLookup = new Dictionary<string, GameObject>();
            if (treePrefabs != null)
            {
                foreach (var tree in treePrefabs)
                {
                    if (tree != null && !prefabLookup.ContainsKey(tree.name))
                        prefabLookup.Add(tree.name, tree);
                }
            }
            if (bushPrefabs != null)
            {
                foreach (var bush in bushPrefabs)
                {
                    if (bush != null && !prefabLookup.ContainsKey(bush.name))
                        prefabLookup.Add(bush.name, bush);
                }
            }
            if (rockPrefabs != null)
            {
                foreach (var rock in rockPrefabs)
                {
                    if (rock != null && !prefabLookup.ContainsKey(rock.name))
                        prefabLookup.Add(rock.name, rock);
                }
            }
        }
    }

    // 如果 ObjectSpawner 是挂在场景上的，Awake 会优先调用
    void Awake()
    {
        EnsurePrefabLookupInitialized();
    }

    public void InitializeObjectData(Dictionary<Vector3Int, RuleTile> tileDictionary)
    {
        spawnedObjects = new Dictionary<Vector3, GameObject>();
        objectRecords = new Dictionary<Vector3, string>();
        foreach (var tile in tileDictionary)
        {
            if (tile.Value.name == "Grass")
            {
                Vector3 worldPos = new Vector3(tile.Key.x, tile.Key.y, 0);
                objectRecords[worldPos] = null;
            }
        }
        GenerateObjects();
    }

    private void GenerateObjects()
    {
        List<Vector3> grassPositions = new List<Vector3>(objectRecords.Keys);
        int forestCount = Random.Range(20, 40);
        int plainCount = Random.Range(30, 60);
        GenerateForests(forestCount, grassPositions);
        GeneratePlains(plainCount, grassPositions);
    }

    private void GenerateForests(int count, List<Vector3> positions)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 center = positions[Random.Range(0, positions.Count)];
            int treeCount = Random.Range(20, 70);
            List<Vector3> occupied = new List<Vector3>();

            for (int j = 0; j < treeCount; j++)
            {
                Vector3 pos = GetValidPosition(center, occupied, 3);
                if (pos != Vector3.zero)
                {
                    // 随机选择一个具体的树的预制体名称并存入 objectRecords
                    GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Length)];
                    objectRecords[pos] = selectedTree.name;
                    occupied.Add(pos);
                }
            }
        }
    }

    private void GeneratePlains(int count, List<Vector3> positions)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 center = positions[Random.Range(0, positions.Count)];
            int bushCount = Random.Range(8, 30);
            int rockCount = Random.Range(4, 20);
            List<Vector3> occupied = new List<Vector3>();

            for (int j = 0; j < bushCount; j++)
            {
                Vector3 pos = GetValidPosition(center, occupied, 6);
                if (pos != Vector3.zero)
                {
                    GameObject selectedBush = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
                    objectRecords[pos] = selectedBush.name;
                    occupied.Add(pos);
                }
            }
            for (int j = 0; j < rockCount; j++)
            {
                Vector3 pos = GetValidPosition(center, occupied, 5);
                if (pos != Vector3.zero)
                {
                    GameObject selectedRock = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                    objectRecords[pos] = selectedRock.name;
                    occupied.Add(pos);
                }
            }
        }
    }

    private Vector3 GetValidPosition(Vector3 center, List<Vector3> occupied, float minDistance)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 pos = center + new Vector3(Random.Range(-5, 6), Random.Range(-5, 6), 0);
            bool valid = true;

            for (int i = 0; i < occupied.Count; i++)
            {
                if (Vector3.Distance(occupied[i], pos) < minDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid && objectRecords.ContainsKey(pos) && objectRecords[pos] == null)
            {
                return pos;
            }
        }
        return Vector3.zero;
    }

    public void UpdateSpawnedObjects(Vector3 playerPosition)
    {
        if (objectRecords == null || spawnedObjects == null)
        {
            Debug.LogWarning("ObjectSpawner 未初始化，请先调用 InitializeObjectData 方法哦！");
            return;
        }

        List<Vector3> toSpawn = new List<Vector3>();
        List<Vector3> toRemove = new List<Vector3>();

        float sqrRange = spawnRange * spawnRange;
        foreach (var entry in objectRecords)
        {
            float distSqr = (playerPosition - entry.Key).sqrMagnitude;
            if (distSqr <= sqrRange && !spawnedObjects.ContainsKey(entry.Key) && entry.Value != null)
            {
                toSpawn.Add(entry.Key);
            }
            else if (distSqr > sqrRange && spawnedObjects.ContainsKey(entry.Key))
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (var pos in toSpawn)
        {
            SpawnObject(pos, objectRecords[pos]);
        }

        foreach (var pos in toRemove)
        {
            Destroy(spawnedObjects[pos]);
            spawnedObjects.Remove(pos);
        }
        dictionary_range = spawnedObjects.Count;
        UpdateZOrders();
    }

    private void SpawnObject(Vector3 pos, string prefabName)
    {
        // 确保预制体查找字典被初始化啦
        EnsurePrefabLookupInitialized();
        
        GameObject prefab = null;
        if (prefabLookup.TryGetValue(prefabName, out prefab))
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
            spawnedObjects[pos] = obj;
        }
        else
        {
            Debug.LogWarning("未在预制体查找字典中找到名称为 " + prefabName + " 的预制体！");
        }
    }

    public void RemoveObject(GameObject obj)
    {
        Vector3 keyToRemove = default(Vector3);
        bool found = false;
        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value == obj)
            {
                keyToRemove = kvp.Key;
                found = true;
                break;
            }
        }

        if (found)
        {
            spawnedObjects.Remove(keyToRemove);
            objectRecords.Remove(keyToRemove);
        }
        Destroy(obj);
    }

    void UpdateZOrders()
    {
        List<GameObject> rootSpawnedObjects = new List<GameObject>();
        foreach (var obj in spawnedObjects.Values)
        {
            if (obj != null && obj.transform.parent == null)
            {
                rootSpawnedObjects.Add(obj);
            }
        }

        List<GameObject> taggedObjects = new List<GameObject>();
        foreach (string tag in new string[] { "Player", "Resources", "PlayerObjects", "Enemy", "PlayerBuddies" })
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
            {
                if (obj != null && obj.transform.parent == null)
                {
                    taggedObjects.Add(obj);
                }
            }
        }

        List<GameObject> allObjects = new List<GameObject>(rootSpawnedObjects);
        allObjects.AddRange(taggedObjects);

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        foreach (var obj in allObjects)
        {
            if (obj != null)
            {
                float currentY = obj.transform.position.y;
                minY = Mathf.Min(minY, currentY);
                maxY = Mathf.Max(maxY, currentY);
            }
        }

        float yRange = maxY - minY;
        float zScale = (yRange == 0) ? 0 : 1.0f / yRange;

        foreach (var obj in allObjects)
        {
            if (obj != null)
            {
                Vector3 pos = obj.transform.position;
                pos.z = (pos.y - maxY) * zScale;
                obj.transform.position = pos;
            }
        }
    }
}
