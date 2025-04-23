using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ExcelDataReader;
using System.IO;       // 用于 File 类
using System.Data;  
using System.Text;   // 用于 DataTable 和 DataSet
using System.Collections;
using Unity.VisualScripting;
public class Globals : MonoBehaviour
{
    public static Globals Instance { get; private set; }
    public class Datas 
    {
        public static string L1_Grid = "Prefabs/Level1_Grid";
        public static string L1_Objects = "Prefabs/Level1_Objects";
        GameObject Angle;
        GameObject Satan;
        public int seed;
        public enum Levels { Level1 , Level2 , Level3};
        public Levels current_level;
        public enum ResourcesType{ Green , Pink , Black }
        public GlobalTimer timer;
        Dictionary<ResourcesType , int> player_current_resources;
        public static List<Enemy> EnemyPool;
        public int enemy_wave_number;
        public float last_enemy_wave_time;
        public float waiting_time;
        public Dictionary<int, Dictionary<string, string>> Bulding_Datas { get; private set;} = new Dictionary<int, Dictionary<string, string>>();
        public Datas()
        {
            Angle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Angle").WaitForCompletion();
            Satan = Addressables.LoadAssetAsync<GameObject>("Prefabs/Satan").WaitForCompletion();
            enemy_wave_number = 0;
            last_enemy_wave_time = 0;
            waiting_time = 5f;
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
            Bulding_Datas = LoadBuldingDatas("Assets/Resources/Excels/Building_Properties.xlsx");
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

        public GameObject GetAnglePrefab()
        {
            return Angle;
        }

        public GameObject GetSatanPrefab()
        {
            return Satan;
        }

        Dictionary<int, Dictionary<string, string>> LoadBuldingDatas(string filePath)
        {
            // 创建一个字典用于保存数据
            var buildingData = new Dictionary<int, Dictionary<string, string>>();

            // 设置编码为 UTF-8
            var encoding = Encoding.GetEncoding(1252); 
            var configuration = new ExcelReaderConfiguration()
            {
                FallbackEncoding = encoding,
                LeaveOpen = false // 在使用 ExcelDataReader 库读取 Excel 文件时，ExcelReaderConfiguration 类中的 LeaveOpen 属性用于控制在读取完成后，是否保持原始数据流（Stream）的打开状态
            };

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream, configuration))
            {
                var result = reader.AsDataSet();
                DataTable table = result.Tables[0];

                if (table.Rows.Count < 2)
                {
                    Debug.LogError("Excel 文件数据不足！");
                    return null;
                }
                // 读取表头（第一行）
                var headers = new List<string>();
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    headers.Add(table.Rows[0][col].ToString());
                }

                // 遍历数据行（从第二行开始）
                for (int row = 1; row < table.Rows.Count; row++)
                {
                    var currentRow = table.Rows[row];
                    if (currentRow.ItemArray.Length == 0)
                        continue;

                    // 解析 ID（第一列必须为可转换成 int 的 ID）
                    if (!int.TryParse(currentRow[0].ToString(), out int id))
                    {
                        Debug.LogWarning($"ID 解析失败，行 {row + 1} 被跳过");
                        continue;
                    }

                    // 构建行数据（包含所有列）
                    var rowData = new Dictionary<string, string>();
                    for (int col = 0; col < headers.Count; col++)
                    {
                        string value = currentRow[col]?.ToString() ?? "null"; // 显式处理空值
                        rowData[headers[col]] = value;
                    }
                    
                    buildingData[id] = rowData;
                }

                Debug.Log("Excel 读取完成！");
            }

            // 返回构造好的数据字典
            return buildingData;
        }
    }

    public class Events 
    { 
        // 在 Globals.Datas 内或 Globals 类中新增标志变量：
        public bool waveActive = false;
        public enum GameState{ playing , pausing , timer_off}
        public GameState current_state;
        public GameObject world_item {get; private set;}
        public bool in_battle;
        public void GameStart()
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/ItemInWorld");
            handle.Completed += op => world_item = handle.Result;
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
            Event.in_battle = false;
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
        StartCoroutine(EnemyWavesCoroutine());
    }
    void Update()
    {
        // 检测窗口是否聚焦
        if (Application.isFocused)
        {
            // 获取鼠标位置（屏幕坐标）
            Vector2 mousePosition = Input.mousePosition;
            
            // 获取当前窗口的安全区域
            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);

            // 如果鼠标在窗口内部，则隐藏鼠标
            if (screenRect.Contains(mousePosition))
            {
                Cursor.lockState = CursorLockMode.Confined; // 限制鼠标在窗口内
                Cursor.visible = false; // 隐藏鼠标
            }
            else
            {
                Cursor.lockState = CursorLockMode.None; // 允许鼠标自由移动
                Cursor.visible = true; // 显示鼠标
            }
        }
        else
        {
            // 窗口不在聚焦状态时，解锁鼠标并显示
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    private static Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();
    private IEnumerator EnemyWavesCoroutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        while (true)
        {
            // 优先判断：如果游戏处于 playing 状态、计时器存在、敌人池为空且不在战斗中，则触发生成敌人波
            if (Event.current_state == Events.GameState.playing && Data.timer != null &&
                Datas.EnemyPool.Count == 0 && Event.in_battle == false)
            {
                // 检查等待时间条件
                if (((Data.timer.GetCurrentTime() - Data.last_enemy_wave_time) >= Data.waiting_time) && Data.enemy_wave_number <= 2)
                {
                    // 更新开始生成波的时间，同时标记为战斗中，防止重复触发
                    Data.last_enemy_wave_time = GlobalTimer.Instance.GetCurrentTime();
                    Event.in_battle = true;
                    StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 5, player.transform.position));
                    Data.enemy_wave_number += 1;
                    if (Data.enemy_wave_number == 3)
                    {
                        Data.waiting_time += 10f;
                    }
                }
                else if (((Data.timer.GetCurrentTime() - Data.last_enemy_wave_time) >= Data.waiting_time) && Data.enemy_wave_number <= 10 && Data.enemy_wave_number > 2)
                {
                    Data.last_enemy_wave_time = GlobalTimer.Instance.GetCurrentTime();
                    Event.in_battle = true;
                    if (Data.enemy_wave_number <= 3)
                    {
                        StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 4, player.transform.position));
                    } 
                    if (Data.enemy_wave_number <= 6)
                    {
                        StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 2, player.transform.position));
                        StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy1", 3, player.transform.position));
                    } 
                    else
                    {
                        StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 1, player.transform.position));
                        StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy1", 4, player.transform.position));                        
                    }
                    Data.enemy_wave_number += 1;
                }
            }
            // 奖励生成：只有当敌人池为空且仍处于战斗状态，并且距离上次记录的时间超过1秒后才发放奖励
            else if (Datas.EnemyPool.Count == 0 && Event.in_battle)
            {
                if ((Data.timer.GetCurrentTime() - Data.last_enemy_wave_time) > 1f)
                {
                    // 战斗结束：生成奖励道具，并重置状态
                    if (Data.enemy_wave_number % 3 == 0)
                    {
                        PlayerController player_controller = player.GetComponent<PlayerController>();
                        if (player_controller.player_properties.current_health > 0.65f * player_controller.player_properties.max_health)
                        {
                            GameObject statue = Instantiate(Data.GetSatanPrefab() , player.transform.position + new Vector3(0,3,0) , Quaternion.identity);
                            GameObject generated_item = GenerateRewardItem(player.transform.position, ItemFactory.PropertieItems, ItemFactory.AdvancedItems);
                            if (generated_item != null)
                                statue.GetComponent<Statues>().write_item(generated_item);
                        }
                        else
                        {
                            Instantiate(Data.GetAnglePrefab() , player.transform.position + new Vector3(0,3,0) , Quaternion.identity);
                            GenerateRewardItem(player.transform.position, ItemFactory.PropertieItems, ItemFactory.AdditionalAttack);
                        }

                        Data.last_enemy_wave_time = GlobalTimer.Instance.GetCurrentTime();
                        Event.in_battle = false;
                    }
                    else
                    {
                        int generating_item_id;
                        if (UnityEngine.Random.value < 0.6f)
                        {
                            generating_item_id = ItemFactory.PropertieItems[UnityEngine.Random.Range(0, ItemFactory.PropertieItems.Count)];
                        }
                        else if(ItemFactory.BulletEffectItems.Count != 0)
                        {
                            generating_item_id = ItemFactory.BulletEffectItems[UnityEngine.Random.Range(0, ItemFactory.BulletEffectItems.Count)];
                        }
                        else
                        {
                            generating_item_id = ItemFactory.PropertieItems[UnityEngine.Random.Range(0, ItemFactory.PropertieItems.Count)];
                        }
                        GameObject world_item_instance = Instantiate(this.Event.world_item, player.transform.position + new Vector3(0, 2, 0), Quaternion.identity);
                        Items.ItemRanks? rank = ItemFactory.GetRankByID(generating_item_id);
                        if (rank != null)
                        {
                            world_item_instance.GetComponent<ItemInWorld>().Initialize(generating_item_id, (Items.ItemRanks)rank);
                        }
                        Data.last_enemy_wave_time = GlobalTimer.Instance.GetCurrentTime();
                        Event.in_battle = false;
                    }
                }
            }
            yield return null;
        }
    }
    private GameObject GenerateRewardItem(Vector3 position, List<int> primaryList, List<int> fallbackList, float primaryChance = 0.6f)
    {
        int generating_item_id;

        if (primaryList.Count == 0 && fallbackList.Count == 0)
        {
            Debug.LogWarning("所有物品列表为空，无法生成奖励。");
            return null; // 或者 throw，根据你的需求
        }
        if (fallbackList.Count == 0 || UnityEngine.Random.value < primaryChance)
        {
            generating_item_id = primaryList[UnityEngine.Random.Range(0, primaryList.Count)];
        }
        else
        {
            generating_item_id = fallbackList[UnityEngine.Random.Range(0, fallbackList.Count)];
        }

        GameObject world_item_instance = Instantiate(this.Event.world_item, position + new Vector3(0, 2, 0), Quaternion.identity);
        Items.ItemRanks? rank = ItemFactory.GetRankByID(generating_item_id);
        if (rank != null)
        {
            world_item_instance.GetComponent<ItemInWorld>().Initialize(generating_item_id, (Items.ItemRanks)rank);
        }
        return world_item_instance;
    }

    private IEnumerator LoadAndSpawnEnemyWave(string enemyKey, int count, Vector3 playerPosition)
    {
        GameObject enemyPrefab = null;

        // 先检查字典中是否已经加载过这个敌人预制体
        if (enemyPrefabs.ContainsKey(enemyKey))
        {
            enemyPrefab = enemyPrefabs[enemyKey];
        }
        else
        {
            // 如果没有缓存，则加载并存入字典
            var handle = Addressables.LoadAssetAsync<GameObject>(enemyKey);
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                enemyPrefab = handle.Result;
                enemyPrefabs[enemyKey] = enemyPrefab;
            }
            else
            {
                Debug.LogError($"Failed to load enemy prefab: {enemyKey}");
                yield break;
            }
        }

        yield return StartCoroutine(SpawnEnemyWaveCoroutine(enemyPrefab, count, playerPosition));
    }


    private IEnumerator SpawnEnemyWaveCoroutine(GameObject enemyPrefab, int count, Vector3 playerPosition)
    {
        // 限制每秒最多生成的敌人数量
        int enemiesPerSecond = 2;
        float interval = 1f / enemiesPerSecond;

        for (int i = 0; i < count; i++)
        {
            float x_offset = UnityEngine.Random.Range(-15, 15);
            float y_offset = UnityEngine.Random.Range(-15, 15);

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

            Vector2 spawnPosition = (Vector2)playerPosition + new Vector2(x_offset, y_offset);
            spawnPosition.x = Mathf.Clamp(spawnPosition.x, 0, 199);
            spawnPosition.y = Mathf.Clamp(spawnPosition.y, 0, 199);

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Datas.EnemyPool.Add(enemy.GetComponent<Enemy>());
            Event.in_battle = true;
            // 等待下一次生成
            yield return new WaitForSeconds(interval);
        }
    }
    /// <summary>
    /// 获取距下一次战斗开始还剩多少秒（向上取整，最小为0）
    /// </summary>
    public int GetSecondsUntilNextWave()
    {
        // 计算下一波战斗的目标开始时间
        float nextWaveTime = Data.last_enemy_wave_time + Data.waiting_time;
        // 当前时间
        float now = GlobalTimer.Instance.GetCurrentTime();
        // 计算剩余时间
        float remaining = nextWaveTime - now;
        // 向上取整并保证非负
        int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(remaining));
        return secondsLeft;
    }
}
