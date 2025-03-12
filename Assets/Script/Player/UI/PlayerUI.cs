using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class PlayerUI : MonoBehaviour
{
    public GameObject building_panel_content;
    const string building_button = "Prefabs/BuildingButton";

    GameObject button_prefab;
    public static PlayerUI Instance {get; private set;}

    public Dictionary<int ,Dictionary<string , string>> All_Buildings_Data;
    Dictionary<int , Dictionary<string , int>> Buildings_Need = new Dictionary<int, Dictionary<string, int>>();
    static Dictionary<int , BuildingButton> BuildingButtonList = new Dictionary<int, BuildingButton>();
    string Green_Need_Key = "Resources_Green_Need";
    string Black_Need_Key = "Resources_Black_Need";
    string Pink_Need_Key = "Resources_Pink_Need";
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
    public TextMeshProUGUI green_value;
    public TextMeshProUGUI black_value;
    // public TextMeshPro pink_value;
    Coroutine get_data;
    Globals globals;
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
            foreach (KeyValuePair<int , Dictionary<string , string>> kv in All_Buildings_Data)
            {
                int.TryParse(kv.Value[Green_Need_Key] , out int g_result);
                int.TryParse(kv.Value[Black_Need_Key] , out int b_result);
                int.TryParse(kv.Value[Pink_Need_Key] , out int p_result);
                Buildings_Need[kv.Key] = new Dictionary<string, int>()
                {
                    { Green_Need_Key, g_result },
                    { Black_Need_Key, b_result },
                    { Pink_Need_Key, p_result }
                };
                print(kv.Key + "|" + Green_Need_Key + ":" + g_result);
                print(kv.Key + "|" + Black_Need_Key + ":" + b_result);
                print(kv.Key + "|" + Pink_Need_Key + ":" + p_result);
            }
            yield break;
        }
        yield return null;
    }
    private IEnumerator ValidBuildings()
    {
        if (button_prefab == null)
        {
            button_prefab = Addressables.LoadAssetAsync<GameObject>(building_button).Result;
        }
        print(Buildings_Need.Count + "|" + (button_prefab != null).ToString() + "|" + (All_Buildings_Data != null).ToString());
        if (Buildings_Need.Count != 0 && button_prefab != null && All_Buildings_Data != null)
        {
            foreach (KeyValuePair<int , Dictionary<string , int>> kv in Buildings_Need)
            {
                if (globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Green) >= kv.Value[Green_Need_Key] &&
                    globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Pink) >= kv.Value[Pink_Need_Key] &&
                    globals.Data.GetResourceAmount(Globals.Datas.ResourcesType.Black) >= kv.Value[Black_Need_Key])
                {
                    print(3);
                    if ( ! BuildingButtonList.ContainsKey(kv.Key))
                    {
                        BuildingButton button = Instantiate(button_prefab , building_panel_content.transform).GetComponent<BuildingButton>();
                        BuildingButtonList.Add(kv.Key , button);
                        button.Initialize(All_Buildings_Data[kv.Key]["UI_Image_Path"] , All_Buildings_Data[kv.Key],kv.Value[Green_Need_Key],kv.Value[Black_Need_Key],kv.Value[Pink_Need_Key],OnClickingBuilding);
                    }
                }
                else if (BuildingButtonList.ContainsKey(kv.Key))
                {
                    Destroy(BuildingButtonList[kv.Key]);
                    BuildingButtonList.Remove(kv.Key);
                }
            }  
            yield break;
        }
        yield return null;
    }
    private bool isValidBuildingsRunning = false;

    void Update()
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
    public void UpdateValues(Globals.Datas.ResourcesType the_type , int number)
    {
        if ( the_type == Globals.Datas.ResourcesType.Green )
        {
            green_value.text = number.ToString();
        }
        else if ( the_type == Globals.Datas.ResourcesType.Black )
        {
            black_value.text = number.ToString();
        }
        else
        {

        }
    }

    public void OnClickingBuilding()
    {

    }
}
