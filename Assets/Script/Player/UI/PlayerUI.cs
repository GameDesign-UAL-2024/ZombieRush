using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;
using System.Drawing;

public class PlayerUI : MonoBehaviour
{
    public GameObject building_panel_content;
    const string building_button = "Prefabs/BuildingButton";
    [SerializeField]public UnityEngine.UI.Image ProactiveItemImage;
    public TextMeshProUGUI CoolDownText;
    GameObject button_prefab;
    public static PlayerUI Instance { get; private set; }

    public Dictionary<int, Dictionary<string, string>> All_Buildings_Data;
    Dictionary<int, Dictionary<string, int>> Buildings_Need = new Dictionary<int, Dictionary<string, int>>();
    static Dictionary<int, BuildingButton> BuildingButtonList = new Dictionary<int, BuildingButton>();
    string Green_Need_Key = "Resources_Green_Need";
    string Black_Need_Key = "Resources_Black_Need";
    string Pink_Need_Key = "Resources_Pink_Need";

    public TextMeshProUGUI green_value;
    public TextMeshProUGUI black_value;
    public TextMeshProUGUI pink_value;
    // public TextMeshProUGUI pink_value; // 可根据需要补充
    [SerializeField]private Descriptions description;
    Coroutine get_data;
    Globals globals;

    // 控制协程调用的标志
    private bool isValidBuildingsRunning = false;

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

    void Start()
    {
        globals = Globals.Instance;
        get_data = StartCoroutine(GetBuildingsData());
    }

    private IEnumerator GetBuildingsData()
    {
        if (globals.Data.Bulding_Datas.Count != 0)
        {
            All_Buildings_Data = globals.Data.Bulding_Datas;
            foreach (KeyValuePair<int, Dictionary<string, string>> kv in All_Buildings_Data)
            {
                int.TryParse(kv.Value[Green_Need_Key], out int g_result);
                int.TryParse(kv.Value[Black_Need_Key], out int b_result);
                int.TryParse(kv.Value[Pink_Need_Key], out int p_result);
                Buildings_Need[kv.Key] = new Dictionary<string, int>()
                {
                    { Green_Need_Key, g_result },
                    { Black_Need_Key, b_result },
                    { Pink_Need_Key, p_result }
                };
            }
            yield break;
        }
        yield return null;
    }

    // 触发更新的入口，当资源变化时调用
    public void TriggerValidBuildingsUpdate()
    {
        if (!isValidBuildingsRunning)
        {
            StartCoroutine(RunValidBuildingsCoroutine());
        }
    }

    private IEnumerator RunValidBuildingsCoroutine()
    {
        isValidBuildingsRunning = true;
        yield return StartCoroutine(ValidBuildings());
        isValidBuildingsRunning = false;
    }

    private IEnumerator ValidBuildings()
    {
        if (button_prefab == null)
        {
            // 建议使用异步加载，防止阻塞
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(building_button);
            yield return handle;
            button_prefab = handle.Result;
        }
        if (Buildings_Need.Count != 0 && button_prefab != null && All_Buildings_Data != null)
        {
            // 用于记录需要移除的按钮ID
            List<int> keysToRemove = new List<int>();

            foreach (KeyValuePair<int, Dictionary<string, int>> kv in Buildings_Need)
            {
                int buildingID = kv.Key;
                int greenNeeded = kv.Value[Green_Need_Key];
                int blackNeeded = kv.Value[Black_Need_Key];
                int pinkNeeded = kv.Value[Pink_Need_Key];

                bool hasSufficient = globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Green) >= greenNeeded &&
                                      globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Black) >= blackNeeded &&
                                      globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Pink) >= pinkNeeded;

                if (hasSufficient)
                {
                    if (!BuildingButtonList.ContainsKey(buildingID))
                    {
                        BuildingButton button = Instantiate(button_prefab, building_panel_content.transform).GetComponent<BuildingButton>();
                        BuildingButtonList.Add(buildingID, button);
                        button.Initialize(All_Buildings_Data[buildingID]["UI_Image_Path"], buildingID, All_Buildings_Data[buildingID],
                                          greenNeeded, blackNeeded, pinkNeeded, OnClickingBuilding);
                    }
                }
                else
                {
                    if (BuildingButtonList.ContainsKey(buildingID))
                    {
                        // 先销毁按钮，再记录ID以便移除字典中的条目
                        Destroy(BuildingButtonList[buildingID].gameObject);
                        keysToRemove.Add(buildingID);
                    }
                }
            }

            foreach (int key in keysToRemove)
            {
                BuildingButtonList.Remove(key);
            }
        }
        yield break;
    }

    public void UpdateValues(Globals.Datas.ResourcesType the_type, int number)
    {
        if (the_type == Globals.Datas.ResourcesType.Green)
        {
            green_value.text = number.ToString();
        }
        else if (the_type == Globals.Datas.ResourcesType.Black)
        {
            black_value.text = number.ToString();
        }
        else
        {
            pink_value.text = number.ToString();
        }
        TriggerValidBuildingsUpdate();
    }

    public void OnClickingBuilding(int id)
    {
        StartCoroutine(PlaceBuildingCoroutine(id));
    }

    private IEnumerator PlaceBuildingCoroutine(int id)
    {
        // 通过 All_Buildings_Data 获取 UI_Image_Path，并加载对应精灵
        if (!All_Buildings_Data.ContainsKey(id))
        {
            Debug.LogError($"No building data found for id: {id}");
            yield break;
        }
        string spritePath = All_Buildings_Data[id]["UI_Image_Path"];
        Sprite buildingSprite = Resources.Load<Sprite>(spritePath);
        if (buildingSprite == null)
        {
            Debug.LogError($"Failed to load sprite from path: {spritePath}");
            yield break;
        }
        
        // 创建新的游戏对象，并添加 SpriteRenderer
        GameObject buildingPreview = new GameObject("BuildingPreview");
        SpriteRenderer sr = buildingPreview.AddComponent<SpriteRenderer>();
        sr.sprite = buildingSprite;
        
        // 循环更新：让新建对象跟随鼠标
        while (buildingPreview != null)
        {
            // 将鼠标屏幕坐标转换为世界坐标，并将位置取整
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // 保证在2D平面上
            Vector2Int gridPos = Vector2Int.FloorToInt(mousePos);
            buildingPreview.transform.position = new Vector3(gridPos.x, gridPos.y, 0f);
            // 检查这个位置是否合法
            bool isValid = PlayerBuildingManager.Instance.IsPositionValid(gridPos);
            sr.color = isValid ? UnityEngine.Color.white : UnityEngine.Color.red;
            
            // 检测左键：输出信息并销毁对象
            if (Input.GetMouseButtonDown(0) && isValid)
            {
                PlayerBuildingManager.Instance.SpawnBuilding(All_Buildings_Data[id]["Prefab_Path"] , gridPos);
                int.TryParse(All_Buildings_Data[id][Green_Need_Key], out int g_need);
                int.TryParse(All_Buildings_Data[id][Black_Need_Key], out int b_need);
                int.TryParse(All_Buildings_Data[id][Pink_Need_Key], out int p_need);
                globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Green,-g_need);
                globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Black,-b_need);
                globals.Data.AddResourceAmount(Globals.Datas.ResourcesType.Pink,-p_need);
                Destroy(buildingPreview);
                yield break;
            }
            // 检测右键：直接销毁对象
            if (Input.GetMouseButtonDown(1))
            {
                Destroy(buildingPreview);
                yield break;
            }
            yield return null;
        }
    }
    public void ShowDescription(int id , int type)
    {
        description.UpdateDescription(id,type);
    }
    public void ClearDescription()
    {
        description.ClearLocalizationReferences();
    }
    public void OnClickingBuildingPanelButton(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = (cg.alpha == 0f) ? 1f : 0f;
            cg.interactable = cg.alpha == 1f;
            cg.blocksRaycasts = cg.alpha == 1f;
        }
    }
}
