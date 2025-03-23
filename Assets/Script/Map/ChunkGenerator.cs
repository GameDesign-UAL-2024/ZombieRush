using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
public class ChunkGenerator : MonoBehaviour
{
    private void Awake()
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
    public static ChunkGenerator Instance;
    public Tilemap tilemap;
    public RuleTile grassTile;
    public RuleTile sandTile;
    public RuleTile waterTile;
    public RuleTile GetGridType(string name)
    {
        if (name == "Grass"){return grassTile;}
        else if (name == "Sand"){return sandTile;}
        else{return waterTile;}
    }
    private Dictionary<Vector3Int, RuleTile> tileDictionary = new Dictionary<Vector3Int, RuleTile>();

    void Start()
    {
        GenerateMap();
        ApplyPostProcessing(); // 进行后处理，确保地形不会出现 1 格宽的过渡
        if (Globals.Instance.Data.current_level == Globals.Datas.Levels.Level1)
        {
            Addressables.LoadAssetAsync<GameObject>(Globals.Datas.L1_Objects).Completed += OnObjectPrefabLoaded;
        }
        
    }
    private void OnObjectPrefabLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject gridPrefab = handle.Result;
            if (gridPrefab != null)
            {
                // 实例化 Prefab 到场景中
                Instantiate(gridPrefab, Vector3.zero, Quaternion.identity);
                gridPrefab.GetComponent<ObjectSpawner>().InitializeObjectData(tileDictionary);
                GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().SetObjectSpawner(gridPrefab.GetComponent<ObjectSpawner>());
            }
            else
            {
                Debug.LogError("Grid Prefab is null.");
            }
        }
        else
        {
            Debug.LogError("Failed to load the Grid Prefab.");
        }
    }
    void GenerateMap()
    {
        float baseNoiseScale = 0.03f;  // 使地形变化更缓慢
        float detailNoiseScale = 0.08f; // 细节减少

        float influence = 0.2f; // 细节层的影响力

        for (int x = 0; x < 200; x++)
        {
            for (int y = 0; y < 200; y++)
            {
                float baseNoise = Mathf.PerlinNoise(
                    (x + Globals.Instance.Data.seed) * baseNoiseScale,
                    (y + Globals.Instance.Data.seed) * baseNoiseScale
                );

                float detailNoise = Mathf.PerlinNoise(
                    (x + Globals.Instance.Data.seed) * detailNoiseScale,
                    (y + Globals.Instance.Data.seed) * detailNoiseScale
                );

                float finalNoise = baseNoise * (1 - influence) + detailNoise * influence; // 组合噪声

                RuleTile selectedTile = SelectTile(finalNoise);

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePosition, selectedTile);
                tileDictionary[tilePosition] = selectedTile;
            }
        }
    }

    RuleTile SelectTile(float noiseValue)
    {
        if (noiseValue < 0.35f)
            return waterTile;
        else if (noiseValue < 0.4f)
            return sandTile;
        else
            return grassTile;
    }

    void ApplyPostProcessing()
    {
        foreach (var tileEntry in tileDictionary)
        {
            Vector3Int pos = tileEntry.Key;
            RuleTile tileType = tileEntry.Value;

            int horizontalMatch = 0;
            int verticalMatch = 0;

            if (tileDictionary.TryGetValue(new Vector3Int(pos.x - 1, pos.y, 0), out RuleTile left) && left == tileType)
                horizontalMatch++;
            if (tileDictionary.TryGetValue(new Vector3Int(pos.x + 1, pos.y, 0), out RuleTile right) && right == tileType)
                horizontalMatch++;

            if (tileDictionary.TryGetValue(new Vector3Int(pos.x, pos.y - 1, 0), out RuleTile down) && down == tileType)
                verticalMatch++;
            if (tileDictionary.TryGetValue(new Vector3Int(pos.x, pos.y + 1, 0), out RuleTile up) && up == tileType)
                verticalMatch++;

            // 确保至少有 1 个同类型的 Tile 在水平或垂直方向上
            if (horizontalMatch == 0 && verticalMatch == 0)
            {
                tilemap.SetTile(pos, SelectTile(Random.value)); // 重新分配 Tile
            }
        }
    }
    public Dictionary<Vector3Int, RuleTile> GetTileDictionary()
    {
        return tileDictionary;
    }    
}
