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
        public Dictionary<int, Dictionary<string, string>> Bulding_Datas { get; private set;} = new Dictionary<int, Dictionary<string, string>>();
        public Datas()
        {
            enemy_wave_number = 0;
            last_enemy_wave_time = 0;
            waiting_time = 10;
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
            if (Event.current_state == Events.GameState.playing && Data.timer != null && Datas.EnemyPool.Count == 0)
            {
                if (Data.timer.GetCurrentTime() - Data.last_enemy_wave_time >= Data.waiting_time && Data.enemy_wave_number <= 2)
                {
                    StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 5, player.transform.position));
                    Data.enemy_wave_number += 1;
                    Data.last_enemy_wave_time = Data.timer.GetCurrentTime();
                    Data.waiting_time += 10;
                }
            }
            yield return null; // 等待下一帧
        }
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

            // 等待下一次生成
            yield return new WaitForSeconds(interval);
        }
    }
}
