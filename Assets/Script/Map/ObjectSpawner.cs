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
    void Awake()
    {
        Debug.Log($"ObjectSpawner 被挂载在："+ gameObject.GetInstanceID());
    }

    Dictionary<Vector3, string> objectRecords;
    Dictionary<Vector3, GameObject> spawnedObjects;
    private int spawnRange = 20;
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

            foreach (var occupiedPos in occupied)
            {
                if (Vector3.Distance(occupiedPos, pos) < minDistance)
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
        List<Vector3> toSpawn = new List<Vector3>();
        List<Vector3> toRemove = new List<Vector3>();

        foreach (var entry in objectRecords)
        {
            float distance = Vector2.Distance(playerPosition, entry.Key);
            if (distance <= spawnRange && !spawnedObjects.ContainsKey(entry.Key) && entry.Value != null)
            {
                toSpawn.Add(entry.Key);
            }
            else if (distance > spawnRange && spawnedObjects.ContainsKey(entry.Key))
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
    }
    private void SpawnObject(Vector3 pos, string prefabName)
    {
        GameObject prefab = null;

        // 在 treePrefabs、bushPrefabs 和 rockPrefabs 中查找匹配的预制体
        foreach (var t in treePrefabs)
        {
            if (t.name == prefabName)
            {
                prefab = t;
                break;
            }
        }
        if (prefab == null)
        {
            foreach (var b in bushPrefabs)
            {
                if (b.name == prefabName)
                {
                    prefab = b;
                    break;
                }
            }
        }
        if (prefab == null)
        {
            foreach (var r in rockPrefabs)
            {
                if (r.name == prefabName)
                {
                    prefab = r;
                    break;
                }
            }
        }

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
            spawnedObjects[pos] = obj;
        }
    }

    public void RemoveObject(GameObject obj)
    {
        print(spawnedObjects);
    }

}
