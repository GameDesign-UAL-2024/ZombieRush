using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Runtime.InteropServices;
using Unity.Mathematics;
public class Globals : MonoBehaviour
{
    public static Globals Instance { get; private set; }
    public class Datas 
    {
        public static string L1_Grid = "Prefabs/Level1_Grid";
        public static string L1_Objects = "Prefabs/Level1_Objects";
        public int seed;
        public enum Levels { Level1 , Level2 , Level3};
        public Levels current_level;
        public enum ResourcesType{ Green , Pink , Black }
        public GlobalTimer timer;
        Dictionary<ResourcesType , int> player_current_resources;
        public static List<Enemy> EnemyPool;
        public int enemy_wave_number;
        public float last_enemy_wave_time;
        public int waiting_time;
        public Datas()
        {
            enemy_wave_number = 0;
            last_enemy_wave_time = 0;
            waiting_time = 30;
            current_level = Levels.Level1;
            EnemyPool = new List<Enemy>() ;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string timestampStr = timestamp.ToString();
            string lastFourDigits = timestampStr.Length >= 4 ? timestampStr.Substring(timestampStr.Length - 4) : timestampStr;
            player_current_resources = new Dictionary<ResourcesType, int>
            {
                { ResourcesType.Green, 0},
                { ResourcesType.Black, 0},
                { ResourcesType.Pink , 0}
            };
            // 转换为整数
            seed = int.Parse(lastFourDigits);
        }
        public int GetResourceAmount(ResourcesType target_type)
        {
            return player_current_resources[target_type];
        }
        public void AddResourceAmount(ResourcesType target_type , int amount)
        {
            player_current_resources[target_type] += amount;
            PlayerUI player_ui = PlayerUI.Instance;
            if (player_ui != null)
            {
                player_ui.UpdateValues(target_type,player_current_resources[target_type]);
            }
        }
    }

    public class Events 
    {
        public enum GameState{ playing , pausing}
        public GameState current_state;
        public void GameStart()
        {
            current_state = GameState.playing;
            GameObject GridObject = GameObject.FindGameObjectWithTag("ChunkGenerator");
            if (GridObject != null){Destroy(GridObject);}

            Addressables.LoadAssetAsync<GameObject>(Globals.Datas.L1_Grid).Completed += OnGridPrefabLoaded;
        }
        private void OnGridPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject gridPrefab = handle.Result;
                if (gridPrefab != null)
                {
                    // 实例化 Prefab 到场景中
                    Instantiate(gridPrefab, new Vector3(0,0,10), Quaternion.identity);
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
    }
    public Datas Data { get; private set; }
    public Events Event { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Data = new Datas();
            Event = new Events();
            Event.current_state = Events.GameState.pausing; 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {

        Screen.SetResolution(1920, 1080, false);
        this.Data.timer = GlobalTimer.Instance;
    }
    void Update()
    {
        EnemyWaves();
    }

    void EnemyWaves()
    {
        if (this.Event.current_state == Events.GameState.playing && this.Data.timer != null && Datas.EnemyPool.Count == 0)
        {
            if (this.Data.timer.GetCurrentTime() - this.Data.last_enemy_wave_time >= Data.waiting_time)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return; // 避免空引用异常

                if (this.Data.enemy_wave_number <= 2)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        float x_offset = UnityEngine.Random.Range(-15, 15);
                        float y_offset = UnityEngine.Random.Range(-15, 15);

                        // 让 x 和 y 独立变化，避免都变成 -10 或 10
                        bool adjustXFirst = UnityEngine.Random.value > 0.5f;

                        if (adjustXFirst)
                        {
                            x_offset = (x_offset <= 0) ? UnityEngine.Random.Range(-15, -10) : UnityEngine.Random.Range(10, 15);
                            y_offset = (y_offset <= 0) ? UnityEngine.Random.Range(-15, -11) : UnityEngine.Random.Range(11, 15);
                        }
                        else
                        {
                            y_offset = (y_offset <= 0) ? UnityEngine.Random.Range(-15, -10) : UnityEngine.Random.Range(10, 15);
                            x_offset = (x_offset <= 0) ? UnityEngine.Random.Range(-15, -11) : UnityEngine.Random.Range(11, 15);
                        }

                        Vector2 spawnPosition = (Vector2)player.transform.position + new Vector2(x_offset, y_offset);
                        spawnPosition.x = Math.Clamp(spawnPosition.x, 0, 199);
                        spawnPosition.y = Math.Clamp(spawnPosition.y, 0, 199);
                        // 通过 Lambda 捕获参数，保证异步回调时仍然能获取正确的偏移量
                        Addressables.LoadAssetAsync<GameObject>("Prefabs/Enemy0").Completed += (handle) =>
                        {
                            OnEnemyLoaded(handle, spawnPosition);
                        };
                    }
                    Data.enemy_wave_number += 1;
                    Data.last_enemy_wave_time = Data.timer.GetCurrentTime();
                    Data.waiting_time += 10;
                }
            }
        }
    }

    void OnEnemyLoaded(AsyncOperationHandle<GameObject> handle, Vector2 spawnPosition)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            GameObject enemy = Instantiate(handle.Result, spawnPosition, Quaternion.identity);
            Datas.EnemyPool.Add(enemy.GetComponent<Enemy>());
        }
        else
        {
            Debug.LogError("Failed to load enemy prefab.");
        }
    }
}
