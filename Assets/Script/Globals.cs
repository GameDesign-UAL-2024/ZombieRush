
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ExcelDataReader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Text;
public class Globals : MonoBehaviour
{
    public static Globals Instance { get; private set; }
    [SerializeField] AudioClip BattleBGM;

    public Datas Data { get; private set; }
    public Events Event { get; private set; }

    private void Awake()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Application.targetFrameRate = 60;
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

    private void Start()
    {
        Screen.SetResolution(1920, 1080, false);
        Data.timer = GlobalTimer.Instance;
        StartCoroutine(InitializeGlobals());
    }

    private IEnumerator InitializeGlobals()
    {
        yield return Addressables.InitializeAsync();
        yield return StartCoroutine(Data.InitializeDatas());

        StartCoroutine(EnemyWavesCoroutine());
    }

    void Update()
    {
        if (Application.isFocused)
        {
            Vector2 mousePosition = Input.mousePosition;
            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);

            if (screenRect.Contains(mousePosition))
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public class Datas
    {
        public static string L1_Grid = "Prefabs/Level1_Grid";
        public static string L1_Objects = "Prefabs/Level1_Objects";
        private GameObject Angle;
        private GameObject Satan;
        public int seed;
        public enum Levels { Level1, Level2, Level3 }
        public Levels current_level;
        public enum ResourcesType { Green, Pink, Black }
        public GlobalTimer timer;
        public Dictionary<ResourcesType, int> player_current_resources { get; private set; }
        public static List<Enemy> EnemyPool;
        public int enemy_wave_number;
        public float last_enemy_wave_time;
        public float waiting_time;
        public Dictionary<int, Dictionary<string, string>> Bulding_Datas { get; private set; }

        public Datas()
        {
            enemy_wave_number = 0;
            last_enemy_wave_time = 0;
            waiting_time = 5f;
            current_level = Levels.Level1;
            EnemyPool = new List<Enemy>();

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string lastFourDigits = timestamp.ToString("D4")[^4..];
            seed = int.Parse(lastFourDigits);

            player_current_resources = new Dictionary<ResourcesType, int>
            {
                { ResourcesType.Green, 0 },
                { ResourcesType.Black, 0 },
                { ResourcesType.Pink, 0 }
            };
        }

        public IEnumerator InitializeDatas()
        {
            var angleHandle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Angle");
            yield return angleHandle;
            Angle = angleHandle.Result;

            var satanHandle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Satan");
            yield return satanHandle;
            Satan = satanHandle.Result;

            string excelPath = Path.Combine(Application.streamingAssetsPath, "Excels/Building_Properties.xlsx");
            yield return Instance.StartCoroutine(LoadBuildingDatasCoroutine(excelPath, dict => Bulding_Datas = dict));
        }

        private IEnumerator LoadBuildingDatasCoroutine(string path, Action<Dictionary<int, Dictionary<string, string>>> callback)
        {
            byte[] fileData;
            if (path.Contains("://"))
            {
                UnityWebRequest www = UnityWebRequest.Get(path);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load Excel file: " + www.error);
                    yield break;
                }
                fileData = www.downloadHandler.data;
            }
            else
            {
                fileData = File.ReadAllBytes(path);
            }

            var buildingData = new Dictionary<int, Dictionary<string, string>>();

            using (var stream = new MemoryStream(fileData))
            using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
            {
                var result = reader.AsDataSet();
                DataTable table = result.Tables[0];
                if (table.Rows.Count < 2)
                {
                    Debug.LogError("Excel 文件数据不足！");
                    yield break;
                }

                var headers = new List<string>();
                for (int col = 0; col < table.Columns.Count; col++)
                    headers.Add(table.Rows[0][col].ToString());

                for (int row = 1; row < table.Rows.Count; row++)
                {
                    var currentRow = table.Rows[row];
                    if (!int.TryParse(currentRow[0]?.ToString(), out int id))
                        continue;

                    var rowData = new Dictionary<string, string>();
                    for (int col = 0; col < headers.Count; col++)
                        rowData[headers[col]] = currentRow[col]?.ToString() ?? "null";

                    buildingData[id] = rowData;
                }

                Debug.Log("Excel 读取完成！");
                callback?.Invoke(buildingData);
            }
        }

        public int GetResourceAmount(ResourcesType target_type) => player_current_resources[target_type];

        public void AddResourceAmount(ResourcesType target_type, int amount)
        {
            player_current_resources[target_type] += amount;
            PlayerUI player_ui = PlayerUI.Instance;
            player_ui?.UpdateValues(target_type, player_current_resources[target_type]);
        }

        public GameObject GetAnglePrefab() => Angle;
        public GameObject GetSatanPrefab() => Satan;
    }

    public class Events
    {
        public bool waveActive = false;
        public enum GameState { playing, pausing, timer_off }
        public GameState current_state;
        public GameObject world_item { get; private set; }
        public bool in_battle;

        public void GameStart()
        {
            Addressables.LoadAssetAsync<GameObject>("Prefabs/ItemInWorld")
                .Completed += op => world_item = op.Result;

            GameObject GridObject = GameObject.FindGameObjectWithTag("ChunkGenerator");
            if (GridObject != null) GameObject.Destroy(GridObject);

            Addressables.LoadAssetAsync<GameObject>(Globals.Datas.L1_Grid)
                .Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                        GameObject.Instantiate(handle.Result, new Vector3(0, 0, 10), Quaternion.identity);
                };
        }
    }

    private static Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();
    private IEnumerator EnemyWavesCoroutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        while (true)
        {
            // 1) 等待满足：正在 play、计时器就绪、敌人池清空、未在战斗中
            if (Event.current_state == Events.GameState.playing &&
                Data.timer != null &&
                Datas.EnemyPool.Count == 0 &&
                Event.in_battle == false)
            {
                float elapsed = Data.timer.GetCurrentTime() - Data.last_enemy_wave_time;
                if (elapsed >= Data.waiting_time)
                {
                    // 2) 跨越到下一波
                    Data.enemy_wave_number++;
                    Data.last_enemy_wave_time = Data.timer.GetCurrentTime();
                    Event.in_battle = true;

                    Vector3 spawnCenter = player.transform.position;
                    int wave = Data.enemy_wave_number;
                    AudioSysManager.Instance.PlayBGM(BattleBGM,120);
                    // 3) 根据波数选择出怪规则
                    switch (wave)
                    {
                        case 1:
                        case 2:
                            // 第1、2波：5只 Enemy0
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 5, spawnCenter));
                            
                            break;

                        case 3:
                            // 第3波：10只 Enemy0，且延长下次间隔
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy0", 10, spawnCenter));
                            
                            Data.waiting_time += 10f;
                            break;

                        case 4:
                        case 5:
                            StartCoroutine(LoadAndSpawnEnemyWave(
                                "Prefabs/Enemy0",
                                UnityEngine.Random.value < 0.5f ? 4 : 6,
                                spawnCenter
                            ));
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy1", 2, spawnCenter));
                            
                            
                            break;

                        case 6:
                            StartCoroutine(LoadAndSpawnEnemyWave(
                                "Prefabs/Enemy0",
                                UnityEngine.Random.value < 0.5f ? 3 : 5,
                                spawnCenter
                            ));
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy4", 1, spawnCenter));
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy1", 1, spawnCenter));
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/E3",2,spawnCenter));
                            
                            break;
                        case 7:
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy2", 4, spawnCenter));
                            break;
                        case 8:
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy5", 1 , spawnCenter));
                            break;
                        case 9:
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/E3",4,spawnCenter));
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/Enemy4", 4, spawnCenter));
                            break;

                        case 10:
                            StartCoroutine(LoadAndSpawnEnemyWave("Prefabs/MaoDie", 1 , spawnCenter));
                            break;
                    }
                }
            }
            // 4) 波内敌人清空后发奖励
            else if (Datas.EnemyPool.Count == 0 && Event.in_battle)
            {
                
                if ((Data.timer.GetCurrentTime() - Data.last_enemy_wave_time) > 1f)
                {
                    // 战斗结束，发奖励并重置状态
                    AudioSysManager.Instance.StopOverrideBGM();
                    if (Data.enemy_wave_number % 3 == 0)
                    {
                        // 每第三波的大奖励逻辑（带雕像 + 3件）
                        Data.last_enemy_wave_time = Data.timer.GetCurrentTime();
                        Event.in_battle = false;
                        PlayerController pc = player.GetComponent<PlayerController>();
                        if (pc.player_properties.current_health > 0.65f * pc.player_properties.max_health)
                        {
                            GameObject statue = Instantiate(Data.GetSatanPrefab(),
                                                            player.transform.position + new Vector3(0, 3, 0),
                                                            Quaternion.identity);
                            GameObject i1 = GenerateRewardItem(player.transform.position + new Vector3(-3, 0, 0),
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.AdvancedItems, 0.7f);
                            GameObject i2 = GenerateRewardItem(player.transform.position,
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.BulletEffectItems);
                            GameObject i3 = GenerateRewardItem(player.transform.position + new Vector3(3, 0, 0),
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.AdditionalAttack, 0.65f);
                            if (i1 != null && i2 != null && i3 != null)
                                statue.GetComponent<Statues>().write_items(i1, i2, i3);
                        }
                        else
                        {
                            GameObject statue = Instantiate(Data.GetAnglePrefab(),
                                                            player.transform.position + new Vector3(0, 3, 0),
                                                            Quaternion.identity);
                            GameObject i1 = GenerateRewardItem(player.transform.position + new Vector3(-3, 0, 0),
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.AdvancedItems, 0.85f);
                            GameObject i2 = GenerateRewardItem(player.transform.position,
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.ProactiveItems, 0.65f);
                            GameObject i3 = GenerateRewardItem(player.transform.position + new Vector3(3, 0, 0),
                                                                ItemFactory.PropertieItems,
                                                                ItemFactory.AdditionalAttack, 0.75f);
                            if (i1 != null && i2 != null && i3 != null)
                                statue.GetComponent<Statues>().write_items(i1, i2, i3);
                        }
                    }
                    else
                    {
                        // 其它波次奖励逻辑
                        int id;
                        if (UnityEngine.Random.value < 0.6f)
                            id = ItemFactory.PropertieItems[UnityEngine.Random.Range(0, ItemFactory.PropertieItems.Count)];
                        else if (ItemFactory.BulletEffectItems.Count > 0)
                            id = ItemFactory.BulletEffectItems[UnityEngine.Random.Range(0, ItemFactory.BulletEffectItems.Count)];
                        else
                            id = ItemFactory.PropertieItems[UnityEngine.Random.Range(0, ItemFactory.PropertieItems.Count)];

                        GameObject item = Instantiate(this.Event.world_item,
                                                    player.transform.position + new Vector3(0, 2, 0),
                                                    Quaternion.identity);
                        var rank = ItemFactory.GetRankByID(id);
                        if (rank != null)
                            item.GetComponent<ItemInWorld>().Initialize(id, rank.Value);

                        Data.last_enemy_wave_time = Data.timer.GetCurrentTime();
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

    public IEnumerator LoadAndSpawnEnemyWave(string enemyKey, int count, Vector3 playerPosition)
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
