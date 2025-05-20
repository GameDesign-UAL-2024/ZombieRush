using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerUI : MonoBehaviour
{
    public GameObject building_panel_content;
    public GameObject building_panel;
    string building_button_path = "Prefabs/BuildingButton";

    [SerializeField] public Image ProactiveItemImage;
    public TextMeshProUGUI CoolDownText;
    public Image BuildingPanelButton;

    public static PlayerUI Instance { get; private set; }

    public Dictionary<int, Dictionary<string, string>> All_Buildings_Data;
    private Dictionary<int, Dictionary<string, int>> Buildings_Need = new Dictionary<int, Dictionary<string, int>>();
    private static Dictionary<int, BuildingButton> BuildingButtonList = new Dictionary<int, BuildingButton>();

    public TextMeshProUGUI green_value;
    public TextMeshProUGUI black_value;
    public TextMeshProUGUI pink_value;

    [SerializeField] private Descriptions description;

    // 同步加载并缓存好的按钮 Prefab
    private  static GameObject button_prefab;

    // 协程控制
    private Coroutine get_data;
    private bool isValidBuildingsRunning = false;
    private Coroutine panelFlashCoroutine;

    // 暂存要移除的键，复用列表避免 GC
    private readonly List<int> keysToRemove = new List<int>();

    // 资源键名
    private readonly string Green_Need_Key = "Resources_Green_Need";
    private readonly string Black_Need_Key = "Resources_Black_Need";
    private readonly string Pink_Need_Key  = "Resources_Pink_Need";

    private Globals globals;

    void Awake()
    {
        if (Instance == null) Instance = this;

        else { Destroy(gameObject); return; }
        Addressables.InitializeAsync().Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                Debug.Log("Addressables 初始化成功");
            else
                Debug.LogError("Addressables 初始化失败：" + handle.OperationException);
        };
        // 提前加载，确保其它脚本访问时已准备好
        button_prefab = Addressables.LoadAssetAsync<GameObject>(building_button_path).WaitForCompletion();

        if (button_prefab == null)
            Debug.LogError($"加载 {building_button_path} 失败");
    }

    void Start()
    {
        globals = Globals.Instance;
        
        // // 必须同步加载 Prefab 保证稳定
        // GameObject button_prefab = Addressables.LoadAssetAsync<GameObject>(building_button_path).WaitForCompletion();
        // print(button_prefab);
        // if (button_prefab == null)
        //     Debug.LogError($"加载 {building_button_path} 失败");

        get_data = StartCoroutine(GetBuildingsData());
    }
    void FixedUpdate()
    {
        TriggerValidBuildingsUpdate();
    }
    private IEnumerator GetBuildingsData()
    {
        // 等待数据准备好
        while (globals.Data.Bulding_Datas == null || globals.Data.Bulding_Datas.Count == 0)
            yield return null;

        All_Buildings_Data = globals.Data.Bulding_Datas;

        foreach (var kv in All_Buildings_Data)
        {
            int g = int.Parse(kv.Value[Green_Need_Key]);
            int b = int.Parse(kv.Value[Black_Need_Key]);
            int p = int.Parse(kv.Value[Pink_Need_Key]);
            Buildings_Need[kv.Key] = new Dictionary<string, int>
            {
                { Green_Need_Key, g },
                { Black_Need_Key, b },
                { Pink_Need_Key,  p }
            };
            yield return null;
        }
        Debug.Log($"[PlayerUI] 成功读取建筑配置：{Buildings_Need.Count} 个建筑");
    }


    public void TriggerValidBuildingsUpdate()
    {
        if (!isValidBuildingsRunning)
            StartCoroutine(RunValidBuildingsCoroutine());
    }

    private IEnumerator RunValidBuildingsCoroutine()
    {
        isValidBuildingsRunning = true;
        yield return StartCoroutine(ValidBuildings());
        isValidBuildingsRunning = false;
    }

    private IEnumerator ValidBuildings()
    {
        if (Buildings_Need.Count == 0 || All_Buildings_Data == null || button_prefab == null)
            yield break;

        keysToRemove.Clear();

        foreach (var kv in Buildings_Need)
        {
            int id = kv.Key;
            var need = kv.Value;
            bool ok =
                globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Green) >= need[Green_Need_Key]
             && globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Black) >= need[Black_Need_Key];

            if (ok)
            {
                if (!BuildingButtonList.ContainsKey(id))
                {
                    var go = Instantiate(button_prefab, building_panel_content.transform);
                    var btn = go.GetComponent<BuildingButton>();
                    BuildingButtonList[id] = btn;
                    btn.Initialize(
                        All_Buildings_Data[id]["UI_Image_Path"],
                        id,
                        All_Buildings_Data[id],
                        need[Green_Need_Key],
                        need[Black_Need_Key],
                        need[Pink_Need_Key],
                        OnClickingBuilding
                    );
                    btn.StartFlash();
                    StartPanelFlash();
                }
            }
            else if (globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Pink) >= need[Pink_Need_Key])
            {
                if (!BuildingButtonList.ContainsKey(id))
                {
                    var go = Instantiate(button_prefab, building_panel_content.transform);
                    var btn = go.GetComponent<BuildingButton>();
                    BuildingButtonList[id] = btn;
                    btn.Initialize(
                        All_Buildings_Data[id]["UI_Image_Path"],
                        id,
                        All_Buildings_Data[id],
                        need[Green_Need_Key],
                        need[Black_Need_Key],
                        need[Pink_Need_Key],
                        OnClickingBuilding
                    );
                    btn.StartFlash();
                    StartPanelFlash();
                }          
            }
            else if (BuildingButtonList.ContainsKey(id))
            {
                Destroy(BuildingButtonList[id].gameObject);
                keysToRemove.Add(id);
            }
        }

        foreach (int id in keysToRemove)
            BuildingButtonList.Remove(id);

        yield break;
    }

    public void UpdateValues(Globals.Datas.ResourcesType type, int number)
    {
        switch (type)
        {
            case Globals.Datas.ResourcesType.Green:
                green_value.text = number.ToString(); break;
            case Globals.Datas.ResourcesType.Black:
                black_value.text = number.ToString(); break;
            case Globals.Datas.ResourcesType.Pink:
                pink_value.text = number.ToString(); break;
        }
    }

    public void OnClickingBuilding(int id)
    {
        StartCoroutine(PlaceBuildingCoroutine(id));
    }

    private IEnumerator PlaceBuildingCoroutine(int id)
    {
        if (!All_Buildings_Data.ContainsKey(id))
        {
            Debug.LogError($"No building data for id {id}");
            yield break;
        }

        string spritePath = All_Buildings_Data[id]["UI_Image_Path"];
        var spriteHandle = Addressables.LoadAssetAsync<Sprite>(spritePath);
        var spr = spriteHandle.WaitForCompletion();
        if (spr == null)
        {
            Debug.LogError($"加载精灵失败：{spritePath}");
            yield break;
        }

        var preview = new GameObject("BuildingPreview");
        var sr = preview.AddComponent<SpriteRenderer>();
        sr.sprite = spr;

        while (preview)
        {
            var m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m.z = 0;
            var grid = Vector2Int.FloorToInt(m);
            preview.transform.position = new Vector3(grid.x, grid.y, 0f);

            bool valid = PlayerBuildingManager.Instance.IsPositionValid(grid);
            sr.color = valid ? Color.white : Color.red;

            if (Input.GetMouseButtonDown(0) && valid)
            {
                PlayerBuildingManager.Instance.SpawnBuilding(
                    All_Buildings_Data[id]["Prefab_Path"], grid
                );
                var need = Buildings_Need[id];
                if (globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Pink) >= need[Pink_Need_Key])
                {
                    globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Pink,  -need[Pink_Need_Key]);
                }
                else
                {
                    globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Green, -need[Green_Need_Key]);
                    globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Black, -need[Black_Need_Key]);
                }
                OnClickingBuildingPanelButton(building_panel);
                Destroy(preview);
                yield break;
            }
            if (Input.GetMouseButtonDown(1))
            {
                OnClickingBuildingPanelButton(building_panel);
                Destroy(preview);
                yield break;
            }
            yield return null;
        }
    }

    private void StartPanelFlash()
    {
        StopPanelFlash();
        if (BuildingPanelButton != null)
            panelFlashCoroutine = StartCoroutine(PanelFlashCoroutine());
    }

    private void StopPanelFlash()
    {
        if (panelFlashCoroutine != null)
        {
            StopCoroutine(panelFlashCoroutine);
            panelFlashCoroutine = null;
        }
        if (BuildingPanelButton != null)
        {
            BuildingPanelButton.CrossFadeAlpha(1f, 0f, true);
            BuildingPanelButton.rectTransform.localScale = Vector3.one;
        }
    }

    private IEnumerator PanelFlashCoroutine()
    {
        float minAlpha = 0.2f, maxAlpha = 1f;
        float minScale = 1f, maxScale = 1.25f;
        float dur = 0.3f;
        var img  = BuildingPanelButton;
        var rect = img.rectTransform;
        Color c   = img.color;

        while (true)
        {
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float n = t / dur;
                c.a = Mathf.Lerp(maxAlpha, minAlpha, n);
                img.color = c;
                rect.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, n);
                yield return null;
            }
            c.a = minAlpha; img.color = c;
            rect.localScale = Vector3.one * maxScale;

            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float n = t / dur;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, n);
                img.color = c;
                rect.localScale = Vector3.one * Mathf.Lerp(maxScale, minScale, n);
                yield return null;
            }
            c.a = maxAlpha; img.color = c;
            rect.localScale = Vector3.one * minScale;
        }
    }

    public void OnClickingBuildingPanelButton(GameObject panel)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) return;
        bool open = cg.alpha == 0f;
        cg.alpha = open ? 1f : 0f;
        cg.interactable    = open;
        cg.blocksRaycasts = open;
        StopPanelFlash();
    }

    public void ShowDescription(int id, int type)
    {
        description.UpdateDescription(id, type);
    }

    public void ClearDescription()
    {
        description.ClearLocalizationReferences();
    }
}